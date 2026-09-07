IF COL_LENGTH('dbo.PsCardItems', 'AIRItemId') IS NULL
BEGIN
    ALTER TABLE dbo.PsCardItems ADD AIRItemId UNIQUEIDENTIFIER NULL;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID('dbo.PsCardItems')
      AND name = 'FK_PsCardItems_AIRItems'
)
BEGIN
    EXEC(N'ALTER TABLE dbo.PsCardItems WITH CHECK
           ADD CONSTRAINT FK_PsCardItems_AIRItems
           FOREIGN KEY (AIRItemId) REFERENCES dbo.AIRItems(Id);');
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.PsCardItems')
      AND name = 'UIX_PsCardItems_AIRItem_OIR'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX UIX_PsCardItems_AIRItem_OIR
           ON dbo.PsCardItems(AIRItemId, OrderItemRequestId)
           WHERE AIRItemId IS NOT NULL;');
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.PsCardItemExtns')
      AND name = 'UIX_PsCardItemExtns_AIRItemExtnId'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX UIX_PsCardItemExtns_AIRItemExtnId
           ON dbo.PsCardItemExtns(AIRItemExtnId)
           WHERE AIRItemExtnId IS NOT NULL;');
END;
