//using CrystalDecisions.CrystalReports.Engine;
//using iLgs.Exceptions;
//using iLgs.Exceptions.Service;
//using iLgs.Models;
//using iLgs.Services.Card;
//using iLgs.Services.Codes;
//using iLgs.Services.Items;
//using iLgs.Services.PropertyCard;
//using iLgs.Utilities;
//using Kendo.Mvc.Extensions;
//using Kendo.Mvc.UI;
//using Microsoft.AspNet.Identity;
//using Newtonsoft.Json;
//using System;
//using System.Data.SqlClient;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Web.Mvc;
//using static iLgs.Models.Enums;

//namespace iLgs.Controllers
//{
//    [AppAuthorize("CARD")]
//    public class CardController : BaseController
//    {
//        private readonly AppManEntities _db;
//        private readonly ICodextnService _codextnService;
//        private readonly ICardService _cardService;
//        private readonly IItemCodeService _itemCodeService;
//        private readonly IPropertyCardValidator _propertyCardValidator;        

//        public CardController(AppManEntities db,
//            ICodextnService codextnService, 
//            ICardService cardService, 
//            IItemCodeService itemCodeService,
//            IPropertyCardValidator propertyCardValidator)
//        {
//            _db = db;
//            _codextnService = codextnService;
//            _cardService = cardService;
//            _itemCodeService = itemCodeService;
//            _propertyCardValidator = propertyCardValidator;
//        }

//        public ActionResult Property()
//        {
//            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
//            ViewBag.CardCategory = (int?)CardCategory.PROPERTY;
//            ViewBag.Title = "Property Card";
            
//            return View("Index");
//        }

//        public ActionResult Stock()
//        {
//            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
//            ViewBag.CardCategory = (int?)CardCategory.STOCK;
//            ViewBag.Title = "Stock Card";

//            return View("Index");
//        }

//        public ActionResult SemiExpendable()
//        {
//            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
//            ViewBag.CardCategory = (int?)CardCategory.SE;
//            ViewBag.Title = "Semi-Expendable Card";

//            return View();
//        }

//        // GET: Index
//        public ActionResult Index()
//        {
//            if (TempData["AllowIndexAccess"] == null || !(bool)TempData["AllowIndexAccess"])
//            {
//                ViewBag.Error = "Access Denied!";
//                return View("Error"); // Or some other handling
//            }
//            return View();
//        }

//        public ActionResult Read([DataSourceRequest] DataSourceRequest request, string userName, int? cardCategory)
//        {
//            object data;

//            if (cardCategory == 1)
//            {
//                data = _cardService.GetAll<PsCardVM>(userName);
//            }
//            else
//            {
//                data = _cardService.GetAll<StockCardVM>(userName);
//            }

//            var result = new JsonNetResult
//            {
//                Data = ((IQueryable)data).ToDataSourceResult(request),
//                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
//                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
//            };

//            return result;
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> Create([DataSourceRequest] DataSourceRequest request, object model, int? cardCategory)
//        {
//            try
//            {
//                var menuId = _cardService.GetMenuId(cardCategory);
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), menuId);
//                Access access = await accessTask;
//                if (!access.AllowAdd)
//                {
//                    ModelState.AddModelError("AddError", "Add Access Denied!");
//                }

//                if (model != null && ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    if (cardCategory == (int?)CardCategory.PROPERTY)
//                    {
//                        model = await _cardService.CreateAsync<PsCardVM>(model as PsCardVM, user, date);
//                    }
//                    else
//                    {
//                        model = await _cardService.CreateAsync<StockCardVM>(model as StockCardVM, user, date);
//                    }
//                }
//            }
//            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
//            {
//                var errors = validationException.GetErrorsForModelState();
//                foreach (var error in errors)
//                {
//                    ModelState.AddModelError(error.Key, error.Message);
//                }
//            }
//            catch (ValidationException validationException)
//            {
//                ModelState.AddModelError("", validationException.InnerException.Message);
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("", e.Message);
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> Update([DataSourceRequest] DataSourceRequest request, object model, int? cardCategory)
//        {
//            try
//            {
//                var menuId = _cardService.GetMenuId(cardCategory);
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), menuId);
//                Access access = await accessTask;
//                if (!access.AllowEdit)
//                {
//                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
//                }

