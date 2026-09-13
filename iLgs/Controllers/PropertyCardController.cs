using CrystalDecisions.CrystalReports.Engine;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.Items;
using iLgs.Services.PropertyCard;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using static iLgs.Models.Enums;

namespace iLgs.Controllers
{
    [AppAuthorize("PROPERTYCARD")]
    public class PropertyCardController : BaseController
    {
        private readonly string _cardCategory = "P";
        //private readonly AppManEntities _db;
        private readonly ICodextnService _codextnService;
        private readonly IPropertyCardService _propertyCardService;
        private readonly IItemCodeService _itemCodeService;
        private readonly IPropertyCardValidator _propertyCardValidator;

        public PropertyCardController()
        {
            //_db = new AppManEntities();
            _codextnService = new CodextnService(_db);
            _propertyCardService = new PropertyCardService(_db);
            _itemCodeService = new ItemCodeService(_db);
            _propertyCardValidator = new PropertyCardValidator(_db);
        }

        
        // GET: Index
        public ActionResult Index()
        {
            return View();
        }


        private PropertyCardWorkspaceService WorkspaceService()
        {
            return new PropertyCardWorkspaceService(_db, _propertyCardService.PsCardItem);
        }

        private bool IsWorkspaceCard(Guid id)
        {
            return _db.PsCards.Any(x => x.Id == id && x.CardCategory == "P");
        }

        public ActionResult Workspace(Guid id)
        {
            var model = WorkspaceService().Get(id);
            if (model == null) return HttpNotFound();
            return View(model);
        }

        public ActionResult WorkspacePosition(Guid id)
        {
            if (!IsWorkspaceCard(id)) return HttpNotFound();
            return Json(WorkspaceService().Position(id), JsonRequestBehavior.AllowGet);
        }

        public ActionResult WorkspaceTab(Guid id, string tab)
        {
            if (!IsWorkspaceCard(id)) return HttpNotFound();
            ViewData["cardId"] = id;
            switch (tab)
            {
                case "Acquisitions": return PartialView("_PropertyCardItem");
                case "Individual Units": return PartialView("_WorkspaceUnits");
                case "Accountability": return PartialView("_WorkspaceAccountability");
                case "History": return PartialView("_WorkspaceHistory");
                case "Documents": return PartialView("_WorkspaceDocuments");
                default: return HttpNotFound();
            }
        }

