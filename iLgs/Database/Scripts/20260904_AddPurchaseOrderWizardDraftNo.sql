SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.PurchaseOrderWizardProgress', 'DraftNo') IS NULL
BEGIN
    ALTER TABLE dbo.PurchaseOrderWizardProgress
        ADD DraftNo varchar(12) NULL;
END;

;WITH DraftSequence AS
(
    SELECT
        d.Id,
        Prefix = CONVERT(char(7), d.CreatedAt, 120) + '-',
        SequenceNo =
            ISNULL
            (
                (
                    SELECT MAX(TRY_CONVERT(int, RIGHT(o.CtrlNo, 4)))
                    FROM dbo.Orders AS o
                    WHERE o.CtrlNo LIKE CONVERT(char(7), d.CreatedAt, 120) + '-[0-9][0-9][0-9][0-9]'
                ),
                0
            ) +
            ROW_NUMBER() OVER
            (
                PARTITION BY YEAR(d.CreatedAt), MONTH(d.CreatedAt)
                ORDER BY d.CreatedAt, d.Id
            )
    FROM dbo.PurchaseOrderWizardProgress AS d
    WHERE d.DraftNo IS NULL
)
UPDATE d
SET DraftNo = s.Prefix + RIGHT('0000' + CONVERT(varchar(4), s.SequenceNo), 4)
FROM dbo.PurchaseOrderWizardProgress AS d
INNER JOIN DraftSequence AS s ON s.Id = d.Id;

IF EXISTS
(
    SELECT 1
    FROM dbo.PurchaseOrderWizardProgress
    WHERE DraftNo IS NULL OR LEN(DraftNo) <> 12
)
    THROW 50001, 'Unable to assign a valid Purchase Order wizard draft number.', 1;

ALTER TABLE dbo.PurchaseOrderWizardProgress
    ALTER COLUMN DraftNo varchar(12) NOT NULL;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.PurchaseOrderWizardProgress')
      AND name = 'UX_PurchaseOrderWizardProgress_DraftNo'
)
BEGIN
    CREATE UNIQUE INDEX UX_PurchaseOrderWizardProgress_DraftNo
        ON dbo.PurchaseOrderWizardProgress (DraftNo);
END;

COMMIT TRANSACTION;