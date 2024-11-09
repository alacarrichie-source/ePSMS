using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using iLgs.Agents.Services;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services;
using iLgs.Services.AllFields;
using iLgs.Services.Interfaces;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static iLgs.Models.Enums;

namespace iLgs.Controllers
{
    [AppAuthorize("RIS")]
    public class RISController : BaseController
    {
        private readonly AppManEntities _db;
        private readonly IRisService _risService;
        private readonly IRisItemService _risItemService;
        private readonly IRisItemUnitGroupService _risItemUnitGroupService;
        private readonly IRisItemUnitGroupDescriptionService _risItemUnitGroupDescriptionService;
        private readonly IRisItemUnitGroupDescriptionItemService _risItemUnitGroupDescriptionItemService;
        private readonly ICodextnService _codextnService;
        private readonly IAllFieldService _allFieldService;

        public RISController()
        {
            _db = new AppManEntities();
            _risService = new RisService(_db);
            _risItemService = new RisItemService(_db);
            _risItemUnitGroupService = new RisItemUnitGroupService(_db);
            _risItemUnitGroupDescriptionService = new RisItemUnitGroupDescriptionService(_db);
            _risItemUnitGroupDescriptionItemService = new RisItemUnitGroupDescriptionItemService(_db);
            _codextnService = new CodextnService(_db);
            _allFieldService = new AllFieldService(_db);
        }

        // GET: RIS
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult RISRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = _risService.GetAll();

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> RISCreate([DataSourceRequest] DataSourceRequest request, RIS_VM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Add Access Denied!");
                }
                
