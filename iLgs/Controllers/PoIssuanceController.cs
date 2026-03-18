using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.ParIcs;
using iLgs.Services.PoIssuance;
using iLgs.Services.PropertyCard;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [AppAuthorize("POISSUANCE")]
    public class PoIssuanceController : BaseController
    {
        //private readonly AppManEntities _db;
        private readonly IPoIssuanceService _poIssuanceService;
        private readonly IPsCardService _psCardService;
        private readonly IIcsParItemService _icsParItemService;
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;
        private readonly IPoIssuanceUploadService _uploadService;

        public PoIssuanceController()
        {
            //_db = new AppManEntities();
            _poIssuanceService = new PoIssuanceService(_db);
            _psCardService = new PsCardService(_db);
            _icsParItemService = new IcsParItemService(_db);
            _psCardItemTransactionService = new PsCardItemTransactionService(_db);
            _uploadService = new PoIssuanceUploadService(_db);
        }

        //public PoIssuanceController(IPoIssuanceService poIssuanceService, IPsCardService psCardService, IPsCardItemService psCardItemService,
        //    IIcsParItemService icsParItemService, IPsCardItemTransactionService psCardItemTransactionService, IPoIssuanceUploadService poIssuanceUploadService)
        //{
        //    _poIssuanceService = poIssuanceService;
        //    _psCardService = psCardService;
        //    _psCardService.PsCardItem = psCardItemService;
        //    _icsParItemService = icsParItemService;
        //    _psCardItemTransactionService = psCardItemTransactionService;
        //    _uploadService = poIssuanceUploadService;
        //}

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
        
        public async Task<ActionResult> _Issuance(Guid? cardItemId, Guid? transferId, decimal? unitCost, Guid? deptId, Guid? locationId)
        {
            var model = await  _psCardService.PsCardItem.GetByTransferIdAsync(transferId);

            ViewData["CardItemId"] = cardItemId;
            ViewData["TransferId"] = transferId;
            ViewData["UnitCost"] = unitCost;
            ViewData["DeptId"] = deptId;
            ViewData["IsWithItemExtn"] = model.IsWithItemExtn;
            ViewData["ItemExtnName"] = _psCardService.GetItemExtnName(cardItemId);
            ViewData["DefaultLocation"] = locationId ?? deptId;
            ViewBag.DefaultLocation = locationId ?? deptId;
            return PartialView();
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

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _IssuanceCreate([DataSourceRequest] DataSourceRequest request, PsCardItemTransferIssuanceVM model)
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

                    model = await _psCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferIssuance.CreateAsync(model, user, date);                    
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
        public async Task<ActionResult> _IssuanceUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemTransferIssuanceVM model)
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

                    model = await _psCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferIssuance.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> _IssuanceDestroy([DataSourceRequest]DataSourceRequest request, PsCardItemTransferIssuanceVM model)
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

                    model = await _psCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferIssuance.DeleteAsync(model, user, date);
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

        #region ITEMEXTN
        public ActionResult _getItemExtn(Guid? cardItemId, Guid? transferId)
        {
            ViewData["CardItemId"] = cardItemId;
            ViewData["TransferId"] = transferId;
            var itemExtnName = _psCardService.GetItemExtnName(cardItemId);

            return PartialView($"_{itemExtnName}");
        }

        public ActionResult _ItemExtnOtherRead([DataSourceRequest] DataSourceRequest request, Guid? transferId)
        {
            //var data = _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnOther.GetByPsCardItemId(transferId);
            var data = _psCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferIssuance.GetCardItemExtnForIssuanceByType(transferId);
            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        public ActionResult _ItemExtnVehicleRead([DataSourceRequest] DataSourceRequest request, Guid? transferId)
        {
            //var data = _psCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnVehicle.GetByPsCardItemId(cardItemId);
            var data = _psCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferIssuance.GetCardItemExtnForIssuanceByType(transferId);
            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }
        
        public ActionResult _ItemExtnVehicleSelectionRead([DataSourceRequest] DataSourceRequest request, Guid? transferId)
        {
            var data = _psCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferIssuance.GetCardItemExtnForVehicleIssuanceSelection(transferId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        public ActionResult _ItemExtnOtherSelectionRead([DataSourceRequest] DataSourceRequest request, Guid? transferId)
        {
            var data = _psCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferIssuance.GetCardItemExtnForOtherIssuanceSelection(transferId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        #endregion

        #region TRANSFER
        //public async Task<ActionResult> _Transfer(Guid? cardItemId, Guid? groupId)
        //{            
        //    var model =  await _psCardService.PsCardItem.GetByTransferIdAsync(transferId);

        //    ViewData["CardItemId"] = cardItemId;
        //    ViewData["TransferId"] = transferId;
        //    ViewData["GroupId"] = groupId;
        //    ViewBag.ItemExtnName = _psCardService.GetItemExtnName(cardItemId);

        //    return PartialView(model);
        //}

        public async Task<ActionResult> _Transfer(Guid? cardItemId, Guid? transferId)
        {
            var model = await _psCardService.PsCardItem.PsCardItemTransfer.GetByIdAsync(transferId);
            model.TransferOut = null;
            model.TransDate = DateTime.Now;
            model.LocationId = null;
            ViewData["CardItemId"] = cardItemId;
            ViewData["TransferId"] = transferId;
            ViewBag.ItemExtnName = _psCardService.GetItemExtnName(cardItemId);

            return PartialView(model);
        }

        public ActionResult _TransferSelectionRead([DataSourceRequest] DataSourceRequest request, Guid? cardItemId, Guid? transferId)
        {
            //var data = _psCardService.PsCardItem.PsCardItemExtn.GetCardItemExtnForIssuanceByType(cardItemId);
            var data = _poIssuanceService.GetCardItemExtnForTransit(cardItemId, transferId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _TransferSave(PsCardItemTransferVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "issuance");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }                

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _poIssuanceService.TransferAsync(model, user, date);                    
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

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> PostIssuance(Guid psCardItemIssuanceId)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "issuance");
        //        Access access = await accessTask;
        //        if (!access.AllowPost)
        //        {
        //            ModelState.AddModelError("UpdateError", "Access Denied!");
        //        }

        //        if (ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            await _poIssuanceService.PostAsync(psCardItemIssuanceId, user, date);
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

        //[HttpPost]
        //public async Task<ActionResult> UnpostIssuance(Guid psCardItemIssuanceId)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "issuance");
        //        Access access = await accessTask;
        //        if (!access.AllowUnpost)
        //        {
        //            ModelState.AddModelError("UpdateError", "Access Denied!");
        //        }

        //        if (ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            await _poIssuanceService.UnpostAsync(psCardItemIssuanceId, user, date);
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

        #region ICS ITEMS
        public ActionResult _Ics(Guid? cardItemId, decimal? unitCost)
        {
            ViewData["CardItemId"] = cardItemId;
            ViewData["UnitCost"] = unitCost;
            return PartialView();
        }

        public ActionResult _IcsRead([DataSourceRequest] DataSourceRequest request, Guid? cardItemId)
        {
            var data = _icsParItemService.GetAllIcsItems(cardItemId);

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

                    model = await _icsParItemService.UpdateAsync(model, user, date);                    
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

                    model = await _icsParItemService.DeleteAsync(model, user, date);                    
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

        #endregion

        #region SUMMARY
        public ActionResult Summary()
        {
            return View();
        }

        public ActionResult SummaryRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = _poIssuanceService.GetSummary();

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }
        #endregion

        public async Task<JsonResult> IsSelected(Guid? psCardItemExtnId, Guid? refId)
        {

            var isSelected = await _psCardItemTransactionService.IsSelectedIssuanceAsync(psCardItemExtnId, refId, "ISSUANCE");
            return Json(new { Errors = "", IsSelected = isSelected }, JsonRequestBehavior.AllowGet);
        }

        #region UPLOADS
        public ActionResult _Images(Guid? imageId)
        {
            ViewData["imageId"] = imageId;
            return PartialView();
        }

        public ActionResult _ImagesAdd(Guid? imageId)
        {
            var model = new Models.Upload()
            {
                ImageId = imageId
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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "issuance");
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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "issuance");
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

        public async Task<ActionResult> _ImagesUpload(IEnumerable<HttpPostedFileBase> files, Models.Upload model, int? accountGroup)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "issuance");
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
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError("AddError", error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("AddError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("AddError", e.Message);
            }

            var errorList = ModelState.Values.SelectMany(v => v.Errors)
                                       .Select(e => e.ErrorMessage)
                                       .ToList();

            if (errorList.Any())
            {
                var errorMessage = string.Join("\n", errorList);
                return Content(errorMessage);
            }

            return Content("");
        }

        public ActionResult DownloadFile(string fileName)
        {
            try
            {
                // Call the service to get the file bytes
                byte[] fileBytes = _uploadService.DownloadFile(fileName);

                // Return the file as a download
                return File(fileBytes, MimeMapping.GetMimeMapping(fileName), fileName);
            }
            catch (FileNotFoundException ex)
            {
                // Handle file not found case
                return HttpNotFound(ex.Message);
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                return new HttpStatusCodeResult(500, "Error downloading file: " + ex.Message);
            }
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
        #endregion
    }
}