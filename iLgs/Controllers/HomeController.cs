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
using Kendo.Mvc.UI;
using iLgs.Services.Codes;
using Kendo.Mvc.Extensions;
using System.Data.Entity;
using iLgs.Services.Dashboard;
using iLgs.Services;

namespace iLgs.Controllers
{
    public class HomeController : BaseController
    {

        //private iLGSEntities db = new iLGSEntities();
        //private static string sysCode = "PSMS";
        //private static string sysAdmin = "PSMS_ADMIN";

        //HttpClient client;

        ////The URL of the WEB API Service
        ////string iLgsApiUrl = ConfigurationManager.AppSettings["APPMAN_API_URL"];
        //string iLgsApiUrl = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["APPMAN_API_URL"].ToString()).DataSource;

        ////The HttpClient Class, this will be used for performing 
        ////HTTP Operations, GET, POST, PUT, DELETE
        ////Set the base address and the Header Formatter
        //public HomeController()
        //{
        //    client = new HttpClient();
        //    client.BaseAddress = new Uri(iLgsApiUrl);
        //    client.DefaultRequestHeaders.Accept.Clear();
        //    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

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

        private readonly ICodextnService _codextnService;
        private readonly IDashboardService _dashboardService;
        private readonly IUserService _userService;

        public HomeController()
        {
            _codextnService = new CodextnService(_db);
            _userService = new UserService(_db);
            _dashboardService = new DashboardService(_db);
        }

        public async Task<ActionResult> Index()
        {
            string userId = User != null && User.Identity.IsAuthenticated ? User.Identity.GetUserId() : null;
            string userName = User != null && User.Identity.IsAuthenticated ? User.Identity.Name : "Guest";

            var isAdmin = await _userService.IsUserNameAdminAsync(userName);

            var model = await _dashboardService.GetDashboardDataAsync(userId, userName);
            model.IsAdmin = isAdmin;

            if (!string.IsNullOrEmpty(userId))
            {
                var cartService = new iLgs.Services.PurchaseRequest.ProcurementCartService(_db);
                model.CartItemCount = await cartService.GetCartCountAsync(userId);
            }




            return View(model);
        }

        public async Task<ActionResult> GetHomePages([DataSourceRequest] DataSourceRequest request, int count)
        {
            var homePages = await _codextnService
                .GetByMastCode("HOME-PAGES")
                .Where(w => w.Desc5 != "N")
                .OrderBy(o => o.Code)
                .Take(count)
                .ToListAsync(); // 🔥 Force execution here

            var data = homePages.Select(item => new {
                item.Description,
                item.Desc2,
                item.Desc3, // Action
                item.Desc4, // Controller
                Url = Url.Action(item.Desc3, item.Desc4)
            });

            return Json(await data.ToDataSourceResultAsync(request));
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
