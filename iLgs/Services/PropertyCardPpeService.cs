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
    public interface IPropertyCardPpeService
    {
        IQueryable<PropertyCardPpeVM> GetAll();
        IQueryable<PropertyCardPpeVM> GetAllByItemCodeId(Guid? itemCodeId);
        ValueTask<PropertyCardPpeVM> GetVmByIdAsync(Guid? id);
        ValueTask<PropertyCard> GetByIdAsync(Guid id);
        ValueTask<PropertyCard> GetByPropNoAsync(string propNo);
        ValueTask<bool> GetAnyPropNoAsync(Guid id, string propNo);
        ValueTask<PropertyCardPpeVM> CreateAsync(PropertyCardPpeVM model, string user, DateTime date);
        ValueTask<PropertyCardPpeVM> UpdateAsync(PropertyCardPpeVM model, string user, DateTime date);
        ValueTask<PropertyCardPpeVM> DeleteAsync(PropertyCardPpeVM model, string user, DateTime date);
        string GetDescription(PropertyCardPpeFields fields);
        //string GetPropNo(PropertyCardPpeVM model);
    }

    public class PropertyCardPpeService : IPropertyCardPpeService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<PropertyCardPpeVM> _vmExceptionService = new ExceptionService<PropertyCardPpeVM>();
        private readonly IExceptionService<PropertyCard> _exceptionService = new ExceptionService<PropertyCard>();

        public PropertyCardPpeService(AppManEntities db)
        {
            _db = db;
        }

        public IQueryable<PropertyCardPpeVM> GetAll() => _vmExceptionService.TryCatch(() =>
        {
            var data = _db.PropertyCards.OfType<PropertyCardPpe>()
                .Select(s => new PropertyCardPpeVM
                {
                    Id = s.Id,
                    ItemCodeId = s.ItemCodeId,
                    Item = s.ItemCode.Description,
                    ItemCode = s.ItemCode.Code,
                    ItemType = s.ItemCode.ItemType.Description,
                    ItemTypeCode = s.ItemCode.ItemType.Code,
                    Description = s.Description,
                    Fund = s.Fund,
                    PropNo = s.PropNo,
                    PrevPropNo = s.PrevPropNo,
                    AcqDate = s.AcqDate,
                    AcqMode = s.AcqMode,
                    Amount = s.Amount,
                    Type = s.Type,
                    Brand = s.Brand,
                    Model_ = s.Model_,
                    SerialNo = s.SerialNo,
                    Others = s.Others,
                    Color = s.Color,
                    InsertedDt = s.InsertedDt
                });
            return data;
        });

        public IQueryable<PropertyCardPpeVM> GetAllByItemCodeId(Guid? itemCodeId) => _vmExceptionService.TryCatch(() =>
        {
            var data = _db.PropertyCards.OfType<PropertyCardPpe>().Where(w => w.ItemCodeId == itemCodeId)
                .Select(s => new PropertyCardPpeVM
                {
                    Id = s.Id,
                    ItemCodeId = s.ItemCodeId,
                    Item = s.ItemCode.Description,
                    ItemCode = s.ItemCode.Code,
                    ItemType = s.ItemCode.ItemType.Description,
                    ItemTypeCode = s.ItemCode.ItemType.Code,
                    Description = s.Description,
                    Fund = s.Fund,
                    PropNo = s.PropNo,
                    PrevPropNo = s.PrevPropNo,
                    AcqDate = s.AcqDate,
                    AcqMode = s.AcqMode,
                    Amount = s.Amount,
                    Type = s.Type,
                    Brand = s.Brand,
                    Model_ = s.Model_,
                    SerialNo = s.SerialNo,
                    Others = s.Others,
                    Color = s.Color,
                    InsertedDt = s.InsertedDt
                });
            return data;
        });

        public ValueTask<PropertyCardPpeVM> GetVmByIdAsync(Guid? id) => _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.PropertyCards.OfType<PropertyCardPpe>().Where(w => w.Id == id)
                .Select(s => new PropertyCardPpeVM
                {
                    Id = s.Id,
                    ItemCodeId = s.ItemCodeId,
                    Item = s.ItemCode.Description,
                    ItemCode = s.ItemCode.Code,
                    ItemType = s.ItemCode.ItemType.Description,
                    ItemTypeCode = s.ItemCode.ItemType.Code,
                    Description = s.Description,
                    Fund = s.Fund,
                    PropNo = s.PropNo,
                    PrevPropNo = s.PrevPropNo,
                    AcqDate = s.AcqDate,
                    AcqMode = s.AcqMode,
                    Amount = s.Amount,
                    Type = s.Type,
                    Brand = s.Brand,
                    Model_ = s.Model_,
                    SerialNo = s.SerialNo,
                    Others = s.Others,
                    Color = s.Color,
                    InsertedDt = s.InsertedDt
                }).FirstOrDefaultAsync();
            return data;
        });

        public ValueTask<PropertyCard> GetByIdAsync(Guid id) => _exceptionService.TryCatch(async () =>
        {
            return await _db.PropertyCards.FindAsync(id);
        });

        public async ValueTask<bool> GetAnyPropNoAsync(Guid id, string propNo)
        {
            return await _db.PropertyCards.AnyAsync(a => a.Id != id && a.PropNo == propNo);
        }

        public ValueTask<PropertyCard> GetByPropNoAsync(string propNo) => _exceptionService.TryCatch(async () =>
        {
            return await _db.PropertyCards.Where(w => w.PropNo == propNo).FirstOrDefaultAsync();
        });

        public ValueTask<PropertyCardPpeVM> CreateAsync(PropertyCardPpeVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
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

            var entity = new PropertyCardPpe
            {
                Id = model.Id,
                ItemCodeId = model.ItemCodeId,
                Fund = model.Fund,
                Description = model.Description,
                Unit = model.Unit,
                PropNo = model.PropNo,
                PrevPropNo = model.PrevPropNo,
                AcqDate = model.AcqDate,
                AcqMode = model.AcqMode,
                Amount = model.Amount,
                Type = model.Type,
                Brand = model.Brand,
                Model_ = model.Model_,
                SerialNo = model.SerialNo,
                Others = model.Others,
                Color = model.Color,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.PropertyCards.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<PropertyCardPpeVM> UpdateAsync(PropertyCardPpeVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            var entity = await _db.PropertyCards.OfType<PropertyCardPpe>().SingleOrDefaultAsync(s => s.Id == model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

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

            model.UpdatedBy = user;
            model.UpdatedDt = date;
            
            entity.ItemCodeId = model.ItemCodeId;
            entity.Fund = model.Fund;
            entity.Description = model.Description;
            entity.PropNo = model.PropNo;
            entity.PrevPropNo = model.PrevPropNo;
            entity.AcqDate = model.AcqDate;
            entity.AcqMode = model.AcqMode;
            entity.Amount = model.Amount;
            entity.Type = model.Type;
            entity.Brand = model.Brand;
            entity.Model_ = model.Model_;
            entity.SerialNo = model.SerialNo;
            entity.Others = model.Others;
            entity.Color = model.Color;
            entity.InsertedBy = model.InsertedBy;
            entity.InsertedDt = model.InsertedDt;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PropertyCards.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<PropertyCardPpeVM> DeleteAsync(PropertyCardPpeVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PropertyCards.OfType<PropertyCardPpe>().SingleOrDefaultAsync(s => s.Id == model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PropertyCards.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PropertyCards.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });
        
        public string GetDescription(PropertyCardPpeFields fields)
        {
            string description = "";
            description += string.IsNullOrWhiteSpace(fields.Type) ? "" : fields.Type.Trim();
            description += string.IsNullOrWhiteSpace(fields.Model_) ? "" : " " + fields.Model_.Trim();
            //description += string.IsNullOrWhiteSpace(fields.SerialNo) ? "" : " " + fields.SerialNo.Trim();
            description += string.IsNullOrWhiteSpace(fields.Others) ? "" : " " + fields.Others.Trim();
            description += string.IsNullOrWhiteSpace(fields.Color) ? "" : " " + fields.Color.Trim();
            description += string.IsNullOrWhiteSpace(fields.Brand) ? "" : " (" + fields.Brand.Trim() + ")";
            return description;
        }

        //public string GetPropNo(PropertyCardPpeVM model)
        //{
        //    var risItemExtns = (List<RisItemExtnVM>)Newtonsoft.Json.JsonConvert.DeserializeObject(model.GridRisItemExtns, typeof(List<RisItemExtnVM>));
        //    string psNo = model.ItemCode.Trim();
        //    var itemType = db.ItemTypes.Where(w => w.Code == model.PsType).FirstOrDefault();
        //    string itemValue = "";
        //    for (var x = 1; x <= itemType.FormulaNo; x++)
        //    {
        //        itemValue = risItemExtns.FirstOrDefault(f => f.ItemNo == x.ToString())?.ItemValue.Replace(" ", "").Trim();
        //        if (string.IsNullOrWhiteSpace(itemValue))
        //        {
        //            psNo += "XXX";
        //        }
        //        else
        //        {
        //            int itemCount = 0;
        //            if (model.PsType == "M")
        //            {
        //                var raItems = itemValue.Split('/');
        //                foreach (var raItem in raItems)
        //                {
        //                    if (++itemCount > 1)
        //                    {
        //                        psNo += "/";
        //                    }

        //                    if (x == 1)
        //                    {
        //                        if (raItem.Length >= 3)
        //                        {
        //                            psNo += raItem.Substring(0, 1) + raItem.Substring(2, 1);
        //                        }
        //                        else
        //                        {
        //                            psNo += raItem.Substring(0, 1) + "X";
        //                        }
        //                    }
        //                    else if (x == 2)
        //                    {
        //                        psNo += raItem;
        //                    }
        //                    else if (x == 3)
        //                    {
        //                        psNo += raItem.PadRight(3, 'X').Substring(0, 3);
        //                    }
        //                }
        //            }
        //            else if (model.PsType == "L")
        //            {
        //                if (x == 1)
        //                {
        //                    psNo += itemValue;
        //                }
        //                else if (x == 2)
        //                {
        //                    psNo += itemValue.Substring(0, 1).ToUpper();
        //                }
        //                else if (x == 3)
        //                {
        //                    psNo += itemValue.Substring(0, 1).ToUpper();
        //                }
        //                else if (x == 4)
        //                {
        //                    psNo += itemValue.Substring(2, 2);
        //                }
        //                else if (x == 5)
        //                {
        //                    psNo += itemValue.Replace(",", "");
        //                }
        //                else if (x == 6)
        //                {
        //                    psNo += itemValue.Substring(0, 1).ToUpper();
        //                }
        //                else if (x == 7)
        //                {
        //                    psNo += itemValue.Substring(itemValue.Length - 3);
        //                }
        //                else if (x == 8)
        //                {
        //                    psNo += itemValue.Substring(0, 1).ToUpper();
        //                }
        //                else if (x == 9)
        //                {
        //                    psNo += itemValue.Substring(2, 2);
        //                }
        //            }
        //        }
        //    }

        //    //if (model.PsType == "M")
        //    //{
        //    //    var ds = risItemExtns.FirstOrDefault(f => f.ItemKey == "Dosage Strength");
        //    //    if (ds != null)
        //    //    {
        //    //        psNo += ds.ItemValue.Replace(" ", "");
        //    //    }

        //    //    var df = risItemExtns.FirstOrDefault(f => f.ItemKey == "Dosage Form");
        //    //    if (df != null)
        //    //    {
        //    //        psNo += df.ItemValue.Substring(0, 3);
        //    //    }
        //    //}

        //    return psNo;
        //}
    }
}