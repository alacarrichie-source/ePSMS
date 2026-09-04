SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.PurchaseOrderWizardProgress', 'DraftNo') IS NULL
    THROW 50002, 'Run 20260904_AddPurchaseOrderWizardDraftNo.sql first.', 1;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.PurchaseOrderWizardProgress') AND name = 'UX_PurchaseOrderWizardProgress_DraftNo')
BEGIN
    DROP INDEX UX_PurchaseOrderWizardProgress_DraftNo ON dbo.PurchaseOrderWizardProgress;
END;

UPDATE dbo.PurchaseOrderWizardProgress
SET DraftNo = REPLACE(DraftNo, '-', '')
WHERE DraftNo LIKE '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9][0-9][0-9]';

IF EXISTS (SELECT 1 FROM dbo.PurchaseOrderWizardProgress WHERE DraftNo IS NULL OR LEN(DraftNo) <> 10)
    THROW 50003, 'One or more wizard draft numbers cannot be converted to yyyyMM####.', 1;

ALTER TABLE dbo.PurchaseOrderWizardProgress ALTER COLUMN DraftNo varchar(10) NOT NULL;

CREATE UNIQUE INDEX UX_PurchaseOrderWizardProgress_DraftNo
    ON dbo.PurchaseOrderWizardProgress (DraftNo);

COMMIT TRANSACTION;