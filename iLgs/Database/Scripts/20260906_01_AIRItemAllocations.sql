-- =============================================================================
-- 20260906_01_AIRItemAllocations.sql
-- Creates the AIRItemAllocations table for per-PR allocation inspection
-- traceability in the AIR Inspection Wizard.
--
-- Run BEFORE deploying updated application code.
-- =============================================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'AIRItemAllocations'
)
BEGIN
    CREATE TABLE [dbo].[AIRItemAllocations] (
        [Id]                 UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT [DF_AIRItemAllocations_Id] DEFAULT NEWID(),
        [AIRItemId]          UNIQUEIDENTIFIER NOT NULL,
        [OrderItemRequestId] UNIQUEIDENTIFIER NOT NULL,
        [QtyAllocated]       DECIMAL(18,4)    NOT NULL
            CONSTRAINT [DF_AIRItemAllocations_QtyAllocated] DEFAULT 0,
        [QtyInspected]       DECIMAL(18,4)    NOT NULL
            CONSTRAINT [DF_AIRItemAllocations_QtyInspected] DEFAULT 0,
        [PRNumber]           NVARCHAR(50)     NULL,
        [Department]         NVARCHAR(200)    NULL,
        [Remarks]            NVARCHAR(500)    NULL,
        [InsertedBy]         NVARCHAR(100)    NULL,
        [InsertedDt]         DATETIME         NULL,
        [UpdatedBy]          NVARCHAR(100)    NULL,
        [UpdatedDt]          DATETIME         NULL,
        CONSTRAINT [PK_AIRItemAllocations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AIRItemAllocations_AIRItems]
            FOREIGN KEY ([AIRItemId])
            REFERENCES [dbo].[AIRItems] ([Id])
            ON DELETE CASCADE,
        CONSTRAINT [FK_AIRItemAllocations_OrderItemRequests]
            FOREIGN KEY ([OrderItemRequestId])
            REFERENCES [dbo].[OrderItemRequests] ([Id])
    );

    CREATE UNIQUE INDEX [UIX_AIRItemAllocations_AirItem_OIR]
        ON [dbo].[AIRItemAllocations] ([AIRItemId], [OrderItemRequestId]);

    PRINT 'AIRItemAllocations table created successfully.';
END
ELSE
BEGIN
    PRINT 'AIRItemAllocations table already exists. No changes made.';
END
