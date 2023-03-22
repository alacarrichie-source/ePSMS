using iLgs.Models;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using Microsoft.AspNet.Identity;
using iLgs.Utilities;
using Newtonsoft.Json;

namespace iLgs.Controllers
{
    [AppAuthorize("ITEMS")]
    public class ItemsController : Controller
    {
        private AppManEntities db = new AppManEntities();
        // GET: Codes
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ItemRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = db.PsCodes.AsQueryable();            
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> ItemCreate([DataSourceRequest] DataSourceRequest request, PsCode model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "items");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Add Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model.Id = Guid.NewGuid();
                    model.InsertedBy = user;
                    model.InsertedDt = date;
                    model.UpdatedBy = user;
                    model.UpdatedDt = date;
                    model.ItemDescription = model.ItemDescription ?? "";
                    model.ReorderPoint = model.ReorderPoint ?? 0;
                   
                    db.PsCodes.Add(model);
                    await db.SaveChangesAsync();                    
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
        public async Task<ActionResult> ItemUpdate([DataSourceRequest] DataSourceRequest request, PsCode model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "items");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model.UpdatedBy = user;
                    model.UpdatedDt = date;
                    model.PsType = model.PsType.ToUpper();
                    model.ItemDescription = model.ItemDescription ?? "";
                    model.ReorderPoint = model.ReorderPoint ?? 0;
                    
                    db.PsCodes.Attach(model);
                    db.Entry(model).State = EntityState.Modified;
                    await db.SaveChangesAsync();
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
        public async Task<ActionResult> ItemDestroy([DataSourceRequest]DataSourceRequest request, PsCode model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "items");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    db.PsCodes.Attach(model);
                    // Delete the entity
                    db.PsCodes.Remove(model);
                    // Or use DeleteObject if using a previous version of Entity Framework
                    // Delete the entity in the database
                    //db.Entry(model).State = System.Data.EntityState.Deleted;
                    await db.SaveChangesAsync();
                    //db.Configuration.ValidateOnSaveEnabled = true;                
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }
    }
}