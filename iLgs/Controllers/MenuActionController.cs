using iLgs.Models;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    public class MenuActionController : Controller
    {
        private static string sysCode = "PSMS";
        private static string sysAdmin = "PSMS_ADMIN";

        private readonly AppManEntities _db;


        HttpClient client;

        //The URL of the WEB API Service
        //string iLgsApiUrl = ConfigurationManager.AppSettings["APPMAN_API_URL"];
        string iLgsApiUrl = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["APPMAN_API_URL"].ToString()).DataSource;

        //string iLgsApiUrl = "http://localhost:3684/";
        //The HttpClient Class, this will be used for performing 
        //HTTP Operations, GET, POST, PUT, DELETE
        //Set the base address and the Header Formatter
        public MenuActionController(AppManEntities db)
        {
            _db = db;
            client = new HttpClient();
            client.BaseAddress = new Uri(iLgsApiUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        
        public async Task<ActionResult> MenuActionRead([DataSourceRequest] DataSourceRequest request, string sysCode, int? menuId)
        {
            menuId = menuId ?? 0;
            HttpResponseMessage responseMessage = await client.GetAsync("MenuAction/" + sysCode + "/" + menuId);
            IEnumerable<MenuActionSw> model = Enumerable.Empty<MenuActionSw>().AsQueryable();
            if (responseMessage.IsSuccessStatusCode)
            {
                var responseData = responseMessage.Content.ReadAsStringAsync().Result;
                model = JsonConvert.DeserializeObject<List<MenuActionSw>>(responseData);

            }

            return Json(model.ToDataSourceResult(request));
        }        

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> MenuActionSet([DataSourceRequest] DataSourceRequest request, MenuActionSw model, string sysCode, int menuId, string actionCode)
        {
            try
            {
                var rec = _db.MenuActions.Where(w => w.MenuId == menuId && w.Menubase.SysCode == sysCode && w.ActionCode == actionCode).FirstOrDefault();

                if (rec == null)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime? date = DateTime.Now;

                    MenuAction action = new MenuAction()
                    {
                        Id = Guid.NewGuid(),
                        MenuId = menuId,
                        ActionCode = actionCode,
                        InsertedBy = user,
                        InsertedDt = date.Value,
                        UpdatedBy = user,
                        UpdatedDt = date.Value,
                    };

                    _db.MenuActions.Add(action);
                    await _db.SaveChangesAsync();
                    model.IsAllowed = true;
                }
                else
                {

                    // Attach the entity
                    _db.MenuActions.Attach(rec);
                    // Delete the entity
                    _db.MenuActions.Remove(rec);
                    // Or use DeleteObject if using a previous versoin of Entity Framework
                    // Delete the entity in the database
                    //db.Entry(model).State = System.Data.EntityState.Deleted;
                    await _db.SaveChangesAsync();                    
                    model.IsAllowed = false;
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
    }
}