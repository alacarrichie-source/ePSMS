using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using iLgs.Models;
using System.Net.Http;
using System.Configuration;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace iLgs.Utilities
{
    public class AppAuthorizeAttribute : AuthorizeAttribute
    {
        AppManEntities context = new AppManEntities(); 
        private readonly string[] allowedController;
        string sysCode = "APPMAN";
        string[] roles = { "ADMIN", "APPMAN_ADMIN" };

        HttpClient client;

        //The URL of the WEB API Service
        //string iLgsApiUrl = ConfigurationManager.AppSettings["APPMAN_API_URL"];

        string iLgsApiUrl = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["APPMAN_API_URL"].ToString()).DataSource;

        //The HttpClient Class, this will be used for performing 
        //HTTP Operations, GET, POST, PUT, DELETE
        //Set the base address and the Header Formatter

        public AppAuthorizeAttribute(params string[] controllerName)
        {
            this.allowedController = controllerName;            
            
            client = new HttpClient();
            client.BaseAddress = new Uri(iLgsApiUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));            
        }



        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            bool authorize = false;
            bool admin = false;
            var id = httpContext.User.Identity.GetUserId();

            if (id == null)
            {
                return false;
            }

            
            //var admin = context.AspNetUserRoles_View.Where(p => p.UserId == userId && (p.RoleId.ToUpper() == "ADMIN" || p.RoleId.ToUpper() == "APPMAN_ADMIN"));
            //HttpResponseMessage responseMessage = await client.GetAsync("roles_/" + id + "/" + role).ConfigureAwait(false);
            
            HttpResponseMessage responseMessage = client.GetAsync("roles_/" + id ).Result;
            if (responseMessage.IsSuccessStatusCode)
            {
                var responseData = responseMessage.Content.ReadAsStringAsync().Result;
                var aspNetUser = JsonConvert.DeserializeObject<IEnumerable<AspNetUser>>(responseData);

                foreach (var role in roles)
                {
                    var roleFound = aspNetUser.Where(w => w.AspNetUserRoles.Where(x => x.RoleId.ToUpper() == role).Any()).Count();
                    if (roleFound > 0)
                    {
                        admin = true;
                        break;
                    }
                }                
            }


            if (admin)
                authorize = true;
            else
            {
                foreach (var controllerName in allowedController)
                {
                    HttpResponseMessage responseMessage2 = client.GetAsync("usermenu_/" + id + "/" + sysCode + "/" + controllerName).Result;
                    if (responseMessage.IsSuccessStatusCode)
                    {
                        var responseData = responseMessage2.Content.ReadAsStringAsync().Result;
                        var menubaseAccess = JsonConvert.DeserializeObject<IEnumerable<MenubaseAccess>>(responseData);
                        authorize = menubaseAccess.Count() > 0;
                        if (authorize)
                        {                            
                            break;
                        }
                    }
                }
                
            }
            return authorize;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = new HttpUnauthorizedResult();
        }

    }    
    
}