//                if (ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    if (cardCategory == (int?)CardCategory.PROPERTY)
//                    {
//                        model = await _cardService.UpdateAsync<PsCardVM>(model as PsCardVM, user, date);
//                    }
//                    else
//                    {
//                        model = await _cardService.UpdateAsync<StockCardVM>(model as StockCardVM, user, date);
//                    }
//                }
//            }
//            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
//            {
//                var errors = validationException.GetErrorsForModelState();
//                foreach (var error in errors)
//                {
//                    ModelState.AddModelError(error.Key, error.Message);
//                }
//            }
//            catch (ValidationException validationException)
//            {
//                ModelState.AddModelError("", validationException.InnerException.Message);
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("", e.Message);
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> Destroy([DataSourceRequest]DataSourceRequest request, object model, int? cardCategory)
//        {
//            try
//            {
//                var menuId = _cardService.GetMenuId(cardCategory);
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), menuId);
//                Access access = await accessTask;
//                if (!access.AllowDelete)
//                {
//                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
//                }
//                else
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    if (cardCategory == (int?)CardCategory.PROPERTY)
//                    {
//                        model = await _cardService.DeleteAsync<PsCardVM>(model as PsCardVM, user, date);
//                    }
//                    else
//                    {
//                        model = await _cardService.DeleteAsync<StockCardVM>(model as StockCardVM, user, date);
//                    }
//                }
//            }
//            catch (ValidationException validationException)
//            {
//                ModelState.AddModelError("DeleteError", validationException.InnerException.Message);
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("DeleteError", e.Message);
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        public async Task<ActionResult> _PropertyCardAddEdit(Guid? cardId, string mode)
//        {
//            var data = await _cardService.GetByIdAsync(cardId);
//            if (data == null)
//            {
//                data = new PropertyCardVM()
//                {
//                    CardCategory = _cardCategory
//                };
//            }

//            data.Mode = mode;

//            return PartialView(data);
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> _PropertyCardSave(PropertyCardVM model)
//        {
//            string errorKey = "";
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
//                Access access = await accessTask;
//                if (model != null && ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    var entity = await _cardService.GetByIdAsync(model.Id);

//                    if (entity == null)
//                    {
//                        if (access.AllowAdd)
//                        {
//                            model = await _cardService.CreateAsync(model, user, date);
//                        }
//                        else
//                        {
//                            errorKey = "AddError";
//                            ModelState.AddModelError(errorKey, "Access Denied!");
//                        }
//                    }
//                    else
//                    {
//                        if (access.AllowEdit)
//                        {
//                            model = await _cardService.UpdateAsync(model, user, date);
//                        }
//                        else
//                        {
//                            errorKey = "UpdateError";
//                            ModelState.AddModelError(errorKey, "Access Denied!");
//                        }
//                    }
//                }
//            }
//            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
//            {
//                var errors = validationException.GetErrorsForModelState();
//                foreach (var error in errors)
//                {
//                    ModelState.AddModelError(error.Key, error.Message);
//                }
//            }
//            catch (ValidationException validationException)
//            {
//                ModelState.AddModelError("", validationException.InnerException.Message);
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("", e.Message);
//            }

//            var errorList = ModelState.Where(ms => ms.Value.Errors.Any())
//                       .Select(ms => new
//                       {
//                           Key = ms.Key, // The field name
//                           Message = ms.Value.Errors.Select(e =>
//                           {
//                               var errorMessage = e.ErrorMessage;
//                               if (e.Exception != null)
//                               {
//                                   var exceptionMessage = e.Exception.Message;
//                                   var innerExceptionMessage = e.Exception.InnerException?.Message;

//                                   // Append exception details
//                                   errorMessage += $" Exception: {exceptionMessage}";
//                                   if (innerExceptionMessage != null)
//                                   {
//                                       errorMessage += $" InnerException: {innerExceptionMessage}";
//                                   }
//                               }

//                               return errorMessage;
//                           }).ToList() // List of messages for the current field
//                       })
//                       .ToList();

//            if (errorList.Any())
//            {
//                return Json(new { Errors = errorList }, JsonRequestBehavior.AllowGet);
//            }

//            return Json(new { Errors = "", Id = model.Id }, JsonRequestBehavior.AllowGet);
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<JsonResult> GetDescription(PropertyCardVM fields)
//        {
//            var description = _cardService.GetDescription(fields);
//            var stockNo = _cardService.GetStockNo(fields);
//            var psCard = await _cardService.GetByPsNoAsync(stockNo);

//            Guid id = Guid.NewGuid();
//            if (psCard != null)
//            {
//                id = psCard.Id;
//            }