        public ActionResult WorkspaceHistoryRead([DataSourceRequest] DataSourceRequest request, Guid id)
        {
            if (!IsWorkspaceCard(id)) return HttpNotFound();
            return new JsonNetResult { Data = WorkspaceService().History(id).ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        public ActionResult WorkspaceChoices(Guid id, string kind, string text)
        {
            if (!IsWorkspaceCard(id)) return HttpNotFound();
            IQueryable<PropertyCardUnitChoiceVM> choices;
            if (kind == "acquisitions")
            {
                choices = _db.PsCardItemTransfers.Where(x => x.PsCardItem.PsCardId == id)
                    .Select(x => new PropertyCardUnitChoiceVM {
                        Id = x.Id, AcquisitionId = x.PsCardItemId, PoNo = x.PsCardItem.PoNo,
                        Label = (x.PsCardItem.PoNo ?? "No PO") + " / " + (x.Codextn.Code ?? "Origin") + " / " + (x.Codextn.Description ?? "")
                    });
            }
            else
            {
                choices = WorkspaceService().Units(id);
                if (kind == "documents")
                {
                    var acquisitions = _db.PsCardItems.Where(x => x.PsCardId == id)
                        .Select(x => new PropertyCardUnitChoiceVM {
                            Id = x.GroupId ?? x.Id, AcquisitionId = x.Id, PoNo = x.PoNo,
                            PropNo = null, CustItemNo = null, Label = "Acquisition / " + (x.PoNo ?? "No PO"), Location = null, Condition = null
                        });
                    choices = choices.Concat(acquisitions);
                }
            }
            if (!string.IsNullOrWhiteSpace(text)) choices = choices.Where(x => x.Label.Contains(text));
            return Json(choices.OrderBy(x => x.Label).Take(100).ToList(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult WorkspaceUnits(Guid id, Guid transferId)
        {
            if (!IsWorkspaceCard(id)) return HttpNotFound();
            var row = _propertyCardService.PsCardItem.GetTransitByCardId(id, null).FirstOrDefault(x => x.TransferId == transferId);
            if (row == null) return HttpNotFound();
            var template = _propertyCardService.GetItemExtnName(row.Id);
            return new JsonNetResult { Data = new { Row = row, Template = template }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        public ActionResult WorkspaceAccountability(Guid id, Guid unitId)
        {
            if (!IsWorkspaceCard(id) || !_db.PsCardItemExtns.Any(x => x.Id == unitId && x.PsCardItem.PsCardId == id)) return HttpNotFound();
            ViewData["psCardItemExtnId"] = unitId;
            ViewData["scopeLabel"] = WorkspaceService().Units(id).Where(x => x.Id == unitId).Select(x => x.Label).FirstOrDefault();
            return PartialView("_WorkspaceAccountabilityScope");
        }

        public ActionResult WorkspaceDocuments(Guid id, Guid imageId)
        {
            if (!IsWorkspaceCard(id)) return HttpNotFound();
            var unit = _db.PsCardItemExtns.Where(x => x.Id == imageId && x.PsCardItem.PsCardId == id).Select(x => x.PsCardItemId).FirstOrDefault();
            var acquisition = _db.PsCardItems.Where(x => x.PsCardId == id && (x.Id == imageId || x.GroupId == imageId)).Select(x => (Guid?)x.Id).FirstOrDefault();
            if (!unit.HasValue && !acquisition.HasValue) return HttpNotFound();
            ViewData["imageId"] = imageId;
            ViewData["psCardItemId"] = unit ?? acquisition;
            ViewData["scopeLabel"] = unit.HasValue
                ? WorkspaceService().Units(id).Where(x => x.Id == imageId).Select(x => x.Label).FirstOrDefault()
                : "Acquisition / " + _db.PsCardItems.Where(x => x.Id == acquisition.Value).Select(x => x.PoNo).FirstOrDefault();
            return PartialView("_WorkspaceDocumentScope");
        }
        public ActionResult Read([DataSourceRequest] DataSourceRequest request, string userName)
        {
            var data = _propertyCardService.GetAll(userName);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Create([DataSourceRequest] DataSourceRequest request, PropertyCardVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("AddError", "Add Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _propertyCardService.CreateAsync(model, user, date);                    
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
        public async Task<ActionResult> Update([DataSourceRequest] DataSourceRequest request, PropertyCardVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _propertyCardService.UpdateAsync(model, user, date);                    
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
        public async Task<ActionResult> Destroy([DataSourceRequest]DataSourceRequest request, PropertyCardVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _propertyCardService.DeleteAsync(model, user, date);                    
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

        public async Task<ActionResult> _PropertyCardAddEdit(Guid? cardId, string mode, bool? isAdmin)
        {
            var data = await _propertyCardService.GetByIdAsync(cardId);
            if (data == null)
            {
                data = new PropertyCardVM()
                {
                    CardCategory = _cardCategory
                };
            }
            
            data.Mode = mode;
            ViewBag.IsAdmin = isAdmin;

            return PartialView(data);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _PropertyCardSave(PropertyCardVM model)
        {
            string errorKey = "";
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
                Access access = await accessTask;
                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    var entity = await _propertyCardService.GetByIdAsync(model.Id);

                    if (entity == null)
                    {
                        if (access.AllowAdd)
                        {
                            model = await _propertyCardService.CreateAsync(model, user, date);                            
                        }
                        else
                        {
                            errorKey = "AddError";
                            ModelState.AddModelError(errorKey, "Access Denied!");
                        }
                    }
                    else
                    {
                        if (access.AllowEdit)
                        {
                            model = await _propertyCardService.UpdateAsync(model, user, date);                            
                        }
                        else
                        {
                            errorKey = "UpdateError";
                            ModelState.AddModelError(errorKey, "Access Denied!");
                        }
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

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<JsonResult> GetDescription(PropertyCardVM fields)
        {
            var description = _propertyCardService.GetDescription(fields);
            var stockNo = await _propertyCardService.GetStockNoAsync(fields);
            var psCard = await _propertyCardService.GetByPsNoAsync(stockNo);

            Guid id = Guid.NewGuid();
            if (psCard != null)
            {
                id = psCard.Id;
            }

            fields.PsNo = stockNo;
            bool isDuplicateStockNo = false;
            if (fields.Mode == "A")
            {
                isDuplicateStockNo = await _propertyCardValidator.IsPsNoAlreadyExistsAsync(fields, Mode.ADD);
            }
            else
            {
                isDuplicateStockNo = await _propertyCardValidator.IsPsNoAlreadyExistsAsync(fields, Mode.EDIT);
            }

            return Json(new { Description = description, StockNo = stockNo, Id = id, IsDuplicateStockNo = isDuplicateStockNo }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult _PropertyCardItem(Guid cardId, string category)
        {
            ViewBag.FieldSw = _propertyCardService.GetFieldSw(category);

            ViewData["partialView"] = _propertyCardService.GetItemFieldsPartialView(category);
            ViewData["cardId"] = cardId;
            ViewData["category"] = category;

            return PartialView();
        }

        public async Task<ActionResult> _PropertyCardItemAddEdit(Guid cardId, Guid? cardItemId)
        {
            var data = await _propertyCardService.PsCardItem.GetByIdAsync(cardItemId);
            if (data == null)
            {
                data = new PsCardItemVM()
                {
                    Id = Guid.NewGuid(),
                    PsCardId = cardId
                };
            }

            ViewData["cardItemId"] = cardItemId;
            return PartialView(data);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _CardItemSave(PsCardItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
                Access access = await accessTask;

                var entity = await _propertyCardService.PsCardItem.GetByIdAsync(model.Id);
                if (entity == null)
                {
                    if (!access.AllowAdd)
                    {
                        ModelState.AddModelError("Access", "Access Denied!");
                    }
                }
                else
                {
                    if (!access.AllowEdit)
                    {
                        ModelState.AddModelError("Access", "Access Denied!");
                    }
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;
                    
                    if (entity == null)
                    {
                        model = await _propertyCardService.PsCardItem.CreateAsync(model, user, date);
                    }
                    else
                    {
                        model = await _propertyCardService.PsCardItem.UpdateAsync(model, user, date);
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


        public ActionResult ItemRead([DataSourceRequest] DataSourceRequest request, Guid? cardId, string userName)
        {
            var data = _propertyCardService.PsCardItem.GetTransitByCardId(cardId, userName);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> ItemCreate([DataSourceRequest] DataSourceRequest request, PsCardItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _propertyCardService.PsCardItem.CreateAsync(model, user, date);
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
        public async Task<ActionResult> ItemUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _propertyCardService.PsCardItem.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> ItemDestroy([DataSourceRequest]DataSourceRequest request, PsCardItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                //if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _propertyCardService.PsCardItem.DeleteAsync(model, user, date);
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

        //public ActionResult IssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? cardItemId)
        //{
        //    var data = _propertyCardService.PsCardItemIssuance.GetByCardItemId(cardItemId);

        //    return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        //}

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> IssuanceCreate([DataSourceRequest] DataSourceRequest request, PsCardItemIssuanceVM model)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
        //        Access access = await accessTask;
        //        if (!access.AllowAdd)
        //        {
        //            ModelState.AddModelError("Access", "Access Denied!");
        //        }

        //        if (model != null && ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            model = await _propertyCardService.PsCardItemIssuance.CreateAsync(model, user, date);
        //        }
        //    }
        //    catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
        //    {
        //        var errors = validationException.GetErrorsForModelState();
        //        foreach (var error in errors)
        //        {
        //            ModelState.AddModelError(error.Key, error.Message);
        //        }
        //    }
        //    catch (ValidationException validationException)
        //    {
        //        ModelState.AddModelError("", validationException.InnerException.Message);
        //    }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError("", e.Message);
        //    }

        //    return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        //}

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> IssuanceUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemIssuanceVM model)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
        //        Access access = await accessTask;
        //        if (!access.AllowEdit)
        //        {
        //            ModelState.AddModelError("Access", "Access Denied!");
        //        }

        //        if (ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            model = await _propertyCardService.PsCardItemIssuance.UpdateAsync(model, user, date);
        //        }
        //    }
        //    catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
        //    {
        //        var errors = validationException.GetErrorsForModelState();
        //        foreach (var error in errors)
        //        {
        //            ModelState.AddModelError(error.Key, error.Message);
        //        }
        //    }
        //    catch (ValidationException validationException)
        //    {
        //        ModelState.AddModelError("", validationException.InnerException.Message);
        //    }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError("", e.Message);
        //    }

        //    return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        //}

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> IssuanceDestroy([DataSourceRequest]DataSourceRequest request, PsCardItemIssuanceVM model)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
        //        Access access = await accessTask;
        //        if (!access.AllowDelete)
        //        {
        //            ModelState.AddModelError("DeleteError", "Delete Access Denied!");
        //        }
        //        else
        //        //if (ModelState.IsValid)
        //        {
        //            ModelState.Clear();
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            model = await _propertyCardService.PsCardItemIssuance.DeleteAsync(model, user, date);
        //            // TO DO: update stocks
        //        }
        //    }
        //    catch (ValidationException validationException)
        //    {
        //        ModelState.AddModelError("DeleteError", validationException.InnerException.Message);
        //    }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError("DeleteError", e.Message);
        //    }

        //    return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        //}        

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> LoadFields([System.Web.Http.FromBody] PropertyCardVM model)
        {
            if (model.Id != Guid.Empty)
            {
                //model = await _cardService.GetVmByIdAsync(model.Id);
                var allField = await _propertyCardService.AllField.GetByIdAsync(model.Id);
                if (allField != null)
                {
                    model.AllField = allField;
                }
            }
            else
            {
                model.AllField.Multipliers = 0;
            }

            //var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            //string partialView = AllFieldsUtil.GetPartialView(itemCode);

            string partialView = await _itemCodeService.GetPartialViewAsync(model.ItemCodeId);

            return PartialView(partialView, model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> LoadItemFields([System.Web.Http.FromBody] PsCardItemVM model)
        {
            var psCard = await _propertyCardService.GetByIdAsync((Guid)model.PsCardId);
            if (model.Id != Guid.Empty)
            {
                var data = await _propertyCardService.PsCardItem.GetByIdAsync(model.Id);
                model = _propertyCardService.PsCardItem.TransferItemField(data, model);

            }
            string partialView = AllFieldsUtil.GetPartialItemField(psCard.ItemTypeCode, psCard.ItemCode);

            return PartialView(partialView, model);
        }        

        #region PRINTOUTS

        public ActionResult StockCardRpt(Guid? selectedId)
        {
            string stringname = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(stringname);
            string rptKey = ConfigurationManager.AppSettings["RptKey"];
            string un = decoder.UserID;
            string pw = rptKey; // decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            ReportClass rpt = new ReportClass();
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/PropertyCard.rpt"));
            rpt.SetDatabaseLogon(un, pw, svr, db_);

            rpt.Load();
            rpt.Refresh();

            foreach (Table table in rpt.Database.Tables)
            {
                var logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo.ServerName = svr;
                logonInfo.ConnectionInfo.DatabaseName = db_;
                logonInfo.ConnectionInfo.UserID = un;
                logonInfo.ConnectionInfo.Password = pw;
                logonInfo.ConnectionInfo.IntegratedSecurity = false;
                table.ApplyLogOnInfo(logonInfo);
            }

            var stockNo = _propertyCardService.GetById((Guid)selectedId)?.PsNo;
            var lgu = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault().Description;
            var imagePath = _codextnService.GetByMastCode("DIRS").Where(w => w.Code == "IMAGE-ITEMS").FirstOrDefault().Description;

            rpt.SetParameterValue("@cStockNo", stockNo);
            rpt.SetParameterValue("ImagePath", imagePath);
            rpt.SetParameterValue("LGU", lgu);

            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");
        }
        #endregion        

        #region ITEMEXTN VEHICLES
        public ActionResult _ItemExtnVehicleRead([DataSourceRequest] DataSourceRequest request, Guid? transferId)
        {
            var data = _propertyCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemVehicle.GetCardItemExtns(transferId);
            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ItemExtnVehicleCreate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnVehicleVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;
                    model = await _propertyCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemVehicle.CreateAsync(model, user, date);

                    // TO DO: save to stock card
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
        public async Task<ActionResult> _ItemExtnVehicleUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnVehicleVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;
                    model = await _propertyCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemVehicle.UpdateAsync(model, user, date);

                    // TO DO: update stock card
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
        public async Task<ActionResult> _ItemExtnVehicleDestroy([DataSourceRequest]DataSourceRequest request, PsCardItemExtnVehicleVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;
                    model = await _propertyCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemVehicle.DeleteAsync(model, user, date);
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
        #endregion  

        #region ITEMEXTN OTHERS
        public ActionResult _ItemExtnOtherRead([DataSourceRequest] DataSourceRequest request, Guid? transferId)
        {
            var data = _propertyCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemOther.GetCardItemExtns(transferId);
            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ItemExtnOtherCreate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnOtherVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;
                    model = await _propertyCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemOther.CreateAsync(model, user, date);

                    // TO DO: save to stock card
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
        public async Task<ActionResult> _ItemExtnOtherUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnOtherVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _propertyCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemOther.UpdateAsync(model, user, date);

                    // TO DO: update stock card
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
        public async Task<ActionResult> _ItemExtnOtherDestroy([DataSourceRequest]DataSourceRequest request, PsCardItemExtnOtherVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("GridError", "Delete Access Denied!");
                }
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _propertyCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemOther.DeleteAsync(model, user, date);
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
        #endregion

        #region ITEMEXTN BUILDINGS
        public ActionResult _ItemExtnBldgRead([DataSourceRequest] DataSourceRequest request, Guid? transferId)
        {
            var data = _propertyCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemBldg.GetCardItemExtns(transferId);
            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ItemExtnBldgCreate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnBldgVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _propertyCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemBldg.CreateAsync(model, user, date);

                    // TO DO: save to stock card
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
        public async Task<ActionResult> _ItemExtnBldgUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnBldgVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _propertyCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemBldg.UpdateAsync(model, user, date);

                    // TO DO: update stock card
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
        public async Task<ActionResult> _ItemExtnBldgDestroy([DataSourceRequest]DataSourceRequest request, PsCardItemExtnBldgVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("GridError", "Delete Access Denied!");
                }
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _propertyCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemBldg.DeleteAsync(model, user, date);
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
        #endregion

        #region ITEMEXTN LAND
        public ActionResult _ItemExtnLandRead([DataSourceRequest] DataSourceRequest request, Guid? transferId)
        {
            var data = _propertyCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemLand.GetCardItemExtns(transferId);
            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ItemExtnLandCreate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnLandVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _propertyCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemLand.CreateAsync(model, user, date);

                    // TO DO: save to stock card
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
        public async Task<ActionResult> _ItemExtnLandUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnLandVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _propertyCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemLand.UpdateAsync(model, user, date);

                    // TO DO: update stock card
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
        public async Task<ActionResult> _ItemExtnLandDestroy([DataSourceRequest]DataSourceRequest request, PsCardItemExtnLandVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("GridError", "Delete Access Denied!");
                }
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _propertyCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemLand.DeleteAsync(model, user, date);
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
        #endregion

        #region QUERY
        public ActionResult ItemQuery()
        {
            return View();
        }

        public ActionResult ItemQueryRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = _propertyCardService.PsCardItem.GetAllProperties();

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }
        #endregion

        #region AJAX CALLS
        [HttpPost]
        public async Task<ActionResult> GetItemExtnTemplate(Guid? id)
        {            
            var itemTransfer = await _propertyCardService.PsCardItem.PsCardItemTransfer.GetByIdAsync(id);
            string itemExtnName = _propertyCardService.GetItemExtnName(itemTransfer.PsCardItemId);

            return Json(new { Errors = "", ItemExtnName = itemExtnName, ItemTransfer = itemTransfer }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult GetEndSeries(string startSeries, Guid? itemId)
        {
            string endSeries = _propertyCardService.PsCardItem.PsCardItemExtn.GetEndSeries(startSeries, itemId);

            return Json(new { Errors = "", EndSeries = endSeries }, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> PostItemRecord(Guid psCardItemId)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _propertyCardService.PsCardItem.PostAsync(psCardItemId, user, date);
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
        public async Task<ActionResult> UnpostItemRecord(Guid psCardItemId)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
                Access access = await accessTask;
                if (!access.AllowUnpost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _propertyCardService.PsCardItem.UnpostAsync(psCardItemId, user, date);
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
        #endregion

        public ActionResult _PoTransfer(Guid psCardItemId)
        {
            var model = new GetPsNoVM()
            {
                PsCardItemId = psCardItemId
            };

            return PartialView(model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _PoTransferSave(GetPsNoVM model)
        {
            string errorKey = "";
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
                Access access = await accessTask;
                if (!access.AllowTransfer) 
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _propertyCardService.TransferPo(model.PsCardItemId, model.Id, user, date);
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
    }
}