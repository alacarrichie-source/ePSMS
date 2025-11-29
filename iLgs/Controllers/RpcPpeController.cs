using CrystalDecisions.CrystalReports.Engine;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services;
using iLgs.Services.Codes;
using iLgs.Services.RPC;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using static iLgs.Models.Enums;

namespace iLgs.Controllers
{
    [AppAuthorize("RPCPPE")]
    public class RpcPpeController : BaseController
    {
        private readonly AppManEntities _db;
        private readonly IRpcPpeService _rpcService;
        private readonly IRpcPpeItemService _rpcItemService;
        private readonly ICodextnService _codextnService;
        private readonly IUserService _userService;

        public RpcPpeController(AppManEntities db, IRpcPpeService rpcPpeService, IRpcPpeItemService rpcPpeItemService,
            ICodextnService codextnService, IUserService userService)
        {
            _db = db;
            _rpcService = rpcPpeService;
            _rpcItemService = rpcPpeItemService;
            _codextnService = codextnService;
            _userService = userService;
        }


        public ActionResult Equipment()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)AccountGroup.PPE;
            ViewBag.Title = "Report on the Physical Count of Equipments";
            return View("Index");
        }

        public ActionResult Vehicle()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)AccountGroup.VEHICLE;
            ViewBag.Title = "Report on the Physical Count of Vehicles";
            return View("Index");
        }

        public ActionResult Supplies()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)AccountGroup.SUPPLIES;
            ViewBag.Title = "Report on the Physical Count of Supplies (With ICS)";
            return View("Index");
        }

        public ActionResult Registry()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)AccountGroup.REGISTRY;
            ViewBag.Title = "Report on the Physical Count of Registry";
            return View("Index");
        }

        public ActionResult Land()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)AccountGroup.LAND;
            ViewBag.Title = "Report on the Physical Count of Land";
            return View("Index");
        }

        public ActionResult Structure()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)AccountGroup.BUILDING;
            ViewBag.Title = "Report on the Physical Count of Structures";
            return View("Index");
        }


        public ActionResult Index()
        {
            if (TempData["AllowIndexAccess"] == null || !(bool)TempData["AllowIndexAccess"])
            {
                ViewBag.Error = "Access Denied!";
                return View("Error"); // Or some other handling
            }
            return View();
        }


        public ActionResult Read([DataSourceRequest] DataSourceRequest request, int? accountGroup)
        {
            var data = _rpcService.GetAllByAccountGroup(accountGroup);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        public async Task<ActionResult> Create([DataSourceRequest] DataSourceRequest request, RpcPpe model)
        {
            try
            {
                var menuId = _rpcService.GetAccountGroupMenuId(model.AccountGroup);
                Task<Access> accessTask = Access(User.Identity.GetUserId(), menuId);
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Add Access Denied!");
                }


                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rpcService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> Update([DataSourceRequest] DataSourceRequest request, RpcPpe model)
        {
            try
            {
                var menuId = _rpcService.GetAccountGroupMenuId(model.AccountGroup);
                Task<Access> accessTask = Access(User.Identity.GetUserId(), menuId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rpcService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> Destroy([DataSourceRequest]DataSourceRequest request, RpcPpe model)
        {
            try
            {
                var menuId = _rpcService.GetAccountGroupMenuId(model.AccountGroup);
                Task<Access> accessTask = Access(User.Identity.GetUserId(), menuId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rpcService.DeleteAsync(model, user, date);
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

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Post(Guid? id, int? accountGroup)
        {
            try
            {
                var menuId = _rpcService.GetAccountGroupMenuId(accountGroup);
                Task<Access> accessTask = Access(User.Identity.GetUserId(), menuId);
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Add Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _rpcService.PostAsync((Guid)id, user, date);
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
        public async Task<ActionResult> UnPost(Guid? id, int? accountGroup)
        {
            try
            {
                var menuId = _rpcService.GetAccountGroupMenuId(accountGroup);
                Task<Access> accessTask = Access(User.Identity.GetUserId(), menuId);
                Access access = await accessTask;
                if (!access.AllowUnpost)
                {
                    ModelState.AddModelError("Access", "Add Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _rpcService.UnPostAsync((Guid)id, user, date);
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

        public ActionResult _Item(Guid? rpcPpeId, int? accountGroup)
        {
            ViewData["RpcPpeId"] = rpcPpeId;
            ViewBag.AccountGroup = accountGroup;
            return PartialView();
        }

        public ActionResult _ItemRead([DataSourceRequest] DataSourceRequest request, Guid? rpcPpeId, int? accountGroup)
        {
            var data = _rpcItemService.GetByRpcPpeId(rpcPpeId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        #region PRINTOUTS
        public ActionResult _Print(Guid? id, int? accountGroup)
        {
            var model = new RpcPrintVM()
            {
                Id = id,
                AccountGroup = accountGroup,
                Save = false
            };

            return PartialView(model);
        }

        public ActionResult RpcPpeRpt(Guid? id, int? accountGroup, bool save, string annex)
        {
            //var rpci = _db.RPCIs.Find(id);
            string stringname = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(stringname);
            string rptKey = ConfigurationManager.AppSettings["RptKey"];
            string un = decoder.UserID;
            string pw = rptKey; // decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;
            string title = "REPORT ON THE PHYSICAL COUNT OF ";
            string account = "";
            ReportClass rpt = new ReportClass();
            if (accountGroup == (int?)AccountGroup.PPE)
            {
                rpt.FileName = Server.MapPath(Url.Content("~/Reports/RpcPpeSupplies.rpt"));
                //rpt.FileName = Server.MapPath(Url.Content("~/Reports/RpcPpeEquipment.rpt"));
                account = "EQUIPMENT";
            }
            else if (accountGroup == (int?)AccountGroup.VEHICLE)
            {
                rpt.FileName = Server.MapPath(Url.Content("~/Reports/RpcPpeVehicles.rpt"));
                account = "VEHICLES";
            }
            else if (accountGroup == (int?)AccountGroup.SUPPLIES)
            {
                rpt.FileName = Server.MapPath(Url.Content("~/Reports/RpcPpeSupplies.rpt"));
                account = "SUPPLIES (WITH ICS)";
            }
            else if (accountGroup == (int?)AccountGroup.REGISTRY)
            {
                //rpt.FileName = Server.MapPath(Url.Content("~/Reports/RpcPpeRegistry.rpt"));
                rpt.FileName = Server.MapPath(Url.Content("~/Reports/RpcPpeSupplies.rpt"));
                account = "REGISTRY";
            }
            else if (accountGroup == (int?)AccountGroup.BUILDING)
            {
                rpt.FileName = Server.MapPath(Url.Content("~/Reports/RpcPpeStructures.rpt"));
                account = "STRUCTURES";
            }
            else if (accountGroup == (int?)AccountGroup.LAND)
            {
                rpt.FileName = Server.MapPath(Url.Content("~/Reports/RpcPpeLands.rpt"));
                account = "LAND";
            }

            title += account;

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
                logonInfo.ConnectionInfo.IntegratedSecurity = false;
                table.ApplyLogOnInfo(logonInfo);
            }

            var lgu = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault().Description;

            rpt.SetParameterValue("TITLE", title);
            rpt.SetParameterValue("@uRpcId", id.ToString());
            rpt.SetParameterValue("@cAnnex", annex == "on" ? "" : annex);
            
            if (save)
            {
                Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.Excel);
                rpt.Close();
                rpt.Dispose();
                return File(stream, "application/xlsx", $"RPC {account}.xls");
            }
            else
            {
                Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
                rpt.Close();
                rpt.Dispose();
                return File(stream, "application/pdf");                
            }
        }
        #endregion
    }
}