//            fields.PsNo = stockNo;
//            bool isDuplicateStockNo = false;
//            if (fields.Mode == "A")
//            {
//                isDuplicateStockNo = _propertyCardValidator.IsPsNoAlreadyExists(fields, Mode.ADD);
//            }
//            else
//            {
//                isDuplicateStockNo = _propertyCardValidator.IsPsNoAlreadyExists(fields, Mode.EDIT);
//            }

//            return Json(new { Description = description, StockNo = stockNo, Id = id, IsDuplicateStockNo = isDuplicateStockNo }, JsonRequestBehavior.AllowGet);
//        }

//        public ActionResult _PropertyCardItem(Guid cardId, string category)
//        {
//            ViewBag.FieldSw = _cardService.GetFieldSw(category);

//            ViewData["partialView"] = _cardService.GetItemFieldsPartialView(category);
//            ViewData["cardId"] = cardId;
//            ViewData["category"] = category;

//            return PartialView();
//        }

//        public async Task<ActionResult> _PropertyCardItemAddEdit(Guid cardId, Guid? cardItemId)
//        {
//            var data = await _cardService.PsCardItem.GetByIdAsync(cardItemId);
//            if (data == null)
//            {
//                data = new PsCardItemVM()
//                {
//                    Id = Guid.NewGuid(),
//                    PsCardId = cardId
//                };
//            }

