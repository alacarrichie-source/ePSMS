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
using System.Text;
using System.IO;
using System.Security.Claims;

namespace iLgs.Utilities
{    
    public abstract class BaseController : Controller
    {
        //private static string sysCode = "APPMAN";
        //private static string sysAdmin = "APPMAN_ADMIN";
        
        //HttpClient client;

        ////The URL of the WEB API Service
        ////string iLgsApiUrl = ConfigurationManager.AppSettings["APPMAN_API_URL"];
        //string iLgsApiUrl = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["APPMAN_API_URL"].ToString()).DataSource;

        //public BaseController()
        //{
        //    client = new HttpClient();
        //    client.BaseAddress = new Uri(iLgsApiUrl);
        //    client.DefaultRequestHeaders.Accept.Clear();
        //    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //    string userId = User.Identity.GetUserId();
        //    ViewBag.ShowMenu = false;
        //    ViewBag.IsAdmin = false;
        //    IQueryable<Menubase> model = Enumerable.Empty<Menubase>().AsQueryable();
        //    if (userId != null)
        //    {
        //        ViewBag.ShowMenu = true;
        //        var isAdmin = GetUserInRole(userId, "admin").Result;
        //        var isSysAdmin = GetUserInRole(userId, sysAdmin).Result;
        //        if (isAdmin || isSysAdmin)
        //        {
        //            ViewBag.IsAdmin = true;
        //            if (isAdmin)
        //            {
        //                model = GetAdminMenu().Result;
        //            }
        //            else
        //            {
        //                model = GetAdminMenu2(userId).Result;
        //            }
        //        }
        //        else
        //        {
        //            model = GetUserMenu(userId).Result;
        //        }
        //    }
        //    ViewData["MainMenu"] = model;

        //}

        //public static class ExtensionMethods
        //{
        //    public static string GetUserId(this ClaimsPrincipal User)
        //    {
        //        return User.Identity.GetUserId();
        //    }
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

        //public async Task<bool> GetUserInRole(string id, string role)
        //{
        //    bool retVal = false;
        //    HttpResponseMessage responseMessage = await client.GetAsync("roles_/" + id + "/" + role).ConfigureAwait(false);
        //    if (responseMessage.IsSuccessStatusCode)
        //    {
        //        var responseData = responseMessage.Content.ReadAsStringAsync().Result;
        //        var aspNetUser = JsonConvert.DeserializeObject<IEnumerable<AspNetUser>>(responseData);

        //        retVal = aspNetUser.Count() > 0;
        //    }

        //    return retVal;
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

        //public async Task<UserProfile> GetUserProfile(string id)
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

        protected override JsonResult Json(object data, string contentType,
            Encoding contentEncoding, JsonRequestBehavior behavior)
        {
            return new JsonNetResult
            {
                Data = data,
                ContentType = contentType,
                ContentEncoding = contentEncoding,
                JsonRequestBehavior = behavior
            };
        }
    }

    /*
     * Source https://wingkaiwan.com/2012/12/28/replacing-mvc-javascriptserializer-with-json-net-jsonserializer/ 
     * 
     */
    public class JsonNetResult : JsonResult
    {
        public JsonNetResult()
        {
            Settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Error
            };
        }

        public JsonSerializerSettings Settings { get; private set; }

        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (this.JsonRequestBehavior == JsonRequestBehavior.DenyGet && string.Equals(context.HttpContext.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("JSON GET is not allowed");

            HttpResponseBase response = context.HttpContext.Response;
            response.ContentType = string.IsNullOrEmpty(this.ContentType) ? "application/json" : this.ContentType;

            if (this.ContentEncoding != null)
                response.ContentEncoding = this.ContentEncoding;
            if (this.Data == null)
                return;

            var scriptSerializer = JsonSerializer.Create(this.Settings);

            using (var sw = new StringWriter())
            {
                scriptSerializer.Serialize(sw, this.Data);
                response.Write(sw.ToString());
            }
        }
    }

    /*
        Now by having your controllers inheriting from BaseController, they will start using JSON.NET to do JSON serialization. 
        Here are the two ways to make use of the new serializer in a controller.
     
         // 1. calling overridden Json() method
        return Json(data, JsonRequestBehavior.AllowGet);
 
        // 2. instantiating JsonNetResult
        var result = new JsonNetResult
                         {
                             Data = data,
                             JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                             Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
                         };
        return result;
    */
}