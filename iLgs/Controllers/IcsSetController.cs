using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.ParIcs;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [AppAuthorize("ICSSET")]
    public class IcsSetController : BaseController
    {
        private AppManEntities _db = new AppManEntities();
        private IIcsService _icsService;
        private ICodextnService _codextnService;
        
        public IcsSetController()
        {
            _icsService = new IcsService(_db);
            _codextnService = new CodextnService(_db);
        }

        // GET: Ics
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Read([DataSourceRequest] DataSourceRequest request)
        {
            var data = _icsService.GetAllPo();
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        //[AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> IcsSetRpt(string icsNo)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics");
                Access access = await accessTask;
                if (access == null)
                {
                    throw new Exception("Access Denied!");
                }

            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                return View("Error");
            }

            Sections crSections;
            ReportDocument rpt, crSubreportDocument;
            SubreportObject crSubreportObject;
            ReportObjects crReportObjects;
            ConnectionInfo crConnectionInfo;
            CrystalDecisions.CrystalReports.Engine.Database crDatabase;
            Tables crTables;
            TableLogOnInfo crTableLogOnInfo;
            rpt = new ReportDocument();
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/IcsSet.rpt"));
            rpt.Refresh();

            string user = ControllerContext.HttpContext.User.Identity.Name;
            string conString = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(conString);

            string un = decoder.UserID;
            string pw = decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            crDatabase = rpt.Database;
            crTables = crDatabase.Tables;
            crConnectionInfo = new ConnectionInfo();
            crConnectionInfo.ServerName = svr;
            crConnectionInfo.DatabaseName = db_;
            crConnectionInfo.UserID = un;
            crConnectionInfo.Password = pw;

            foreach (CrystalDecisions.CrystalReports.Engine.Table aTable in crTables)
            {
                crTableLogOnInfo = aTable.LogOnInfo;
                crTableLogOnInfo.ConnectionInfo = crConnectionInfo;
                aTable.ApplyLogOnInfo(crTableLogOnInfo);
            }
            // THIS STUFF HERE IS FOR REPORTS HAVING SUBREPORTS 
            // set the sections object to the current report's section 
            crSections = rpt.ReportDefinition.Sections;
            // loop through all the sections to find all the report objects 
            foreach (CrystalDecisions.CrystalReports.Engine.Section crSection in crSections)
            {
                crReportObjects = crSection.ReportObjects;
                //loop through all the report objects in there to find all subreports 
                foreach (ReportObject crReportObject in crReportObjects)
                {
                    if (crReportObject.Kind == ReportObjectKind.SubreportObject)
                    {
                        crSubreportObject = (SubreportObject)crReportObject;
                        //open the subreport object and logon as for the general report 
                        crSubreportDocument = crSubreportObject.OpenSubreport(crSubreportObject.SubreportName);
                        crDatabase = crSubreportDocument.Database;
                        crTables = crDatabase.Tables;
                        foreach (CrystalDecisions.CrystalReports.Engine.Table aTable in crTables)
                        {
                            crTableLogOnInfo = aTable.LogOnInfo;
                            crTableLogOnInfo.ConnectionInfo = crConnectionInfo;
                            aTable.ApplyLogOnInfo(crTableLogOnInfo);
                        }
                    }
                }
            }

            var lgu = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault().Description;

            rpt.SetParameterValue("@cRefNo", icsNo);
            rpt.SetParameterValue("@cRefType", "I");
            rpt.SetParameterValue("LGU", lgu);

            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");
        }


        #region PO ITEMS
        public ActionResult _PoItems(string poNo)
        {
            ViewData["PoNo"] = poNo;
            return PartialView();
        }

        public ActionResult _PoItemsRead([DataSourceRequest] DataSourceRequest request, string poNo, DateTime? poDate, Guid? deptId)
        {
            var data = _icsService.GetItemsByPoNo(poNo, poDate, deptId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
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

                    model = await _icsService.PsCardItem.UpdateNoICSAsync(model, user, date);
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

        public ActionResult _PoItemSetRead([DataSourceRequest] DataSourceRequest request, string poNo, DateTime? poDate, Guid? deptId)
        {
            var data = _icsService.GetItemSetsByPoNo(poNo, poDate, deptId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        public ActionResult _PoItemSetDescriptionRead([DataSourceRequest] DataSourceRequest request, Guid? unitGroupId)
        {
            var data = _icsService.GetItemSetDescriptionsByUnitGroupId(unitGroupId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        public ActionResult _PoItemSetDescriptionItemRead([DataSourceRequest] DataSourceRequest request, Guid? unitGroupDescriptionId)
        {
            var data = _icsService.GetItemSetDescriptionItemsByUnitGroupDescriptionId(unitGroupDescriptionId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _PoItemSetDescriptionItemUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemUnitGroupDescriptionItem model)
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

                    model = await _icsService.UpdateNoICSAsync(model, user, date);
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

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> PostIcs(string icsNo)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _icsService.PostAsync(icsNo, user, date);
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

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> UnpostIcs(string icsNo)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _icsService.UnPostAsync(icsNo, user, date);
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

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> PostPoItemSet(Guid? unitGroupId)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
        //        Access access = await accessTask;
        //        if (!access.AllowPost)
        //        {
        //            ModelState.AddModelError("Access", "Access Denied!");
        //        }

        //        if (ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            await _icsService.PostSetAsync(unitGroupId, user, date);
        //        }
        //    }
        //    catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
        //    {
        //        var errors = validationException.GetErrorsForModelState();
        //        foreach (var error in errors)
        //        {
        //            ModelState.AddModelError(error.Key, error.Message);
        //        }
        //    }
        //    catch (ValidationException validationException)
        //    {
        //        ModelState.AddModelError("", validationException.InnerException.Message);
        //    }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError("", e.Message);
        //    }

        //    var query = from state in ModelState.Values
        //                from error in state.Errors
        //                select error.ErrorMessage;

        //    var errorList = query.ToList();

        //    if (errorList.Count() > 0)
        //    {
        //        return Json(new { Errors = errorList }, JsonRequestBehavior.DenyGet);
        //    }

        //    return Json(new { Errors = "" }, JsonRequestBehavior.AllowGet);
        //}

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> UnpostPoItemSet(Guid? unitGroupId)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
        //        Access access = await accessTask;
        //        if (!access.AllowPost)
        //        {
        //            ModelState.AddModelError("Access", "Access Denied!");
        //        }

        //        if (ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            await _icsService.UnPostSetAsync(unitGroupId, user, date);
        //        }
        //    }
        //    catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
        //    {
        //        var errors = validationException.GetErrorsForModelState();
        //        foreach (var error in errors)
        //        {
        //            ModelState.AddModelError(error.Key, error.Message);
        //        }
        //    }
        //    catch (ValidationException validationException)
        //    {
        //        ModelState.AddModelError("", validationException.InnerException.Message);
        //    }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError("", e.Message);
        //    }

        //    var query = from state in ModelState.Values
        //                from error in state.Errors
        //                select error.ErrorMessage;

        //    var errorList = query.ToList();

        //    if (errorList.Count() > 0)
        //    {
        //        return Json(new { Errors = errorList }, JsonRequestBehavior.DenyGet);
        //    }

        //    return Json(new { Errors = "" }, JsonRequestBehavior.AllowGet);
        //}
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
            var data = _icsService.IcsParItem.GetAllIcsItems(cardItemGroupId);

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

                    model = await _icsService.IcsParItem.UpdateAsync(model, user, date);                    
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

                    model = await _icsService.IcsParItem.DeleteAsync(model, user, date);
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
            var data = await _icsService.IcsParItem.GetByIdAsync(parItemId);

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

                    model = await _icsService.IcsParItem.UpdateAsync(model, user, date);
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

        public async Task<ActionResult> _GenerateIcs(Guid? psCardItemId, string refType)
        {                        
            var psCardItem = await _icsService.GetByIdAsync(psCardItemId);
            var model = new GenerateIcsParVM()
            {
                PsCardItemId = psCardItemId,
                Qty = psCardItem.IcsBalance,
                Date = DateTime.Now,
                RefType = refType,
                IcsPar = new IcsPar(),
                IndSet = "I"
            };

            ViewData["psCardItemId"] = psCardItemId;
            ViewBag.ItemExtnName = _icsService.PsCard.GetItemExtnName(psCardItemId);

            return PartialView(model);
        }

        public ActionResult _GenerateIcsSet(Guid? unitGroupId, string refType)
        {
            var model = new GenerateIcsParVM()
            {
                UnitGroupId = unitGroupId,
                Date = DateTime.Now,
                RefType = refType,
                IcsPar = new IcsPar(),
                IndSet = "S"
            };

            ViewData["unitGroupId"] = unitGroupId;

            return PartialView(model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> GenerateIcs(GenerateIcsParVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    if (model.IndSet == "I")
                    {
                        await _icsService.GenerateIcs(model, user, date);
                    }
                    else
                    {
                        await _icsService.GenerateIcsSet(model, user, date);
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

        public ActionResult _GenerateIcsSelectionRead([DataSourceRequest] DataSourceRequest request, Guid? psCardItemId)
        {
            var data = _icsService.PsCardItemExtn.GetCardItemExtnForIcsParsByType(psCardItemId);

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
            var data = _icsService.PsCardItemExtn.GetCardItemExtnSetForIcsParByUnitGroupId(unitGroupId);

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
            var model = new GenerateIcsParVM()
            {
                PoNo = poNo,
                PoDate = poDate,
                DeptId = deptId,
                Date = DateTime.Now,
                RefType = "I",
                IcsPar = new IcsPar()                
            };

            return PartialView(model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> GenerateIcsBatch(GenerateIcsParVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _icsService.GenerateIcsBatch(model, user, date);
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
            var data = _icsService.IcsPar.GetAllIcs(cardItemGroupId);

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

                    var result = await _icsService.IcsPar.DeleteAsync(model, user, date);
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
            var data = await _icsService.IcsParItem.GetByIdAsync(parItemId);

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

                    model = await _icsService.IcsParItem.UpdateAsync(model, user, date);
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
        public ActionResult _Issuance(Guid? cardItemId)
        {
            ViewData["CardItemId"] = cardItemId;
            return PartialView();
        }

        public ActionResult _IssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? cardItemId)
        {
            var data = _icsService.PsCardItemIssaunce.GetByCardItemId(cardItemId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }
        #endregion


        public ActionResult GetAllPo(string text)
        {

            IQueryable<ParIcsPOGroupVM> model = null;

            if (string.IsNullOrEmpty(text))
            {
                model = _icsService.GetAllPoCombo().AsQueryable<ParIcsPOGroupVM>();
            }
            else
            {
                text = text.Trim();
                model = _icsService.GetAllPoCombo(text).AsQueryable<ParIcsPOGroupVM>();
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