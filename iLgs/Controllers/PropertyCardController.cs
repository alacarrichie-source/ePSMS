using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services;
using iLgs.Services.Codes;
using iLgs.Services.Interfaces;
using iLgs.Services.Items;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static iLgs.Models.Enums;

namespace iLgs.Controllers
{
    [AppAuthorize("PROPERTYCARD")]
    public class PropertyCardController : BaseController
    {
        private readonly string _cardCategory = "P";
        private readonly AppManEntities _db;
        private readonly ICodextnService _codextnService;
        private readonly IPropertyCardService _propertyCardService;
        private readonly IItemCodeService _itemCodeService;
        
        public PropertyCardController()
        {
            _db = new AppManEntities();
            _codextnService = new CodextnService(_db);
            _propertyCardService = new PropertyCardService(_db);
            _itemCodeService = new ItemCodeService(_db);
        }

        // GET: Index
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Read([DataSourceRequest] DataSourceRequest request)
        {
            var data = _propertyCardService.GetAll();

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

        public async Task<ActionResult> _PropertyCardAddEdit(Guid? cardId)
        {
            var data = await _propertyCardService.GetByIdAsync(cardId);
            if (data == null)
            {
                data = new PropertyCardVM()
                {
                    CardCategory = _cardCategory
                };
            }
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
            catch (ValidationException validationException)
                when (validationException.InnerException is AlreadyExistsException)
            {
                ModelState.AddModelError(errorKey, validationException.InnerException);
            }
            catch (ValidationException validationException)
            {
                var jErrors = validationException.GetFormattedErrorsAsJson();
                return Json(new { Errors = jErrors }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError(errorKey, "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError(errorKey, e.Message);
                }
            }

            // Extracting all errors from ModelState, including inner exceptions if they exist
            var errors = ModelState.Values.SelectMany(v => v.Errors)
                                          .Select(e =>
                                          {
                                              // Get the basic error message
                                              var errorMessage = e.ErrorMessage;

                                              // Check for an exception and add inner exception details if present
                                              if (e.Exception != null)
                                              {
                                                  var exceptionMessage = e.Exception.Message;
                                                  var innerExceptionMessage = e.Exception.InnerException?.Message;

                                                  // Append inner exception details if available
                                                  errorMessage += $" Exception: {exceptionMessage}";
                                                  if (innerExceptionMessage != null)
                                                  {
                                                      errorMessage += $" InnerException: {innerExceptionMessage}";
                                                  }
                                              }

                                              return errorMessage;
                                          })
                                          .ToList();

            if (errors.Any())
            {
                return Json(new { Errors = errors }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Errors = "", Id = model.Id }, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult GetDescription(PropertyCardVM fields)
        {
            var description = _propertyCardService.GetDescription(fields);
            var stockNo = _propertyCardService.GetStockNo(fields);

            return Json(new { Description = description, StockNo = stockNo }, JsonRequestBehavior.AllowGet);
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
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    var entity = await _propertyCardService.PsCardItem.GetByIdAsync(model.Id);

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


        public ActionResult ItemRead([DataSourceRequest] DataSourceRequest request, Guid? cardId)
        {
            var data = _propertyCardService.PsCardItem.GetByCardId(cardId);

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

        public ActionResult IssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? cardItemId)
        {
            var data = _propertyCardService.PsCardItemIssuance.GetByCardItemId(cardItemId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> IssuanceCreate([DataSourceRequest] DataSourceRequest request, PsCardItemIssuanceVM model)
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

                    model = await _propertyCardService.PsCardItemIssuance.CreateAsync(model, user, date);
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
        public async Task<ActionResult> IssuanceUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemIssuanceVM model)
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

                    model = await _propertyCardService.PsCardItemIssuance.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> IssuanceDestroy([DataSourceRequest]DataSourceRequest request, PsCardItemIssuanceVM model)
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
                    ModelState.Clear();
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _propertyCardService.PsCardItemIssuance.DeleteAsync(model, user, date);
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

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> LoadFields([System.Web.Http.FromBody] PropertyCardVM model)
        //{
        //    if (model.Id != Guid.Empty)
        //    {
        //        //model = await _cardService.GetVmByIdAsync(model.Id);
        //        var allField = await _propertyCardService.AllField.GetByIdAsync(model.Id);
        //        if (allField != null)
        //        {
        //            model.AllField = allField;
        //        }
        //    }
        //    string partialView = "";
        //    if (Enum.TryParse(model.ItemTypeCode, out Category c))
        //    {
        //        if (c == CatLandsProp())
        //        {
        //            partialView = "_FieldLand";
        //        }
        //        else if (c == CatMachineriesProp()
        //            || c == CatTransportationProp()
        //            || c == CatFurnituresProp()
        //            || c == CatOtherProperties()
        //            || c == CatMedicalSupply()
        //            || c == CatAgriculturalSupply()
        //            || c == CatAnimalSupplies()
        //            || c == CatConstructionMaterialsSupply()
        //            || c == CatOfficeSupplies()
        //            || c == CatAccountableFormsSupply()
        //            || c == CatNonAccountableFornsSupply()
        //            || c == CatMilitarySupply()
        //            || c == CatOtherSupplies())
        //        {
        //            partialView = "_FieldBrand";
        //        }
        //        else if (c == CatDrugsSupply())
        //        {
        //            partialView = "_FieldDrugs";
        //        }
        //        else if (c == CatRepairSupply())
        //        {
        //            partialView = "_FieldSerial";
        //        }
        //    }
        //    return PartialView(partialView, model);
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
            //string partialView = AllFieldsUtil.GetPartialField(model.ItemTypeCode, model.ItemCode);

            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);
            
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

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> LoadItemFields([System.Web.Http.FromBody] PsCardItemVM model)
        //{
        //    var psCard = await _propertyCardService.GetByIdAsync((Guid)model.PsCardId);
        //    if (model.Id != Guid.Empty)
        //    {
        //        var data = await _propertyCardService.PsCardItem.GetByIdAsync(model.Id);
        //        model = _propertyCardService.PsCardItem.TransferItemField(data, model);

        //    }
        //    string partialView = "";
        //    if (Enum.TryParse(psCard.ItemTypeCode, out Category c))
        //    {
        //        if (c == CatLandsProp())
        //        {
        //            partialView = "_ItemFieldLand";
        //        }
        //        else if (c == CatMachineriesProp()
        //            || c == CatTransportationProp()
        //            || c == CatFurnituresProp()
        //            || c == CatOtherProperties()
        //            || c == CatMedicalSupply()
        //            || c == CatAgriculturalSupply()
        //            || c == CatAnimalSupplies()
        //            || c == CatConstructionMaterialsSupply()
        //            || c == CatOfficeSupplies()
        //            || c == CatAccountableFormsSupply()
        //            || c == CatNonAccountableFornsSupply()
        //            || c == CatMilitarySupply()
        //            || c == CatOtherSupplies())
        //        {
        //            partialView = "_ItemFieldBrand";
        //        }
        //        else if (c == CatDrugsSupply())
        //        {
        //            partialView = "_ItemFieldDrugs";
        //        }
        //        else if (c == CatRepairSupply())
        //        {
        //            partialView = "_ItemFieldSerial";
        //        }
        //    }
        //    return PartialView(partialView, model);
        //}


        #region PRINTOUTS

        public ActionResult StockCardRpt(Guid? selectedId)
        {
            string stringname = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(stringname);

            string un = decoder.UserID;
            string pw = decoder.Password;
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

        [HttpPost]
        public ActionResult GetItemExtnTemplate(Guid? id)
        {
            string itemExtnName = _propertyCardService.GetItemExtnName(id);

            return Json(new { Errors = "", ItemExtnName = itemExtnName }, JsonRequestBehavior.AllowGet);
        }

        #region ITEMEXTN VEHICLES
        public ActionResult _ItemExtnVehicleRead([DataSourceRequest] DataSourceRequest request, Guid? psCardItemId)
        {
            var data = _propertyCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnVehicle.GetByPsCardItemId(psCardItemId);
            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ItemExtnVehicleCreate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnVehicle model)
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

                    model = await _propertyCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnVehicle.CreateAsync(model, user, date);

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
        public async Task<ActionResult> _ItemExtnVehicleUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnVehicle model)
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

                    model = await _propertyCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnVehicle.UpdateAsync(model, user, date);

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
        public async Task<ActionResult> _ItemExtnVehicleDestroy([DataSourceRequest]DataSourceRequest request, PsCardItemExtnVehicle model)
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

                    model = await _propertyCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnVehicle.DeleteAsync(model, user, date);
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
        public ActionResult _ItemExtnOtherRead([DataSourceRequest] DataSourceRequest request, Guid? psCardItemId)
        {
            var data = _propertyCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnOther.GetByPsCardItemId(psCardItemId);
            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ItemExtnOtherCreate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnOther model)
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

                    model = await _propertyCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnOther.CreateAsync(model, user, date);

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
        public async Task<ActionResult> _ItemExtnOtherUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnOther model)
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

                    model = await _propertyCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnOther.UpdateAsync(model, user, date);

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
        public async Task<ActionResult> _ItemExtnOtherDestroy([DataSourceRequest]DataSourceRequest request, PsCardItemExtnOther model)
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

                    model = await _propertyCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnOther.DeleteAsync(model, user, date);
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

        #region AJAX CALLS
        [HttpGet]
        public ActionResult GetEndSeries(string startSeries, Guid? itemId)
        {
            string endSeries = _propertyCardService.PsCardItem.PsCardItemExtn.GetEndSeries(startSeries, itemId);

            return Json(new { Errors = "", EndSeries = endSeries }, JsonRequestBehavior.AllowGet);
        }
        #endregion
    }
}