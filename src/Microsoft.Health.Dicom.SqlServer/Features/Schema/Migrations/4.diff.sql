-- TODO: dbo.ReindexStatus?
CREATE TABLE dbo.ReindexStore (
    TagKey                  INT                  NOT NULL, --PK
    OperationId             VARCHAR(40)          NULL,
    ReindexStatus           TINYINT              NOT NULL, -- 0: Processing, 1: Paused, 2: Completed
    -- TODO: should we allow either Start or EndWatermark as NULL?
    StartWatermark          BIGINT               NULL, 
    EndWatermark            BIGINT               NULL, 
)
GO
-- TODO: Add index for ReindexStore

-- TODO: better naming?
-- TODO: Also include operationId?
CREATE TYPE  dbo.PrepareReindexingTableType_1 AS TABLE
(
    TagKey   INT -- TagKey
)
GO

CREATE PROCEDURE dbo.PrepareReindexing(
    @tagKeys dbo.PrepareReindexingTableType_1 READONLY,
    @operationId VARCHAR(40)
)
AS
    SET NOCOUNT ON
    SET XACT_ABORT ON
    BEGIN  TRANSACTION
    -- TODO: verify all tagkeys are valid and also lock them for editing
    -- Add tagKey and operationId combination to ReindexStore
    -- TODO: performance improve? -- don't need to each time call SELECT MIN/MAX?
    INSERT INTO dbo.ReindexStore (TagKey, OperationId, ReindexStatus, StartWatermark, EndWatermark)
    SELECT
    TagKey,
    @operationId,
    0,
    (SELECT MIN(Watermark) FROM dbo.Instance),
    (SELECT MAX(Watermark) FROM dbo.Instance)
    FROM @tagKeys
    
    -- Update tagStatus to be Adding
    UPDATE dbo.ExtendedQueryTag SET TagStatus = 0
    WHERE TagKey IN (SELECT TagKey FROM @tagKeys)

    -- join with ExtendeQueryTagStore to return all information
    SELECT
    E.TagKey,
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
    INNER JOIN @tagKeys I
    ON E.TagKey = I.TagKey
    INNER JOIN dbo.ReindexStore R
    ON E.TagKey = R.TagKey
    
    COMMIT TRANSACTION
GO

CREATE PROCEDURE dbo.CompleteReindexing(
    @operationId VARCHAR(40)
)
AS
    SET NOCOUNT ON
    SET XACT_ABORT ON
    BEGIN  TRANSACTION
    -- 1. Update ReindexStore.ReindexStatus to Completed (2)
    -- 2. Update ExtendedQueryTag.TagStatus to Ready (1)
    -- TODO: verify all tagkeys are valid and also lock them for editing
    UPDATE dbo.ReindexStore SET ReindexStatus = 1
    WHERE OperationId = @operationId

    UPDATE dbo.ExtendedQueryTag SET TagStatus = 1
    WHERE TagKey IN
    (SELECT TagKey FROM dbo.ReindexStore WHERE OperationId = @operationId)
    
    COMMIT TRANSACTION
GO


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
       IF EXISTS
       (
            SELECT TagKey 
            FROM dbo.ExtendedQueryTag WITH(HOLDLOCK) 
            INNER JOIN @extendedQueryTags input 
            ON input.TagPath = dbo.ExtendedQueryTag.TagPath
       )
            THROW 50409, 'extended query tag(s) already exist', 2

        -- add to extended query tag table with status 1(Ready)
        INSERT INTO dbo.ExtendedQueryTag 
            (TagKey, TagPath, TagPrivateCreator, TagVR, TagLevel, TagStatus)
        SELECT NEXT VALUE FOR TagKeySequence, TagPath, TagPrivateCreator, TagVR, TagLevel, 1 FROM @extendedQueryTags

        -- Return tags
        SELECT
        E.TagKey,
        E.TagPath,
        E.TagVR,
        E.TagPrivateCreator,
        E.TagLevel,
        E.TagStatus
        FROM dbo.ExtendedQueryTag E INNER JOIN @extendedQueryTags I
        ON E.TagPath = I.TagPath

    COMMIT TRANSACTION
GO
