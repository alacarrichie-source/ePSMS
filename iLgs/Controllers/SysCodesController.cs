using iLgs.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Mvc;
using Kendo.Mvc.Extensions;
using Microsoft.AspNet.Identity;
using Kendo.Mvc.UI;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using iLgs.Utilities;

namespace iLgs.Controllers
{
    public class SysCodesController : BaseController
    {

        private AppManEntities db = new AppManEntities();

        //HttpClient client;
        ////The URL of the WEB API Service
        ////string iLgsApiUrl = ConfigurationManager.AppSettings["APPMAN_API_URL"];
        //string iLgsApiUrl = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["APPMAN_API_URL"].ToString()).DataSource;


        //public SysCodesController()
        //{
        //    client = new HttpClient();
        //    client.BaseAddress = new Uri(iLgsApiUrl);
        //    client.DefaultRequestHeaders.Accept.Clear();
        //    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //}

        //// GET: SysCodes
        //public ActionResult Index()
        //{
        //    var userId = User.Identity.GetUserId();
        //    ViewBag.IsNotAdmin = !db.AspNetUserRoles.Where(w => w.UserId == userId && w.RoleId == "admin").Any();
        //    return View();
        //}

        //public ActionResult SysCodesRead([DataSourceRequest] DataSourceRequest request)
        //{
        //    var model = db.SysCodes.Select(s => new { SysCode1 = s.SysCode1, SysDescription = s.SysDescription, SysCodeOld = s.SysCode1 });
        //    return Json(model.ToDataSourceResult(request));


        //}

        //[AcceptVerbs(HttpVerbs.Post)]
        //public ActionResult SysCodesCreate([DataSourceRequest] DataSourceRequest request, SysCode model)
        //{
        //    try
        //    {
        //        if (model != null && ModelState.IsValid)
        //        {

        //            db.SysCodes.Add(model);
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

        //[AcceptVerbs(HttpVerbs.Post)]
        //public ActionResult SysCodesUpdate([DataSourceRequest] DataSourceRequest request, SysCode model)
        //{
        //    try
        //    {
        //        if (ModelState.IsValid)
        //        {

        //            //string code = model.SysCode1;
        //            //var myObjectState = ((IObjectContextAdapter)db).ObjectContext.ObjectStateManager.GetObjectStateEntry(model);
        //            //var modifiedProperties = myObjectState.GetModifiedProperties();
        //            //foreach (var propName in modifiedProperties)
        //            //{
        //            //    //Console.WriteLine("Property {0} changed from {1} to {2}",
        //            //    //     propName,
        //            //    //     myObjectState.OriginalValues[propName],
        //            //    //     myObjectState.CurrentValues[propName]);
        //            //    if (propName == "SysCode1")
        //            //    {
        //            //        code = myObjectState.OriginalValues[propName].ToString();
        //            //    }

        //            //}

        //            var entity = db.SysCodes.Find(model.SysCode1);
        //            //entity.SysCode1 = model.SysCode1;
        //            entity.SysDescription = model.SysDescription;

        //            db.SysCodes.Attach(entity);
        //            db.Entry(entity).State = EntityState.Modified;
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

        //[AcceptVerbs(HttpVerbs.Post)]
        //public ActionResult SysCodesDestroy([DataSourceRequest]DataSourceRequest request, SysCode model)
        //{
        //    try
        //    {
        //        if (ModelState.IsValid)
        //        {
        //            // Attach the entity
        //            db.SysCodes.Attach(model);
        //            // Delete the entity
        //            db.SysCodes.Remove(model);
        //            // Or use DeleteObject if using a previous versoin of Entity Framework
        //            // Delete the entity in the database
        //            //db.Entry(model).State = System.Data.EntityState.Deleted;
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

        public ActionResult GetSysCodes(string text)
        {

            string userId = User.Identity.GetUserId();
            IEnumerable<Codextn> model = Enumerable.Empty<Codextn>().AsQueryable();
            HttpResponseMessage responseMessage = client.GetAsync("syscodes_/" + userId).Result;
            if (responseMessage.IsSuccessStatusCode)
            {
                var responseData = responseMessage.Content.ReadAsStringAsync().Result;
                model = JsonConvert.DeserializeObject<List<Codextn>>(responseData);
                if (!string.IsNullOrEmpty(text))
                {
                    model = model.Where(w => w.Code.Contains(text));
                }
            }

            var retVal = model.Select(c => new { Code = c.Code, Description = c.Description }).ToList();
            retVal.Add(new { Code = "", Description = "" });

            return Json(retVal.OrderBy(o => o.Code), JsonRequestBehavior.AllowGet);

        }



    }
}