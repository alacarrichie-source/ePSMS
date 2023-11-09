using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services
{
    public class ItemCodeService : IItemCodeService
    {
        private readonly AppManEntities db = new AppManEntities();

        public ItemCodeService(AppManEntities db)
        {
            this.db = db;
        }

        public IQueryable<ItemCodeVM> GetAll()
        {
            var data = db.ItemCodes
                .Select(s => new ItemCodeVM
                {
                    Id = s.Id,                    
                    ItemTypeId = s.ItemTypeId,
                    ItemNo = s.ItemNo,
                    ItemNoIndex = s.ItemNoIndex,
                    Code = s.Code,
                    Description = s.Description,     
                    ItemSw = s.ItemSw,
                    AccountCode = s.AccountCode,
                    InsertedDt = s.InsertedDt
                });
            return data;
        }

        public IQueryable<ItemCodeVM> GetAllByItemTypeId(Guid? itemTypeId)
        {            
            var data = db.ItemCodes.Where(w => w.ItemTypeId == itemTypeId).ToList()
                .Select(s => new ItemCodeVM
                {
                    Id = s.Id,
                    ItemTypeId = s.ItemTypeId,
                    ItemNoIndex = s.ItemNoIndex,
                    ItemNo = s.ItemNo,
                    Code = s.Code,
                    Description = s.Description.PadLeft(s.ItemNo.Count(c => c == '.') * 20, ' '),
                    ItemSw = s.ItemSw,
                    AccountCode = s.AccountCode,
                    InsertedDt = s.InsertedDt
                }).AsQueryable();
            return data;
        }            

        public async Task<ItemCode> GetByIdAsync(Guid id)
        {
            var data = await db.ItemCodes.FindAsync(id);                
            return data;
        }        

        public async Task<ItemCodeVM> CreateAsync(ItemCodeVM model, string user, DateTime date)
        {
            //if (!string.IsNullOrWhiteSpace(model.ItemSw) && !(model.ItemSw == "Y" && model.ItemSw == "N"))
            //{
            //    throw new InvalidValueException("Valid value for Is Item is Y or N.");
            //}
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            model.Id = Guid.NewGuid();
            model.Code = GetItemCode(model.ItemTypeId, model.ItemNo, model.Description);
            model.ItemNoIndex = ItemNoIndex(model.ItemNo);

            ItemCode entity = new ItemCode()
            {
                Id = model.Id,
                ItemTypeId = model.ItemTypeId,
                ItemNo = model.ItemNo.Trim(),
                ItemNoIndex = model.ItemNoIndex,
                Code = model.Code,
                Description = string.IsNullOrWhiteSpace(model.Description) ? "" : model.Description.Trim(),
                ItemSw = string.IsNullOrWhiteSpace(model.ItemSw) ? "" : model.ItemSw.ToUpper(),
                AccountCode = string.IsNullOrEmpty(model.AccountCode) ? "" : model.AccountCode.ToUpper(),
                InsertedBy = user,
                InsertedDt = date,
                UpdatedBy = user,
                UpdatedDt = date
            };

            db.ItemCodes.Add(entity);
            await db.SaveChangesAsync();

            return model;
        }
        
        public async Task<ItemCodeVM> UpdateAsync(ItemCodeVM model, string user, DateTime date)
        {
            //if (!string.IsNullOrWhiteSpace(model.ItemSw) && !(model.ItemSw == "Y" && model.ItemSw == "N"))
            //{
            //    throw new InvalidValueException("Valid value for Is Item is Y or N.");
            //}

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            ItemCode entity = await db.ItemCodes.FindAsync(model.Id);

            model.Code = GetItemCode(model.ItemTypeId, model.ItemNo, model.Description);
            model.ItemNoIndex = ItemNoIndex(model.ItemNo);

            entity.ItemTypeId = model.ItemTypeId;
            entity.ItemNo = model.ItemNo.Trim();
            entity.ItemNoIndex = model.ItemNoIndex;
            entity.Code = model.Code;
            entity.Description = string.IsNullOrWhiteSpace(model.Description) ? "" : model.Description.Trim();
            entity.ItemSw = string.IsNullOrWhiteSpace(model.ItemSw) ? "" : model.ItemSw.ToUpper();
            entity.AccountCode = string.IsNullOrEmpty(model.AccountCode) ? "" : model.AccountCode.ToUpper();
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.ItemCodes.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<ItemCodeVM> DeleteAsync(ItemCodeVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            ItemCode entity = await db.ItemCodes.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.ItemCodes.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.ItemCodes.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            return model;
        }


        private string GetItemCode(Guid? itemTypeId, string itemNo, string description)
        {
            var itemType = db.ItemTypes.Find(itemTypeId);
            string exemptionPattern = @"[-.]+|\[.*?\]|\(.*?\)";
            string itemCode = Regex.Replace(itemNo, exemptionPattern, "");
            if (string.IsNullOrWhiteSpace(description))
            {
                return itemType.Code + "*" + itemCode;
            }
            return itemType.Code + itemCode;
        }

        private string ItemNoIndex(string itemNo)
        {
            var raItemNo = itemNo.Trim().Split('.');
            string itemNoIndex = "";
            for(var x = 0; x < raItemNo.Length; x++)
            {
                itemNoIndex += (x > 0 ? "-" : "") + raItemNo[x].PadLeft(3, '0');
            }
            return itemNoIndex;
        }
    }
}