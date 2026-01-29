using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Items;
using iLgs.Services.ParIcs;
using iLgs.Services.PoIssuance;
using iLgs.Services.PropertyCard;
using iLgs.Services.Uploads;
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
using static iLgs.Models.Enums;

namespace iLgs.Controllers
{
    [AppAuthorize("ITEMCARD")]
    public class ItemCardController : BaseController
    {
        private readonly AppManEntities _db;
        private readonly IIcsParService _icsParService;
        private readonly IPsCardService _psCardService;
        private readonly IAddCostUploadService _uploadService;
        private readonly IItemCodeService _itemCodeService;
        private readonly IPoIssuanceService _poIssuanceService;

        public ItemCardController(AppManEntities db, IIcsParService icsParService, IPsCardService psCardService, IAddCostUploadService addCostUploadService,
            IItemCodeService itemCodeService, IPoIssuanceService poIssuanceService)
        {
            _db = db;
            _icsParService = icsParService;
            _psCardService = psCardService;
            _uploadService = addCostUploadService;
            _itemCodeService = itemCodeService;
            _poIssuanceService = poIssuanceService;
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

        public async Task<ActionResult> _ItemOrder(Guid? psCardItemExtnId, int? accountGroup)
        {
            ViewData["psCardItemExtnId"] = psCardItemExtnId;
            if (accountGroup == (int?)AccountGroup.PPE || accountGroup == (int?)AccountGroup.SUPPLIES)
            {
                var model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnPpeEntryAsync(psCardItemExtnId);
                ViewData["psCardItemId"] = model == null ? Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardPpeOrder", model);
            }
            if (accountGroup == (int?)AccountGroup.VEHICLE)
            {
                var model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnVehicleEntryAsync(psCardItemExtnId);
                ViewData["psCardItemId"] = model == null ? Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardVehicleOrder", model);
            }
            if (accountGroup == (int?)AccountGroup.LAND)
            {
                var model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnLandEntryAsync(psCardItemExtnId);
                ViewData["psCardItemId"] = model == null ? Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardLandOrder", model);
            }
            if (accountGroup == (int?)AccountGroup.BUILDING)
            {
                var model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnStructuresEntryAsync(psCardItemExtnId);
                ViewData["psCardItemId"] = model == null ? Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardStructureOrder", model);
            }
            else
            {
                return PartialView();
            }
        }

        public async Task<ActionResult> _ItemAccount(Guid? psCardItemExtnId, int? accountGroup)
        {
            ViewData["psCardItemExtnId"] = psCardItemExtnId;
            if (accountGroup == (int?)AccountGroup.PPE || accountGroup == (int?)AccountGroup.SUPPLIES)
            {
                var model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnPpeEntryAsync(psCardItemExtnId);
                ViewData["psCardItemId"] = model == null ? Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardPpeAccount", model);
            }
            if (accountGroup == (int?)AccountGroup.VEHICLE)
            {
                var model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnVehicleEntryAsync(psCardItemExtnId);
                ViewData["psCardItemId"] = model == null ? Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardVehicleAccount", model);
            }
            if (accountGroup == (int?)AccountGroup.LAND)
            {
                var model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnLandEntryAsync(psCardItemExtnId);
                ViewData["psCardItemId"] = model == null ? Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardLandAccount", model);
            }
            if (accountGroup == (int?)AccountGroup.BUILDING)
            {
                var model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnStructuresEntryAsync(psCardItemExtnId);
                ViewData["psCardItemId"] = model == null ? Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardStructureAccount", model);
            }
            else
            {
                return PartialView();
            }
        }

        public async Task<ActionResult> _ItemCardEntry(Guid? psCardItemExtnId, int? accountGroup)
        {
            ViewData["psCardItemExtnId"] = psCardItemExtnId;
            if (accountGroup == (int?)AccountGroup.PPE || accountGroup == (int?)AccountGroup.SUPPLIES)
            {
                //var model = _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnPpeEntry(psCardItemExtnId);                
                var model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnOther.GetByIdAsync(psCardItemExtnId);
                ViewData["psCardItemId"] = model == null ? Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardPpeEntry", model);
            }
            if (accountGroup == (int?)AccountGroup.VEHICLE)
            {
                //var model = _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnVehicleEntry(psCardItemExtnId);
                var model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnVehicle.GetByIdAsync(psCardItemExtnId);
                ViewData["psCardItemId"] = model == null ? Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardVehicleEntry", model);
            }
            if (accountGroup == (int?)AccountGroup.LAND)
            {
                //var model = _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnLandEntry(psCardItemExtnId);
                var model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnLand.GetByIdAsync(psCardItemExtnId);
                ViewData["psCardItemId"] = model == null ? Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardLandEntry", model);
            }
            if (accountGroup == (int?)AccountGroup.BUILDING)
            {
                //var model = _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnStructuresEntry(psCardItemExtnId);
                var model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnBldg.GetByIdAsync(psCardItemExtnId);
                ViewData["psCardItemId"] = model == null ? Guid.Empty : model.PsCardItemId;
                return PartialView("_ItemCardStructureEntry", model);
            }
            else
            {
                return PartialView();
            }
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ItemCardPpeSave(PsCardItemExtnOtherVM model)
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

                    //model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.UpdatePpeAsync(model, user, date);                    
                    model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnOther.UpdateAsync(model, user, date);

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
        public async Task<ActionResult> _ItemCardVehicleSave(PsCardItemExtnVehicleVM model)
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

                    //model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.UpdateVehicleAsync(model, user, date);
                    model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnVehicle.UpdateAsync(model, user, date);

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
        public async Task<ActionResult> _ItemCardLandSave(PsCardItemExtnLandVM model)
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

                    //model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnUpdate.UpdateLandAsync(model, user, date);
                    model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnLand.UpdateAsync(model, user, date);

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
        public async Task<ActionResult> _ItemCardBldgSave(PsCardItemExtnBldgVM model)
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

                    model = await _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnBldg.UpdateAsync(model, user, date);

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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "item_card");
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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "item_card");
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

