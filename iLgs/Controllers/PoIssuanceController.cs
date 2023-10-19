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
    public class PoIssuanceController : Controller
    {
        private AppManEntities _db = new AppManEntities();
        private IPoIssuanceService _orderIssuanceService;
        private IRisIssuedService _risIssuedService;

        public PoIssuanceController()
        {
            _orderIssuanceService = new PoIssuanceService(_db);
            _risIssuedService = new RisIssuedService(_db);
        }

        // GET: PoIssuance
        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> IssuanceRead([DataSourceRequest] DataSourceRequest request)
        {
            var userId = User.Identity.GetUserId();
            var data = await _orderIssuanceService.GetAllPostedPoWithPostedAir(userId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        public ActionResult _RISIssued(Guid? orderItemId, Guid? risItemId, decimal? unitCost)
        {
            ViewData["OrderItemId"] = orderItemId;
            ViewData["RisItemId"] = risItemId;
            ViewData["UnitCost"] = unitCost;
            return PartialView();
        }

        public ActionResult _RISIssuedRead([DataSourceRequest] DataSourceRequest request, Guid? risItemId)
        {
            var data = _risIssuedService.GetByRisItemId(risItemId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _RISIssuedCreate([DataSourceRequest] DataSourceRequest request, RisIssuedVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Add Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _risIssuedService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> _RISIssuedUpdate([DataSourceRequest] DataSourceRequest request, RisIssuedVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _risIssuedService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> _RISIssuedDestroy([DataSourceRequest]DataSourceRequest request, RisIssuedVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _risIssuedService.DeleteAsync(model, user, date);
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

    }
}