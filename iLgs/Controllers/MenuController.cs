using iLgs.Models;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Net.Http;
using System.Configuration;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using iLgs.Utilities;
using Microsoft.AspNet.Identity;
using System.Data.SqlClient;

namespace iLgs.Controllers
{

    [iLGSAuthorize("Menu")]
    public class MenuController : BaseController
    {

        private AppManEntities db = new AppManEntities();


        HttpClient client;

        //The URL of the WEB API Service
        //string iLgsApiUrl = ConfigurationManager.AppSettings["APPMAN_API_URL"];
        string iLgsApiUrl = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["APPMAN_API_URL"].ToString()).DataSource;

        //string iLgsApiUrl = "http://localhost:3684/";
        //The HttpClient Class, this will be used for performing 
        //HTTP Operations, GET, POST, PUT, DELETE
        //Set the base address and the Header Formatter
        public MenuController()
        {
            client = new HttpClient();
            client.BaseAddress = new Uri(iLgsApiUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
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

                //    Menubase menubase = new Menubase()
                //    {
                //        SysCode = model.SysCode,
                //        Sequence = model.Sequence,
                //        ParentId = model.ParentId,
                //        ChildId = model.ChildId,
                //        Description = model.Description,
                //        Action = model.Action,
                //        Controller = model.Controller,
                //        ObjectParam = model.ObjectParam,
                //        MenuId = model.MenuId,
                //        InsertedBy = model.InsertedBy,
                //        InsertedDt = model.InsertedDt
                //    };

                //    db.Menubases.Add(menubase);
                //    await db.SaveChangesAsync();

                //    model = db.Menubases.Where(w => w.ChildId == model.ChildId)
                //        .Select(s => new Menubase_VM
                //        {
                //            SysCode = s.SysCode,
                //            Sequence = s.Sequence,
                //            ParentId = s.ParentId,
                //            Description = s.Description,
                //            Action = s.Action,
                //            Controller = s.Controller,
                //            ObjectParam = s.ObjectParam,
                //            MenuId = s.MenuId
                //        }).SingleOrDefault();
                //}

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

                    //Menubase menubase = db.Menubases.Find(model.ChildId);
                    //menubase.SysCode = model.SysCode;
                    //menubase.Sequence = model.Sequence;
                    //menubase.ParentId = model.ParentId;
                    //menubase.Description = model.Description;
                    //menubase.Action = model.Action;
                    //menubase.Controller = model.Controller;
                    //menubase.ObjectParam = model.ObjectParam;
                    //menubase.MenuId = model.MenuId;
                    //menubase.UpdatedBy = model.UpdatedBy;
                    //menubase.UpdatedDt = model.UpdatedDt;
                    ;

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
                    
                    //Menubase menubase = db.Menubases.Find(model.ChildId);
                    //db.Menubases.Remove(menubase);
                    //await db.SaveChangesAsync();
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


        

    }
}