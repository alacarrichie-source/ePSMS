using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using iLgs.Models;
using iLgs.Services;
using iLgs.Services.Interfaces;
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
using static iLgs.Models.CategoryEnum;

namespace iLgs.Controllers
{
    [AppAuthorize("STOCKCARD")]
    public class StockCardController : BaseController
    {
        private readonly string _cardCategory = "S";
        private AppManEntities _db = new AppManEntities();
        private ICodextnService _codextnService;
        private IPsCardService _psCardService;
        //private IPsCardItemService _psCardItemService;
        //private IPsCardItemIssuanceService _psCardItemIssuanceService;
        //private IAllFieldService _allFieldService;

        public StockCardController()
        {
            _codextnService = new CodextnService(_db);
            _psCardService = new PsCardService(_db);
            //_psCardItemService = new PsCardItemService(_db);
            //_psCardItemIssuanceService = new PsCardItemIssuanceService(_db);
            //_allFieldService = new AllFieldService(_db);
        }

        // GET: Index
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Read([DataSourceRequest] DataSourceRequest request)
        {
            var data = _psCardService.StockCard.GetAll();

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

                    var result = await _psCardService.StockCard.CreateAsync(model, user, date);
                    if (result.IsSuccess)
                    {
                        return Json(new[] { result.Data }.ToDataSourceResult(request, ModelState));
                    }
                    return Json(new { Errors = result.Errors }, JsonRequestBehavior.DenyGet);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }
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

                    var result = await _psCardService.StockCard.UpdateAsync(model, user, date);
                    if (result.IsSuccess)
                    {
                        return Json(new[] { result.Data }.ToDataSourceResult(request, ModelState));
                    }
                    return Json(new { Errors = result.Errors }, JsonRequestBehavior.DenyGet);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }
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

