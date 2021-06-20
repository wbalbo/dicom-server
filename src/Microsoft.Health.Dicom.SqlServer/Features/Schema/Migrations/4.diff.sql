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