                if (model != null && ModelState.IsValid)
                {                    
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _risService.CreateAsync(model, user, date);                    
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
        public async Task<ActionResult> RISUpdate([DataSourceRequest] DataSourceRequest request, RIS_VM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Update Access Denied!");
                }                

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _risService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> RISDestroy([DataSourceRequest]DataSourceRequest request, RIS_VM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else { 
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _risService.DeleteAsync(model, user, date);                    
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
        public async Task<ActionResult> PostRIS(Guid risId)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }                

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _risService.PostAsync(risId, user, date);
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

        [HttpPost]
        public async Task<ActionResult> UnpostRIS(Guid risId)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _risService.UnpostAsync(risId, user, date);
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

        #region ITEMS
        public ActionResult _RISItem(Guid risId)
        {
            ViewData["risId"] = risId;
            return PartialView();
        }
        public async Task<ActionResult> _RISItemAddEdit(Guid risId, Guid? risItemId)
        {
            var data = await _risItemService.GetEntryVmByIdAsync(risItemId);
            if (data == null)
            {
                data = new RisItemEntryVM()
                {
                    RisId = risId
                };
            }
            ViewData["risItemId"] = risItemId;
            return PartialView(data);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _RISItemSave(RisItemEntryVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowAdd || !access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                
                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;
                    
                    var entity = await _risItemService.GetByIdAsync(model.Id);

                    if (entity == null)
                    {
                        model = await _risItemService.CreateAsync(model, user, date);
                    }
                    else
                    {
                        model = await _risItemService.UpdateAsync(model, user, date);
                    }
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

            //var query = from state in ModelState.Values
            //            from error in state.Errors
            //            select error.ErrorMessage;

            //var errorList = query.ToList();
            //if (errorList.Count() > 0)
            //{
            //    return Json(new { Errors = errorList }, JsonRequestBehavior.DenyGet);
            //}

            var errorList = ModelState.Where(ms => ms.Value.Errors.Any())
                       .Select(ms => new
                       {
                           Key = ms.Key, // The field name
                           Message = ms.Value.Errors.Select(e =>
                           {
                               var errorMessage = e.ErrorMessage;
                               if (e.Exception != null)
                               {
                                   var exceptionMessage = e.Exception.Message;
                                   var innerExceptionMessage = e.Exception.InnerException?.Message;

                                   // Append exception details
                                   errorMessage += $" Exception: {exceptionMessage}";
                                   if (innerExceptionMessage != null)
                                   {
                                       errorMessage += $" InnerException: {innerExceptionMessage}";
                                   }
                               }

                               return errorMessage;
                           }).ToList() // List of messages for the current field
                       })
                       .ToList();

            if (errorList.Any())
            {
                return Json(new { Errors = errorList }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Errors = "", Id = model.Id }, JsonRequestBehavior.AllowGet);
        }


        public ActionResult _RISItemRead([DataSourceRequest] DataSourceRequest request, Guid? risId)
        {
            var data = _risItemService.GetByRisId(risId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _RISItemDestroy([DataSourceRequest]DataSourceRequest request, RisItemEntryVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _risItemService.DeleteAsync(model, user, date);
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
        #endregion

        #region UNIT GROUP
        public ActionResult _RISItemUnitGroup(Guid risId)
        {
            ViewData["risId"] = risId;
            return PartialView();
        }

        public ActionResult _RISItemUnitGroupRead([DataSourceRequest] DataSourceRequest request, Guid? risId)
        {
            var data = _risItemUnitGroupService.GetByRisId(risId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _RISItemUnitGroupCreate([DataSourceRequest] DataSourceRequest request, RisItemUnitGroupVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("AddError", "Add Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _risItemUnitGroupService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> _RISItemUnitGroupUpdate([DataSourceRequest] DataSourceRequest request, RisItemUnitGroupVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _risItemUnitGroupService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> _RISItemUnitGroupDestroy([DataSourceRequest]DataSourceRequest request, RisItemUnitGroupVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _risItemUnitGroupService.DeleteAsync(model, user, date);
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

        #endregion

        #region UNIT GROUP DESCRIPTION
        public ActionResult _RISItemUnitGroupDescription(Guid unitGroupId)
        {
            ViewData["unitGroupId"] = unitGroupId;
            return PartialView();
        }

        public ActionResult _RISItemUnitGroupDescriptionRead([DataSourceRequest] DataSourceRequest request, Guid? unitGroupId)
        {
            var data = _risItemUnitGroupDescriptionService.GetByUnitGroupId(unitGroupId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _RISItemUnitGroupDescriptionCreate([DataSourceRequest] DataSourceRequest request, RisItemUnitGroupDescriptionVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("AddError", "Add Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _risItemUnitGroupDescriptionService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> _RISItemUnitGroupDescriptionUpdate([DataSourceRequest] DataSourceRequest request, RisItemUnitGroupDescriptionVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _risItemUnitGroupDescriptionService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> _RISItemUnitGroupDescriptionDestroy([DataSourceRequest]DataSourceRequest request, RisItemUnitGroupDescriptionVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _risItemUnitGroupDescriptionService.DeleteAsync(model, user, date);
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

        #endregion

        #region UNIT GROUP DESCRIPTION ITEMS
        public ActionResult _RISItemUnitGroupDescriptionItem(Guid unitGroupDescriptionId)
        {
            ViewData["unitGroupDescriptionId"] = unitGroupDescriptionId;
            return PartialView();
        }

        public ActionResult _RISItemUnitGroupDescriptionItemRead([DataSourceRequest] DataSourceRequest request, Guid? unitGroupDescriptionId)
        {
            var data = _risItemUnitGroupDescriptionItemService.GetByUnitGroupDescriptionId(unitGroupDescriptionId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }
        public ActionResult _RISItemAvailableUnitGroupItemRead([DataSourceRequest] DataSourceRequest request, Guid? risId)
        {
            var data = _risItemUnitGroupDescriptionItemService.GetAvailableUnitGroupItem(risId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _RISItemUnitGroupDescriptionItemCreate([DataSourceRequest] DataSourceRequest request, RisItemUnitGroupDescriptionItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("AddError", "Add Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _risItemUnitGroupDescriptionItemService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> _RISItemUnitGroupDescriptionItemUpdate([DataSourceRequest] DataSourceRequest request, RisItemUnitGroupDescriptionItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _risItemUnitGroupDescriptionItemService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> _RISItemUnitGroupDescriptionItemDestroy([DataSourceRequest]DataSourceRequest request, RisItemUnitGroupDescriptionItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _risItemUnitGroupDescriptionItemService.DeleteAsync(model, user, date);
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

        #endregion

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> LoadFields([System.Web.Http.FromBody] RisItemEntryVM model)
        {
            if (model.Id != Guid.Empty)
            {
                var allField = await _allFieldService.GetByIdAsync(model.Id);
                if (allField != null)
                {
                    model.AllField = allField;
                }
            }
            string partialView = "";
            if (Enum.TryParse(model.PsType, out Category c))
            {
                if (c == CatLands())
                {
                    partialView = "_FieldLand";
                }
                else if (c == CatMachineries() 
                    || c == CatTransportations() 
                    || c == CatFurnitures() 
                    || c == CatOtherProperties()
                    || c == CatMedicals() 
                    || c == CatAgriculturals() 
                    || c == CatAnimalSupplies() 
                    || c == CatConstructionMaterials()
                    || c == CatOfficeSupplies() 
                    || c == CatAccountableForms() 
                    || c == CatNonAccountableForns() 
                    || c == CatMilitaries()
                    || c == CatOtherSupplies())
                {
                    partialView = "_FieldBrand";
                }
                else if (c == CatDrugs())
                {
                    partialView = "_FieldDrugs";
                }
                else if (c == CatRepairs())
                {
                    partialView = "_FieldSerial";
                }
            }
            return PartialView(partialView, model);
        }

        //[Authorize]
        //public ActionResult _RISItemExtnBatchRead([DataSourceRequest] DataSourceRequest request, Guid? risItemId, string psType)
        //{
        //    var data = _sa.RisItemExtn.GetBatchInfo(risItemId, psType);
        //    var result = new JsonNetResult
        //    {
        //        Data = data.ToDataSourceResult(request),
        //        JsonRequestBehavior = JsonRequestBehavior.AllowGet,
        //        Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
        //    };

        //    return result;
        //}

        public async Task<ActionResult> RISRpt(string risNo)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "report_ris");
                Access access = await accessTask;
                if (access == null)
                {
                    throw new Exception("Access Denied!");
                }

            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                return View("Error");
            }

            Sections crSections;
            ReportDocument rpt, crSubreportDocument;
            SubreportObject crSubreportObject;
            ReportObjects crReportObjects;
            ConnectionInfo crConnectionInfo;
            CrystalDecisions.CrystalReports.Engine.Database crDatabase;
            Tables crTables;
            TableLogOnInfo crTableLogOnInfo;
            rpt = new ReportDocument();
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/Ris.rpt"));
            rpt.Refresh();

            string user = ControllerContext.HttpContext.User.Identity.Name;
            string conString = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(conString);

            string un = decoder.UserID;
            string pw = decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            crDatabase = rpt.Database;
            crTables = crDatabase.Tables;
            crConnectionInfo = new ConnectionInfo();
            crConnectionInfo.ServerName = svr;
            crConnectionInfo.DatabaseName = db_;
            crConnectionInfo.UserID = un;
            crConnectionInfo.Password = pw;

            foreach (CrystalDecisions.CrystalReports.Engine.Table aTable in crTables)
            {
                crTableLogOnInfo = aTable.LogOnInfo;
                crTableLogOnInfo.ConnectionInfo = crConnectionInfo;
                aTable.ApplyLogOnInfo(crTableLogOnInfo);
            }
            // THIS STUFF HERE IS FOR REPORTS HAVING SUBREPORTS 
            // set the sections object to the current report's section 
            crSections = rpt.ReportDefinition.Sections;
            // loop through all the sections to find all the report objects 
            foreach (CrystalDecisions.CrystalReports.Engine.Section crSection in crSections)
            {
                crReportObjects = crSection.ReportObjects;
                //loop through all the report objects in there to find all subreports 
                foreach (ReportObject crReportObject in crReportObjects)
                {
                    if (crReportObject.Kind == ReportObjectKind.SubreportObject)
                    {
                        crSubreportObject = (SubreportObject)crReportObject;
                        //open the subreport object and logon as for the general report 
                        crSubreportDocument = crSubreportObject.OpenSubreport(crSubreportObject.SubreportName);
                        crDatabase = crSubreportDocument.Database;
                        crTables = crDatabase.Tables;
                        foreach (CrystalDecisions.CrystalReports.Engine.Table aTable in crTables)
                        {
                            crTableLogOnInfo = aTable.LogOnInfo;
                            crTableLogOnInfo.ConnectionInfo = crConnectionInfo;
                            aTable.ApplyLogOnInfo(crTableLogOnInfo);
                        }
                    }
                }
            }

            var lgu = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault().Description;

            rpt.SetParameterValue("@cRisNo", risNo);
            rpt.SetParameterValue("LGU", lgu);
            
            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");            
        }

        public ActionResult Requisition()
        {
            return View();
        }
        
        public ActionResult Cart(List<PsCodeVM> cartItems)
        {
            //var cartItems = JsonConvert.DeserializeObject<List<PsCodeVM>>(localStorage.getItem("cartItems")) ?? new List<OrderItem>();
            return View(cartItems);
        }
        
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult GetDescription(RisItemEntryVM entry)
        {
            var description = _risItemService.GetDescription(entry);

            return Json(new { Description = description }, JsonRequestBehavior.AllowGet);
        }
    }
}