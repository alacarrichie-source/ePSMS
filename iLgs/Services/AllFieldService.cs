//using iLgs.Exceptions;
//using iLgs.Models;
//using iLgs.Services.Interfaces;
//using System;
//using System.Collections.Generic;
//using System.Data.Entity;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Web;

//namespace iLgs.Services
//{
//    public interface IAllFieldService
//    {
//        IQueryable<AllField> GetAllByPsCardId(Guid? psCardId);
//        IQueryable<AllField> GetAllByRisItemId(Guid? risItemId);
//        ValueTask<AllField> GetByIdAsync(Guid id);
//        ValueTask<AllField> CreatePsCardFieldsAsync(AllField model, string user, DateTime date);
//        ValueTask<AllField> CreateRisFieldsAsync(AllField model, string user, DateTime date);
//        ValueTask<AllField> UpdatePsCardFieldsAsync(AllField model, string user, DateTime date);
//        ValueTask<AllField> UpdateRisFieldsAsync(AllField model, string user, DateTime date);
//        ValueTask<AllField> DeleteAsync(AllField model, string user, DateTime date);
//        string GetDescription(AllField allFields);
//        string GetDescription(PsCardVM psCardVM);
//        string GetRisDescription(AllField allFields);
//        string GetStockNo(AllField allFields);
//        string GetRisStockNo(AllField allFields);
//    }

//    public class AllFieldService : IAllFieldService
//    {
//        private readonly AppManEntities _db = new AppManEntities();
//        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
//        private readonly IExceptionService<AllField> _exceptionService = new ExceptionService<AllField>();

//        public AllFieldService(AppManEntities db)
//        {
//            _db = db;
//        }

//        public IQueryable<AllField> GetAllByPsCardId(Guid? psCardId) => _exceptionService.TryCatch(() =>
//        {
//            var data = _db.AllFields.Where(w => w.PsCardId == psCardId).AsNoTracking();
//            return data;
//        });

//        public IQueryable<AllField> GetAllByRisItemId(Guid? risItemId) => _exceptionService.TryCatch(() =>
//        {
//            var data = _db.AllFields.Where(w => w.RisItemId == risItemId).AsNoTracking();
//            return data;
//        });

//        public ValueTask<AllField> GetByIdAsync(Guid id) => _exceptionService.TryCatch(async () =>
//        {
//            var data = await _db.AllFields.FindAsync(id);
//            return data;
//        });

//        private void ValidateEntry(AllField model, ItemCode itemCode)
//        {
//            if (!(itemCode.ForDistribution == "Y" && itemCode.IsConsumable == "Y" && itemCode.IsIncorporated == "Y"))
//            {
//                if (Enum.TryParse(itemCode.ItemType.Code, out Category category))
//                {
//                    if (category == Category.D)
//                    {
//                        if (string.IsNullOrWhiteSpace(model.GenericName))
//                        {
//                            throw new InvalidValueException("Generic Name is Required!");
//                        }
//                        if (string.IsNullOrWhiteSpace(model.Brand))
//                        {
//                            throw new InvalidValueException("Brand is Required!");
//                        }
//                        if (model.PsCard.ItemCode.ItemNo.Substring(0, 4) == "5.1.") // Alcoh1ol
//                        {
//                            if (string.IsNullOrWhiteSpace(model.DosageVolume))
//                            {
//                                throw new InvalidValueException("Dosage Volume is Required!");
//                            }
//                        }
//                        else
//                        {
//                            if (string.IsNullOrWhiteSpace(model.DosageStrength))
//                            {
//                                throw new InvalidValueException("Dosage Strength is Required!");
//                            }
//                            if (string.IsNullOrWhiteSpace(model.DosageForm))
//                            {
//                                throw new InvalidValueException("Dosage Form is Required!");
//                            }
//                        }
//                    }
//                    else if (category == Category.M)
//                    {
//                        if (string.IsNullOrWhiteSpace(model.Brand))
//                        {
//                            throw new InvalidValueException("Brand is Required!");
//                        }
//                        if (string.IsNullOrWhiteSpace(model.Model_))
//                        {
//                            throw new InvalidValueException("Model is Required!");
//                        }
//                        if (string.IsNullOrWhiteSpace(model.Size) && string.IsNullOrWhiteSpace(model.Dimension) && string.IsNullOrWhiteSpace(model.Weight)
//                            && string.IsNullOrWhiteSpace(model.Materials) && string.IsNullOrWhiteSpace(model.Capacity) && string.IsNullOrWhiteSpace(model.Color))
//                        {
//                            throw new InvalidValueException("Dimonsion or Sizeor Weight or Materials or Capacity or Color is Required!");
//                        }
//                    }
//                }
//            }
//        }

//        public ValueTask<AllField> CreatePsCardFieldsAsync(AllField model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
//        {
//            if (_db.AllFields.Any(a => a.Id == model.Id))
//            {
//                throw new RecordAlreadyExistsException(string.Format("Record already exists!"));
//            }

//            ValidateEntry(model, model.PsCard.ItemCode);

//            await CreateAsync(model, user, date);

//            return model;
//        });

//        public ValueTask<AllField> CreateRisFieldsAsync(AllField model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
//        {
//            if (_db.AllFields.Any(a => a.Id == model.Id))
//            {
//                throw new RecordAlreadyExistsException(string.Format("Record already exists!"));
//            }

//            ValidateEntry(model, model.RisItem.ItemCode);

//            await CreateAsync(model, user, date);

//            return model;
//        });

//        private async ValueTask CreateAsync(AllField model, string user, DateTime date)
//        {
//            model.Id = Guid.NewGuid();
//            model.InsertedBy = user;
//            model.InsertedDt = date;
//            model.UpdatedBy = user;
//            model.UpdatedDt = date;

