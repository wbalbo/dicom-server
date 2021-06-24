/***************************************************************************************/
-- STORED PROCEDURE
--     AddExtendedQueryTags
--
-- DESCRIPTION
--    Add a list of extended query tags.
--
-- PARAMETERS
--     @extendedQueryTags
--         * The extended query tag list
--     @maxCount
--         * The max allowed extended query tag count
/***************************************************************************************/
ALTER PROCEDURE dbo.AddExtendedQueryTags (
    @extendedQueryTags dbo.AddExtendedQueryTagsInputTableType_1 READONLY,
    @initStatus TINYINT,  --- 0 - Adding, 1 - Ready, 2 - Deleting
    @maxAllowedCount INT
)
AS

    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    BEGIN TRANSACTION
        -- Check if total count exceed @maxCount
        -- HOLDLOCK to prevent adding queryTags from other transactions at same time.
        IF ((SELECT COUNT(*) FROM dbo.ExtendedQueryTag WITH(HOLDLOCK)) + (SELECT COUNT(*) FROM @extendedQueryTags)) > @maxAllowedCount 
             THROW 50409, 'extended query tags exceed max allowed count', 1 
        
        -- Check if tag with same path already exist
        SELECT TagKey 
        FROM dbo.ExtendedQueryTag WITH(HOLDLOCK) 
        INNER JOIN @extendedQueryTags input 
        ON input.TagPath = dbo.ExtendedQueryTag.TagPath 
	    
        IF @@ROWCOUNT <> 0
            THROW 50409, 'extended query tag(s) already exist', 2 

        -- add to extended query tag table with status 1(Ready)
        INSERT INTO dbo.ExtendedQueryTag 
            (TagKey, TagPath, TagPrivateCreator, TagVR, TagLevel, TagStatus)
        SELECT NEXT VALUE FOR TagKeySequence, TagPath, TagPrivateCreator, TagVR, TagLevel, @initStatus FROM @extendedQueryTags
        
    COMMIT TRANSACTION
GO
/*************************************************************
    Reindex State Table. 
    Reindex state on each extended query tag.
**************************************************************/
CREATE TABLE dbo.ReindexState (
    TagKey                  INT                  NOT NULL,
    OperationId             VARCHAR(40)          NULL,
    ReindexStatus           TINYINT              NOT NULL, -- 0: Processing, 1: Paused, 2: Completed
    StartWatermark          BIGINT               NULL, 
    EndWatermark            BIGINT               NULL, 
) WITH (DATA_COMPRESSION = PAGE)

-- One tag should have and only have one entry.
CREATE UNIQUE CLUSTERED INDEX IXC_ReindexState on dbo.ReindexState
(
    TagKey
)
GO

/*************************************************************
    Table valued parameter to provide tag keys to PrepareReindexing
*************************************************************/
CREATE TYPE  dbo.PrepareReindexingTableType_1 AS TABLE
(
    TagKey                     INT -- TagKey
)
GO

