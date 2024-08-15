using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    public class GettersController : Controller
    {
        private AppManEntities db = new AppManEntities();
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

        public ActionResult GetDepartmentList(string text)
        {

            var model = db.Database.SqlQuery<GetDepartmentVM>("Select distinct case when Isnull(Department, '')  = '' then 'NONE' else Department end as Department From UserProfiles").AsQueryable();
            if (!string.IsNullOrEmpty(text))
            {
                text = text.Trim();
                model = model.Where(p => p.Department.Contains(text));
            }

            var retModel = model.Select(c => new GetDepartmentVM { Department = c.Department }).ToList();
            if (string.IsNullOrEmpty(text))
            {
                retModel.Insert(0, new GetDepartmentVM { Department = "ALL" });
            }

            return Json(retModel, JsonRequestBehavior.AllowGet);

        }

        public ActionResult GetSignatories(string department, string text)
        {
            department = string.IsNullOrWhiteSpace(department) ? "" : department.Trim();
            var model = db.AccountableOfficers.Where(w => w.Codextn.CodeMast.Code == "DEPARTMENTS" && w.Codextn.Description == department && w.LocationId == w.Codextn.Id).AsQueryable();
            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.Name.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, Name = c.Name, Designation = c.Designation }), JsonRequestBehavior.AllowGet);

        }

        public ActionResult GetAccountableOfficers(string department, string text)
        {
            department = string.IsNullOrWhiteSpace(department) ? "" : department.Trim();
            var model = db.AccountableOfficers.Where(w => w.Codextn.CodeMast.Code == "LOCATIONS" && w.Codextn.Description == department && w.LocationId == w.Codextn.Id).AsQueryable();
            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.Name.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, Name = c.Name, Designation = c.Designation }), JsonRequestBehavior.AllowGet);

        }

        public ActionResult GetAccountableOfficersByDeptId(Guid? deptId, string text)
        {
            var model = db.AccountableOfficers.Where(w => w.LocationId == deptId).AsQueryable();
            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.Name.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, Name = c.Name, Designation = c.Designation }), JsonRequestBehavior.AllowGet);

        }

        public ActionResult GetUserNameList(string text)
        {

            var model = db.AspNetUsers.Include("UserProfiles").AsQueryable();
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

            var model = db.AspNetUsers.Include("UserProfiles").AsQueryable();
            if (!string.IsNullOrEmpty(text))
            {
                text = text.Trim();
                model = model.Where(p => p.UserName.Contains(text) || p.UserProfile.NameFull.Contains(text));
            }

            var retModel = model.Select(c => new { Id = c.Id, Email = c.Email, UserName = c.UserName, NameFull = c.UserProfile.NameFull }).ToList();            

            return Json(retModel, JsonRequestBehavior.AllowGet);

        }

        public ActionResult GetRoleList(string text)
        {

            var model = db.AspNetRoles.AsQueryable();
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

        public JsonResult GetCodes(string mastCode, string text)
        {

            var model = db.Codextns.Where(w => w.CodeMast.Code == mastCode);

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3 }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCodeList(string mastCode, bool addAll, string text)
        {

            var model = db.Codextns.Where(w => w.CodeMast.Code == mastCode).OrderBy(o => o.Code).AsQueryable();
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

            var model = db.Codextns.Where(w => w.CodeMast.Code == "PS-REMARKS").OrderByDescending(o => o.Desc2).AsQueryable();
            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text) || p.Code.Contains(text));
            }

            var retModel = model.Select(c => new GetCodeListVM { Id = c.Id, Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3 }).ToList();            

            return Json(retModel, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetSupplier(string text)
        {

            var model = db.Suppliers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(text))
            {                
                model = model.Where(p => p.Id.ToString() == text || p.Name.Contains(text) || p.BusinessName.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Name = c.Name, Address = c.Address, TIN = c.TIN }), JsonRequestBehavior.AllowGet);            
        }

        //public JsonResult GetPsCode(string text)
        //{

        //    var model = db.PsCodes.AsQueryable();

        //    if (!string.IsNullOrWhiteSpace(text))
        //    {
        //        model = model.Where(p => p.Id.ToString() == text || p.ItemName.Contains(text) || p.ItemDescription.Contains(text) || p.PsNo.Contains(text));
        //    }

        //    return Json(model.Select(c => new { Id = c.Id, Code = c.PsNo, Name = c.ItemName, Description = c.ItemDescription, Unit = c.UnitMeas, Type = c.PsType }), JsonRequestBehavior.AllowGet);
        //}

        public JsonResult GetPrNos(string text)
        {

            var model = db.Requests.Where(w => w.SubmittedBy != null).AsQueryable();

            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.PrNo.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, PrNo = c.PrNo, PrDate = c.PrDate, Department = c.RISs.Office }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPrNoWithRemainingItems(Guid? orderId, string text)
        {
            orderId = orderId ?? Guid.Empty;
            var model = db.Requests.Where(w => w.SubmittedBy != null).AsQueryable();
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

            return Json(model.Select(c => new { Id = c.Id, PrNo = c.PrNo, PrDate = c.PrDate, Department = c.RISs.Office }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetRisNos(string text)
        {

            var model = db.RISses.Where(w => w.PostedBy != null).AsQueryable();

            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.RisNo.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, RisNo = c.RisNo, RisDate = c.RisDate, Department = c.Office, Section = c.Division,
                Fund = c.Fund, Purpose = c.Purpose, FPP = c.FPP,
                ApprovedBy = c.ApprovedBy,
                ApprovedByDesignation = c.ApprovedByDesignation}), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetRisNosWithNoPr(Guid? prId, string text)
        {
            prId = prId ?? Guid.Empty;
            var model = db.RISses.Where(w => w.PostedBy != null).AsQueryable();
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

            var model = db.RequestItems.Where(w => w.PrId == prId).AsQueryable();

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
            var model = db.RequestItems.Where(w => w.PrId == prId);
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
            var model = db.Orders.AsQueryable();

            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.PoNo.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, PoNo = c.PoNo, PoDate = c.PoDate, Department = c.Request.RISs.Office, Supplier = c.Supplier.BusinessName }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPoNosWithoutPr(Guid? airId, string text)
        {
            airId = airId ?? Guid.Empty;
            var model = db.Orders.Where(w => w.PostedBy != null).AsQueryable();
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

            return Json(model.Select(c => new { Id = c.Id, PoNo = c.PoNo, PoDate = c.PoDate, Department = c.Request.RISs.Office, Supplier = c.Supplier.BusinessName }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetRisPoNos(string text)
        {

            var model = db.Orders.AsQueryable();

            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.PoNo.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, PoNo = c.PoNo, PoDate = c.PoDate, Department = c.Request.Department, Division = c.Request.Section, Fund = c.Request.Fund, FPP = c.Request.FPP }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPoItems(Guid orderId, string text)
        {

            var model = db.OrderItems.Where(w => w.OrderId == orderId).AsQueryable();

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

        //public JsonResult GetOrderItem(Guid orderId, string text)
        //{

        //    var model = db.OrderItems.Include("PsCodes").Where(w => w.OrderId == orderId).AsQueryable();

        //    if (!string.IsNullOrWhiteSpace(text))
        //    {
        //        model = model.Where(p => p.Id.ToString() == text || p.PsCode.ItemName.Contains(text) || p.PsCode.ItemDescription.Contains(text) || p.PsCode.PsNo.Contains(text));
        //    }

        //    return Json(model.Select(c => new { Id = c.Id, Code = c.PsCode.PsNo, Name = c.PsCode.ItemName, Description = c.PsCode.ItemDescription
        //        , Unit = c.PsCode.UnitMeas, Type = c.PsCode.PsType
        //        , Qty = c.Qty// - (c.IssuedItems.Sum(s => s.Qty) ?? 0)
        //        , UnitCost = c.UnitCost
        //        })//.Where(w => w.Qty > 0)
        //    , JsonRequestBehavior.AllowGet);
        //}

        public JsonResult GetDepartments(string text)
        {

            var model = db.Codextns.Where(w => w.CodeMast.Code == "DEPARTMENTS");
            
            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3, c.Desc4 }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetIssuedTo(string text)
        {

            var model = db.Codextns.Where(w => w.CodeMast.Code == "ISSUED-TO");

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3, c.Desc4 }).OrderBy(o => o.Code), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetLocations(string text)
        {

            var model = db.Codextns.Where(w => w.CodeMast.Code == "LOCATIONS");

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3, c.Desc4 }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetSections(string department, string text)
        {
            var deptCode = db.Codextns.Where(w => w.CodeMast.Code == "DEPARTMENTS" && w.Description == department).FirstOrDefault()?.Code.Trim() + "-";
            var model = db.Codextns.Where(w => w.CodeMast.Code == "DEPARTMENTS" && w.Code.StartsWith(deptCode));

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3 }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetRequestedBy(string department, string text)
        {
            var deptCode = db.Codextns.Where(w => w.CodeMast.Code == "DEPARTMENTS" && w.Description == department).FirstOrDefault()?.Code.Trim() + "-";
            var model = db.Codextns.Where(w => w.CodeMast.Code == "REQUEST-BY" && w.Code.StartsWith(deptCode));

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3 }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetReceivedBy(string department, string text)
        {
            var deptCode = db.Codextns.Where(w => w.CodeMast.Code == "DEPARTMENTS" && w.Description == department).FirstOrDefault()?.Code.Trim() + "-";
            var model = db.Codextns.Where(w => w.CodeMast.Code == "REQUEST-BY" && w.Code.StartsWith(deptCode));

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3 }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetApprovedBy(string text)
        {
            var model = db.Codextns.Where(w => w.CodeMast.Code == "APPROVED-BY");

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3 }), JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetCustodians(string text)
        {
            var model = db.Codextns.Where(w => w.CodeMast.Code == "CUSTODIANS");

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3 }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetOfficers(string text)
        {
            var model = db.Codextns.Where(w => w.CodeMast.Code == "OFFICERS");

            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text));
            }

            return Json(model.Select(c => new { Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3 }), JsonRequestBehavior.AllowGet);
        }
        
    }

    public class GetSysCodeVM
    {
        public string Code { get; set; }
        public string Description { get; set; }        
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