using iLgs.Models;
using iLgs.Services;
using iLgs.Services.Items;
using System;
using System.Linq;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [Authorize]
    public class ItemHelperController : Controller
    {
        private readonly IItemTypeService _itemTypeService;
        private readonly IItemCodeService _itemCodeService;
        
        public ItemHelperController(IItemTypeService itemTypeService, IItemCodeService itemCodeService)
        {
            _itemTypeService = itemTypeService;
            _itemCodeService = itemCodeService;        
        }

        public JsonResult GetItems(string text)
        {
            var model = _itemCodeService.GetItems(text).OrderBy(o => o.ItemType).ThenBy(o => o.ItemNoIndex);
            return Json(model.Select(c => new { Id = c.Id, ItemNoIndex = c.ItemNoIndex, Code = c.Code, Description = c.Description, Type = c.ItemType, TypeDesc = c.ItemTypeDesc, ItemNo = c.ItemNo, MainDesc = c.MainDesc, Account = c.Account, SubArticle = c.SubArticle, MainDescCode = c.MainDescCode }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetItemAccounts(string text)
        {
            var model = _itemCodeService.GetItemAccounts(text).OrderBy(o => o.ItemType).ThenBy(o => o.ItemNoIndex);
            return Json(model.Select(c => new
            {
                Id = c.Id,
                ItemNoIndex = c.ItemNoIndex,
                Code = c.Code,
                Description = c.Description,
                Type = c.ItemType,
                TypeDesc = c.Account,
                ItemNo = c.ItemNo,
                MainDesc = c.MainDesc,
                Account = c.Account,
                SubAccount1 = c.SubAccount1,
                SubAccount2 = c.SubAccount2,
                SubAccount3 = c.SubAccount3,
                SubAccount4 = c.SubAccount4,
                Article = c.Article,
                SubArticle = c.SubArticle,
                MainDescCode = c.MainDescCode,
                Category = c.Category
            }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPropertyItemAccounts(string text)
        {
            var model = _itemCodeService.GetItemAccountsByCategory("P", text).OrderBy(o => o.ItemType).ThenBy(o => o.ItemNoIndex);
            return Json(model.Select(c => new
            {
                Id = c.Id,
                ItemNoIndex = c.ItemNoIndex,
                Code = c.Code,
                Description = c.Description,
                Type = c.ItemType,
                TypeDesc = c.Account,
                ItemNo = c.ItemNo,
                MainDesc = c.MainDesc,
                Account = c.Account,
                SubAccount1 = c.SubAccount1,
                SubAccount2 = c.SubAccount2,
                SubAccount3 = c.SubAccount3,
                SubAccount4 = c.SubAccount4,
                Article = c.Article,
                SubArticle = c.SubArticle,
                MainDescCode = c.MainDescCode
            }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetStockItemAccounts(string text)
        {
            var model = _itemCodeService.GetItemAccountsByCategory("S", text).OrderBy(o => o.ItemType).ThenBy(o => o.ItemNoIndex);
            return Json(model.Select(c => new
            {
                Id = c.Id,
                ItemNoIndex = c.ItemNoIndex,
                Code = c.Code,
                Description = c.Description,
                Type = c.ItemType,
                TypeDesc = c.Account,
                ItemNo = c.ItemNo,
                MainDesc = c.MainDesc,
                Account = c.Account,
                SubAccount1 = c.SubAccount1,
                SubAccount2 = c.SubAccount2,
                SubAccount3 = c.SubAccount3,
                SubAccount4 = c.SubAccount4,
                Article = c.Article,
                SubArticle = c.SubArticle,
                MainDescCode = c.MainDescCode
            }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCustodianItemAll(string text)
        {
            var model = _itemCodeService.GetCustodianItemAll(text).OrderBy(o => o.ItemType).ThenBy(o => o.ItemNoIndex);
            return Json(model.Select(c => new
            {
                Id = c.Id,
                ItemNoIndex = c.ItemNoIndex,
                Code = c.Code,
                Description = c.Description,
                Type = c.ItemType,
                TypeDesc = c.Account,
                ItemNo = c.ItemNo,
                MainDesc = c.MainDesc,
                Account = c.Account,
                SubAccount1 = c.SubAccount1,
                SubAccount2 = c.SubAccount2,
                SubAccount3 = c.SubAccount3,
                SubAccount4 = c.SubAccount4,
                Article = c.Article,
                SubArticle = c.SubArticle,
                MainDescCode = c.MainDescCode,
                Category = c.Category
            }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCustodianMainAccounts(int? accountGroup, string text)
        {
            var model = _itemTypeService.GetCustodianMainAccounts(accountGroup, text).OrderBy(o => o.Code).ToList();
            // Insert "ALL" at the top
            model.Insert(0, new CustodianAccountVM { Id = Guid.Empty, Category = string.Empty, Code = string.Empty, MainAccount = "ALL" });

            return Json(model, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCustodianMainAccountVehiclesCategory(int? accountGroup, string category, string text)
        {
            var model = _itemTypeService.GetCustodianMainAccounts(accountGroup, text);

            if (!string.IsNullOrWhiteSpace(category))
            {
                if (category == "S")
                {
                    model = model.Where(w => w.Category == "S");
                }
                else
                {
                    model = model.Where(w => w.Category != "S");
                }
            }

            var modelList = model.OrderBy(o => o.Code).ToList();
            // Insert "ALL" at the top
            modelList.Insert(0, new CustodianAccountVM { Id = Guid.Empty, Category = string.Empty, Code = string.Empty, MainAccount = "ALL" });

            return Json(modelList, JsonRequestBehavior.AllowGet);
        }        

        public JsonResult GetCustodianItemPpe(string text)
        {
            var model = _itemCodeService.GetCustodianItemPpe(text).OrderBy(o => o.ItemType).ThenBy(o => o.ItemNoIndex);
            return Json(model.Select(c => new
            {
                Id = c.Id,
                ItemNoIndex = c.ItemNoIndex,
                Code = c.Code,
                Description = c.Description,
                Type = c.ItemType,
                TypeDesc = c.Account,
                ItemNo = c.ItemNo,
                MainDesc = c.MainDesc,
                Account = c.Account,
                SubAccount1 = c.SubAccount1,
                SubAccount2 = c.SubAccount2,
                SubAccount3 = c.SubAccount3,
                SubAccount4 = c.SubAccount4,
                Article = c.Article,
                SubArticle = c.SubArticle,
                MainDescCode = c.MainDescCode,
                Category = c.Category
            }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCustodianItemStocks(string text)
        {
            var model = _itemCodeService.GetCustodianItemStocks(text).OrderBy(o => o.ItemType).ThenBy(o => o.ItemNoIndex);
            return Json(model.Select(c => new
            {
                Id = c.Id,
                ItemNoIndex = c.ItemNoIndex,
                Code = c.Code,
                Description = c.Description,
                Type = c.ItemType,
                TypeDesc = c.Account,
                ItemNo = c.ItemNo,
                MainDesc = c.MainDesc,
                Account = c.Account,
                SubAccount1 = c.SubAccount1,
                SubAccount2 = c.SubAccount2,
                SubAccount3 = c.SubAccount3,
                SubAccount4 = c.SubAccount4,
                Article = c.Article,
                SubArticle = c.SubArticle,
                MainDescCode = c.MainDescCode,
                Category = c.Category
            }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCustodianItemVehicle(string text)
        {
            var model = _itemCodeService.GetCustodianItemVehicle(text).OrderBy(o => o.ItemType).ThenBy(o => o.ItemNoIndex);
            return Json(model.Select(c => new
            {
                Id = c.Id,
                ItemNoIndex = c.ItemNoIndex,
                Code = c.Code,
                Description = c.Description,
                Type = c.ItemType,
                TypeDesc = c.Account,
                ItemNo = c.ItemNo,
                MainDesc = c.MainDesc,
                Account = c.Account,
                SubAccount1 = c.SubAccount1,
                SubAccount2 = c.SubAccount2,
                SubAccount3 = c.SubAccount3,
                SubAccount4 = c.SubAccount4,
                Article = c.Article,
                SubArticle = c.SubArticle,
                MainDescCode = c.MainDescCode,
                Category = c.Category
            }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCustodianItemLand(string text)
        {
            var model = _itemCodeService.GetCustodianItemLand(text).OrderBy(o => o.ItemType).ThenBy(o => o.ItemNoIndex);
            return Json(model.Select(c => new
            {
                Id = c.Id,
                ItemNoIndex = c.ItemNoIndex,
                Code = c.Code,
                Description = c.Description,
                Type = c.ItemType,
                TypeDesc = c.Account,
                ItemNo = c.ItemNo,
                MainDesc = c.MainDesc,
                Account = c.Account,
                SubAccount1 = c.SubAccount1,
                SubAccount2 = c.SubAccount2,
                SubAccount3 = c.SubAccount3,
                SubAccount4 = c.SubAccount4,
                Article = c.Article,
                SubArticle = c.SubArticle,
                MainDescCode = c.MainDescCode,
                Category = c.Category
            }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCustodianItemBldg(string text)
        {
            var model = _itemCodeService.GetCustodianItemBldg(text).OrderBy(o => o.ItemType).ThenBy(o => o.ItemNoIndex);
            return Json(model.Select(c => new
            {
                Id = c.Id,
                ItemNoIndex = c.ItemNoIndex,
                Code = c.Code,
                Description = c.Description,
                Type = c.ItemType,
                TypeDesc = c.Account,
                ItemNo = c.ItemNo,
                MainDesc = c.MainDesc,
                Account = c.Account,
                SubAccount1 = c.SubAccount1,
                SubAccount2 = c.SubAccount2,
                SubAccount3 = c.SubAccount3,
                SubAccount4 = c.SubAccount4,
                Article = c.Article,
                SubArticle = c.SubArticle,
                MainDescCode = c.MainDescCode,
                Category = c.Category
            }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCustodianItemPpeWithNonArticles(string text)
        {
            var model = _itemCodeService.GetCustodianItemPpeWithNonArticles(text).OrderBy(o => o.ItemType).ThenBy(o => o.ItemNoIndex);
            return Json(model.Select(c => new
            {
                Id = c.Id,
                ItemNoIndex = c.ItemNoIndex,
                Code = c.Code,
                Description = c.Description,
                Type = c.ItemType,
                TypeDesc = c.Account,
                ItemNo = c.ItemNo,
                MainDesc = c.MainDesc,
                Account = c.Account,
                SubAccount1 = c.SubAccount1,
                SubAccount2 = c.SubAccount2,
                SubAccount3 = c.SubAccount3,
                SubAccount4 = c.SubAccount4,
                Article = c.Article,
                SubArticle = c.SubArticle,
                MainDescCode = c.MainDescCode,
                Category = c.Category
            }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCustodianItemStocksWithNonArticles(string text)
        {
            var model = _itemCodeService.GetCustodianItemStocksWithNonArticles(text).OrderBy(o => o.ItemType).ThenBy(o => o.ItemNoIndex);
            return Json(model.Select(c => new
            {
                Id = c.Id,
                ItemNoIndex = c.ItemNoIndex,
                Code = c.Code,
                Description = c.Description,
                Type = c.ItemType,
                TypeDesc = c.Account,
                ItemNo = c.ItemNo,
                MainDesc = c.MainDesc,
                Account = c.Account,
                SubAccount1 = c.SubAccount1,
                SubAccount2 = c.SubAccount2,
                SubAccount3 = c.SubAccount3,
                SubAccount4 = c.SubAccount4,
                Article = c.Article,
                SubArticle = c.SubArticle,
                MainDescCode = c.MainDescCode,
                Category = c.Category
            }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCustodianItemVehicleWithNonArticles(string text)
        {
            var model = _itemCodeService.GetCustodianItemVehicleWithNonArticles(text).OrderBy(o => o.ItemType).ThenBy(o => o.ItemNoIndex);
            return Json(model.Select(c => new
            {
                Id = c.Id,
                ItemNoIndex = c.ItemNoIndex,
                Code = c.Code,
                Description = c.Description,
                Type = c.ItemType,
                TypeDesc = c.Account,
                ItemNo = c.ItemNo,
                MainDesc = c.MainDesc,
                Account = c.Account,
                SubAccount1 = c.SubAccount1,
                SubAccount2 = c.SubAccount2,
                SubAccount3 = c.SubAccount3,
                SubAccount4 = c.SubAccount4,
                Article = c.Article,
                SubArticle = c.SubArticle,
                MainDescCode = c.MainDescCode,
                Category = c.Category
            }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCustodianItemLandWithNonArticles(string text)
        {
            var model = _itemCodeService.GetCustodianItemLandWithNonArticles(text).OrderBy(o => o.ItemType).ThenBy(o => o.ItemNoIndex);
            return Json(model.Select(c => new
            {
                Id = c.Id,
                ItemNoIndex = c.ItemNoIndex,
                Code = c.Code,
                Description = c.Description,
                Type = c.ItemType,
                TypeDesc = c.Account,
                ItemNo = c.ItemNo,
                MainDesc = c.MainDesc,
                Account = c.Account,
                SubAccount1 = c.SubAccount1,
                SubAccount2 = c.SubAccount2,
                SubAccount3 = c.SubAccount3,
                SubAccount4 = c.SubAccount4,
                Article = c.Article,
                SubArticle = c.SubArticle,
                MainDescCode = c.MainDescCode,
                Category = c.Category
            }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCustodianItemBldgWithNonArticles(string text)
        {
            var model = _itemCodeService.GetCustodianItemBldgWithNonArticles(text).OrderBy(o => o.ItemType).ThenBy(o => o.ItemNoIndex);
            return Json(model.Select(c => new
            {
                Id = c.Id,
                ItemNoIndex = c.ItemNoIndex,
                Code = c.Code,
                Description = c.Description,
                Type = c.ItemType,
                TypeDesc = c.Account,
                ItemNo = c.ItemNo,
                MainDesc = c.MainDesc,
                Account = c.Account,
                SubAccount1 = c.SubAccount1,
                SubAccount2 = c.SubAccount2,
                SubAccount3 = c.SubAccount3,
                SubAccount4 = c.SubAccount4,
                Article = c.Article,
                SubArticle = c.SubArticle,
                MainDescCode = c.MainDescCode,
                Category = c.Category
            }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetRpciAccounts(string text)
        {
            var model = _itemTypeService.GetRpciAccounts(text);
            var retModel = model.Select(c => new ItemTypeVM
            {
                Id = c.Id,
                Code = c.Code,
                Description = c.Description,
                Category = c.Category,
                GroupCode = c.GroupCode
            }).ToList();

            // Add "ALL" item
            retModel.Insert(0, new ItemTypeVM { Id = null, Code = "ALL", Description = "ALL", Category = "", GroupCode = "" });
            return Json(retModel, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetItemsByCategory(string category, string text)
        {
            var model = _itemCodeService.GetItemsByCategory(category, text).OrderBy(o => o.ItemType).ThenBy(o => o.ItemNoIndex);
            return Json(model.Select(c => new { Id = c.Id, ItemNoIndex = c.ItemNoIndex, Code = c.Code, Description = c.Description, Type = c.ItemType, TypeDesc = c.ItemTypeDesc, ItemNo = c.ItemNo, MainDesc = c.MainDesc, Account = c.Account, SubArticle = c.SubArticle, MainDescCode = c.MainDescCode }), JsonRequestBehavior.AllowGet);
            //return Json(model.Select(c => new { Id = c.Id, ItemNoIndex = c.ItemNoIndex, Code = c.Code, Description = c.Description, Type = c.ItemType, TypeDesc = c.ItemTypeDesc, ItemNo = c.ItemNo, MainDesc = c.MainDesc, Account = c.Account, SubArticle = c.SubArticle, MainDescCode = c.MainDescCode, FieldGroupNo = c.FieldGroupNo }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetItemsByTypeCode(string typeCode, string text)
        {
            var model = _itemCodeService.GetItemsByTypeCode(typeCode, text).OrderBy(o => o.ItemType).ThenBy(o => o.ItemNoIndex);
            return Json(model.Select(c => new { Id = c.Id, ItemNoIndex = c.ItemNoIndex, Code = c.Code, Description = c.Description, Type = c.ItemType, TypeDesc = c.ItemTypeDesc, ItemNo = c.ItemNo, MainDesc = c.MainDesc, Account = c.Account, SubArticle = c.SubArticle, MainDescCode = c.MainDescCode }), JsonRequestBehavior.AllowGet);
            //return Json(model.Select(c => new { Id = c.Id, ItemNoIndex = c.ItemNoIndex, Code = c.Code, Description = c.Description, Type = c.ItemType, TypeDesc = c.ItemTypeDesc, ItemNo = c.ItemNo, MainDesc = c.MainDesc, Account = c.Account, SubArticle = c.SubArticle, MainDescCode = c.MainDescCode, FieldGroupNo = c.FieldGroupNo }), JsonRequestBehavior.AllowGet);
        }        
    }
}