/***************************************************************************************/
-- STORED PROCEDURE
--    PrepareReindexing
--
-- DESCRIPTION
--    Prepare reindexing on tags with operation id.
--
-- PARAMETERS
--     @@tagKeys
--         * The tag keys.
--     @@operationId
--         * The operation id
/***************************************************************************************/
CREATE PROCEDURE dbo.PrepareReindexing(
    @tagKeys dbo.PrepareReindexingTableType_1 READONLY,
    @operationId VARCHAR(40)
)
AS
    SET NOCOUNT ON
    SET XACT_ABORT ON
    BEGIN  TRANSACTION

        -- Validate: tagkeys should be in extendedQueryTag with status of Adding
        IF
        (
            (
                SELECT COUNT(1) FROM @tagKeys
            )
            <>
            (
                SELECT COUNT(1) FROM dbo.ExtendedQueryTag E WITH (REPEATABLEREAD) 
                INNER JOIN @tagKeys I
                ON E.TagKey = I.TagKey
                AND E.TagStatus = 0 -- 0 - Adding, 1 - Ready, 2 - Deleting
            )
        )
            THROW 50412, 'Not all tags are valid for reindexing', 1
        
        -- Add tagKey and operationId combination to ReindexState table
        INSERT INTO dbo.ReindexState
        (TagKey, OperationId, ReindexStatus, StartWatermark, EndWatermark)
        SELECT  TagKey,
                @operationId,
                0, -- 0 - Processing, 1 - Paused, 2 - Completed
                ( SELECT MIN(Watermark) FROM dbo.Instance ),
                ( SELECT MAX(Watermark) FROM dbo.Instance )
        FROM @tagKeys
    
        -- Join with ExtendeQueryTag to return all information
        SELECT  E.TagKey,
                E.TagPath,
                E.TagVR,
                E.TagPrivateCreator,
                E.TagLevel,
                E.TagStatus,
                R.OperationId,
                R.ReindexStatus,
                R.StartWatermark,
                R.EndWatermark
        FROM    dbo.ExtendedQueryTag E
        INNER JOIN @tagKeys I
        ON      E.TagKey = I.TagKey
        INNER JOIN dbo.ReindexState R
        ON      E.TagKey = R.TagKey
    
    COMMIT TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--    GetReindexStateEntries
--
-- DESCRIPTION
--    Get reindex state entries.
--
-- PARAMETERS
--     @operationId
--         * The operation id
/***************************************************************************************/
CREATE PROCEDURE dbo.GetReindexStateEntries (
     @operationId VARCHAR(40)
)
AS
    -- TODO: create index on operationId
    SET NOCOUNT     ON
    SET XACT_ABORT  ON
    SELECT  E.TagKey,
            E.TagPath,
            E.TagVR,
            E.TagPrivateCreator,
            E.TagLevel,
            E.TagStatus,
            R.OperationId,
            R.ReindexStatus,
            R.StartWatermark,
            R.EndWatermark
    FROM dbo.ExtendedQueryTag E
    INNER JOIN dbo.ReindexState R
    ON E.TagKey = R.TagKey and R.OperationId = @operationId
GO

 /***************************************************************************************/
-- STORED PROCEDURE
--    CompleteReindexing
--
-- DESCRIPTION
--    Complete reindexing.
--
-- PARAMETERS
--     @operationId
--         * The operation id
/***************************************************************************************/
CREATE PROCEDURE dbo.CompleteReindexing(
    @operationId VARCHAR(40)
)
AS
    SET NOCOUNT ON
    SET XACT_ABORT ON
    BEGIN  TRANSACTION
        -- 1. Update ExtendedQueryTag.TagStatus to Ready (1)        
        -- 2. Remove entries whose operationId is @operationId from ReindexState table
        -- TODO: verify all tagkeys are valid and also lock them for editing

        UPDATE dbo.ExtendedQueryTag SET TagStatus = 1
        WHERE TagKey IN
        (SELECT TagKey FROM dbo.ReindexState WHERE OperationId = @operationId)

        DELETE FROM dbo.ReindexState
        WHERE OperationId = @operationId
    
    COMMIT TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--    Reindex instance
--
-- DESCRIPTION
--    Reidex instance
--
-- PARAMETERS
--     @studyInstanceUid
--         * The study instance Uid
--     @seriesInstanceUid
--         * The series instance Uid
/***************************************************************************************/
CREATE PROCEDURE dbo.ReindexInstance
    @studyInstanceUid                   VARCHAR(64),
    @seriesInstanceUid                  VARCHAR(64),
    @sopInstanceUid                     VARCHAR(64),               
    @stringExtendedQueryTags dbo.InsertStringExtendedQueryTagTableType_1 READONLY,    
    @longExtendedQueryTags dbo.InsertLongExtendedQueryTagTableType_1 READONLY,
    @doubleExtendedQueryTags dbo.InsertDoubleExtendedQueryTagTableType_1 READONLY,
    @dateTimeExtendedQueryTags dbo.InsertDateTimeExtendedQueryTagTableType_1 READONLY,
    @personNameExtendedQueryTags dbo.InsertPersonNameExtendedQueryTagTableType_1 READONLY
