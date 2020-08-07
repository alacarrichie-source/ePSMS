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
        private iLGSEntities db = new iLGSEntities();
        public ActionResult GetSysCodeList(string text)
        {


            var model = db.SysCodes.OrderBy(o => o.SysCode1).AsQueryable();
            if (!string.IsNullOrEmpty(text))
            {
                text = text.Trim();
                model = model.Where(p => p.SysCode1 == text || p.SysDescription.Contains(text) || p.SysCode1.Contains(text));
            }

            var retModel = model.Select(c => new GetSysCodeVM { Code = c.SysCode1, Description = c.SysDescription }).ToList();
            if (string.IsNullOrEmpty(text))
            {
                retModel.Insert(0, new GetSysCodeVM { Code = "ALL", Description = "ALL" });
            }

            return Json(retModel, JsonRequestBehavior.AllowGet);

        }

        public ActionResult GetDepartmentList(string text)
        {

            var model = db.Database.SqlQuery<GetDepartmentVM>("Select distinct case when Isnull(Department, '')  = '' then 'NONE' else Department end as Department From UserProfiles").AsQueryable();
            if (!string.IsNullOrEmpty(text))
            {
                text = text.Trim();
                model = model.Where(p => p.Department.Contains(text));
            }

            var retModel = model.Select(c => new GetDepartmentVM { Department  = c.Department }).ToList();
            if (string.IsNullOrEmpty(text))
            {
                retModel.Insert(0, new GetDepartmentVM { Department  = "ALL" });
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
}