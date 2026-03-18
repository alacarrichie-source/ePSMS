using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services;
using iLgs.Services.CustodianReports;
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
    [Authorize]
    public class CustodianUploadScanController : BaseController
    {
        //private readonly AppManEntities _db;
        private readonly ICustodianReportService _custodianReportService;
        private readonly ICustodianDeptUploadService _scanUploadService;
        
        public CustodianUploadScanController()
        {
            //_db = db;
            _custodianReportService = new CustodianReportService(_db);
            _scanUploadService = new CustodianDeptUploadService(_db).Create("SCAN");            
        }

        public ActionResult _Scanned(string deptCode, int? accountGroup, bool isAdmin)
        {
            ViewData["deptCode"] = deptCode;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.AccountGroup = accountGroup;
            return PartialView();
        }

        public ActionResult _ScannedScripts()
        {
            return PartialView();
        }

        public ActionResult _ScannedAdd(string deptCode, int? accountGroup)
        {
            var model = new Models.Upload();
            ViewData["deptCode"] = deptCode;
            ViewData["fileSize"] = model.FileSize;
            ViewBag.AccountGroup = accountGroup;
            return PartialView(model);
        }

        public ActionResult _ScannedRead([DataSourceRequest] DataSourceRequest request, string deptCode, int? accountGroup)
        {
            var data = _scanUploadService.GetAllDeptUploads($"{deptCode}-{accountGroup}");
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        public async Task<ActionResult> _ScannedDestroy([DataSourceRequest]DataSourceRequest request, Models.Upload model, int? accountGroup)
        {
            try
            {
                var menuId = _custodianReportService.GetAccountGroupMenuId(accountGroup);
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

                    model = await _scanUploadService.DeleteAsync(model, user, date);
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
        public async Task<ActionResult> _ScannedUpdate([DataSourceRequest] DataSourceRequest request, Models.Upload model, int? accountGroup)
        {
            try
            {
                var menuId = _custodianReportService.GetAccountGroupMenuId(accountGroup);
                Task<Access> accessTask = Access(User.Identity.GetUserId(), menuId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _scanUploadService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> _ScannedUpload(IEnumerable<HttpPostedFileBase> files, Models.Upload model, string deptCode, int? accountGroup)
        {
            try
            {
                var menuId = _custodianReportService.GetAccountGroupMenuId(accountGroup);
                Task<Access> accessTask = Access(User.Identity.GetUserId(), menuId);
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("AddError", "Upload Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _scanUploadService.UploadAsync(files, model, $"{deptCode}-{accountGroup}", user, date);
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


        public ActionResult ScannedDownloadFile(string fileName)
        {
            try
            {
                // Call the service to get the file bytes
                byte[] fileBytes = _scanUploadService.DownloadFile(fileName);

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

        public async Task<ActionResult> ScannedPreviewUpload(Guid id)
        {
            var fileResult = await _scanUploadService.GetUploadedFileAsync(id);
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