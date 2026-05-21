using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services;
using iLgs.Services.Codes;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [AppAuthorize("CODES")]
    public class CodesController : BaseController
    {
        //private AppManEntities _db;
        private ICodextnService _codextnService;
        private IDepartmentUserService _departmentUserService;
        private IAccountableOfficerService _accountableOfficerService;
        private ApplicationUserManager _userManager;
        private string _menuId = string.Empty;
        
        public CodesController()
        {
            //_db = db;            
            _codextnService = new CodextnService(_db);
            _departmentUserService = new DepartmentUserService(_db);
            _accountableOfficerService = new AccountableOfficerService(_db);        
        }


        //public ApplicationSignInManager SignInManager
        //{
        //    get
        //    {
        //        return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
        //    }
        //    private set
        //    {
        //        _signInManager = value;
        //    }
        //}

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        // GET: Codes
        public async Task<ActionResult> Index()
        {
            _menuId = "codes";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["codes"] = _menuId;
            return View();
        }

        public async Task<ActionResult> PriceCap()
        {
            _menuId = "codes_price_cap";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["codes"] = _menuId;
            var code = "PRICE-CAP";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Price Cap";
            return View("Codextn", codeMast);
        }

        public async Task<ActionResult> SemiExpendable()
        {
            _menuId = "codes_semi_expendable";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["codes"] = _menuId;
            var code = "SPHV";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Semi-Expendable Property Value";
            return View("Codextn", codeMast);
        }

        
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult> Issuance(string pin)
        {
            _menuId = "codes_issuance";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["codes"] = _menuId;
            var userId = User.Identity.GetUserId();
            var key = $"{userId}_Issuances";
            var code = "ISSUANCE-YEAR";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "PO Issuance Year";
            ViewBag.IsValid = false;
            ViewBag.UseOtp = false;            
                        
            return View("Codextn", codeMast);
        }

        public async Task<ActionResult> PoYear()
        {
            _menuId = "codes_po_year";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["codes"] = _menuId;
            var code = "PO-YEAR";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "PO-Encoding Year";

            return View("Codextn", codeMast);
        }

        public async Task<ActionResult> ReportingYearEnd()
        {
            _menuId = "codes_reporting_year_end";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["codes"] = _menuId;
            var code = "REPORT-YEAR-END";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Reporting Year-End";
            
            return View("Codextn", codeMast);
        }

        public async Task<ActionResult> Series()
        {
            _menuId = "codes_series";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["codes"] = _menuId;
            var code = "SERIES";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Last Series Numbers";
            return View("Codextn", codeMast);
        }

        public async Task<ActionResult> Unit()
        {
            _menuId = "codes_unit";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["codes"] = _menuId;
            var code = "Unit";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Unit";
            return View("Codextn", codeMast);
        }

        public async Task<ActionResult> UnitGroup()
        {
            _menuId = "codes_unit_group";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["codes"] = _menuId;
            var code = "UNIT-GROUP";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Unit Group";
            return View("Codextn" ,codeMast);
        }        

        public async Task<ActionResult> Department()
        {
            _menuId = "codes_department";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["codes"] = _menuId;
            var code = "Departments";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Departments";
            return View(codeMast);
        }

        public async Task<ActionResult> Location()
        {
            _menuId = "codes_location";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["codes"] = _menuId;
            var code = "Locations";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Locations";
            return View(codeMast);
        }

        public async Task<ActionResult> IssuedBy()
        {
            _menuId = "codes_issued_by";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["codes"] = _menuId;
            var code = "ISSUED-BY";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Issued by";
            return View("Codextn", codeMast);
        }

        public async Task<ActionResult> RequestedBy()
        {
            _menuId = "codes_requested_by";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["codes"] = _menuId;
            var code = "REQUEST-BY";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Requested by";
            return View("Codextn", codeMast);
        }

        public async Task<ActionResult> ApprovedBy()
        {
            _menuId = "codes_approved_by";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["codes"] = _menuId;
            var code = "APPROVED-BY";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Approved by";
            return View("Codextn", codeMast);
        }

        public async Task<ActionResult> CashAvailability()
        {
            _menuId = "codes_cash_availability";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["codes"] = _menuId;
            var code = "CASH-AVAILABLE";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Cash Availability";
            return View("Codextn", codeMast);
        }

        public async Task<ActionResult> PsFields()
        {
            _menuId = "codes_ps_fields";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["codes"] = _menuId;
            var code = "PS-FIELDS";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Property & Supply Fields";
            return View("Codextn", codeMast);
        }

        public async Task<ActionResult> Custodians()
        {
            _menuId = "codes_custodians";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["codes"] = _menuId;
            var code = "CUSTODIANS";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "Supply/Property Custodians";
            return View("Codextn", codeMast);
        }

        public async Task<ActionResult> AirOfficers()
        {
            _menuId = "codes_air_officers";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["codes"] = _menuId;
            var code = "OFFICERS";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "AIR Inspection Officer/Inspection Committee";
            return View("Codextn", codeMast);
        }

        public async Task<ActionResult> AirCustodians()
        {
            _menuId = "codes_air_custodians";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["codes"] = _menuId;
            var code = "AIR-CUSTODIANS";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "AIR Acceptance Custodians";
            return View("Codextn", codeMast);
        }

        public async Task<ActionResult> ParIssuedBy()
        {
            _menuId = "codes_par_issued_by";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["codes"] = _menuId;
            var code = "PAR-ISSUED-BY";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "PAR - Issued by";
            return View("Codextn", codeMast);
        }

        public async Task<ActionResult> IcsReceivedFrom()
        {
            _menuId = "codes_ics_received_from";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["codes"] = _menuId;
            var code = "ICS-RECEIVED-FROM";
            var codeMast = await _db.CodeMasts.Where(w => w.Code == code).FirstOrDefaultAsync();
            ViewData["code"] = code;
            ViewData["title"] = "ICS - Received from";
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
                _menuId = TempData["codes"]?.ToString();
                TempData.Keep("codes");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
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
                    await _db.SaveChangesAsync();
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
                _menuId = TempData["codes"]?.ToString();
                TempData.Keep("codes");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
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

                        entity.Code = model.Code;
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
                        await _db.SaveChangesAsync();
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
                _menuId = TempData["codes"]?.ToString();
                TempData.Keep("codes");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
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
                    await _db.SaveChangesAsync();
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
                _menuId = TempData["codes"]?.ToString();
                TempData.Keep("codes");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
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
        public async Task<ActionResult> CodextnUpdate([DataSourceRequest] DataSourceRequest request, CodextnVM model)
        {
            try
            {
                _menuId = TempData["codes"]?.ToString();
                TempData.Keep("codes");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
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
        public async Task<ActionResult> CodextnDestroy([DataSourceRequest]DataSourceRequest request, CodextnVM model)
        {
            try
            {
                _menuId = TempData["codes"]?.ToString();
                TempData.Keep("codes");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
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
                _menuId = TempData["codes"]?.ToString();
                TempData.Keep("codes");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
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
                _menuId = TempData["codes"]?.ToString();
                TempData.Keep("codes");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
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
                _menuId = TempData["codes"]?.ToString();
                TempData.Keep("codes");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
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
                _menuId = TempData["codes"]?.ToString();
                TempData.Keep("codes");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
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
                _menuId = TempData["codes"]?.ToString();
                TempData.Keep("codes");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
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
                _menuId = TempData["codes"]?.ToString();
                TempData.Keep("codes");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
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
    }
}