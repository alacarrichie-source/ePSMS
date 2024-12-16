using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.Uploads;
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
    public class ItemCodeRequestController : BaseController
    {
        private readonly AppManEntities _db;
        private readonly IItemCodeRequestService _itemCodeRequestService;
        private readonly IItemCodeRequestUploadService _uploadService;

        public ItemCodeRequestController()
        {
            _db = new AppManEntities();
            _itemCodeRequestService = new ItemCodeRequestService(_db);
            _uploadService = new ItemCodeRequestUploadService(_db);
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Read([DataSourceRequest] DataSourceRequest request)
        {
            string user = ControllerContext.HttpContext.User.Identity.Name;
            var data = _itemCodeRequestService.GetAllByUser(user);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Create([DataSourceRequest] DataSourceRequest request, ItemCodeRequestVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "item_code_request");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Add Access Denied!");
                }


                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _itemCodeRequestService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> Update([DataSourceRequest] DataSourceRequest request, ItemCodeRequestVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "item_code_request");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _itemCodeRequestService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> Destroy([DataSourceRequest]DataSourceRequest request, ItemCodeRequestVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "item_code_request");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _itemCodeRequestService.DeleteAsync(model, user, date);
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

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> Post(Guid id)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "item_code_request");
        //        Access access = await accessTask;
        //        if (!access.AllowPost)
        //        {
        //            ModelState.AddModelError("GridError", "Access Denied!");
        //        }
        //        else
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            await _itemCodeRequestService.PostAsync(id, user, date);
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
        //public async Task<ActionResult> UnPost(Guid id)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "item_code_request");
        //        Access access = await accessTask;
        //        if (!access.AllowUnpost)
        //        {
        //            ModelState.AddModelError("GridError", "Access Denied!");
        //        }
        //        else
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            await _itemCodeRequestService.UnPostAsync(id, user, date);
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

        public async Task<ActionResult> _ImagesDestroy([DataSourceRequest]DataSourceRequest request, Models.Upload model, int? accountGroup)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "item_code_request");
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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "item_code_request");
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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "item_code_request");
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