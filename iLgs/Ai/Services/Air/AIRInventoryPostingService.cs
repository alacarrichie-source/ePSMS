using iLgs.Models;
using iLgs.Services.Items;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace iLgs.Ai.Services.Air
{
    public interface IAIRInventoryPostingService
    {
        Task<AIRInventoryPostingResult> PostCompletedAirAsync(Guid airId, string user);
    }

    public class AIRInventoryPostingResult
    {
        public int ReceiptCount { get; set; }
        public int AssetCount { get; set; }
        public bool AlreadyPosted { get; set; }
    }

    public class AIRInventoryPostingService : IAIRInventoryPostingService
    {
        private readonly AppManEntities _db;
        private readonly IDocumentHistoryService _historyService;
        private readonly IItemCodeService _itemCodeService;

        public AIRInventoryPostingService(
            AppManEntities context,
            IDocumentHistoryService historyService)
        {
            _db = context;
            _historyService = historyService;
            _itemCodeService = new ItemCodeService(_db);
        }

        public async Task<AIRInventoryPostingResult> PostCompletedAirAsync(
            Guid airId,
            string user)
        {
            using (var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    var air = await _db.AIRs
                        .Include(a => a.Order)
                        .Include(a => a.AIRItems.Select(i => i.OrderItemRequest.OrderItem))
                        .Include(a => a.AIRItems.Select(i => i.OrderItemRequest.OrderItem.AllField))
                        .Include(a => a.AIRItems.Select(i => i.OrderItemRequest.RequestItem.Request))
                        .Include(a => a.AIRItems.Select(i => i.AIRItemAllocations.Select(x => x.OrderItemRequest.OrderItem)))
                        .Include(a => a.AIRItems.Select(i => i.AIRItemAllocations.Select(x => x.OrderItemRequest.OrderItem.AllField)))
                        .Include(a => a.AIRItems.Select(i => i.AIRItemAllocations.Select(x => x.OrderItemRequest.RequestItem.Request)))
                        .Include(a => a.AIRItems.Select(i => i.AIRItemExtns))
                        .Include(a => a.AIRItems.Select(i => i.AIRSubItems))
                        .FirstOrDefaultAsync(a => a.Id == airId);

                    ValidateAirForPosting(air);

                    await ValidateDatabaseSupportAsync();

                    bool alreadyPosted = await HasInventoryReceiptsAsync(
                        air.AIRItems.Select(i => i.Id).ToList());

                    if (alreadyPosted)
                    {
                        transaction.Commit();
                        return new AIRInventoryPostingResult
                        {
                            AlreadyPosted = true
                        };
                    }

                    if (air.AIRItems.Any(i => i.AIRSubItems.Any(s => s.AcceptedQty > 0)))
                    {
                        throw new InvalidOperationException(
                            "Set/lot sub-item posting is not enabled yet; no records were created.");
                    }

                    var groups = await _db.Database.SqlQuery<OrderItemGroupVM>(
                        "Exec OrderService_GetOrderItemGroup {0}, {1}",
                        air.OrderId,
                        0)
                        .ToListAsync();

                    int receiptCount = 0;
                    int assetCount = 0;
                    DateTime postingDate = DateTime.Now;

                    foreach (var airItem in air.AIRItems.Where(i => (i.AcceptedQty ?? 0) > 0))
                    {
                        var trackedItems = airItem.AIRItemExtns
                            .OrderBy(e => e.ContentNo)
                            .ToList();
                        int trackedItemIndex = 0;

                        foreach (var portion in BuildReceiptPortions(airItem))
                        {
                            var orderItem = portion.OrderItemRequest.OrderItem;
                            var sourceRequest = GetSourceRequest(portion.OrderItemRequest);
                            var group = groups.FirstOrDefault(g =>
                                g.ItemCodeId == orderItem.ItemCodeId &&
                                g.StockNo == orderItem.PsNo &&
                                g.Fund == sourceRequest.Fund);

                            if (group == null)
                            {
                                group = groups.FirstOrDefault(g =>
                                    g.ItemCodeId == orderItem.ItemCodeId &&
                                    g.StockNo == orderItem.PsNo);
                            }

                            if (group == null)
                            {
                                throw new InvalidOperationException(
                                    "Card classification not found for OrderItemRequest " +
                                    portion.OrderItemRequest.Id + ".");
                            }

                            decimal previouslyPosted = await GetPreviouslyPostedQuantityAsync(
                                portion.OrderItemRequest.Id);

                            if (previouslyPosted + portion.Quantity >
                                (portion.OrderItemRequest.QtyApplied ?? 0))
                            {
                                throw new InvalidOperationException(
                                    "Posting would exceed the PO allocation quantity.");
                            }

                            var card = await GetOrCreateCardAsync(
                                group,
                                orderItem,
                                sourceRequest,
                                user,
                                postingDate);

                            var cardItem = CreateCardItem(
                                air,
                                airItem,
                                portion,
                                card,
                                sourceRequest,
                                user,
                                postingDate);

                            var receiptMovement = CreateReceiptMovement(
                                air,
                                cardItem,
                                portion.Quantity,
                                user,
                                postingDate);

                            if (trackedItems.Any())
                            {
                                if (decimal.Truncate(portion.Quantity) != portion.Quantity)
                                {
                                    throw new InvalidOperationException(
                                        "Tracked asset quantities must be whole numbers.");
                                }

                                var selectedItems = trackedItems
                                    .Skip(trackedItemIndex)
                                    .Take((int)portion.Quantity)
                                    .ToList();

                                if (selectedItems.Count != (int)portion.Quantity)
                                {
                                    throw new InvalidOperationException(
                                        "Accepted property quantity does not have a complete physical-unit detail record.");
                                }

                                foreach (var sourceItem in selectedItems)
                                {
                                    var targetItem = CloneExtension(
                                        sourceItem,
                                        cardItem.Id,
                                        user,
                                        postingDate,
                                        orderItem.UnitCost);

                                    cardItem.PsCardItemExtns.Add(targetItem);
                                    receiptMovement.PsCardItemTransferItems.Add(
                                        CreateTransferItem(receiptMovement.Id, targetItem.Id, user, postingDate));
                                    assetCount++;
                                }

                                trackedItemIndex += selectedItems.Count;
                            }

                            cardItem.PsCardItemTransfers.Add(receiptMovement);
                            _db.PsCardItems.Add(cardItem);
                            await _db.SaveChangesAsync();

                            await SetAirItemReferenceAsync(cardItem.Id, airItem.Id);
                            receiptCount++;
                        }
                    }

                    _historyService.AddStatusHistory(
                        DocumentTypes.AcceptanceInspectionReport,
                        air.Id,
                        air.AIRNo ?? air.CtrlNo,
                        AirStatuses.Posted,
                        AirStatuses.Posted,
                        "AIR_INVENTORY_POSTED",
                        string.Format(
                            "Posted {0} card receipt(s), {1} asset unit(s).",
                            receiptCount,
                            assetCount),
                        user);

                    await _db.SaveChangesAsync();
                    transaction.Commit();

                    return new AIRInventoryPostingResult
                    {
                        ReceiptCount = receiptCount,
                        AssetCount = assetCount
                    };
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private static void ValidateAirForPosting(AIR air)
        {
            if (air == null)
            {
                throw new InvalidOperationException("AIR record not found.");
            }

            if (air.OverallStatus != AirStatuses.Posted ||
                air.AcceptanceStatus != AirAcceptanceStatuses.Accepted ||
                air.PostedDt == null)
            {
                throw new InvalidOperationException(
                    "Only an officially accepted AIR can be posted to inventory.");
            }
        }

        private async Task ValidateDatabaseSupportAsync()
        {
            int columnCount = await _db.Database.SqlQuery<int>(
                "SELECT COUNT(*) FROM sys.columns " +
                "WHERE object_id = OBJECT_ID('dbo.PsCardItems') " +
                "AND name = 'AIRItemId'")
                .SingleAsync();

            if (columnCount == 0)
            {
                throw new InvalidOperationException(
                    "Apply database script 20260907_01_AIRInventoryPosting.sql first.");
            }
        }

        private async Task<decimal> GetPreviouslyPostedQuantityAsync(Guid orderItemRequestId)
        {
            return await _db.Database.SqlQuery<decimal>(
                "SELECT ISNULL(SUM(Qty), 0) FROM dbo.PsCardItems " +
                "WHERE OrderItemRequestId = @requestId AND AIRItemId IS NOT NULL",
                new SqlParameter("@requestId", orderItemRequestId))
                .SingleAsync();
        }

        private async Task<bool> HasInventoryReceiptsAsync(List<Guid> airItemIds)
        {
            if (!airItemIds.Any())
            {
                return false;
            }

            var parameters = new List<SqlParameter>();
            var parameterNames = new List<string>();

            for (int index = 0; index < airItemIds.Count; index++)
            {
                string parameterName = "@airItemId" + index;
                parameterNames.Add(parameterName);
                parameters.Add(new SqlParameter(parameterName, airItemIds[index]));
            }

            string sql =
                "SELECT COUNT(*) FROM dbo.PsCardItems " +
                "WHERE AIRItemId IN (" + string.Join(",", parameterNames) + ")";

            int receiptCount = await _db.Database.SqlQuery<int>(
                sql,
                parameters.Cast<object>().ToArray())
                .SingleAsync();

            return receiptCount > 0;
        }

        private async Task<PsCard> GetOrCreateCardAsync(
            OrderItemGroupVM group,
            OrderItem orderItem,
            Request sourceRequest,
            string user,
            DateTime postingDate)
        {
            var card = await _db.PsCards.FirstOrDefaultAsync(c =>
                c.PsNo == group.StockNo &&
                c.Fund == sourceRequest.Fund &&
                c.FromDonation != true);

            if (card != null)
            {
                return card;
            }

            card = await _db.PsCards.FirstOrDefaultAsync(c =>
                c.PsNo == group.StockNo &&
                (c.Fund == null || c.Fund == "") &&
                c.FromDonation != true);

            if (card != null)
            {
                card.Fund = sourceRequest.Fund;
                card.FromDonation = false;
                card.UpdatedBy = user;
                card.UpdatedDt = postingDate;
                return card;
            }

            card = new PsCard
            {
                Id = Guid.NewGuid(),
                ItemCodeId = group.ItemCodeId,
                Fund = sourceRequest.Fund,
                FromDonation = false,
                PsNo = group.StockNo,
                Description = "Please see attachment.",
                SubAccountCode = _itemCodeService.GetSubAccountCode(group.ItemCodeId),
                CardCategory = group.CardCategory,
                InsertedBy = user,
                InsertedDt = postingDate,
                UpdatedBy = user,
                UpdatedDt = postingDate
            };

            if (orderItem.AllField != null)
            {
                card.AllField = CloneAllField(
                    orderItem.AllField,
                    card.Id,
                    user,
                    postingDate);
            }

            _db.PsCards.Add(card);
            return card;
        }

        private static AllField CloneAllField(
            AllField source,
            Guid cardId,
            string user,
            DateTime postingDate)
        {
            var target = new AllField();

            foreach (var sourceProperty in typeof(AllField).GetProperties(
                BindingFlags.Public | BindingFlags.Instance))
            {
                var targetProperty = typeof(AllField).GetProperty(sourceProperty.Name);
                bool isSimpleProperty = sourceProperty.PropertyType.IsValueType ||
                    sourceProperty.PropertyType == typeof(string);

                if (targetProperty != null &&
                    targetProperty.CanWrite &&
                    isSimpleProperty &&
                    sourceProperty.Name != "Id")
                {
                    targetProperty.SetValue(
                        target,
                        sourceProperty.GetValue(source, null),
                        null);
                }
            }

            target.Id = cardId;
            target.InsertedBy = user;
            target.InsertedDt = postingDate;
            target.UpdatedBy = user;
            target.UpdatedDt = postingDate;

            return target;
        }

        private static PsCardItem CreateCardItem(
            AIR air,
            AIRItem airItem,
            ReceiptPortion portion,
            PsCard card,
            Request sourceRequest,
            string user,
            DateTime postingDate)
        {
            var orderItem = portion.OrderItemRequest.OrderItem;
            var cardItem = new PsCardItem
            {
                Id = Guid.NewGuid(),
                PsCardId = card.Id,
                OrderItemRequestId = portion.OrderItemRequest.Id,
                PoNo = air.Order.PoNo,
                PoDate = air.Order.PoDate,
                AirNo = air.AIRNo ?? air.CtrlNo,
                AirDate = air.AIRDate,
                Qty = portion.Quantity,
                QtyIss = 0,
                QtyBal = portion.Quantity,
                Unit = orderItem.Unit,
                UnitCost = orderItem.UnitCost,
                TUnitCost = orderItem.UnitCost,
                Amount = (orderItem.UnitCost ?? 0) * portion.Quantity,
                GTotalCost = (orderItem.UnitCost ?? 0) * portion.Quantity,
                DeptId = sourceRequest.DeptId,
                DeptDisplay = sourceRequest.Department,
                Description = orderItem.Description,
                InvDist = airItem.InvDist,
                IsConsumable = airItem.IsConsumable,
                IsForICS = airItem.IsForICS,
                Vendor = air.Order.SupName,
                PostedBy = user,
                PostedDt = postingDate,
                InsertedBy = user,
                InsertedDt = postingDate,
                UpdatedBy = user,
                UpdatedDt = postingDate
            };

            cardItem.GroupId = cardItem.Id;
            return cardItem;
        }

        private static Request GetSourceRequest(OrderItemRequest orderItemRequest)
        {
            if (orderItemRequest == null)
            {
                throw new InvalidOperationException(
                    "An AIR receipt portion does not have an OrderItemRequest.");
            }

            if (orderItemRequest.RequestItem == null ||
                orderItemRequest.RequestItem.Request == null)
            {
                throw new InvalidOperationException(
                    "OrderItemRequest " + orderItemRequest.Id +
                    " does not have a complete RequestItem/Request relationship.");
            }

            if (string.IsNullOrWhiteSpace(orderItemRequest.RequestItem.Request.Fund))
            {
                throw new InvalidOperationException(
                    "OrderItemRequest " + orderItemRequest.Id +
                    " has no source Request Fund.");
            }

            return orderItemRequest.RequestItem.Request;
        }

        private static PsCardItemTransfer CreateReceiptMovement(
            AIR air,
            PsCardItem cardItem,
            decimal quantity,
            string user,
            DateTime postingDate)
        {
            return new PsCardItemTransfer
            {
                Id = Guid.NewGuid(),
                PsCardItemId = cardItem.Id,
                Qty = quantity,
                QtyIss = 0,
                QtyBal = quantity,
                Amount = cardItem.GTotalCost,
                TranType = "I",
                TransDate = air.AcceptedDate ?? air.AIRDate ?? postingDate,
                InsertedBy = user,
                InsertedDt = postingDate,
                UpdatedBy = user,
                UpdatedDt = postingDate
            };
        }

        private static PsCardItemTransferItem CreateTransferItem(
            Guid transferId,
            Guid extensionId,
            string user,
            DateTime postingDate)
        {
            return new PsCardItemTransferItem
            {
                Id = Guid.NewGuid(),
                PsCardItemTransferId = transferId,
                PsCardItemExtnId = extensionId,
                InsertedBy = user,
                InsertedDt = postingDate,
                UpdatedBy = user,
                UpdatedDt = postingDate
            };
        }

        private async Task SetAirItemReferenceAsync(Guid cardItemId, Guid airItemId)
        {
            await _db.Database.ExecuteSqlCommandAsync(
                "UPDATE dbo.PsCardItems SET AIRItemId = @airItemId WHERE Id = @cardItemId",
                new SqlParameter("@airItemId", airItemId),
                new SqlParameter("@cardItemId", cardItemId));
        }

        private static List<ReceiptPortion> BuildReceiptPortions(AIRItem airItem)
        {
            var portions = new List<ReceiptPortion>();
            decimal remainingQuantity = airItem.AcceptedQty ?? 0;

            if (!airItem.AIRItemAllocations.Any())
            {
                portions.Add(new ReceiptPortion
                {
                    OrderItemRequest = airItem.OrderItemRequest,
                    Quantity = remainingQuantity
                });
                return portions;
            }

            foreach (var allocation in airItem.AIRItemAllocations.OrderBy(a => a.Id))
            {
                decimal quantity = Math.Min(remainingQuantity, allocation.QtyInspected);
                if (quantity > 0)
                {
                    portions.Add(new ReceiptPortion
                    {
                        OrderItemRequest = allocation.OrderItemRequest,
                        Quantity = quantity
                    });
                }
                remainingQuantity -= quantity;
            }

            if (remainingQuantity != 0)
            {
                throw new InvalidOperationException(
                    "Accepted quantity cannot be reconciled to its allocations.");
            }

            return portions;
        }

        private static PsCardItemExtn CloneExtension(
            AIRItemExtn source,
            Guid cardItemId,
            string user,
            DateTime postingDate,
            decimal? acquisitionCost)
        {
            PsCardItemExtn target;

            if (source is AIRItemExtnVehicle)
            {
                target = new PsCardItemExtnVehicle();
            }
            else if (source is AIRItemExtnLand)
            {
                target = new PsCardItemExtnLand();
            }
            else if (source is AIRItemExtnBuilding)
            {
                target = new PsCardItemExtnBuilding();
            }
            else
            {
                target = new PsCardItemExtnOther();
            }

            foreach (var sourceProperty in source.GetType().GetProperties(
                BindingFlags.Public | BindingFlags.Instance))
            {
                var targetProperty = target.GetType().GetProperty(sourceProperty.Name);
                bool isSimpleProperty = sourceProperty.PropertyType.IsValueType ||
                    sourceProperty.PropertyType == typeof(string);

                if (targetProperty != null &&
                    targetProperty.CanWrite &&
                    targetProperty.PropertyType == sourceProperty.PropertyType &&
                    isSimpleProperty &&
                    sourceProperty.Name != "Id")
                {
                    targetProperty.SetValue(
                        target,
                        sourceProperty.GetValue(source, null),
                        null);
                }
            }

            target.Id = Guid.NewGuid();
            target.GroupId = target.Id;
            target.PsCardItemId = cardItemId;
            target.AIRItemExtnId = source.Id;
            target.AcqCost = acquisitionCost;
            target.InsertedBy = user;
            target.InsertedDt = postingDate;
            target.UpdatedBy = user;
            target.UpdatedDt = postingDate;

            return target;
        }

        private class ReceiptPortion
        {
            public OrderItemRequest OrderItemRequest { get; set; }
            public decimal Quantity { get; set; }
        }
    }
}
