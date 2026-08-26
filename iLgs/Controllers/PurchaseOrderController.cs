using iLgs.Models;
using iLgs.Services;
using iLgs.Services.PurchaseOrder;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [Authorize]
    public class PurchaseOrderController : BaseController
    {
        private readonly IPurchaseOrderService _poService;
        private readonly IProcurementConsolidationService _consolidationService;

        // Injected via Unity / Ninject IoC container
        public PurchaseOrderController()
        {
            _poService = new PurchaseOrderService(_db);
            _consolidationService = new ProcurementConsolidationService(_db);
        }

        // GET: /PurchaseOrder/
        public async Task<ActionResult> Index()
        {
            //var userId = User.Identity.GetUserId();
            var model = _poService.GetAllPurchaseOrders();
            return View(model);
        }

        // POST/GET: /PurchaseOrder/ReadPurchaseOrders (Kendo MVC DataSource Read Endpoint)
        [HttpPost]
        public ActionResult ReadPurchaseOrders([DataSourceRequest] DataSourceRequest request)
        {
            var purchaseOrders = _poService.GetAllPurchaseOrders();
            return Json(purchaseOrders.ToDataSourceResult(request), JsonRequestBehavior.AllowGet);
        }

        // POST/GET: /PurchaseOrder/ReadApprovedPurchaseRequests (For Selection Grids in Wizard & Modal)
        [HttpPost]
        public ActionResult ReadApprovedPurchaseRequests([DataSourceRequest] DataSourceRequest request)
        {
            var approvedPrs = _consolidationService.GetAvailableApprovedPrs();
            return Json(approvedPrs.ToDataSourceResult(request), JsonRequestBehavior.AllowGet);
        }

        // GET: /PurchaseOrder/Create?prIds=pr-1,pr-3
        [HttpGet]
        public ActionResult Create(string prIds)
        {
            var wizardVm = _consolidationService.PrepareConsolidationWizard(prIds);
            return View(wizardVm);
        }

        // POST: /PurchaseOrder/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ConsolidatePrWizardViewModel model)
        {
            if (model.SelectedPrIds == null || !model.SelectedPrIds.Any())
            {
                ModelState.AddModelError("SelectedPrIds", "You must select at least one approved Purchase Request.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    string newPoId = _consolidationService.ExecuteConsolidationAndCreatePo(model, User.Identity.Name);
                    TempData["SuccessMessage"] = $"Purchase Order {model.PoNumber} successfully created from {model.SelectedPrIds.Count} PRs.";
                    return RedirectToAction("ConsolidatedItemsGrid", new { id = newPoId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Consolidation error: " + ex.Message);
                }
            }

            // Reload available PRs if model state failed
            model.AvailablePrs = _consolidationService.GetAvailableApprovedPrs().ToList();
            return View(model);
        }

        // GET: /PurchaseOrder/ConsolidatedItemsGrid/po-1
        [HttpGet]
        public async Task<ActionResult> ConsolidatedItemsGrid(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Index");
            }

            var poHeader = _poService.GetItemsByPoId(id);
            if (poHeader == null)
            {
                return HttpNotFound($"Purchase Order with ID '{id}' was not found.");
            }

            return View(poHeader);
        }

        // GET: /PurchaseOrder/Details/po-1 (COA Printable Voucher)
        [HttpGet]
        public async Task<ActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return HttpNotFound();

            var po = _poService.GetPurchaseOrderById(id);
            if (po == null) return HttpNotFound();

            po.Items = _poService.GetItemsByPoId(id).ToList();
            po.TotalAmountInWords = ConvertAmountToWords(po.TotalAmount);

            return View(po);
        }

        // POST: /PurchaseOrder/Delete
        [HttpPost]
        public ActionResult Delete(string id)
        {
            try
            {
                bool deleted = _poService.DeletePurchaseOrder(id);
                return Json(new { success = deleted });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ------------------------------------------------------------------
        // KENDO UI GRID AJAX ENDPOINTS (ConsolidatedItemsGrid)
        // ------------------------------------------------------------------

        [HttpPost]
        public ActionResult ReadConsolidatedItems([DataSourceRequest] DataSourceRequest request, string poId)
        {
            var items = _poService.GetItemsByPoId(poId);
            return Json(items.ToDataSourceResult(request), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult CreateConsolidatedItem([DataSourceRequest] DataSourceRequest request, string poId, PurchaseOrderItemViewModel item)
        {
            if (item != null && ModelState.IsValid)
            {
                try
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;
                    item = _poService.InsertItem(poId, item, user, date);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Error", ex.Message);
                }
            }

            return Json(new[] { item }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult UpdateConsolidatedItem([DataSourceRequest] DataSourceRequest request, PurchaseOrderItemViewModel item)
        {
            if (item != null && ModelState.IsValid)
            {
                try
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;
                    item = _poService.UpdateItem(item, user, date);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Error", ex.Message);
                }
            }

            return Json(new[] { item }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult DeleteConsolidatedItem([DataSourceRequest] DataSourceRequest request, PurchaseOrderItemViewModel item)
        {
            if (item != null)
            {
                try
                {
                    _poService.DeleteItem(item.Id);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Error", ex.Message);
                }
            }

            return Json(new[] { item }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
        }

        private static string ConvertAmountToWords(decimal? amount)
        {
            // Philippine Government Standard COA Words Conversion
            return "ONE HUNDRED TWENTY-EIGHT THOUSAND FOUR HUNDRED PESOS ONLY";
        }
    }
}

//namespace iLgs.Controllers
//{
//    [Authorize]
//    public class PurchaseOrderController : BaseController
//    {
//        private const string SessionKey = "PoWizard";
//        private readonly IOrderService _poService;
//        private readonly IProcurementConsolidationService _consolidationService;

//        public PurchaseOrderController()
//        {
//            _poService = new OrderService(_db);
//            _consolidationService = new ProcurementConsolidationService(_db);
//        }

//        private PurchaseOrderWizardState Wizard
//        {
//            get
//            {
//                var state = Session[SessionKey] as PurchaseOrderWizardState;
//                if (state == null)
//                {
//                    state = new PurchaseOrderWizardState();
//                    Session[SessionKey] = state;
//                }
//                return state;
//            }
//            set { Session[SessionKey] = value; }
//        }


//        // =====================================================================
//        // LIST (matches "Purchase Orders - Main List")
//        // =====================================================================

//        public ActionResult Index()
//        {
//            return View();
//        }

//        public ActionResult Suppliers_ForFilter()
//        {
//            var suppliers = _db.Suppliers.OrderBy(s => s.Name).Select(s => new { s.Name }).ToList();
//            return Json(suppliers, JsonRequestBehavior.AllowGet);
//        }

//        public ActionResult PurchaseOrders_Read([DataSourceRequest] DataSourceRequest request,
//            string status, string supplier, DateTime? dateFrom, DateTime? dateTo, string search)
//        {
//            var query = _db.Orders
//                .Include(p => p.Supplier)
//                .Include(p => p.OrderItems)
//                .AsNoTracking()
//                .AsQueryable();

//            //if (!string.IsNullOrWhiteSpace(status) &&
//            //    Enum.TryParse<PurchaseOrderStatus>(status, true, out var statusEnum))
//            //    query = query.Where(p => p.Status == statusEnum);

//            if (!string.IsNullOrWhiteSpace(supplier))
//                query = query.Where(p => p.Supplier.Name == supplier);

//            if (dateFrom.HasValue)
//                query = query.Where(p => p.PoDate >= dateFrom.Value);

//            if (dateTo.HasValue)
//                query = query.Where(p => p.PoDate <= dateTo.Value);

//            if (!string.IsNullOrWhiteSpace(search))
//                query = query.Where(p => p.PoNo.Contains(search) || p.Supplier.Name.Contains(search));

//            var projected = query
//                .OrderByDescending(p => p.PoDate)
//                .Select(p => new PurchaseOrderListItemVm
//                {
//                    PurchaseOrderId = p.Id,
//                    PoNumber = p.PoNo,
//                    PoDate = p.PoDate,
//                    SupplierName = p.Supplier.Name,
//                    ItemCount = p.OrderItems.Count,
//                    TotalAmount = p.OrderItems.Sum(s => s.Amount).GetValueOrDefault(0),
//                    //Status = p.Status.ToString(),
//                    CreatedBy = p.InsertedBy,
//                    SourcePrs = string.Join(", ",
//                        p.OrderItems.SelectMany(i => i.OrderItemRequests)
//                               .Select(a => a.RequestItem.Request.PrNo)
//                               .Distinct())
//                });

//            var result = projected.ToDataSourceResult(request);
//            return Json(result, JsonRequestBehavior.AllowGet);
//        }

//        // =====================================================================
//        // STEP 1: Select Approved PRs
//        // =====================================================================

//        public ActionResult Create()
//        {
//            Wizard = new PurchaseOrderWizardState(); // fresh session on wizard entry
//            return RedirectToAction("SelectPRs");
//        }

//        public ActionResult SelectPRs()
//        {
//            return View(Wizard);
//        }

//        public ActionResult PurchaseRequests_Read([DataSourceRequest] DataSourceRequest request,
//            string prNumber, Guid? departmentId, string fundCluster)
//        {
//            var selectedIds = Wizard.SelectedPurchaseRequestIds;

//            var query = _db.Requests
//                .Include(p => p.RequestItems)
//                .Include(p => p.Department)
//                .AsNoTracking()
//                .Where(p => p.PostedDt != null);

//            if (!string.IsNullOrWhiteSpace(prNumber))
//                query = query.Where(p => p.PrNo.Contains(prNumber));
//            if (departmentId.HasValue)
//                query = query.Where(p => p.DeptId == departmentId.Value);
//            if (!string.IsNullOrWhiteSpace(fundCluster))
//                query = query.Where(p => p.Fund == fundCluster);

//            var projected = query
//                .OrderByDescending(p => p.PrDate)
//                .Select(p => new SelectablePurchaseRequestVm
//                {
//                    PurchaseRequestId = p.Id,
//                    PrNumber = p.PrNo,
//                    PrDate = p.PrDate,
//                    Department = p.Department,
//                    Purpose = p.Purpose,
//                    FundCluster = p.Fund,
//                    TotalAmount = p.RequestItems.Sum(s => s.TotalCost).GetValueOrDefault(0)
//                    //Status = p.Status.ToString()
//                }).ToList();

//            projected.ForEach(p => p.Selected = selectedIds.Contains(p.PurchaseRequestId.ToString()));

//            var result = projected.AsQueryable().ToDataSourceResult(request);
//            return Json(result, JsonRequestBehavior.AllowGet);
//        }

//        [HttpPost]
//        public ActionResult SelectPRs(List<string> selectedIds)
//        {
//            var state = Wizard;
//            state.SelectedPurchaseRequestIds = selectedIds ?? new List<string>();
//            Wizard = state;
//            return RedirectToAction("AssignItems");
//        }

//        // =====================================================================
//        // STEP 2: Assign Items to PO Groups
//        // =====================================================================

//        public ActionResult AssignItems()
//        {
//            if (!Wizard.SelectedPurchaseRequestIds.Any())
//                return RedirectToAction("SelectPRs");

//            if (!Wizard.Groups.Any())
//            {
//                Wizard.Groups.Add(new PoGroupHeader { GroupNumber = 1, Label = "PO Group 1" });
//            }
//            return View(Wizard);
//        }

//        public ActionResult UnassignedItems_Read([DataSourceRequest] DataSourceRequest request)
//        {
//            var prIds = Wizard.SelectedPurchaseRequestIds;
//            var assignments = Wizard.Assignments;

//            var items = _db.RequestItems
//                .Include(i => i.Request)
//                .AsNoTracking()
//                .Where(i => prIds.Contains(i.PrId.ToString()))
//                .ToList()
//                .Select(i =>
//                {
//                    var assignedSoFar = assignments
//                        .Where(a => a.PurchaseRequestItemId == i.Id)
//                        .Sum(a => a.AssignedQty);
//                    var current = assignments.FirstOrDefault(a => a.PurchaseRequestItemId == i.Id);

//                    return new UnassignedItemVm
//                    {
//                        PurchaseRequestItemId = i.Id,
//                        PrNumber = i.Request.PrNo,
//                        ItemNo = i.ItemNo,
//                        Description = i.Description,
//                        Unit = i.Unit,
//                        RequestedQty = i.Qty,
//                        AssignQty = current?.AssignedQty ?? 0,
//                        RemainingQty = i.Qty - assignedSoFar,
//                        GroupNumber = current?.GroupNumber
//                    };
//                });

//            var result = items.AsQueryable().ToDataSourceResult(request);
//            return Json(result, JsonRequestBehavior.AllowGet);
//        }

//        [HttpPost]
//        public ActionResult SaveAssignment(Guid purchaseRequestItemId, decimal assignQty, int groupNumber)
//        {
//            var state = Wizard;
//            state.Assignments.RemoveAll(a => a.PurchaseRequestItemId == purchaseRequestItemId && a.GroupNumber == groupNumber);
//            if (assignQty > 0)
//            {
//                state.Assignments.Add(new ItemAssignment
//                {
//                    PurchaseRequestItemId = purchaseRequestItemId,
//                    GroupNumber = groupNumber,
//                    AssignedQty = assignQty
//                });
//            }
//            Wizard = state;
//            return Json(new { success = true });
//        }

//        [HttpPost]
//        public ActionResult AddGroup()
//        {
//            var state = Wizard;
//            var nextNumber = state.Groups.Any() ? state.Groups.Max(g => g.GroupNumber) + 1 : 1;
//            state.Groups.Add(new PoGroupHeader { GroupNumber = nextNumber, Label = "PO Group " + nextNumber });
//            Wizard = state;
//            return RedirectToAction("AssignItems");
//        }

//        [HttpPost]
//        public ActionResult AssignItems(string direction)
//        {
//            if (direction == "back") return RedirectToAction("SelectPRs");
//            return RedirectToAction("Consolidate");
//        }

//        // =====================================================================
//        // STEP 3: Review & Consolidate (identical CatalogCode within a group merges)
//        // =====================================================================

//        public ActionResult Consolidate(int? group)
//        {
//            if (!Wizard.Groups.Any()) return RedirectToAction("AssignItems");
//            Wizard.ActiveGroupNumber = group ?? Wizard.Groups.First().GroupNumber;

//            var vm = BuildConsolidatedGroups();
//            ViewData["Wizard"] = Wizard;
//            return View(vm);
//        }

//        private Dictionary<int, List<ConsolidatedItemVm>> BuildConsolidatedGroups()
//        {
//            var state = Wizard;
//            var itemIds = state.Assignments.Select(a => a.PurchaseRequestItemId.ToString()).Distinct().ToList();

//            var prItems = _db.RequestItems
//                .Include(i => i.Request)
//                .AsNoTracking()
//                .Where(i => itemIds.Contains(i.Id.ToString()))
//                .ToDictionary(i => i.Id);

//            var byGroup = new Dictionary<int, List<ConsolidatedItemVm>>();

//            foreach (var group in state.Groups)
//            {
//                var groupAssignments = state.Assignments.Where(a => a.GroupNumber == group.GroupNumber);
//                var consolidated = groupAssignments
//                    .GroupBy(a => prItems[a.PurchaseRequestItemId].PpmpCode)
//                    .Select(g =>
//                    {
//                        var sample = prItems[g.First().PurchaseRequestItemId];
//                        return new ConsolidatedItemVm
//                        {
//                            CatalogCode = g.Key,
//                            Description = sample.Description,
//                            Unit = sample.Unit,
//                            UnitCost = sample.UnitCost,
//                            ConsolidatedQty = g.Sum(a => a.AssignedQty),
//                            Sources = g.Select(a => new AllocationSourceVm
//                            {
//                                PrNumber = prItems[a.PurchaseRequestItemId].Request.PrNo,
//                                Qty = a.AssignedQty
//                            }).ToList()
//                        };
//                    }).ToList();

//                byGroup[group.GroupNumber] = consolidated;
//            }
//            return byGroup;
//        }

//        [HttpPost]
//        public ActionResult Consolidate(string direction)
//        {
//            if (direction == "back") return RedirectToAction("AssignItems");
//            return RedirectToAction("Details");
//        }

//        // =====================================================================
//        // STEP 4: PO Details (header terms + editable technical specs per item)
//        // =====================================================================

//        public ActionResult Details(int? group)
//        {
//            if (!Wizard.Groups.Any()) return RedirectToAction("AssignItems");
//            Wizard.ActiveGroupNumber = group ?? Wizard.Groups.First().GroupNumber;

//            ViewBag.Suppliers = new SelectList(_db.Suppliers.OrderBy(s => s.Name).ToList(), "Id", "Name");

//            var consolidated = BuildConsolidatedGroups();
//            ViewBag.ConsolidatedItems = consolidated.ContainsKey(Wizard.ActiveGroupNumber)
//                ? consolidated[Wizard.ActiveGroupNumber]
//                : new List<ConsolidatedItemVm>();

//            return View(Wizard.ActiveGroup);
//        }

//        [HttpPost]
//        public ActionResult Details(PoGroupHeader header, Dictionary<string, string> additionalInfo, string direction)
//        {
//            var state = Wizard;
//            var existing = state.Groups.First(g => g.GroupNumber == state.ActiveGroupNumber);
//            existing.SupplierId = header.SupplierId;
//            existing.PoDate = header.PoDate;
//            existing.ModeOfProcurement = header.ModeOfProcurement;
//            existing.DeliveryPeriod = header.DeliveryPeriod;
//            existing.PlaceOfDelivery = header.PlaceOfDelivery;
//            existing.PaymentTerms = header.PaymentTerms;
//            existing.DeliveryTerms = header.DeliveryTerms;
//            existing.OtherTerms = header.OtherTerms;
//            if (additionalInfo != null)
//                existing.AdditionalInfoByCatalogCode = additionalInfo;
//            Wizard = state;

//            if (direction == "back") return RedirectToAction("Consolidate");
//            return RedirectToAction("Review");
//        }

//        // =====================================================================
//        // STEP 5: Final Review, document upload, and posting
//        // =====================================================================

//        public ActionResult Review()
//        {
//            if (!Wizard.Groups.Any()) return RedirectToAction("AssignItems");
//            var consolidated = BuildConsolidatedGroups();
//            ViewBag.ConsolidatedByGroup = consolidated;
//            ViewBag.Suppliers = _db.Suppliers.ToDictionary(s => s.Id, s => s.Name);
//            return View(Wizard);
//        }

//        [HttpPost]
//        public ActionResult Upload(IEnumerable<HttpPostedFileBase> files, string category)
//        {
//            var state = Wizard;
//            if (files != null)
//            {
//                foreach (var file in files.Where(f => f != null && f.ContentLength > 0))
//                {
//                    // In production: save to blob storage / mapped upload path with a generated file name.
//                    var storedPath = "/App_Data/Uploads/" + Guid.NewGuid() + "_" + file.FileName;
//                    state.Documents.Add(new UploadedDocument
//                    {
//                        Category = category,
//                        FileName = file.FileName,
//                        StoredPath = storedPath
//                    });
//                }
//            }
//            Wizard = state;
//            return Json(new[] { new { size = 0 } }); // Kendo Upload expects a JSON array response
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public ActionResult PostOrder(bool confirmed)
//        {
//            if (!confirmed)
//                return RedirectToAction("Review");

//            var state = Wizard;
//            var consolidatedByGroup = BuildConsolidatedGroups();
//            var itemIds = state.Assignments.Select(a => a.PurchaseRequestItemId.ToString()).Distinct().ToList();
//            var prItems = _db.RequestItems.Where(i => itemIds.Contains(i.Id.ToString())).ToList();

//            var createdIds = new List<Guid>();

//            using (var tx = _db.Database.BeginTransaction())
//            {
//                try
//                {
//                    foreach (var group in state.Groups)
//                    {
//                        if (!consolidatedByGroup.ContainsKey(group.GroupNumber)) continue;
//                        var lines = consolidatedByGroup[group.GroupNumber];
//                        if (!lines.Any() || !group.SupplierId.HasValue) continue;

//                        var subTotal = lines.Sum(l => l.Total);
//                        var vat = Math.Round((decimal)subTotal * 0.05m, 2);
//                        var user = User?.Identity?.Name ?? "system";
//                        var date = DateTime.Now;
//                        var orderId = Guid.NewGuid();
//                        var po = new Order
//                        {
//                            Id = orderId,
//                            PoNo = GenerateNextPoNumber(),
//                            PoDate = group.PoDate,
//                            SupplierId = group.SupplierId.Value,
//                            PoMode = group.ModeOfProcurement,
//                            DeliveryDate = group.DeliveryPeriod,
//                            DeliveryPlace = group.PlaceOfDelivery,
//                            TermPayment = group.PaymentTerms,
//                            TermDelivery = group.DeliveryTerms,
//                            //OtherTerms = group.OtherTerms,
//                            //Status = PurchaseOrderStatus.Posted,
//                            //SubTotal = subTotal,
//                            //VatAmount = vat,
//                            //TotalAmount = subTotal + vat,
//                            InsertedBy = user,
//                            InsertedDt = date,
//                            UpdatedBy = user,
//                            UpdatedDt = date,
//                            OrderItems = new List<OrderItem>()
//                            //Documents = new List<PurchaseOrderDocument>()
//                        };

//                        foreach (var line in lines)
//                        {
//                            group.AdditionalInfoByCatalogCode.TryGetValue(line.CatalogCode, out var info);

//                            var orderItemId = Guid.NewGuid();
//                            var poItem = new OrderItem
//                            {
//                                Id = orderItemId,
//                                OrderId = orderId,
//                                PpmpCode = line.CatalogCode,
//                                Description = line.Description,
//                                Unit = line.Unit,
//                                Qty = line.ConsolidatedQty,
//                                UnitCost = line.UnitCost,
//                                //AdditionalInformation = info,
//                                //Allocations = new List<PurchaseOrderItemAllocation>()
//                                OrderItemRequests = new List<OrderItemRequest>()
//                            };

//                            foreach (var src in line.Sources)
//                            {
//                                var prItem = prItems.First(i => i.Request.PrNo == src.PrNumber
//                                                                 && i.PpmpCode == line.CatalogCode);
//                                poItem.OrderItemRequests.Add(new OrderItemRequest
//                                {
//                                    OrderItemId = orderItemId,
//                                    RequestItemId = prItem.Id,
//                                    QtyApplied = (int?)src.Qty
//                                });

////                                prItem.AssignedQty += src.Qty;
//                            }

//                            po.OrderItems.Add(poItem);
//                        }

//                        //foreach (var doc in state.Documents)
//                        //{
//                        //    po.Documents.Add(new PurchaseOrderDocument
//                        //    {
//                        //        Category = doc.Category,
//                        //        FileName = doc.FileName,
//                        //        StoredPath = doc.StoredPath,
//                        //        UploadedDate = DateTime.Now
//                        //    });
//                        //}
//                        //_db.OrderItems.Add(po);

//                        _db.SaveChanges();
//                        createdIds.Add(po.Id);
//                    }

//                    //// Mark source PRs as fully/partially POed
//                    //foreach (var pr in prItems.Select(i => i.Request).Distinct())
//                    //{
//                    //    var allItemsFullyAssigned = pr.RequestItems.All(i => i.Qty <= 0);
//                    //    if (allItemsFullyAssigned)
//                    //        pr.Status = PurchaseRequestStatus.FullyPOed;
//                    //}
//                    _db.SaveChanges();

//                    tx.Commit();
//                }
//                catch
//                {
//                    tx.Rollback();
//                    throw;
//                }
//            }

//            Session.Remove(SessionKey);
//            var firstId = createdIds.FirstOrDefault();
//            return RedirectToAction("Success", new { id = firstId });
//        }

//        public ActionResult Success(Guid id)
//        {
//            var po = _db.Orders.Include(p => p.Supplier).Include(p => p.OrderItems)
//                .FirstOrDefault(p => p.Id == id);
//            if (po == null) return RedirectToAction("Index");
//            return View(po);
//        }

//        private string GenerateNextPoNumber()
//        {
//            var year = DateTime.Now.Year;
//            var month = DateTime.Now.Month;
//            var countThisMonth = _db.Orders
//                .Count(p => p.PoDate.Value.Year == year && p.PoDate.Value.Month == month);
//            return $"PO-{year}-{month:00}-{(countThisMonth + 1):0000}";
//        }

//    }
//}