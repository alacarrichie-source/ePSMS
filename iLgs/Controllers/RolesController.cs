using iLgs.Models;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [AppAuthorize("roles")]
    public class RolesController : BaseController
    {
        private readonly AppManEntities _db;

        public RolesController(AppManEntities db)
        {
            _db = db;
        }

        //HttpClient client;

        ////The URL of the WEB API Service
        ////string url = "http://localhost:60143/api/EmployeeInfoAPI";

        ////string iLgsApiUrl = ConfigurationManager.AppSettings["APPMAN_API_URL"];

        //string iLgsApiUrl = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["APPMAN_API_URL"].ToString()).DataSource;

        ////The HttpClient Class, this will be used for performing 
        ////HTTP Operations, GET, POST, PUT, DELETE
        ////Set the base address and the Header Formatter
        //public RolesController()
        //{
        //    client = new HttpClient();
        //    client.BaseAddress = new Uri(iLgsApiUrl);
        //    client.DefaultRequestHeaders.Accept.Clear();
        //    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //}

        // GET: Menu
        public ActionResult Index()
        {
            var userId = User.Identity.GetUserId();
            ViewBag.IsNotAdmin = !_db.AspNetUserRoles.Where(w => w.UserId == userId && w.RoleId == "admin").Any();
            return View();
        }


        public ActionResult RolesRead([DataSourceRequest] DataSourceRequest request)
        {
            var userId = User.Identity.GetUserId();
            var isAdmin = _db.AspNetUserRoles.Where(w => w.UserId == userId && w.RoleId == "admin").Any();
            if (isAdmin)
            {
                return Json(_db.AspNetRoles.ToDataSourceResult(request), JsonRequestBehavior.AllowGet);
            }
            else
            {
                //return Json(db.AspNetRoles.Where(w => db.AspNetUserRoles.Where(x => x.UserId == userId && x.RoleId == w.Id && x.RoleId.Contains("_admin")).Any()
                //            ||
                //            db.AspNetUserRoles.Where(x => x.UserId == userId && x.RoleId == w.Id
                //                && db.AspNetUserRoles.Where(y => y.UserId == x.UserId && y.RoleId.Contains("_admin") && y.RoleId.Contains(x.RoleId)).Any()
                //            ).Any()
                //        ).ToDataSourceResult(request));                
                var data = _db.AspNetRoles.Where(w => w.Id != "admin" && _db.AspNetUserRoles.Any(a => a.UserId == userId && a.RoleId.Contains(w.Id)));
                return Json(data.ToDataSourceResult(request), JsonRequestBehavior.AllowGet);
            }
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult RolesCreate([DataSourceRequest] DataSourceRequest request, AspNetRole model)
        {
            try
            {
                if (model != null && ModelState.IsValid)
                {
                    _db.AspNetRoles.Add(model);
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
        public ActionResult RolesUpdate([DataSourceRequest] DataSourceRequest request, AspNetRole model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    
                    _db.AspNetRoles.Attach(model);
                    _db.Entry(model).State = EntityState.Modified;
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
        public ActionResult RolesDestroy([DataSourceRequest]DataSourceRequest request, AspNetRole model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Attach the entity
                    _db.AspNetRoles.Attach(model);
                    // Delete the entity
                    _db.AspNetRoles.Remove(model);
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


        #region user in role

        public ActionResult UserInRoleRead([DataSourceRequest] DataSourceRequest request, string roleId)
        {
            return Json(_db.Database.SqlQuery<AspNetUserRoles_View>("Select *, CompKeyId = UserId + RoleId From AspNetUserRoles_View where RoleId = {0}", roleId).ToDataSourceResult(request));

        }

        [AcceptVerbs(HttpVerbs.Post)]
        //[AcceptVerbs(HttpVerbs.Get)]
        //public ActionResult UserInRoleCreate([DataSourceRequest] DataSourceRequest request, AspNetUserRoles_View model, string roleId)
        public ActionResult UserInRoleCreate([DataSourceRequest] DataSourceRequest request, AspNetUserRoles_View model, string roleId)
        {
            try
            {
                if (model != null && ModelState.IsValid)
                {
                    model.RoleId = roleId;
                    AspNetUserRole entity = SetAspNetUserRole(model);

                    _db.AspNetUserRoles.Add(entity);
                    _db.SaveChanges();
                    model.CompKeyId = model.UserId + model.RoleId;
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);

            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));

        }


        public AspNetUserRole SetAspNetUserRole(AspNetUserRoles_View model)
        {
            AspNetUserRole entity = new AspNetUserRole();
            entity.UserId = model.UserId;
            entity.RoleId = model.RoleId;
            return entity;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        //[AcceptVerbs(HttpVerbs.Get)]
        public ActionResult UserInRoleDestroy([DataSourceRequest]DataSourceRequest request, AspNetUserRoles_View model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var entity = _db.AspNetUserRoles.FirstOrDefault(f => f.RoleId == model.RoleId && f.UserId == model.UserId);
                    //AspNetUserRole entity = SetAspNetUserRole(model);

                    // Attach the entity
                    _db.AspNetUserRoles.Attach(entity);
                    // Delete the entity
                    _db.AspNetUserRoles.Remove(entity);
                    // Or use DeleteObject if using a previous versoin of Entity Framework
                    // Delete the entity in the database
                    _db.Entry(entity).State = EntityState.Deleted;
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

        #endregion

        //public JsonResult GetRoles(string sysCode)
        //{
        //    var model = db.AspNetRoles.AsQueryable();
        //    if (!string.IsNullOrEmpty(sysCode))
        //    {
        //        model = db.Menubases.Where(p => p.SysCode == sysCode).OrderBy(o => o.ParentId);
        //    }
        //    return Json(model.Select(c => new { Code = c.ChildId, Description = c.Description }), JsonRequestBehavior.AllowGet);

        //}


        public async Task<ActionResult> MenuAccessRead([DataSourceRequest] DataSourceRequest request, string sysCode, int? parentId, string userId, bool superAdmin, string department)
        {
            parentId = parentId ?? 0;
            if (superAdmin)
            {
                HttpResponseMessage responseMessage = await client.GetAsync("menubases_/access/" + sysCode + "/" + parentId + "/" + userId);
                IEnumerable<MenubaseVM> model = Enumerable.Empty<MenubaseVM>().AsQueryable();
                if (responseMessage.IsSuccessStatusCode)
                {
                    var responseData = responseMessage.Content.ReadAsStringAsync().Result;
                    model = JsonConvert.DeserializeObject<List<MenubaseVM>>(responseData);

                }

                return Json(model.ToDataSourceResult(request));
            }
            else
            {

                HttpResponseMessage responseMessage = await client.GetAsync("menubases_/access/adminMenu/" + sysCode + "/" + parentId + "/" + userId + "/" + department);
                IEnumerable<MenubaseVM> model = Enumerable.Empty<MenubaseVM>().AsQueryable();
                if (responseMessage.IsSuccessStatusCode)
                {
                    var responseData = responseMessage.Content.ReadAsStringAsync().Result;
                    model = JsonConvert.DeserializeObject<List<MenubaseVM>>(responseData);

                }

                return Json(model.ToDataSourceResult(request));
            }

        }
    }
}