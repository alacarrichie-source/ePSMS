using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services;
using iLgs.Services.AccountCode_;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [AppAuthorize("ACCOUNTCODE")]
    public class AccountCodeController : BaseController
    {
        private readonly IAccountCodeService _accountCodeService;
        private readonly IUserService _userService;
        private string _menuId = string.Empty;

        public AccountCodeController()
        {
            _accountCodeService = new AccountCodeService(_db);
            _userService = new UserService(_db);
        }

        public async Task<ActionResult> Budget()
        {
            _menuId = "account_code_budget";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["account_code"] = _menuId;
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.Title = "Account Codes - Budget";
            ViewBag.Type = "B";

            return View("Index");
        }


        public async Task<ActionResult> Acctg()
        {
            _menuId = "account_code_acctg";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["account_code"] = _menuId;
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.Title = "Account Codes - Accounting";
            ViewBag.Type = "S";

            return View("Index");
        }

        // GET: Application
        public async Task<ActionResult> Index()
        {
            var userId = User.Identity.GetUserId();
            _menuId = "account_code";
            var access = await Access(userId, _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }
            TempData["account_code"] = _menuId;
            if (TempData["AllowIndexAccess"] == null || !(bool)TempData["AllowIndexAccess"])
            {
                ViewBag.Error = "Access Denied!";
                return View("Error"); // Or some other handling
            }

            ViewBag.IsAdmin = await _userService.IsAdminAsync(userId);
            return View();
        }

        public ActionResult Read([DataSourceRequest] DataSourceRequest request, string type)
        {
            var data = _accountCodeService.GetAllByType(type);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }
        
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Create([DataSourceRequest] DataSourceRequest request, AccountCodeVM model)
        {
            try
            {
                _menuId = TempData["account_code"]?.ToString();
                TempData.Keep("account_code");
                var access = await Access(User.Identity.GetUserId(), _menuId);
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _accountCodeService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> Update([DataSourceRequest] DataSourceRequest request, AccountCodeVM model)
        {
            try
            {
                _menuId = TempData["account_code"]?.ToString();
                TempData.Keep("account_code");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _accountCodeService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> Destroy([DataSourceRequest]DataSourceRequest request, AccountCodeVM model)
        {
            try
            {
                _menuId = TempData["account_code"]?.ToString();
                TempData.Keep("account_code");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _accountCodeService.DeleteAsync(model, user, date);
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
        
        public ActionResult ItemRead([DataSourceRequest] DataSourceRequest request, Guid? accountCodeId)
        {
            var data = _accountCodeService.AccountCodeItem.GetAllItemTypeAccount(accountCodeId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> ItemUpdate([DataSourceRequest] DataSourceRequest request, ItemTypeAccountVM model)
        {
            try
            {
                _menuId = TempData["account_code"]?.ToString();
                TempData.Keep("account_code");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _accountCodeService.AccountCodeItem.SaveAsync(model, user, date);
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
        public ActionResult Excel_Export_Save(string contentType, string base64, string fileName)
        {
            var fileContents = Convert.FromBase64String(base64);

            return File(fileContents, contentType, fileName);
        }
    }
}