SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[IcsPars_GetAll]
    @cRefNo varchar(50) = '',
    @cRefType char(1) = ''
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        s.Id,
        s.UpdateCode,
        s.RefNo,
        s.RefDate,
        s.RefType,
        s.LocationId,
        s.LocationCode,
        s.Location,
        s.ReceivedById,
        s.ReceivedByTitle,
        s.ReceivedBy,
        s.ReceivedByTitle2,
        s.ReceivedByPosition,
        s.ReceivedDate,
        s.ReceivedDept,
        s.IssuedBy,
        s.IssuedByPosition,
        s.IssuedDate,
        s.IssuedDept,
        s.PostedBy,
        s.PostedDt,
        s.InsertedBy,
        s.InsertedDt,
        s.UpdatedBy,
        s.UpdatedDt,
        Status_ = CASE
            WHEN s.PostedDt IS NULL AND s.UpdateCode = 'T' THEN 'DRAFT TRANSFER'
            WHEN s.PostedDt IS NULL THEN 'DRAFT'
            WHEN ISNULL(itemState.TransferredItemCount, 0) > 0
                 AND ISNULL(itemState.CurrentItemCount, 0) > 0 THEN 'POSTED - PARTIALLY TRANSFERRED'
            WHEN ISNULL(itemState.ItemCount, 0) > 0
                 AND ISNULL(itemState.TransferredItemCount, 0) = ISNULL(itemState.ItemCount, 0) THEN 'POSTED - FULLY TRANSFERRED'
            WHEN ISNULL(itemState.DraftTransferItemCount, 0) > 0 THEN 'POSTED - DRAFT TRANSFER EXISTS'
            ELSE 'POSTED - CURRENT'
        END,
        ItemCount = ISNULL(itemState.ItemCount, 0),
        ItemCountActive = CASE WHEN s.PostedDt IS NOT NULL THEN ISNULL(itemState.CurrentItemCount, 0) ELSE 0 END,
        ActiveItems = CONVERT(varchar(20), CASE WHEN s.PostedDt IS NOT NULL THEN ISNULL(itemState.CurrentItemCount, 0) ELSE 0 END)
            + '/' + CONVERT(varchar(20), ISNULL(itemState.ItemCount, 0)),
        TransferableItemCount = CASE WHEN s.PostedDt IS NOT NULL THEN ISNULL(itemState.TransferableItemCount, 0) ELSE 0 END,
        DraftTransferItemCount = ISNULL(itemState.DraftTransferItemCount, 0),
        TransferredItemCount = ISNULL(itemState.TransferredItemCount, 0),
        HasDraftTransfer = CONVERT(bit, CASE WHEN ISNULL(itemState.DraftTransferItemCount, 0) > 0 THEN 1 ELSE 0 END),
        TransferStatus = CASE
            WHEN s.PostedDt IS NULL AND s.UpdateCode = 'T' THEN 'DRAFT TRANSFER'
            WHEN s.PostedDt IS NULL THEN 'DRAFT'
            WHEN ISNULL(itemState.TransferredItemCount, 0) > 0
                 AND ISNULL(itemState.CurrentItemCount, 0) > 0 THEN 'POSTED - PARTIALLY TRANSFERRED'
            WHEN ISNULL(itemState.ItemCount, 0) > 0
                 AND ISNULL(itemState.TransferredItemCount, 0) = ISNULL(itemState.ItemCount, 0) THEN 'POSTED - FULLY TRANSFERRED'
            WHEN ISNULL(itemState.DraftTransferItemCount, 0) > 0 THEN 'POSTED - DRAFT TRANSFER EXISTS'
            ELSE 'POSTED - CURRENT'
        END
    FROM dbo.IcsPars s
    OUTER APPLY
    (
        SELECT
            ItemCount = COUNT(*),
            CurrentItemCount = ISNULL(SUM(CASE WHEN itemLineage.HasPostedSuccessor = 0 THEN 1 ELSE 0 END), 0),
            TransferableItemCount = ISNULL(SUM(CASE WHEN itemLineage.HasAnySuccessor = 0 THEN 1 ELSE 0 END), 0),
            DraftTransferItemCount = ISNULL(SUM(CASE WHEN itemLineage.HasPostedSuccessor = 0 AND itemLineage.HasDraftSuccessor = 1 THEN 1 ELSE 0 END), 0),
            TransferredItemCount = ISNULL(SUM(itemLineage.HasPostedSuccessor), 0)
        FROM
        (
            SELECT
                item.Id,
                HasAnySuccessor = CASE WHEN EXISTS
                (
                    SELECT 1 FROM dbo.IcsParItems successor
                    WHERE successor.PrevItemId = item.Id
                ) THEN 1 ELSE 0 END,
                HasPostedSuccessor = CASE WHEN EXISTS
                (
                    SELECT 1 FROM dbo.IcsParItems successor
                    INNER JOIN dbo.IcsPars successorHeader ON successorHeader.Id = successor.IcsParId
                    WHERE successor.PrevItemId = item.Id
                      AND successorHeader.PostedDt IS NOT NULL
                ) THEN 1 ELSE 0 END,
                HasDraftSuccessor = CASE WHEN EXISTS
                (
                    SELECT 1 FROM dbo.IcsParItems successor
                    INNER JOIN dbo.IcsPars successorHeader ON successorHeader.Id = successor.IcsParId
                    WHERE successor.PrevItemId = item.Id
                      AND successorHeader.PostedDt IS NULL
                ) THEN 1 ELSE 0 END
            FROM dbo.IcsParItems item
            INNER JOIN dbo.PsCardItemExtns extn ON extn.Id = item.PsCardItemExtnId
            WHERE item.IcsParId = s.Id
              AND extn.PsCardSubItemId IS NULL
        ) itemLineage
    ) itemState
    WHERE (ISNULL(@cRefType, '') = '' OR s.RefType = @cRefType)
      AND (ISNULL(@cRefNo, '') = '' OR s.RefNo = @cRefNo);
