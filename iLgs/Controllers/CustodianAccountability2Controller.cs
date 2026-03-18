using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [AppAuthorize("CUSTODIANACCOUNTABILITY2")]
    public class CustodianAccountability2Controller : BaseController
    {
        //private readonly AppManEntities _db;
        private readonly ICustodianDeptUploadService _uploadService;
        private readonly IUserService _userService;

        public CustodianAccountability2Controller()
        {
            //_db = new AppManEntities();
            _uploadService = new CustodianDeptUploadService(_db).Create("PROCUREMENT");
            _userService = new UserService(_db);
        }

        //public CustodianAccountability2Controller(AppManEntities db,
        //    ICustodianDeptUploadService uploadService,
        //    IUserService userService)
        //{
        //    _db = db;
        //    _uploadService = uploadService.Create("PROCUREMENT");
        //    _userService = userService;
        //}

        public ActionResult Index()
        {
            string userName = ControllerContext.HttpContext.User.Identity.Name;
            if (_userService.IsUserNameAdmin(userName))
            {
                ViewBag.IsAdmin = true;
            }
            else
            {
                ViewBag.IsAdmin = false;
            }
            ViewBag.Title = "Custodian Accountability 2";
            return View();
        }

        public ActionResult _Images(string deptCode)
        {
            ViewBag.DeptCode = deptCode;
            return PartialView();
        }

        public ActionResult _ImagesAdd(string deptCode)
        {
            var model = new Models.Upload();

            ViewBag.DeptCode = deptCode;
            ViewData["fileSize"] = model.FileSize;
            return PartialView(model);
        }

        public ActionResult Read([DataSourceRequest] DataSourceRequest request, string deptCode)
        {
            var data = _uploadService.GetAllDeptUploads(deptCode);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        public async Task<ActionResult> Destroy([DataSourceRequest]DataSourceRequest request, Models.Upload model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_accountability_2");
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
        public async Task<ActionResult> Update([DataSourceRequest] DataSourceRequest request, Models.Upload model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_accountability_2");
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

        [HttpPost]
        public async Task<ActionResult> _ImagesUpload(IEnumerable<HttpPostedFileBase> files, Models.Upload model, string deptCode)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_accountability_2");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("AddError", "Upload Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _uploadService.UploadAsync(files, model, deptCode, user, date);
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

        public ActionResult DownloadAllFiles(string deptCode)
        {
            try
            {
                var uploads = _uploadService.GetAllDeptUploads(deptCode);
                if (!uploads.Any())
                {
                    throw new FileNotFoundException();
                }

                using (var memoryStream = new MemoryStream())
                {

                    // Create a ZIP archive in memory
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        foreach (var upload in uploads)
                        {
                            byte[] fileBytes = _uploadService.DownloadFile(upload.FileName);
                            if (fileBytes == null)
                                continue; // Skip if file not found

                            // Add each file into the ZIP
                            var zipEntry = archive.CreateEntry(upload.FileName, CompressionLevel.Optimal);
                            using (var entryStream = zipEntry.Open())
                            using (var fileStream = new MemoryStream(fileBytes))
                            {
                                fileStream.CopyTo(entryStream);
                            }
                        }
                    }

                    // Return the ZIP file
                    return File(memoryStream.ToArray(), "application/zip", $"{deptCode}-AllFiles.zip");
                }
            }
            catch (FileNotFoundException ex)
            {
                // Handle file not found case
                return HttpNotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, "Error downloading files: " + ex.Message);
            }
        }

        public async Task<ActionResult> PreviewUploadThumb(Guid id)
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

        public async Task<ActionResult> PreviewUploadX(Guid id)
        {
            var file = await _uploadService.GetUploadedFileAsync(id);

            if (file == null)
                return HttpNotFound("File not found");

            // Pass the file URL to the view
            string fileUrl = Url.Action("PreviewUploadThumb", "CustodianAccountability2", new { id });

            return View("PreviewUpload", model: fileUrl);
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

    }
}