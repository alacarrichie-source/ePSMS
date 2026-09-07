SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- ============================================================================
-- 20260906_02_AIRWizardProgress_Revise.sql
-- Extends AIRWizardProgress for persistent resumable drafts, revision tracking,
-- and soft-discard. Adds withdrawal audit columns to AIRs.
-- ============================================================================

-- 1. Add new columns to AIRWizardProgress
-- ============================================================================

IF COL_LENGTH('dbo.AIRWizardProgress', 'UserId') IS NULL
    ALTER TABLE dbo.AIRWizardProgress ADD UserId nvarchar(128) NULL;

IF COL_LENGTH('dbo.AIRWizardProgress', 'Status') IS NULL
    ALTER TABLE dbo.AIRWizardProgress ADD [Status] varchar(30) NOT NULL
        CONSTRAINT DF_AIRWizardProgress_Status DEFAULT 'Draft';

IF COL_LENGTH('dbo.AIRWizardProgress', 'CurrentStep') IS NULL
    ALTER TABLE dbo.AIRWizardProgress ADD CurrentStep int NOT NULL
        CONSTRAINT DF_AIRWizardProgress_CurrentStep DEFAULT 1;

IF COL_LENGTH('dbo.AIRWizardProgress', 'AIRId') IS NULL
    ALTER TABLE dbo.AIRWizardProgress ADD AIRId uniqueidentifier NULL;

IF COL_LENGTH('dbo.AIRWizardProgress', 'SourceAIRId') IS NULL
    ALTER TABLE dbo.AIRWizardProgress ADD SourceAIRId uniqueidentifier NULL;

IF COL_LENGTH('dbo.AIRWizardProgress', 'RevisionNo') IS NULL
    ALTER TABLE dbo.AIRWizardProgress ADD RevisionNo int NOT NULL
        CONSTRAINT DF_AIRWizardProgress_RevisionNo DEFAULT 0;

IF COL_LENGTH('dbo.AIRWizardProgress', 'SubmittedDt') IS NULL
    ALTER TABLE dbo.AIRWizardProgress ADD SubmittedDt datetime NULL;

IF COL_LENGTH('dbo.AIRWizardProgress', 'DiscardedDt') IS NULL
    ALTER TABLE dbo.AIRWizardProgress ADD DiscardedDt datetime NULL;

IF COL_LENGTH('dbo.AIRWizardProgress', 'DiscardedBy') IS NULL
    ALTER TABLE dbo.AIRWizardProgress ADD DiscardedBy nvarchar(128) NULL;

IF COL_LENGTH('dbo.AIRWizardProgress', 'DiscardedReason') IS NULL
    ALTER TABLE dbo.AIRWizardProgress ADD DiscardedReason nvarchar(500) NULL;

-- Backfill UserId from CreatedBy for existing rows via dynamic SQL to avoid compile-time column resolution
EXEC('UPDATE dbo.AIRWizardProgress SET UserId = CreatedBy WHERE UserId IS NULL');

-- Index for draft lookups
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AIRWizardProgress_UserId_Status' AND object_id = OBJECT_ID('dbo.AIRWizardProgress'))
    CREATE INDEX IX_AIRWizardProgress_UserId_Status
        ON dbo.AIRWizardProgress (UserId, [Status]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AIRWizardProgress_AIRId' AND object_id = OBJECT_ID('dbo.AIRWizardProgress'))
    CREATE INDEX IX_AIRWizardProgress_AIRId
        ON dbo.AIRWizardProgress (AIRId);

-- 2. Add withdrawal audit columns to AIRs
-- ============================================================================

IF COL_LENGTH('dbo.AIRs', 'WithdrawnBy') IS NULL
    ALTER TABLE dbo.AIRs ADD WithdrawnBy nvarchar(128) NULL;

IF COL_LENGTH('dbo.AIRs', 'WithdrawnDt') IS NULL
    ALTER TABLE dbo.AIRs ADD WithdrawnDt datetime NULL;

IF COL_LENGTH('dbo.AIRs', 'Disposition') IS NULL
    ALTER TABLE dbo.AIRs ADD Disposition varchar(50) NULL;

IF COL_LENGTH('dbo.AIRs', 'InvDist') IS NULL
    ALTER TABLE dbo.AIRs ADD InvDist varchar(10) NULL;

-- 3. Add sub-item extension columns if missing
-- ============================================================================

IF COL_LENGTH('dbo.AIRSubItems', 'QtyPerParent') IS NULL
    ALTER TABLE dbo.AIRSubItems ADD QtyPerParent decimal(18,2) NOT NULL
        CONSTRAINT DF_AIRSubItems_QtyPerParent DEFAULT 1;

IF COL_LENGTH('dbo.AIRSubItems', 'CategoryCode') IS NULL
    ALTER TABLE dbo.AIRSubItems ADD CategoryCode varchar(20) NULL;

IF COL_LENGTH('dbo.AIRSubItems', 'ItemExtnName') IS NULL
    ALTER TABLE dbo.AIRSubItems ADD ItemExtnName varchar(50) NULL;

-- 4. Add AIRItemExtn sub-item reference column if missing
-- ============================================================================

IF COL_LENGTH('dbo.AIRItemExtns', 'AIRSubItemId') IS NULL
    ALTER TABLE dbo.AIRItemExtns ADD AIRSubItemId uniqueidentifier NULL;

IF COL_LENGTH('dbo.AIRItemExtns', 'SetLotNo') IS NULL
    ALTER TABLE dbo.AIRItemExtns ADD SetLotNo varchar(50) NULL;

IF COL_LENGTH('dbo.AIRItemExtns', 'SetLotQtyNo') IS NULL
    ALTER TABLE dbo.AIRItemExtns ADD SetLotQtyNo int NULL;

COMMIT TRANSACTION;
