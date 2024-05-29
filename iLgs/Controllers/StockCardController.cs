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
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [AppAuthorize("STOCKCARD")]
    public class StockCardController : BaseController
    {
        private readonly string _cardCategory = "S";
        private AppManEntities _db = new AppManEntities();
        private ICodextnService _codextnService;
        private IPsCardService _cardService;
        private IPsCardItemService _cardItemService;
        private IPsCardItemIssuanceService _cardItemIssuanceService;

        public StockCardController()
        {
            _codextnService = new CodextnService(_db);
            _cardService = new PsCardService(_db, _cardCategory);
            _cardItemService = new PsCardItemService(_db);
            _cardItemIssuanceService = new PsCardItemIssuanceService(_db);
        }

        // GET: Index
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Read([DataSourceRequest] DataSourceRequest request)
        {
            var data = _cardService.GetAll();

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Create([DataSourceRequest] DataSourceRequest request, PsCardVM model)
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

                    model = await _cardService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> Update([DataSourceRequest] DataSourceRequest request, PsCardVM model)
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

                    model = await _cardService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> Destroy([DataSourceRequest]DataSourceRequest request, PsCardVM model)
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

                    model = await _cardService.DeleteAsync(model, user, date);
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
            var data = await _cardService.GetVmByIdAsync(cardId);
            if (data == null)
            {
                data = new PsCardVM()
                {
                    CardCategory = _cardCategory
                };
            }
            return PartialView(data);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _StockCardSave(PsCardVM model)
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

                    var entity = await _cardService.GetByIdAsync(model.Id);

                    if (entity == null)
                    {
                        if (access.AllowAdd)
                        {
                            model = await  _cardService.CreateAsync(model, user, date);
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
                            model = await _cardService.UpdateAsync(model, user, date);
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
        public JsonResult GetDescription(PsCardVM fields)
        {
            var description = _cardService.GetDescription(fields);
            var stockNo = _cardService.GetStockNo(fields);

            return Json(new { Description = description, StockNo = stockNo }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult _StockCardItem(Guid cardId)
        {
            ViewData["cardId"] = cardId;
            return PartialView();
        }

        public async Task<ActionResult> _StockCardItemAddEdit(Guid cardId, Guid? cardItemId)
        {
            var data = await _cardItemService.GetByIdAsync(cardItemId);
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

                    var entity = await _cardItemService.GetByIdAsync(model.Id);

                    if (entity == null)
                    {
                        model = await _cardItemService.CreateAsync(model, user, date);
                    }
                    else
                    {
                        model = await _cardItemService.UpdateAsync(model, user, date);
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
            var data = _cardItemService.GetByCardId(cardId);

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

                    model = await _cardItemService.CreateAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("AddError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("AddError", e.Message);
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
                    ModelState.AddModelError("UpdateError", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _cardItemService.UpdateAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("UpdateError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("UpdateError", e.Message);
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

                    model = await _cardItemService.DeleteAsync(model, user, date);
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
            var data = _cardItemIssuanceService.GetByCardItemId(cardItemId);

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

                    model = await _cardItemIssuanceService.CreateAsync(model, user, date);
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

                    model = await _cardItemIssuanceService.UpdateAsync(model, user, date);
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

                    model = await _cardItemIssuanceService.DeleteAsync(model, user, date);
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

        #region PRINTOUTS
        public ActionResult StockCardRpt(string stockNo)
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
    }
}