                    var result = await _psCardService.StockCard.DeleteAsync(model, user, date);
                    if (result.IsSuccess)
                    {
                        return Json(new[] { result.Data }.ToDataSourceResult(request, ModelState));
                    }
                    return Json(new { Errors = result.Errors }, JsonRequestBehavior.DenyGet);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("DeleteError", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public async Task<ActionResult> _StockCardAddEdit(Guid? cardId)
        {
            var data = await _psCardService.StockCard.GetByIdAsync(cardId);
            if (data == null)
            {
                data = new StockCardVM()
                {
                    CardCategory = _cardCategory
                };
            }
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

                    var entity = await _psCardService.StockCard.GetByIdAsync(model.Id);

                    if (entity == null)
                    {
                        if (access.AllowAdd)
                        {
                            var result = await  _psCardService.StockCard.CreateAsync(model, user, date);
                            if (!result.IsSuccess)
                            {
                                return Json(new { Errors = string.Join("; ", result.Errors.Select(e => e.Value)) }, JsonRequestBehavior.DenyGet);
                            }
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
                            var result = await _psCardService.StockCard.UpdateAsync(model, user, date);
                            if (!result.IsSuccess)
                            {
                                return Json(new { Errors = string.Join("; ", result.Errors.Select(e => e.Value)) }, JsonRequestBehavior.DenyGet);
                            }
                        }
                        else
                        {
                            errorKey = "UpdateError";
                            ModelState.AddModelError(errorKey, "Access Denied!");
                        }
                    }
                    
                }
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

            var query = from state in ModelState.Values
                        from error in state.Errors
                        select error.ErrorMessage;

            var errorList = query.ToList();
            if (errorList.Count() > 0)
            {
                return Json(new { Errors = errorList }, JsonRequestBehavior.DenyGet);
            }

            return Json(new { Errors = "", Id = model.Id}, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult GetDescription(StockCardVM fields)
        {
            var description = _psCardService.GetDescription(fields);
            var stockNo = _psCardService.GetStockNo(fields);

            return Json(new { Description = description, StockNo = stockNo }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult _StockCardItem(Guid cardId, string category)
        {                        
            ViewBag.FieldSw = _psCardService.GetFieldSw(category);

            ViewData["partialView"] = _psCardService.GetItemFieldsPartialView(category);
            ViewData["cardId"] = cardId;
            ViewData["category"] = category;            

            return PartialView();
        }

        public async Task<ActionResult> _StockCardItemAddEdit(Guid cardId, Guid? cardItemId)
        {
            var data = await _psCardService.StockCard.PsCardItem.GetByIdAsync(cardItemId);
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

                    var entity = await _psCardService.StockCard.PsCardItem.GetByIdAsync(model.Id);

                    if (entity == null)
                    {
                        model = await _psCardService.StockCard.PsCardItem.CreateAsync(model, user, date);
                    }
                    else
                    {
                        model = await _psCardService.StockCard.PsCardItem.UpdateAsync(model, user, date);
                    }                    
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }
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
            var data = _psCardService.StockCard.PsCardItem.GetByCardId(cardId);

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

                    model = await _psCardService.StockCard.PsCardItem.CreateAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }
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

                    model = await _psCardService.StockCard.PsCardItem.UpdateAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }
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

                    model = await _psCardService.StockCard.PsCardItem.DeleteAsync(model, user, date);
                    // TO DO: update stocks
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("DeleteError", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public ActionResult IssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? cardItemId)
        {
            var data = _psCardService.StockCard.PsCardItemIssuance.GetByCardItemId(cardItemId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> IssuanceCreate([DataSourceRequest] DataSourceRequest request, PsCardItemIssuanceVM model)
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

                    model = await _psCardService.StockCard.PsCardItemIssuance.CreateAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> IssuanceUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemIssuanceVM model)
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

                    model = await _psCardService.StockCard.PsCardItemIssuance.UpdateAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> IssuanceDestroy([DataSourceRequest]DataSourceRequest request, PsCardItemIssuanceVM model)
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
                    ModelState.Clear();
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _psCardService.StockCard.PsCardItemIssuance.DeleteAsync(model, user, date);
                    // TO DO: update stocks
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("DeleteError", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> LoadFields([System.Web.Http.FromBody] StockCardVM model)
        {
            if (model.Id != Guid.Empty)
            {
                //model = await _cardService.GetVmByIdAsync(model.Id);
                var allField = await _psCardService.StockCard.AllField.GetByIdAsync(model.Id);
                if (allField != null)
                {
                    model.AllField = allField;
                }
            }
            string partialView = "";
            if (Enum.TryParse(model.ItemTypeCode, out Category c))
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

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> LoadItemFields([System.Web.Http.FromBody] PsCardItemVM model)
        {
            var psCard = await _psCardService.GetByIdAsync((Guid)model.PsCardId);
            if (model.Id != Guid.Empty)
            {
                var data = await _psCardService.StockCard.PsCardItem.GetByIdAsync(model.Id);
                model = _psCardService.StockCard.PsCardItem.TransferItemField(data, model);

            }
            string partialView = "";
            if (Enum.TryParse(psCard.ItemCode.ItemType.Code, out Category c))
            {
                if (c == CatLands())
                {
                    partialView = "_ItemFieldLand";
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
                    partialView = "_ItemFieldBrand";
                }
                else if (c == CatDrugs())
                {
                    partialView = "_ItemFieldDrugs";
                }
                else if (c == CatRepairs())
                {
                    partialView = "_ItemFieldSerial";
                }
            }
            return PartialView(partialView, model);
        }


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
                table.ApplyLogOnInfo(logonInfo);
            }

            var stockNo = _psCardService.GetById((Guid)selectedId)?.PsNo;
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

            string itemExtnName = _psCardService.GetItemExtnName(id);

            return Json(new { Errors = "", ItemExtnName = itemExtnName }, JsonRequestBehavior.AllowGet);

        }

        #region ITEMEXTN VEHICLES
        public ActionResult _ItemExtnVehicleRead([DataSourceRequest] DataSourceRequest request, Guid? psCardItemId)
        {
            var data = _psCardService.StockCard.PsCardItem.PsCardItemExtn.PsCardItemExtnVehicle.GetByPsCardItemId(psCardItemId);
            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ItemExtnVehicleCreate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnVehicle model)
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

                    model = await _psCardService.StockCard.PsCardItem.PsCardItemExtn.PsCardItemExtnVehicle.CreateAsync(model, user, date);

                    // TO DO: save to stock card
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ItemExtnVehicleUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnVehicle model)
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

                    model = await _psCardService.StockCard.PsCardItem.PsCardItemExtn.PsCardItemExtnVehicle.UpdateAsync(model, user, date);

                    // TO DO: update stock card
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ItemExtnVehicleDestroy([DataSourceRequest]DataSourceRequest request, PsCardItemExtnVehicle model)
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

                    model = await _psCardService.StockCard.PsCardItem.PsCardItemExtn.PsCardItemExtnVehicle.DeleteAsync(model, user, date);
                    // TO DO: update stocks
                }

            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("DeleteError", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }
        #endregion  


        #region ITEMEXTN OTHERS
        public ActionResult _ItemExtnOtherRead([DataSourceRequest] DataSourceRequest request, Guid? psCardItemId)
        {
            var data = _psCardService.StockCard.PsCardItem.PsCardItemExtn.PsCardItemExtnOther.GetByPsCardItemId(psCardItemId);
            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ItemExtnOtherCreate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnOther model)
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

                    model = await _psCardService.StockCard.PsCardItem.PsCardItemExtn.PsCardItemExtnOther.CreateAsync(model, user, date);

                    // TO DO: save to stock card
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ItemExtnOtherUpdate([DataSourceRequest] DataSourceRequest request, PsCardItemExtnOther model)
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

                    model = await _psCardService.StockCard.PsCardItem.PsCardItemExtn.PsCardItemExtnOther.UpdateAsync(model, user, date);

                    // TO DO: update stock card
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ItemExtnOtherDestroy([DataSourceRequest]DataSourceRequest request, PsCardItemExtnOther model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stock_card");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("GridError", "Delete Access Denied!");
                }
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _psCardService.StockCard.PsCardItem.PsCardItemExtn.PsCardItemExtnOther.DeleteAsync(model, user, date);
                    // TO DO: update stocks
                }

            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }
        #endregion  
    }
}