AS
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

        DECLARE @studyKey BIGINT
        DECLARE @seriesKey BIGINT
        DECLARE @instanceKey BIGINT
        DECLARE @watermark BIGINT
        -- Add lock so that the instance won't be removed    
        SELECT
            @studyKey = StudyKey,
            @seriesKey = SeriesKey,
            @instanceKey = InstanceKey,
            @watermark = Watermark
        FROM dbo.Instance WITH (REPEATABLEREAD) 
        WHERE StudyInstanceUid = @studyInstanceUid
            AND SeriesInstanceUid = @seriesInstanceUid
            AND SopInstanceUid = @sopInstanceUid
            AND Status = 1 -- Created

      -- TODO: updat message and code
      IF @@ROWCOUNT = 0
        THROW 50409, 'Instance not exists or in invalid status', 1

    -- Insert Extended Query Tags

    -- String Key tags
    IF EXISTS (SELECT 1 FROM @stringExtendedQueryTags)
    BEGIN      
        MERGE INTO dbo.ExtendedQueryTagString AS T
        USING 
        (
            -- Locks tags in dbo.ExtendedQueryTag
            SELECT input.TagKey, input.TagValue, input.TagLevel 
            FROM @stringExtendedQueryTags input
            INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD) 
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey
            -- Not merge on extended query tag which is being deleted.
            AND dbo.ExtendedQueryTag.TagStatus <> 2     
        ) AS S
        ON T.TagKey = S.TagKey        
            AND T.StudyKey = @studyKey
            -- Null SeriesKey indicates a Study level tag, no need to compare SeriesKey
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey      
            -- Null InstanceKey indicates a Study/Series level tag, no to compare InstanceKey
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED THEN
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = IIF(@watermark > T.Watermark, @watermark, T.Watermark), T.TagValue = IIF(@watermark > T.Watermark, S.TagValue, T.TagValue)
        WHEN NOT MATCHED THEN 
            INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
            VALUES(
            S.TagKey,
            S.TagValue,
            @studyKey,
            -- When TagLevel is not Study, we should fill SeriesKey
            (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),
            -- When TagLevel is Instance, we should fill InstanceKey
            (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
            @watermark);        
    END

    -- Long Key tags
    IF EXISTS (SELECT 1 FROM @longExtendedQueryTags)
    BEGIN      
        MERGE INTO dbo.ExtendedQueryTagLong AS T
        USING 
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel 
            FROM @longExtendedQueryTags input
            INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD) 
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey            
            AND dbo.ExtendedQueryTag.TagStatus <> 2     
        ) AS S
        ON T.TagKey = S.TagKey        
            AND T.StudyKey = @studyKey            
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey           
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED THEN 
             -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = IIF(@watermark > T.Watermark, @watermark, T.Watermark), T.TagValue = IIF(@watermark > T.Watermark, S.TagValue, T.TagValue)
        WHEN NOT MATCHED THEN 
            INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
            VALUES(
            S.TagKey,
            S.TagValue,
            @studyKey,            
            (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),            
            (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
            @watermark);        
    END

    -- Double Key tags
    IF EXISTS (SELECT 1 FROM @doubleExtendedQueryTags)
    BEGIN      
        MERGE INTO dbo.ExtendedQueryTagDouble AS T
        USING 
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel 
            FROM @doubleExtendedQueryTags input
            INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD) 
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey            
            AND dbo.ExtendedQueryTag.TagStatus <> 2     
        ) AS S
        ON T.TagKey = S.TagKey        
            AND T.StudyKey = @studyKey            
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey           
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED THEN 
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = IIF(@watermark > T.Watermark, @watermark, T.Watermark), T.TagValue = IIF(@watermark > T.Watermark, S.TagValue, T.TagValue)
        WHEN NOT MATCHED THEN 
            INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
            VALUES(
            S.TagKey,
            S.TagValue,
            @studyKey,            
            (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),            
            (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
            @watermark);        
    END

    -- DateTime Key tags
    IF EXISTS (SELECT 1 FROM @dateTimeExtendedQueryTags)
    BEGIN      
        MERGE INTO dbo.ExtendedQueryTagDateTime AS T
        USING 
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel 
            FROM @dateTimeExtendedQueryTags input
            INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD) 
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey            
            AND dbo.ExtendedQueryTag.TagStatus <> 2     
        ) AS S
        ON T.TagKey = S.TagKey        
            AND T.StudyKey = @studyKey            
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey           
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED THEN 
             -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = IIF(@watermark > T.Watermark, @watermark, T.Watermark), T.TagValue = IIF(@watermark > T.Watermark, S.TagValue, T.TagValue)
        WHEN NOT MATCHED THEN 
            INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
            VALUES(
            S.TagKey,
            S.TagValue,
            @studyKey,            
            (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),            
            (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
            @watermark);        
    END

    -- PersonName Key tags
    IF EXISTS (SELECT 1 FROM @personNameExtendedQueryTags)
    BEGIN      
        MERGE INTO dbo.ExtendedQueryTagPersonName AS T
        USING 
        (
            SELECT input.TagKey, input.TagValue, input.TagLevel 
            FROM @personNameExtendedQueryTags input
            INNER JOIN dbo.ExtendedQueryTag WITH (REPEATABLEREAD) 
            ON dbo.ExtendedQueryTag.TagKey = input.TagKey            
            AND dbo.ExtendedQueryTag.TagStatus <> 2     
        ) AS S
        ON T.TagKey = S.TagKey        
            AND T.StudyKey = @studyKey            
            AND ISNULL(T.SeriesKey, @seriesKey) = @seriesKey           
            AND ISNULL(T.InstanceKey, @instanceKey) = @instanceKey
        WHEN MATCHED THEN 
            -- When index already exist, update only when watermark is newer
            UPDATE SET T.Watermark = IIF(@watermark > T.Watermark, @watermark, T.Watermark), T.TagValue = IIF(@watermark > T.Watermark, S.TagValue, T.TagValue)
        WHEN NOT MATCHED THEN 
            INSERT (TagKey, TagValue, StudyKey, SeriesKey, InstanceKey, Watermark)
            VALUES(
            S.TagKey,
            S.TagValue,
            @studyKey,            
            (CASE WHEN S.TagLevel <> 2 THEN @seriesKey ELSE NULL END),            
            (CASE WHEN S.TagLevel = 0 THEN @instanceKey ELSE NULL END),
            @watermark);        
    END

    COMMIT TRANSACTION
GO

/***************************************************************************************/
-- STORED PROCEDURE
--     GetInstance
--
-- DESCRIPTION
--     Gets valid dicom instances at study/series/instance level
--
-- PARAMETERS
--     @invalidStatus
--         * Filter criteria to search only valid instances
/***************************************************************************************/
CREATE PROCEDURE dbo.GetInstanceByWatermark (
    @validStatus        TINYINT,
    @startWatermark     BIGINT,
    @endWatermark       BIGINT
)
AS
BEGIN
    SET NOCOUNT     ON
    SET XACT_ABORT  ON

    SELECT  StudyInstanceUid,
            SeriesInstanceUid,
            SopInstanceUid,
            Watermark
    FROM    dbo.Instance
    WHERE   Watermark BETWEEN @startWatermark AND @endWatermark
            AND Status              = @validStatus
END
GO
