using iLgs.Models;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    public abstract class BaseController : Controller
    {
        protected static string sysCode = "PSMS";
        protected static string sysAdmin = "PSMS_ADMIN";

        private bool InitMenu { get; set; }

        protected HttpClient client;

        //The URL of the WEB API Service
        string iLgsApiUrl = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["APPMAN_API_URL"].ToString()).DataSource;

        //The HttpClient Class, this will be used for performing 
        //HTTP Operations, GET, POST, PUT, DELETE
        //Set the base address and the Header Formatter
        public BaseController()
        {
            client = new HttpClient();
            client.BaseAddress = new Uri(iLgsApiUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            this.InitMenu = true;
        }
        
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            ViewData["MenuTreeList"] = null;
            if (User != null && User.Identity.IsAuthenticated)
            {                
                string userId = User.Identity.GetUserId();
                               
                
                var allMenu = Task.Run(async () => await GetMainMenu(User.Identity.GetUserId())).Result;


                bool isLocalhost = false;

                string host = HttpContext.Request.Url.Host;
                Console.WriteLine("Host = " + host);
                if (host == "localhost" || host == "127.0.0.1" || host == "::1")
                {
                    isLocalhost = true;
                }

                if (isLocalhost)
                {
                    Session["WebsiteName"] = string.Empty;
                }
                else
                {
                    string url = HttpContext.Request.Url.ToString();

                    Uri uri = new Uri(url);
                    string path = uri.AbsolutePath;
                    //string websiteName = path.Trim('/');                    

                    // Extract the first segment of the path
                    string websiteName = uri.Segments.Length > 1 ? uri.Segments[1].Trim('/') : string.Empty;


                    foreach (var menu in allMenu)
                    {
                        if (!isLocalhost)
                        {
                            menu.Controller = websiteName + "/" + menu.Controller;
                            Console.WriteLine(menu.Controller);
                        }
                    }
                }

                var menuTreeList = new List<TreeViewItemModel>();
                var menus = allMenu.Where(w => w.ParentId == 0);

                menuTreeList = GetMenuTree(allMenu, menus);

                ViewData["MenuTreeList"] = menuTreeList;
            }

            base.OnActionExecuting(filterContext);
        }

        protected List<TreeViewItemModel> GetMenuTree(IQueryable<Menubase> allMenus, IQueryable<Menubase> menus)
        {
            var menuTree = new List<TreeViewItemModel>();

            foreach (var menu in menus)
            {
                var children = allMenus.Where(w => w.ParentId == menu.ChildId).OrderBy(o => o.Description);
                var hasChildren = children.Any();
                var items = new List<TreeViewItemModel>();                

                if (hasChildren)
                {
                    items = GetMenuTree(allMenus, children);                                        
                }
                
                var node = new TreeViewItemModel()
                {
                    Id = menu.ChildId.ToString(),
                    Expanded = false,
                    Text = menu.Description,
                    HasChildren = hasChildren,
                    Url = hasChildren ? null : $"/{menu.Controller}/{menu.Action}",
                    Items = items
                };

                menuTree.Add(node);
            }

            return menuTree;
        }

        protected async Task<IQueryable<Menubase>> GetMainMenu(string userId)
        {
            ViewBag.ShowMenu = false;
            ViewBag.IsAdmin = false;
            IQueryable<Menubase> model = Enumerable.Empty<Menubase>().AsQueryable();
            if (userId != null)
            {
                var admin = await GetUserInRole(userId, "admin");
                var sysadmin = await GetUserInRole(userId, sysAdmin);

                ViewBag.ShowMenu = true;
                if (admin || sysadmin)
                {
                    ViewBag.IsAdmin = true;
                    if (admin)
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
            return model;
        }

        protected async Task<UserProfile> GetUserProfile(string id)
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

        protected async Task<IQueryable<Menubase>> GetAdminMenu2(string userId)
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

        protected async Task<IQueryable<Menubase>> GetAdminMenu()
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

        protected async Task<bool> GetUserInRole(string id, string role)
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


        protected async Task<IQueryable<Menubase>> GetUserMenu(string userId)
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

        protected async Task<Access> Access(string userId, params string[] menuIds)
        {
            Access returnAccess = new Access();
            foreach (var menuId in menuIds)
            {
                var access =  await Access(userId, menuId);
                if (access != null)
                {
                    returnAccess = access;
                    break;
                }
            }
            return returnAccess;
        }

        protected async Task<Access> Access(string userId, string menuId)
        {
            if (await GetUserInRole(userId, "admin") || await GetUserInRole(userId, sysAdmin))
            {
                return new Access()
                {
                    IsAdmin = true,
                    IsAllowed = true,
                    AllowAdd = true,
                    AllowEdit = true,
                    AllowDelete = true,
                    AllowPost = true,
                    AllowUnpost = true,
                    AllowPrint = true,
                    AllowTransfer = true,
                    Actions = new List<MenuAccessAction>()
                };
            }
            else
            {
                HttpResponseMessage responseMessage = client.GetAsync("menubases_/menuAccessRights/" + userId + "/" + menuId + "/" + sysCode).Result;
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                client.Dispose();
            }
            base.Dispose(disposing);
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