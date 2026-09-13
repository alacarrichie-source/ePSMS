using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace iLgs.Services.PurchaseOrder
{
    public interface IPurchaseOrderService
    {
        IEnumerable<PurchaseOrderViewModel> GetAllPurchaseOrders();
        PurchaseOrderViewModel GetPurchaseOrderById(string id);
        IEnumerable<PurchaseOrderItemViewModel> GetItemsByPoId(string poId);
        PurchaseOrderItemViewModel InsertItem(string poId, PurchaseOrderItemViewModel item, string user, DateTime date);
        PurchaseOrderItemViewModel UpdateItem(PurchaseOrderItemViewModel item, string user, DateTime date);
        bool DeleteItem(string itemId);
        bool DeletePurchaseOrder(string poId);
        bool UpdateStatus(string poId, string status, string username);
        bool CancelPurchaseOrder(string poId, string reason, string username);
        IEnumerable<object> GetCatalogLookupItems(string filterText);
        IEnumerable<string> GetAvailableUnits();
        IEnumerable<string> GetSupplierList();
    }

    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly AppManEntities _context;

        public PurchaseOrderService(AppManEntities context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IEnumerable<PurchaseOrderViewModel> GetAllPurchaseOrders()
        {
            var list = _context.Orders
                .Include(i => i.OrderItems)
                .AsNoTracking()
                .OrderByDescending(p => p.InsertedDt)
                .ToList();

            return list.Select(p => new PurchaseOrderViewModel
            {
                Id = p.Id.ToString(),
                PoNumber = p.PoNo,
                PoDate = p.PoDate,
                Supplier = p.SupName,
                SupplierAddress = p.SupAddress,
                ModeOfProcurement = p.PoMode,
                Status = p.PostedDt != null ? "POSTED" : "PENDING",
                TotalAmount = p.OrderItems.Sum(s => s.Amount) ?? 0,
                ItemCount = p.OrderItems.Count,
                SourcePrSummary = p.PrNo
                //SourcePrSummary = string.Join(", ", p.OrderItems.Where(i => !string.IsNullOrEmpty(i.SourcePrs)).SelectMany(i => i.SourcePrs.Split(',')).Select(s => s.Trim()).Distinct())
            }).ToList();
        }

        public PurchaseOrderViewModel GetPurchaseOrderById(string id)
        {
            if (!Guid.TryParse(id, out Guid orderId))
                return null;

            var entity = _context.Orders
                .Include(p => p.OrderItems)
                .FirstOrDefault(p => p.Id == orderId);

            if (entity == null) return null;

            return new PurchaseOrderViewModel
            {
                Id = entity.Id.ToString(),
                PoNumber = entity.PoMode,
                PoDate = entity.PoDate,
                Supplier = entity.SupName,
                SupplierAddress = entity.SupAddress,
                ModeOfProcurement = entity.PoMode,
                DeliveryPeriod = entity.DeliveryDate,
                PlaceOfDelivery = entity.DeliveryPlace,
                PaymentTerms = entity.TermPayment,
                DeliveryTerms = entity.TermDelivery,
                Status = entity.PostedDt != null ? "POSTED" : "PENDING",
                TotalAmount = entity.OrderItems.Sum(s => s.Amount) ?? 0,
                ItemCount = entity.OrderItems.Count,
                SourcePrSummary = entity.PrNo,
                //SourcePrSummary = string.Join(", ", entity.Items.Where(i => !string.IsNullOrEmpty(i.SourcePrs)).SelectMany(i => i.SourcePrs.Split(',')).Select(s => s.Trim()).Distinct()),
                Items = entity.OrderItems.Select(i => new PurchaseOrderItemViewModel
                {
                    Id = i.Id.ToString(),
                    PurchaseOrderId = i.OrderId.ToString(),
                    ItemNo = int.Parse(i.ItemNo),
                    CatalogCode = i.PpmpCode,
                    Description = i.Description,
                    Unit = i.Unit,
                    Quantity = (int)i.Qty,
                    UnitCost = (decimal)i.UnitCost,
                    TotalCost = (decimal)(i.AddCost * i.UnitCost)
                    //SourcePrs = i.SourcePrs,
                    //TechnicalSpecs = i.TechnicalSpecs
                }).OrderBy(i => i.ItemNo).ToList()
            };
        }

        public IEnumerable<PurchaseOrderItemViewModel> GetItemsByPoId(string poId)
        {
            if (!Guid.TryParse(poId, out Guid orderId))
                return Enumerable.Empty<PurchaseOrderItemViewModel>();

            return _context.OrderItems
                .AsNoTracking()
                .Where(i =>  i.OrderId == orderId)
                .OrderBy(i => i.ItemNo)
                .Select(i => new PurchaseOrderItemViewModel
                {
                    Id = i.Id.ToString(),
                    PurchaseOrderId = i.OrderId.ToString(),
                    ItemNo = int.Parse(i.ItemNo),
                    CatalogCode = i.PpmpCode,
                    Description = i.Description,
                    Unit = i.Unit,
                    Quantity = (int)i.Qty,
                    UnitCost = (decimal)i.UnitCost,
                    TotalCost = (decimal)(i.AddCost * i.UnitCost)
                    //SourcePrs = i.SourcePrs,
                    //TechnicalSpecs = i.TechnicalSpecs
                })
                .ToList();
        }

        public PurchaseOrderItemViewModel InsertItem(string poId, PurchaseOrderItemViewModel item, string user, DateTime date)
        {
            new PurchaseOrderLifecycleService(_context).EnsureEditable(Guid.Parse(poId));
            var entity = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.Parse(poId),
                ItemNo = (int.Parse(_context.OrderItems
                    .Where(x => x.OrderId == Guid.Parse(poId))
                    .Max(x => x.ItemNo)) + 1).ToString() ,
                PpmpCode = item.CatalogCode,
                Description = item.Description,
                Unit = item.Unit,
                Qty = item.Quantity,
                UnitCost = item.UnitCost,
                //SourcePrs = item.SourcePrs,
                //TechnicalSpecs = item.TechnicalSpecs,
                //CreatedDate = DateTime.UtcNow
                InsertedBy = user,
                InsertedDt = date,
                UpdatedBy = user,
                UpdatedDt = date
            };

            _context.OrderItems.Add(entity);
            _context.SaveChanges();

            RecalculatePoTotal(poId);

            item.Id = entity.Id.ToString();
            item.ItemNo = int.Parse(entity.ItemNo);
            item.TotalCost = (decimal) (entity.Qty * entity.UnitCost);
            return item;
        }

        public PurchaseOrderItemViewModel UpdateItem(PurchaseOrderItemViewModel item, string user, DateTime date)
        {
            var entity = _context.OrderItems.FirstOrDefault(f => f.Id == Guid.Parse(item.Id));
            if (entity == null) throw new KeyNotFoundException($"Item '{item.Id}' not found.");
            new PurchaseOrderLifecycleService(_context).EnsureEditable(entity.OrderId.Value);
            
            entity.PpmpCode = item.CatalogCode;
            entity.Description = item.Description;
            entity.Unit = item.Unit;
            entity.Qty = item.Quantity;
            entity.UnitCost = item.UnitCost;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;
            //entity.TechnicalSpecs = item.TechnicalSpecs;

            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();

            RecalculatePoTotal(entity.OrderId.ToString());

            item.TotalCost = item.Quantity * item.UnitCost;
            return item;
        }

        public bool DeleteItem(string itemId)
        {
            if (!Guid.TryParse(itemId, out Guid orderItemId))
                return false;

            var entity = _context.OrderItems.Find(orderItemId);
            if (entity == null) return false;

            new PurchaseOrderLifecycleService(_context).EnsureEditable(entity.OrderId.Value);
            string poId = entity.OrderId.ToString();
            _context.OrderItems.Remove(entity);
            _context.SaveChanges();

            RecalculatePoTotal(poId);
            return true;
        }

        public bool DeletePurchaseOrder(string poId)
        {
            if (!Guid.TryParse(poId, out Guid orderId))
                return false;

            var po = _context.Orders.Find(orderId);
            if (po == null) return false;

            new PurchaseOrderLifecycleService(_context).EnsureEditable(po.Id);
            _context.Orders.Remove(po);
            _context.SaveChanges();
            return true;
        }

        public bool UpdateStatus(string poId, string status, string username)
        {
            //var po = _context.Orders.Find(poId);
            //if (po == null) return false;

            //po.Status = status;
            //po.UpdatedDt = DateTime.Now;

            //if (status == "Transmitted")
            //{
            //    po.TransmittedDate = DateTime.Now;
            //}
            //else if (status == "Approved")
            //{
            //    po.ApprovedDate = DateTime.Now;
            //    po.ApprovedBy = username ?? "Head of Agency";
            //}

            //_context.SaveChanges();
            return true;
        }

        public bool CancelPurchaseOrder(string poId, string reason, string username)
        {
            //var po = _context.Orders.Find(poId);
            //if (po == null) return false;

            //po.Status = "Cancelled";
            //po.CancellationReason = reason;
            //po.ModifiedDate = DateTime.UtcNow;

            //_context.SaveChanges();
            return true;
        }

        public IEnumerable<object> GetCatalogLookupItems(string filterText)
        {
            var query = _context.RequestItems
                .Select(i => new
                {
                    CatalogCode = i.PpmpCode,
                    Description = i.Description,
                    Unit = i.Unit,
                    EstimatedUnitCost = i.UnitCost
                })
                .Distinct();

            if (!string.IsNullOrWhiteSpace(filterText))
            {
                query = query.Where(i => i.CatalogCode.Contains(filterText) || i.Description.Contains(filterText));
            }

            return query.Take(50).ToList();
        }

        public IEnumerable<string> GetAvailableUnits()
        {
            return new[]
            {
                "Piece", "Set", "Unit", "Ream", "Box", "Pack", "Bottle", "Roll", "Lot", "Month", "Liter", "Meter", "Pair"
            };
        }

        public IEnumerable<string> GetSupplierList()
        {
            return new[]
            {
                "TechCorp Solutions Inc.",
                "Manila Office Supplies & Logistics",
                "Crown Paper & Stationery Mart",
                "Amanah IT Systems & Supplies",
                "Philippine Global Logistics Corp.",
                "Advance Computer Systems Corp."
            };
        }

        private void RecalculatePoTotal(string poId)
        {
            //var po = _context.Orders.Find(Guid.Parse(poId));
            //if (po != null)
            //{
            //    po.TotalAmount = _context.PurchaseOrderItems
            //        .Where(i => i.PurchaseOrderId == poId)
            //        .Sum(i => (decimal?)(i.Quantity * i.UnitCost)) ?? 0m;
            //    _context.SaveChanges();
            //}
        }
    }
}