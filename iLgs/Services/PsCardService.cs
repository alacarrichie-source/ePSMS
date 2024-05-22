using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services
{
    public interface IPsCardService
    {
        IQueryable<PsCardVM> GetAll();
        IQueryable<PsCardVM> GetAllByItemCodeId(Guid? itemCodeId);
        ValueTask<PsCardVM> GetVmByIdAsync(Guid? id);
        ValueTask<PsCard> GetByIdAsync(Guid id);
        ValueTask<PsCard> GetByPsNoAsync(string psNo);
        ValueTask<bool> GetAnyPsNoAsync(Guid id, string psNo);
        ValueTask<PsCardVM> CreateAsync(PsCardVM model, string user, DateTime date);
        ValueTask<PsCardVM> UpdateAsync(PsCardVM model, string user, DateTime date);
        ValueTask<PsCardVM> DeleteAsync(PsCardVM model, string user, DateTime date);
        string GetDescription(PsCardVM fields);
        string GetStockNo(PsCardVM model);
    }

    public class PsCardService : IPsCardService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<PsCardVM> _vmExceptionService = new ExceptionService<PsCardVM>();
        private readonly IExceptionService<PsCard> _exceptionService = new ExceptionService<PsCard>();

        private readonly string _cardCategory = "";

        public PsCardService(AppManEntities db)
        {
            _db = db;            
        }

        public PsCardService(AppManEntities db, string cardCategory)
        {            
            _cardCategory = cardCategory;
            _db = db;
        }


        public IQueryable<PsCardVM> GetAll() => _vmExceptionService.TryCatch(() =>
        {
            var data = _db.PsCards.AsNoTracking()
                .Select(s => new PsCardVM
                {
                    Id = s.Id,
                    ItemCodeId = s.ItemCodeId,
                    Item = s.ItemCode.Description,
                    ItemCode = s.ItemCode.Code,
                    ItemType = s.ItemCode.ItemType.Description,
                    ItemTypeCode = s.ItemCode.ItemType.Code,
                    //ItemTypeCategory = s.ItemCode.ItemType.Category,
                    CardCategory = s.CardCategory,
                    Description = s.Description,
                    SubAccountCode = s.SubAccountCode,
                    SubAccount = _db.ItemCodes.Where(w => w.ItemTypeId == s.ItemCode.ItemTypeId && w.Code == s.SubAccountCode).Select(x => x.Description).FirstOrDefault(),
                    Fund = s.Fund,
                    Unit = s.Unit,                    
                    PsNo = s.PsNo,
                    PsName = s.PsName,
                    PrevPsNo = s.PrevPsNo,
                    Amount = s.Amount,
                    FromDonation = s.FromDonation,
                    FieldsMedicine = s.FieldsMedicine,
                    FieldsOther = s.FieldsOther,
                    FieldsPpe = s.FieldsPpe,
                    FieldsVehicle = s.FieldsVehicle,
                    InsertedDt = s.InsertedDt                    
                });
            
            return GetAllByCategory(data);
        });

        private IQueryable<PsCardVM> GetAllByCategory(IQueryable<PsCardVM> data)
        {
            if (!string.IsNullOrEmpty(_cardCategory))
            {
                data = data.Where(w => w.CardCategory == _cardCategory);
            }
            return data;
        }

        
        public IQueryable<PsCardVM> GetAllByItemCodeId(Guid? itemCodeId) => _vmExceptionService.TryCatch(() =>
        {
            var data = _db.PsCards.Where(w => w.ItemCodeId == itemCodeId).AsNoTracking()
                .Select(s => new PsCardVM
                {
                    Id = s.Id,
                    ItemCodeId = s.ItemCodeId,
                    Item = s.ItemCode.Description,
                    ItemCode = s.ItemCode.Code,
                    ItemType = s.ItemCode.ItemType.Description,
                    ItemTypeCode = s.ItemCode.ItemType.Code,
                    //ItemTypeCategory = s.ItemCode.ItemType.Category,
                    CardCategory = s.CardCategory,
                    Description = s.Description,
                    SubAccountCode = s.SubAccountCode,
                    SubAccount = _db.ItemCodes.Where(w => w.ItemTypeId == s.ItemCode.ItemTypeId && w.Code == s.SubAccountCode).Select(x => x.Description).FirstOrDefault(),
                    Fund = s.Fund,
                    Unit = s.Unit,
                    PsNo = s.PsNo,
                    PsName = s.PsName,
                    PrevPsNo = s.PrevPsNo,
                    FromDonation = s.FromDonation,
                    Amount = s.Amount,
                    FieldsMedicine = s.FieldsMedicine,
                    FieldsOther = s.FieldsOther,
                    FieldsPpe = s.FieldsPpe,
                    FieldsVehicle = s.FieldsVehicle,
                    InsertedDt = s.InsertedDt                    
                });
            return GetAllByCategory(data);
        });

        public ValueTask<PsCardVM> GetVmByIdAsync(Guid? id) => _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.PsCards.Where(w => w.Id == id).AsNoTracking()
                .Select(s => new PsCardVM
                {
                    Id = s.Id,
                    ItemCodeId = s.ItemCodeId,
                    Item = s.ItemCode.Description,
                    ItemCode = s.ItemCode.Code,
                    ItemType = s.ItemCode.ItemType.Description,
                    ItemTypeCode = s.ItemCode.ItemType.Code,
                    //ItemTypeCategory = s.ItemCode.ItemType.Category,
                    CardCategory = s.CardCategory,
                    Description = s.Description,
                    SubAccountCode = s.SubAccountCode,
                    SubAccount = _db.ItemCodes.Where(w => w.ItemTypeId == s.ItemCode.ItemTypeId && w.Code == s.SubAccountCode).Select(x => x.Description).FirstOrDefault(),
                    Fund = s.Fund,
                    Unit = s.Unit,                    
                    PsNo = s.PsNo,
                    PsName = s.PsName,
                    PrevPsNo = s.PrevPsNo,
                    FromDonation = s.FromDonation,
                    Amount = s.Amount,
                    FieldsMedicine = s.FieldsMedicine,
                    FieldsOther = s.FieldsOther,
                    FieldsPpe = s.FieldsPpe,
                    FieldsVehicle = s.FieldsVehicle,
                    InsertedDt = s.InsertedDt
                }).FirstOrDefaultAsync();
            return data;
        });

        public ValueTask<PsCard> GetByIdAsync(Guid id) => _exceptionService.TryCatch(async () =>
        {
            return await _db.PsCards.AsNoTracking()
                .Include(i => i.FieldsMedicine)
                .Include(i => i.FieldsOther)
                .Include(i => i.FieldsVehicle)
                .Include(i => i.FieldsPpe)
                .FirstOrDefaultAsync(f => f.Id == id);            
        });

        public async ValueTask<bool> GetAnyPsNoAsync(Guid id, string psNo)
        {
            return await _db.PsCards.AnyAsync(a => a.Id != id && a.PsNo == psNo);
        }

        public ValueTask<PsCard> GetByPsNoAsync(string psNo) => _exceptionService.TryCatch(async () =>
        {
            return await _db.PsCards.Where(w => w.PsNo == psNo).FirstOrDefaultAsync();
        });

        public ValueTask<PsCardVM> CreateAsync(PsCardVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            if (model.ItemCodeId == null)
            {
                throw new InvalidValueException("PPE is Required!");
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                throw new InvalidValueException("Description is Required!");
            }

            if (string.IsNullOrWhiteSpace(model.Fund))
            {
                throw new InvalidValueException("Fund is Required!");
            }

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = new PsCard
            {
                Id = model.Id,
                ItemCodeId = model.ItemCodeId,
                SubAccountCode = model.SubAccountCode,
                Fund = model.Fund,
                Description = model.Description,
                Unit = model.Unit,
                CardCategory = model.CardCategory,
                PsNo = model.PsNo,
                PsName = model.PsName,
                PrevPsNo = model.PrevPsNo,
                FromDonation = model.FromDonation,
                Amount = model.Amount,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            entity = SetItemEntity(entity, model);

            _db.PsCards.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<PsCardVM> UpdateAsync(PsCardVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            var entity = await GetByIdAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (model.ItemCodeId == null)
            {
                throw new InvalidValueException("Item is Required!");
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                throw new InvalidValueException("Description is Required!");
            }

            if (string.IsNullOrWhiteSpace(model.Fund))
            {
                throw new InvalidValueException("Fund is Required!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.ItemCodeId = model.ItemCodeId;
            entity.SubAccountCode = model.SubAccountCode;
            entity.Fund = model.Fund;
            entity.Description = model.Description;
            entity.Unit = model.Unit;
            entity.CardCategory = model.CardCategory;
            entity.PsNo = model.PsNo;
            entity.PsName = model.PsName;
            entity.PrevPsNo = model.PrevPsNo;
            entity.FromDonation = model.FromDonation;
            entity.Amount = model.Amount;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            entity = SetItemEntity(entity, model);

            _db.PsCards.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<PsCardVM> DeleteAsync(PsCardVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await GetByIdAsync(model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PsCards.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCards.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        private PsCard SetItemEntity(PsCard entity, PsCardVM model)
        {
            entity.FieldsMedicine = null;
            entity.FieldsOther = null;
            entity.FieldsPpe = null;
            entity.FieldsVehicle = null;

            if (Enum.TryParse(model.ItemTypeCode, out Category category))
            {
                if (category == Category.T)
                {
                    model.FieldsVehicle.Id = entity.Id;
                    entity.FieldsVehicle = model.FieldsVehicle;
                }
                else if (category == Category.D)
                {
                    model.FieldsMedicine.Id = entity.Id;
                    entity.FieldsMedicine = model.FieldsMedicine;
                }
                else if (category == Category.U)
                {
                    model.FieldsPpe.Id = entity.Id;
                    entity.FieldsPpe = model.FieldsPpe;
                }
            }
            
            return entity;
        }


        private string NextPropertyNo(string psNo)
        {
            string keyName = psNo;

            var data = _db.PsCards.Where(w => w.PsNo == psNo)
                .OrderByDescending(o => o.PsNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "001";
            }
            else
            {
                var sequence = (int.Parse(data.PsNo.Split('-')[1]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(3, '0');
            }
        }

        public string GetDescription(PsCardVM fields)
        {
            var f = fields.FieldsMedicine;
            string description = "";
            description += string.IsNullOrWhiteSpace(f.GenericName) ? "" : f.GenericName.Trim();
            description += string.IsNullOrWhiteSpace(f.DosageStrength) ? "" : " " + f.DosageStrength.Trim();
            description += string.IsNullOrWhiteSpace(f.DosageForm) ? "" : " " + f.DosageForm.Trim();
            description += string.IsNullOrWhiteSpace(f.Others) ? "" : " " + f.Others.Trim();
            description += string.IsNullOrWhiteSpace(f.Brand) ? "" : " (" + f.Brand.Trim() + ")";
            return description;
        }

        public string GetStockNo(PsCardVM model)
        {
            string stockNo = model.ItemCode.Trim();
            if (model.FromDonation == true)
            {
                stockNo = "FD" + stockNo;
            }
            if (Enum.TryParse(model.ItemTypeCode, out Category category))
            {
                if (category == Category.D)
                {
                    var f = model.FieldsMedicine;
                    if (f.GenericName.Length >= 3)
                    {
                        stockNo += f.GenericName.Substring(0, 1) + f.GenericName.Substring(2, 1);
                    }
                    else
                    {
                        stockNo += f.GenericName.Substring(0, 1) + "X";
                    }
                    if (!string.IsNullOrWhiteSpace(f.DosageStrength))
                    {
                        stockNo += f.DosageStrength.Replace(" ", "").Trim();
                    }
                    if (!string.IsNullOrWhiteSpace(f.DosageForm))
                    {
                        stockNo += f.DosageForm.PadRight(3, 'X').Substring(0, 3);
                    }
                    if (!string.IsNullOrWhiteSpace(f.Brand))
                    {
                        stockNo += f.Brand.Replace(" ", "").Trim();
                    }
                }
                else if (category == Category.T)
                {
                    var f = model.FieldsVehicle;
                    if (f.Make.Length >= 3)
                    {
                        stockNo += f.Make.Substring(0, 1) + f.Make.Substring(2, 1);
                    }
                    else
                    {
                        stockNo += f.Make.Substring(0, 1) + "X";
                    }

                    if (f.YearModel > 0)
                    {
                        stockNo += f.YearModel.ToString().Trim();
                    }

                    if (string.IsNullOrWhiteSpace(f.Series))
                    {
                        stockNo += "XXX";
                    }
                    else
                    {
                        stockNo += f.Series.Substring(0, 3);
                    }
                }
            }

            return stockNo;
        }

        //public string GetStockName(PsCardVM model)
        //{
            
        //    string stockName = "";

        //    if (Enum.TryParse(model.ItemTypeCode, out Category category))
        //    {
        //        if (category == Category.T)
        //        {

        //        }
        //        else if (category == Category.D)
        //        {
        //            stockName = orderItem.Brand.Replace(" ", "").Trim();
        //            var fieldsMedicine = await _db.FieldsMedicines.FindAsync(risItemId);

        //            if (!string.IsNullOrWhiteSpace(fieldsMedicine.DosageForm))
        //            {
        //                stockName += fieldsMedicine.DosageForm.Substring(0, 3);
        //            }

        //            if (!string.IsNullOrWhiteSpace(fieldsMedicine.DosageStrength))
        //            {
        //                stockName += fieldsMedicine.DosageStrength.Replace(" ", "");
        //            }

        //            stockName += fieldsMedicine.GenericName.Replace(" ", "");
        //            stockName += orderItem.ItemCode.ToString(); ;
        //        }
        //        else if (category == Category.U)
        //        {

        //        }
        //    }

        //    return stockName;
        //}

    }
}