//            var entity = new AllField
//            {
//                Id = model.Id,
//                PsCardId = model.PsCardId,
//                RisItemId = model.RisItemId,
//                SerialNo = model.SerialNo,
//                PropertyNo = model.PropertyNo,
//                PlateNo = model.PlateNo,
//                BodyNo = model.BodyNo,
//                MVFileNo = model.MVFileNo,
//                InvDist = model.InvDist,
//                GenericName = model.GenericName,
//                DosageStrength = model.DosageStrength,
//                DosageForm = model.DosageForm,
//                DosageVolume = model.DosageVolume,
//                Others = model.Others,
//                Brand = model.Brand,
//                Model_ = model.Model_,
//                Dimension = model.Dimension,
//                Size = model.Size,
//                Capacity = model.Capacity,
//                Color = model.Color,
//                InsertedBy = model.InsertedBy,
//                InsertedDt = model.InsertedDt,
//                UpdatedBy = model.UpdatedBy,
//                UpdatedDt = model.UpdatedDt
//            };

//            _db.AllFields.Add(entity);
//            await _db.SaveChangesAsync();
//        }

//        public ValueTask<AllField> UpdatePsCardFieldsAsync(AllField model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
//        {
//            ValidateEntry(model, model.PsCard.ItemCode);
//            await UpdateAsync(model, user, date);
//            return model;
//        });

//        public ValueTask<AllField> UpdateRisFieldsAsync(AllField model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
//        {
//            ValidateEntry(model, model.RisItem.ItemCode);
//            await UpdateAsync(model, user, date);
//            return model;
//        });

//        private async ValueTask UpdateAsync(AllField model, string user, DateTime date)
//        {
//            var entity = await GetByIdAsync(model.Id);
//            if (entity == null)
//            {
//                throw new RecordNotFoundException(model.Id);
//            }

//            model.UpdatedBy = user;
//            model.UpdatedDt = date;

//            entity.SerialNo = model.SerialNo;
//            entity.PropertyNo = model.PropertyNo;
//            entity.PlateNo = model.PlateNo;
//            entity.BodyNo = model.BodyNo;
//            entity.MVFileNo = model.MVFileNo;
//            entity.InvDist = model.InvDist;
//            entity.GenericName = model.GenericName;
//            entity.DosageStrength = model.DosageStrength;
//            entity.DosageForm = model.DosageForm;
//            entity.DosageVolume = model.DosageVolume;
//            entity.Others = model.Others;
//            entity.Brand = model.Brand;
//            entity.Model_ = model.Model_;
//            entity.Dimension = model.Dimension;
//            entity.Size = model.Size;
//            entity.Capacity = model.Capacity;
//            entity.Color = model.Color;
//            entity.UpdatedBy = user;
//            entity.UpdatedDt = date;

//            _db.AllFields.Attach(entity);
//            _db.Entry(entity).State = EntityState.Modified;
//            await _db.SaveChangesAsync();
//        }

//        public ValueTask<AllField> DeleteAsync(AllField model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
//        {
//            model.UpdatedBy = user;
//            model.UpdatedDt = date;

//            var entity = await GetByIdAsync(model.Id);

//            entity.UpdatedBy = user;
//            entity.UpdatedDt = date;

//            _db.AllFields.Attach(entity);
//            _db.Entry(entity).State = EntityState.Modified;
//            await _db.SaveChangesAsync();

//            _db.AllFields.Remove(entity);
//            _db.Entry(entity).State = EntityState.Deleted;
//            await _db.SaveChangesAsync();

//            return model;
//        });


//        private string NextPropertyNo(string psNo)
//        {
//            string keyName = psNo;

//            var data = _db.PsCards.Where(w => w.PsNo == psNo)
//                .OrderByDescending(o => o.PsNo).FirstOrDefault();
//            if (data == null)
//            {
//                return keyName + "-" + "001";
//            }
//            else
//            {
//                var sequence = (int.Parse(data.PsNo.Split('-')[1]) + 1).ToString();
//                return keyName + "-" + sequence.PadLeft(3, '0');
//            }
//        } 

//        //public string GetRisDescription(RisItemEntryVM fields)
//        //{
//        //    string description = "";
//        //    if (Enum.TryParse(fields.PsType, out Category category))
//        //    {
//        //        if (category == Category.D)
//        //        {
//        //            description = GetMedicineDescription(fields.FieldsMedicine);
//        //        }
//        //        else if (category == Category.O || category == Category.M)
//        //        {
//        //            description = GetOtherDescription(fields.FieldsOther);
//        //        }
//        //        else if (category == Category.T)
//        //        {
//        //            description = GetVehicleDescription(fields.FieldsVehicle);
//        //        }
//        //        //else if (category == Category.W)
//        //        //{
//        //        //    description = "Please see attachement.";
//        //        //}
//        //    }
//        //    return description ?? "";
//        //}

        

//        //public string GetRisStockNo(RisItemEntryVM model)
//        //{
//        //    string stockNo = model.ItemCode.Trim();
//        //    if (Enum.TryParse(model.PsType, out Category category))
//        //    {
//        //        if (category == Category.D)
//        //        {
//        //            stockNo += GetMedicinePsNo(model.FieldsMedicine);
//        //        }
//        //        else if (category == Category.O || category == Category.M)
//        //        {
//        //            stockNo += GetOtherPsNo(model.FieldsOther);
//        //        }
//        //        else if (category == Category.T)
//        //        {
//        //            stockNo += GetVehiclePsNo(model.FieldsVehicle);
//        //        }
//        //    }
//        //    return stockNo ?? "";
//        //}
        
//    }
//}