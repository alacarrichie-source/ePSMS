using iLgs.Exceptions.PARs;
using iLgs.Models;
using iLgs.Services;
using iLgs.Services.Interfaces;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [AppAuthorize("POISSUANCE")]
    public class PoIssuanceController : BaseController
    {
        private AppManEntities _db = new AppManEntities();
        private IPoIssuanceService _poIssuanceService;
        private IPsCardItemIssuanceService _psCardItemIssuanceService;
        //private IRisIssuedService _risIssuedService;        

        public PoIssuanceController()
        {
            _poIssuanceService = new PoIssuanceService(_db);
            _psCardItemIssuanceService = new PsCardItemIssuanceService(_db);
            //_risIssuedService = new RisIssuedService(_db);            
        }

        // GET: PoIssuance
        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> IssuanceRead([DataSourceRequest] DataSourceRequest request)
        {
            var userId = User.Identity.GetUserId();
            var data = await _poIssuanceService.GetAllAsync(userId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        public ActionResult _Issuance(Guid? orderItemId, Guid? risItemId, decimal? unitCost)
        {
            ViewData["OrderItemId"] = orderItemId;
            ViewData["RisItemId"] = risItemId;
            ViewData["UnitCost"] = unitCost;
            return PartialView();
        }

        public ActionResult _IssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? cardItemId)
        {
            var data = _psCardItemIssuanceService.GetByCardItemId(cardItemId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _IssuanceCreate([DataSourceRequest] DataSourceRequest request, PsCardItemIssuanceVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "issuance");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Add Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _psCardItemIssuanceService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> _IssuanceUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemIssuanceVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "issuance");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _psCardItemIssuanceService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> _IssuanceDestroy([DataSourceRequest]DataSourceRequest request, PsCardItemIssuanceVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "issuance");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _psCardItemIssuanceService.DeleteAsync(model, user, date);
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

        //public async Task<ActionResult> _GeneratePAR(Guid airItemId, Guid? orderItemId, string refType)
        //{
        //    ViewData["orderItemId"] = orderItemId;

        //    var data = await _poIssuanceService.GetOrderItemByAirItemIdAsync(airItemId);
        //    var model = new RisIssuedVM()
        //    {
        //        Id = airItemId,
        //        OrderItemId = orderItemId,
        //        Qty = data.Balance,
        //        RefDate = DateTime.Now,
        //        RefType = refType
        //    };

        //    return PartialView(model);
        //}

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> GeneratePAR(RisIssuedVM model)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "issuance");
        //        Access access = await accessTask;
        //        if (!access.AllowPost)
        //        {
        //            ModelState.AddModelError("Access", "Access Denied!");
        //        }

        //        if (ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            await _poIssuanceService.GeneratePAR(model, user, date);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        if (e.GetType().Name == "ServiceException")
        //        {
        //            ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
        //                 "please contact tech support with this message: " + e.Message);
        //        }
        //        else
        //        {
        //            ModelState.AddModelError("", e.Message);
        //        }
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

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> PostIssuance(Guid psCardItemIssuanceId)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "issuance");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("UpdateError", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _poIssuanceService.PostAsync(psCardItemIssuanceId, user, date);
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
                    ModelState.AddModelError("UpdateError", e.Message);
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

            return Json(new { Errors = "UpdateError" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> UnpostIssuance(Guid psCardItemIssuanceId)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "issuance");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("UpdateError", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _poIssuanceService.UnpostAsync(psCardItemIssuanceId, user, date);
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
    }
}