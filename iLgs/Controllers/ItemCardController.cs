using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Items;
using iLgs.Services.ParIcs;
using iLgs.Services.PoIssuance;
using iLgs.Services.PropertyCard;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using static iLgs.Models.Enums;

namespace iLgs.Controllers
{
    [AppAuthorize("ITEMCARD")]
    public class ItemCardController : BaseController
    {
        private readonly AppManEntities _db;
        private readonly IIcsParService _icsParService;
        private readonly IPsCardService _psCardService;
        private readonly IParIcsUploadService _uploadService;
        private readonly IItemCodeService _itemCodeService;
        private readonly IPoIssuanceService _poIssuanceService;        

        public ItemCardController()
        {
            _db = new AppManEntities();
            _icsParService = new IcsParService(_db);
            _psCardService = new PsCardService(_db);
            _uploadService = new ParIcsUploadService(_db);
            _itemCodeService = new ItemCodeService(_db);
            _poIssuanceService = new PoIssuanceService(_db);
        }

        public ActionResult Supplies()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)AccountGroup.SUPPLIES;
            ViewBag.RefType = "I";
            ViewBag.Title = "Supplies Item Card";

            return View("Index");
        }

        public ActionResult Equipment()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)AccountGroup.PPE;
            ViewBag.RefType = "P";
            ViewBag.Title = "Equipment Item Card";

            return View("Index");
        }

        public ActionResult Vehicle()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)AccountGroup.VEHICLE;
            ViewBag.RefType = "P";
            ViewBag.Title = "Vehicles Item Card";

            return View("Index");
        }

        public ActionResult Land()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)AccountGroup.LAND;
            ViewBag.RefType = "P";
            ViewBag.Title = "Land Item Card";

            return View("Index");
        }

        public ActionResult Structure()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)AccountGroup.BUILDING;
            ViewBag.RefType = "P";
            ViewBag.Title = "Structure Item Card";

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
            var data = _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetAll(accountGroup);
            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }
        
        public ActionResult _ItemOrder(Guid? psCardItemExtnId, int? accountGroup)
        {
            ViewData["psCardItemExtnId"] = psCardItemExtnId;
            if (accountGroup == (int?)AccountGroup.PPE || accountGroup == (int?)AccountGroup.SUPPLIES)
            {
                var model = _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnPpeEntry(psCardItemExtnId);
                ViewData["psCardItemId"] = model == null ? Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardPpeOrder", model);
            }
            if (accountGroup == (int?)AccountGroup.VEHICLE)
            {
                var model = _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnVehicleEntry(psCardItemExtnId);
                ViewData["psCardItemId"] = model == null ? Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardVehicleOrder", model);
            }
            if (accountGroup == (int?)AccountGroup.LAND)
            {
                var model = _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnLandEntry(psCardItemExtnId);
                ViewData["psCardItemId"] = model == null ? Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardLandOrder", model);
            }
            if (accountGroup == (int?)AccountGroup.BUILDING)
            {
                var model = _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnStructuresEntry(psCardItemExtnId);
                ViewData["psCardItemId"] = model == null ? Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardStructureOrder", model);
            }
            else
            {
                return PartialView();
            }
        }

        public ActionResult _ItemAccount(Guid? psCardItemExtnId, int? accountGroup)
        {
            ViewData["psCardItemExtnId"] = psCardItemExtnId;
            if (accountGroup == (int?)AccountGroup.PPE || accountGroup == (int?)AccountGroup.SUPPLIES)
            {
                var model = _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnPpeEntry(psCardItemExtnId);
                ViewData["psCardItemId"] = model == null ? Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardPpeAccount", model);
            }
            if (accountGroup == (int?)AccountGroup.VEHICLE)
            {
                var model = _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnVehicleEntry(psCardItemExtnId);
                ViewData["psCardItemId"] = model == null ? Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardVehicleAccount", model);
            }
            if (accountGroup == (int?)AccountGroup.LAND)
            {
                var model = _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnLandEntry(psCardItemExtnId);
                ViewData["psCardItemId"] = model == null ? Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardLandAccount", model);
            }
            if (accountGroup == (int?)AccountGroup.BUILDING)
            {
                var model = _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnStructuresEntry(psCardItemExtnId);
                ViewData["psCardItemId"] = model == null ? Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardStructureAccount", model);
            }
            else
            {
                return PartialView();
            }
        }

        public ActionResult _ItemCardEntry(Guid? psCardItemExtnId, int? accountGroup)
        {
            ViewData["psCardItemExtnId"] = psCardItemExtnId;
            if (accountGroup == (int?)AccountGroup.PPE || accountGroup == (int?)AccountGroup.SUPPLIES)
            {
                var model = _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnPpeEntry(psCardItemExtnId);                
                ViewData["psCardItemId"] = model == null ?  Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardPpeEntry", model);
            }
            if (accountGroup == (int?)AccountGroup.VEHICLE)
            {
                var model = _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnVehicleEntry(psCardItemExtnId);
                ViewData["psCardItemId"] = model == null ? Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardVehicleEntry", model);                
            }
            if (accountGroup == (int?)AccountGroup.LAND)
            {
                var model = _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnLandEntry(psCardItemExtnId);
                ViewData["psCardItemId"] = model == null ? Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardLandEntry", model);
            }
            if (accountGroup == (int?)AccountGroup.BUILDING)
            {
                var model = _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnStructuresEntry(psCardItemExtnId);
                ViewData["psCardItemId"] = model == null ? Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardStructureEntry", model);
            }
            else
            {
                return PartialView();
            }
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ItemCardPpeSave(PsCardItemExtnPpeEntryVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "item_card");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access Error", "Access Denied!");
                }

                
                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.UpdatePpeAsync(model, user, date);                    

                    return Json(new { Errors = "", Model = model });
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

            return Json(new { Errors = ModelState.Keys.SelectMany(k => ModelState[k].Errors).Select(m => m.ErrorMessage).ToArray() });
        }


        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ItemCardVehicleSave(PsCardItemExtnVehicleEntryVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "item_card");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access Error", "Access Denied!");
                }


                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.UpdateVehicleAsync(model, user, date);

                    return Json(new { Errors = "", Model = model });
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

            return Json(new { Errors = ModelState.Keys.SelectMany(k => ModelState[k].Errors).Select(m => m.ErrorMessage).ToArray() });
        }

        public ActionResult _VehicleRepair(Guid? psCardItemExtnVehicleId)
        {
            ViewData["psCardItemExtnVehicleId"] = psCardItemExtnVehicleId;
            return PartialView();
        }

        public ActionResult _VehicleRepairRead([DataSourceRequest] DataSourceRequest request, Guid? psCardItemExtnVehicleId)
        {
            var data = _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnVehicle.PsCardItemExtnVehicleRepair.GetAll(psCardItemExtnVehicleId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _VehicleRepairCreate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnVehicleRepair model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "vehicle_repair");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnVehicle.PsCardItemExtnVehicleRepair.CreateAsync(model, user, date);
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
        public async Task<ActionResult> _VehicleRepairUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnVehicleRepair model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "vehicle_repair");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnVehicle.PsCardItemExtnVehicleRepair.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> _VehicleRepairDestroy([DataSourceRequest]DataSourceRequest request, PsCardItemExtnVehicleRepair model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "vehicle_repair");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnVehicle.PsCardItemExtnVehicleRepair.DeleteAsync(model, user, date);
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

        public ActionResult _ParIcs(Guid? psCardItemExtnId)
        {
            ViewData["psCardItemExtnId"] = psCardItemExtnId;            
            return PartialView();
        }

        public ActionResult _ParIcsRead([DataSourceRequest] DataSourceRequest request, Guid? psCardItemExtnId)
        {
            var data = _icsParService.GetByPsCardItemExtnId(psCardItemExtnId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        public ActionResult _Images(Guid? imageId, string postedBy)
        {
            ViewData["imageId"] = imageId;
            ViewData["postedBy"] = postedBy;
            return PartialView();
        }


        public ActionResult _ImagesRead([DataSourceRequest] DataSourceRequest request, Guid imageId)
        {
            var data = _uploadService.GetAllByImageId(imageId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        public async Task<ActionResult> PreviewUpload(Guid id)
        {
            var fileResult = await _uploadService.GetUploadedFileAsync(id);
            if (fileResult != null)
            {
                return fileResult; // Return the file result directly
            }
            else
            {
                return HttpNotFound("File not found"); // Handle not found case
            }
        }

        public ActionResult _TransitRead([DataSourceRequest] DataSourceRequest request, Guid? psCardItemId)
        {
            var data = _poIssuanceService.GetById(psCardItemId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        public ActionResult _IssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? transferId)
        {
            var data = _psCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferIssuance.GetByTransferId(transferId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        #region ADDITIONAL COST
        [Authorize]
        public ActionResult _AddCost(Guid? psCardItemExtnId)
        {
            ViewData["psCardItemExtnId"] = psCardItemExtnId;
            ViewData["postedBy"] = null;
            return PartialView();
        }

        [Authorize]
        public ActionResult _AddCostRead([DataSourceRequest] DataSourceRequest request, Guid? psCardItemExtnId)
        {
            var data = _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnAddCost.GetAll(psCardItemExtnId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [Authorize]
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _AddCostCreate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnAddCost model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "item_card");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnAddCost.CreateAsync(model, user, date);
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

        [Authorize]
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _AddCostUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnAddCost model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "add_cost");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnAddCost.UpdateAsync(model, user, date);
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

        [Authorize]
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _AddCostDestroy([DataSourceRequest]DataSourceRequest request, PsCardItemExtnAddCost model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "add_cost");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnAddCost.DeleteAsync(model, user, date);
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
        #endregion

        #region AJAX CALLS
        [Authorize]
        [HttpPost]
        public ActionResult GetAddCost(Guid? psCardItemExtnId)
        {
            var totalAddCost =  _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnAddCost.GetTotalAddCost(psCardItemExtnId);

            return Json(new { Errors = "", TotalAddCost = totalAddCost}, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public ActionResult GetItemExtnTemplate(Guid? psCarItemExtnid)
        {
            string itemExtnName = _psCardService.GetItemExtnNameByItmExtnId(psCarItemExtnid);

            return Json(new { Errors = "", ItemExtnName = itemExtnName }, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> LoadPpeFields([System.Web.Http.FromBody] PsCardItemExtnPpeEntryVM model)
        {
            if (model.Id != Guid.Empty)
            {
                var allField = await _psCardService.AllField.GetByItemExtnIdAsync(model.Id);
                if (allField != null)
                {
                    model.AllField = allField;
                }
            }
            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);

            if (!string.IsNullOrWhiteSpace(partialView))
            {
                partialView = $"_Ppe{partialView}";
            }

            return PartialView(partialView, model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> LoadSuppliesFields([System.Web.Http.FromBody] PsCardItemExtnSuppliesEntryVM model)
        {
            if (model.Id != Guid.Empty)
            {
                var allField = await _psCardService.AllField.GetByItemExtnIdAsync(model.Id);
                if (allField != null)
                {
                    model.AllField = allField;
                }
            }
            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);

            if (!string.IsNullOrWhiteSpace(partialView))
            {
                partialView = $"_Stock{partialView}";
            }

            return PartialView(partialView, model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> LoadVehicleFields([System.Web.Http.FromBody] PsCardItemExtnVehicleEntryVM model)
        {
            if (model.Id != Guid.Empty)
            {
                var allField = await _psCardService.AllField.GetByItemExtnIdAsync(model.Id);
                if (allField != null)
                {
                    model.AllField = allField;
                }
            }
            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);

            if (!string.IsNullOrWhiteSpace(partialView))
            {
                partialView = $"_Vehicle{partialView}";
            }

            return PartialView(partialView, model);
        }

        #endregion
    }
}