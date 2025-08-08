using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.CustodianDisposal_;
using iLgs.Services.CustodianReports;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    public class CustodianDisposalController : BaseController
    {
        private readonly AppManEntities _db;
        private readonly ICustodianReportService _custodianReportService;
        private readonly ICustodianReportItemService _custodianReportItemService;
        private readonly ICustodianDisposalService _custodianDisposalService;
        private readonly ICustodianDisposalItemService _custodianDisposalItemService;
        private readonly ICodextnService _codextnService;

        public CustodianDisposalController(AppManEntities db, 
            ICustodianReportService custodianReportService, ICustodianReportItemService custodianReportItemService,
            ICustodianDisposalService custodianDisposalService, ICustodianDisposalItemService custodianDisposalItemService,
            ICodextnService codextnService)
        {
            _db = db;
            _custodianReportService = custodianReportService;
            _custodianReportItemService = custodianReportItemService;
            _custodianDisposalService = custodianDisposalService;
            _custodianDisposalItemService = custodianDisposalItemService;
            _codextnService = codextnService;
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Read([DataSourceRequest] DataSourceRequest request, Guid? deptId)
        {
            var data = _custodianDisposalService.GetAllByDeptId(deptId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Create([DataSourceRequest] DataSourceRequest request, CustodianDisposal model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_disposal");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Add Access Denied!");
                }


                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianDisposalService.CreateAsync(model, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Update([DataSourceRequest] DataSourceRequest request, CustodianDisposal model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_disposal");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianDisposalService.UpdateAsync(model, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Destroy([DataSourceRequest]DataSourceRequest request, CustodianDisposal model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_disposal");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianDisposalService.DeleteAsync(model, user, date);
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

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Post(Guid id)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_disposal");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("GridError", "Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _custodianDisposalService.PostAsync(id, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
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

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> UnPost(Guid id)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_disposal");
                Access access = await accessTask;
                if (!access.AllowUnpost)
                {
                    ModelState.AddModelError("GridError", "Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _custodianDisposalService.UnPostAsync(id, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
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

        public ActionResult _Item(Guid disposalId)
        {            
            ViewData["disposalId"] = disposalId;
            return PartialView();
        }

        public ActionResult _ItemAddEdit(Guid disposalId, Guid deptId)
        {                        
            var data = new CustodianDisposalItem();
            data.Id = Guid.NewGuid();
            data.CustodianDisposalId = disposalId;

            ViewData["disposalId"] = disposalId;
            ViewData["deptId"] = deptId;
            return PartialView(data);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ItemSave(CustodianDisposalItem model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_disposal");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                
                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianDisposalItemService.SaveAsync(model, user, date);
                    
                    //var orderItemExtns = (List<OrderItemExtnVM>)Newtonsoft.Json.JsonConvert.DeserializeObject(model.GridOrderItemExtns, typeof(List<OrderItemExtnVM>));
                    //await orderItemExtnService.SaveAsync(model.Id, orderItemExtns, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
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

        public ActionResult _AvaialbleItemRead([DataSourceRequest] DataSourceRequest request, int? forYear, Guid? deptId)
        {
            var data = _custodianReportItemService.GetAvailableItemsForDisposal(forYear, deptId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        public ActionResult _ItemRead([DataSourceRequest] DataSourceRequest request, Guid? disposalId)
        {
            var data = _custodianDisposalItemService.GetAllByCustodianDisposalId(disposalId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }        
        
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ItemDestroy([DataSourceRequest]DataSourceRequest request, CustodianDisposalItem model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_disposal");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianDisposalItemService.DeleteAsync(model, user, date);
                    // TO DO: update stocks
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
        
        [HttpPost]
        public async Task<JsonResult> VerifyDepartment(Guid? id)
        {
            var data = await _custodianReportService.GetByIdAsync(id);
            if (data == null)
            {
                return Json(new { Errors = "No custodian record found for this department!", Id = data }, JsonRequestBehavior.DenyGet);
            }
            else
            {
                return Json(new { Errors = "", Id = data.Id }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}