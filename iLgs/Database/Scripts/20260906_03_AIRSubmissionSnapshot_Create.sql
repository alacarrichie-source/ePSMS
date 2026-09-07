SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- ============================================================================
-- 20260906_03_AIRSubmissionSnapshot_Create.sql
-- Creates dbo.AIRSubmissionSnapshots for immutable historical presentation snapshots
-- of submitted AIR Wizard payloads.
-- ============================================================================

IF OBJECT_ID('dbo.AIRSubmissionSnapshots', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIRSubmissionSnapshots (
        Id uniqueidentifier NOT NULL CONSTRAINT DF_AIRSubmissionSnapshots_Id DEFAULT NEWID(),
        AIRId uniqueidentifier NOT NULL,
        WizardProgressId uniqueidentifier NULL,
        VersionNo int NOT NULL CONSTRAINT DF_AIRSubmissionSnapshots_VersionNo DEFAULT 1,
        PayloadJson nvarchar(max) NOT NULL,
        SubmittedBy nvarchar(128) NULL,
        SubmittedDt datetime NOT NULL CONSTRAINT DF_AIRSubmissionSnapshots_SubmittedDt DEFAULT GETDATE(),
        CreatedDt datetime NOT NULL CONSTRAINT DF_AIRSubmissionSnapshots_CreatedDt DEFAULT GETDATE(),
        CONSTRAINT PK_AIRSubmissionSnapshots PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_AIRSubmissionSnapshots_AIRs FOREIGN KEY (AIRId) REFERENCES dbo.AIRs(Id) ON DELETE CASCADE,
        CONSTRAINT FK_AIRSubmissionSnapshots_AIRWizardProgress FOREIGN KEY (WizardProgressId) REFERENCES dbo.AIRWizardProgress(Id)
    );

    CREATE NONCLUSTERED INDEX IX_AIRSubmissionSnapshots_AIRId 
        ON dbo.AIRSubmissionSnapshots (AIRId);

    CREATE NONCLUSTERED INDEX IX_AIRSubmissionSnapshots_AIRId_VersionNo 
        ON dbo.AIRSubmissionSnapshots (AIRId, VersionNo);

    CREATE NONCLUSTERED INDEX IX_AIRSubmissionSnapshots_WizardProgressId 
        ON dbo.AIRSubmissionSnapshots (WizardProgressId);

    PRINT 'dbo.AIRSubmissionSnapshots created successfully.';
END
ELSE
BEGIN
    PRINT 'dbo.AIRSubmissionSnapshots already exists.';
END

COMMIT TRANSACTION;