END
GO

ALTER PROCEDURE [dbo].[IcsPars_GetItems]
    @cRefNo varchar(50),
    @cRefType char(1)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        item.Id,
        IcsParId = header.Id,
        item.PsCardItemExtnId,
        item.PrevItemId,
        cardItem.PoNo,
        extn.SetLotNo,
        extn.SetLotQtyNo,
        extn.ContentNo,
        TContentNo = CONVERT(int, cardItem.Qty),
        cardItem.Description,
        RefNo = serial.SerialNo,
        TUnitCost = extn.AcqCost,
        PrevIcsParNo = CASE WHEN previousHeader.Id IS NULL THEN NULL ELSE previousHeader.RefType + '-' + LTRIM(RTRIM(previousHeader.RefNo)) END,
        CanByIcsParNo = CASE WHEN successor.RefNo IS NULL THEN NULL ELSE successor.RefType + '-' + LTRIM(RTRIM(successor.RefNo)) END,
        extn.PropNo,
        item.IssuedTo,
        item.Designation,
        SerialNo = serial.SerialNo,
        cardItem.Unit,
        AcquisitionCost = extn.AcqCost,
        CurrentRefNo = header.RefNo,
        PreviousRefNo = previousHeader.RefNo,
        SuccessorRefNo = successor.RefNo,
        CurrentAccountableOfficer = currentOwner.AccountableOfficer,
        AccountableOfficerPosition = currentOwner.AccountableOfficerPosition,
        AccountableOfficerDepartment = currentOwner.AccountableOfficerDepartment,
        ComponentCount = (SELECT COUNT(*) FROM dbo.IcsParItemComponents component WHERE component.IcsParItemId = item.Id),
        HasDraftTransfer = CONVERT(bit, CASE WHEN EXISTS
        (
            SELECT 1 FROM dbo.IcsParItems nextItem
            INNER JOIN dbo.IcsPars nextHeader ON nextHeader.Id = nextItem.IcsParId
            WHERE nextItem.PrevItemId = item.Id AND nextHeader.PostedDt IS NULL
        ) THEN 1 ELSE 0 END),
        HasPostedSuccessor = CONVERT(bit, CASE WHEN EXISTS
        (
            SELECT 1 FROM dbo.IcsParItems nextItem
            INNER JOIN dbo.IcsPars nextHeader ON nextHeader.Id = nextItem.IcsParId
            WHERE nextItem.PrevItemId = item.Id AND nextHeader.PostedDt IS NOT NULL
        ) THEN 1 ELSE 0 END),
        CanTransfer = CONVERT(bit, CASE WHEN header.PostedDt IS NOT NULL AND NOT EXISTS
        (
            SELECT 1 FROM dbo.IcsParItems nextItem
            WHERE nextItem.PrevItemId = item.Id
        ) THEN 1 ELSE 0 END),
        TransferStatus = CASE
            WHEN header.PostedDt IS NULL AND item.PrevItemId IS NOT NULL THEN 'DRAFT TRANSFER'
            WHEN header.PostedDt IS NULL THEN 'DRAFT'
            WHEN EXISTS
            (
                SELECT 1 FROM dbo.IcsParItems nextItem
                INNER JOIN dbo.IcsPars nextHeader ON nextHeader.Id = nextItem.IcsParId
                WHERE nextItem.PrevItemId = item.Id AND nextHeader.PostedDt IS NOT NULL
            ) THEN 'TRANSFERRED'
            ELSE 'CURRENT'
        END
    FROM dbo.IcsPars header
    INNER JOIN dbo.IcsParItems item ON item.IcsParId = header.Id
    INNER JOIN dbo.PsCardItemExtns extn ON extn.Id = item.PsCardItemExtnId
    INNER JOIN dbo.PsCardItems cardItem ON cardItem.Id = extn.PsCardItemId
    LEFT JOIN dbo.IcsParItems previousItem ON previousItem.Id = item.PrevItemId
    LEFT JOIN dbo.IcsPars previousHeader ON previousHeader.Id = previousItem.IcsParId
    OUTER APPLY
    (
        SELECT SerialNo = COALESCE(NULLIF(otherExtn.SerialNo, ''), NULLIF(vehicleExtn.PlateNo, ''), NULLIF(vehicleExtn.ConductionNo, ''), NULLIF(extn.SeriesNo, ''))
        FROM (SELECT 1 AS Anchor) anchor
        LEFT JOIN dbo.PsCardItemExtnOthers otherExtn ON otherExtn.Id = extn.Id
        LEFT JOIN dbo.PsCardItemExtnVehicles vehicleExtn ON vehicleExtn.Id = extn.Id
    ) serial
    OUTER APPLY
    (
        SELECT TOP (1)
            successorHeader.RefNo,
            successorHeader.RefType
        FROM dbo.IcsParItems successorItem
        INNER JOIN dbo.IcsPars successorHeader ON successorHeader.Id = successorItem.IcsParId
        WHERE successorItem.PrevItemId = item.Id
        ORDER BY CASE WHEN successorHeader.PostedDt IS NOT NULL THEN 0 ELSE 1 END,
                 successorHeader.PostedDt,
                 successorHeader.RefDate,
                 successorHeader.InsertedDt,
                 successorItem.Id
    ) successor
    OUTER APPLY
    (
        SELECT TOP (1)
            AccountableOfficer = COALESCE(NULLIF(currentItem.IssuedTo, ''), currentHeader.ReceivedBy),
            AccountableOfficerPosition = COALESCE(NULLIF(currentItem.Designation, ''), currentHeader.ReceivedByPosition),
            AccountableOfficerDepartment = currentHeader.ReceivedDept
        FROM dbo.IcsParItems currentItem
        INNER JOIN dbo.IcsPars currentHeader ON currentHeader.Id = currentItem.IcsParId
        WHERE currentItem.PsCardItemExtnId = item.PsCardItemExtnId
          AND currentHeader.PostedDt IS NOT NULL
          AND NOT EXISTS
          (
              SELECT 1 FROM dbo.IcsParItems postedNext
              INNER JOIN dbo.IcsPars postedNextHeader ON postedNextHeader.Id = postedNext.IcsParId
              WHERE postedNext.PrevItemId = currentItem.Id
                AND postedNextHeader.PostedDt IS NOT NULL
          )
        ORDER BY currentHeader.PostedDt DESC, currentHeader.RefDate DESC, currentItem.Id
    ) currentOwner
    WHERE header.RefNo = @cRefNo
      AND header.RefType = @cRefType
      AND extn.PsCardSubItemId IS NULL;
