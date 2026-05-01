using CrystalDecisions.CrystalReports.Engine;
using Dapper;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.PoAdjustments_;
using iLgs.Services.PropertyCard;
using iLgs.Services.StockCards;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [AppAuthorize("POTRANSFER")]
    public class PoTransferController : BaseController
    {
        private readonly ICodextnService _codextnService;
        private readonly IStockCardService _stockCardService;
        private readonly IPsCardItemService _psCardItemService;
        private readonly IPoAdjustmentService _poAdjustmentService;

        public PoTransferController()
        {
            _codextnService = new CodextnService(_db);
            _stockCardService = new StockCardService(_db);
            _psCardItemService = new PsCardItemService(_db);
            _poAdjustmentService = new PoAdjustmentService(_db);
        }

        public ActionResult Index()
        {
            var data = new PoQueryVM()
            {
                PoStatus = 3
            };
            return View(data);
        }

        public ActionResult PoAdjustmentRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = _poAdjustmentService.GetAll();
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> PoAdjustmentCreate([DataSourceRequest] DataSourceRequest request, PoAdjustmentVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "po_transfer");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _poAdjustmentService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> PoAdjustmentUpdate([DataSourceRequest] DataSourceRequest request, PoAdjustmentVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "po_transfer");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _poAdjustmentService.UpdateAsync(model, user, date);

                    // TO DO: update stock card
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
        public async Task<ActionResult> PoAdjustmentDestroy([DataSourceRequest]DataSourceRequest request, PoAdjustmentVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "po_transfer");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _poAdjustmentService.DeleteAsync(model, user, date);
                    // TO DO: update stocks
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

        public ActionResult PoRead([DataSourceRequest] DataSourceRequest request, string poNo)
        {
            poNo = string.IsNullOrWhiteSpace(poNo) ? "NO P.O. Reference" : poNo;
            IEnumerable<QueryPoVM> data = _db.Database.Connection.Query<QueryPoVM>("Exec Card_GetPoNumbers '', 3, @p0", new { p0 = poNo }).ToList();            
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            
            return result;
        }

        public ActionResult PoItemRead([DataSourceRequest] DataSourceRequest request, string fund, string poNo, DateTime? poDate, Guid? deptId)
        {
            var data = _db.PsCardItems
                .Include(i => i.PsCard.ItemCode.ItemType.Description)
                .Where(w => (w.PoNo == poNo || (w.PoNo == null && string.IsNullOrEmpty(poNo))) 
                    && w.PoDate == poDate
                    && w.PsCard.Fund == fund
                    && w.DeptId == deptId
                ).AsNoTracking()
                .Select(s => new PsCardItemVM
                {
                    Id = s.Id,
                    Account = s.PsCard.ItemCode.ItemType.Description,
                    StockNo = s.PsCard.PsNo,
                    Description = s.Description,
                    Unit = s.Unit,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    InvDist = s.InvDist,
                    InvDistDesc = _db.Codextns.Where(w => w.CodeMast.Code == "PS-REMARKS" && w.Code == s.InvDist).Select(x => x.Description).FirstOrDefault(),
                    InsertedBy = s.InsertedBy,
                    InsertedDt = s.InsertedDt,
                    UpdatedBy = s.UpdatedBy,
                    UpdatedDt = s.UpdatedDt,
                    Consumable = s.IsConsumable.HasValue ? (s.IsConsumable == true ? "Y" :  "N") : s.PsCard.ItemCode.IsConsumable
                })
                .AsQueryable();
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }
        
        public ActionResult _Transfer(string fund, string poNo, DateTime? poDate, Guid? deptId, string department)
        {
            var data = new PoTransferVM()
            {
                Fund = fund,
                PoNo = poNo,
                PoDate = poDate,
                DeptId = deptId,
                DeptDisplay = department
            };

            return PartialView(data);
        }


        [HttpPost]
        public async Task<ActionResult> Transfer(PoTransferVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "po_transfer");
                Access access = await accessTask;
                if (!access.IsAdmin)
                {
                    ModelState.AddModelError("UpdateError", "Access Denied!");
                }
                else
                {
                    ModelState.Clear();
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _stockCardService.TransferAsync(model, user, date);
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

        public ActionResult _TransferDept(string fund, string poNo, DateTime? poDate, Guid? deptId, string department)
        {
            var data = new PoTransferVM()
            {
                Fund = fund,
                PoNo = poNo,
                PoDate = poDate,
                DeptId = deptId,
                DeptDisplay = department,
                NewDeptId = deptId
            };

            return PartialView(data);
        }


        [HttpPost]
        public async Task<ActionResult> TransferDept(PoTransferVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "po_transfer");
                Access access = await accessTask;
                if (!access.IsAdmin)
                {
                    ModelState.AddModelError("UpdateError", "Access Denied!");
                }
                else
                {
                    ModelState.Clear();
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _stockCardService.TransferDeptAsync(model, user, date);
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

        public ActionResult _TransferPoNo(string fund, string poNo, DateTime? poDate, Guid? deptId, string department)
        {
            var data = new PoTransferPOVM()
            {
                Fund = fund,
                PoNo = poNo,
                PoDate = poDate,
                DeptId = deptId,
                DeptDisplay = department,
                NewPoDate = poDate,
            };

            return PartialView(data);
        }


        [HttpPost]
        public async Task<ActionResult> TransferPoNo(PoTransferPOVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "po_transfer");
                Access access = await accessTask;
                if (!access.IsAdmin)
                {
                    ModelState.AddModelError("UpdateError", "Access Denied!");
                }
                else
                {
                    ModelState.Clear();
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _stockCardService.TransferPoNoAsync(model, user, date);
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

        public ActionResult PoRpt(string poNo, int originalSw)
        {
            string stringname = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(stringname);
            string rptKey = ConfigurationManager.AppSettings["RptKey"];
            string un = decoder.UserID;
            string pw = rptKey; // decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            ReportClass rpt = new ReportClass();
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/Card_Po_.rpt"));
            rpt.SetDatabaseLogon(un, pw, svr, db_);

            rpt.Load();
            rpt.Refresh();

            foreach (Table table in rpt.Database.Tables)
            {
                var logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo.ServerName = svr;
                logonInfo.ConnectionInfo.DatabaseName = db_;
                logonInfo.ConnectionInfo.UserID = un;
                logonInfo.ConnectionInfo.Password = pw;
                logonInfo.ConnectionInfo.IntegratedSecurity = false;
                table.ApplyLogOnInfo(logonInfo);
            }

            var lgu = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault().Description;

            rpt.SetParameterValue("@cPoNo", string.IsNullOrWhiteSpace(poNo) ? null : poNo);
            rpt.SetParameterValue("LGU", lgu);
            rpt.SetParameterValue("IsOriginal", originalSw == 1);

            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");
        }

        public ActionResult _PoTransfer(Guid psCardItemId)
        {
            var model = new GetPsNoVM()
            {
                PsCardItemId = psCardItemId
            };

            return PartialView(model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _PoTransferSave(GetPsNoVM model)
        {
            string errorKey = "";
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _stockCardService.TransferPo(model.PsCardItemId, model.Id, user, date);
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

            var errorList = ModelState.Where(ms => ms.Value.Errors.Any())
                       .Select(ms => new
                       {
                           Key = ms.Key, // The field name
                           Message = ms.Value.Errors.Select(e =>
                           {
                               var errorMessage = e.ErrorMessage;
                               if (e.Exception != null)
                               {
                                   var exceptionMessage = e.Exception.Message;
                                   var innerExceptionMessage = e.Exception.InnerException?.Message;

                                   // Append exception details
                                   errorMessage += $" Exception: {exceptionMessage}";
                                   if (innerExceptionMessage != null)
                                   {
                                       errorMessage += $" InnerException: {innerExceptionMessage}";
                                   }
                               }

                               return errorMessage;
                           }).ToList() // List of messages for the current field
                       })
                       .ToList();

            if (errorList.Any())
            {
                return Json(new { Errors = errorList }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Errors = "", Id = model.Id }, JsonRequestBehavior.AllowGet);
        }
    }
}