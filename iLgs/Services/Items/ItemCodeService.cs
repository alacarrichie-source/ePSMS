using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services.Items
{
    public interface IItemCodeService
    {
        IQueryable<ItemCodeVM> GetAll();
        IQueryable<ItemCodeVM> GetAllByItemTypeId(Guid? itemTypeId);
        ValueTask<ItemCode> GetByIdAsync(Guid? id);
        ItemCode GetById(Guid? id);
        IQueryable<ItemCodeVM> GetItems(string item);
        IQueryable<ItemCodeVM> GetItemAccounts(string item);
        IQueryable<ItemCodeVM> GetItemAccountsByCategory(string category, string item);
        string GetSubAccounts(Guid? id);
        string GetSubAccount(Guid? id, int pos);
        string GetSubAccountCode(Guid? id);
        IQueryable<ItemCodeVM> GetItemsByCategory(string category, string item);
        IQueryable<ItemCodeVM> GetItemsByTypeCode(string typeCode, string item);
        IQueryable<ItemCodePreviewVM> GetItemCodePreview(string category);
        IQueryable<ItemCodePreviewVM> GetItemCodePreviewByUser(string category, string userId);
        IQueryable<ItemCodeVM> GetCustodianItemPpe(string item);
        IQueryable<ItemCodeVM> GetCustodianItemStocks(string item);
        IQueryable<ItemCodeVM> GetCustodianItemVehicle(string item);
        IQueryable<ItemCodeVM> GetCustodianItemLand(string item);
        IQueryable<ItemCodeVM> GetCustodianItemBldg(string item);

        bool IsProperty(Guid? id);
        bool IsWithParIcs(Guid? id);
        string GetInvDist(Guid? id);

        ValueTask<ItemCodeVM> CreateAsync(ItemCodeVM model, string user, DateTime date);
        ValueTask<ItemCodeVM> UpdateAsync(ItemCodeVM model, string user, DateTime date);
        ValueTask<ItemCodeVM> DeleteAsync(ItemCodeVM model, string user, DateTime date);

        void ValidateRelationship(Guid itemcodeId);
    }

    public class ItemCodeService : IItemCodeService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<ItemCodeVM> _VmExceptionService = new ExceptionService<ItemCodeVM>();
        private readonly IExceptionService<ItemCode> _ExceptionService = new ExceptionService<ItemCode>();        

        public ItemCodeService(AppManEntities db)
        {
            _db = db;            
        }

        public IQueryable<ItemCodeVM> GetAll()
        {
            var data = _db.ItemCodes.AsNoTracking().ToList()
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
                    PartialPage = s.PartialPage,
                    RequiredFields = _db.Codextns.Where(w => w.Desc2 == s.PartialPage && w.CodeMast.Code == "REQUIRED-FIELDS").FirstOrDefault()?.Description,
                    //ItemSwUI = s.ItemSw == "Y" ? true : false,
                    AccountCode = s.AccountCode,
                    InsertedDt = s.InsertedDt
                }).AsQueryable();
            return data;
        }

        public IQueryable<ItemCodeVM> GetAllByItemTypeId(Guid? itemTypeId)
        {
            var data = _db.ItemCodes.Where(w => w.ItemTypeId == itemTypeId).AsNoTracking().ToList()
                .Select(s => new ItemCodeVM
                {
                    Id = s.Id,
                    ItemTypeId = s.ItemTypeId,
                    ItemNoIndex = s.ItemNoIndex,
                    ItemNo = s.ItemNo,
                    Code = s.Code,
                    Description = s.Description,
                    ItemSw = s.ItemSw,
                    IsConsumable = s.IsConsumable,
                    IsIncorporated = s.IsIncorporated,
                    ForDistribution = s.ForDistribution,
                    PartialPage = s.PartialPage,
                    RequiredFields = _db.Codextns.Where(w => w.Desc2 == s.PartialPage && w.CodeMast.Code == "REQUIRED-FIELDS").FirstOrDefault()?.Description,
                    //ItemSwUI = s.ItemSw == "Y" ? true : false,
                    Padding = s.ItemNo.Count(c => c == '.') * 30,
                    AccountCode = s.AccountCode,
                    InsertedDt = s.InsertedDt
                }).AsQueryable();
            return data;
        }

        public ValueTask<ItemCode> GetByIdAsync(Guid? id) => _ExceptionService.TryCatch(async () =>
        {
            var data = await _db.ItemCodes.Include(i => i.ItemType).FirstOrDefaultAsync(f => f.Id == id);
            return data;
        });

        public ItemCode GetById(Guid? id) 
        {
            var data = _db.ItemCodes.Include(i => i.ItemType).FirstOrDefault(f => f.Id == id);
            return data;
        }

        public IQueryable<ItemCodeVM> GetItems(string item) => _VmExceptionService.TryCatch(() =>
        {
            var data = _db.Database.SqlQuery<ItemCodeVM>("Exec ItemCodes_GetItems {0}", item).AsQueryable().AsNoTracking();
            return data;
        });

        public IQueryable<ItemCodeVM> GetCustodianItemPpe(string item) => _VmExceptionService.TryCatch(() =>
        {
            var data = _db.Database.SqlQuery<ItemCodeVM>("Exec ItemCodes_GetCustodianAccount {0}, {1}", (int)CustodianAccountGroup.PPE, item).AsQueryable().AsNoTracking();
            return data;
        });

        //public IQueryable<ItemCodeVM> GetCustodianItemStocks(string item) => _VmExceptionService.TryCatch(() =>
        //{
        //    var data = db.Database.SqlQuery<ItemCodeVM>("Exec ItemCodes_GetCustodianAccount {0}, {1}", (int)CustodianAccountGroup.STOCK, item).AsQueryable().AsNoTracking();
        //    return data;
        //});

        public IQueryable<ItemCodeVM> GetCustodianItemStocks(string item) 
        {
            var data = _db.Database.SqlQuery<ItemCodeVM>("Exec ItemCodes_GetCustodianAccount {0}, {1}", (int)CustodianAccountGroup.STOCK, item).AsQueryable().AsNoTracking();
            return data;
        }

        public IQueryable<ItemCodeVM> GetCustodianItemVehicle(string item) => _VmExceptionService.TryCatch(() =>
        {
            var data = _db.Database.SqlQuery<ItemCodeVM>("Exec ItemCodes_GetCustodianAccount {0}, {1}", (int)CustodianAccountGroup.VEHICLE, item).AsQueryable().AsNoTracking();
            return data;
        });

        public IQueryable<ItemCodeVM> GetCustodianItemLand(string item) => _VmExceptionService.TryCatch(() =>
        {
            var data = _db.Database.SqlQuery<ItemCodeVM>("Exec ItemCodes_GetCustodianAccount {0}, {1}", (int)CustodianAccountGroup.LAND, item).AsQueryable().AsNoTracking();
            return data;
        });

        public IQueryable<ItemCodeVM> GetCustodianItemBldg(string item) => _VmExceptionService.TryCatch(() =>
        {
            var data = _db.Database.SqlQuery<ItemCodeVM>("Exec ItemCodes_GetCustodianAccount {0}, {1}", (int)CustodianAccountGroup.BUILDING, item).AsQueryable().AsNoTracking();
            return data;
        });

        public IQueryable<ItemCodeVM> GetItemAccounts(string item) => _VmExceptionService.TryCatch(() =>
        {
            var data = _db.Database.SqlQuery<ItemCodeVM>("Exec ItemCodes_GetAccounts '', {0}", item).AsQueryable().AsNoTracking();
            return data;
        });        

        public IQueryable<ItemCodeVM> GetItemAccountsByCategory(string category, string item) => _VmExceptionService.TryCatch(() =>
        {
            var data = _db.Database.SqlQuery<ItemCodeVM>("Exec ItemCodes_GetAccounts {0}, {1}", category, item).AsQueryable().AsNoTracking();
            return data;
        });

        public string GetSubAccountCode(Guid? id)
        {
            var itemCode = _db.ItemCodes.FirstOrDefault(f => f.Id == id);
            if (itemCode == null)
            {
                return string.Empty;
            }
            var raCode = itemCode.Code.Split('.');            
            return raCode[raCode.Length -1];
        }

        public string GetSubAccounts(Guid? id) 
        {
            // Retrieve the Code for the given id
            var code = _db.ItemCodes
                .Where(i => i.Id == id)
                .Select(i => i.Code)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(code))
                return string.Empty; // Handle the case where the code is not found

            //// Use the retrieved Code in the query
            //var data = string.Join("/", _db.ItemCodes
            //    .Where(w => w.Id != id && w.Code.StartsWith(code))
            //    .Select(s => s.Description));

            var data = _db.Database.SqlQuery<string>($"select STRING_AGG(Description, '/') from ItemCodes where Id != '{id}' and '{code}' like code + '%'").FirstOrDefault();

            return data;
        }

        public string GetSubAccount(Guid? id, int pos)
        {
            // Retrieve the Code for the given id
            var code = _db.ItemCodes
                .Where(i => i.Id == id)
                .Select(i => i.Code)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(code))
                return string.Empty; // Handle the case where the code is not found

            var data = _db.Database.SqlQuery<string>($"Select dbo.fn_SubAccountAt('{code}', {pos})").FirstOrDefault();

            return data;
        }

        public IQueryable<ItemCodeVM> GetItemsByCategory(string category, string item) => _VmExceptionService.TryCatch(() =>
        {
            var data = _db.Database.SqlQuery<ItemCodeVM>("Exec ItemCodes_GetItemsByCategory {0}, {1}", category, item).AsQueryable().AsNoTracking();
            return data;
        });

        public IQueryable<ItemCodeVM> GetItemsByTypeCode(string typeCode, string item) => _VmExceptionService.TryCatch(() =>
        {
            var data = _db.Database.SqlQuery<ItemCodeVM>("Exec ItemCodes_GetItemsByTypeCode {0}, {1}", typeCode, item).AsQueryable().AsNoTracking();
            return data;
        });

        public IQueryable<ItemCodePreviewVM> GetItemCodePreview(string category)
        {
            if (category == "ALL")
            {
                category = "";
            }

            var data = _db.Database.SqlQuery<ItemCodePreviewVM>("Exec ItemCodes_GetPreview {0}", category).AsQueryable().AsNoTracking();
            return data;
        }

        public IQueryable<ItemCodePreviewVM> GetItemCodePreviewByUser(string category, string userId)
        {
            if (category == "ALL")
            {
                category = "";
            }

            var data = _db.Database.SqlQuery<ItemCodePreviewVM>("Exec ItemCodes_GetPreview {0}, {1}", category, userId).AsQueryable().AsNoTracking();
            return data;
        }

        public bool IsProperty(Guid? id)
        {
            var itemCode = _db.ItemCodes.Include(i => i.ItemType).Where(w => w.Id == id).AsNoTracking().FirstOrDefault();
            if (itemCode != null)
            {
                return itemCode.ItemType.Category != "S";
            }
            return false;
        }

        public bool IsWithParIcs(Guid? id)
        {
            var itemCode = _db.ItemCodes.Include(i => i.ItemType).Where(w => w.Id == id).AsNoTracking().FirstOrDefault();
            if (itemCode != null)
            {
                if (itemCode.IsConsumable == "Y" || itemCode.IsIncorporated == "Y" || itemCode.ForDistribution == "Y")
                {
                    return false;
                }
            }
            return true;
        }

        public string GetInvDist(Guid? id)
        {
            var itemCode = _db.ItemCodes.Include(i => i.ItemType).Where(w => w.Id == id).AsNoTracking().FirstOrDefault();
            if (itemCode != null)
            {
                //if (itemCode.ItemType.Category != "S")
                //{
                //    return "I";
                //}
                //if (itemCode.ForDistribution == "Y" || itemCode.IsConsumable == "Y" || itemCode.IsIncorporated == "Y")
                //{
                //    return "D";
                //}                
                if (itemCode.ForDistribution == "Y")
                {
                    return "D";
                }
                if (itemCode.ForDistribution == "N")
                {
                    return "I";
                }
            }

            return "";
        }

        private void ValidateFields(ItemCodeVM model)
        {
            if (!string.IsNullOrWhiteSpace(model.ItemSw))
            {
                var f = model.ItemSw.ToUpper().Trim();
                if (f != "Y" && f != "N" && f != "")
                {
                    throw new InvalidValueException("Invalid Article value!");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.IsConsumable))
            {
                var f = model.IsConsumable.ToUpper().Trim();
                if (f != "Y" && f != "N" && f != "")
                {
                    throw new InvalidValueException("Invalid Consumable value!");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.IsIncorporated)) 
            {
                var f = model.IsIncorporated.ToUpper().Trim();
                if (f != "Y" && f != "N" && f != "")
                {
                    throw new InvalidValueException("Invalid Incorporated value!");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.ForDistribution))
            {
                var f = model.ForDistribution.ToUpper().Trim();
                if (f != "Y" && f != "N" && f != "O" && f != "")
                {
                    throw new InvalidValueException("Invalid For Distribution value!");
                }
            }
        }

        public ValueTask<ItemCodeVM> CreateAsync(ItemCodeVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
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
                ItemSw = string.IsNullOrWhiteSpace(model.ItemSw) ? "" : model.ItemSw.Trim().ToUpper(),
                IsConsumable = string.IsNullOrWhiteSpace(model.IsConsumable) ? "" : model.IsConsumable.Trim().ToUpper(),
                IsIncorporated = string.IsNullOrWhiteSpace(model.IsIncorporated) ? "" : model.IsIncorporated.Trim().ToUpper(),
                ForDistribution = string.IsNullOrWhiteSpace(model.ForDistribution) ? "" : model.ForDistribution.Trim().ToUpper(),
                AccountCode = string.IsNullOrEmpty(model.AccountCode) ? "" : model.AccountCode.Trim().ToUpper(),
                PartialPage = model.PartialPage,
                InsertedBy = user,
                InsertedDt = date,
                UpdatedBy = user,
                UpdatedDt = date
            };

            _db.ItemCodes.Add(entity);
            await _db.SaveChangesAsync();

            return model;

        });

        public ValueTask<ItemCodeVM> UpdateAsync(ItemCodeVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateFields(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            ItemCode entity = await _db.ItemCodes.FindAsync(model.Id);
            ValidateRecord(entity, model.Id);
            //ValidateRelationship(model.Id);

            model.Code = GetItemCode(model.ItemTypeId, model.ItemNo, model.Description);
            model.ItemNoIndex = ItemNoIndex(model.ItemNo);
            //model.ItemSw = model.ItemSwUI == true ? "Y" : "";

            entity.ItemTypeId = model.ItemTypeId;
            entity.ItemNo = model.ItemNo.Trim();
            entity.ItemNoIndex = model.ItemNoIndex;
            entity.Code = model.Code;
            entity.Description = string.IsNullOrWhiteSpace(model.Description) ? "" : model.Description.Trim();
            entity.ItemSw = string.IsNullOrWhiteSpace(model.ItemSw) ? "" : model.ItemSw.ToUpper().Trim().Trim();
            entity.IsConsumable = string.IsNullOrWhiteSpace(model.IsConsumable) ? "" : model.IsConsumable.Trim().ToUpper();
            entity.IsIncorporated = string.IsNullOrWhiteSpace(model.IsIncorporated) ? "" : model.IsIncorporated.Trim().ToUpper();
            entity.ForDistribution = string.IsNullOrWhiteSpace(model.ForDistribution) ? "" : model.ForDistribution.Trim().ToUpper();
            entity.AccountCode = string.IsNullOrEmpty(model.AccountCode) ? "" : model.AccountCode.ToUpper().Trim();
            entity.PartialPage = model.PartialPage;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.ItemCodes.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<ItemCodeVM> DeleteAsync(ItemCodeVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            ItemCode entity = await _db.ItemCodes.FindAsync(model.Id);
            ValidateRecord(entity, model.Id);
            ValidateRelationship(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.ItemCodes.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.ItemCodes.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });


        private string GetItemCode(Guid? itemTypeId, string itemNo, string description)
        {
            var itemType = _db.ItemTypes.Find(itemTypeId);
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
            for (var x = 0; x < raItemNo.Length; x++)
            {
                itemNoIndex += (x > 0 ? "-" : "") + raItemNo[x].PadLeft(3, '0');
            }
            return itemNoIndex;
        }

        public void ValidateRelationship(Guid itemcodeId)
        {
            var psCards = _db.PsCards.Where(w => w.ItemCodeId == itemcodeId);
            if (psCards.Any())
            {
                //var cardNos = string.Join("/", psCards.Select(s => s.PsNo));
                throw new RecordRelationshipException($"Item Code is in use in Stock/Property Card, cannot proceed!");
            }

            var cust1 = _db.CustodianReportItems.Where(w => w.ItemCodeId == itemcodeId);
            if (cust1.Any())
            {
                throw new RecordRelationshipException("Item Code is in use in custodian report, cannot proceed!");
            }

            var cust2 = _db.CustodianReportBldgItems.Where(w => w.ItemCodeId == itemcodeId);
            if (cust2.Any())
            {
                throw new RecordRelationshipException("Item Code is in use in custodian structures, cannot proceed!");
            }

            var cust3 = _db.CustodianReportLandItems.Where(w => w.ItemCodeId == itemcodeId);
            if (cust3.Any())
            {
                throw new RecordRelationshipException("Item Code is in use in custodian land, cannot proceed!");
            }

            var rpciItems = _db.RPCIItems.Where(w => w.ItemCodeId == itemcodeId);
            if (rpciItems.Any())
            {
                throw new RecordRelationshipException("Item Code is in use in RPCI, cannot proceed!");
            }

            var risItems = _db.RisItems.Where(w => w.ItemCodeId == itemcodeId);
            if (risItems.Any())
            {
                throw new RecordRelationshipException("Item Code is in use in RIS, cannot proceed!");
            }

            var orderItems = _db.OrderItems.Where(w => w.ItemCodeId == itemcodeId);
            if (orderItems.Any())
            {
                throw new RecordRelationshipException("Item Code is in use in PURHASE ORDERS, cannot proceed!");
            }
        }

        private void ValidateRecord(ItemCode entity, Guid id)
        {
            if (entity is null)
            {
                throw new NotFoundException(id);
            }            
        }

        private static void ValidateIfNull(ItemCodeVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }
    }
}