using System.Collections.Generic;
using System.IO;
using System.Web;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.ParIcs;
using iLgs.Services.PropertyCard;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [AppAuthorize("ICSSET")]
    public class IcsSetController : BaseController
    {
        //private readonly AppManEntities _db;
        private readonly IPsCardService _psCardService;
        private readonly IIcsParService _icsParService;
        private readonly ICodextnService _codextnService;
        private readonly IParIcsUploadService _uploadService;

        public IcsSetController()
        {
            //_db = new AppManEntities();
            _psCardService = new PsCardService(_db);
            _icsParService = new IcsParService(_db);
            _codextnService = new CodextnService(_db);
            _uploadService = new ParIcsUploadService(_db);
        }

        //public IcsSetController(AppManEntities db, IPsCardService psCardService, IIcsParService icsParService, ICodextnService codextnService)
        //{
        //    _db = db;
        //    _psCardService = psCardService;
        //    _icsParService = icsParService;
        //    _codextnService = codextnService;
        //}

        // GET: Ics
        public ActionResult Index()
        {
            ViewBag.ForYear = DateTime.Now.Year;
            return View();
        }

                [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Read([DataSourceRequest] DataSourceRequest request, int? forYear, int? source)
        {
            int year = forYear ?? DateTime.Now.Year;
            int poSource = (source.HasValue && (source.Value == 1 || source.Value == 2)) ? source.Value : 0;
            var data = _icsParService.IcsService.GetAllPo(year, poSource).ToList();

            foreach (var item in data)
            {
                if (item.PoDate.HasValue && string.IsNullOrEmpty(item.SPoDate))
                {
                    item.SPoDate = item.PoDate.Value.ToString("MM/dd/yyyy");
                }
                if (!item.QtyFinished.HasValue)
                {
                    item.QtyFinished = 0;
                }
                if (string.IsNullOrEmpty(item.Department))
                {
                    item.Department = "General Services Office";
                }
            }

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult GetSummaryMetrics(int? forYear, int? source)
        {
            try
            {
                int year = forYear ?? DateTime.Now.Year;
                int poSource = (source.HasValue && (source.Value == 1 || source.Value == 2)) ? source.Value : 0;
                var pos = _icsParService.IcsService.GetAllPo(year, poSource).ToList();
                int totalPos = pos.Count;
                int pending = pos.Count(p => (p.QtyFinished ?? 0) == 0 && (p.Qty ?? 0) > 0);
                int partial = pos.Count(p => (p.QtyFinished ?? 0) > 0 && (p.QtyBalance ?? 0) > 0);
                int completed = pos.Count(p => (p.QtyBalance ?? 0) == 0 && (p.Qty ?? 0) > 0);
                int remainingItems = (int)pos.Sum(p => (p.QtyBalance ?? 0));

                return Json(new
                {
                    success = true,
                    totalPos = totalPos,
                    pending = pending,
                    partial = partial,
                    completed = completed,
                    remainingItems = remainingItems
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public async Task<ActionResult> GetPoDetails(string poNo, DateTime? poDate, Guid? deptId, int? source)
        {
            try
            {
                var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.PoNo == poNo);
                int? poYear = poDate.HasValue ? (int?)poDate.Value.Year : (int?)DateTime.Now.Year;
                int poSource = (source.HasValue && (source.Value == 1 || source.Value == 2)) ? source.Value : 0;
                var poSummary = await _icsParService.IcsService.GetByPoNoAsync(poNo);
                   

                decimal acqValue = 0;
                string supplier = order != null ? order.SupName : "";
                string dept = order != null ? order.Department : (poSummary != null ? poSummary.Department : "");
                string fund = order != null ? order.Fund : "";
                string poMode = order != null ? order.PoMode : "";
                DateTime? pDate = order != null ? order.PoDate : (poSummary != null ? poSummary.PoDate : poDate);

                if (order != null && order.OrderItems.Any())
                {
                    acqValue = order.OrderItems.Sum(oi => (oi.Amount ?? (oi.UnitCost * oi.Qty) ?? 0));
                }

                int totalItems = poSummary != null ? (int)(poSummary.Qty ?? 0) : 0;
                int finishedItems = poSummary != null ? (poSummary.QtyFinished ?? 0) : 0;
                int balanceItems = poSummary != null ? (int)(poSummary.QtyBalance ?? 0) : 0;

                return Json(new
                {
                    success = true,
                    poNo = poNo,
                    poDate = pDate.HasValue ? pDate.Value.ToString("MM/dd/yyyy") : "",
                    supplier = supplier,
                    department = dept,
                    fund = fund,
                    poMode = poMode,
                    acqValue = acqValue,
                    totalItems = totalItems,
                    finishedItems = finishedItems,
                    balanceItems = balanceItems
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        //[AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        //public async Task<ActionResult> GetPoDetails(string poNo, DateTime? poDate, Guid? deptId, int? source)
        //{
        //    try
        //    {
        //        var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.PoNo == poNo);
        //        int poSource = (source.HasValue && (source.Value == 1 || source.Value == 2)) ? source.Value : 0;
        //        var poSummary = _icsParService.IcsService.GetAllPo(poYear, poSource)
        //            .FirstOrDefault(p => p.PoNo == poNo);

        //        decimal acqValue = 0;
        //        string supplier = order != null ? order.SupName : "";
        //        string dept = order != null ? order.Department : (poSummary != null ? poSummary.Department : "");
        //        string fund = order != null ? order.Fund : "";
        //        string poMode = order != null ? order.PoMode : "";
        //        DateTime? pDate = order != null ? order.PoDate : (poSummary != null ? poSummary.PoDate : poDate);

        //        if (order != null && order.OrderItems.Any())
        //        {
        //            acqValue = order.OrderItems.Sum(oi => (oi.Amount ?? (oi.UnitCost * oi.Qty) ?? 0));
        //        }

        //        int totalItems = poSummary != null ? (int)(poSummary.Qty ?? 0) : 0;
        //        int finishedItems = poSummary != null ? (poSummary.QtyFinished ?? 0) : 0;
        //        int balanceItems = poSummary != null ? (int)(poSummary.QtyBalance ?? 0) : 0;

        //        return Json(new
        //        {
        //            success = true,
        //            poNo = poNo,
        //            poDate = pDate.HasValue ? pDate.Value.ToString("MM/dd/yyyy") : "",
        //            supplier = supplier,
        //            department = dept,
        //            fund = fund,
        //            poMode = poMode,
        //            acqValue = acqValue,
        //            totalItems = totalItems,
        //            finishedItems = finishedItems,
        //            balanceItems = balanceItems
        //        }, JsonRequestBehavior.AllowGet);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
        //    }
        //}                

        #region PO ITEMS
        public ActionResult _PoItems(string poNo)
        {
            ViewData["PoNo"] = poNo;
            return PartialView();
        }

        public ActionResult _PoItemsReadOld([DataSourceRequest] DataSourceRequest request, string poNo, DateTime? poDate, Guid? deptId)
        {
            var data = _icsParService.IcsService.GetItemsByPoNo(poNo, poDate, deptId).ToList();

            if (data.Any())
            {
                var cardItemIds = data.Select(s => s.Id).ToList();
                var extns = _db.PsCardItemExtns.AsNoTracking()
                    .Where(e => e.PsCardItemId.HasValue && cardItemIds.Contains(e.PsCardItemId.Value))
                    .Select(e => new
                    {
                        e.PsCardItemId,
                        e.UpcomingOfficer,
                        e.PropNo,
                        e.SeriesNo,
                        HasIcs = e.IcsParItems.Any(),
                        IcsNo = e.IcsParItems.Select(i => i.IcsPar.RefNo).FirstOrDefault()
                    })
                    .ToList();

                foreach (var item in data)
                {
                    var itemExtns = extns.Where(e => e.PsCardItemId == item.Id).ToList();
                    if (itemExtns.Any())
                    {
                        item.DesignatedCustodian = itemExtns.Select(e => e.UpcomingOfficer).FirstOrDefault(o => !string.IsNullOrEmpty(o));
                        item.PropNo = string.Join(", ", itemExtns.Where(e => !string.IsNullOrEmpty(e.PropNo)).Select(e => e.PropNo).Distinct().Take(2));
                        item.SerialNo = string.Join(", ", itemExtns.Where(e => !string.IsNullOrEmpty(e.SeriesNo)).Select(e => e.SeriesNo).Distinct().Take(2));
                        var generatedIcs = itemExtns.Where(e => e.HasIcs && !string.IsNullOrEmpty(e.IcsNo)).Select(e => e.IcsNo).Distinct().ToList();
                        if (generatedIcs.Any())
                        {
                            item.GeneratedIcsNo = string.Join(", ", generatedIcs);
                        }

                        item.IcsStatus = (item.Balance ?? 0) <= 0 ? "Generated" : "Pending";
                    }
                    else
                    {
                        item.IcsStatus = (item.Balance ?? 0) <= 0 ? "Generated" : "Pending";
                    }
                }
            }

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        public ActionResult _PoItemsRead(
    [DataSourceRequest] DataSourceRequest request,
    string poNo,
    DateTime? poDate,
    Guid? deptId)
        {
            var data = _icsParService.IcsService
                .GetItemsByPoNo(poNo, poDate, deptId)
                .ToList();

            if (data.Any())
            {
                var cardItemIds = data
                    .Select(s => s.Id)
                    .ToList();

                // Common PsCardItemExtn data
                var extns = _db.PsCardItemExtns
                    .AsNoTracking()
                    .Where(e =>
                        e.PsCardItemId.HasValue &&
                        e.PsCardSubItemId == null &&
                        cardItemIds.Contains(e.PsCardItemId.Value))
                    .Select(e => new
                    {
                        e.PsCardItemId,
                        e.UpcomingOfficer,
                        e.PropNo,

                        HasIcs = e.IcsParItems.Any(),

                        IcsNo = e.IcsParItems
                            .Select(i => i.IcsPar.RefNo)
                            .FirstOrDefault(),

                        IsPosted = e.IcsParItems.Any(i => i.IcsPar.PostedDt != null)
                    })
                    .ToList();

                // Vehicle extensions:
                // SerialNo should come from PlateNo
                var vehicleExtns = _db.PsCardItemExtns
                    .OfType<PsCardItemExtnVehicle>()
                    .AsNoTracking()
                    .Where(e =>
                        e.PsCardItemId.HasValue &&
                        e.PsCardSubItemId == null &&
                        cardItemIds.Contains(e.PsCardItemId.Value))
                    .Select(e => new
                    {
                        e.PsCardItemId,
                        e.ContentNo,
                        SerialNo = e.PlateNo

                    }).OrderBy(o => o.ContentNo)
                    .ToList();

                // Other extensions:
                // SerialNo should come from SerialNo
                var otherExtns = _db.PsCardItemExtns
                    .OfType<PsCardItemExtnOther>()
                    .AsNoTracking()
                    .Where(e =>
                        e.PsCardItemId.HasValue &&
                        e.PsCardSubItemId == null &&
                        cardItemIds.Contains(e.PsCardItemId.Value))
                    .Select(e => new
                    {
                        e.PsCardItemId,
                        e.ContentNo,
                        SerialNo = e.SerialNo
                    }).OrderBy(o => o.ContentNo)
                    .ToList();

                foreach (var item in data)
                {
                    var itemExtns = extns
                        .Where(e => e.PsCardItemId == item.Id)
                        .ToList();

                    if (itemExtns.Any())
                    {
                        item.DesignatedCustodian = itemExtns
                            .Select(e => e.UpcomingOfficer)
                            .FirstOrDefault(o => !string.IsNullOrEmpty(o));

                        item.PropNo = string.Join(", ",
                            itemExtns
                                .Where(e => !string.IsNullOrEmpty(e.PropNo))
                                .Select(e => e.PropNo)
                                .Distinct()
                                .Take(2));

                        // ------------------------------------
                        // Get SerialNo based on derived type
                        // ------------------------------------

                        var vehicleSerialNos = vehicleExtns
                            .Where(e =>
                                e.PsCardItemId == item.Id &&
                                !string.IsNullOrEmpty(e.SerialNo))
                            .Select(e => e.SerialNo);

                        var otherSerialNos = otherExtns
                            .Where(e =>
                                e.PsCardItemId == item.Id &&
                                !string.IsNullOrEmpty(e.SerialNo))
                            .Select(e => e.SerialNo);

                        item.SerialNo = string.Join(", ",
                            vehicleSerialNos
                                .Concat(otherSerialNos)
                                .Distinct());

                        var generatedIcs = itemExtns
                            .Where(e =>
                                e.HasIcs &&
                                !string.IsNullOrEmpty(e.IcsNo))
                            .Select(e => e.IcsNo)
                            .Distinct()
                            .ToList();

                        if (generatedIcs.Any())
                        {
                            item.GeneratedIcsNo =
                                string.Join(", ", generatedIcs);
                        }

                        bool isItemPosted = itemExtns.Any(e => e.IsPosted);
                        bool hasItemIcs = (item.Balance ?? 0) <= 0 || itemExtns.Any(e => e.HasIcs);

                        if (isItemPosted)
                        {
                            item.IcsStatus = "Posted";
                        }
                        else if (hasItemIcs)
                        {
                            item.IcsStatus = "Generated";
                        }
                        else
                        {
                            item.IcsStatus = "Pending";
                        }
                    }
                    else
                    {
                        item.IcsStatus =
                            (item.Balance ?? 0) <= 0
                                ? "Generated"
                                : "Pending";
                    }
                }
            }

            return Json(
                data.ToDataSourceResult(request),
                JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public async Task<ActionResult> _PoItemGeneratedUnitsRead([DataSourceRequest] DataSourceRequest request, Guid psCardItemId)
        {
            if (psCardItemId == Guid.Empty)
            {
                return Json(new DataSourceResult(), JsonRequestBehavior.AllowGet);
            }

            var extns = await _db.PsCardItemExtns
                .AsNoTracking()
                .Where(e => e.PsCardItemId == psCardItemId && e.PsCardSubItemId == null)
                .Include(e => e.IcsParItems.Select(i => i.IcsPar))
                .ToListAsync();

            var vehicleExtns = await _db.PsCardItemExtns
                .OfType<PsCardItemExtnVehicle>()
                .AsNoTracking()
                .Where(e => e.PsCardItemId == psCardItemId && e.PsCardSubItemId == null)
                .Select(e => new { e.Id, SerialNo = e.PlateNo })
                .ToListAsync();

            var otherExtns = await _db.PsCardItemExtns
                .OfType<PsCardItemExtnOther>()
                .AsNoTracking()
                .Where(e => e.PsCardItemId == psCardItemId && e.PsCardSubItemId == null)
                .Select(e => new { e.Id, SerialNo = e.SerialNo })
                .ToListAsync();

            var list = new List<object>();
            foreach (var e in extns)
            {
                var ipi = e.IcsParItems
                    .Where(i => i.IcsPar != null && i.IcsPar.RefType == "I")
                    .OrderByDescending(i => i.IcsPar.PostedDt.HasValue)
                    .ThenByDescending(i => i.IcsPar.InsertedDt)
                    .FirstOrDefault();
                var otherAccountability = e.IcsParItems.FirstOrDefault(i => i.IcsPar != null && i.IcsPar.RefType != "I");

                string serial = vehicleExtns.FirstOrDefault(v => v.Id == e.Id)?.SerialNo
                    ?? otherExtns.FirstOrDefault(o => o.Id == e.Id)?.SerialNo
                    ?? e.SeriesNo;

                bool isPosted = ipi != null && ipi.IcsPar != null && ipi.IcsPar.PostedDt != null;
                string slipNo = ipi != null && ipi.IcsPar != null ? ipi.IcsPar.RefNo : (otherAccountability != null ? otherAccountability.IcsPar.RefNo : "");
                string issuedTo = ipi != null && !string.IsNullOrWhiteSpace(ipi.IssuedTo)
                    ? ipi.IssuedTo
                    : (ipi != null && ipi.IcsPar != null ? ipi.IcsPar.ReceivedBy : (e.UpcomingOfficer ?? ""));

                list.Add(new
                {
                    Id = e.Id,
                    IcsParItemId = ipi != null ? (Guid?)ipi.Id : null,
                    IcsParId = ipi != null ? ipi.IcsParId : null,
                    PsCardItemExtnId = e.Id,
                    PropNo = !string.IsNullOrEmpty(e.PropNo) ? e.PropNo : "Unassigned",
                    SerialNo = !string.IsNullOrEmpty(serial) ? serial : "-",
                    IssuedTo = issuedTo ?? "",
                    IsPosted = isPosted,
                    Status = ipi == null ? (otherAccountability == null ? "Available" : "Assigned") : (isPosted ? "Posted" : "Draft"),
                    IcsNo = slipNo
                });
            }

            return Json(list.ToDataSourceResult(request), JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> UpdateIssuedTo(Guid icsParItemId, string issuedTo)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    return Json(new { success = false, message = "Access Denied: You do not have permission to edit ICS records." });
                }

                if (icsParItemId == Guid.Empty || string.IsNullOrWhiteSpace(issuedTo))
                {
                    return Json(new { success = false, message = "Invalid record or recipient name." });
                }

                var ipi = await _db.IcsParItems
                    .Include(i => i.IcsPar)
                    .Include(i => i.PsCardItemExtn)
                    .FirstOrDefaultAsync(i => i.Id == icsParItemId);

                if (ipi == null)
                {
                    return Json(new { success = false, message = "Generated property record not found." });
                }

                if (ipi.IcsPar != null && ipi.IcsPar.PostedDt != null)
                {
                    return Json(new { success = false, message = "Issued To cannot be changed because the generated ICS has already been posted." });
                }

                string user = ControllerContext.HttpContext.User.Identity.Name;
                DateTime date = DateTime.Now;

                ipi.IssuedTo = issuedTo.Trim();
                ipi.UpdatedBy = user;
                ipi.UpdatedDt = date;

                if (ipi.PsCardItemExtn != null)
                {
                    ipi.PsCardItemExtn.UpcomingOfficer = issuedTo.Trim();
                    ipi.PsCardItemExtn.UpdatedBy = user;
                    ipi.PsCardItemExtn.UpdatedDt = date;
                }

                if (ipi.IcsPar != null)
                {
                    ipi.IcsPar.ReceivedBy = issuedTo.Trim();
                    ipi.IcsPar.UpdatedBy = user;
                    ipi.IcsPar.UpdatedDt = date;
                }

                await _db.SaveChangesAsync();

                return Json(new { success = true, message = $"Issued To successfully updated to {issuedTo.Trim()}." });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = msg });
            }
        }

        public ActionResult _PoGeneratedSlipsRead([DataSourceRequest] DataSourceRequest request, string poNo)
        {
            if (string.IsNullOrEmpty(poNo))
            {
                return Json(new DataSourceResult(), JsonRequestBehavior.AllowGet);
            }

            var slips = _db.IcsPars.AsNoTracking()
                .Where(w => w.RefType == "I" &&
                    w.IcsParItems.Any(i => i.PsCardItemExtn.PsCardItem.PoNo == poNo))
                .Select(s => new
                {
                    Id = s.Id,
                    RefNo = s.RefNo,
                    RefDate = s.RefDate,
                    ReceivedBy = s.ReceivedBy,
                    ReceivedByPosition = s.ReceivedByPosition,
                    ReceivedDept = s.ReceivedDept,
                    ItemCount = s.IcsParItems.Count,
                    TotalValue = s.IcsParItems.Sum(x => (decimal?)x.Amount) ?? 0,
                    Status = s.PostedBy != null ? "Posted" : "Draft",
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt,
                    HasAir = _db.Uploads.Any(u => u.ImageId == s.Id),
                    AirFileName = _db.Uploads.Where(u => u.ImageId == s.Id).OrderByDescending(o => o.InsertedDt).Select(u => u.FileName).FirstOrDefault()
                })
                .OrderByDescending(o => o.RefDate)
                .ThenByDescending(o => o.RefNo);

            var result = new JsonNetResult
            {
                Data = slips.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [HttpPost]
        public async Task<ActionResult> AssignCustodian(Guid[] cardItemIds, string employeeName)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    return Json(new { success = false, message = "Access Denied: You do not have permission to edit ICS records." });
                }

                if (cardItemIds == null || cardItemIds.Length == 0)
                {
                    return Json(new { success = false, message = "Please select at least one item to assign custodian." });
                }

                if (string.IsNullOrWhiteSpace(employeeName))
                {
                    return Json(new { success = false, message = "Please select an employee/custodian." });
                }

                string user = ControllerContext.HttpContext.User.Identity.Name;
                DateTime now = DateTime.Now;

                var extns = await _db.PsCardItemExtns
                    .Where(w => (cardItemIds.Contains(w.PsCardItemId.Value) || cardItemIds.Contains(w.Id)) && !w.IcsParItems.Any())
                    .ToListAsync();

                if (!extns.Any())
                {
                    return Json(new { success = false, message = "No eligible accountable property items found for custodian assignment." });
                }

                foreach (var extn in extns)
                {
                    extn.UpcomingOfficer = employeeName.Trim();
                    extn.UpdatedBy = user;
                    extn.UpdatedDt = now;
                }

                await _db.SaveChangesAsync();

                return Json(new { success = true, updatedCount = extns.Count, message = string.Format("Successfully assigned custodian '{0}' to {1} property item(s).", employeeName, extns.Count) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to assign custodian: " + ex.Message });
            }
        }

        public async Task<ActionResult> _ViewIcs(string icsNo)
        {
            var header = await _db.IcsPars.AsNoTracking().FirstOrDefaultAsync(w => w.RefNo == icsNo && w.RefType == "I");
            if (header == null)
            {
                return HttpNotFound("ICS record not found.");
            }

            var items = await _db.IcsParItems.AsNoTracking()
                .Where(w => w.IcsParId == header.Id)
                .Select(s => new IcsParItemVM
                {
                    Id = s.Id,
                    PsCardItemExtnId = s.PsCardItemExtnId,
                    TUnitCost = s.Amount,
                    Description = s.PsCardItemExtn.PsCardItem.Description,
                    PoNo = s.PsCardItemExtn.PsCardItem.PoNo,
                    PropNo = s.PsCardItemExtn.PropNo,
                    IssuedTo = s.IssuedTo,
                    Designation = s.Designation
                })
                .ToListAsync();

            var lguRecord = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault();
            ViewBag.LguName = lguRecord != null ? lguRecord.Description : "LOCAL GOVERNMENT UNIT";
            ViewBag.Header = header;
            ViewBag.Items = items;

            return PartialView("_ViewIcs");
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _PoItemsUpdate([DataSourceRequest] DataSourceRequest request, ParIcsItemVm model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _psCardService.PsCardItem.UpdateNoICSAsync(model, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError("UpdateError", error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("UpdateError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("UpdateError", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        //public ActionResult _PoItemSetRead([DataSourceRequest] DataSourceRequest request, string poNo, DateTime? poDate, Guid? deptId)
        //{
        //    var data = _icsParService.IcsService.GetItemSetsByPoNo(poNo, poDate, deptId);

        //    var result = new JsonNetResult
        //    {
        //        Data = data.ToDataSourceResult(request),
        //        JsonRequestBehavior = JsonRequestBehavior.AllowGet,
        //        Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
        //    };
        //    return result;
        //}

        //public ActionResult _PoItemSetDescriptionRead([DataSourceRequest] DataSourceRequest request, Guid? unitGroupId)
        //{
        //    var data = _icsParService.IcsService.GetItemSetDescriptionsByUnitGroupId(unitGroupId);

        //    var result = new JsonNetResult
        //    {
        //        Data = data.ToDataSourceResult(request),
        //        JsonRequestBehavior = JsonRequestBehavior.AllowGet,
        //        Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
        //    };
        //    return result;
        //}

        //public ActionResult _PoItemSetDescriptionItemRead([DataSourceRequest] DataSourceRequest request, Guid? unitGroupDescriptionId)
        //{
        //    var data = _icsParService.IcsService.GetItemSetDescriptionItemsByUnitGroupDescriptionId(unitGroupDescriptionId);

        //    var result = new JsonNetResult
        //    {
        //        Data = data.ToDataSourceResult(request),
        //        JsonRequestBehavior = JsonRequestBehavior.AllowGet,
        //        Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
        //    };
        //    return result;
        //}

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> _PoItemSetDescriptionItemUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemUnitGroupDescriptionItem model)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics");
        //        Access access = await accessTask;
        //        if (!access.AllowEdit)
        //        {
        //            ModelState.AddModelError("UpdateError", "Update Access Denied!");
        //        }

        //        if (ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            model = await _icsParService.IcsService.UpdateNoICSAsync(model, user, date);
        //        }
        //    }
        //    catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
        //    {
        //        var errors = validationException.GetErrorsForModelState();
        //        foreach (var error in errors)
        //        {
        //            ModelState.AddModelError("UpdateError", error.Message);
        //        }
        //    }
        //    catch (ValidationException validationException)
        //    {
        //        ModelState.AddModelError("UpdateError", validationException.InnerException.Message);
        //    }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError("UpdateError", e.Message);
        //    }

        //    return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        //}

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> PostIcs(string icsNo)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    return Json(new { success = false, message = "Access Denied: You do not have permission to post ICS records." }, JsonRequestBehavior.AllowGet);
                }

                if (string.IsNullOrWhiteSpace(icsNo))
                {
                    return Json(new { success = false, message = "Invalid ICS Number." }, JsonRequestBehavior.AllowGet);
                }

                // Authoritative server-side verification: reload current ICS record from DB
                var ics = await _db.IcsPars.FirstOrDefaultAsync(w => w.RefNo == icsNo && w.RefType == "I");
                if (ics == null)
                {
                    return Json(new { success = false, message = $"ICS No. {icsNo} not found." }, JsonRequestBehavior.AllowGet);
                }

                if (!string.IsNullOrEmpty(ics.PostedBy))
                {
                    return Json(new { success = false, message = $"ICS No. {icsNo} was already posted by {ics.PostedBy} on {ics.PostedDt:MM/dd/yyyy}." }, JsonRequestBehavior.AllowGet);
                }

                // Authoritative server-side verification: check AIR attachment in DB
                bool hasAir = await _db.Uploads.AnyAsync(u => u.ImageId == ics.Id);
                if (!hasAir)
                {
                    return Json(new { success = false, message = "Unable to post ICS because an AIR document has not been uploaded. Please upload the AIR before posting." }, JsonRequestBehavior.AllowGet);
                }

                string user = ControllerContext.HttpContext.User.Identity.Name;
                DateTime date = System.DateTime.Now;

                await _icsParService.IcsService.PostAsync(icsNo, user, date);

                return Json(new { success = true, message = $"ICS No. {icsNo} was posted successfully." }, JsonRequestBehavior.AllowGet);
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                var msg = string.Join("; ", errors.Select(e => e.Message));
                return Json(new { success = false, message = !string.IsNullOrEmpty(msg) ? msg : "Validation failed." }, JsonRequestBehavior.AllowGet);
            }
            catch (ValidationException validationException)
            {
                var msg = validationException.InnerException != null ? validationException.InnerException.Message : validationException.Message;
                return Json(new { success = false, message = msg }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                var msg = e.InnerException != null ? e.InnerException.Message : e.Message;
                return Json(new { success = false, message = msg }, JsonRequestBehavior.AllowGet);
            }
        }

                [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> UnpostIcs(string icsNo)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics");
                Access access = await accessTask;
                if (!access.AllowUnpost)
                {
                    return Json(new { success = false, message = "Access Denied: You do not have permission to unpost ICS records." }, JsonRequestBehavior.AllowGet);
                }

                if (string.IsNullOrWhiteSpace(icsNo))
                {
                    return Json(new { success = false, message = "Invalid ICS Number." }, JsonRequestBehavior.AllowGet);
                }

                var ics = await _db.IcsPars.FirstOrDefaultAsync(w => w.RefNo == icsNo && w.RefType == "I");
                if (ics == null)
                {
                    return Json(new { success = false, message = $"ICS No. {icsNo} not found." }, JsonRequestBehavior.AllowGet);
                }

                if (string.IsNullOrEmpty(ics.PostedBy) && ics.PostedDt == null)
                {
                    return Json(new { success = false, message = $"ICS No. {icsNo} is already in Draft / Unposted state." }, JsonRequestBehavior.AllowGet);
                }

                string user = ControllerContext.HttpContext.User.Identity.Name;
                DateTime date = System.DateTime.Now;

                await _icsParService.IcsService.UnPostAsync(icsNo, user, date);

                return Json(new { success = true, message = $"ICS No. {icsNo} was successfully unposted." }, JsonRequestBehavior.AllowGet);
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                var msg = string.Join("; ", errors.Select(e => e.Message));
                return Json(new { success = false, message = !string.IsNullOrEmpty(msg) ? msg : "Validation failed." }, JsonRequestBehavior.AllowGet);
            }
            catch (ValidationException validationException)
            {
                var msg = validationException.InnerException != null ? validationException.InnerException.Message : validationException.Message;
                return Json(new { success = false, message = msg }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                var msg = e.InnerException != null ? e.InnerException.Message : e.Message;
                return Json(new { success = false, message = msg }, JsonRequestBehavior.AllowGet);
            }
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> DeleteIcs(string icsNo)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    return Json(new { success = false, message = "Access Denied: You do not have permission to delete ICS records." });
                }

                var entity = await _db.IcsPars
                    .Include(i => i.IcsParItems)
                    .FirstOrDefaultAsync(w => w.RefNo == icsNo && w.RefType == "I");

                if (entity == null)
                {
                    return Json(new { success = false, message = $"ICS No. {icsNo} was not found." });
                }

                if (entity.PostedBy != null || entity.PostedDt != null)
                {
                    return Json(new { success = false, message = "Cannot delete a posted ICS record. Only Draft/Unposted records can be deleted." });
                }

                string user = ControllerContext.HttpContext.User.Identity.Name;
                DateTime date = DateTime.Now;

                // Clean up any uploaded files linked to this ICS
                var uploads = await _db.Uploads.Where(w => w.ImageId == entity.Id).ToListAsync();
                if (uploads.Any())
                {
                    foreach (var u in uploads)
                    {
                        try
                        {
                            var filePath = Path.Combine(_uploadService.GetDirectoryPath(), u.FileName);
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                        }
                        catch { }
                    }
                    _db.Uploads.RemoveRange(uploads);
                    await _db.SaveChangesAsync();
                }

                // Use service to reset PsCardItemExtn and delete entity
                await _icsParService.DeleteAsync(entity, user, date);

                return Json(new { success = true, message = $"ICS {icsNo} has been permanently deleted." });
            }
            catch (ValidationException validationException)
            {
                return Json(new { success = false, message = validationException.InnerException != null ? validationException.InnerException.Message : validationException.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult> _UploadAir(string icsNo)
        {
            var ics = await _db.IcsPars.AsNoTracking().FirstOrDefaultAsync(w => w.RefNo == icsNo && w.RefType == "I");
            if (ics == null)
            {
                return HttpNotFound($"ICS No. {icsNo} not found.");
            }

            // Existing upload for this ICS
            var existingUpload = await _db.Uploads.AsNoTracking()
                .Where(w => w.ImageId == ics.Id)
                .OrderByDescending(o => o.InsertedDt)
                .FirstOrDefaultAsync();

            // Associated PO Number from items
            var poNo = await _db.IcsParItems.AsNoTracking()
                .Where(w => w.IcsParId == ics.Id)
                .Select(s => s.PsCardItemExtn.PsCardItem.PoNo)
                .FirstOrDefaultAsync();

            // Check if PO has an AIR with an upload already in ePSMS
            Guid? poAirUploadId = null;
            string poAirNo = null;
            string poAirFileName = null;

            if (!string.IsNullOrEmpty(poNo))
            {
                var airData = await (from a in _db.AIRs.AsNoTracking()
                                     where a.Order.PoNo == poNo || a.AIRItems.Any(ai => ai.OrderItemRequest.OrderItem.Order.PoNo == poNo)
                                     join u in _db.Uploads.AsNoTracking() on a.Id equals u.ImageId
                                     select new
                                     {
                                         a.AIRNo,
                                         u.Id,
                                         u.FileName
                                     }).FirstOrDefaultAsync();

                if (airData != null)
                {
                    poAirUploadId = airData.Id;
                    poAirNo = airData.AIRNo;
                    poAirFileName = airData.FileName;
                }
            }

            ViewBag.IcsId = ics.Id;
            ViewBag.IcsNo = ics.RefNo;
            ViewBag.Custodian = ics.ReceivedBy;
            ViewBag.Department = ics.ReceivedDept;
            ViewBag.PoNo = poNo;
            ViewBag.HasExistingUpload = existingUpload != null;
            ViewBag.ExistingUploadId = existingUpload != null ? (Guid?)existingUpload.Id : null;
            ViewBag.ExistingFileName = existingUpload != null ? existingUpload.FileName : null;
            ViewBag.ExistingUploadDate = existingUpload != null ? existingUpload.InsertedDt : null;
            ViewBag.PoAirUploadId = poAirUploadId;
            ViewBag.PoAirNo = poAirNo;
            ViewBag.PoAirFileName = poAirFileName;

            return PartialView();
        }

        [HttpPost]
        public async Task<ActionResult> UploadAirFile(Guid icsId, HttpPostedFileBase airFile, string description)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics");
                Access access = await accessTask;
                if (!access.AllowAdd && !access.AllowEdit)
                {
                    return Json(new { success = false, message = "Upload Access Denied!" });
                }

                if (airFile == null || airFile.ContentLength == 0)
                {
                    return Json(new { success = false, message = "Please select a valid file to upload." });
                }

                if (airFile.ContentLength > 10 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "The selected file exceeds the 10 MB maximum allowed size." });
                }

                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(airFile.FileName)?.ToLower();
                if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                {
                    return Json(new { success = false, message = "Invalid file type. Only PDF, JPG, and PNG files are supported." });
                }

                var ics = await _db.IcsPars.FirstOrDefaultAsync(w => w.Id == icsId);
                if (ics == null)
                {
                    return Json(new { success = false, message = "ICS record not found." });
                }

                string user = ControllerContext.HttpContext.User.Identity.Name;
                DateTime date = DateTime.Now;

                // Clean previous uploads for this ICS
                var existingUploads = await _db.Uploads.Where(w => w.ImageId == icsId).ToListAsync();
                if (existingUploads.Any())
                {
                    foreach (var u in existingUploads)
                    {
                        try
                        {
                            var oldPath = Path.Combine(_uploadService.GetDirectoryPath(), u.FileName);
                            if (System.IO.File.Exists(oldPath))
                            {
                                System.IO.File.Delete(oldPath);
                            }
                        }
                        catch { }
                    }
                    _db.Uploads.RemoveRange(existingUploads);
                    await _db.SaveChangesAsync();
                }

                var uploadId = Guid.NewGuid();
                var safeOriginalName = Path.GetFileName(airFile.FileName);
                var storedFileName = uploadId + "-" + safeOriginalName;
                var physicalDir = _uploadService.GetDirectoryPath();

                if (!Directory.Exists(physicalDir))
                {
                    Directory.CreateDirectory(physicalDir);
                }

                var physicalPath = Path.Combine(physicalDir, storedFileName);
                airFile.SaveAs(physicalPath);

                var upload = new Models.Upload
                {
                    Id = uploadId,
                    ImageId = icsId,
                    FileName = storedFileName,
                    Description = string.IsNullOrWhiteSpace(description) ? "AIR" : description,
                    VirtualDirectory = physicalDir,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };

                _db.Uploads.Add(upload);
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "AIR document uploaded successfully.", fileName = safeOriginalName, uploadId = uploadId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error uploading AIR document: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> ReusePoAirFile(Guid icsId, Guid sourceUploadId)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics");
                Access access = await accessTask;
                if (!access.AllowAdd && !access.AllowEdit)
                {
                    return Json(new { success = false, message = "Access Denied!" });
                }

                var ics = await _db.IcsPars.FirstOrDefaultAsync(w => w.Id == icsId);
                if (ics == null)
                {
                    return Json(new { success = false, message = "ICS record not found." });
                }

                var sourceUpload = await _db.Uploads.FindAsync(sourceUploadId);
                if (sourceUpload == null)
                {
                    return Json(new { success = false, message = "Source AIR document not found." });
                }

                string user = ControllerContext.HttpContext.User.Identity.Name;
                DateTime date = DateTime.Now;

                // Clean previous uploads for this ICS
                var existingUploads = await _db.Uploads.Where(w => w.ImageId == icsId).ToListAsync();
                if (existingUploads.Any())
                {
                    _db.Uploads.RemoveRange(existingUploads);
                    await _db.SaveChangesAsync();
                }

                var newUpload = new Models.Upload
                {
                    Id = Guid.NewGuid(),
                    ImageId = icsId,
                    FileName = sourceUpload.FileName,
                    Description = "AIR",
                    VirtualDirectory = sourceUpload.VirtualDirectory,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };

                _db.Uploads.Add(newUpload);
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "PO AIR document successfully linked to this ICS.", fileName = sourceUpload.FileName });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error linking AIR document: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult> ViewAirDocument(Guid icsId)
        {
            var upload = await _db.Uploads.Where(u => u.ImageId == icsId).OrderByDescending(o => o.InsertedDt).FirstOrDefaultAsync();
            if (upload == null)
            {
                return HttpNotFound("No AIR document has been attached to this ICS record.");
            }

            string dir = !string.IsNullOrEmpty(upload.VirtualDirectory) ? upload.VirtualDirectory : _uploadService.GetDirectoryPath();
            string physicalPath = Path.Combine(dir, upload.FileName);

            if (!System.IO.File.Exists(physicalPath))
            {
                var fallbackPath = Path.Combine(@"C:\UPLOADS\AIR", upload.FileName);
                if (System.IO.File.Exists(fallbackPath))
                {
                    physicalPath = fallbackPath;
                }
                else
                {
                    fallbackPath = Path.Combine(@"C:\UPLOADS\PAR", upload.FileName);
                    if (System.IO.File.Exists(fallbackPath))
                    {
                        physicalPath = fallbackPath;
                    }
                }
            }

            if (!System.IO.File.Exists(physicalPath))
            {
                return HttpNotFound($"Attached file '{upload.FileName}' not found on server.");
            }

            var ext = Path.GetExtension(upload.FileName)?.ToLower();
            string mime;
            switch (ext)
            {
                case ".pdf":
                    mime = "application/pdf";
                    break;
                case ".jpg":
                case ".jpeg":
                    mime = "image/jpeg";
                    break;
                case ".png":
                    mime = "image/png";
                    break;
                case ".gif":
                    mime = "image/gif";
                    break;
                default:
                    mime = "application/octet-stream";
                    break;
            }

            return new FilePathResult(physicalPath, mime);
        }        
        #endregion


        #region ICS ITEMS
        public ActionResult _Ics(Guid? cardItemGroupId, string postedBy)
        {
            ViewData["cardItemGroupId"] = cardItemGroupId;
            ViewData["postedBy"] = postedBy;
            return PartialView();
        }

        public ActionResult _IcsRead([DataSourceRequest] DataSourceRequest request, Guid? cardItemGroupId)
        {
            var data = _icsParService.IcsParItem.GetAllIcsItems(cardItemGroupId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _IcsUpdate([DataSourceRequest] DataSourceRequest request, IcsParItem model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _icsParService.IcsParItem.UpdateAsync(model, user, date);                    
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _IcsDestroy([DataSourceRequest]DataSourceRequest request, IcsParItem model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _icsParService.IcsParItem.DeleteAsync(model, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }            

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public async Task<ActionResult> _IcsItemEdit(Guid? parItemId)
        {
            var data = await _icsParService.IcsParItem.GetByIdAsync(parItemId);

            return PartialView(data);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _IcsItemSave(IcsParItem model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Update Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _icsParService.IcsParItem.UpdateAsync(model, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            var query = from state in ModelState.Values
                        from error in state.Errors
                        select error.ErrorMessage;

            var errorList = query.ToList();
            if (errorList.Count() > 0)
            {
                return Json(new { Errors = errorList }, JsonRequestBehavior.DenyGet);
            }

            return Json(new { Errors = "" }, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> _GenerateIcs(Guid? mainPsCardItemExtnId, Guid? icsParId, Guid? psCardItemId, string refType)
        {
            var date = DateTime.Now;
            var issued = _db.Codextns.Where(w => w.CodeMast.Code == "ISSUED-BY").AsNoTracking().OrderByDescending(o => o.Code).FirstOrDefault();

            if (icsParId.HasValue && icsParId.Value != Guid.Empty)
            {
                var draftItem = await _db.IcsParItems.Include(i => i.IcsPar).AsNoTracking()
                    .SingleOrDefaultAsync(i => i.IcsParId == icsParId.Value && i.IcsPar.RefType == "I");
                if (draftItem == null) return HttpNotFound("The Draft ICS was not found.");
                if (draftItem.IcsPar.PostedDt != null)
                    return Content("<div class='alert alert-warning' style='margin:15px'>This ICS is posted and can no longer be edited.</div>");
                mainPsCardItemExtnId = draftItem.PsCardItemExtnId;
            }

            ParBundleBuilderInitVM bundleData = null;
            if (mainPsCardItemExtnId.HasValue && mainPsCardItemExtnId.Value != Guid.Empty)
            {
                var posted = await _db.IcsParItems.Include(i => i.IcsPar).AsNoTracking()
                    .FirstOrDefaultAsync(i => i.PsCardItemExtnId == mainPsCardItemExtnId.Value && i.IcsPar.RefType == "I" && i.IcsPar.PostedDt != null);
                if (posted != null)
                    return Content("<div class='alert alert-warning' style='margin:15px'>This physical unit is already assigned to posted ICS <b>" + (posted.IcsPar.RefNo ?? "") + "</b>.</div>");
                bundleData = await _icsParService.IcsService.GetSingleUnitBundleDataAsync(mainPsCardItemExtnId.Value);
                psCardItemId = bundleData.PsCardItemId;
            }
            else if (psCardItemId.HasValue && psCardItemId.Value != Guid.Empty)
            {
                bundleData = await _icsParService.IcsService.GetBundleBuilderDataAsync(psCardItemId.Value);
            }

            var psCardItem = psCardItemId.HasValue ? await _icsParService.IcsService.GetByIdAsync(psCardItemId) : null;
            var model = new GenerateIcsParVM()
            {
                PsCardItemId = psCardItemId,
                MainPsCardItemExtnId = mainPsCardItemExtnId,
                ExistingParId = bundleData != null ? bundleData.ExistingParId : null,
                Qty = 1,
                Date = date,
                RefType = "I",
                IcsPar = new IcsPar() { ReceivedDate = date, IssuedDate = date, IssuedBy = issued != null ? issued.Description : null, IssuedByPosition = issued != null ? issued.Desc2 : null, IssuedDept = issued != null ? issued.Desc3 : null },
                IndSet = "I"
            };

            if (psCardItem != null) { model.PoNo = psCardItem.PoNo; model.PoDate = psCardItem.PoDate; model.DeptId = psCardItem.DeptId; }
            if (bundleData != null)
            {
                model.Date = bundleData.ExistingRefDate ?? date;
                model.LocationId = bundleData.ExistingLocationId;
                model.Location = bundleData.ExistingLocation;
                model.LocationCode = bundleData.ExistingLocationCode;
                model.IssuedTo = bundleData.ExistingIssuedTo;
                model.Designation = bundleData.ExistingDesignation;
                model.IcsPar.ReceivedById = bundleData.ExistingReceivedById;
                model.IcsPar.ReceivedBy = bundleData.ExistingReceivedBy;
                model.IcsPar.ReceivedByTitle = bundleData.ExistingReceivedByTitle;
                model.IcsPar.ReceivedByTitle2 = bundleData.ExistingReceivedByTitle2;
                model.IcsPar.ReceivedByPosition = bundleData.ExistingReceivedByPosition;
                model.IcsPar.ReceivedDept = bundleData.ExistingReceivedDept;
                model.IcsPar.ReceivedDate = bundleData.ExistingReceivedDate ?? date;
                model.IcsPar.IssuedBy = bundleData.ExistingIssuedBy ?? model.IcsPar.IssuedBy;
                model.IcsPar.IssuedByPosition = bundleData.ExistingIssuedByPosition ?? model.IcsPar.IssuedByPosition;
                model.IcsPar.IssuedDept = bundleData.ExistingIssuedDept ?? model.IcsPar.IssuedDept;
                model.IcsPar.IssuedDate = bundleData.ExistingIssuedDate ?? date;
            }

            model.IcsPar.RefDate = model.Date;
            ViewData["psCardItemId"] = psCardItemId;
            ViewBag.ItemExtnName = psCardItemId.HasValue ? _psCardService.GetItemExtnName(psCardItemId) : null;
            ViewBag.BundleBuilderData = bundleData;

            return PartialView("_GenerateIcs", model);
        }

        public ActionResult _GenerateIcsSet(Guid? unitGroupId, string refType)
        {
            var date = DateTime.Now;
            var model = new GenerateIcsParVM()
            {
                UnitGroupId = unitGroupId,
                Date = date,
                RefType = refType,
                IcsPar = new IcsPar() { ReceivedDate = date, IssuedDate = date },
                IndSet = "S"
            };

            model.IcsPar.RefDate = model.Date;
            ViewData["unitGroupId"] = unitGroupId;

            return PartialView(model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GenerateIcs(GenerateIcsParVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics");
                Access access = await accessTask;
                var isDraftUpdate = model.ExistingParId.HasValue;
                if (!isDraftUpdate && model.MainPsCardItemExtnId.HasValue)
                {
                    isDraftUpdate = await _db.IcsParItems.AnyAsync(i => i.PsCardItemExtnId == model.MainPsCardItemExtnId.Value && i.IcsPar.RefType == "I" && i.IcsPar.PostedDt == null);
                }
                if ((isDraftUpdate && !access.AllowEdit) || (!isDraftUpdate && !access.AllowAdd))
                {
                    ModelState.AddModelError("Access", isDraftUpdate ? "Access Denied: You cannot edit Draft ICS records." : "Access Denied: You cannot generate ICS records.");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _icsParService.IcsService.GenerateIcsBundle(model, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            var query = from state in ModelState.Values
                        from error in state.Errors
                        select error.ErrorMessage;

            var errorList = query.ToList();
            if (errorList.Count() > 0)
            {
                return Json(new { Errors = errorList }, JsonRequestBehavior.DenyGet);
            }

            return Json(new { Errors = "" }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult _GenerateIcsSelectionRead([DataSourceRequest] DataSourceRequest request, Guid? psCardItemId)
        {
            var data = _psCardService.PsCardItem.PsCardItemExtn.GetCardItemExtnForIcsParsByType(psCardItemId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        public ActionResult _GenerateIcsSelectionSetRead([DataSourceRequest] DataSourceRequest request, Guid? unitGroupId)
        {
            var data = _psCardService.PsCardItem.PsCardItemExtn.GetCardItemExtnSetForIcsParByUnitGroupId(unitGroupId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        public ActionResult _GenerateIcsBatch(string poNo, DateTime? poDate, Guid? deptId)
        {
            var issued = _db.Codextns.Where(w => w.CodeMast.Code == "ISSUED-BY").AsNoTracking().OrderByDescending(o => o.Code).FirstOrDefault();
            var date = DateTime.Now;
            var model = new GenerateIcsParVM()
            {
                PoNo = poNo,
                PoDate = poDate,
                DeptId = deptId,
                Date = date,                
                RefType = "I",
                IcsPar = new IcsPar() { ReceivedDate = date, IssuedDate = date, IssuedBy = issued?.Description, IssuedByPosition = issued?.Desc2, IssuedDept = issued?.Desc3}
            };

            return PartialView(model);
        }

        [HttpGet]
        public async Task<ActionResult> PreviewIcsBatch(string poNo, DateTime? poDate, Guid? deptId)
        {
            try
            {
                var data = await _icsParService.IcsService.BuildBatchPreviewAsync(poNo, poDate, deptId);
                return new JsonNetResult { Data = data, JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.InnerException != null ? ex.InnerException.Message : ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GenerateIcsBatch(GenerateIcsParVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _icsParService.IcsService.GenerateIcsBatchBundles(model, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            var query = from state in ModelState.Values
                        from error in state.Errors
                        select error.ErrorMessage;

            var errorList = query.ToList();
            if (errorList.Count() > 0)
            {
                return Json(new { Errors = errorList }, JsonRequestBehavior.DenyGet);
            }

            return Json(new { Errors = "" }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ICS SET ITEM
        public ActionResult _IcsSet(Guid? cardItemGroupId, string postedBy)
        {
            ViewData["cardItemGroupId"] = cardItemGroupId;
            ViewData["postedBy"] = postedBy;
            return PartialView();
        }

        public ActionResult _IcsSetRead([DataSourceRequest] DataSourceRequest request, Guid? cardItemGroupId)
        {
            var data = _icsParService.GetAllIcs(cardItemGroupId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _IcsSetDestroy([DataSourceRequest]DataSourceRequest request, IcsPar model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    var result = await _icsParService.DeleteAsync(model, user, date);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("DeleteError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public async Task<ActionResult> _IcsSetItemEdit(Guid? parItemId)
        {
            var data = await _icsParService.IcsParItem.GetByIdAsync(parItemId);

            return PartialView(data);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _IcsSetItemSave(IcsParItem model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Update Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _icsParService.IcsParItem.UpdateAsync(model, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            var query = from state in ModelState.Values
                        from error in state.Errors
                        select error.ErrorMessage;

            var errorList = query.ToList();
            if (errorList.Count() > 0)
            {
                return Json(new { Errors = errorList }, JsonRequestBehavior.DenyGet);
            }

            return Json(new { Errors = "" }, JsonRequestBehavior.AllowGet);
        }
        #endregion  

        #region Issuance View
        //public ActionResult _Issuance(Guid? cardItemId)
        //{
        //    ViewData["CardItemId"] = cardItemId;
        //    return PartialView();
        //}

        //public ActionResult _IssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? cardItemId)
        //{
        //    var data = _psCardService.PsCardItemIssuance.GetByCardItemId(cardItemId);

        //    var result = new JsonNetResult
        //    {
        //        Data = data.ToDataSourceResult(request),
        //        JsonRequestBehavior = JsonRequestBehavior.AllowGet,
        //        Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
        //    };
        //    return result;
        //}
        #endregion


        #region ICS/PAR Item Issuance
        public ActionResult _IcsParItemIssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? icsParId)
        {
            var data = _icsParService.IcsParItem.GetIssuance(icsParId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _IcsParItemIssuanceUpdate([DataSourceRequest] DataSourceRequest request, IcsParItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _icsParService.IcsParItem.UpdateIssuanceAsync(model, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        #endregion
        public ActionResult GetAllPo(string text)
        {

            IQueryable<ParIcsPOGroupVM> model = null;

            if (string.IsNullOrEmpty(text))
            {
                model = _icsParService.IcsService.GetAllPoCombo().AsQueryable<ParIcsPOGroupVM>();
            }
            else
            {
                text = text.Trim();
                model = _icsParService.IcsService.GetAllPoCombo(text).AsQueryable<ParIcsPOGroupVM>();
            }

            //return Json(formattedModel, JsonRequestBehavior.AllowGet);

            return Json(model.Select(c => new
            {
                PoNo = c.PoNo,
                PoDate = c.PoDate,
                AirNo = c.AirNo,
                AirDate = c.AirDate,
                DeptId = c.DeptId,
                Department = c.Department
            }), JsonRequestBehavior.AllowGet);
        }
    }
}






