using iLgs.Models;
using iLgs.Services;
using iLgs.Services.Interfaces;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    public class ParItemsController : Controller
    {
        private AppManEntities db = new AppManEntities();
        private IParService parService;
        private IParItemService parItemService;

        public ParItemsController()
        {
            this.parService = new ParService(db);
            this.parItemService = new ParItemService(db);
        }

        public ActionResult _Item(Guid parId)
        {
            ViewData["parId"] = parId;
            return PartialView();
        }

        public async Task<ActionResult> _ItemAddEdit(Guid parId, Guid? parItemId)
        {
            var data = await parItemService.GetVmByIdAsync(parItemId);
            if (data == null)
            {
                data = new PARItemVM()
                {
                    Id = Guid.NewGuid(),
                    ParId = parId
                };
            }
            ViewData["parItemId"] = parItemId;
            return PartialView(data);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Save(PARItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "pars");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await parService.IsPostedAsync((Guid)model.ParId))
                {
                    ModelState.AddModelError("PAR No.", "PAR Number already Posted, cannot update!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    var entity = await parItemService.GetByIdAsync(model.Id);

                    if (entity == null)
                    {
                        model = await parItemService.CreateAsync(model, user, date);
                    }
                    else
                    {
                        model = await parItemService.UpdateAsync(model, user, date);
                    }                    
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            var query = from state in ModelState.Values
                        from error in state.Errors
                        select error.ErrorMessage;

            var errorList = query.ToList();
            if (errorList.Count() > 0)
            {
                return Json(new { Errors = errorList }, JsonRequestBehavior.DenyGet);
            }

            return Json(new { Errors = "" }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Read([DataSourceRequest] DataSourceRequest request, Guid? parId)
        {
            var data = parItemService.GetAll(parId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }


        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Destroy([DataSourceRequest]DataSourceRequest request, PARItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "pars");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else if (await parService.IsPostedAsync((Guid)model.ParId))
                {
                    ModelState.AddModelError("DeleteError", "PAR Number already Posted, cannot update!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await parItemService.DeleteAsync(model, user, date);
                }

            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }
    }
}