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
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [AppAuthorize("STOCKCARD", "PROPERTYCARD")]
    public class CardUploadController : BaseController
    {
        private readonly ICardUploadService _uploadService;

        public CardUploadController()
        {
            _uploadService = new CardUploadService(_db);
        }

        public ActionResult _Images(Guid? imageId, Guid? psCardItemId)
        {
            ViewData["imageId"] = imageId;
            ViewData["psCardItemId"] = psCardItemId;
            return PartialView();
        }

        public ActionResult _ImagesAdd(Guid? imageId, Guid? psCardItemId)
        {
            var model = new Models.Upload()
            {
                ImageId = imageId
            };
            ViewData["imageId"] = imageId;
            ViewData["psCardItemId"] = psCardItemId;
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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card", "property_card");
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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card", "property_card");
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
                    ModelState.AddModelError(error.Key, error.Message);
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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card", "property_card");
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
                ModelState.AddModelError("AddError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("AddError", e.Message);
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

        public int Count(string word)
        {
            //Simon rian T. Alacar
            //         i
            //01234567890123456789001234567890
            //0        1         2          34567890
            //nyny
            int count = 0;
            for (int i = 0; i < word.Length; i++)
            {
                string chr = word.Substring(i, 1);
                count += IsVowel(chr);                
            }
            return count;
        }

        public int IsVowel(string v)
        {
            string vowels = "aeiou";
            for (int i = 0; i < vowels.Length; i++)
            {
                if (vowels.Substring(i, 1) == v.ToLower())
                {
                    return 1;
                }                
            }
            return 0;
        }
    }
}