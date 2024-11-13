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
    public class LocationController : BaseController
    {
        private readonly AppManEntities _db;
        private readonly ILocationService _locationService;
        private readonly IDepartmentUserService _departmentUserService;
        private readonly IAccountableOfficerService _accountableOfficerService;

        public LocationController()
        {
            _db = new AppManEntities();
            _locationService = new LocationService(_db);
            _departmentUserService = new DepartmentUserService(_db);
            _accountableOfficerService = new AccountableOfficerService(_db);
        }

        // GET: Location
        public async Task<ActionResult> Index()
        {
            var code = "Locations";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Locations";
            return View(codeMast);
        }
        public ActionResult Read([DataSourceRequest] DataSourceRequest request, Guid mastId)
        {
            var data = _locationService.GetByMastId(mastId);

            return Json(data.ToDataSourceResult(request));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Create([DataSourceRequest] DataSourceRequest request, CodextnVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "location");
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

                    model = await _locationService.CreateAsync(model, user, date);
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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "location");
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

                    model = await _locationService.UpdateAsync(model, user, date);
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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "location");
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

                    model = await _locationService.DeleteAsync(model, user, date);
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
        public ActionResult _DepartmentUsers(Guid deptId)
        {
            ViewData["DeptId"] = deptId;
            return PartialView();
        }

        public ActionResult _DepartmentUserRead([DataSourceRequest] DataSourceRequest request, Guid deptId)
        {
            var data = _departmentUserService.GetAllByDeptId(deptId);

            return Json(data.ToDataSourceResult(request));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _DepartmentUserCreate([DataSourceRequest] DataSourceRequest request, DepartmentUserVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "codes");
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

                    model = await _departmentUserService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> _DepartmentUserUpdate([DataSourceRequest] DataSourceRequest request, DepartmentUserVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "codes");
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

                    model = await _departmentUserService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> _DepartmentUserDestroy([DataSourceRequest]DataSourceRequest request, DepartmentUserVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "codes");
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

                    model = await _departmentUserService.DeleteAsync(model, user, date);
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

        #region ACCOUNTABLE OFFICER
        public ActionResult _AccountableOfficers(Guid deptId)
        {
            ViewData["DeptId"] = deptId;
            return PartialView();
        }

        public ActionResult _AccountableOfficerRead([DataSourceRequest] DataSourceRequest request, Guid deptId)
        {
            var data = _accountableOfficerService.GetAllByLocationId(deptId);

            return Json(data.ToDataSourceResult(request));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _AccountableOfficerCreate([DataSourceRequest] DataSourceRequest request, AccountableOfficerVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "codes");
                Access access = await accessTask;
                if (!access.IsAdmin)
                {
                    if (!(access.IsAllowed || access.Actions.Any(a => a.MenuAction.ActionCode == "ADD")))
                    {
                        ModelState.AddModelError("AddError", "Access Denied!");
                    }
                }


                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _accountableOfficerService.CreateAsync(model, user, date);
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
                ModelState.AddModelError("AddError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("AddEror", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));

        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _AccountableOfficerUpdate([DataSourceRequest] DataSourceRequest request, AccountableOfficerVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "codes");
                Access access = await accessTask;
                if (!access.IsAdmin)
                {
                    if (!(access.IsAllowed || access.Actions.Any(a => a.MenuAction.ActionCode == "EDIT")))
                    {
                        ModelState.AddModelError("UpdateError", "Access Denied!");
                    }
                }


                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _accountableOfficerService.UpdateAsync(model, user, date);
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

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _AccountableOfficerDestroy([DataSourceRequest]DataSourceRequest request, AccountableOfficerVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "codes");
                Access access = await accessTask;
                if (!access.IsAdmin)
                {
                    if (!(access.IsAllowed || access.Actions.Any(a => a.MenuAction.ActionCode == "DELETE")))
                    {
                        ModelState.AddModelError("DeleteError", "Access Denied!");
                    }
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _accountableOfficerService.DeleteAsync(model, user, date);
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

        public async Task UpdateIndexNo()
        {
            string user = ControllerContext.HttpContext.User.Identity.Name;
            DateTime date = System.DateTime.Now;

            await _locationService.UpdateIndexNo(user, date);
        }
    }
}