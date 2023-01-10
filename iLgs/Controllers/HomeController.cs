using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Microsoft.AspNet.Identity;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using iLgs.Utilities;
using System.Data.SqlClient;

namespace iLgs.Controllers
{
    public class HomeController : BaseController
    {

        //private iLGSEntities db = new iLGSEntities();
        private static string sysCode = "APPMAN";
        private static string sysAdmin = "APPMAN_ADMIN";

        HttpClient client;

        //The URL of the WEB API Service
        //string iLgsApiUrl = ConfigurationManager.AppSettings["APPMAN_API_URL"];
        string iLgsApiUrl = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["APPMAN_API_URL"].ToString()).DataSource;

        //The HttpClient Class, this will be used for performing 
        //HTTP Operations, GET, POST, PUT, DELETE
        //Set the base address and the Header Formatter
        public HomeController()
        {
            client = new HttpClient();
            client.BaseAddress = new Uri(iLgsApiUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        }

        public async Task<IQueryable<Menubase>> GetAdminMenu2(string userId)
        {
            IQueryable<Menubase> model = Enumerable.Empty<Menubase>().AsQueryable();
            UserProfile userProfile = await GetUserProfile(userId);
            if (userProfile != null)
            {

                HttpResponseMessage responseMessage = await client.GetAsync("menubases_/adminMenu/" + sysCode + "/" + userProfile.Department);
                if (responseMessage.IsSuccessStatusCode)
                {
                    var responseData = responseMessage.Content.ReadAsStringAsync().Result;
                    if (responseData != "[]")
                    {
                        model = JsonConvert.DeserializeObject<List<Menubase>>(responseData).AsQueryable();
                    }
                }

            }
            return model;
        }

        public async Task<IQueryable<Menubase>> GetAdminMenu()
        {
            IQueryable<Menubase> model = Enumerable.Empty<Menubase>().AsQueryable();
            HttpResponseMessage responseMessage = await client.GetAsync("menubases_/" + sysCode);
            if (responseMessage.IsSuccessStatusCode)
            {
                var responseData = responseMessage.Content.ReadAsStringAsync().Result;
                if (responseData != "[]")
                {
                    model = JsonConvert.DeserializeObject<List<Menubase>>(responseData).AsQueryable();
                }
            }
            return model;

        }

        public async Task<bool> GetUserInRole(string id, string role)
        {
            bool retVal = false;
            HttpResponseMessage responseMessage = await client.GetAsync("roles_/" + id + "/" + role).ConfigureAwait(false);
            if (responseMessage.IsSuccessStatusCode)
            {
                var responseData = responseMessage.Content.ReadAsStringAsync().Result;
                var aspNetUser = JsonConvert.DeserializeObject<IEnumerable<AspNetUser>>(responseData);

                retVal = aspNetUser.Count() > 0;
            }

            return retVal;
        }


        public async Task<IQueryable<Menubase>> GetUserMenu(string userId)
        {
            IQueryable<Menubase> model = Enumerable.Empty<Menubase>().AsQueryable();
            HttpResponseMessage responseMessage = await client.GetAsync("menubases_/usermenu/" + userId + "/" + sysCode);
            if (responseMessage.IsSuccessStatusCode)
            {
                var responseData = responseMessage.Content.ReadAsStringAsync().Result;
                if (responseData != "[]")
                {
                    model = JsonConvert.DeserializeObject<List<Menubase>>(responseData).AsQueryable();
                }
            }
            return model;
        }


        public async Task<ActionResult> Index()
        {
            try
            {
                ViewBag.Message = "iLGS";
                string userId = User.Identity.GetUserId();
                ViewBag.ShowMenu = false;
                ViewBag.IsAdmin = false;
                IQueryable<Menubase> model = Enumerable.Empty<Menubase>().AsQueryable();
                if (userId != null)
                {                 
                    ViewBag.ShowMenu = true;
                    if (await GetUserInRole(userId, "admin") || await GetUserInRole(userId, sysAdmin))
                    {
                        ViewBag.IsAdmin = true;
                        if (await GetUserInRole(userId, "admin"))
                        {
                            model = await GetAdminMenu();
                        }
                        else
                        {
                            model = await GetAdminMenu2(userId);
                        }
                    }
                    else
                    {
                        model = await GetUserMenu(userId);
                    }
                }
                return View(model);
            }
            catch (Exception e)
            {
                ViewBag.Error = "CANNOT CONNECT TO WEB API.";
                return View("Error");
            }

        }

        //public async Task<Access> Access(string userId, string menuId)
        //{
        //    if (await GetUserInRole(userId, "admin") || await GetUserInRole(userId, sysAdmin))
        //    {
        //        return new Access() { AllowAdd = true, AllowEdit = true, AllowDelete = true , AllowPost = true, AllowUnpost = true, IsAdmin = true};
        //    }
        //    else
        //    {
        //        HttpResponseMessage responseMessage = client.GetAsync("menubases_/accessfile/" + userId + "/" + menuId + "/" + sysCode).Result;
        //        if (responseMessage.IsSuccessStatusCode)
        //        {
        //            var responseData = responseMessage.Content.ReadAsStringAsync().Result;
        //            Accessfile model = JsonConvert.DeserializeObject<Accessfile>(responseData);
        //            if (model.RecId == Guid.Empty)
        //            {
        //                return null;
        //            }
        //            return new Access() { AllowAdd = model.AllowAdd, AllowEdit = model.AllowEdit, AllowDelete = model.AllowDelete, AllowPost = model.AllowPost, AllowUnpost = model.AllowUnpost, IsAdmin = false };
        //        }
        //        else
        //        {
        //            return new Access() { AllowAdd = false, AllowEdit = false, AllowDelete = false, AllowPost = false, AllowUnpost = false, IsAdmin = false };
        //        }
        //    }
        //}

        public async Task<Access> Access(string userId, string menuId)
        {
            if (await GetUserInRole(userId, "admin") || await GetUserInRole(userId, sysAdmin))
            {
                return new Access() { IsAdmin = true };
            }
            else
            {
                HttpResponseMessage responseMessage = client.GetAsync("menubases_/accessRights/" + userId + "/" + menuId + "/" + sysCode).Result;
                if (responseMessage.IsSuccessStatusCode)
                {
                    var responseData = responseMessage.Content.ReadAsStringAsync().Result;
                    Access model = JsonConvert.DeserializeObject<Access>(responseData);
                    
                    return model;
                }
                else
                {
                    return new Access();
                }
            }
        }

        public async Task<UserProfile> GetUserProfile(string id)
        {
            HttpResponseMessage responseMessage = client.GetAsync("Users_/Profile/" + id).Result;
            if (responseMessage.IsSuccessStatusCode)
            {
                var responseData = await responseMessage.Content.ReadAsStringAsync();
                UserProfile model = JsonConvert.DeserializeObject<UserProfile>(responseData);
                return model;
            }
            else
            {
                return new UserProfile();
            }
        }


        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}