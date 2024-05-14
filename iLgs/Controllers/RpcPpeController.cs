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
    [AppAuthorize("RPCPPE")]
    public class RpcPpeController : BaseController
    {
        private AppManEntities _db = new AppManEntities();
        private IRpcPpeService _rpcService;
        private IRpcPpeItemService _rpcItemService;
        private ICodextnService _codextnService;

        public RpcPpeController()
        {
            _rpcService = new RpcPpeService(_db);
            _rpcItemService = new RpcPpeItemService(_db);
            _codextnService = new CodextnService(_db);
        }

        // GET: RpcPpe
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Read([DataSourceRequest] DataSourceRequest request)
        {
            var data = _rpcService.GetAll();

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Create([DataSourceRequest] DataSourceRequest request, RpcPpeVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rpcppe");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Add Access Denied!");
                }


                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    //model = await _rpcService.GenerateAsync(model, user, date);
                    model = await _rpcService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> Update([DataSourceRequest] DataSourceRequest request, RpcPpeVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rpcppe");
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
        public async Task<ActionResult> Destroy([DataSourceRequest]DataSourceRequest request, RpcPpeVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rpcppe");
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

        public ActionResult _Item(Guid? rpcId)
        {
            ViewData["rpcId"] = rpcId;
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


        public ActionResult _ItemRead([DataSourceRequest] DataSourceRequest request, Guid? rpcId)
        {
            var data = _rpcItemService.GetByRpcId(rpcId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ItemCreate([DataSourceRequest] DataSourceRequest request, RpcPpeItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rpcppe");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rpcItemService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> _ItemUpdate([DataSourceRequest] DataSourceRequest request, RpcPpeItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rpcppe");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rpcItemService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> _ItemDestroy([DataSourceRequest]DataSourceRequest request, RpcPpeItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rpcppe");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rpcItemService.DeleteAsync(model, user, date);
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
        //public ActionResult RpciRpt(DateTime? asAt)
        //{
        //    return RpciRpt(asAt, "");
        //}
        public ActionResult RpcRpt(DateTime? asOf, Guid? id)
        {
            //var rpci = _db.RPCIs.Find(id);
            string stringname = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(stringname);

            string un = decoder.UserID;
            string pw = decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            ReportClass rpt = new ReportClass();
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/RpcPpe.rpt"));
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

            //rpt.SetParameterValue("LGU", lgu);
            rpt.SetParameterValue("@dAsOf", asOf);
            rpt.SetParameterValue("@uRpcId", id == null ? null : id.ToString());

            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");

        }
        #endregion
    }
}