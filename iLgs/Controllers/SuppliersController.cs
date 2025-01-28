using iLgs.Models;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Newtonsoft.Json;
using System.Linq;
using System.Web.Mvc;


namespace iLgs.Controllers
{
    public class SuppliersController : BaseController
    {
        private AppManEntities _db;

        public SuppliersController()
        {
            _db = new AppManEntities();
        }
        // GET: Suppliers
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult SupplierRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = _db.Database.SqlQuery<SupplierVM>("Exec Supplier_GetAll").AsQueryable();            
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> SupplierCreate([DataSourceRequest] DataSourceRequest request, Supplier model)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "suppliers");
        //        Access access = await accessTask;
        //        if (!access.AllowAdd)
        //        {
        //            ModelState.AddModelError("", "Add Access Denied!");
        //        }

        //        if (model != null && ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            model.Id = Guid.NewGuid();
        //            model.InsertedBy = user;
        //            model.InsertedDt = date;
        //            model.UpdatedBy = user;
        //            model.UpdatedDt = date;
                    
        //            _db.Suppliers.Add(model);
        //            await _db.SaveChangesAsync();                    
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
        //public async Task<ActionResult> SupplierUpdate([DataSourceRequest] DataSourceRequest request, Supplier model)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "suppliers");
        //        Access access = await accessTask;
        //        if (!access.AllowEdit)
        //        {
        //            ModelState.AddModelError("", "Update Access Denied!");
        //        }

        //        if (ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            model.UpdatedBy = user;
        //            model.UpdatedDt = date;
                    
        //            _db.Suppliers.Attach(model);
        //            _db.Entry(model).State = EntityState.Modified;
        //            await _db.SaveChangesAsync();
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
        //public async Task<ActionResult> SupplierDestroy([DataSourceRequest]DataSourceRequest request, Supplier model)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "suppliers");
        //        Access access = await accessTask;
        //        if (!access.AllowDelete)
        //        {
        //            ModelState.AddModelError("DeleteError", "Delete Access Denied!");
        //        }
        //        else
        //        {
        //            _db.Suppliers.Attach(model);
        //            // Delete the entity
        //            _db.Suppliers.Remove(model);
        //            // Or use DeleteObject if using a previous version of Entity Framework
        //            // Delete the entity in the database
        //            //db.Entry(model).State = System.Data.EntityState.Deleted;
        //            await _db.SaveChangesAsync();
        //            //db.Configuration.ValidateOnSaveEnabled = true;                
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
        //             "please contact tech support with this message: " + e.Message);
        //    }

        //    return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        //}
    }    
}