using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.ParIcs;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    public class ParItemsController : BaseController
    {
        private readonly IIcsParService _icsParService;
        private readonly IParService _parService;

        public ParItemsController(IIcsParService icsParService, IParService parService)
        {
            _icsParService = icsParService;
            _parService = parService;
        }

        public ActionResult _Item(Guid parId)
        {
            ViewData["parId"] = parId;
            return PartialView();
        }

        public async Task<ActionResult> _ItemAddEdit(Guid parId, Guid? parItemId)
        {
            var data = await _parService.ParItem.GetVmByIdAsync(parItemId);
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
        
        public ActionResult Read([DataSourceRequest] DataSourceRequest request, Guid? parId)
        {
            var data = _parService.ParItem.GetAll(parId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Destroy([DataSourceRequest]DataSourceRequest request, PARItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "pars");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _parService.ParItem.DeleteAsync(model, user, date);
                }

            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("DeleteError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }
    }
}