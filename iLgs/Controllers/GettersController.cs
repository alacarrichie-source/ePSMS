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

            return Json(model.Select(c => new { Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3 }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCodeList(string mastCode, bool addAll, string text)
        {

            var model = db.Codextns.Where(w => w.CodeMast.Code == mastCode).OrderBy(o => o.Code).AsQueryable();
            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.Description.Contains(text) || p.Code.Contains(text));
            }

            var retModel = model.Select(c => new GetCodeListVM { Code = c.Code, Description = c.Description, Desc2 = c.Desc2, Desc3 = c.Desc3 }).ToList();
            if (addAll)
            {
                retModel.Insert(0, new GetCodeListVM { Code = "ALL", Description = "ALL", Desc2 = "", Desc3 = "" });
            }

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

        public JsonResult GetPsCode(string text)
        {

            var model = db.PsCodes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.ItemName.Contains(text) || p.ItemDescription.Contains(text) || p.PsNo.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, Code = c.PsNo, Name = c.ItemName, Description = c.ItemDescription, Unit = c.UnitMeas, Type = c.PsType }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPrNos(string text)
        {

            var model = db.Requests.AsQueryable();

            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.PrNo.Contains(text));
            }

            return Json(model.Select(c => new { Id = c.Id, PrNo = c.PrNo, PrDate = c.PrDate, Department = c.Department }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPrItems(Guid prId, string text)
        {

            var model = db.RequestItems.Include("PsCodes").Where(w => w.PrId == prId).AsQueryable();

            if (!string.IsNullOrWhiteSpace(text))
            {
                model = model.Where(p => p.Id.ToString() == text || p.PsCode.ItemName.Contains(text) || p.PsCode.ItemDescription.Contains(text) || p.PsCode.PsNo.Contains(text));
            }

            return Json(model.Select(c => new
            {
                Id = c.Id,
                Code = c.PsCode.PsNo,
                Name = c.PsCode.ItemName,
                Description = c.PsCode.ItemDescription,
                Unit = c.PsCode.UnitMeas,
                Type = c.PsCode.PsType,
                Qty = c.Qty, 
                UnitCost = c.UnitCost,
                TotalCost = c.TotalCost
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

            return Json(model.Select(c => new { Id = c.Id, PoNo = c.PoNo, PoDate = c.PoDate, Department = c.Request.Department, Supplier = c.Supplier.BusinessName }), JsonRequestBehavior.AllowGet);
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
                model = model.Where(p => p.Id.ToString() == text || p.RequestItem.PsCode.ItemName.Contains(text) || p.RequestItem.PsCode.ItemDescription.Contains(text) || p.RequestItem.PsCode.PsNo.Contains(text) || p.Description.Contains(text));
            }

            return Json(model.Select(c => new
            {
                Id = c.Id,
                Code = c.RequestItem.PsCode.PsNo,
                Name = c.RequestItem.PsCode.ItemName,
                Description = c.Description,
                Unit = c.RequestItem.PsCode.UnitMeas,
                Type = c.RequestItem.PsCode.PsType,
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
        public string Code { get; set; }
        public string Description { get; set; }
        public string Desc2 { get; set; }
        public string Desc3 { get; set; }
    }
}