//            ViewData["cardItemId"] = cardItemId;
//            return PartialView(data);
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> _CardItemSave(PsCardItemVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
//                Access access = await accessTask;

//                var entity = await _cardService.PsCardItem.GetByIdAsync(model.Id);
//                if (entity == null)
//                {
//                    if (!access.AllowAdd)
//                    {
//                        ModelState.AddModelError("Access", "Access Denied!");
//                    }
//                }
//                else
//                {
//                    if (!access.AllowEdit)
//                    {
//                        ModelState.AddModelError("Access", "Access Denied!");
//                    }
//                }

//                if (model != null && ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    if (entity == null)
//                    {
//                        model = await _cardService.PsCardItem.CreateAsync(model, user, date);
//                    }
//                    else
//                    {
//                        model = await _cardService.PsCardItem.UpdateAsync(model, user, date);
//                    }
//                }
//            }
//            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
//            {
//                var errors = validationException.GetErrorsForModelState();
//                foreach (var error in errors)
//                {
//                    ModelState.AddModelError(error.Key, error.Message);
//                }
//            }
//            catch (ValidationException validationException)
//            {
//                ModelState.AddModelError("", validationException.InnerException.Message);
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("", e.Message);
//            }

//            var query = from state in ModelState.Values
//                        from error in state.Errors
//                        select error.ErrorMessage;

//            var errorList = query.ToList();
//            if (errorList.Count() > 0)
//            {
//                return Json(new { Errors = errorList }, JsonRequestBehavior.DenyGet);
//            }

//            return Json(new { Errors = "" }, JsonRequestBehavior.AllowGet);
//        }


//        public ActionResult ItemRead([DataSourceRequest] DataSourceRequest request, Guid? cardId, string userName)
//        {
//            var data = _cardService.PsCardItem.GetTransitByCardId(cardId, userName);

//            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> ItemCreate([DataSourceRequest] DataSourceRequest request, PsCardItemVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
//                Access access = await accessTask;
//                if (!access.AllowAdd)
//                {
//                    ModelState.AddModelError("Access", "Access Denied!");
//                }

//                if (model != null && ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    model = await _cardService.PsCardItem.CreateAsync(model, user, date);
//                }
//            }
//            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
//            {
//                var errors = validationException.GetErrorsForModelState();
//                foreach (var error in errors)
//                {
//                    ModelState.AddModelError(error.Key, error.Message);
//                }
//            }
//            catch (ValidationException validationException)
//            {
//                ModelState.AddModelError("", validationException.InnerException.Message);
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("", e.Message);
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> ItemUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
//                Access access = await accessTask;
//                if (!access.AllowEdit)
//                {
//                    ModelState.AddModelError("Access", "Access Denied!");
//                }

//                if (ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    model = await _cardService.PsCardItem.UpdateAsync(model, user, date);
//                }
//            }
//            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
//            {
//                var errors = validationException.GetErrorsForModelState();
//                foreach (var error in errors)
//                {
//                    ModelState.AddModelError(error.Key, error.Message);
//                }
//            }
//            catch (ValidationException validationException)
//            {
//                ModelState.AddModelError("", validationException.InnerException.Message);
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("", e.Message);
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> ItemDestroy([DataSourceRequest]DataSourceRequest request, PsCardItemVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
//                Access access = await accessTask;
//                if (!access.AllowDelete)
//                {
//                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
//                }
//                else
//                //if (ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    model = await _cardService.PsCardItem.DeleteAsync(model, user, date);
//                    // TO DO: update stocks
//                }
//            }
//            catch (ValidationException validationException)
//            {
//                ModelState.AddModelError("DeleteError", validationException.InnerException.Message);
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("DeleteError", e.Message);
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        //public ActionResult IssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? cardItemId)
//        //{
//        //    var data = _propertyCardService.PsCardItemIssuance.GetByCardItemId(cardItemId);

//        //    return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
//        //}

//        //[AcceptVerbs(HttpVerbs.Post)]
//        //public async Task<ActionResult> IssuanceCreate([DataSourceRequest] DataSourceRequest request, PsCardItemIssuanceVM model)
//        //{
//        //    try
//        //    {
//        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
//        //        Access access = await accessTask;
//        //        if (!access.AllowAdd)
//        //        {
//        //            ModelState.AddModelError("Access", "Access Denied!");
//        //        }

//        //        if (model != null && ModelState.IsValid)
//        //        {
//        //            string user = ControllerContext.HttpContext.User.Identity.Name;
//        //            DateTime date = System.DateTime.Now;

//        //            model = await _propertyCardService.PsCardItemIssuance.CreateAsync(model, user, date);
//        //        }
//        //    }
//        //    catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
//        //    {
//        //        var errors = validationException.GetErrorsForModelState();
//        //        foreach (var error in errors)
//        //        {
//        //            ModelState.AddModelError(error.Key, error.Message);
//        //        }
//        //    }
//        //    catch (ValidationException validationException)
//        //    {
//        //        ModelState.AddModelError("", validationException.InnerException.Message);
//        //    }
//        //    catch (Exception e)
//        //    {
//        //        ModelState.AddModelError("", e.Message);
//        //    }

//        //    return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        //}

//        //[AcceptVerbs(HttpVerbs.Post)]
//        //public async Task<ActionResult> IssuanceUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemIssuanceVM model)
//        //{
//        //    try
//        //    {
//        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
//        //        Access access = await accessTask;
//        //        if (!access.AllowEdit)
//        //        {
//        //            ModelState.AddModelError("Access", "Access Denied!");
//        //        }

//        //        if (ModelState.IsValid)
//        //        {
//        //            string user = ControllerContext.HttpContext.User.Identity.Name;
//        //            DateTime date = System.DateTime.Now;

//        //            model = await _propertyCardService.PsCardItemIssuance.UpdateAsync(model, user, date);
//        //        }
//        //    }
//        //    catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
//        //    {
//        //        var errors = validationException.GetErrorsForModelState();
//        //        foreach (var error in errors)
//        //        {
//        //            ModelState.AddModelError(error.Key, error.Message);
//        //        }
//        //    }
//        //    catch (ValidationException validationException)
//        //    {
//        //        ModelState.AddModelError("", validationException.InnerException.Message);
//        //    }
//        //    catch (Exception e)
//        //    {
//        //        ModelState.AddModelError("", e.Message);
//        //    }

//        //    return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        //}

//        //[AcceptVerbs(HttpVerbs.Post)]
//        //public async Task<ActionResult> IssuanceDestroy([DataSourceRequest]DataSourceRequest request, PsCardItemIssuanceVM model)
//        //{
//        //    try
//        //    {
//        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
//        //        Access access = await accessTask;
//        //        if (!access.AllowDelete)
//        //        {
//        //            ModelState.AddModelError("DeleteError", "Delete Access Denied!");
//        //        }
//        //        else
//        //        //if (ModelState.IsValid)
//        //        {
//        //            ModelState.Clear();
//        //            string user = ControllerContext.HttpContext.User.Identity.Name;
//        //            DateTime date = System.DateTime.Now;

//        //            model = await _propertyCardService.PsCardItemIssuance.DeleteAsync(model, user, date);
//        //            // TO DO: update stocks
//        //        }
//        //    }
//        //    catch (ValidationException validationException)
//        //    {
//        //        ModelState.AddModelError("DeleteError", validationException.InnerException.Message);
//        //    }
//        //    catch (Exception e)
//        //    {
//        //        ModelState.AddModelError("DeleteError", e.Message);
//        //    }

//        //    return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        //}        

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> LoadFields([System.Web.Http.FromBody] PropertyCardVM model)
//        {
//            if (model.Id != Guid.Empty)
//            {
//                //model = await _cardService.GetVmByIdAsync(model.Id);
//                var allField = await _cardService.AllField.GetByIdAsync(model.Id);
//                if (allField != null)
//                {
//                    model.AllField = allField;
//                }
//            }
//            else
//            {
//                model.AllField.Multipliers = 0;
//            }

//            //string partialView = AllFieldsUtil.GetPartialField(model.ItemTypeCode, model.ItemCode);
//            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
//            string partialView = AllFieldsUtil.GetPartialView(itemCode);

//            return PartialView(partialView, model);
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> LoadItemFields([System.Web.Http.FromBody] PsCardItemVM model)
//        {
//            var psCard = await _cardService.GetByIdAsync((Guid)model.PsCardId);
//            if (model.Id != Guid.Empty)
//            {
//                var data = await _cardService.PsCardItem.GetByIdAsync(model.Id);
//                model = _cardService.PsCardItem.TransferItemField(data, model);

//            }
//            string partialView = AllFieldsUtil.GetPartialItemField(psCard.ItemTypeCode, psCard.ItemCode);

//            return PartialView(partialView, model);
//        }

//        #region PRINTOUTS

//        public ActionResult StockCardRpt(Guid? selectedId)
//        {
//            string stringname = _db.Database.Connection.ConnectionString.ToString();
//            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(stringname);

//            string un = decoder.UserID;
//            string pw = decoder.Password;
//            string svr = decoder.DataSource;
//            string db_ = decoder.InitialCatalog;

//            ReportClass rpt = new ReportClass();
//            rpt.FileName = Server.MapPath(Url.Content("~/Reports/PropertyCard.rpt"));
//            rpt.SetDatabaseLogon(un, pw, svr, db_);

//            rpt.Load();
//            rpt.Refresh();

//            foreach (Table table in rpt.Database.Tables)
//            {
//                var logonInfo = table.LogOnInfo;
//                logonInfo.ConnectionInfo.ServerName = svr;
//                logonInfo.ConnectionInfo.DatabaseName = db_;
//                logonInfo.ConnectionInfo.UserID = un;
//                logonInfo.ConnectionInfo.Password = pw;
//                logonInfo.ConnectionInfo.IntegratedSecurity = true;
//                table.ApplyLogOnInfo(logonInfo);
//            }

//            var stockNo = _cardService.GetById((Guid)selectedId)?.PsNo;
//            var lgu = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault().Description;
//            var imagePath = _codextnService.GetByMastCode("DIRS").Where(w => w.Code == "IMAGE-ITEMS").FirstOrDefault().Description;

//            rpt.SetParameterValue("@cStockNo", stockNo);
//            rpt.SetParameterValue("ImagePath", imagePath);
//            rpt.SetParameterValue("LGU", lgu);

//            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
//            rpt.Close();
//            rpt.Dispose();
//            return File(stream, "application/pdf");
//        }
//        #endregion        

//        #region ITEMEXTN VEHICLES
//        public ActionResult _ItemExtnVehicleRead([DataSourceRequest] DataSourceRequest request, Guid? transferId)
//        {
//            var data = _cardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemVehicle.GetCardItemExtns(transferId);
//            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> _ItemExtnVehicleCreate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnVehicleVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
//                Access access = await accessTask;
//                if (!access.AllowAdd)
//                {
//                    ModelState.AddModelError("", "Access Denied!");
//                }

//                if (model != null && ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;
//                    model = await _cardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemVehicle.CreateAsync(model, user, date);

//                    // TO DO: save to stock card
//                }
//            }
//            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
//            {
//                var errors = validationException.GetErrorsForModelState();
//                foreach (var error in errors)
//                {
//                    ModelState.AddModelError(error.Key, error.Message);
//                }
//            }
//            catch (ValidationException validationException)
//            {
//                ModelState.AddModelError("", validationException.InnerException.Message);
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("", e.Message);
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> _ItemExtnVehicleUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnVehicleVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
//                Access access = await accessTask;
//                if (!access.AllowEdit)
//                {
//                    ModelState.AddModelError("", "Access Denied!");
//                }

//                if (ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;
//                    model = await _cardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemVehicle.UpdateAsync(model, user, date);

//                    // TO DO: update stock card
//                }
//            }
//            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
//            {
//                var errors = validationException.GetErrorsForModelState();
//                foreach (var error in errors)
//                {
//                    ModelState.AddModelError(error.Key, error.Message);
//                }
//            }
//            catch (ValidationException validationException)
//            {
//                ModelState.AddModelError("", validationException.InnerException.Message);
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("", e.Message);
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> _ItemExtnVehicleDestroy([DataSourceRequest]DataSourceRequest request, PsCardItemExtnVehicleVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
//                Access access = await accessTask;
//                if (!access.AllowDelete)
//                {
//                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
//                }
//                if (ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;
//                    model = await _cardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemVehicle.DeleteAsync(model, user, date);
//                    // TO DO: update stocks
//                }
//            }
//            catch (ValidationException validationException)
//            {
//                ModelState.AddModelError("DeleteError", validationException.InnerException.Message);
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("DeleteError", e.Message);
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }
//        #endregion  

//        #region ITEMEXTN OTHERS
//        public ActionResult _ItemExtnOtherRead([DataSourceRequest] DataSourceRequest request, Guid? transferId)
//        {
//            var data = _cardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemOther.GetCardItemExtns(transferId);
//            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> _ItemExtnOtherCreate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnOtherVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
//                Access access = await accessTask;
//                if (!access.AllowAdd)
//                {
//                    ModelState.AddModelError("", "Access Denied!");
//                }

//                if (model != null && ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;
//                    model = await _cardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemOther.CreateAsync(model, user, date);

//                    // TO DO: save to stock card
//                }
//            }
//            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
//            {
//                var errors = validationException.GetErrorsForModelState();
//                foreach (var error in errors)
//                {
//                    ModelState.AddModelError(error.Key, error.Message);
//                }
//            }
//            catch (ValidationException validationException)
//            {
//                ModelState.AddModelError("", validationException.InnerException.Message);
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("", e.Message);
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> _ItemExtnOtherUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnOtherVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
//                Access access = await accessTask;
//                if (!access.AllowEdit)
//                {
//                    ModelState.AddModelError("", "Access Denied!");
//                }

//                if (ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    model = await _cardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemOther.UpdateAsync(model, user, date);

//                    // TO DO: update stock card
//                }
//            }
//            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
//            {
//                var errors = validationException.GetErrorsForModelState();
//                foreach (var error in errors)
//                {
//                    ModelState.AddModelError(error.Key, error.Message);
//                }
//            }
//            catch (ValidationException validationException)
//            {
//                ModelState.AddModelError("", validationException.InnerException.Message);
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("", e.Message);
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> _ItemExtnOtherDestroy([DataSourceRequest]DataSourceRequest request, PsCardItemExtnOtherVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
//                Access access = await accessTask;
//                if (!access.AllowDelete)
//                {
//                    ModelState.AddModelError("GridError", "Delete Access Denied!");
//                }
//                if (ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    model = await _cardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemOther.DeleteAsync(model, user, date);
//                    // TO DO: update stocks
//                }
//            }
//            catch (ValidationException validationException)
//            {
//                ModelState.AddModelError("DeleteError", validationException.InnerException.Message);
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("DeleteError", e.Message);
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }
//        #endregion

//        #region ITEMEXTN BUILDINGS
//        public ActionResult _ItemExtnBldgRead([DataSourceRequest] DataSourceRequest request, Guid? transferId)
//        {
//            var data = _cardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemBldg.GetCardItemExtns(transferId);
//            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> _ItemExtnBldgCreate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnBldgVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
//                Access access = await accessTask;
//                if (!access.AllowAdd)
//                {
//                    ModelState.AddModelError("", "Access Denied!");
//                }

//                if (model != null && ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    model = await _cardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemBldg.CreateAsync(model, user, date);

//                    // TO DO: save to stock card
//                }
//            }
//            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
//            {
//                var errors = validationException.GetErrorsForModelState();
//                foreach (var error in errors)
//                {
//                    ModelState.AddModelError(error.Key, error.Message);
//                }
//            }
//            catch (ValidationException validationException)
//            {
//                ModelState.AddModelError("", validationException.InnerException.Message);
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("", e.Message);
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> _ItemExtnBldgUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnBldgVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
//                Access access = await accessTask;
//                if (!access.AllowEdit)
//                {
//                    ModelState.AddModelError("", "Access Denied!");
//                }

//                if (ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    model = await _cardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemBldg.UpdateAsync(model, user, date);

//                    // TO DO: update stock card
//                }
//            }
//            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
//            {
//                var errors = validationException.GetErrorsForModelState();
//                foreach (var error in errors)
//                {
//                    ModelState.AddModelError(error.Key, error.Message);
//                }
//            }
//            catch (ValidationException validationException)
//            {
//                ModelState.AddModelError("", validationException.InnerException.Message);
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("", e.Message);
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> _ItemExtnBldgDestroy([DataSourceRequest]DataSourceRequest request, PsCardItemExtnBldgVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
//                Access access = await accessTask;
//                if (!access.AllowDelete)
//                {
//                    ModelState.AddModelError("GridError", "Delete Access Denied!");
//                }
//                if (ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    model = await _cardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemBldg.DeleteAsync(model, user, date);
//                    // TO DO: update stocks
//                }
//            }
//            catch (ValidationException validationException)
//            {
//                ModelState.AddModelError("DeleteError", validationException.InnerException.Message);
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("DeleteError", e.Message);
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }
//        #endregion

//        #region ITEMEXTN LAND
//        public ActionResult _ItemExtnLandRead([DataSourceRequest] DataSourceRequest request, Guid? transferId)
//        {
//            var data = _cardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemLand.GetCardItemExtns(transferId);
//            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> _ItemExtnLandCreate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnLandVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
//                Access access = await accessTask;
//                if (!access.AllowAdd)
//                {
//                    ModelState.AddModelError("", "Access Denied!");
//                }

//                if (model != null && ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    model = await _cardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemLand.CreateAsync(model, user, date);

//                    // TO DO: save to stock card
//                }
//            }
//            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
//            {
//                var errors = validationException.GetErrorsForModelState();
//                foreach (var error in errors)
//                {
//                    ModelState.AddModelError(error.Key, error.Message);
//                }
//            }
//            catch (ValidationException validationException)
//            {
//                ModelState.AddModelError("", validationException.InnerException.Message);
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("", e.Message);
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> _ItemExtnLandUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnLandVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
//                Access access = await accessTask;
//                if (!access.AllowEdit)
//                {
//                    ModelState.AddModelError("", "Access Denied!");
//                }

//                if (ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    model = await _cardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemLand.UpdateAsync(model, user, date);

//                    // TO DO: update stock card
//                }
//            }
//            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
//            {
//                var errors = validationException.GetErrorsForModelState();
//                foreach (var error in errors)
//                {
//                    ModelState.AddModelError(error.Key, error.Message);
//                }
//            }
//            catch (ValidationException validationException)
//            {
//                ModelState.AddModelError("", validationException.InnerException.Message);
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("", e.Message);
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> _ItemExtnLandDestroy([DataSourceRequest]DataSourceRequest request, PsCardItemExtnLandVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
//                Access access = await accessTask;
//                if (!access.AllowDelete)
//                {
//                    ModelState.AddModelError("GridError", "Delete Access Denied!");
//                }
//                if (ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    model = await _cardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemLand.DeleteAsync(model, user, date);
//                    // TO DO: update stocks
//                }
//            }
//            catch (ValidationException validationException)
//            {
//                ModelState.AddModelError("DeleteError", validationException.InnerException.Message);
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("DeleteError", e.Message);
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }
//        #endregion

//        #region QUERY
//        public ActionResult ItemQuery()
//        {
//            return View();
//        }

//        public ActionResult ItemQueryRead([DataSourceRequest] DataSourceRequest request)
//        {
//            var data = _cardService.PsCardItem.GetAllProperties();

//            var result = new JsonNetResult
//            {
//                Data = data.ToDataSourceResult(request),
//                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
//                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
//            };

//            return result;
//        }
//        #endregion

//        #region AJAX CALLS
//        [HttpPost]
//        public async Task<ActionResult> GetItemExtnTemplate(Guid? id)
//        {
//            var itemTransfer = await _cardService.PsCardItem.PsCardItemTransfer.GetByIdAsync(id);
//            string itemExtnName = _cardService.GetItemExtnName(itemTransfer.PsCardItemId);

//            return Json(new { Errors = "", ItemExtnName = itemExtnName, ItemTransfer = itemTransfer }, JsonRequestBehavior.AllowGet);
//        }

//        [HttpGet]
//        public ActionResult GetEndSeries(string startSeries, Guid? itemId)
//        {
//            string endSeries = _cardService.PsCardItem.PsCardItemExtn.GetEndSeries(startSeries, itemId);

//            return Json(new { Errors = "", EndSeries = endSeries }, JsonRequestBehavior.AllowGet);
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> PostItemRecord(Guid psCardItemId)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
//                Access access = await accessTask;
//                if (!access.AllowPost)
//                {
//                    ModelState.AddModelError("Access", "Access Denied!");
//                }

//                if (ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    await _cardService.PsCardItem.PostAsync(psCardItemId, user, date);
//                }
//            }
//            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
//            {
//                var errors = validationException.GetErrorsForModelState();
//                foreach (var error in errors)
//                {
//                    ModelState.AddModelError(error.Key, error.Message);
//                }
//            }
//            catch (ValidationException validationException)
//            {
//                ModelState.AddModelError("", validationException.InnerException.Message);
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("", e.Message);
//            }

//            var query = from state in ModelState.Values
//                        from error in state.Errors
//                        select error.ErrorMessage;

//            var errorList = query.ToList();
//            if (errorList.Count() > 0)
//            {
//                return Json(new { Errors = errorList }, JsonRequestBehavior.DenyGet);
//            }

//            return Json(new { Errors = "" }, JsonRequestBehavior.AllowGet);
//        }

//        [HttpPost]
//        public async Task<ActionResult> UnpostItemRecord(Guid psCardItemId)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
//                Access access = await accessTask;
//                if (!access.AllowUnpost)
//                {
//                    ModelState.AddModelError("Access", "Access Denied!");
//                }

//                if (ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    await _cardService.PsCardItem.UnpostAsync(psCardItemId, user, date);
//                }
//            }
//            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
//            {
//                var errors = validationException.GetErrorsForModelState();
//                foreach (var error in errors)
//                {
//                    ModelState.AddModelError(error.Key, error.Message);
//                }
//            }
//            catch (ValidationException validationException)
//            {
//                ModelState.AddModelError("", validationException.InnerException.Message);
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("", e.Message);
//            }

//            var query = from state in ModelState.Values
//                        from error in state.Errors
//                        select error.ErrorMessage;

//            var errorList = query.ToList();
//            if (errorList.Count() > 0)
//            {
//                return Json(new { Errors = errorList }, JsonRequestBehavior.DenyGet);
//            }

//            return Json(new { Errors = "" }, JsonRequestBehavior.AllowGet);
//        }
//        #endregion

//        public ActionResult _PoTransfer(Guid psCardItemId)
//        {
//            var model = new GetPsNoVM()
//            {
//                PsCardItemId = psCardItemId
//            };

//            return PartialView(model);
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> _PoTransferSave(GetPsNoVM model)
//        {
//            string errorKey = "";
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "property_card");
//                Access access = await accessTask;
//                if (!access.AllowTransfer)
//                {
//                    ModelState.AddModelError("Access", "Access Denied!");
//                }

//                if (model != null && ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    await _cardService.TransferPo(model.PsCardItemId, model.Id, user, date);
//                }
//            }
//            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
//            {
//                var errors = validationException.GetErrorsForModelState();
//                foreach (var error in errors)
//                {
//                    ModelState.AddModelError(error.Key, error.Message);
//                }
//            }
//            catch (ValidationException validationException)
//            {
//                ModelState.AddModelError("", validationException.InnerException.Message);
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("", e.Message);
//            }

//            var errorList = ModelState.Where(ms => ms.Value.Errors.Any())
//                       .Select(ms => new
//                       {
//                           Key = ms.Key, // The field name
//                           Message = ms.Value.Errors.Select(e =>
//                           {
//                               var errorMessage = e.ErrorMessage;
//                               if (e.Exception != null)
//                               {
//                                   var exceptionMessage = e.Exception.Message;
//                                   var innerExceptionMessage = e.Exception.InnerException?.Message;

//                                   // Append exception details
//                                   errorMessage += $" Exception: {exceptionMessage}";
//                                   if (innerExceptionMessage != null)
//                                   {
//                                       errorMessage += $" InnerException: {innerExceptionMessage}";
//                                   }
//                               }

//                               return errorMessage;
//                           }).ToList() // List of messages for the current field
//                       })
//                       .ToList();

//            if (errorList.Any())
//            {
//                return Json(new { Errors = errorList }, JsonRequestBehavior.AllowGet);
//            }

//            return Json(new { Errors = "", Id = model.Id }, JsonRequestBehavior.AllowGet);
//        }
//    }
//}