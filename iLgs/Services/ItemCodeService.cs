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
        private readonly IExceptionService<ItemCodeVM> _VmExceptionService = new ExceptionService<ItemCodeVM>();
        private readonly IExceptionService<ItemCode> _ExceptionService = new ExceptionService<ItemCode>();

        public ItemCodeService(AppManEntities db)
        {
            this.db = db;
        }

        public IQueryable<ItemCodeVM> GetAll()
        {
            var data = db.ItemCodes.AsNoTracking()
                .Select(s => new ItemCodeVM
                {
                    Id = s.Id,                    
                    ItemTypeId = s.ItemTypeId,
                    ItemNo = s.ItemNo,
                    ItemNoIndex = s.ItemNoIndex,
                    Code = s.Code,
                    Description = s.Description,     
                    ItemSw = s.ItemSw,
                    IsConsumable = s.IsConsumable,
                    IsIncorporated = s.IsIncorporated,
                    ForDistribution = s.ForDistribution,
                    //ItemSwUI = s.ItemSw == "Y" ? true : false,
                    AccountCode = s.AccountCode,
                    InsertedDt = s.InsertedDt
                });
            return data;
        }

        public IQueryable<ItemCodeVM> GetAllByItemTypeId(Guid? itemTypeId)
        {            
            var data = db.ItemCodes.Where(w => w.ItemTypeId == itemTypeId).AsNoTracking().ToList()
                .Select(s => new ItemCodeVM
                {
                    Id = s.Id,
                    ItemTypeId = s.ItemTypeId,
                    ItemNoIndex = s.ItemNoIndex,
                    ItemNo = s.ItemNo,
                    Code = s.Code,
                    Description = s.Description.PadLeft(s.ItemNo.Count(c => c == '.') * 20, ' '),
                    ItemSw = s.ItemSw,
                    IsConsumable = s.IsConsumable,
                    IsIncorporated = s.IsIncorporated,
                    ForDistribution = s.ForDistribution,
                    //ItemSwUI = s.ItemSw == "Y" ? true : false,
                    AccountCode = s.AccountCode,
                    InsertedDt = s.InsertedDt
                }).AsQueryable();
            return data;
        }

        public ValueTask<ItemCode> GetByIdAsync(Guid id) => _ExceptionService.TryCatch(async () =>
        {
            var data = await db.ItemCodes.FindAsync(id);
            return data;
        });

        public IQueryable<ItemCodeVM> GetItems(string item) => _VmExceptionService.TryCatch(() =>
        {
            var data = db.Database.SqlQuery<ItemCodeVM>("Exec ItemCodes_GetItems {0}", item).AsQueryable();
            return data;
        });

        public IQueryable<ItemCodeVM> GetItemsByCategory(string category, string item) => _VmExceptionService.TryCatch(() =>
        {
            var data = db.Database.SqlQuery<ItemCodeVM>("Exec ItemCodes_GetItemsByCategory {0}, {1}", category, item).AsQueryable();
            return data;
        });

        public IQueryable<ItemCodeVM> GetItemsByTypeCode(string typeCode, string item) => _VmExceptionService.TryCatch(() =>
        {
            var data = db.Database.SqlQuery<ItemCodeVM>("Exec ItemCodes_GetItemsByTypeCode {0}, {1}", typeCode, item).AsQueryable();
            return data;
        });

        private void ValidateFields(ItemCodeVM model)
        {
            if (!string.IsNullOrWhiteSpace(model.ItemSw) && !(model.ItemSw == "Y" || model.ItemSw == "N" || model.ItemSw == ""))
            {
                throw new InvalidValueException("Invalid Article value!");
            }

            if (!string.IsNullOrWhiteSpace(model.IsConsumable) && !(model.IsConsumable == "Y" || model.IsConsumable == "N" || model.IsConsumable == ""))
            {
                throw new InvalidValueException("Invalid Consumable value!");
            }

            if (!string.IsNullOrWhiteSpace(model.IsIncorporated) && !(model.IsIncorporated == "Y" || model.IsIncorporated == "N" || model.IsIncorporated == ""))
            {
                throw new InvalidValueException("Invalid Incorporated value!");
            }

            if (!string.IsNullOrWhiteSpace(model.ForDistribution) && !(model.ForDistribution == "Y" || model.ForDistribution == "N" || model.ForDistribution == "O" || model.ForDistribution == ""))
            {
                throw new InvalidValueException("Invalid For Distribution value!");
            }
        }

        public ValueTask<ItemCodeVM> CreateAsync(ItemCodeVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            ValidateFields(model);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            model.Id = Guid.NewGuid();
            model.Code = GetItemCode(model.ItemTypeId, model.ItemNo, model.Description);
            model.ItemNoIndex = ItemNoIndex(model.ItemNo);
            //model.ItemSw = model.ItemSwUI == true? "Y" : "";

            ItemCode entity = new ItemCode()
            {
                Id = model.Id,
                ItemTypeId = model.ItemTypeId,
                ItemNo = model.ItemNo.Trim(),
                ItemNoIndex = model.ItemNoIndex,
                Code = model.Code,
                Description = string.IsNullOrWhiteSpace(model.Description) ? "" : model.Description.Trim(),
                ItemSw = string.IsNullOrWhiteSpace(model.ItemSw) ? "" : model.ItemSw.ToUpper(),
                IsConsumable = string.IsNullOrWhiteSpace(model.IsConsumable) ? "" : model.IsConsumable.ToUpper(),
                IsIncorporated = string.IsNullOrWhiteSpace(model.IsIncorporated) ? "" : model.IsIncorporated.ToUpper(),
                ForDistribution = string.IsNullOrWhiteSpace(model.ForDistribution) ? "" : model.ForDistribution.ToUpper(),
                AccountCode = string.IsNullOrEmpty(model.AccountCode) ? "" : model.AccountCode.ToUpper(),
                InsertedBy = user,
                InsertedDt = date,
                UpdatedBy = user,
                UpdatedDt = date
            };

            db.ItemCodes.Add(entity);
            await db.SaveChangesAsync();

            return model;
          
        });

        public ValueTask<ItemCodeVM> UpdateAsync(ItemCodeVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>

        {
            ValidateFields(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            ItemCode entity = await db.ItemCodes.FindAsync(model.Id);

            model.Code = GetItemCode(model.ItemTypeId, model.ItemNo, model.Description);
            model.ItemNoIndex = ItemNoIndex(model.ItemNo);
            //model.ItemSw = model.ItemSwUI == true ? "Y" : "";

            entity.ItemTypeId = model.ItemTypeId;
            entity.ItemNo = model.ItemNo.Trim();
            entity.ItemNoIndex = model.ItemNoIndex;
            entity.Code = model.Code;
            entity.Description = string.IsNullOrWhiteSpace(model.Description) ? "" : model.Description.Trim();
            entity.ItemSw = string.IsNullOrWhiteSpace(model.ItemSw) ? "" : model.ItemSw.ToUpper().Trim();
            entity.IsConsumable = string.IsNullOrWhiteSpace(model.IsConsumable) ? "" : model.IsConsumable.ToUpper();
            entity.IsIncorporated = string.IsNullOrWhiteSpace(model.IsIncorporated) ? "" : model.IsIncorporated.ToUpper();
            entity.ForDistribution = string.IsNullOrWhiteSpace(model.ForDistribution) ? "" : model.ForDistribution.ToUpper();
            entity.AccountCode = string.IsNullOrEmpty(model.AccountCode) ? "" : model.AccountCode.ToUpper().Trim();
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.ItemCodes.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return model;
        });

        public ValueTask<ItemCodeVM> DeleteAsync(ItemCodeVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
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
        });


        private string GetItemCode(Guid? itemTypeId, string itemNo, string description)
        {
            var itemType = db.ItemTypes.Find(itemTypeId);
            //string exemptionPattern = @"[-.]+|\[.*?\]|\(.*?\)";
            //string itemCode = Regex.Replace(itemNo, exemptionPattern, "");
            //if (string.IsNullOrWhiteSpace(description))
            //{
            //    return itemType.Code + "*" + itemCode;
            //}
            //return itemType.Code + itemType.GroupCode + itemCode;
            return itemType.Code + itemType.GroupCode + "-" + itemNo;
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