        public ActionResult _Images(Guid? imageId, string postedBy, string description)
        {
            ViewData["imageId"] = imageId;
            ViewData["postedBy"] = postedBy;
            ViewData["description"] = description;
            return PartialView();
        }

        public ActionResult _ImagesAdd(Guid? imageId, string description)
        {
            var model = new Models.Upload()
            {
                ImageId = imageId,
                Description = description
            };
            ViewData["imageId"] = imageId;
            ViewData["fileSize"] = model.FileSize;
            return PartialView(model);
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

        public async Task<ActionResult> _ImagesDestroy([DataSourceRequest]DataSourceRequest request, Models.Upload model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "item_card");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _uploadService.DeleteAsync(model, user, date);
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
        public async Task<ActionResult> _ImagesUpdate([DataSourceRequest] DataSourceRequest request, Models.Upload model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "item_card");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _uploadService.UpdateAsync(model, user, date);
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

        public async Task<ActionResult> _ImagesUpload(IEnumerable<HttpPostedFileBase> files, Models.Upload model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "item_card");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("AddError", "Upload Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _uploadService.UploadAsync(files, model, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var vErrors = validationException.GetErrorsForModelState();
                foreach (var error in vErrors)
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

            var errors = ModelState.Values.SelectMany(v => v.Errors)
                                       .Select(e => e.ErrorMessage)
                                       .ToList();

            if (errors.Any())
            {
                var errorMessage = string.Join("\n", errors);
                return Content(errorMessage);
            }

            return Content("");
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
        public async Task<ActionResult> GetAddCost(Guid? psCardItemExtnId)
        {
            var totalAddCost = _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnAddCost.GetTotalAddCost(psCardItemExtnId);
            var psCardItem = await _psCardService.PsCardItem.GetByItemExtnIdAsync(psCardItemExtnId);
            var unitCost = psCardItem == null ? 0 : psCardItem.UnitCost;

            return Json(new { Errors = "", TotalAddCost = totalAddCost, UnitCost = unitCost }, JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        [HttpPost]
        public ActionResult GetPoNo(string poNo)
        {
            var data = _db.PsCardItems.OrderByDescending(f => f.PoDate).FirstOrDefault(f => f.PoNo == poNo);

            if (data == null)
            {
                return Json(new { Errors = "Invalid PO No." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Errors = "", PoDate = data.PoDate }, JsonRequestBehavior.AllowGet);
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
            //var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            //string partialView = AllFieldsUtil.GetPartialView(itemCode);

            string partialView = await _itemCodeService.GetPartialViewAsync(model.ItemCodeId);

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
            //var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            //string partialView = AllFieldsUtil.GetPartialView(itemCode);

            string partialView = await _itemCodeService.GetPartialViewAsync(model.ItemCodeId);

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
            //var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            //string partialView = AllFieldsUtil.GetPartialView(itemCode);

            string partialView = await _itemCodeService.GetPartialViewAsync(model.ItemCodeId);

            if (!string.IsNullOrWhiteSpace(partialView))
            {
                partialView = $"_Vehicle{partialView}";
            }

            return PartialView(partialView, model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> LoadLandFields([System.Web.Http.FromBody] PsCardItemExtnLandEntryVM model)
        {
            if (model.Id != Guid.Empty)
            {
                var allField = await _psCardService.AllField.GetByItemExtnIdAsync(model.Id);
                if (allField != null)
                {
                    model.AllField = allField;
                }
            }
            //var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            //string partialView = AllFieldsUtil.GetPartialView(itemCode);

            string partialView = await _itemCodeService.GetPartialViewAsync(model.ItemCodeId);

            if (!string.IsNullOrWhiteSpace(partialView))
            {
                partialView = $"_Land{partialView}";
            }

            return PartialView(partialView, model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> LoadStructuresFields([System.Web.Http.FromBody] PsCardItemExtnStructuresEntryVM model)
        {
            if (model.Id != Guid.Empty)
            {
                var allField = await _psCardService.AllField.GetByItemExtnIdAsync(model.Id);
                if (allField != null)
                {
                    model.AllField = allField;
                }
            }
            //var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            //string partialView = AllFieldsUtil.GetPartialView(itemCode);

            string partialView = await _itemCodeService.GetPartialViewAsync(model.ItemCodeId);

            if (!string.IsNullOrWhiteSpace(partialView))
            {
                partialView = $"_Structure{partialView}";
            }

            return PartialView(partialView, model);
        }

        #endregion
    }
}