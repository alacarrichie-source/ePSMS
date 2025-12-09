using CrystalDecisions.CrystalReports.Engine;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.Items;
using iLgs.Services.StockCards;
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
    [AppAuthorize("STOCKCARD")]
    public class StockCardController : BaseController
    {
        private readonly string _cardCategory = "S";
        private readonly AppManEntities _db;
        private readonly IStockCardService _stockCardService;
        private readonly ICodextnService _codextnService;
        private readonly IItemCodeService _itemCodeService;
        private readonly IStockCardValidator _stockCardValidator;

        public StockCardController(AppManEntities db, IStockCardService stockCardService, ICodextnService codextnService,
            IItemCodeService itemCodeService, IStockCardValidator stockCardValidator)
        {
            _db = db;
            _codextnService = codextnService;
            _stockCardService = stockCardService;
            _itemCodeService = itemCodeService;
            _stockCardValidator = stockCardValidator;
        }

        // GET: Index
        public ActionResult Index()
        {
            return View();
        }
        
        public ActionResult Read([DataSourceRequest] DataSourceRequest request, string userName)
        {
            var data = _stockCardService.GetAll(userName);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Create([DataSourceRequest] DataSourceRequest request, StockCardVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("AddError", "Add Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _stockCardService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> Update([DataSourceRequest] DataSourceRequest request, StockCardVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _stockCardService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> Destroy([DataSourceRequest]DataSourceRequest request, StockCardVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _stockCardService.DeleteAsync(model, user, date);
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

        public ActionResult _StockCardAddEdit(Guid? cardId, string mode)
        {
            var data = _stockCardService.GetById(cardId);
            if (data == null)
            {
                data = new StockCardVM()
                {
                    CardCategory = _cardCategory
                };
            }
            data.Mode = mode;

            return PartialView(data);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _StockCardSave(StockCardVM model)
        {
            string errorKey = "";
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
                Access access = await accessTask;
                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    var entity = await _stockCardService.GetByIdAsync(model.Id);

                    if (entity == null)
                    {
                        if (access.AllowAdd)
                        {
                            model = await _stockCardService.CreateAsync(model, user, date);
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
                            model = await _stockCardService.UpdateAsync(model, user, date);
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
        public async Task<JsonResult> GetDescription(StockCardVM fields)
        {
            var description = _stockCardService.GetDescription(fields);
            var stockNo = await _stockCardService.GetStockNoAsync(fields);
            var psCard = await _stockCardService.GetByPsNoAsync(stockNo);

            Guid id = Guid.NewGuid();
            if (psCard != null)
            {
                id = psCard.Id;
            }

            fields.PsNo = stockNo;
            bool isDuplicateStockNo = false;
            if (fields.Mode == "A")
            {
                isDuplicateStockNo = await _stockCardValidator.IsPsNoAlreadyExistsAsync(fields, Mode.ADD);
            }
            else
            {
                isDuplicateStockNo = await _stockCardValidator.IsPsNoAlreadyExistsAsync(fields, Mode.EDIT);
            }

            return Json(new { Description = description, StockNo = stockNo, Id = id, IsDuplicateStockNo = isDuplicateStockNo }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult _StockCardItem(Guid cardId, string category)
        {
            ViewBag.FieldSw = _stockCardService.GetFieldSw(category);

            ViewData["partialView"] = _stockCardService.GetItemFieldsPartialView(category);
            ViewData["cardId"] = cardId;
            ViewData["category"] = category;

            return PartialView();
        }

        public async Task<ActionResult> _StockCardItemAddEdit(Guid cardId, Guid? cardItemId)
        {
            var data = await _stockCardService.PsCardItem.GetByIdAsync(cardItemId);
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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    var entity = await _stockCardService.PsCardItem.GetByIdAsync(model.Id);

                    if (entity == null)
                    {
                        model = await _stockCardService.PsCardItem.CreateAsync(model, user, date);
                    }
                    else
                    {
                        model = await _stockCardService.PsCardItem.UpdateAsync(model, user, date);
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
            var data = _stockCardService.PsCardItem.GetTransitByCardId(cardId, userName);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> ItemCreate([DataSourceRequest] DataSourceRequest request, PsCardItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _stockCardService.PsCardItem.CreateAsync(model, user, date);
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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _stockCardService.PsCardItem.UpdateAsync(model, user, date);
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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
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

                    model = await _stockCardService.PsCardItem.DeleteAsync(model, user, date);
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
        //    var data = _stockCardService.PsCardItemIssuance.GetByCardItemId(cardItemId);

        //    return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        //}

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> IssuanceCreate([DataSourceRequest] DataSourceRequest request, PsCardItemIssuanceVM model)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
        //        Access access = await accessTask;
        //        if (!access.AllowAdd)
        //        {
        //            ModelState.AddModelError("Access", "Access Denied!");
        //        }

        //        if (model != null && ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            model = await _stockCardService.PsCardItemIssuance.CreateAsync(model, user, date);
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
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
        //        Access access = await accessTask;
        //        if (!access.AllowEdit)
        //        {
        //            ModelState.AddModelError("Access", "Access Denied!");
        //        }

        //        if (ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            model = await _stockCardService.PsCardItemIssuance.UpdateAsync(model, user, date);
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
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
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

        //            model = await _stockCardService.PsCardItemIssuance.DeleteAsync(model, user, date);
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
        public async Task<ActionResult> LoadFields([System.Web.Http.FromBody] StockCardVM model)
        {
            if (model.Id != Guid.Empty)
            {
                //model = await _cardService.GetVmByIdAsync(model.Id);
                var allField = await _stockCardService.AllField.GetByIdAsync(model.Id);
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
            var psCard = _stockCardService.GetById((Guid)model.PsCardId);
            if (model.Id != Guid.Empty)
            {
                var data = await _stockCardService.PsCardItem.GetByIdAsync(model.Id);
                model = _stockCardService.PsCardItem.TransferItemField(data, model);

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
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/StockCard.rpt"));
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

            var stockNo = _stockCardService.GetById((Guid)selectedId)?.PsNo;
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

        public async Task<ActionResult> StockCardPoRpt(Guid? selectedItemId, int originalSw)
        {
            string stringname = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(stringname);
            string rptKey = ConfigurationManager.AppSettings["RptKey"];
            string un = decoder.UserID;
            string pw = rptKey; // decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            ReportClass rpt = new ReportClass();
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/Card_Po_.rpt"));
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

            var cardItem = await _stockCardService.PsCardItem.GetByIdAsync(selectedItemId);
            var poNo = cardItem.PoNo;
            var lgu = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault().Description;
            //var imagePath = _codextnService.GetByMastCode("DIRS").Where(w => w.Code == "IMAGE-ITEMS").FirstOrDefault().Description;

            rpt.SetParameterValue("@cPoNo", poNo);
            //rpt.SetParameterValue("ImagePath", imagePath);
            rpt.SetParameterValue("LGU", lgu);
            rpt.SetParameterValue("IsOriginal", originalSw == 1);

            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");
        }
        #endregion

        #region ITEMEXTN VEHICLES
        public ActionResult _ItemExtnVehicleRead([DataSourceRequest] DataSourceRequest request, Guid? transferId)
        {
            var data = _stockCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemVehicle.GetCardItemExtns(transferId);
            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ItemExtnVehicleCreate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnVehicleVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _stockCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemVehicle.CreateAsync(model, user, date);

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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _stockCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemVehicle.UpdateAsync(model, user, date);

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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _stockCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemVehicle.DeleteAsync(model, user, date);
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
            var data = _stockCardService.PsCardItem.PsCardItemTransfer.PsCardItemTransferItem.PsCardItemTransferItemOther.GetCardItemExtns(transferId);
            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ItemExtnOtherCreate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnOtherVM model, Guid? psCardItemId)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _stockCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnOther.CreateAsync(model, user, date);

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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _stockCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnOther.UpdateAsync(model, user, date);

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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _stockCardService.PsCardItem.PsCardItemExtn.PsCardItemExtnOther.DeleteAsync(model, user, date);
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
            var data = _stockCardService.PsCardItem.GetAllStocks();

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
            var itemTransfer = await _stockCardService.PsCardItem.PsCardItemTransfer.GetByIdAsync(id);
            string itemExtnName = _stockCardService.GetItemExtnName(itemTransfer.PsCardItemId);

            return Json(new { Errors = "", ItemExtnName = itemExtnName, ItemTransfer = itemTransfer }, JsonRequestBehavior.AllowGet);

        }

        [HttpGet]
        public ActionResult GetEndSeries(string startSeries, Guid? itemId)
        {

            string endSeries = _stockCardService.PsCardItem.PsCardItemExtn.GetEndSeries(startSeries, itemId);

            return Json(new { Errors = "", EndSeries = endSeries }, JsonRequestBehavior.AllowGet);

        }
        #endregion

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> PostRecord(Guid psCardId)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _stockCardService.PostAsync(psCardId, user, date);
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
        public async Task<ActionResult> UnpostRecord(Guid psCardId)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }


                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _stockCardService.UnpostAsync(psCardId, user, date);
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
        public async Task<ActionResult> PostItemRecord(Guid psCardItemId)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _stockCardService.PsCardItem.PostAsync(psCardItemId, user, date);
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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }


                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _stockCardService.PsCardItem.UnpostAsync(psCardItemId, user, date);
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

        public ActionResult _PostBatch()
        {
            return PartialView();
        }

        public ActionResult _UnpostBatch()
        {
            return PartialView();
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> PostBatchItemRecord(string userName)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _stockCardService.PsCardItem.PostBatchAsync(userName, user, date);
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
        public async Task<ActionResult> UnpostBatchItemRecord(string userName)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _stockCardService.PsCardItem.UnpostBatchAsync(userName, user, date);
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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _stockCardService.TransferPo(model.PsCardItemId, model.Id, user, date);
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