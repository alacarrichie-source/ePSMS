using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services
{
    public interface IProcurementConsolidationService
    {
        IEnumerable<PurchaseRequestSelectionViewModel> GetAvailableApprovedPrs();
        ConsolidatePrWizardViewModel PrepareConsolidationWizard(string commaDelimitedPrIds);
        PurchaseRequestSelectionViewModel GetPurchaseRequestById(string prId);
        string ExecuteConsolidationAndCreatePo(ConsolidatePrWizardViewModel wizard, string userName);
        IEnumerable<PrItemAssignmentViewModel> GetAssignableItemsForPrs(string commaDelimitedPrIds);
    }
    
    public class ProcurementConsolidationService : IProcurementConsolidationService
    {
        private readonly AppManEntities _context;

        public ProcurementConsolidationService(AppManEntities context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IEnumerable<PurchaseRequestSelectionViewModel> GetAvailableApprovedPrs()
        {
            return _context.Requests
                .Where(r => r.PostedDt != null) // == "Approved")
                .OrderByDescending(r => r.PrDate)
                .Select(r => new PurchaseRequestSelectionViewModel
                {
                    Id = r.Id.ToString(),
                    PrNumber = r.PrNo,
                    PrDate = r.PrDate,
                    Department = r.Department,
                    Purpose = r.Purpose,
                    ItemCount = r.RequestItems.Count,
                    TotalAmount = r.RequestItems.Sum(s => s.TotalCost) ?? 0,
                    IsSelected = false
                })
                .ToList();
        }

        public ConsolidatePrWizardViewModel PrepareConsolidationWizard(string commaDelimitedPrIds)
        {
            var selectedIds = (commaDelimitedPrIds ?? "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();

            var available = GetAvailableApprovedPrs().ToList();
            foreach (var pr in available)
            {
                if (selectedIds.Contains(pr.Id) || selectedIds.Contains(pr.PrNumber))
                {
                    pr.IsSelected = true;
                }
            }

            return new ConsolidatePrWizardViewModel
            {
                PoNumber = $"PO-{DateTime.UtcNow.Year}-{new Random().Next(100, 999):D3}",
                PoDate = DateTime.Today,
                Supplier = "TechCorp Solutions Inc.",
                SupplierAddress = "784 Cyberpark Tower, Cubao, Quezon City, Metro Manila",
                ModeOfProcurement = "Public Bidding",
                DeliveryPeriod = "30 Days",
                PlaceOfDelivery = "Main Agency Warehouse, Port Area, Manila",
                PaymentTerms = "Check upon Delivery",
                DeliveryTerms = "FOB Destination",
                AvailablePrs = available,
                SelectedPrIds = available.Where(a => a.IsSelected).Select(a => a.Id).ToList()
            };
        }

        public string ExecuteConsolidationAndCreatePo(ConsolidatePrWizardViewModel wizard, string userName)
        {
            var selectedPrs = _context.Requests
                .Where(p => wizard.SelectedPrIds.Contains(p.Id.ToString()))
                .ToList();
            var date = DateTime.Now;
            var newPo = new Order
            {
                Id = Guid.NewGuid(),
                PoNo = wizard.PoNumber,
                PoDate = wizard.PoDate,
                SupName = wizard.Supplier,
                SupAddress = wizard.SupplierAddress,
                PoMode = wizard.ModeOfProcurement,
                DeliveryDate = wizard.DeliveryPeriod,
                DeliveryPlace = wizard.PlaceOfDelivery,
                TermPayment = wizard.PaymentTerms,
                TermDelivery = wizard.DeliveryTerms,
                //Status = "Draft",
                InsertedDt = date,
                InsertedBy = userName ?? "System",
                UpdatedBy = userName ?? "System",
                UpdatedDt = date
            };

            // Aggregate items by Catalog Code across all selected PRs
            var prItems = _context.RequestItems
                .Where(i => wizard.SelectedPrIds.Contains(i.Id.ToString()))
                .ToList();

            var grouped = prItems.GroupBy(i => i.PpmpCode).ToList();
            int itemNo = 1;
            decimal? grandTotal = 0;

            foreach (var group in grouped)
            {
                var first = group.First();
                var totalQty = (int?)group.Sum(x => x.Qty);
                var unitCost = (decimal?)first.UnitCost > 0 ? first.UnitCost : 100m;
                var itemTotal = (decimal)totalQty * unitCost;
                grandTotal += itemTotal;

                var sourcePrNumbers = group.Select(g => g.Request.PrNo).Distinct();

                newPo.OrderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = newPo.Id,
                    ItemNo = (itemNo++).ToString(),
                    PpmpCode = group.Key,
                    Description = first.Description,
                    Unit = first.Unit,
                    Qty = totalQty,
                    UnitCost = unitCost,
                    //SourcePrs = string.Join(", ", sourcePrNumbers),
                    //TechnicalSpecs = "Standard technical specifications per approved PR allocation.",
                    //CreatedDate = DateTime.UtcNow
                    InsertedBy = userName,
                    InsertedDt = date,
                    UpdatedBy = userName,
                    UpdatedDt = date
                });
            }

            //newPo.TotalAmount = grandTotal;

            _context.Orders.Add(newPo);
            _context.SaveChanges();

            return newPo.Id.ToString();
        }

        public PurchaseRequestSelectionViewModel GetPurchaseRequestById(string prId)
        {
            var r = _context.Requests
                .Include(x => x.RequestItems)
                .FirstOrDefault(x => x.Id == Guid.Parse(prId) || x.PrNo == prId);

            if (r == null) return null;

            return new PurchaseRequestSelectionViewModel
            {
                Id = r.Id.ToString(),
                PrNumber = r.PrNo,
                PrDate = r.PrDate,
                Department = r.Department,
                Purpose = r.Purpose,
                ItemCount = r.RequestItems.Count,
                TotalAmount = r.RequestItems.Sum(s => s.TotalCost) ?? 0,
                IsSelected = false
            };
        }

        public IEnumerable<PrItemAssignmentViewModel> GetAssignableItemsForPrs(string commaDelimitedPrIds)
        {
            var selectedIds = (commaDelimitedPrIds ?? "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();

            var query = _context.RequestItems
                .Include(i => i.Request)
                .AsQueryable();

            if (selectedIds.Any())
            {
                query = query.Where(i => selectedIds.Contains(i.PrId.ToString()) || selectedIds.Contains(i.Request.PrNo));
            }

            return query
                .OrderBy(i => i.Request.PrNo)
                .ThenBy(i => i.PpmpCode)
                .ThenBy(i => i.Description)
                .Select(i => new PrItemAssignmentViewModel
                {
                    Id = i.Id.ToString(),
                    PrNumber = i.Request.PrNo,
                    CatalogCode = i.PpmpCode,
                    Description = i.Description,
                    Unit = i.Unit,
                    RequestedQty = (int)(i.Qty ?? 0),
                    AssignedQty = (int)(i.Qty ?? 0),
                    RemainingQty = 0,
                    PoGroup = "PO Group 1 (Main)",
                    UnitCost = i.UnitCost ?? 0,
                    TechnicalSpecs = "Standard specifications per approved PR allocation."
                })
                .ToList();
        }
    }
}

//using iLgs.Models;
//using iLgs.Services.PPMP_;
//using iLgs.Services.PurchaseRequest;
//using System;
//using System.Collections.Generic;
//using System.Data.Entity;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Web;

//namespace iLgs.Services.PurchaseOrder
//{
//    public interface IProcurementConsolidationService
//    {
//        IEnumerable<OrderItemVM> ConsolidatePrItems(IEnumerable<string> prIds);
//        Task<IEnumerable<OrderItemVM>> ConsolidatePrItemsAsync(IEnumerable<string> prIds);
//        //IEnumerable<OrderItemVM> GroupAndAggregateLines(List<RequestItemVM> rawLines);
//        Dictionary<string, List<OrderItemVM>> GroupItemsByProcurementMode(
//            IEnumerable<OrderItemVM> items);
//        //bool CheckAppBudgetAvailability(string fundCluster, decimal totalRequiredAmount);
//        //decimal GetHistoricalAverageUnitCost(string catalogCode);
//    }

//    public class ProcurementConsolidationService : IProcurementConsolidationService
//    {
//        private readonly AppManEntities _context;
//        private readonly IPPMPService _iPPMPSerive;
//        private readonly IRequestService _requestService;

//        public ProcurementConsolidationService(AppManEntities context)
//        {
//            _context = context ?? throw new ArgumentNullException(nameof(context));
//            _iPPMPSerive = new PPMPService(context);
//            _requestService = new RequestService(context);
//        }

//        public IEnumerable<OrderItemVM> ConsolidatePrItems(IEnumerable<string> prIds)
//        {
//            if (prIds == null || !prIds.Any())
//                return Enumerable.Empty<OrderItemVM>();

//            var selectedPrIds = prIds.ToList();

//            //var rawLines = _context.RequestItems
//            //    .AsNoTracking()
//            //    .Where(r => selectedPrIds.Contains(r.PrId.ToString()))
//            //    .ToList();

//            var rawLines = _requestService.RequestItem.GetAll()
//                .Where(w => selectedPrIds.Contains(w.PrId.ToString())).ToList();

//            return GroupAndAggregateLines(rawLines);
//        }

//        public async Task<IEnumerable<OrderItemVM>> ConsolidatePrItemsAsync(IEnumerable<string> prIds)
//        {
//            if (prIds == null || !prIds.Any())
//                return Enumerable.Empty<OrderItemVM>();

//            var selectedPrIds = prIds.ToList();

//            //var rawLines = await _context.RequestItems
//            //    .AsNoTracking()
//            //    .Where(r => selectedPrIds.Contains(r.PrId.ToString()))
//            //    .ToListAsync();

//            var rawLines = await _requestService.RequestItem.GetAll()
//                .Where(w => selectedPrIds.Contains(w.PrId.ToString())).ToListAsync();

//            return GroupAndAggregateLines(rawLines);
//        }

//        private IEnumerable<OrderItemVM> GroupAndAggregateLines(List<RequestItemVM> rawLines)
//        {
//            return rawLines
//                .GroupBy(r => new { r.PpmpCode, r.Description, r.Unit })
//                .Select((group, index) =>
//                {
//                    int totalQty = (int)group.Sum(g => g.Qty);
//                    decimal avgUnitCost = (decimal)group.Average(g => g.UnitCost);
//                    string concatenatedPrs = string.Join(", ", group.Select(g => g.Request.PrNo).Distinct());

//                    return new OrderItemVM
//                    {
//                        Id = Guid.NewGuid(),
//                        ItemNo = (index + 1).ToString(),
//                        PpmpCode = group.Key.PpmpCode,
//                        Description = group.Key.Description,
//                        Unit = group.Key.Unit,
//                        Qty = totalQty,
//                        UnitCost = avgUnitCost,
//                        Amount = totalQty * avgUnitCost,
//                        SourcePrs = concatenatedPrs
//                        //TechnicalSpecs = group.FirstOrDefault()?.TechnicalSpecs
//                    };
//                })
//                .ToList();
//        }

//        public Dictionary<string, List<OrderItemVM>> GroupItemsByProcurementMode(
//            IEnumerable<OrderItemVM> items)
//        {
//            var result = new Dictionary<string, List<OrderItemVM>>(StringComparer.OrdinalIgnoreCase);
//            if (items == null) return result;

//            foreach (var item in items)
//            {
//                string mode = item.Amount > 50000m ? "Public Bidding" : "Small Value Procurement (Sec 53.9)";

//                if (!result.ContainsKey(mode))
//                {
//                    result[mode] = new List<OrderItemVM>();
//                }
//                result[mode].Add(item);
//            }

//            return result;
//        }

//        //public bool CheckAppBudgetAvailability(string fundCluster, decimal totalRequiredAmount)
//        //{
//        //    decimal allocated = _context.PPMPItems
//        //        .AsNoTracking()
//        //        //.Where(f => f.FundCluster == fundCluster && f.FiscalYear == DateTime.UtcNow.Year)
//        //        .Where(f => f.PPMP.ForYear == DateTime.UtcNow.Year)
//        //        .Select(f => f.qty)
//        //        .FirstOrDefault();            

//        //    return allocated >= totalRequiredAmount;
//        //}

//        //public decimal GetHistoricalAverageUnitCost(string catalogCode)
//        //{
//        //    if (string.IsNullOrWhiteSpace(catalogCode)) return 0.00m;

//        //    var history = _context.PurchaseOrderItems
//        //        .AsNoTracking()
//        //        .Where(i => i.CatalogCode == catalogCode)
//        //        .OrderByDescending(i => i.CreatedDate)
//        //        .Take(5)
//        //        .Select(i => i.UnitCost);

//        //    return history.Any() ? history.Average() : 0.00m;
//        //}
//    }
//}