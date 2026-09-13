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
    [AppAuthorize("PARSET")]
    public class ParSetController : BaseController
    {

        private static string GetSerialNo(PsCardItemExtn extn)
        {
            if (extn == null) return null;
            var other = extn as PsCardItemExtnOther;
            if (other != null && !string.IsNullOrWhiteSpace(other.SerialNo)) return other.SerialNo;
            var vehicle = extn as PsCardItemExtnVehicle;
            if (vehicle != null) return !string.IsNullOrWhiteSpace(vehicle.PlateNo) ? vehicle.PlateNo : vehicle.ConductionNo;
            return extn.SeriesNo;
        }

        //private readonly AppManEntities _db;
        private readonly IIcsParService _icsParService;
        private readonly IPsCardService _psCardService;
        private readonly ICodextnService _codextnService;
        private readonly IParIcsUploadService _uploadService;

        public ParSetController()
        {
            //_db = new AppManEntities();
            _icsParService = new IcsParService(_db);
            _psCardService = new PsCardService(_db);
            _codextnService = new CodextnService(_db);
            _uploadService = new ParIcsUploadService(_db);
        }

        // GET: PARs
        public ActionResult Index()
        {
            ViewBag.ForYear = DateTime.Now.Year;
            return View();
        }

        public ActionResult Read([DataSourceRequest] DataSourceRequest request, int? forYear, int? source)
        {
            var data = _icsParService.ParService.GetAllPo(forYear, source);
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
                int year = forYear.HasValue ? forYear.Value : DateTime.Now.Year;
                int poSource = (source.HasValue && (source.Value == 1 || source.Value == 2)) ? source.Value : 0;
                var pos = _icsParService.ParService.GetAllPo(year, poSource).ToList();
                int totalPos = pos.Count;
                int pending = pos.Count(p => (p.QtyFinished.HasValue ? p.QtyFinished.Value : 0) == 0 && (p.Qty.HasValue ? p.Qty.Value : 0) > 0);
                int partial = pos.Count(p => (p.QtyFinished.HasValue ? p.QtyFinished.Value : 0) > 0 && (p.QtyBalance.HasValue ? p.QtyBalance.Value : 0) > 0);
                int completed = pos.Count(p => (p.QtyBalance.HasValue ? p.QtyBalance.Value : 0) == 0 && (p.Qty.HasValue ? p.Qty.Value : 0) > 0);
                int remainingItems = (int)pos.Sum(p => (p.QtyBalance.HasValue ? p.QtyBalance.Value : 0));

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
                //var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.PoNo == poNo);
                /*Use CARD PO for PARS*/
                //int? poYear = poDate.HasValue ? (int?)poDate.Value.Year : (int?)DateTime.Now.Year;
                //int poSource = (source.HasValue && (source.Value == 1 || source.Value == 2)) ? source.Value : 0;
                var poSummary = await _icsParService.ParService.GetByPoNoAsync(poNo);

                //decimal acqValue = 0;
                //string supplier = order != null ? order.SupName : "";
                //string dept = order != null ? order.Department : (poSummary != null ? poSummary.Department : "");
                //string fund = order != null ? order.Fund : "";
                //string poMode = order != null ? order.PoMode : "";
                //DateTime? pDate = order != null ? order.PoDate : (poSummary != null ? poSummary.PoDate : poDate);

                decimal acqValue = poSummary != null ? poSummary.Amount ?? 0 : 0;
                //string supplier = order != null ? order.SupName : "";
                string dept = poSummary != null ? poSummary.Department : "";
                string fund = poSummary != null ? poSummary.Fund : "";
                //string poMode = poSummary != null ? order.PoMode : "";
                DateTime? pDate = poSummary != null ? poSummary.PoDate : null;

                //if (order != null && order.OrderItems.Any())
                //{
                //    acqValue = order.OrderItems.Sum(oi => (oi.Amount.HasValue ? oi.Amount.Value : (oi.UnitCost * oi.Qty).HasValue ? (oi.UnitCost * oi.Qty).Value : 0));
                //}

                int totalItems = poSummary != null ? (int)(poSummary.Qty.HasValue ? poSummary.Qty.Value : 0) : 0;
                int finishedItems = poSummary != null ? (poSummary.QtyFinished.HasValue ? poSummary.QtyFinished.Value : 0) : 0;
                int balanceItems = poSummary != null ? (int)(poSummary.QtyBalance.HasValue ? poSummary.QtyBalance.Value : 0) : 0;

                return Json(new
                {
                    success = true,
                    poNo = poNo,
                    poDate = pDate.HasValue ? pDate.Value.ToString("MM/dd/yyyy") : "",
                    //supplier = supplier,
                    department = dept,
                    fund = fund,
                    //poMode = poMode,
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
        
        #region PO ITEMS
        public ActionResult _PoItems(string poNo)
        {
            ViewData["PoNo"] = poNo;            
            return PartialView();
        }

        public ActionResult _PoItemsRead(
            [DataSourceRequest] DataSourceRequest request,
            string poNo,
            DateTime? poDate,
            Guid? deptId)
        {
            var data = _icsParService.ParService
                .GetItemsByPoNo(poNo, poDate, deptId)
                .ToList();

            if (data.Any())
            {
                var cardItemIds = data
                    .Select(s => s.Id)
                    .ToList();

                // Common PsCardItemExtn data for PAR (main items only)
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
                        HasPar = e.IcsParItems.Any(i => i.IcsPar.RefType == "P"),
                        ParNo = e.IcsParItems
                            .Where(i => i.IcsPar.RefType == "P")
                            .Select(i => i.IcsPar.RefNo)
                            .FirstOrDefault(),
                        IsPosted = e.IcsParItems.Any(i => i.IcsPar.RefType == "P" && i.IcsPar.PostedDt != null)
                    })
                    .ToList();

                // Vehicle extensions: PlateNo / ConductionNo
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
                        SerialNo = e.PlateNo ?? e.ConductionNo
                    }).OrderBy(o => o.ContentNo)
                    .ToList();

                // Other extensions: SerialNo
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

                var cardIds = data.Where(d => d.PsCardId.HasValue).Select(d => d.PsCardId.Value).Distinct().ToList();
                var allFields = _db.AllFields.AsNoTracking().Where(af => cardIds.Contains(af.Id)).ToList();

                foreach (var item in data)
                {
                    if (string.IsNullOrEmpty(item.CardNo))
                    {
                        item.CardNo = item.StockNo;
                    }

                    if (item.PsCardId.HasValue)
                    {
                        var af = allFields.FirstOrDefault(a => a.Id == item.PsCardId.Value);
                        if (af != null)
                        {
                            string baseDesc = !string.IsNullOrWhiteSpace(item.Description) ? item.Description : (item.Article ?? "");
                            string brand = !string.IsNullOrWhiteSpace(af.Brand) ? af.Brand.Trim() : "";
                            string model = !string.IsNullOrWhiteSpace(af.Model_) ? af.Model_.Trim() : "";

                            bool hasBrand = !string.IsNullOrEmpty(brand) && baseDesc.IndexOf(brand, StringComparison.OrdinalIgnoreCase) >= 0;
                            bool hasModel = !string.IsNullOrEmpty(model) && baseDesc.IndexOf(model, StringComparison.OrdinalIgnoreCase) >= 0;

                            string brandModel = "";
                            if (!hasBrand && !hasModel && !string.IsNullOrEmpty(brand) && !string.IsNullOrEmpty(model))
                            {
                                brandModel = string.Format(" ({0} {1})", brand, model);
                            }
                            else if (!hasBrand && !string.IsNullOrEmpty(brand))
                            {
                                brandModel = string.Format(" ({0})", brand);
                            }
                            else if (!hasModel && !string.IsNullOrEmpty(model))
                            {
                                brandModel = string.Format(" ({0})", model);
                            }

                            if (!string.IsNullOrEmpty(brandModel))
                            {
                                item.Description = (baseDesc + brandModel).Trim();
                            }
                        }
                    }

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

                        var generatedPars = itemExtns
                            .Where(e =>
                                e.HasPar &&
                                !string.IsNullOrEmpty(e.ParNo))
                            .Select(e => e.ParNo)
                            .Distinct()
                            .ToList();

                        if (generatedPars.Any())
                        {
                            item.GeneratedIcsNo = string.Join(", ", generatedPars);
                        }

                        bool isItemPosted = itemExtns.Any(e => e.IsPosted);
                        bool hasItemPar = (item.Balance.HasValue ? item.Balance.Value : 0) <= 0 || itemExtns.Any(e => e.HasPar);

                        if (isItemPosted)
                        {
                            item.IcsStatus = "Posted";
                        }
                        else if (hasItemPar)
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
                            (item.Balance.HasValue ? item.Balance.Value : 0) <= 0
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

            var cardItem = await _db.PsCardItems
                .Include(ci => ci.PsCard.AllField)
                .Include(ci => ci.PsCard.ItemCode)
                .AsNoTracking()
                .FirstOrDefaultAsync(ci => ci.Id == psCardItemId);

            string baseDesc = cardItem != null ? (cardItem.Description ?? (cardItem.PsCard != null && cardItem.PsCard.ItemCode != null ? cardItem.PsCard.ItemCode.Description : "")) : "";
            string brandModel = "";
            if (cardItem != null && cardItem.PsCard != null && cardItem.PsCard.AllField != null)
            {
                var af = cardItem.PsCard.AllField;
                string brand = !string.IsNullOrWhiteSpace(af.Brand) ? af.Brand.Trim() : "";
                string model = !string.IsNullOrWhiteSpace(af.Model_) ? af.Model_.Trim() : "";
                
                bool hasBrand = !string.IsNullOrEmpty(brand) && baseDesc.IndexOf(brand, StringComparison.OrdinalIgnoreCase) >= 0;
                bool hasModel = !string.IsNullOrEmpty(model) && baseDesc.IndexOf(model, StringComparison.OrdinalIgnoreCase) >= 0;

                if (!hasBrand && !hasModel && !string.IsNullOrEmpty(brand) && !string.IsNullOrEmpty(model))
                {
                    brandModel = string.Format(" ({0} {1})", brand, model);
                }
                else if (!hasBrand && !string.IsNullOrEmpty(brand))
                {
                    brandModel = string.Format(" ({0})", brand);
                }
                else if (!hasModel && !string.IsNullOrEmpty(model))
                {
                    brandModel = string.Format(" ({0})", model);
                }
            }
            string fullDesc = (baseDesc + brandModel).Trim();

            // ONLY MAIN PsCardItemExtn units (PsCardSubItemId == null)
            var extns = await _db.PsCardItemExtns
                .AsNoTracking()
                .Where(e => e.PsCardItemId == psCardItemId && e.PsCardSubItemId == null)
                .Include(e => e.IcsParItems.Select(i => i.IcsPar))
                .OrderBy(e => e.SeriesNo)
                .ThenBy(e => e.Id)
                .ToListAsync();

            var vehicleExtns = await _db.PsCardItemExtns
                .OfType<PsCardItemExtnVehicle>()
                .AsNoTracking()
                .Where(e => e.PsCardItemId == psCardItemId && e.PsCardSubItemId == null)
                .Select(e => new { e.Id, SerialNo = e.PlateNo ?? e.ConductionNo })
                .ToListAsync();

            var otherExtns = await _db.PsCardItemExtns
                .OfType<PsCardItemExtnOther>()
                .AsNoTracking()
                .Where(e => e.PsCardItemId == psCardItemId && e.PsCardSubItemId == null)
                .Select(e => new { e.Id, SerialNo = e.SerialNo })
                .ToListAsync();

            var list = new List<object>();
            int unitSeq = 1;
            foreach (var e in extns)
            {
                var ipi = e.IcsParItems
                    .Where(i => i.IcsPar != null && i.IcsPar.RefType == "P")
                    .OrderByDescending(i => i.IcsPar.InsertedDt)
                    .FirstOrDefault();

                string serial = null;
                var vExt = vehicleExtns.FirstOrDefault(v => v.Id == e.Id);
                if (vExt != null) serial = vExt.SerialNo;
                if (string.IsNullOrEmpty(serial))
                {
                    var oExt = otherExtns.FirstOrDefault(o => o.Id == e.Id);
                    if (oExt != null) serial = oExt.SerialNo;
                }
                if (string.IsNullOrEmpty(serial)) serial = e.SeriesNo;

                bool hasPar = ipi != null && ipi.IcsPar != null;
                bool isPosted = hasPar && ipi.IcsPar.PostedDt != null;
                string slipNo = hasPar ? (ipi.IcsPar.RefNo ?? "") : "";
                string officerName = hasPar
                    ? (!string.IsNullOrWhiteSpace(ipi.IcsPar.ReceivedBy) ? ipi.IcsPar.ReceivedBy : (!string.IsNullOrWhiteSpace(ipi.IssuedTo) ? ipi.IssuedTo : ""))
                    : "";
                string officerPosition = hasPar ? (ipi.IcsPar.ReceivedByPosition ?? "") : "";

                string status = !hasPar ? "Available" : (isPosted ? "Posted" : "Draft");
                string action = !hasPar ? "Generate" : (isPosted ? "View PAR" : "Continue");

                list.Add(new
                {
                    Id = ipi != null ? ipi.Id : Guid.Empty,
                    UnitNo = unitSeq++,
                    PsCardItemExtnId = e.Id,
                    Description = fullDesc,
                    PropNo = !string.IsNullOrEmpty(e.PropNo) ? e.PropNo : "Unassigned",
                    SerialNo = !string.IsNullOrEmpty(serial) ? serial : "-",
                    ParNo = slipNo,
                    AccountableOfficer = officerName,
                    IssuedTo = officerName,
                    Position = officerPosition,
                    AccountableOfficerPosition = officerPosition,
                    Status = status,
                    IsPosted = isPosted,
                    Action = action,
                    IcsNo = slipNo,
                    ParId = hasPar ? ipi.IcsParId : (Guid?)null
                });
            }

            return Json(list.ToDataSourceResult(request), JsonRequestBehavior.AllowGet);
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> UpdateIssuedTo(Guid parItemId, string issuedTo)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    return Json(new { success = false, message = "Access Denied: You do not have permission to edit PAR records." });
                }

                if (parItemId == Guid.Empty || string.IsNullOrWhiteSpace(issuedTo))
                {
                    return Json(new { success = false, message = "Invalid record or recipient name." });
                }

                var ipi = await _db.IcsParItems
                    .Include(i => i.IcsPar)
                    .Include(i => i.PsCardItemExtn)
                    .FirstOrDefaultAsync(i => i.Id == parItemId);

                if (ipi == null)
                {
                    return Json(new { success = false, message = "Generated property record not found." });
                }

                if (ipi.IcsPar != null && ipi.IcsPar.PostedDt != null)
                {
                    return Json(new { success = false, message = "Issued To cannot be changed because the generated PAR has already been posted." });
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

                return Json(new { success = true, message = "Accountable Officer successfully updated to " + issuedTo.Trim() + "." });
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
                .Where(w => w.RefType == "P" &&
                    w.IcsParItems.Any(i => i.PsCardItemExtn.PsCardItem.PoNo == poNo))
                .Select(s => new
                {
                    Id = s.Id,
                    MainPsCardItemExtnId = s.IcsParItems.Select(i => i.PsCardItemExtnId).FirstOrDefault(),
                    RefNo = s.RefNo,
                    RefDate = s.RefDate,
                    ReceivedBy = s.ReceivedBy,
                    ReceivedByPosition = s.ReceivedByPosition,
                    ReceivedDept = s.ReceivedDept,
                    ItemCount = s.IcsParItems.Count,
                    TotalValue = s.IcsParItems.Sum(x => (decimal?)x.Amount) ?? 0,
                    Status = s.PostedDt != null ? "Posted" : "Draft",
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

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _PoItemsUpdate([DataSourceRequest] DataSourceRequest request, ParIcsItemVm model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _psCardService.PsCardItem.UpdateIsForICSAsync(model, user, date);
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

        //public ActionResult _PoItemSetRead([DataSourceRequest] DataSourceRequest request, string poNo, DateTime? poDate, Guid? deptId)
        //{
        //    var data = _icsParService.ParService.GetItemSetsByPoNo(poNo, poDate, deptId);

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
        //    var data = _icsParService.ParService.GetItemSetDescriptionsByUnitGroupId(unitGroupId);

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
        //    var data = _icsParService.ParService.GetItemSetDescriptionItemsByUnitGroupDescriptionId(unitGroupDescriptionId);

        //    var result = new JsonNetResult
        //    {
        //        Data = data.ToDataSourceResult(request),
        //        JsonRequestBehavior = JsonRequestBehavior.AllowGet,
        //        Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
        //    };
        //    return result;
        //}        

        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PostPar(string parNo)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    return Json(new { success = false, Errors = "Access Denied!", message = "Access Denied: You do not have permission to post PAR records." }, JsonRequestBehavior.AllowGet);
                }

                if (string.IsNullOrWhiteSpace(parNo))
                {
                    return Json(new { success = false, Errors = "Invalid PAR Number.", message = "Invalid PAR Number." }, JsonRequestBehavior.AllowGet);
                }

                string user = ControllerContext.HttpContext.User.Identity.Name;
                DateTime date = System.DateTime.Now;

                await _icsParService.ParService.PostAsync(parNo, user, date);

                return Json(new { success = true, Errors = "", message = "PAR No. " + parNo + " was posted successfully." }, JsonRequestBehavior.AllowGet);
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                var msg = string.Join("; ", errors.Select(e => e.Message));
                return Json(new { success = false, Errors = msg, message = !string.IsNullOrEmpty(msg) ? msg : "Validation failed." }, JsonRequestBehavior.AllowGet);
            }
            catch (ValidationException validationException)
            {
                var msg = validationException.InnerException != null ? validationException.InnerException.Message : validationException.Message;
                return Json(new { success = false, Errors = msg, message = msg }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                var msg = e.InnerException != null ? e.InnerException.Message : e.Message;
                return Json(new { success = false, Errors = msg, message = msg }, JsonRequestBehavior.AllowGet);
            }
        }

        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UnpostPar(string parNo)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
                Access access = await accessTask;
                if (!access.AllowUnpost)
                {
                    return Json(new { success = false, Errors = "Access Denied!", message = "Access Denied: You do not have permission to unpost PAR records." }, JsonRequestBehavior.AllowGet);
                }

                if (string.IsNullOrWhiteSpace(parNo))
                {
                    return Json(new { success = false, Errors = "Invalid PAR Number.", message = "Invalid PAR Number." }, JsonRequestBehavior.AllowGet);
                }

                var par = await _db.IcsPars.FirstOrDefaultAsync(w => w.RefNo == parNo && w.RefType == "P");
                if (par == null)
                {
                    return Json(new { success = false, Errors = "PAR No. " + parNo + " not found.", message = "PAR No. " + parNo + " not found." }, JsonRequestBehavior.AllowGet);
                }

                if (string.IsNullOrEmpty(par.PostedBy) && par.PostedDt == null)
                {
                    return Json(new { success = false, Errors = "PAR No. " + parNo + " is already in Draft / Unposted state.", message = "PAR No. " + parNo + " is already in Draft / Unposted state." }, JsonRequestBehavior.AllowGet);
                }

                string user = ControllerContext.HttpContext.User.Identity.Name;
                DateTime date = System.DateTime.Now;

                await _icsParService.ParService.UnPostAsync(parNo, user, date);

                return Json(new { success = true, Errors = "", message = "PAR No. " + parNo + " was successfully unposted." }, JsonRequestBehavior.AllowGet);
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                var msg = string.Join("; ", errors.Select(e => e.Message));
                return Json(new { success = false, Errors = msg, message = !string.IsNullOrEmpty(msg) ? msg : "Validation failed." }, JsonRequestBehavior.AllowGet);
            }
            catch (ValidationException validationException)
            {
                var msg = validationException.InnerException != null ? validationException.InnerException.Message : validationException.Message;
                return Json(new { success = false, Errors = msg, message = msg }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                var msg = e.InnerException != null ? e.InnerException.Message : e.Message;
                return Json(new { success = false, Errors = msg, message = msg }, JsonRequestBehavior.AllowGet);
            }
        }

        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeletePar(Guid icsParId)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    return Json(new { success = false, message = "Access Denied: You do not have permission to delete PAR records." });
                }

                if (icsParId == Guid.Empty)
                {
                    return Json(new { success = false, message = "The Draft PAR could not be identified." });
                }

                var entity = await _db.IcsPars
                    .Include(i => i.IcsParItems)
                    .FirstOrDefaultAsync(w => w.Id == icsParId && w.RefType == "P");

                if (entity == null)
                {
                    return Json(new { success = false, message = "The Draft PAR was not found." });
                }

                if (entity.PostedBy != null || entity.PostedDt != null)
                {
                    return Json(new { success = false, message = "Cannot delete a posted PAR record. Only Draft/Unposted records can be deleted." });
                }

                string user = ControllerContext.HttpContext.User.Identity.Name;
                DateTime date = DateTime.Now;
                string parNo = entity.RefNo;

                // Clean up any uploaded files linked to this PAR
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

                return Json(new { success = true, message = "PAR " + parNo + " has been permanently deleted." });
            }
            catch (ValidationException validationException)
            {
                var msg = validationException.InnerException != null ? validationException.InnerException.Message : validationException.Message;
                return Json(new { success = false, message = msg });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<ActionResult> _ViewPar(string parNo)
        {
            var header = await _db.IcsPars.AsNoTracking().FirstOrDefaultAsync(w => w.RefNo == parNo && w.RefType == "P");
            if (header == null)
            {
                return HttpNotFound("PAR record not found.");
            }

            var rawItems = await _db.IcsParItems.AsNoTracking()
                .Where(w => w.IcsParId == header.Id)
                .Include(i => i.PsCardItemExtn.PsCardItem)
                .ToListAsync();

            var items = rawItems.Select(s => new IcsParItemVM
            {
                Id = s.Id,
                PsCardItemExtnId = s.PsCardItemExtnId,
                TUnitCost = s.Amount,
                Description = s.PsCardItemExtn != null && s.PsCardItemExtn.PsCardItem != null ? s.PsCardItemExtn.PsCardItem.Description : null,
                PoNo = s.PsCardItemExtn != null && s.PsCardItemExtn.PsCardItem != null ? s.PsCardItemExtn.PsCardItem.PoNo : null,
                PropNo = s.PsCardItemExtn != null ? s.PsCardItemExtn.PropNo : null,
                SerialNo = GetSerialNo(s.PsCardItemExtn),
                IssuedTo = s.IssuedTo,
                Designation = s.Designation
            }).ToList();

            var itemIds = items.Select(i => i.Id).ToList();
            var rawComponents = await _db.IcsParItemComponents.AsNoTracking()
                .Where(c => itemIds.Contains(c.IcsParItemId))
                .Include(c => c.PsCardSubItem)
                .Include(c => c.PsCardItemExtn)
                .ToListAsync();

            var components = rawComponents.Select(c => new ParBundleComponentDisplayVM
            {
                IcsParItemId = c.IcsParItemId,
                PsCardSubItemId = c.PsCardSubItemId,
                PsCardItemExtnId = c.PsCardItemExtnId,
                Description = c.PsCardSubItem != null ? c.PsCardSubItem.Description : null,
                ItemCode = c.PsCardSubItem != null ? c.PsCardSubItem.SubItemNo : null,
                SerialNo = GetSerialNo(c.PsCardItemExtn),
                Qty = c.Qty,
                Unit = c.PsCardSubItem != null ? c.PsCardSubItem.Unit : null,
                SourceType = c.PsCardSubItem != null ? c.PsCardSubItem.SourceType : null,
                IsRequiredForBundle = c.PsCardSubItem != null && (c.PsCardSubItem.IsRequiredForBundle ?? false)
            }).ToList();

            var lguRecord = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault();
            ViewBag.LguName = lguRecord != null ? lguRecord.Description : "LOCAL GOVERNMENT UNIT";
            ViewBag.Header = header;
            ViewBag.Items = items;
            ViewBag.Components = components;

            return PartialView("_ViewPar");
        }

        [HttpGet]
        public async Task<ActionResult> _UploadAir(string parNo)
        {
            var par = await _db.IcsPars.AsNoTracking().FirstOrDefaultAsync(w => w.RefNo == parNo && w.RefType == "P");
            if (par == null)
            {
                return HttpNotFound("PAR No. " + parNo + " not found.");
            }

            // Existing upload for this PAR
            var existingUpload = await _db.Uploads.AsNoTracking()
                .Where(w => w.ImageId == par.Id)
                .OrderByDescending(o => o.InsertedDt)
                .FirstOrDefaultAsync();

            // Associated PO Number from items
            var poNo = await _db.IcsParItems.AsNoTracking()
                .Where(w => w.IcsParId == par.Id)
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

            ViewBag.ParId = par.Id;
            ViewBag.ParNo = par.RefNo;
            ViewBag.Custodian = par.ReceivedBy;
            ViewBag.Department = par.ReceivedDept;
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
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UploadAirFile(Guid parId, HttpPostedFileBase airFile, string description)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
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
                var fileName = airFile.FileName;
                var ext = (!string.IsNullOrEmpty(fileName)) ? Path.GetExtension(fileName).ToLower() : "";
                if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                {
                    return Json(new { success = false, message = "Invalid file type. Only PDF, JPG, and PNG files are supported." });
                }

                var par = await _db.IcsPars.FirstOrDefaultAsync(w => w.Id == parId);
                if (par == null)
                {
                    return Json(new { success = false, message = "PAR record not found." });
                }

                string user = ControllerContext.HttpContext.User.Identity.Name;
                DateTime date = DateTime.Now;

                // Clean previous uploads for this PAR
                var existingUploads = await _db.Uploads.Where(w => w.ImageId == parId).ToListAsync();
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
                    ImageId = parId,
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
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ReusePoAirFile(Guid parId, Guid sourceUploadId)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
                Access access = await accessTask;
                if (!access.AllowAdd && !access.AllowEdit)
                {
                    return Json(new { success = false, message = "Access Denied!" });
                }

                var par = await _db.IcsPars.FirstOrDefaultAsync(w => w.Id == parId);
                if (par == null)
                {
                    return Json(new { success = false, message = "PAR record not found." });
                }

                var sourceUpload = await _db.Uploads.FindAsync(sourceUploadId);
                if (sourceUpload == null)
                {
                    return Json(new { success = false, message = "Source AIR document not found." });
                }

                string user = ControllerContext.HttpContext.User.Identity.Name;
                DateTime date = DateTime.Now;

                // Clean previous uploads for this PAR
                var existingUploads = await _db.Uploads.Where(w => w.ImageId == parId).ToListAsync();
                if (existingUploads.Any())
                {
                    _db.Uploads.RemoveRange(existingUploads);
                    await _db.SaveChangesAsync();
                }

                var newUpload = new Models.Upload
                {
                    Id = Guid.NewGuid(),
                    ImageId = parId,
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

                return Json(new { success = true, message = "PO AIR document successfully linked to this PAR.", fileName = sourceUpload.FileName });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error linking AIR document: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult> ViewAirDocument(Guid parId)
        {
            var upload = await _db.Uploads.Where(u => u.ImageId == parId).OrderByDescending(o => o.InsertedDt).FirstOrDefaultAsync();
            if (upload == null)
            {
                return HttpNotFound("No AIR document has been attached to this PAR record.");
            }

            string dir = !string.IsNullOrEmpty(upload.VirtualDirectory) ? upload.VirtualDirectory : _uploadService.GetDirectoryPath();
            string physicalPath = Path.Combine(dir, upload.FileName);

            if (!System.IO.File.Exists(physicalPath))
            {
                return HttpNotFound("The attached document file could not be found on the server storage.");
            }

            string contentType = MimeMapping.GetMimeMapping(upload.FileName);
            return File(physicalPath, contentType);
        }
        #endregion

        #region PAR ITEMS
        public ActionResult _Pars(Guid? cardItemGroupId, string postedBy)
        {
            ViewData["cardItemGroupId"] = cardItemGroupId;
            ViewData["postedBy"] = postedBy;
            return PartialView();
        }

        public ActionResult _ParsRead([DataSourceRequest] DataSourceRequest request, Guid? cardItemGroupId)
        {
            var data = _icsParService.IcsParItem.GetAllParItems(cardItemGroupId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }        

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ParsUpdate([DataSourceRequest] DataSourceRequest request, IcsParItem model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
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
        public async Task<ActionResult> _ParsDestroy([DataSourceRequest]DataSourceRequest request, IcsParItem model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    var result = await _icsParService.IcsParItem.DeleteAsync(model, user, date);                    
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

        public async Task<ActionResult> _ParItemEdit(Guid? parItemId)
        {
            var data = await _icsParService.IcsParItem.GetByIdAsync(parItemId);

            return PartialView(data);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ParItemSave(IcsParItem model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
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

        public async Task<ActionResult> _GeneratePar(Guid? mainPsCardItemExtnId, Guid? icsParId, Guid? psCardItemId, string refType)
        {
            var date = DateTime.Now;
            var issued = _db.Codextns.Where(w => w.CodeMast.Code == "ISSUED-BY").AsNoTracking().OrderByDescending(o => o.Code).FirstOrDefault();

            if (icsParId.HasValue && icsParId.Value != Guid.Empty)
            {
                var draftItem = await _db.IcsParItems
                    .Include(i => i.IcsPar)
                    .AsNoTracking()
                    .SingleOrDefaultAsync(i => i.IcsParId == icsParId.Value && i.IcsPar.RefType == "P");

                if (draftItem == null)
                {
                    return HttpNotFound("The Draft PAR was not found.");
                }

                if (draftItem.IcsPar.PostedDt != null)
                {
                    return Content("<div class='alert alert-warning' style='margin: 15px;'><strong>Notice:</strong> This PAR has already been posted and can no longer be edited.</div>");
                }

                if (mainPsCardItemExtnId.HasValue &&
                    mainPsCardItemExtnId.Value != Guid.Empty &&
                    mainPsCardItemExtnId.Value != draftItem.PsCardItemExtnId)
                {
                    return new HttpStatusCodeResult(400, "The Draft PAR does not belong to the selected physical unit.");
                }

                mainPsCardItemExtnId = draftItem.PsCardItemExtnId;
            }

            ParBundleBuilderInitVM bundleData = null;
            if (mainPsCardItemExtnId.HasValue && mainPsCardItemExtnId.Value != Guid.Empty)
            {
                var postedItem = await _db.IcsParItems
                    .Include(i => i.IcsPar)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.PsCardItemExtnId == mainPsCardItemExtnId.Value && i.IcsPar.RefType == "P" && i.IcsPar.PostedDt != null);
                if (postedItem != null)
                {
                    return Content("<div class='alert alert-warning' style='margin: 15px;'><strong>Notice:</strong> This physical unit is already assigned to posted PAR <b>" + (postedItem.IcsPar.RefNo ?? "") + "</b> and cannot be edited. Please view the posted PAR instead.</div>");
                }

                bundleData = await _icsParService.ParService.GetSingleUnitBundleDataAsync(mainPsCardItemExtnId.Value);
                psCardItemId = bundleData.PsCardItemId;
            }
            else if (psCardItemId.HasValue && psCardItemId.Value != Guid.Empty)
            {
                bundleData = await _icsParService.ParService.GetBundleBuilderDataAsync(psCardItemId.Value);
            }

            var psCardItem = psCardItemId.HasValue ? await _icsParService.ParService.GetByIdAsync(psCardItemId) : null;
            var model = new GenerateIcsParVM()
            {
                PsCardItemId = psCardItemId,
                MainPsCardItemExtnId = mainPsCardItemExtnId,
                ExistingParId = bundleData != null ? bundleData.ExistingParId : null,
                Qty = 1,
                Date = date,
                RefType = refType,                
                IndSet = "I",
                IcsPar = new IcsPar() 
                { 
                    ReceivedDate = date, 
                    IssuedDate = date, 
                    IssuedBy = issued != null ? issued.Description : null, 
                    IssuedByPosition = issued != null ? issued.Desc2 : null, 
                    IssuedDept = issued != null ? issued.Desc3 : null 
                }
            };

            if (psCardItem != null)
            {
                model.PoNo = psCardItem.PoNo;
                model.PoDate = psCardItem.PoDate;
                model.DeptId = psCardItem.DeptId;
            }
            if (bundleData != null && !string.IsNullOrEmpty(bundleData.PoNo))
            {
                model.PoNo = bundleData.PoNo;
            }

            // If existing draft info is present
            if (bundleData != null)
            {
                if (bundleData.ExistingRefDate.HasValue)
                {
                    model.Date = bundleData.ExistingRefDate.Value;
                    model.IcsPar.RefDate = bundleData.ExistingRefDate.Value;
                }
                if (bundleData.ExistingLocationId.HasValue)
                {
                    model.LocationId = bundleData.ExistingLocationId;
                    model.Location = bundleData.ExistingLocation;
                    model.LocationCode = !string.IsNullOrEmpty(bundleData.ExistingLocationCode) ? bundleData.ExistingLocationCode : bundleData.ExistingLocation;
                }
                if (bundleData.ExistingReceivedById.HasValue)
                {
                    model.IcsPar.ReceivedById = bundleData.ExistingReceivedById;
                }
                if (!string.IsNullOrWhiteSpace(bundleData.ExistingReceivedBy))
                {
                    model.IcsPar.ReceivedBy = bundleData.ExistingReceivedBy;
                }
                if (!string.IsNullOrWhiteSpace(bundleData.ExistingReceivedByTitle))
                {
                    model.IcsPar.ReceivedByTitle = bundleData.ExistingReceivedByTitle;
                }
                if (!string.IsNullOrWhiteSpace(bundleData.ExistingReceivedByTitle2))
                {
                    model.IcsPar.ReceivedByTitle2 = bundleData.ExistingReceivedByTitle2;
                }
                if (!string.IsNullOrWhiteSpace(bundleData.ExistingReceivedByPosition))
                {
                    model.IcsPar.ReceivedByPosition = bundleData.ExistingReceivedByPosition;
                }
                if (!string.IsNullOrWhiteSpace(bundleData.ExistingReceivedDept))
                {
                    model.IcsPar.ReceivedDept = bundleData.ExistingReceivedDept;
                }
                if (bundleData.ExistingReceivedDate.HasValue)
                {
                    model.IcsPar.ReceivedDate = bundleData.ExistingReceivedDate.Value;
                }
                if (!string.IsNullOrWhiteSpace(bundleData.ExistingIssuedTo))
                {
                    model.IssuedTo = bundleData.ExistingIssuedTo;
                }
                if (!string.IsNullOrWhiteSpace(bundleData.ExistingDesignation))
                {
                    model.Designation = bundleData.ExistingDesignation;
                }
                if (!string.IsNullOrWhiteSpace(bundleData.ExistingIssuedBy))
                {
                    model.IcsPar.IssuedBy = bundleData.ExistingIssuedBy;
                }
                if (!string.IsNullOrWhiteSpace(bundleData.ExistingIssuedByPosition))
                {
                    model.IcsPar.IssuedByPosition = bundleData.ExistingIssuedByPosition;
                }
                if (!string.IsNullOrWhiteSpace(bundleData.ExistingIssuedDept))
                {
                    model.IcsPar.IssuedDept = bundleData.ExistingIssuedDept;
                }
                if (bundleData.ExistingIssuedDate.HasValue)
                {
                    model.IcsPar.IssuedDate = bundleData.ExistingIssuedDate.Value;
                }
            }

            model.IcsPar.RefDate = model.Date;
            ViewData["psCardItemId"] = psCardItemId;
            ViewBag.ItemExtnName = psCardItemId.HasValue ? _psCardService.GetItemExtnName(psCardItemId) : null;
            ViewBag.BundleBuilderData = bundleData;

            return PartialView("_GeneratePar", model);
        }
        [Obsolete]
        public ActionResult _GenerateParSet(Guid? unitGroupId, string refType)
        {
            return HttpNotFound();
        }
        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GeneratePar(GenerateIcsParVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
                Access access = await accessTask;
                var isDraftUpdate = model.ExistingParId.HasValue;
                if (!isDraftUpdate && model.MainPsCardItemExtnId.HasValue)
                {
                    isDraftUpdate = await _db.IcsParItems.AnyAsync(i =>
                        i.PsCardItemExtnId == model.MainPsCardItemExtnId.Value &&
                        i.IcsPar.RefType == "P" &&
                        i.IcsPar.PostedDt == null);
                }

                if ((isDraftUpdate && !access.AllowEdit) || (!isDraftUpdate && !access.AllowAdd))
                {
                    ModelState.AddModelError("Access", isDraftUpdate
                        ? "Access Denied: You do not have permission to edit Draft PAR records."
                        : "Access Denied: You do not have permission to generate PAR records.");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    if (model.IndSet == "I")
                    {
                        await _icsParService.ParService.GeneratePAR(model, user, date);
                    }
                    else
                    {
                        await _icsParService.ParService.GenerateParSet(model, user, date);
                    }
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

        [HttpGet]
        public async Task<ActionResult> GetBundleBuilderData(Guid psCardItemId)
        {
            try
            {
                var data = await _icsParService.ParService.GetBundleBuilderDataAsync(psCardItemId);
                var result = new JsonNetResult
                {
                    Data = data,
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                    Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
                };
                return result;
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { error = msg }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public async Task<ActionResult> GetSingleUnitBundleData(Guid mainPsCardItemExtnId)
        {
            try
            {
                var data = await _icsParService.ParService.GetSingleUnitBundleDataAsync(mainPsCardItemExtnId);
                var result = new JsonNetResult
                {
                    Data = data,
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                    Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
                };
                return result;
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { error = msg }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult _GenerateParSelectionRead([DataSourceRequest] DataSourceRequest request, Guid? psCardItemId)
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

        public ActionResult _GenerateParSelectionSetRead([DataSourceRequest] DataSourceRequest request, Guid? unitGroupId)
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
        #endregion

        #region PAR SET ITEM
        public ActionResult _ParSet(Guid? cardItemGroupId, string postedBy)
        {
            ViewData["cardItemGroupId"] = cardItemGroupId;
            ViewData["postedBy"] = postedBy;
            return PartialView();
        }

        public ActionResult _ParSetRead([DataSourceRequest] DataSourceRequest request, Guid? cardItemGroupId)
        {
            var data = _icsParService.GetAllPars(cardItemGroupId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }
        
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ParSetDestroy([DataSourceRequest]DataSourceRequest request, IcsPar model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
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

        public async Task<ActionResult> _ParSetItemEdit(Guid? parItemId)
        {
            var data = await _icsParService.IcsParItem.GetByIdAsync(parItemId);

            return PartialView(data);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ParSetItemSave(IcsParItem model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
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

        public ActionResult GetAllPo(string text)
        {
            IQueryable<ParIcsPOGroupVM> model = null;
            
            if (string.IsNullOrEmpty(text))
            {
                model = _icsParService.ParService.GetAllPoCombo().AsQueryable<ParIcsPOGroupVM>();
            }
            else
            {
                text = text.Trim();
                model = _icsParService.ParService.GetAllPoCombo(text).AsQueryable<ParIcsPOGroupVM>();
            }

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

        #region Item Fields
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> LoadFields([System.Web.Http.FromBody] IcsParItem model)
        {
            var data = await _icsParService.IcsParItem.GetByIdAsync(model.Id);
            data.IcsPar = model.IcsPar;

            string partialView = "";
            var category = await _psCardService.PsCardItem.GetCategoryAsync(model.PsCardItemExtn.PsCardItemId);
            if (Enum.TryParse(category, out Enums.Category c))
            {
                if (c == Enums.CatLandsProp())
                {
                    partialView = "_FieldLand";
                }
                else if (c == Enums.CatTransportationProp())
                {
                    partialView = "_FieldTransportation";
                }
                else
                {
                    partialView = "_FieldOther";
                }
            }
            return PartialView(partialView, data);
        }
        #endregion        
    }
}
