using CrystalDecisions.CrystalReports.Engine;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services;
using iLgs.Services.Codes;
using iLgs.Services.CustodianReports;
using iLgs.Services.CustodianUploads;
using iLgs.Services.Items;
using iLgs.Services.ParIcs;
using iLgs.Services.PropertyCard;
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
    [AppAuthorize("PROPNOUPDATE")]
    public class PropNoItemController : BaseController
    {
        private readonly AppManEntities _db;
        private readonly IPsCardItemExtnService _psCardItemExtnService;
        private readonly IParIcsUploadService _uploadService;
        //private readonly string _stockId, _ppeId, _transpoId;

        public PropNoItemController()
        {
            _db = new AppManEntities();
            _psCardItemExtnService = new PsCardItemExtnService(_db);
            _uploadService = new ParIcsUploadService(_db);
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

        #region SUPPLIES ITEM
        public ActionResult Read([DataSourceRequest] DataSourceRequest request, int? accountGroup)
        {
            var data = _psCardItemExtnService.PsCardItemExtnUpdate.GetAll(accountGroup);
            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }


        //public async Task<ActionResult> _SuppliesDestroy([DataSourceRequest]DataSourceRequest request, CustodianReportItemStockVM model)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), _stockId);
        //        Access access = await accessTask;
        //        if (!access.AllowDelete)
        //        {
        //            ModelState.AddModelError("DeleteError", "Delete Access Denied!");
        //        }

        //        if (ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            model = await _custodianReportItemStockService.DeleteAsync(model, user, date);
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
        #endregion


        public ActionResult _items(Guid id, string propNo, string refType)
        {
            ViewData["Id"] = id;
            ViewData["PropNo"] = propNo;
            ViewBag.RefType = refType;

            return PartialView();
        }

        public ActionResult _ItemsRead([DataSourceRequest] DataSourceRequest request, string propNo, string refType)
        {
            var data = _psCardItemExtnService.IcsPar.GetAllByPropNo(propNo, refType);
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

        
    }
}