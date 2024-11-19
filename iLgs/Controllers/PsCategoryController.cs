using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services;
using iLgs.Services.Codes;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [AppAuthorize("PSCATEGORY")]
    public class PsCategoryController : BaseController
    {
        private readonly AppManEntities _db;
        private readonly IPsCategoryService _psCategoryService;
        private readonly IDepartmentUserService _userService;
        
        public PsCategoryController()
        {
            _db = new AppManEntities();
            _psCategoryService = new PsCategoryService(_db);
            _userService = new DepartmentUserService(_db);        
        }

        // GET: Location
        public async Task<ActionResult> Index()
        {
            var code = "PS-CATEGORY";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Property/Supply Category";
            return View(codeMast);
        }
        public ActionResult Read([DataSourceRequest] DataSourceRequest request, Guid mastId)
        {
            var data = _psCategoryService.GetByMastId(mastId);

            return Json(data.ToDataSourceResult(request));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Create([DataSourceRequest] DataSourceRequest request, CodextnVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ps_category");
                Access access = await accessTask;
                if (!access.IsAdmin)
                {
                    if (!(access.IsAllowed || access.Actions.Any(a => a.MenuAction.ActionCode == "ADD")))
                    {
                        ModelState.AddModelError("Access Error", "Access Denied!");
                    }
                }

                if (ModelState.IsValid)
                {
                    if (_db.Codextns.Any(a => a.MastId == model.MastId && a.Code == model.Code))
                    {
                        ModelState.AddModelError("Code", "Already Exists!");
                    }
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _psCategoryService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> Update([DataSourceRequest] DataSourceRequest request, CodextnVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ps_category");
                Access access = await accessTask;
                if (!access.IsAdmin)
                {
                    if (!(access.IsAllowed || access.Actions.Any(a => a.MenuAction.ActionCode == "EDIT")))
                    {
                        ModelState.AddModelError("Access Error", "Access Denied!");
                    }
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _psCategoryService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> Destroy([DataSourceRequest]DataSourceRequest request, CodextnVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ps_category");
                Access access = await accessTask;
                if (!access.IsAdmin)
                {
                    if (!(access.IsAllowed || access.Actions.Any(a => a.MenuAction.ActionCode == "DELETE")))
                    {
                        ModelState.AddModelError("Access Error", "Access Denied!");
                    }
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _psCategoryService.DeleteAsync(model, user, date);
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

        #region DEPARTMENT USERS
        public ActionResult _CategoryUsers(Guid deptId)
        {
            ViewData["DeptId"] = deptId;
            return PartialView();
        }

        public ActionResult _CategoryUserRead([DataSourceRequest] DataSourceRequest request, Guid deptId)
        {
            var data = _userService.GetAllByDeptId(deptId);

            return Json(data.ToDataSourceResult(request));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _CategoryUserCreate([DataSourceRequest] DataSourceRequest request, DepartmentUserVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ps_category");
                Access access = await accessTask;
                if (!access.IsAdmin)
                {
                    if (!(access.IsAllowed || access.Actions.Any(a => a.MenuAction.ActionCode == "ADD")))
                    {
                        ModelState.AddModelError("Access Error", "Access Denied!");
                    }
                }


                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _userService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> _CategoryUserUpdate([DataSourceRequest] DataSourceRequest request, DepartmentUserVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ps_category");
                Access access = await accessTask;
                if (!access.IsAdmin)
                {
                    if (!(access.IsAllowed || access.Actions.Any(a => a.MenuAction.ActionCode == "EDIT")))
                    {
                        ModelState.AddModelError("Access Error", "Access Denied!");
                    }
                }


                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _userService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> _CategorytUserDestroy([DataSourceRequest]DataSourceRequest request, DepartmentUserVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ps_category");
                Access access = await accessTask;
                if (!access.IsAdmin)
                {
                    if (!(access.IsAllowed || access.Actions.Any(a => a.MenuAction.ActionCode == "DELETE")))
                    {
                        ModelState.AddModelError("Access Error", "Access Denied!");
                    }
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _userService.DeleteAsync(model, user, date);
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
    }
}