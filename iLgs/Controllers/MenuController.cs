using iLgs.Models;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{

    [AppAuthorize("Menu")]
    public class MenuController : BaseController
    {
        //private static string sysCode = "PSMS";
        //private static string sysAdmin = "PSMS_ADMIN";

        //private readonly AppManEntities _db = new AppManEntities();

        public MenuController()
        {

        }

        //HttpClient client;

        ////The URL of the WEB API Service
        ////string iLgsApiUrl = ConfigurationManager.AppSettings["APPMAN_API_URL"];
        //string iLgsApiUrl = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["APPMAN_API_URL"].ToString()).DataSource;

        ////string iLgsApiUrl = "http://localhost:3684/";
        ////The HttpClient Class, this will be used for performing 
        ////HTTP Operations, GET, POST, PUT, DELETE
        ////Set the base address and the Header Formatter
        //public MenuController()
        //{
        //    client = new HttpClient();
        //    client.BaseAddress = new Uri(iLgsApiUrl);
        //    client.DefaultRequestHeaders.Accept.Clear();
        //    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //}

        // GET: Menu
        public ActionResult Index(string sysCode, int? parentId)
        {
            ViewData["sysCode"] = sysCode;
            ViewData["parentId"] = parentId;
            return View();
        }

        public async Task<ActionResult> MenuRead([DataSourceRequest] DataSourceRequest request, string sysCode, int? parentId)
        {
            parentId = parentId ?? 0;
            HttpResponseMessage responseMessage = await client.GetAsync("menubases_/" + sysCode + "/" + parentId);
            IEnumerable<Menubase> model = Enumerable.Empty<Menubase>().AsQueryable();
            if (responseMessage.IsSuccessStatusCode)
            {
                var responseData = responseMessage.Content.ReadAsStringAsync().Result;
                model = JsonConvert.DeserializeObject<List<Menubase>>(responseData);

            }

            return Json(model.ToDataSourceResult(request));
        }        

        public ActionResult MenuCreate([DataSourceRequest] DataSourceRequest request, Menubase model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    model.InsertedBy = user;
                    model.InsertedDt = System.DateTime.Now;

                    HttpResponseMessage response = client.PostAsJsonAsync("menubases_/create", model).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = response.Content.ReadAsStringAsync().Result;
                        model = JsonConvert.DeserializeObject<Menubase>(responseData);

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

        public ActionResult MenuUpdate([DataSourceRequest] DataSourceRequest request, Menubase model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    model.UpdatedBy = user;
                    model.UpdatedDt = System.DateTime.Now;                    

                    HttpResponseMessage response = client.PutAsJsonAsync("menubases_/update/" + model.ChildId, model).Result;
                }

            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);

            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public ActionResult MenuDestroy([DataSourceRequest]DataSourceRequest request, Menubase model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    HttpResponseMessage response = client.DeleteAsync("menubases_/delete/" + model.ChildId).Result;
                                        
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);

            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AllowAnonymous]
        public JsonResult GetMenuLevels(string sysCode)
        {

            HttpResponseMessage responseMessage = client.GetAsync("menubases_/" + sysCode).Result;
            IEnumerable<Menubase> model = Enumerable.Empty<Menubase>().AsQueryable();
            if (responseMessage.IsSuccessStatusCode)
            {
                var responseData = responseMessage.Content.ReadAsStringAsync().Result;
                model = JsonConvert.DeserializeObject<List<Menubase>>(responseData);
            }

            var retVal = model.Select(c => new { Code = c.ChildId, Description = c.Description }).ToList();
            retVal.Add(new { Code = 0, Description = "ROOT" });

            return Json(retVal.OrderBy(o => o.Code), JsonRequestBehavior.AllowGet);

        }

        /*
         * Menu operations
         */

        //public async Task<IQueryable<Menubase>> GetMainMenu(string userId)
        //{
        //    //string userId = User.Identity.GetUserId();
        //    ViewBag.ShowMenu = false;
        //    ViewBag.IsAdmin = false;
        //    IQueryable<Menubase> model = Enumerable.Empty<Menubase>().AsQueryable();
        //    if (userId != null)
        //    {
        //        var admin = await GetUserInRole(userId, "admin");
        //        var sysadmin = await GetUserInRole(userId, sysAdmin);

        //        ViewBag.ShowMenu = true;
        //        if (admin || sysadmin)
        //        {
        //            ViewBag.IsAdmin = true;
        //            if (admin)
        //            {
        //                model = await GetAdminMenu();
        //            }
        //            else
        //            {
        //                model = await GetAdminMenu2(userId);
        //            }
        //        }
        //        else
        //        {
        //            model = await GetUserMenu(userId);
        //        }
        //    }
        //    return model;
        //}

        //public async Task<IQueryable<Menubase>> GetUserMenu(string userId)
        //{
        //    IQueryable<Menubase> model = Enumerable.Empty<Menubase>().AsQueryable();
        //    HttpResponseMessage responseMessage = await client.GetAsync("menubases_/usermenu/" + userId + "/" + sysCode);
        //    if (responseMessage.IsSuccessStatusCode)
        //    {
        //        var responseData = responseMessage.Content.ReadAsStringAsync().Result;
        //        if (responseData != "[]")
        //        {
        //            model = JsonConvert.DeserializeObject<List<Menubase>>(responseData).AsQueryable();
        //        }
        //    }
        //    return model;
        //}

        //public async Task<bool> GetUserInRole(string id, string role)
        //{
        //    bool retVal = false;
        //    HttpResponseMessage responseMessage = await client.GetAsync("roles_/" + id + "/" + role).ConfigureAwait(false);
        //    if (responseMessage.IsSuccessStatusCode)
        //    {
        //        var responseData = responseMessage.Content.ReadAsStringAsync().Result;
        //        var aspNetUser = JsonConvert.DeserializeObject<IEnumerable<iLgs.Models.AspNetUser>>(responseData);

        //        retVal = aspNetUser.Count() > 0;
        //    }

        //    return retVal;
        //}

        //public async Task<IQueryable<Menubase>> GetAdminMenu2(string userId)
        //{
        //    IQueryable<Menubase> model = Enumerable.Empty<Menubase>().AsQueryable();
        //    UserProfile userProfile = await GetUserProfile(userId);
        //    if (userProfile != null)
        //    {

        //        HttpResponseMessage responseMessage = await client.GetAsync("menubases_/adminMenu/" + sysCode + "/" + userProfile.Department);
        //        if (responseMessage.IsSuccessStatusCode)
        //        {
        //            var responseData = responseMessage.Content.ReadAsStringAsync().Result;
        //            if (responseData != "[]")
        //            {
        //                model = JsonConvert.DeserializeObject<List<Menubase>>(responseData).AsQueryable();
        //            }
        //        }

        //    }
        //    return model;
        //}

        //public async Task<IQueryable<Menubase>> GetAdminMenu()
        //{
        //    IQueryable<Menubase> model = Enumerable.Empty<Menubase>().AsQueryable();
        //    HttpResponseMessage responseMessage = await client.GetAsync("menubases_/" + sysCode);
        //    if (responseMessage.IsSuccessStatusCode)
        //    {
        //        var responseData = responseMessage.Content.ReadAsStringAsync().Result;
        //        if (responseData != "[]")
        //        {
        //            model = JsonConvert.DeserializeObject<List<Menubase>>(responseData).AsQueryable();
        //        }
        //    }
        //    return model;
        //}

        //public async Task<iLgs.Models.UserProfile> GetUserProfile(string id)
        //{
        //    HttpResponseMessage responseMessage = client.GetAsync("Users_/Profile/" + id).Result;
        //    if (responseMessage.IsSuccessStatusCode)
        //    {
        //        var responseData = await responseMessage.Content.ReadAsStringAsync();
        //        UserProfile model = JsonConvert.DeserializeObject<UserProfile>(responseData);
        //        return model;
        //    }
        //    else
        //    {
        //        return new UserProfile();
        //    }
        //}

        public async Task<ActionResult> MenuActionRead([DataSourceRequest] DataSourceRequest request, string sysCode, int? parentId)
        {
            parentId = parentId ?? 0;
            HttpResponseMessage responseMessage = await client.GetAsync("menubases_/" + sysCode + "/" + parentId);
            IEnumerable<Menubase> model = Enumerable.Empty<Menubase>().AsQueryable();
            if (responseMessage.IsSuccessStatusCode)
            {
                var responseData = responseMessage.Content.ReadAsStringAsync().Result;
                model = JsonConvert.DeserializeObject<List<Menubase>>(responseData);

            }

            return Json(model.ToDataSourceResult(request));
        }        
    }
}