﻿/*************************************************************
    Extended Query Tag Errors Table
    Stores errors from Extended Query Tag operations
    TagKey and Watermark is Primary Key
**************************************************************/
CREATE TABLE dbo.ExtendedQueryTagError (
    TagKey                  INT             NOT NULL, --FK
    ErrorCode               SMALLINT        NOT NULL,
    Watermark               BIGINT          NOT NULL,
    CreatedTime             DATETIME2(7)    NOT NULL,
)

CREATE UNIQUE CLUSTERED INDEX IXC_ExtendedQueryTagError ON dbo.ExtendedQueryTagError
(
    TagKey,
    Watermark
)

CREATE NONCLUSTERED INDEX IX_ExtendedQueryTagError_CreatedTime_Watermark_TagKey ON dbo.ExtendedQueryTagError
(
    CreatedTime,
    Watermark,
    TagKey
)
INCLUDE
(
    ErrorCode
)
