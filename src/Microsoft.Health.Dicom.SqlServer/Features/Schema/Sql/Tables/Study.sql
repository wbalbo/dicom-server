﻿/*************************************************************
    Study Table
    Table containing normalized standard Study tags
**************************************************************/
CREATE TABLE dbo.Study (
    StudyKey                    BIGINT                            NOT NULL, --PK
    StudyInstanceUid            VARCHAR(64)                       NOT NULL,
    PatientId                   NVARCHAR(64)                      NOT NULL,
    PatientName                 NVARCHAR(200)                     COLLATE SQL_Latin1_General_CP1_CI_AI NULL,
    ReferringPhysicianName      NVARCHAR(200)                     COLLATE SQL_Latin1_General_CP1_CI_AI NULL,
    StudyDate                   DATE                              NULL,
    StudyDescription            NVARCHAR(64)                      NULL,
    AccessionNumber             NVARCHAR(16)                      NULL,
    PatientNameWords            AS REPLACE(REPLACE(PatientName, '^', ' '), '=', ' ') PERSISTED,
    ReferringPhysicianNameWords AS REPLACE(REPLACE(ReferringPhysicianName, '^', ' '), '=', ' ') PERSISTED,
    PatientBirthDate            DATE                              NULL
) WITH (DATA_COMPRESSION = PAGE)

CREATE UNIQUE CLUSTERED INDEX IXC_Study ON dbo.Study
(
    StudyKey
)

CREATE UNIQUE NONCLUSTERED INDEX IX_Study_StudyInstanceUid ON dbo.Study
(
    StudyInstanceUid
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Study_PatientId ON dbo.Study
(
    PatientId
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Study_PatientName ON dbo.Study
(
    PatientName
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Study_ReferringPhysicianName ON dbo.Study
(
    ReferringPhysicianName
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Study_StudyDate ON dbo.Study
(
    StudyDate
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Study_StudyDescription ON dbo.Study
(
    StudyDescription
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Study_AccessionNumber ON dbo.Study
(
    AccessionNumber
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_Study_PatientBirthDate ON dbo.Study
(
    PatientBirthDate
)
INCLUDE
(
    StudyKey
)
WITH (DATA_COMPRESSION = PAGE)
