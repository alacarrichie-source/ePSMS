SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- ============================================================================
-- 1. AIR WIZARD PROGRESS TABLE (Matches PurchaseOrderWizardProgress pattern)
-- ============================================================================
IF OBJECT_ID('dbo.AIRWizardProgress', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIRWizardProgress
    (
        Id uniqueidentifier NOT NULL PRIMARY KEY,
        DraftNo varchar(12) NOT NULL,
        OrderId uniqueidentifier NULL,
        CreatedBy nvarchar(128) NOT NULL,
        CreatedAt datetime NOT NULL,
        UpdatedAt datetime NULL,
        LastStep int NOT NULL DEFAULT 1,
        WizardStateJson nvarchar(max) NULL,
        IsCompleted bit NOT NULL DEFAULT 0
    );

    CREATE UNIQUE INDEX UX_AIRWizardProgress_DraftNo
        ON dbo.AIRWizardProgress (DraftNo);

    CREATE INDEX IX_AIRWizardProgress_CreatedBy_IsCompleted
        ON dbo.AIRWizardProgress (CreatedBy, IsCompleted);
END;

-- ============================================================================
-- 2. EXTEND dbo.AIRs TABLE WITH WORKFLOW & METADATA COLUMNS
-- ============================================================================
IF COL_LENGTH('dbo.AIRs', 'InspectionStatus') IS NULL
    ALTER TABLE dbo.AIRs ADD InspectionStatus varchar(50) NULL;

IF COL_LENGTH('dbo.AIRs', 'AcceptanceStatus') IS NULL
    ALTER TABLE dbo.AIRs ADD AcceptanceStatus varchar(50) NULL;

IF COL_LENGTH('dbo.AIRs', 'OverallStatus') IS NULL
    ALTER TABLE dbo.AIRs ADD OverallStatus varchar(50) NULL;

IF COL_LENGTH('dbo.AIRs', 'DrNo') IS NULL
    ALTER TABLE dbo.AIRs ADD DrNo varchar(100) NULL;

IF COL_LENGTH('dbo.AIRs', 'InvoiceAmount') IS NULL
    ALTER TABLE dbo.AIRs ADD InvoiceAmount decimal(18,2) NULL;

IF COL_LENGTH('dbo.AIRs', 'InvoiceType') IS NULL
    ALTER TABLE dbo.AIRs ADD InvoiceType varchar(50) NULL;

IF COL_LENGTH('dbo.AIRs', 'BillingReference') IS NULL
    ALTER TABLE dbo.AIRs ADD BillingReference varchar(100) NULL;

IF COL_LENGTH('dbo.AIRs', 'InspectionLocation') IS NULL
    ALTER TABLE dbo.AIRs ADD InspectionLocation varchar(200) NULL;

IF COL_LENGTH('dbo.AIRs', 'InspectorName') IS NULL
    ALTER TABLE dbo.AIRs ADD InspectorName varchar(150) NULL;

IF COL_LENGTH('dbo.AIRs', 'InspectorDesignation') IS NULL
    ALTER TABLE dbo.AIRs ADD InspectorDesignation varchar(150) NULL;

IF COL_LENGTH('dbo.AIRs', 'InspectionCommittee') IS NULL
    ALTER TABLE dbo.AIRs ADD InspectionCommittee varchar(250) NULL;

IF COL_LENGTH('dbo.AIRs', 'AcceptanceStartedBy') IS NULL
    ALTER TABLE dbo.AIRs ADD AcceptanceStartedBy nvarchar(128) NULL;

IF COL_LENGTH('dbo.AIRs', 'AcceptanceStartedDt') IS NULL
    ALTER TABLE dbo.AIRs ADD AcceptanceStartedDt datetime NULL;

IF COL_LENGTH('dbo.AIRs', 'AcceptedBy') IS NULL
    ALTER TABLE dbo.AIRs ADD AcceptedBy varchar(150) NULL;

IF COL_LENGTH('dbo.AIRs', 'AcceptedByDesignation') IS NULL
    ALTER TABLE dbo.AIRs ADD AcceptedByDesignation varchar(150) NULL;

IF COL_LENGTH('dbo.AIRs', 'AcceptanceRemarks') IS NULL
    ALTER TABLE dbo.AIRs ADD AcceptanceRemarks varchar(max) NULL;

IF COL_LENGTH('dbo.AIRs', 'WithdrawalRequested') IS NULL
    ALTER TABLE dbo.AIRs ADD WithdrawalRequested bit NOT NULL DEFAULT 0;

IF COL_LENGTH('dbo.AIRs', 'WithdrawalRequestedBy') IS NULL
    ALTER TABLE dbo.AIRs ADD WithdrawalRequestedBy nvarchar(128) NULL;

IF COL_LENGTH('dbo.AIRs', 'WithdrawalRequestedDt') IS NULL
    ALTER TABLE dbo.AIRs ADD WithdrawalRequestedDt datetime NULL;

IF COL_LENGTH('dbo.AIRs', 'WithdrawalReason') IS NULL
    ALTER TABLE dbo.AIRs ADD WithdrawalReason varchar(max) NULL;

IF COL_LENGTH('dbo.AIRs', 'RevisionComments') IS NULL
    ALTER TABLE dbo.AIRs ADD RevisionComments varchar(max) NULL;

IF COL_LENGTH('dbo.AIRs', 'ReturnedBy') IS NULL
    ALTER TABLE dbo.AIRs ADD ReturnedBy nvarchar(128) NULL;

IF COL_LENGTH('dbo.AIRs', 'ReturnedDt') IS NULL
    ALTER TABLE dbo.AIRs ADD ReturnedDt datetime NULL;

-- ============================================================================
-- 3. EXTEND dbo.AIRItems TABLE WITH QUANTITIES & DISPOSITION
-- ============================================================================
IF COL_LENGTH('dbo.AIRItems', 'InspectedQty') IS NULL
    ALTER TABLE dbo.AIRItems ADD InspectedQty decimal(18,2) NULL;

IF COL_LENGTH('dbo.AIRItems', 'AcceptedQty') IS NULL
    ALTER TABLE dbo.AIRItems ADD AcceptedQty decimal(18,2) NULL;

IF COL_LENGTH('dbo.AIRItems', 'Disposition') IS NULL
    ALTER TABLE dbo.AIRItems ADD Disposition varchar(50) NULL;

IF COL_LENGTH('dbo.AIRItems', 'DestinationDepartment') IS NULL
    ALTER TABLE dbo.AIRItems ADD DestinationDepartment varchar(150) NULL;

IF COL_LENGTH('dbo.AIRItems', 'DestinationCustodian') IS NULL
    ALTER TABLE dbo.AIRItems ADD DestinationCustodian varchar(150) NULL;

-- ============================================================================
-- 4. CREATE dbo.AIRSubItems TABLE FOR SET / LOT COMPONENT INSPECTIONS
-- ============================================================================
IF OBJECT_ID('dbo.AIRSubItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIRSubItems
    (
        Id uniqueidentifier NOT NULL PRIMARY KEY,
        AirItemId uniqueidentifier NOT NULL,
        OrderSubItemId uniqueidentifier NOT NULL,
        OrderSubItemRequestId uniqueidentifier NULL,
        SubItemNo varchar(20) NULL,
        Description varchar(500) NULL,
        Unit varchar(50) NULL,
        ExpectedQty decimal(18,2) NOT NULL DEFAULT 0,
        InspectedQty decimal(18,2) NOT NULL DEFAULT 0,
        AcceptedQty decimal(18,2) NOT NULL DEFAULT 0,
        Remarks varchar(500) NULL,
        InsertedBy nvarchar(50) NULL,
        InsertedDt datetime NULL,
        UpdatedBy nvarchar(50) NULL,
        UpdatedDt datetime NULL,
        CONSTRAINT FK_AIRSubItems_AIRItems FOREIGN KEY (AirItemId) REFERENCES dbo.AIRItems (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_AIRSubItems_AirItemId ON dbo.AIRSubItems (AirItemId);
    CREATE INDEX IX_AIRSubItems_OrderSubItemId ON dbo.AIRSubItems (OrderSubItemId);
END;

-- ============================================================================
-- 5. CREATE dbo.AIRDocuments TABLE FOR INVOICE COPIES & INSPECTION ATTACHMENTS
-- ============================================================================
IF OBJECT_ID('dbo.AIRDocuments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIRDocuments
    (
        Id uniqueidentifier NOT NULL PRIMARY KEY,
        AirId uniqueidentifier NULL,
        DocumentType varchar(50) NOT NULL,
        FileName nvarchar(255) NOT NULL,
        FilePath nvarchar(500) NOT NULL,
        FileSize varchar(50) NULL,
        UploadedBy nvarchar(128) NULL,
        UploadedDt datetime NOT NULL,
        CONSTRAINT FK_AIRDocuments_AIRs FOREIGN KEY (AirId) REFERENCES dbo.AIRs (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_AIRDocuments_AirId ON dbo.AIRDocuments (AirId);
END;

COMMIT TRANSACTION;
