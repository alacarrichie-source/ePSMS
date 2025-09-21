using iLgs.Services.Items;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [Authorize]
    public class ItemListController : Controller
    {
        private readonly IItemCodeService _itemCodeService;
        private readonly IItemUploadService _itemUploadService;

        public ItemListController(IItemCodeService itemCodeService,
            IItemUploadService itemUploadService)
        {
            _itemCodeService = itemCodeService;
            _itemUploadService = itemUploadService;
        }

        // GET: ItemList
        public ActionResult Index()
        {
            return View();
        }

        //[HttpGet]
        public ActionResult Read([DataSourceRequest] DataSourceRequest request)
        {
            var model = _itemCodeService.GetAll();
            return new JsonNetResult { Data = model.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        //[HttpGet]
        public async Task<ActionResult> Details(Guid? itemId)
        {
            var model = await _itemCodeService.GetByIdAsync(itemId);

            if (model == null)
            {
                return RedirectToAction("Index", "Home");
            }

            AddToRecentlyViewed(itemId);

            //if (!string.IsNullOrEmpty(model.SubCategory))
            //{
            //    ViewBag.SubCategory = model.SubCategory;
            //}

            return View(model);
        }

        private void AddToRecentlyViewed(Guid? itemId)
        {
            var key = "_RecentlyViewed";
            var value = Session[key] as Queue<Guid?>;

            if (value == null)
            {
                value = new Queue<Guid?>(4);
                value.Enqueue(itemId);
            }
            else if (!value.Contains(itemId))
            {
                if (value.Count == 4)
                {
                    value.Dequeue();
                }
                value.Enqueue(itemId);
            }

            Session[key] = value;
        }

        [HttpGet]
        public async Task<ActionResult> GetThumbnailPhotoById(Guid? photoId)
        {
            var fileResult = await _itemUploadService.GetThumbnailPhotoByIdAsync(photoId);

            if (fileResult != null)
            {
                return fileResult; // Return the file result directly
            }
            else
            {
                return File("~/Assets/NoImageAvailable.png", "image/png");
            }
        }

        [HttpGet]
        public async Task<ActionResult> GetLargePhotoById(Guid? photoId)
        {
            var fileResult = await _itemUploadService.GetLargePhotoByIdAsync(photoId);

            if (fileResult != null)
            {
                return fileResult; // Return the file result directly
            }
            else
            {                
                return File("~/Assets/NoImageAvailable.png", "image/png");                
            }
        }
    }
}