using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.ParIcsFromPo;
using iLgs.Services.PropertyCard;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using static iLgs.Models.Enums;

namespace iLgs.Controllers
{
    [AppAuthorize("PARSETORDER")]
    public class ParSetOrderController : BaseController
    {
        //private readonly AppManEntities _db;
        private readonly IIcsParService _icsParService;
        private readonly IPsCardService _psCardService;
        private readonly ICodextnService _codextnService;

        public ParSetOrderController()
        {
            //_db = new AppManEntities();
            _icsParService = new IcsParService(_db);
            _psCardService = new PsCardService(_db);
            _codextnService = new CodextnService(_db);
        }

        //public ParSetController(IIcsParService icsParService, IPsCardService psCardService, ICodextnService codextnService)
        //{
        //    _icsParService = icsParService;
        //    _psCardService = psCardService;
        //    _codextnService = codextnService;
        //}

        // GET: PARs
        public ActionResult Index()
        {
            ViewBag.ForYear = DateTime.Now.Year;
            return View();
        }

        public ActionResult Read([DataSourceRequest] DataSourceRequest request, int? forYear)
        {
            var data = _icsParService.ParService.GetAllPo(forYear);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        #region PO ITEMS
        public ActionResult _PoItems(string poNo)
        {
            ViewData["PoNo"] = poNo;
            return PartialView();
        }

        public async Task<ActionResult> _PoItemsRead([DataSourceRequest] DataSourceRequest request, string poNo, DateTime? poDate)
        {
            var data = (await _icsParService.ParService.GetItemsByPoNoAsync(poNo, poDate)).OrderBy(o => o.ItemNoIndex);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _PoItemsUpdate([DataSourceRequest] DataSourceRequest request, ParIcsItemVm model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _psCardService.PsCardItem.UpdateIsForICSAsync(model, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError("UpdateError", error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("UpdateError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("UpdateError", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public ActionResult _PoItemSetRead([DataSourceRequest] DataSourceRequest request, string poNo, DateTime? poDate)
        {
            var data = _icsParService.ParService.GetItemSetsByPoNo(poNo, poDate);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        public ActionResult _PoItemSetDescriptionRead([DataSourceRequest] DataSourceRequest request, Guid? unitGroupId)
        {
            var data = _icsParService.ParService.GetItemSetDescriptionsByUnitGroupId(unitGroupId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        public ActionResult _PoItemSetDescriptionItemRead([DataSourceRequest] DataSourceRequest request, Guid? unitGroupDescriptionId)
        {
            var data = _icsParService.ParService.GetItemSetDescriptionItemsByUnitGroupDescriptionId(unitGroupDescriptionId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> PostPar(string parNo)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _icsParService.ParService.PostAsync(parNo, user, date);
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
        public async Task<ActionResult> UnpostPar(string parNo)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
                Access access = await accessTask;
                if (!access.AllowUnpost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _icsParService.ParService.UnPostAsync(parNo, user, date);
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


        #region PAR ITEMS
        public ActionResult _Pars(Guid? cardItemGroupId, string postedBy)
        {
            ViewData["cardItemGroupId"] = cardItemGroupId;
            ViewData["postedBy"] = postedBy;
            return PartialView();
        }

        public ActionResult _ParsRead([DataSourceRequest] DataSourceRequest request, Guid? cardItemGroupId)
        {
            var data = _icsParService.IcsParItem.GetAllParItems(cardItemGroupId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ParsUpdate([DataSourceRequest] DataSourceRequest request, IcsParItem model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _icsParService.IcsParItem.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> _ParsDestroy([DataSourceRequest]DataSourceRequest request, IcsParItem model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    var result = await _icsParService.IcsParItem.DeleteAsync(model, user, date);
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

        public async Task<ActionResult> _ParItemEdit(Guid? parItemId)
        {
            var data = await _icsParService.IcsParItem.GetByIdAsync(parItemId);

            return PartialView(data);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ParItemSave(IcsParItem model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Update Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _icsParService.IcsParItem.UpdateAsync(model, user, date);
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

        public async Task<ActionResult> _GeneratePar(Guid? psCardItemId, string refType)
        {
            var date = DateTime.Now;
            var issued = await _db.Codextns.Where(w => w.CodeMast.Code == "ISSUED-BY").AsNoTracking().OrderByDescending(o => o.Code).FirstOrDefaultAsync();
            var psCardItem = await _icsParService.ParService.GetByIdAsync(psCardItemId);
            var model = new GenerateIcsParVM()
            {
                PsCardItemId = psCardItemId,
                Qty = psCardItem.ParBalance,
                Date = date,
                RefType = refType,
                IndSet = "I",
                IcsPar = new IcsPar() { ReceivedDate = date, IssuedDate = date, IssuedBy = issued?.Description, IssuedByPosition = issued?.Desc2, IssuedDept = issued?.Desc3 }
            };

            model.IcsPar.RefDate = model.Date;
            ViewData["psCardItemId"] = psCardItemId;
            ViewBag.ItemExtnName = _psCardService.GetItemExtnName(psCardItemId);

            return PartialView(model);
        }

        public ActionResult _GenerateParSet(Guid? unitGroupId, string refType)
        {
            var date = DateTime.Now;
            var issued = _db.Codextns.Where(w => w.CodeMast.Code == "ISSUED-BY").AsNoTracking().OrderByDescending(o => o.Code).FirstOrDefault();
            var model = new GenerateIcsParVM()
            {
                UnitGroupId = unitGroupId,
                Date = date,
                RefType = refType,
                IndSet = "S",
                IcsPar = new IcsPar() { ReceivedDate = date, IssuedDate = date, IssuedBy = issued?.Description, IssuedByPosition = issued?.Desc2, IssuedDept = issued?.Desc3 }
            };

            ViewData["unitGroupId"] = unitGroupId;

            return PartialView(model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> GeneratePar(GenerateIcsParVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    if (model.IndSet == "I")
                    {
                        await _icsParService.ParService.GeneratePAR(model, user, date);
                    }
                    else
                    {
                        await _icsParService.ParService.GenerateParSet(model, user, date);
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

        public ActionResult _GenerateParSelectionRead([DataSourceRequest] DataSourceRequest request, Guid? psCardItemId)
        {
            var data = _psCardService.PsCardItem.PsCardItemExtn.GetCardItemExtnForIcsParsByType(psCardItemId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        public ActionResult _GenerateParSelectionSetRead([DataSourceRequest] DataSourceRequest request, Guid? unitGroupId)
        {
            var data = _psCardService.PsCardItem.PsCardItemExtn.GetCardItemExtnSetForIcsParByUnitGroupId(unitGroupId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }
        #endregion

        #region PAR SET ITEM
        public ActionResult _ParSet(Guid? unitGroupId, Guid? cardItemId, string postedBy)
        {
            ViewData["unitGroupId"] = unitGroupId;
            ViewData["cardItemId"] = cardItemId;
            ViewData["postedBy"] = postedBy;
            return PartialView();
        }

        public ActionResult _ParSetRead([DataSourceRequest] DataSourceRequest request, Guid? unitGroupId, Guid? cardItemId)
        {
            var data = _icsParService.GetAllPars(unitGroupId, cardItemId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ParSetDestroy([DataSourceRequest]DataSourceRequest request, IcsPar model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    var result = await _icsParService.DeleteAsync(model, user, date);
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

        public async Task<ActionResult> _ParSetItemEdit(Guid? parItemId)
        {
            var data = await _icsParService.IcsParItem.GetByIdAsync(parItemId);

            return PartialView(data);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ParSetItemSave(IcsParItem model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Update Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _icsParService.IcsParItem.UpdateAsync(model, user, date);
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

        #region Issuance View
        //public ActionResult _Issuance(Guid? cardItemId)
        //{
        //    ViewData["CardItemId"] = cardItemId;
        //    return PartialView();
        //}

        //public ActionResult _IssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? cardItemId)
        //{
        //    var data = _psCardService.PsCardItemIssuance.GetByCardItemId(cardItemId);

        //    var result = new JsonNetResult
        //    {
        //        Data = data.ToDataSourceResult(request),
        //        JsonRequestBehavior = JsonRequestBehavior.AllowGet,
        //        Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
        //    };
        //    return result;
        //}
        #endregion

        #region ICS/PAR Item Issuance
        public ActionResult _IcsParItemIssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? icsParId)
        {
            var data = _icsParService.IcsParItem.GetIssuance(icsParId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _IcsParItemIssuanceUpdate([DataSourceRequest] DataSourceRequest request, IcsParItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "par");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _icsParService.IcsParItem.UpdateIssuanceAsync(model, user, date);
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

        #endregion

        public ActionResult GetAllPo(string text)
        {

            IQueryable<ParIcsPOGroupVM> model = null;

            if (string.IsNullOrEmpty(text))
            {
                model = _icsParService.ParService.GetAllPoCombo().AsQueryable<ParIcsPOGroupVM>();
            }
            else
            {
                text = text.Trim();
                model = _icsParService.ParService.GetAllPoCombo(text).AsQueryable<ParIcsPOGroupVM>();
            }

            //return Json(formattedModel, JsonRequestBehavior.AllowGet);

            return Json(model.Select(c => new
            {
                PoNo = c.PoNo,
                PoDate = c.PoDate,
                AirNo = c.AirNo,
                AirDate = c.AirDate,
                DeptId = c.DeptId,
                Department = c.Department
            }), JsonRequestBehavior.AllowGet);
        }

        #region Item Fields
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> LoadFields([System.Web.Http.FromBody] IcsParItem model)
        {
            var data = await _icsParService.IcsParItem.GetByIdAsync(model.Id);
            data.IcsPar = model.IcsPar;

            string partialView = "";
            var category = await _psCardService.PsCardItem.GetCategoryAsync(model.PsCardItemExtn.PsCardItemId);
            if (Enum.TryParse(category, out Category c))
            {
                if (c == CatLandsProp())
                {
                    partialView = "_FieldLand";
                }
                else if (c == CatTransportationProp())
                {
                    partialView = "_FieldTransportation";
                }
                else
                {
                    partialView = "_FieldOther";
                }
                //else if (c == CatMachineries() || c == CatTransportations() || c == CatFurnitures() || c == CatOtherProperties()
                //    || c == CatMedicals() || c == CatAgriculturals() || c == CatAnimalSupplies() || c == CatConstructionMaterials()
                //    || c == CatOfficeSupplies() || c == CatAccountableForms() || c == CatNonAccountableForns() || c == CatMilitaries()
                //    || c == CatOtherSupplies())
                //{
                //    partialView = "_FieldBrand";
                //}
                //else if (c == CatDrugs())
                //{
                //    partialView = "_FieldDrugs";
                //}
                //else if (c == CatRepairs())
                //{
                //    partialView = "_FieldSerial";
                //}
            }
            return PartialView(partialView, data);
        }
        #endregion        
    }
}