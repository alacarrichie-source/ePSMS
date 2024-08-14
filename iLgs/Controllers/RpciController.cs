using iLgs.Models;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Data.Entity;
using Microsoft.AspNet.Identity;
using iLgs.Utilities;
using Newtonsoft.Json;
using iLgs.Services.Interfaces;
using iLgs.Services;
using CrystalDecisions.Shared;
using CrystalDecisions.CrystalReports.Engine;
using System.Data.SqlClient;
using System.IO;
using System.Collections.Generic;
using iLgs.Exceptions;

namespace iLgs.Controllers
{
    [AppAuthorize("RPCI")]
    public class RpciController : BaseController
    {
        private AppManEntities _db = new AppManEntities();
        private IRpciService _rpciService;
        private IRpciItemService _rpciItemService;
        private ICodextnService _codextnService;

        public RpciController()
        {
            _rpciService = new RpciService(_db);
            _rpciItemService = new RpciItemService(_db);
            _codextnService = new CodextnService(_db);
        }

        // GET: Rpci
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult RpciRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = _rpciService.GetAll();

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> RpciCreate([DataSourceRequest] DataSourceRequest request, RPCI_VM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rpci");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Add Access Denied!");
                }

                
                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rpciService.GenerateAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> RpciUpdate([DataSourceRequest] DataSourceRequest request, RPCI_VM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rpci");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rpciService.UpdateAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> RpciDestroy([DataSourceRequest]DataSourceRequest request, RPCI_VM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rpci");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rpciService.DeleteAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("DeleteError", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> RpciPost(Guid? rpciId)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rpci");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Add Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _rpciService.PostAsync(rpciId, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }
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
        public async Task<ActionResult> RpciUnPost(Guid? rpciId)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rpci");
                Access access = await accessTask;
                if (!access.AllowUnpost)
                {
                    ModelState.AddModelError("Access", "Add Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _rpciService.UnPostAsync(rpciId, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }
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

        public ActionResult _RpciItem(Guid rpciId)
        {
            ViewData["rpciId"] = rpciId;
            return PartialView();
        }

        
        //public async Task<ActionResult> _RpciItemAddEdit(Guid orderId, Guid? orderItemId)
        //{
        //    var data = await _rpciItemService.GetByIdAsync(orderItemId);
        //    if (data == null)
        //    {
        //        data = new OrderItemVM()
        //        {
        //            Id = Guid.NewGuid(),
        //            OrderId = orderId,
        //            Mode = "A"
        //        };
        //    }
        //    else
        //    {
        //        data.Mode = "E";
        //    }
        //    ViewData["orderItemId"] = orderItemId;
        //    return PartialView(data);
        //}


        public ActionResult _RpciItemRead([DataSourceRequest] DataSourceRequest request, Guid? rpciId)
        {
            var data = _rpciItemService.GetVmByRpciId(rpciId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _RpciItemCreate([DataSourceRequest] DataSourceRequest request, RPCIItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rpci");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                
                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rpciItemService.CreateAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _RpciItemUpdate([DataSourceRequest] DataSourceRequest request, RPCIItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rpci");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Access Denied!");
                }
                
                if (ModelState.IsValid)
                {                    
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rpciItemService.UpdateAsync(model, user, date);                
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("UpdateError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("UpdateError", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _RpciItemDestroy([DataSourceRequest]DataSourceRequest request, RPCIItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rpci");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rpciItemService.DeleteAsync(model, user, date);
                    // TO DO: update stocks
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("DeleteError", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }


        #region PRINTOUTS

        //public ActionResult RpciXls(DateTime asOf, Guid? id)
        //{
        //    string fileName = "RPCI.XLSX";
        //    int row = 1;

        //    var rpci = _rpciService.GetRpciXls(asOf, id);

        //    using (XLWorkbook wb = new XLWorkbook())
        //    {

        //        //wb.Worksheets.Add(table); working if the whole table will transfer into excel

        //        IXLWorksheet ws = wb.AddWorksheet("Sheet1");
        //        ws.Row(row).Cell(1).SetValue("REPORT ON THE PHYSICAL COUNT OF INVENTORIES");
        //        //ws.Row(row).Cell(1).SetValue("FROM DONATION");

        //        row += 3;
                
        //        ws.Row(row).Cell(1).SetValue("Articles");
        //        ws.Row(row).Cell(2).SetValue("PO No.");
        //        ws.Row(row).Cell(3).SetValue("PO Date");
        //        ws.Row(row).Cell(4).SetValue("AIR No.");
        //        ws.Row(row).Cell(5).SetValue("AIR Date");
        //        ws.Row(row).Cell(6).SetValue("PO Unit Cost");
        //        ws.Row(row).Cell(7).SetValue("Unit of Measurement");
        //        ws.Row(row).Cell(8).SetValue("PO Owner");
        //        ws.Row(row).Cell(9).SetValue("In Balance");
        //        ws.Row(row).Cell(10).SetValue("Code");
        //        ws.Row(row).Cell(11).SetValue("Location");
        //        ws.Row(row).Cell(12).SetValue("Transfer In");
        //        ws.Row(row).Cell(13).SetValue("Total Balance");
        //        ws.Row(row).Cell(14).SetValue("Acquisition Cost");
        //        ws.Row(row).Cell(15).SetValue("Old Stock No.");
        //        ws.Row(row).Cell(16).SetValue("Stock No.");
        //        ws.Row(row).Cell(17).SetValue("Brand");
        //        ws.Row(row).Cell(18).SetValue("Model");
        //        ws.Row(row).Cell(19).SetValue("Serial No.");
        //        ws.Row(row).Cell(20).SetValue("Pinagtratrabahuhang Lugar  **");
        //        ws.Row(row).Cell(21).SetValue("Bene_UCT **");
        //        ws.Row(row).Cell(22).SetValue("Bene_4ps **");
        //        ws.Row(row).Cell(23).SetValue("Katutubo **");
        //        ws.Row(row).Cell(24).SetValue("Katutubo  Name **");
        //        ws.Row(row).Cell(25).SetValue("Bene_others **");
        //        ws.Row(row).Cell(26).SetValue("Others Name **");
        //        ws.Row(row).Cell(27).SetValue("Petsa ng Pagrehistro (mm/dd/yyyy)**");
        //        ws.Row(row).Cell(28).SetValue("Pangalan ng Punong Barangay **");
        //        ws.Row(row).Cell(29).SetValue("Pangalan ng LSWDO **");
        //        var templates = db.Database.SqlQuery<_ReportTemplates>("execute Rpt_Templates {0},{1}", BrgyId, Tranche).ToList();
        //        foreach (var f in templates)
        //        {
        //            row++;
        //            ws.Row(row).Cell(1).SetValue(f.Fld_RowIndicator);
        //            ws.Row(row).Cell(2).SetValue(f.Fld_BarCode);
        //            ws.Row(row).Cell(3).SetValue(f.Fld_LName);
        //            ws.Row(row).Cell(4).SetValue(f.Fld_FName);
        //            ws.Row(row).Cell(5).SetValue(f.Fld_MName);
        //            ws.Row(row).Cell(6).SetValue(f.Fld_EName);
        //            ws.Row(row).Cell(7).SetValue(f.Fld_FamilyOrder);
        //            ws.Row(row).Cell(8).SetValue(f.Fld_BirthDate);
        //            ws.Row(row).Cell(9).SetValue(f.Fld_Gender);
        //            ws.Row(row).Cell(10).SetValue(f.Fld_Work);
        //            ws.Row(row).Cell(11).SetValue(f.Fld_Sector);
        //            ws.Row(row).Cell(12).SetValue(f.Fld_HealthCondition);
        //            ws.Row(row).Cell(13).SetValue(f.Fld_BrgyCode);
        //            ws.Row(row).Cell(14).SetValue(f.Fld_Address);
        //            ws.Row(row).Cell(15).SetValue(f.Fld_Street);
        //            ws.Row(row).Cell(16).SetValue(f.Fld_Idtype);
        //            ws.Row(row).Cell(17).SetValue(f.Fld_IdNumber);
        //            ws.Row(row).Cell(18).SetValue(f.Fld_MonthlySalary);
        //            ws.Row(row).Cell(19).SetValue(f.Fld_ContactNos);
        //            ws.Row(row).Cell(20).SetValue(f.Fld_PlaceofWork);
        //            ws.Row(row).Cell(21).SetValue(f.Fld_UCTBenefit);
        //            ws.Row(row).Cell(22).SetValue(f.Fld_4PsBenefit);
        //            ws.Row(row).Cell(23).SetValue(f.Fld_EthnicBenefit);
        //            ws.Row(row).Cell(24).SetValue(f.Fld_GroupName);
        //            ws.Row(row).Cell(25).SetValue(f.Fld_Others);
        //            ws.Row(row).Cell(26).SetValue(f.Fld_OthersName);
        //            ws.Row(row).Cell(27).SetValue(f.Fld_DateRegistered);
        //            ws.Row(row).Cell(28).SetValue(f.Fld_BrgyCaptain);
        //            ws.Row(row).Cell(29).SetValue(f.Fld_DSWDOfficer);
        //        }


        //        using (MemoryStream stream = new MemoryStream())
        //        {
        //            wb.SaveAs(stream);
        //            //Return xlsx Excel File  
        //            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName + ".xlsx");
        //        }
        //    }

            
        //    return new JsonResult() { Data = 0 };
        //}

        

        public ActionResult RpciRpt(Guid? id)
        {
            //var rpci = _db.RPCIs.Find(id);
            string stringname = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(stringname);

            string un = decoder.UserID;
            string pw = decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            ReportClass rpt = new ReportClass();
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/Rpci.rpt"));
            rpt.Load();
            rpt.Refresh();

            rpt.SetDatabaseLogon(un, pw, svr, db_);
            foreach (Table table in rpt.Database.Tables)
            {
                var logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo.ServerName = svr;
                logonInfo.ConnectionInfo.DatabaseName = db_;
                logonInfo.ConnectionInfo.UserID = un;
                logonInfo.ConnectionInfo.Password = pw;
                table.ApplyLogOnInfo(logonInfo);
            }

            var lgu = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault().Description;

            rpt.SetParameterValue("LGU", lgu);
            rpt.SetParameterValue("@dAsOf", null);
            rpt.SetParameterValue("@uRpciId", id.ToString());

            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");

        }
        #endregion
    }
}