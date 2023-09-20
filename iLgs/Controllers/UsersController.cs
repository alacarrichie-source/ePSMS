using iLgs.Models;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System;
using System.Collections.Generic;
using Microsoft.AspNet.Identity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using iLgs.Utilities;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Data.SqlClient;

namespace iLgs.Controllers
{
    [AppAuthorize("users")]
    public class UsersController : BaseController
    {
        private static string sysCode = "PSMS";
        private static string sysAdmin = "PSMS_ADMIN";

        private AppManEntities db = new AppManEntities();

        HttpClient client;

        //The URL of the WEB API Service
        //string url = "http://localhost:60143/api/EmployeeInfoAPI";

        //string iLgsApiUrl = ConfigurationManager.AppSettings["APPMAN_API_URL"];

        string iLgsApiUrl = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["APPMAN_API_URL"].ToString()).DataSource;

        //The HttpClient Class, this will be used for performing 
        //HTTP Operations, GET, POST, PUT, DELETE
        //Set the base address and the Header Formatter
        public UsersController()
        {
            client = new HttpClient();
            client.BaseAddress = new Uri(iLgsApiUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // GET: Users
        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> UserRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = db.Database.SqlQuery<AspNetUser>("Select * from AspNetUsers");

            return Json(await data.ToDataSourceResultAsync(request));
        }

        public async Task<ActionResult> UserReadAsync([DataSourceRequest] DataSourceRequest request)
        {
            IEnumerable<AspNetUser> model = Enumerable.Empty<AspNetUser>().AsQueryable();
            HttpResponseMessage responseMessage = await client.GetAsync("users_/");
            if (responseMessage.IsSuccessStatusCode)
            {
                var responseData = responseMessage.Content.ReadAsStringAsync().Result;
                model = JsonConvert.DeserializeObject<IEnumerable<AspNetUser>>(responseData);

            }

            return Json(model.ToDataSourceResult(request));
        }


        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult UserUpdate([DataSourceRequest] DataSourceRequest request, Menubase model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;
                    model.UpdatedBy = user;
                    model.UpdatedDt = date;

                    db.Menubases.Attach(model);
                    db.Entry(model).State = EntityState.Modified;
                    db.SaveChanges();
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
        public ActionResult UserDestroy([DataSourceRequest]DataSourceRequest request, Menubase model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;
                    model.UpdatedBy = user;
                    model.UpdatedDt = date;
                    // Attach the entity
                    db.Menubases.Attach(model);
                    // Delete the entity
                    db.Menubases.Remove(model);
                    // Or use DeleteObject if using a previous versoin of Entity Framework
                    // Delete the entity in the database
                    //db.Entry(model).State = System.Data.EntityState.Deleted;
                    db.SaveChanges();

                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);

            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public async Task<ActionResult> UserProfilesRead([DataSourceRequest] DataSourceRequest request)
        {

            string userId = User.Identity.GetUserId();
            Task<bool> task = new HomeController().GetUserInRole(userId, "admin");
            var isAdmin = await task;

            //var data = db.AspNetUsers.Include(i => i.UserInfo).ToList();


            var data = db.Database.SqlQuery<AspNetUsers_View>("Select * From AspNetUsers_View").ToList();
            if (!isAdmin)
            {
                //data = data.Where(w => w.AspNetUserRoles.Where(x => x.RoleId.TrimEnd() != "admin" && x.UserId == w.Id).Any()).ToList();
                data = data.Where(w => db.AspNetUserRoles.Where(x => x.RoleId != "admin" && x.UserId == w.Id).Any() || db.AspNetUserRoles.Where(y => y.UserId == w.Id).Count() == 0).ToList();
            }
            return Json(data.ToDataSourceResult(request));

        }

        //[AcceptVerbs(HttpVerbs.Post)]
        //public ActionResult UserProfilesCreate([DataSourceRequest] DataSourceRequest request, UserProfile model, string userId)
        //{
        //    try
        //    {
        //        if (model != null && ModelState.IsValid)
        //        {

        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            model.InsertedBy = user;
        //            model.InsertedDt = date;
        //            model.UpdatedBy = user;
        //            model.UpdatedDt = date;

        //            db.UserProfiles.Add(model);
        //            db.SaveChanges();


        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
        //             "please contact tech support with this message: " + e.Message);

        //    }

        //    return Json(new[] { model }.ToDataSourceResult(request, ModelState));

        //}

        public UserProfile SetUserProfile(AspNetUsers_View model, string mode)
        {
            string user = ControllerContext.HttpContext.User.Identity.Name;
            DateTime date = System.DateTime.Now;

            UserProfile entity = new UserProfile();

            entity.NameLast = model.NameLast;
            entity.NameFirst = model.NameFirst;
            entity.NameMid = model.NameMid;
            entity.NameFull = model.NameFull;
            entity.Birthday = model.Birthday;
            entity.Sex = model.Sex;
            entity.AddressBarangay = model.AddressBarangay;
            entity.AddressCity = model.AddressCity;
            entity.AddressHouseNo = model.AddressHouseNo;
            entity.AddressProvince = model.AddressProvince;
            entity.AddressStreet = model.AddressStreet;
            entity.AddressSubdivision = model.AddressSubdivision;
            entity.AddressZipCode = model.AddressZipCode;
            entity.Department = model.Department;
            entity.Division = model.Division;
            entity.Section = model.Section;
            entity.UserCode = model.UserCode;
            entity.UserId = model.Id;
            if (mode == "A")
            {
                entity.InsertedBy = user;
                entity.InsertedDt = date;
            }
            else
            {
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            return entity;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult UserProfilesUpdate([DataSourceRequest] DataSourceRequest request, AspNetUsers_View model)
        {
            try
            {
                if (ModelState.IsValid)
                {


                    // check if record already exists
                    UserProfile entity = db.UserProfiles.Where(p => p.UserId == model.Id).SingleOrDefault();
                    if (entity == null)
                    {

                        entity = SetUserProfile(model, "A");
                        db.UserProfiles.Add(entity);
                        db.SaveChanges();
                    }
                    else
                    {

                        var aspNetUser = db.AspNetUsers.Find(model.Id);
                        aspNetUser.Email = model.Email;
                        db.AspNetUsers.Attach(aspNetUser);
                        db.Entry(aspNetUser).State = EntityState.Modified;
                        db.SaveChanges();


                        string user = ControllerContext.HttpContext.User.Identity.Name;
                        DateTime date = System.DateTime.Now;

                        model.UpdatedBy = user;
                        model.UpdatedDt = date;

                        int id = db.Database.ExecuteSqlCommand("Exec UserProfile_Update {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20}, {21}, {22}, {23}",
                            model.Id, model.NameLast, model.NameFirst, model.NameMid, model.NameFull, model.Birthday,
                            model.Sex, model.TelNo, model.MobileNo, model.AddressHouseNo, model.AddressStreet, model.AddressSubdivision,
                            model.AddressBarangay, model.AddressCity, model.AddressProvince, model.AddressZipCode, model.InsertedBy, model.InsertedDt, model.UpdatedBy,
                            model.UpdatedDt, model.Department, model.Division, model.Section, model.UserCode);

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
        public async Task<ActionResult> UserProfilesDestroy([DataSourceRequest]DataSourceRequest request, AspNetUsers_View model)
        {
            try
            {
                string userId = User.Identity.GetUserId();
                var admin = await new HomeController().GetUserInRole(userId, "admin");
                var sysadmin = await new HomeController().GetUserInRole(userId, sysAdmin);
                if (!(admin || sysadmin))
                {
                    ModelState.AddModelError("Access", "Access Denied! Only iLGS admins & Super Admins can delete users...");
                }
                if (ModelState.IsValid)
                {
                    var entity = db.AspNetUsers.Find(model.Id);
                    // Attach the entity
                    db.AspNetUsers.Attach(entity);
                    // Delete the entity
                    db.AspNetUsers.Remove(entity);
                    // Or use DeleteObject if using a previous versoin of Entity Framework
                    // Delete the entity in the database
                    //db.Entry(model).State = System.Data.EntityState.Deleted;
                    db.SaveChanges();

                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);

            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }



        public JsonResult GetUsers(string text)
        {

            var model = db.UserProfiles.Select(c => new { OwnerId = c.UserId, NameFull = c.NameFull, UserName = c.AspNetUser.UserName }).AsQueryable();
            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.NameFull.Contains(text));
            }
            return Json(model, JsonRequestBehavior.AllowGet);

        }

        public JsonResult GetRoleUsers(string text)
        {

            var userId = User.Identity.GetUserId();
            var isAdmin = db.AspNetUserRoles.Where(w => w.UserId == userId && w.RoleId == "admin").Any();


            var model = db.UserProfiles.AsQueryable();
            if (!string.IsNullOrEmpty(text))
            {
                model = model.Where(p => p.NameFull.Contains(text) || p.AspNetUser.UserName.Contains(text));
            }
            if (!isAdmin)
            {
                model = model.Where(w => db.AspNetUserRoles.Where(x => x.UserId == w.UserId && x.RoleId == "admin").Count() == 0);
            }

            return Json(model.Select(c => new { OwnerId = c.UserId, NameFull = c.NameFull, UserName = c.AspNetUser.UserName }), JsonRequestBehavior.AllowGet);

        }

        public JsonResult GetUserNotInRole(string roleId, string text)
        {

            var model = db.UserProfiles.AsQueryable().Where(w => !db.AspNetUserRoles.Where(r => r.RoleId == roleId && r.UserId == w.UserId).Any());
            if (!string.IsNullOrEmpty(text))
            {
                model = db.UserProfiles.Where(p => p.NameFull.Contains(text));
            }
            return Json(model.Select(c => new { OwnerId = c.UserId, NameFull = c.NameFull }), JsonRequestBehavior.AllowGet);

        }


        #region User Codes
        public ActionResult UserCodes()
        {
            return View();
        }


        public ActionResult UserCodesRead([DataSourceRequest] DataSourceRequest request)
        {
            return Json(db.Database.SqlQuery<UserCodes_View>("Select * From UserCodes_View").ToDataSourceResult(request));

        }

        public UserCode SetUserCode(UserCodes_View model, string mode)
        {
            string user = ControllerContext.HttpContext.User.Identity.Name;
            DateTime date = System.DateTime.Now;

            UserCode e = new UserCode();
            e.UserCode1 = model.UserCode;
            e.UserId = model.UserId;
            e.Department = model.Department;
            e.Division = model.Division;
            e.Section = model.Section;
            e.UserGroup = model.UserGroup;
            if (mode == "A")
            {
                e.InsertedBy = user;
                e.InsertedDt = date;
            }
            e.UpdatedBy = user;
            e.UpdatedDt = date;

            return e;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult UserCodesCreate([DataSourceRequest] DataSourceRequest request, UserCodes_View model)
        {
            try
            {
                if (model != null && ModelState.IsValid)
                {

                    UserCode e = SetUserCode(model, "A");

                    db.UserCodes.Add(e);
                    db.SaveChanges();

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
        public ActionResult UserCodesUpdate([DataSourceRequest] DataSourceRequest request, UserCodes_View model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    UserCode e = SetUserCode(model, "U");

                    db.UserCodes.Attach(e);
                    db.Entry(model).State = EntityState.Modified;
                    db.SaveChanges();
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
        public ActionResult UserCodesDestroy([DataSourceRequest]DataSourceRequest request, UserCodes_View model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    UserCode e = SetUserCode(model, "U");

                    // Attach the entity
                    db.UserCodes.Attach(e);
                    // Delete the entity
                    db.UserCodes.Remove(e);
                    // Or use DeleteObject if using a previous versoin of Entity Framework
                    // Delete the entity in the database
                    //db.Entry(model).State = System.Data.EntityState.Deleted;
                    db.SaveChanges();

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

        #region Access

        public async Task<ActionResult> _Access(string userId, string nameFull, string userName, string sysCode, int? parentId)
        {
            var adminId = User.Identity.GetUserId();
            ViewData["userId"] = userId;
            ViewData["nameFull"] = nameFull;
            ViewData["userName"] = userName;

            ViewData["sysCode"] = sysCode;
            ViewData["parentId"] = parentId;

            ViewData["superAdmin"] = await new HomeController().GetUserInRole(adminId, "admin");
            UserProfile adminProfile = await new HomeController().GetUserProfile(adminId);
            var department = string.IsNullOrEmpty(adminProfile.Department) ? "" : adminProfile.Department;
            ViewData["department"] = department;

            return View();
        }        

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult MenuAccessSet([DataSourceRequest] DataSourceRequest request, MenubaseVM model, string userId, string sysCode, int childId)
        {
            try
            {
                string user = HttpContext.User.Identity.Name;
                DateTime? date = DateTime.Now;
                var access = db.MenuAccesses.Where(w => w.Menubase.SysCode == sysCode && w.Menubase.ChildId == childId && w.UserId == userId).FirstOrDefault();

                if (access == null)
                {
                    access = new MenuAccess()
                    {
                        Id = Guid.NewGuid(),
                        MenuId = childId,
                        UserId = userId,
                        IsAllowed = true,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };

                    db.MenuAccesses.Add(access);
                    db.SaveChanges();
                    model.IsAllowed = true;
                }
                else
                {
                    access.IsAllowed = access.IsAllowed == true ? false : true;
                    access.UpdatedBy = user;
                    access.UpdatedDt = date;

                    db.MenuAccesses.Attach(access);
                    db.Entry(access).State = EntityState.Modified;
                    db.SaveChanges();

                    model.IsAllowed = (bool) access.IsAllowed;
                }

                model.AccessId = access.Id;
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            return new JsonNetResult
            {
                Data = new[] { model }.ToDataSourceResult(request, ModelState),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
        }

        public ActionResult AccessActionRead([DataSourceRequest] DataSourceRequest request, Guid? accessId, string userId)
        {
            HttpResponseMessage responseMessage = client.GetAsync("MenuAction/MenuAccessAction/" + accessId + "/" + userId).Result;
            IEnumerable<MenuAccessAction> model = Enumerable.Empty<MenuAccessAction>().AsQueryable();
            if (responseMessage.IsSuccessStatusCode)
            {
                var responseData = responseMessage.Content.ReadAsStringAsync().Result;
                model = JsonConvert.DeserializeObject<List<MenuAccessAction>>(responseData);
            }

            return Json(model.ToDataSourceResult(request));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult AccessActionSet([DataSourceRequest] DataSourceRequest request, MenubaseVM model, string userId, Guid accessId, Guid? actionId)
        {
            try
            {
                string user = HttpContext.User.Identity.Name;
                DateTime? date = DateTime.Now;
                var rec = db.MenuAccessActions.Where(w => w.MenuAccess.Id == accessId && w.MenuAccess.UserId == userId && w.ActionId == actionId).FirstOrDefault();

                if (rec == null)
                {                    

                    MenuAccessAction action = new MenuAccessAction()
                    {
                        Id = Guid.NewGuid(),
                        AccessId = accessId,
                        ActionId = actionId,
                        InsertedBy = user,
                        InsertedDt = date.Value,
                        UpdatedBy = user,
                        UpdatedDt = date.Value,
                        IsAllowed = true
                    };

                    db.MenuAccessActions.Add(action);
                    db.SaveChanges();                    
                }
                else
                {
                    rec.IsAllowed = !rec.IsAllowed;
                    model.UpdatedBy = user;
                    model.UpdatedDt = date;

                    db.MenuAccessActions.Attach(rec);
                    db.Entry(rec).State = EntityState.Modified;
                    db.SaveChanges();                    
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            return new JsonNetResult
            {
                Data = new[] { model }.ToDataSourceResult(request, ModelState),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            }; 
        }


        
        public ActionResult _CopyAccess(string userId, string userName, string sysCode)
        {
            CopyAccessVM model = new CopyAccessVM()
            {
                SourceUserId = userId,
                SourceUserName = userName,
                SysCode = sysCode
            };

            return PartialView(model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> CopyAccessSave([DataSourceRequest] DataSourceRequest request, CopyAccessVM model)
        {
            try
            {
                string user = ControllerContext.HttpContext.User.Identity.Name;
                var date = System.DateTime.Now;

                db.MenuAccesses.RemoveRange(db.MenuAccesses.Where(w => w.Menubase.SysCode == model.SysCode && w.UserId == model.TargetUserId));
                await db.SaveChangesAsync();

                var menubaseList = db.MenuAccesses.Include(i => i.MenuAccessActions).Where(w => w.Menubase.SysCode == model.SysCode && w.UserId == model.SourceUserId).ToList();
                foreach (var e in menubaseList)
                {
                    var menu = new MenuAccess()
                    {
                        Id = Guid.NewGuid(),                                               
                        UserId = model.TargetUserId,
                        MenuId = e.MenuId,
                        IsAllowed = e.IsAllowed,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };
                    db.MenuAccesses.Add(menu);
                    await db.SaveChangesAsync();

                    foreach(var accessAction in e.MenuAccessActions)
                    {
                        var action = new MenuAccessAction()
                        {
                            Id = Guid.NewGuid(),
                            AccessId = menu.Id,
                            ActionId = accessAction.ActionId,
                            IsAllowed = accessAction.IsAllowed,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };
                        db.MenuAccessActions.Add(action);
                        await db.SaveChangesAsync();
                    }
                }                                
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);

                var query = from state in ModelState.Values
                            from error in state.Errors
                            select error.ErrorMessage;

                var errorList = query.ToList();
                if (errorList.Count() > 0)
                {
                    return Json(new { Errors = errorList }, JsonRequestBehavior.DenyGet);
                }
            }

            return Json(new { Errors = "" }, JsonRequestBehavior.AllowGet);

        }


        #endregion

    }
}