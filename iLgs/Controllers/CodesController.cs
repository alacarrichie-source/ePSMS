using iLgs.Models;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using iLgs.Utilities;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using iLgs.Services.Interfaces;
using iLgs.Services;

namespace iLgs.Controllers
{
    [AppAuthorize("CODES")]
    public class CodesController : Controller
    {
        private AppManEntities _db = new AppManEntities();
        private ICodextnService _codextnService;
        private IDepartmentUserService _departmentUserService;
        private IAccountableOfficerService _accountableOfficerService;

        public CodesController()
        {
            _codextnService = new CodextnService(_db);
            _departmentUserService = new DepartmentUserService(_db);
            _accountableOfficerService = new AccountableOfficerService(_db);
        }
        // GET: Codes
        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> Location()
        {
            var code = "Locations";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Location of Items";
            return View(codeMast);
        }

        public async Task<ActionResult> Department()
        {
            var code = "Departments";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Department & Sections";
            return View(codeMast);
        }

        public async Task<ActionResult> IssuedBy()
        {
            var code = "ISSUED-BY";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Issued by";
            return View("Codextn", codeMast);
        }

        public async Task<ActionResult> RequestedBy()
        {
            var code = "REQUEST-BY";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Requested by";
            return View("Codextn", codeMast);
        }

        public async Task<ActionResult> ApprovedBy()
        {
            var code = "APPROVED-BY";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Approved by";
            return View("Codextn", codeMast);
        }

        public async Task<ActionResult> CashAvailability()
        {
            var code = "CASH-AVAILABLE";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Cash Availability";
            return View("Codextn", codeMast);
        }

        public async Task<ActionResult> PsFields()
        {
            var code = "PS-FIELDS";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Property & Supply Fields";
            return View("Codextn", codeMast);
        }

        public async Task<ActionResult> Custodians()
        {
            var code = "CUSTODIANS";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Supply/Property Custodians";
            return View("Codextn", codeMast);
        }

        public async Task<ActionResult> Officers()
        {
            var code = "OFFICERS";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Inspection Officer/Inspection Committee";
            return View("Codextn", codeMast);
        }

        public ActionResult _Codextn(Guid mastId)
        {
            ViewData["MastId"] = mastId;
            return PartialView();
        }

        public ActionResult Codextn()
        {
            return View();
        }

        public ActionResult CodeMastRead([DataSourceRequest] DataSourceRequest request)
        {
            var model = _db.CodeMasts.AsNoTracking();
            return Json(model.ToDataSourceResult(request));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> CodeMastCreate([DataSourceRequest] DataSourceRequest request, CodeMast model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "codes");
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
                    if (_db.CodeMasts.Any(a => a.Code == model.Code))
                    {
                        ModelState.AddModelError("Code", "Already Exists!");
                    }
                }

                if (model != null && ModelState.IsValid)
                {
                    model.Id = Guid.NewGuid();
                    model.InsertedBy = User.Identity.Name;
                    model.InsertedDt = DateTime.Now;
                    model.UpdatedBy = model.InsertedBy;
                    model.UpdatedDt = model.InsertedDt;

                    _db.CodeMasts.Add(model);
                    _db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);

            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));

        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> CodeMastUpdate([DataSourceRequest] DataSourceRequest request, CodeMast model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "codes");
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
                    var entity = _db.CodeMasts.Find(model.Id);

                    if (entity != null)
                    {
                        model.UpdatedBy = User.Identity.Name;
                        model.UpdatedDt = DateTime.Now;

                        entity.Description = model.Description;
                        entity.CodeHdg = model.CodeHdg;
                        entity.Desc1Hdg = model.Desc1Hdg;
                        entity.Desc2Hdg = model.Desc2Hdg;
                        entity.Desc3Hdg = model.Desc3Hdg;
                        entity.Desc4Hdg = model.Desc4Hdg;
                        entity.Desc5Hdg = model.Desc5Hdg;
                        entity.UpdatedBy = model.UpdatedBy;
                        entity.UpdatedDt = model.UpdatedDt;

                        _db.CodeMasts.Attach(entity);
                        _db.Entry(entity).State = EntityState.Modified;
                        _db.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);

            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> CodeMastDestroy([DataSourceRequest]DataSourceRequest request, CodeMast model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "codes");
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
                    // Attach the entity
                    _db.CodeMasts.Attach(model);
                    // Delete the entity
                    _db.CodeMasts.Remove(model);
                    // Or use DeleteObject if using a previous versoin of Entity Framework
                    // Delete the entity in the database
                    //db.Entry(model).State = System.Data.EntityState.Deleted;
                    _db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);

            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public ActionResult CodextnRead([DataSourceRequest] DataSourceRequest request, Guid mastId)
        {
            var data = _codextnService.GetByMastId(mastId);

            return Json(data.ToDataSourceResult(request));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> CodextnCreate([DataSourceRequest] DataSourceRequest request, CodextnVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "codes");
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

                    model = await _codextnService.CreateAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);

            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));

        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> CodextnUpdate([DataSourceRequest] DataSourceRequest request, CodextnVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "codes");
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
                    if (_db.Codextns.Any(a => a.MastId == model.MastId && a.Code == model.Code && a.Id != model.Id))
                    {
                        ModelState.AddModelError("Code", "Already Exists!");
                    }
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _codextnService.UpdateAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> CodextnDestroy([DataSourceRequest]DataSourceRequest request, CodextnVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "codes");
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

                    model = await _codextnService.DeleteAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
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
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "codes");
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
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }

            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));

        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _DepartmentUserUpdate([DataSourceRequest] DataSourceRequest request, DepartmentUserVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "codes");
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
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _DepartmentUserDestroy([DataSourceRequest]DataSourceRequest request, DepartmentUserVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "codes");
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
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("DeleteError", e.Message);
                }
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
            var data = _accountableOfficerService.GetAllByDeptId(deptId);

            return Json(data.ToDataSourceResult(request));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _AccountableOfficerCreate([DataSourceRequest] DataSourceRequest request, AccountableOfficerVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "codes");
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
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("AddError", e.Message);
                }

            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));

        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _AccountableOfficerUpdate([DataSourceRequest] DataSourceRequest request, AccountableOfficerVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "codes");
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
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("UpdateError", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _AccountableOfficerDestroy([DataSourceRequest]DataSourceRequest request, AccountableOfficerVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "codes");
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
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("DeleteError", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }
        #endregion  
    }
}