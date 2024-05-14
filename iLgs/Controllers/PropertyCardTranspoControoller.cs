//using CrystalDecisions.CrystalReports.Engine;
//using CrystalDecisions.Shared;
//using iLgs.Models;
//using iLgs.Services;
//using iLgs.Services.Interfaces;
//using iLgs.Utilities;
//using Kendo.Mvc.Extensions;
//using Kendo.Mvc.UI;
//using Microsoft.AspNet.Identity;
//using Newtonsoft.Json;
//using System;
//using System.Data.Entity;
//using System.Data.SqlClient;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Web.Mvc;

//namespace iLgs.Controllers
//{
//    [AppAuthorize("PROPERTYCARDTRANSPO")]
//    public class PropertyCardTranspoController : Controller
//    {
//        private AppManEntities _db = new AppManEntities();
//        private IPropertyCardVehicleService _cardService;
//        private IPropertyCardItemService _cardItemService;

//        public PropertyCardTranspoController()
//        {
//            _cardService = new PropertyCardVehicleService(_db);
//            _cardItemService = new PropertyCardItemService(_db);
//        }

//        // GET: Index
//        public ActionResult Index()
//        {
//            return View();
//        }

//        public ActionResult Read([DataSourceRequest] DataSourceRequest request)
//        {
//            var data = _cardService.GetAll();

//            var result = new JsonNetResult
//            {
//                Data = data.ToDataSourceResult(request),
//                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
//                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
//            };

//            return result;
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> Create([DataSourceRequest] DataSourceRequest request, PropertyCardVehicleVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "vehicle_card");
//                Access access = await accessTask;
//                if (!access.AllowAdd)
//                {
//                    ModelState.AddModelError("AddError", "Add Access Denied!");
//                }

//                if (model != null && ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    model = await _cardService.CreateAsync(model, user, date);
//                }
//            }
//            catch (Exception e)
//            {
//                if (e.GetType().Name == "ServiceException")
//                {
//                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
//                         "please contact tech support with this message: " + e.Message);
//                }
//                else
//                {
//                    ModelState.AddModelError("", e.Message);
//                }
//            }
//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> Update([DataSourceRequest] DataSourceRequest request, PropertyCardVehicleVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "vehicle_card");
//                Access access = await accessTask;
//                if (!access.AllowEdit)
//                {
//                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
//                }

//                if (ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    model = await _cardService.UpdateAsync(model, user, date);
//                }
//            }
//            catch (Exception e)
//            {
//                if (e.GetType().Name == "ServiceException")
//                {
//                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
//                         "please contact tech support with this message: " + e.Message);
//                }
//                else
//                {
//                    ModelState.AddModelError("", e.Message);
//                }
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> Destroy([DataSourceRequest]DataSourceRequest request, PropertyCardVehicleVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "vehicle_card");
//                Access access = await accessTask;
//                if (!access.AllowDelete)
//                {
//                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
//                }
//                else
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    model = await _cardService.DeleteAsync(model, user, date);
//                }
//            }
//            catch (Exception e)
//            {
//                if (e.GetType().Name == "ServiceException")
//                {
//                    ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
//                         "please contact tech support with this message: " + e.Message);
//                }
//                else
//                {
//                    ModelState.AddModelError("DeleteError", e.Message);
//                }
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        public async Task<ActionResult> _CardAddEdit(Guid? cardId)
//        {
//            var data = await _cardService.GetVmByIdAsync(cardId);
//            if (data == null)
//            {
//                data = new PropertyCardVehicleVM()
//                {
//                    Id = Guid.NewGuid()
//                };
//            }
//            ViewData["cardId"] = cardId;
//            return PartialView(data);
//        }

//        //public ActionResult _CardItemExtnBatchRead([DataSourceRequest] DataSourceRequest request, Guid? cardId, string psType)
//        //{
//        //    var data = _cardItemExtnService.GetBatchInfo(cardId, psType);
//        //    var result = new JsonNetResult
//        //    {
//        //        Data = data.ToDataSourceResult(request),
//        //        JsonRequestBehavior = JsonRequestBehavior.AllowGet,
//        //        Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
//        //    };

//        //    return result;
//        //}

//        [AcceptVerbs(HttpVerbs.Post)]
//        public JsonResult GetDescription(PropertyCardVehicleVM fields)
//        {
//            var description = _cardService.GetDescription(fields);

//            return Json(new { Description = description }, JsonRequestBehavior.AllowGet);
//        }

//        public ActionResult ItemRead([DataSourceRequest] DataSourceRequest request, Guid? cardId)
//        {
//            var data = _cardItemService.GetByCardId(cardId);

//            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> ItemCreate([DataSourceRequest] DataSourceRequest request, PropertyCardItemVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "vehicle_card");
//                Access access = await accessTask;
//                if (!access.AllowAdd)
//                {
//                    ModelState.AddModelError("Access", "Access Denied!");
//                }

//                if (model != null && ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    model = await _cardItemService.CreateAsync(model, user, date);
//                }
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
//                     "please contact tech support with this message: " + e.Message);
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> ItemUpdate([DataSourceRequest] DataSourceRequest request, PropertyCardItemVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "vehicle_card");
//                Access access = await accessTask;
//                if (!access.AllowEdit)
//                {
//                    ModelState.AddModelError("UpdateError", "Access Denied!");
//                }

//                if (ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    model = await _cardItemService.UpdateAsync(model, user, date);
//                }
//            }
//            catch (Exception e)
//            {
//                if (e.GetType().Name == "ServiceException")
//                {
//                    ModelState.AddModelError("UpdateError", "Unable to save changes, Try again, and if the problem persists " +
//                         "please contact tech support with this message: " + e.Message);
//                }
//                else
//                {
//                    ModelState.AddModelError("UpdateError", e.Message);
//                }
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> ItemDestroy([DataSourceRequest]DataSourceRequest request, PropertyCardItemVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "vehicle_card");
//                Access access = await accessTask;
//                if (!access.AllowDelete)
//                {
//                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
//                }

//                if (ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    model = await _cardItemService.DeleteAsync(model, user, date);
//                    // TO DO: update stocks
//                }
//            }
//            catch (Exception e)
//            {
//                if (e.GetType().Name == "ServiceException")
//                {
//                    ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
//                         "please contact tech support with this message: " + e.Message);
//                }
//                else
//                {
//                    ModelState.AddModelError("DeleteError", e.Message);
//                }
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//    }
//}