END
GO

ALTER PROCEDURE [dbo].[IcsPars_GetItemsForTransfer]
    @cRefNo varchar(50),
    @cRefType char(1)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        item.Id,
        item.PsCardItemExtnId,
        item.PrevItemId,
        cardItem.PoNo,
        extn.SetLotNo,
        extn.SetLotQtyNo,
        extn.ContentNo,
        TContentNo = CONVERT(int, cardItem.Qty),
        cardItem.Description,
        RefNo = serial.SerialNo,
        TUnitCost = extn.AcqCost,
        extn.PropNo,
        SerialNo = serial.SerialNo,
        cardItem.Unit,
        AcquisitionCost = extn.AcqCost,
        CurrentRefNo = header.RefNo,
        CurrentAccountableOfficer = COALESCE(NULLIF(item.IssuedTo, ''), header.ReceivedBy),
        AccountableOfficerPosition = COALESCE(NULLIF(item.Designation, ''), header.ReceivedByPosition),
        AccountableOfficerDepartment = header.ReceivedDept,
        ComponentCount = (SELECT COUNT(*) FROM dbo.IcsParItemComponents component WHERE component.IcsParItemId = item.Id),
        HasDraftTransfer = CONVERT(bit, 0),
        HasPostedSuccessor = CONVERT(bit, 0),
        CanTransfer = CONVERT(bit, 1),
        TransferStatus = CONVERT(varchar(30), 'CURRENT'),
        SuccessorRefNo = CONVERT(varchar(50), NULL)
    FROM dbo.IcsPars header
    INNER JOIN dbo.IcsParItems item ON item.IcsParId = header.Id
    INNER JOIN dbo.PsCardItemExtns extn ON extn.Id = item.PsCardItemExtnId
    INNER JOIN dbo.PsCardItems cardItem ON cardItem.Id = extn.PsCardItemId
    OUTER APPLY
    (
        SELECT SerialNo = COALESCE(NULLIF(otherExtn.SerialNo, ''), NULLIF(vehicleExtn.PlateNo, ''), NULLIF(vehicleExtn.ConductionNo, ''), NULLIF(extn.SeriesNo, ''))
        FROM (SELECT 1 AS Anchor) anchor
        LEFT JOIN dbo.PsCardItemExtnOthers otherExtn ON otherExtn.Id = extn.Id
        LEFT JOIN dbo.PsCardItemExtnVehicles vehicleExtn ON vehicleExtn.Id = extn.Id
    ) serial
    WHERE header.RefNo = @cRefNo
      AND header.RefType = @cRefType
      AND header.PostedDt IS NOT NULL
      AND extn.PsCardSubItemId IS NULL
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.IcsParItems successor
          WHERE successor.PrevItemId = item.Id
      );
END
GO
