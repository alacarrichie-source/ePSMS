using iLgs.Models;
using iLgs.Services;
using iLgs.Services.Codes;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [Authorize]
    public class GettersController : BaseController
    {
        private readonly AppManEntities _db;
        private readonly ICodextnService _codextnService;
        private readonly ILocationService _locationService;
        private readonly ILocationBudgetService _locationBudgetService;
        private readonly IUserService _userService;

        public GettersController(AppManEntities db, 
            ICodextnService codextnService,
            ILocationService locationService,
            ILocationBudgetService locationBudgetService,
            IUserService userService)
        {
            _db = db;
            _codextnService = codextnService;
            _locationService = locationService;
            _locationBudgetService = locationBudgetService;
            _userService = userService;
        }

        //public ActionResult GetSysCodeList(string text)
        //{


        //    var model = db.SysCodes.OrderBy(o => o.SysCode1).AsQueryable();
        //    if (!string.IsNullOrEmpty(text))
        //    {
        //        text = text.Trim();
        //        model = model.Where(p => p.SysCode1 == text || p.SysDescription.Contains(text) || p.SysCode1.Contains(text));
        //    }

        //    var retModel = model.Select(c => new GetSysCodeVM { Code = c.SysCode1, Description = c.SysDescription }).ToList();
        //    if (string.IsNullOrEmpty(text))
        //    {
        //        retModel.Insert(0, new GetSysCodeVM { Code = "ALL", Description = "ALL" });
        //    }

        //    return Json(retModel, JsonRequestBehavior.AllowGet);

        //}

        public JsonResult GetGsoUsers(string text)
        {
            var model = _db.UserProfiles
                .Where(w => w.Department == "General Services Office")
                .AsNoTracking()
                .Select(c => new { UserId = c.UserId, NameFull = c.NameFull, UserName = c.AspNetUser.UserName })
                .AsQueryable();
            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.NameFull.Contains(text));
            }
            return Json(model, JsonRequestBehavior.AllowGet);

        }
        public ActionResult GetDepartmentList(string text)
        {

            var model = _db.Database.SqlQuery<GetDepartmentVM>("Select distinct case when Isnull(Department, '')  = '' then 'NONE' else Department end as Department From UserProfiles").AsQueryable();
            if (!string.IsNullOrEmpty(text))
            {
                text = text.Trim();
                model = model.Where(p => p.Department.Contains(text));
            }

            var retModel = model.Select(c => new GetDepartmentVM { Department = c.Department }).OrderBy(o => o.Department).ToList();
            if (string.IsNullOrEmpty(text))
            {
                retModel.Insert(0, new GetDepartmentVM { Department = "ALL" });
            }

            return Json(retModel, JsonRequestBehavior.AllowGet);

        }

        public ActionResult GetSignatories(string department, string text)
        {
            department = string.IsNullOrWhiteSpace(department) ? "" : department.Trim();
            var model = _db.AccountableOfficers.Where(w => w.Codextn.CodeMast.Code == "DEPARTMENTS" && w.Codextn.Description == department && w.LocationId == w.Codextn.Id).AsQueryable();
            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.Name.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, Name = c.Name, Designation = c.Designation }), JsonRequestBehavior.AllowGet);

        }

        public ActionResult GetAccountableOfficers(string department, string text)
        {
            department = string.IsNullOrWhiteSpace(department) ? "" : department.Trim();
            var model = _db.AccountableOfficers.Where(w => w.Codextn.CodeMast.Code == "LOCATIONS" && w.Codextn.Description == department && w.LocationId == w.Codextn.Id).AsQueryable();
            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.Name.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, Name = c.Name, Designation = c.Designation }), JsonRequestBehavior.AllowGet);

        }

        public ActionResult GetAccountableOfficersByDeptId(Guid? deptId, string text)
        {
            var model = _db.AccountableOfficers.Where(w => w.LocationId == deptId).AsQueryable();
            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.Name.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, Name = c.Name, Designation = c.Designation }), JsonRequestBehavior.AllowGet);

        }

        public ActionResult GetUserNameList(string text)
        {

            var model = _db.AspNetUsers.Include("UserProfiles").AsQueryable();
            if (!string.IsNullOrEmpty(text))
            {
                text = text.Trim();
                model = model.Where(p => p.UserName.Contains(text) || p.UserProfile.NameFull.Contains(text));
            }

            var retModel = model.Select(c => new GetUserNameVM { UserName = c.UserName, NameFull = c.UserProfile.NameFull }).ToList();
            if (string.IsNullOrEmpty(text))
            {
                retModel.Insert(0, new GetUserNameVM { UserName = "ALL", NameFull = "ALL" });
            }

            return Json(retModel, JsonRequestBehavior.AllowGet);

        }

        public ActionResult GetDeptUserList(string text)
        {

            var model = _db.AspNetUsers.Include("UserProfiles").AsQueryable();
            if (!string.IsNullOrEmpty(text))
            {
                text = text.Trim();
                model = model.Where(p => p.UserName.Contains(text) || p.UserProfile.NameFull.Contains(text));
            }

            var retModel = model.Select(c => new { Id = c.Id, Email = c.Email, UserName = c.UserName, NameFull = c.UserProfile.NameFull }).ToList();            

            return Json(retModel, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> GetUserDepartmentsAsync(string text)
        {
            var userId = User.Identity.GetUserId();
            var model = await _codextnService.GetUserDepartmentsAsync(userId);
            if (!string.IsNullOrEmpty(text))
            {
                text = text.Trim();
                model = model.Where(p => p.Code.Contains(text) || p.Description.Contains(text));
            }

            //model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Description }).ToList();
            var retModel = model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3, c.Desc4 }).ToList();
            return Json(retModel, JsonRequestBehavior.AllowGet);

        }

        public async Task<ActionResult> GetUserDepartmentsWithAllAsync(string text)
        {
            var userId = User.Identity.GetUserId();
            List<Codextn> retModel;
            var model = await _codextnService.GetUserDepartmentsAsync(userId);
            if (!string.IsNullOrEmpty(text))
            {
                text = text.Trim();
                model = model.Where(p => p.Code.Contains(text) || p.Description.Contains(text));
            }

            retModel = model.ToList();            
            retModel.Insert(0, new Codextn { Id = Guid.Empty , Code = "ALL", Description = "ALL" });

            return Json(retModel.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3, c.Desc4 }), JsonRequestBehavior.AllowGet);

        }

        public ActionResult GetRoleList(string text)
        {

            var model = _db.AspNetRoles.AsQueryable();
            if (!string.IsNullOrEmpty(text))
            {
                text = text.Trim();
                model = model.Where(p => p.Id.Contains(text) || p.Name.Contains(text));
            }

            var retModel = model.Select(c => new GetUserRoleVM { RoleId = c.Id, RoleName = c.Name }).ToList();
            if (string.IsNullOrEmpty(text))
            {
                retModel.Insert(0, new GetUserRoleVM { RoleId = "ALL", RoleName = "ALL" });
            }

            return Json(retModel, JsonRequestBehavior.AllowGet);

        }

        public JsonResult GetUnits(string text)
        {

            var model = _db.Codextns.Where(w => w.CodeMast.Code == "UNIT").AsNoTracking();

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text) || p.Code.Contains(text) || p.Desc2.Contains(text) || p.Desc3.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Description, Desc2 = c.Desc2 ?? "", Desc3 = c.Desc3 ?? "" }).OrderBy(o => o.Desc2).ThenBy(t => t.Description), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCodes(string mastCode, string text)
        {

            var model = _db.Codextns.Where(w => w.CodeMast.Code == mastCode).AsNoTracking();

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text) || p.Code.Contains(text) || p.Desc2.Contains(text) || p.Desc3.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Description, Desc2 = c.Desc2 ?? "", Desc3 = c.Desc3 ?? ""}), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetRequiredFields(string text)
        {

            var model = _db.Codextns.Where(w => w.CodeMast.Code == "REQUIRED-FIELDS").AsNoTracking();

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text) || p.Code.Contains(text) || p.Desc2.Contains(text) || p.Desc3.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Description, Desc2 = c.Desc2 ?? "", Desc3 = c.Desc3 ?? "" }).OrderBy(o => o.Code), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetLocationBudget(string text)
        {

            var model = _db.Codextns.Where(w => w.CodeMast.Code == "BUDGET-CODE").AsNoTracking();

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text) || p.Code.Contains(text) || p.Desc2.Contains(text) || p.Desc3.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Description, Desc2 = c.Desc2 ?? "", Desc3 = c.Desc3 ?? "" }).OrderBy(o => o.Code), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCategory(string text)
        {

            var model = _db.Codextns.Where(w => w.CodeMast.Code == "PS-CATEGORY").AsNoTracking();            
            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text) || p.Code.Contains(text) || p.Desc2.Contains(text) || p.Desc3.Contains(text));
            }

            var retModel = model.Select(c => new GetCodeListVM { Id = c.Id, Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3 }).ToList();
            retModel.Insert(0, new GetCodeListVM { Id = Guid.Empty, Code = "ALL", Description = "ALL", Desc2 = "", Desc3 = "" });            

            return Json(retModel, JsonRequestBehavior.AllowGet);
        }

        public async Task<JsonResult> GetCategoryPreview(string text)
        {
            string userId = User.Identity.GetUserId();
            var admin = await GetUserInRole(userId, "admin");
            var sysadmin = await GetUserInRole(userId, sysAdmin);
            var model = _db.Codextns.Include(i => i.DepartmentUsers).Where(w => w.CodeMast.Code == "PS-CATEGORY").AsNoTracking();
            
            if (!admin && !sysadmin)
            {
                model = model.Where(w => w.DepartmentUsers.Any(i => i.UserId == userId)).AsNoTracking();
            }

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text) || p.Code.Contains(text) || p.Desc2.Contains(text) || p.Desc3.Contains(text));
            }

            var retModel = model.Select(c => new GetCodeListVM { Id = c.Id, Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3 }).ToList();
            retModel.Insert(0, new GetCodeListVM { Id = Guid.Empty, Code = "ALL", Description = "ALL", Desc2 = "", Desc3 = "" });

            return Json(retModel, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCodeList(string mastCode, bool addAll, string text)
        {

            var model = _db.Codextns.Where(w => w.CodeMast.Code == mastCode).AsNoTracking().OrderBy(o => o.Code).AsQueryable();
            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text) || p.Code.Contains(text));
            }

            var retModel = model.Select(c => new GetCodeListVM { Id = c.Id, Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3 }).ToList();
            if (addAll)
            {
                retModel.Insert(0, new GetCodeListVM { Id = Guid.Empty, Code = "ALL", Description = "ALL", Desc2 = "", Desc3 = "" });
            }

            return Json(retModel, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetInvDistList(string text)
        {

            var model = _db.Codextns.Where(w => w.CodeMast.Code == "PS-REMARKS").AsNoTracking().OrderByDescending(o => o.Desc2).AsQueryable();
            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text) || p.Code.Contains(text));
            }

            var retModel = model.Select(c => new GetCodeListVM { Id = c.Id, Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3 }).ToList();            

            return Json(retModel, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetSupplier(string text)
        {
            var model = _db.Database.SqlQuery<SupplierVM>("Exec Supplier_GetAll {0}", text).AsQueryable().Take(100);

            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Name = c.Name, BusinessName = c.BusinessName, Address = c.Address, TIN = c.TIN, ZipCode = c.ZipCode, Email = c.Email, ContactNo = c.ContactNo }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetEmployee(string text)
        {
            var model = _db.Database.SqlQuery<EmployeeVM>("Exec Employee_GetAll {0}", text).AsQueryable().Take(100);
            
            return Json(model.Select(c => new { Id = c.Id, Name = c.Name, Department = c.Department, Position = c.Position, Status = c.Status }), JsonRequestBehavior.AllowGet);
        }
        
        public JsonResult GetPrNos(string text)
        {

            var model = _db.Requests.AsNoTracking().Where(w => w.SubmittedBy != null).AsQueryable();

            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.PrNo.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, PrNo = c.PrNo, PrDate = c.PrDate, Department = c.RISs.Office }), JsonRequestBehavior.AllowGet);
        }

        public async Task<JsonResult> GetPrNoWithRemainingItems(Guid? orderId, string text)
        {
            orderId = orderId ?? Guid.Empty;
            var userId = User.Identity.GetUserId();
            var IsAdmin = await _userService.IsAdminAsync(userId);
            IQueryable<Request> model;
            if (IsAdmin)
            {
                model = _db.Requests.Where(w => w.SubmittedBy != null).AsNoTracking().AsQueryable();
            }
            else
            {
                model = _db.Requests.Where(w => w.RISs.Codextn.DepartmentUsers.Any(a => a.UserId == userId) && w.SubmittedBy != null).AsNoTracking().AsQueryable();
            }

            if (orderId == Guid.Empty)
            {
                model = model.Where(w => w.RequestItems.Any(a => !a.OrderItems.Any()));
            }
            else
            {
                model = model.Where(w => w.Orders.Any(a => a.Id == orderId) || (!w.Orders.Any(a => a.Id != orderId && w.RequestItems.Any(a2 => !a2.OrderItems.Any()))));
                //model = model.Where(w => w.Requests.Any(a => a.Id == prId) || !w.Requests.Any(a => a.Id != prId));
            }
            
            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.PrNo.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, PrNo = c.PrNo, PrDate = c.PrDate, Department = c.RISs.Office, ApprovedBy = c.ApprovedBy, ApprovedDesig = c.ApprovedDesig }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetFpp(string department, string text)
        {

            var model = _locationBudgetService.GetAll(department);

            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.BudgetCode.Contains(text) || p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.BudgetId, Code = c.BudgetCode, Description = c.Description, Fund = c.Fund }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetFppByDeptId(Guid? deptId, string text)
        {

            var model = _locationBudgetService.GetAll(deptId);

            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.BudgetCode.Contains(text) || p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.BudgetId, Code = c.BudgetCode, Description = c.Description, Fund = c.Fund }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetRisNos(string text)
        {

            var model = _db.RISses.Where(w => w.PostedBy != null).AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.RisNo.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, RisNo = c.RisNo, RisDate = c.RisDate, Department = c.Office, Section = c.Division,
                Fund = c.Fund, Purpose = c.Purpose, FPP = c.FPP,
                ApprovedBy = c.ApprovedBy,
                ApprovedByDesignation = c.ApprovedByDesignation}), JsonRequestBehavior.AllowGet);
        }
        
        public async Task<JsonResult> GetRisNosWithNoPr(Guid? prId, string text)
        {
            prId = prId ?? Guid.Empty;
            var userId = User.Identity.GetUserId();
            var IsAdmin = await _userService.IsAdminAsync(userId);
            IQueryable<RISs> model;
            if (IsAdmin)
            {
                model = _db.RISses.Where(w => w.PostedBy != null).AsNoTracking().AsQueryable();
            }
            else
            {
                model = _db.RISses.Where(w => w.Codextn.DepartmentUsers.Any(a => a.UserId == userId) && w.PostedBy != null).AsNoTracking().AsQueryable();
            }

            if (prId == Guid.Empty)
            {
                model = model.Where(w => !w.Requests.Any());
            }
            else
            {
                model = model.Where(w => w.Requests.Any(a => a.Id == prId) || !w.Requests.Any(a => a.Id != prId));
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.RisNo.Contains(text));
            }

            return Json(model.Select(c => new {
                Id = c.Id,
                RisNo = c.RisNo,
                RisDate = c.RisDate,
                Department = c.Office,
                Section = c.Division,
                Fund = c.Fund,
                Purpose = c.Purpose,
                FPP = c.FPP,
                ApprovedBy = c.ApprovedBy,
                ApprovedByDesignation = c.ApprovedByDesignation
            }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPrItems(Guid prId, string text)
        {

            var model = _db.RequestItems.Where(w => w.PrId == prId).AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.RisItem.ItemName.Contains(text) 
                    || p.RisItem.Description.Contains(text) || p.RisItem.PsNo.Contains(text));
            }

            return Json(model.Select(c => new
            {
                Id = c.Id,
                Code = c.RisItem.PsNo,
                Name = c.RisItem.ItemName,
                Description = c.RisItem.Description,
                Unit = c.RisItem.Unit,
                Type = c.RisItem.ItemCode.ItemType.Code,
                Qty = c.Qty, 
                UnitCost = c.UnitCost,
                TotalCost = c.TotalCost
            })
            , JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPrItemsWithNoPo(string mode, Guid prId, string text)
        {
            var model = _db.RequestItems.Where(w => w.PrId == prId).AsNoTracking();
            if (mode == "A")
            {
                model = model.Where(w => !w.OrderItems.Any());
            }
            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.RisItem.ItemName.Contains(text)
                    || p.RisItem.Description.Contains(text) || p.RisItem.PsNo.Contains(text));                    
            }

            return Json(model.Select(c => new
            {
                Id = c.Id,
                ItemCode = c.RisItem.ItemCode.Code,
                ItemType = c.RisItem.ItemCode.Description,
                ItemName = c.RisItem.ItemName,
                PsNo = c.RisItem.PsNo,
                Description = c.RisItem.Description,
                Unit = c.RisItem.Unit,                
                Qty = c.Qty,
                UnitCost = c.UnitCost,
                TotalCost = c.TotalCost,
                PsType = c.RisItem.ItemCode.ItemType.Code
            })
            , JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPoNos(string text)
        {
            var model = _db.Orders.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.PoNo.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, PoNo = c.PoNo, PoDate = c.PoDate, Department = c.Request.RISs.Office, Supplier = c.SupName }), JsonRequestBehavior.AllowGet);
        }

        public async Task<JsonResult> GetPoNosWithoutPr(Guid? airId, string text)
        {
            airId = airId ?? Guid.Empty;
            var userId = User.Identity.GetUserId();
            var IsAdmin = await _userService.IsAdminAsync(userId);
            IQueryable<Order> model;
            if (IsAdmin)
            {
                model = _db.Orders.Where(w => w.PostedBy != null).AsNoTracking().AsQueryable();
            }
            else
            {
                model = _db.Orders.Where(w => w.Request.RISs.Codextn.DepartmentUsers.Any(a => a.UserId == userId) && w.PostedBy != null).AsNoTracking().AsQueryable();
            }

            if (airId == Guid.Empty)
            {
                model = model.Where(w => !w.AIRs.Any());
            }
            else
            {
                model = model.Where(w => w.AIRs.Any(a => a.Id == airId) || !w.AIRs.Any(a => a.Id != airId));
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.PoNo.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, PoNo = c.PoNo, PoDate = c.PoDate, Department = c.Request.RISs.Office, Supplier = c.SupName }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetRisPoNos(string text)
        {

            var model = _db.Orders.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.PoNo.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, PoNo = c.PoNo, PoDate = c.PoDate, Department = c.Request.Department, Division = c.Request.Section, Fund = c.Request.Fund, FPP = c.Request.FPP }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPoItems(Guid orderId, string text)
        {

            var model = _db.OrderItems.Where(w => w.OrderId == orderId).AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.RequestItem.RisItem.ItemName.Contains(text) 
                    || p.RequestItem.RisItem.Description.Contains(text) || p.RequestItem.RisItem.PsNo.Contains(text) 
                    || p.Description.Contains(text));
            }

            return Json(model.Select(c => new
            {
                Id = c.Id,
                Code = c.RequestItem.RisItem.PsNo,
                Name = c.RequestItem.RisItem.ItemName,
                Description = c.Description,
                Unit = c.RequestItem.RisItem.Unit,
                Type = c.RequestItem.RisItem.ItemCode.ItemType.Code,
                Qty = c.Qty,
                UnitCost = c.UnitCost,
                Amount = c.Amount                
            })
            , JsonRequestBehavior.AllowGet);
        }
        
        public JsonResult GetDepartments(string text)
        {

            var model = _db.Codextns.Where(w => w.CodeMast.Code == "LOCATIONS" && w.Code.Substring(w.Code.Length-2) == "00").OrderBy(o => o.Description).AsNoTracking();

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text) || p.Code.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3, c.Desc4 }), JsonRequestBehavior.AllowGet);
        }

        public async Task<JsonResult> GetSections(Guid? deptId, string text)
        {
            if (deptId == null)
                return Json(Enumerable.Empty<object>(), JsonRequestBehavior.AllowGet);

            var deptCode = (await _codextnService.GetByIdAsync(deptId))?.Code;
            if (string.IsNullOrEmpty(deptCode) || deptCode.Length < 2)
                return Json(Enumerable.Empty<object>(), JsonRequestBehavior.AllowGet);

            var query = _db.Codextns
                .Where(w => w.CodeMast.Code == "LOCATIONS"
                    && !w.Code.EndsWith("00")
                    && w.Code.StartsWith(deptCode.Substring(0, 2)))
                .OrderBy(o => o.Description)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(text))
            {
                var lowerText = text.ToLower();
                query = query.Where(p => p.Description.ToLower().Contains(lowerText)
                                      || p.Code.ToLower().Contains(lowerText));
            }

            var result = await query
                .Select(c => new
                {
                    c.Id,
                    c.Code,
                    c.Description,
                    c.Desc2,
                    c.Desc3,
                    c.Desc4
                })
                .ToListAsync();

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetIssuedTo(string text)
        {

            var model = _db.Codextns.Where(w => w.CodeMast.Code == "ISSUED-TO").AsNoTracking();

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3, c.Desc4 }).OrderBy(o => o.Code), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetIssuedBy(string text)
        {

            var model = _db.Codextns.Where(w => w.CodeMast.Code == "ISSUED-BY").AsNoTracking();

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3, c.Desc4 }).OrderBy(o => o.Code), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetLocations(string text)
        {

            var model = _db.Codextns.Where(w => w.CodeMast.Code == "LOCATIONS").OrderBy(o => o.Desc4).AsNoTracking();

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text) || p.Code.Contains(text) || p.Desc4.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3, c.Desc4 }), JsonRequestBehavior.AllowGet);
        }        

        public JsonResult GetAccountExclusion(Guid? itemUserId, string category, string text)
        {

            var model = _db.ItemTypes.Where(w => w.Category == category
                //&& _db.Codextns.Where(x => x.Code == w.Category && x.CodeMast.Code == "PS-CATEGORY").Any())
                && !w.ItemTypeExclusions.Any(a => a.ItemUserId == itemUserId)); 

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text) || p.Code.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Description }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetRpciLocation(string text)
        {

            var model = _locationService.GetLocations(text);

            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Location, Desc2 = c.SubLocation, Desc3 = c.MainLocation }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetTransitLocation(string text)
        {

            var model = _locationService.GetLocations(text).Where(w => !w.Code.StartsWith("68") && w.MainLocation != null);

            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Location, Desc2 = c.SubLocation, Desc3 = c.MainLocation }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetLocationSp(string text)
        {

            var model = _locationService.GetLocations(text).Where(w => !w.Code.StartsWith("68"));

            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Location, Desc2 = c.SubLocation, Desc3 = c.MainLocation }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPsLocation(string text)
        {

            var model = _locationService.GetLocations(text).Where(w => !w.Code.StartsWith("68") && w.MainLocation != "");

            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Location, Desc2 = c.SubLocation, Desc3 = c.MainLocation }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetLandLocation(string text)
        {

            var model = _locationService.GetLocations(text);

            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Location, Desc2 = c.SubLocation, Desc3 = c.MainLocation }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetSections(string department, string text)
        {
            var deptCode = _db.Codextns.Where(w => w.CodeMast.Code == "DEPARTMENTS" && w.Description == department).FirstOrDefault()?.Code.Trim() + "-";
            var model = _db.Codextns.Where(w => w.CodeMast.Code == "DEPARTMENTS" && w.Code.StartsWith(deptCode)).AsNoTracking();

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3 }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetRequestedBy(string department, string text)
        {
            var deptCode = _db.Codextns.Where(w => w.CodeMast.Code == "DEPARTMENTS" && w.Description == department).FirstOrDefault()?.Code.Trim() + "-";
            var model = _db.Codextns.Where(w => w.CodeMast.Code == "REQUEST-BY" && w.Code.StartsWith(deptCode)).AsNoTracking();

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3 }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetReceivedBy(string department, string text)
        {
            var deptCode = _db.Codextns.Where(w => w.CodeMast.Code == "DEPARTMENTS" && w.Description == department).FirstOrDefault()?.Code.Trim() + "-";
            var model = _db.Codextns.Where(w => w.CodeMast.Code == "REQUEST-BY" && w.Code.StartsWith(deptCode)).AsNoTracking();

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3 }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetApprovedBy(string text)
        {
            var model = _db.Codextns.Where(w => w.CodeMast.Code == "APPROVED-BY").AsNoTracking();

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3 }), JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetCustodians(string text)
        {
            var model = _db.Codextns.Where(w => w.CodeMast.Code == "CUSTODIANS").AsNoTracking();

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3 }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetAirCustodians(string text)
        {
            var model = _db.Codextns.Where(w => w.CodeMast.Code == "AIR-CUSTODIANS").AsNoTracking();

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3 }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetOfficers(string text)
        {
            var model = _db.Codextns.Where(w => w.CodeMast.Code == "OFFICERS").AsNoTracking();

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3 }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetConditions(string text)
        {
            var model = _db.Codextns.Where(w => w.CodeMast.Code == "CONDITIONS").AsNoTracking();

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3 }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetConditionBldg(string text)
        {
            var model = _db.Codextns.Where(w => w.CodeMast.Code == "CONDITION-B").AsNoTracking();

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3 }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetRequriedFields(string part, string text)
        {
            var model = _codextnService.GetRequiredFields(part);

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Code = c.Code, Description = c.Description }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetUploadList(string text)
        {
            var model = _codextnService.GetUploadList();

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Code = c.Code, Description = c.Description }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetItemCodeRequestUploadList(string text)
        {
            var model = _codextnService.GetItemCodeRequestUploadList();

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Code = c.Code, Description = c.Description }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPsNos(string text)
        {
            var model = _db.Database.SqlQuery<GetPsNoVM>("Exec Card_GetPsNos '', {0}", text).AsQueryable().Take(100);
            
            return Json(model.Select(c => new {
                Id = c.Id,
                PsNo = c.PsNo,
                Fund = c.Fund, 
                Account = c.Account,
                SubAccount1 = c.SubAccount1,
                SubAccount2 = c.SubAccount2,
                SubAccount3 = c.SubAccount3,
                SubAccount4 = c.SubAccount4,
                Article = c.Article
            }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCustodianMainAccounts(int? accountGroup, string text)
        {
            List<CustodianAccountVM> model;

            var query = _db.CustodianReportItems
                .Where(w => w.CustodianReport.AccountGroup == accountGroup);

            if (!string.IsNullOrWhiteSpace(text))
            {
                query = query.Where(w => w.ItemCode.ItemType.Description.Contains(text));
            }

            model = query
                .GroupBy(g => new { g.ItemCode.ItemType.Id, g.ItemCode.ItemType.Description })
                .Select(s => new CustodianAccountVM { Id = s.Key.Id, MainAccount = s.Key.Description })
                .OrderBy(o => o.MainAccount)
                .ToList();

            // Insert "ALL" at the top
            model.Insert(0, new CustodianAccountVM { Id = Guid.Empty, MainAccount = "ALL" });

            return Json(model, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCustodianSubAccount1(int? accountGroup, Guid? mainAccount, string text)
        {
            var query = _db.Database.SqlQuery<ItemCodeVM>("Select ItemTypeId, Code, Article from dbo.fn_SubAccount1({0})", accountGroup).AsQueryable();
            
            query = query.Where(w => w.ItemTypeId == mainAccount);

            if (!string.IsNullOrWhiteSpace(text))
            {
                query = query.Where(w => w.Code.Contains(text) || w.Article.Contains(text));
            }
         
            return Json(query.Select(c => new { Code = c.Code, Description = c.Article }).OrderBy(o => o.Description), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCustodianSubAccount2(int? accountGroup, string subAccountCode, string text)
        {
            var query = _db.Database.SqlQuery<ItemCodeVM>("Select Code, Article from dbo.fn_SubAccount2({0})", accountGroup).AsQueryable();

            query = query.Where(w => w.Code != subAccountCode && w.Code.StartsWith(subAccountCode));

            if (!string.IsNullOrWhiteSpace(text))
            {
                query = query.Where(w => w.Code.Contains(text) || w.Article.Contains(text));
            }

            return Json(query.Select(c => new { Code = c.Code, Description = c.Article }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCustodianSubAccount3(int? accountGroup, string subAccountCode, string text)
        {
            var query = _db.Database.SqlQuery<ItemCodeVM>("Select Code, Article from dbo.fn_SubAccount3({0})", accountGroup).AsQueryable();

            query = query.Where(w => w.Code != subAccountCode && w.Code.StartsWith(subAccountCode));

            if (!string.IsNullOrWhiteSpace(text))
            {
                query = query.Where(w => w.Code.Contains(text) || w.Article.Contains(text));
            }

            return Json(query.Select(c => new { Code = c.Code, Description = c.Article }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCustodianSubAccount4(int? accountGroup, string subAccountCode, string text)
        {
            var query = _db.Database.SqlQuery<ItemCodeVM>("Select Code, Article from dbo.fn_SubAccount4({0})", accountGroup).AsQueryable();

            query = query.Where(w => w.Code != subAccountCode && w.Code.StartsWith(subAccountCode));

            if (!string.IsNullOrWhiteSpace(text))
            {
                query = query.Where(w => w.Code.Contains(text) || w.Article.Contains(text));
            }

            return Json(query.Select(c => new { Code = c.Code, Description = c.Article }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCurrentDate()
        {
            
            return Json(new { Date = DateTime.Now.ToShortDateString() }, JsonRequestBehavior.AllowGet);
        }
    }
    
    public class GetPsNoVM
    {
        public Guid Id { get; set; }
        public Guid? PsCardItemId { get; set; }
        public string Fund { get; set; }
        [Display(Name="Stock/Property No.")]
        public string PsNo { get; set; }
        public string Account { get; set; }
        [Display(Name = "Sub-Account 1")]
        public string SubAccount1 { get; set; }
        [Display(Name = "Sub-Account 2")]
        public string SubAccount2 { get; set; }
        [Display(Name = "Sub-Account 3")]
        public string SubAccount3 { get; set; }
        [Display(Name = "Sub-Account 4")]
        public string SubAccount4 { get; set; }
        public string Article { get; set; }
    }

    public class GetSysCodeVM
    {
        public string Code { get; set; }
        public string Description { get; set; }        
    }

    public class CustodianAccountVM
    {
        public Guid Id { get; set; }
        public string MainAccount { get; set; }
        public string MainDesc { get; set; }
    }

    public class GetDepartmentVM
    {
        public string Department { get; set; }        
    }

    public class GetUserNameVM
    {
        public string UserName { get; set; }
        public string NameFull { get; set; }
    }

    public class GetUserRoleVM
    {
        public string RoleId { get; set; }
        public string RoleName { get; set; }
    }

    public class GetCodeListVM
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string Desc2 { get; set; }
        public string Desc3 { get; set; }
    }
}