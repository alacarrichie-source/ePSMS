using iLgs.Models;
using iLgs.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [Authorize]
    public class ItemHelperController : Controller
    {
        private readonly AppManEntities _db;
        private readonly IItemTypeService _itemTypeService;
        private readonly IItemCodeService _itemCodeService;
        
        public ItemHelperController()
        {
            _db = new AppManEntities();
            _itemTypeService = new ItemTypeService(_db);
            _itemCodeService = new ItemCodeService(_db);        
        }

        public JsonResult GetItems(string text)
        {
            var model = _itemCodeService.GetItems(text);
            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Description, Type = c.ItemType, TypeDesc = c.ItemTypeDesc, ItemNo = c.ItemNo, MainDesc = c.MainDesc, Account = c.Account, SubArticle = c.SubArticle, MainDescCode = c.MainDescCode, FieldGroupNo = c.FieldGroupNo }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetItemAccounts(string text)
        {
            var model = _itemCodeService.GetItemAccounts(text);
            return Json(model.Select(c => new
            {
                Id = c.Id,
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

        public JsonResult GetPropertyItemAccounts(string text)
        {
            var model = _itemCodeService.GetItemAccountsByCategory("P", text);
            return Json(model.Select(c => new
            {
                Id = c.Id,
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
            var model = _itemCodeService.GetItemAccountsByCategory("S", text);
            return Json(model.Select(c => new
            {
                Id = c.Id,
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

        public JsonResult GetCustodianItemPpe(string text)
        {
            var model = _itemCodeService.GetCustodianItemPpe(text);
            return Json(model.Select(c => new
            {
                Id = c.Id,
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

        public JsonResult GetCustodianItemStocks(string text)
        {
            var model = _itemCodeService.GetCustodianItemStocks(text);
            return Json(model.Select(c => new
            {
                Id = c.Id,
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

        public JsonResult GetCustodianItemVehicle(string text)
        {
            var model = _itemCodeService.GetCustodianItemVehicle(text);
            return Json(model.Select(c => new
            {
                Id = c.Id,
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

        public JsonResult GetCustodianItemLand(string text)
        {
            var model = _itemCodeService.GetCustodianItemLand(text);
            return Json(model.Select(c => new
            {
                Id = c.Id,
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

        public JsonResult GetCustodianItemBldg(string text)
        {
            var model = _itemCodeService.GetCustodianItemBldg(text);
            return Json(model.Select(c => new
            {
                Id = c.Id,
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

        public JsonResult GetRpciAccounts(string text)
        {
            var model = _itemTypeService.GetRpciAccounts(text);
            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Description, Category = c.Category, GroupCode = c.GroupCode }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetItemsByCategory(string category, string text)
        {
            var model = _itemCodeService.GetItemsByCategory(category, text);
            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Description, Type = c.ItemType, TypeDesc = c.ItemTypeDesc, ItemNo = c.ItemNo, MainDesc = c.MainDesc, Account = c.Account, SubArticle = c.SubArticle, MainDescCode = c.MainDescCode, FieldGroupNo = c.FieldGroupNo }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetItemsByTypeCode(string typeCode, string text)
        {
            var model = _itemCodeService.GetItemsByTypeCode(typeCode, text);
            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Description, Type = c.ItemType, TypeDesc = c.ItemTypeDesc, ItemNo = c.ItemNo, MainDesc = c.MainDesc, Account = c.Account, SubArticle = c.SubArticle, MainDescCode = c.MainDescCode, FieldGroupNo = c.FieldGroupNo }), JsonRequestBehavior.AllowGet);
        }        
    }
}