using iLgs.Models;
using iLgs.Services.Items;
using iLgs.Services.ParIcs;
using iLgs.Services.PropertyCard;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using static iLgs.Models.Enums;

namespace iLgs.Controllers
{
    [AppAuthorize("ITEMCARD")]
    public class ItemCardController : BaseController
    {
        private readonly AppManEntities _db;
        private readonly IPsCardService _psCardService;
        private readonly IParIcsUploadService _uploadService;
        private readonly IItemCodeService _itemCodeService;
        //private readonly string _stockId, _ppeId, _transpoId;

        public ItemCardController()
        {
            _db = new AppManEntities();
            _psCardService = new PsCardService(_db);
            _uploadService = new ParIcsUploadService(_db);
            _itemCodeService = new ItemCodeService(_db);
        }

        public ActionResult Supplies()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)AccountGroup.SUPPLIES;
            ViewBag.RefType = "I";
            ViewBag.Title = "Supplies Item Card";
            
            return View("Index");
        }

        public ActionResult Equipment()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)AccountGroup.PPE;
            ViewBag.RefType = "P";
            ViewBag.Title = "Equipment Item Card";
            
            return View("Index");
        }
        
        public ActionResult Vehicle()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)AccountGroup.VEHICLE;
            ViewBag.RefType = "P";
            ViewBag.Title = "Vehicles Item Card";
            
            return View("Index");
        }

        public ActionResult Index()
        {
            if (TempData["AllowIndexAccess"] == null || !(bool)TempData["AllowIndexAccess"])
            {
                ViewBag.Error = "Access Denied!";
                return View("Error"); // Or some other handling
            }
            return View();
        }

        public ActionResult Read([DataSourceRequest] DataSourceRequest request, int? accountGroup)
        {
            var data = _psCardService.PsCardItemExtn.PsCardItemExtnUpdate.GetAll(accountGroup);
            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        public ActionResult _ItemCardEntry(Guid? psCardItemExtnId, int? accountGroup)
        {
            ViewData["psCardItemExtnId"] = psCardItemExtnId;
            if (accountGroup == (int?)AccountGroup.PPE || accountGroup == (int?)AccountGroup.SUPPLIES)
            {
                var model = _psCardService.PsCardItemExtn.PsCardItemExtnUpdate.GetCardItemExtnPpeEntry(psCardItemExtnId);
                return PartialView("_ItemCardPpeEntry", model);
            }
            else
            {
                return PartialView();
            }
        }

        public ActionResult _ParIcs(Guid id, string propNo, string refType)
        {
            ViewData["Id"] = id;
            ViewData["PropNo"] = propNo;
            ViewBag.RefType = refType;

            return PartialView();
        }

        public ActionResult _ParIcsRead([DataSourceRequest] DataSourceRequest request, string propNo, string refType)
        {
            var data = _psCardService.PsCardItemExtn.IcsPar.GetAllByPropNo(propNo, refType);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        public ActionResult _Images(Guid? imageId, string postedBy)
        {
            ViewData["imageId"] = imageId;
            ViewData["postedBy"] = postedBy;
            return PartialView();
        }

        
        public ActionResult _ImagesRead([DataSourceRequest] DataSourceRequest request, Guid imageId)
        {
            var data = _uploadService.GetAllByImageId(imageId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        public async Task<ActionResult> PreviewUpload(Guid id)
        {
            var fileResult = await _uploadService.GetUploadedFileAsync(id);
            if (fileResult != null)
            {
                return fileResult; // Return the file result directly
            }
            else
            {
                return HttpNotFound("File not found"); // Handle not found case
            }
        }

        #region AJAX CALLS
        [HttpPost]
        public ActionResult GetItemExtnTemplate(Guid? psCarItemExtnid)
        {
            string itemExtnName = _psCardService.GetItemExtnNameByItmExtnId(psCarItemExtnid);

            return Json(new { Errors = "", ItemExtnName = itemExtnName }, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> LoadPpeFields([System.Web.Http.FromBody] PsCardItemExtnPpeEntryVM model)
        {
            if (model.Id != Guid.Empty)
            {
                var allField = await _psCardService.AllField.GetByItemExtnIdAsync(model.Id);
                if (allField != null)
                {
                    model.AllField = allField;
                }
            }
            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);

            if (!string.IsNullOrWhiteSpace(partialView))
            {
                partialView = $"_Ppe{partialView}";
            }

            return PartialView(partialView, model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> LoadSuppliesFields([System.Web.Http.FromBody] PsCardItemExtnSuppliesEntryVM model)
        {
            if (model.Id != Guid.Empty)
            {
                var allField = await _psCardService.AllField.GetByItemExtnIdAsync(model.Id);
                if (allField != null)
                {
                    model.AllField = allField;
                }
            }
            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);

            if (!string.IsNullOrWhiteSpace(partialView))
            {
                partialView = $"_Stock{partialView}";
            }

            return PartialView(partialView, model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> LoadVehicleFields([System.Web.Http.FromBody] PsCardItemExtnVehicleEntryVM model)
        {
            if (model.Id != Guid.Empty)
            {
                var allField = await _psCardService.AllField.GetByItemExtnIdAsync(model.Id);
                if (allField != null)
                {
                    model.AllField = allField;
                }
            }
            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);

            if (!string.IsNullOrWhiteSpace(partialView))
            {
                partialView = $"_Vehicle{partialView}";
            }

            return PartialView(partialView, model);
        }

        #endregion
    }
}