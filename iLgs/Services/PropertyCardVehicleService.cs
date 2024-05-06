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
//    public interface IPropertyCardVehicleService
//    {
//        IQueryable<PropertyCardVehicleVM> GetAll();
//        IQueryable<PropertyCardVehicleVM> GetAllByItemCodeId(Guid? itemCodeId);
//        ValueTask<PropertyCardVehicleVM> GetVmByIdAsync(Guid? id);
//        ValueTask<PropertyCard> GetByIdAsync(Guid id);
//        ValueTask<PropertyCard> GetByPropNoAsync(string propNo);
//        ValueTask<bool> GetAnyPropNoAsync(Guid id, string propNo);
//        ValueTask<PropertyCardVehicleVM> CreateAsync(PropertyCardVehicleVM model, string user, DateTime date);
//        ValueTask<PropertyCardVehicleVM> UpdateAsync(PropertyCardVehicleVM model, string user, DateTime date);
//        ValueTask<PropertyCardVehicleVM> DeleteAsync(PropertyCardVehicleVM model, string user, DateTime date);
//        string GetDescription(PropertyCardVehicleVM fields);
//        //string GetPropNo(PropertyCardVehicleVM model);
//    }

//    public class PropertyCardVehicleService : IPropertyCardVehicleService
//    {
//        private readonly AppManEntities _db = new AppManEntities();
//        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
//        private readonly IExceptionService<PropertyCardVehicleVM> _vmExceptionService = new ExceptionService<PropertyCardVehicleVM>();
//        private readonly IExceptionService<PropertyCard> _exceptionService = new ExceptionService<PropertyCard>();

//        public PropertyCardVehicleService(AppManEntities db)
//        {
//            _db = db;
//        }

//        public IQueryable<PropertyCardVehicleVM> GetAll() => _vmExceptionService.TryCatch(() =>
//        {
//            var data = _db.PropertyCards.OfType<PropertyCardVehicle>()
//                .Select(s => new PropertyCardVehicleVM
//                {
//                    Id = s.Id,
//                    ItemCodeId = s.ItemCodeId,
//                    Item = s.ItemCode.Description,
//                    ItemCode = s.ItemCode.Code,
//                    ItemType = s.ItemCode.ItemType.Description,
//                    ItemTypeCode = s.ItemCode.ItemType.Code,
//                    Description = s.Description,
//                    Fund = s.Fund,
//                    PropNo = s.PropNo,
//                    PrevPropNo = s.PrevPropNo,
//                    AcqDate = s.AcqDate,
//                    AcqMode = s.AcqMode,
//                    Amount = s.Amount,
//                    Type = s.Type,
//                    Make = s.Make,
//                    Series = s.Series,
//                    YearModel = s.YearModel,
//                    PlateNo = s.PlateNo,
//                    BodyNo = s.BodyNo,
//                    Color = s.Color,
//                    EngineNo = s.EngineNo,
//                    ChassisNo = s.ChassisNo,
//                    InsertedDt = s.InsertedDt
//                });
//            return data;
//        });

//        public IQueryable<PropertyCardVehicleVM> GetAllByItemCodeId(Guid? itemCodeId) => _vmExceptionService.TryCatch(() =>
//        {
//            var data = _db.PropertyCards.OfType<PropertyCardVehicle>().Where(w => w.ItemCodeId == itemCodeId)
//                .Select(s => new PropertyCardVehicleVM
//                {
//                    Id = s.Id,
//                    ItemCodeId = s.ItemCodeId,
//                    Item = s.ItemCode.Description,
//                    ItemCode = s.ItemCode.Code,
//                    ItemType = s.ItemCode.ItemType.Description,
//                    ItemTypeCode = s.ItemCode.ItemType.Code,
//                    Description = s.Description,
//                    Fund = s.Fund,
//                    PropNo = s.PropNo,
//                    PrevPropNo = s.PrevPropNo,
//                    AcqDate = s.AcqDate,
//                    AcqMode = s.AcqMode,
//                    Amount = s.Amount,
//                    Type = s.Type,
//                    Make = s.Make,
//                    Series = s.Series,
//                    YearModel = s.YearModel,
//                    PlateNo = s.PlateNo,
//                    BodyNo = s.BodyNo,
//                    Color = s.Color,
//                    EngineNo = s.EngineNo,
//                    ChassisNo = s.ChassisNo,
//                    InsertedDt = s.InsertedDt
//                });
//            return data;
//        });

//        public ValueTask<PropertyCardVehicleVM> GetVmByIdAsync(Guid? id) => _vmExceptionService.TryCatch(async () =>
//        {
//            var data = await _db.PropertyCards.OfType<PropertyCardVehicle>().Where(w => w.Id == id)
//                .Select(s => new PropertyCardVehicleVM
//                {
//                    Id = s.Id,
//                    ItemCodeId = s.ItemCodeId,
//                    Item = s.ItemCode.Description,
//                    ItemCode = s.ItemCode.Code,
//                    ItemType = s.ItemCode.ItemType.Description,
//                    ItemTypeCode = s.ItemCode.ItemType.Code,
//                    Description = s.Description,
//                    Fund = s.Fund,
//                    PropNo = s.PropNo,
//                    PrevPropNo = s.PrevPropNo,
//                    AcqDate = s.AcqDate,
//                    AcqMode = s.AcqMode,
//                    Amount = s.Amount,
//                    Type = s.Type,
//                    Make = s.Make,
//                    Series = s.Series,
//                    YearModel = s.YearModel,
//                    PlateNo = s.PlateNo,
//                    BodyNo = s.BodyNo,
//                    Color = s.Color,
//                    EngineNo = s.EngineNo,
//                    ChassisNo = s.ChassisNo,
//                    InsertedDt = s.InsertedDt
//                }).FirstOrDefaultAsync();
//            return data;
//        });

//        public ValueTask<PropertyCard> GetByIdAsync(Guid id) => _exceptionService.TryCatch(async () =>
//        {
//            return await _db.PropertyCards.FindAsync(id);
//        });

//        public async ValueTask<bool> GetAnyPropNoAsync(Guid id, string propNo)
//        {
//            return await _db.PropertyCards.AnyAsync(a => a.Id != id && a.PropNo == propNo);
//        }

//        public ValueTask<PropertyCard> GetByPropNoAsync(string propNo) => _exceptionService.TryCatch(async () =>
//        {
//            return await _db.PropertyCards.Where(w => w.PropNo == propNo).FirstOrDefaultAsync();
//        });

//        public ValueTask<PropertyCardVehicleVM> CreateAsync(PropertyCardVehicleVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
//        {
//            if (model.ItemCodeId == null)
//            {
//                throw new InvalidValueException("Item is Required!");
//            }

//            if (string.IsNullOrWhiteSpace(model.Description))
//            {
//                throw new InvalidValueException("Description is Required!");
//            }

//            if (string.IsNullOrWhiteSpace(model.Fund))
//            {
//                throw new InvalidValueException("Fund is Required!");
//            }

//            model.Id = Guid.NewGuid();
//            model.InsertedBy = user;
//            model.InsertedDt = date;
//            model.UpdatedBy = user;
//            model.UpdatedDt = date;

//            var entity = new PropertyCardVehicle
//            {
//                Id = model.Id,
//                ItemCodeId = model.ItemCodeId,
//                Fund = model.Fund,
//                Description = model.Description,
//                Unit = model.Unit,
//                PropNo = model.PropNo,
//                PrevPropNo = model.PrevPropNo,
//                AcqDate = model.AcqDate,
//                AcqMode = model.AcqMode,
//                Amount = model.Amount,
//                Type = model.Type,
//                Make = model.Make,
//                Series = model.Series,
//                YearModel = model.YearModel,
//                PlateNo = model.PlateNo,
//                BodyNo = model.BodyNo,
//                Color = model.Color,
//                EngineNo = model.EngineNo,
//                ChassisNo = model.ChassisNo,
//                InsertedBy = model.InsertedBy,
//                InsertedDt = model.InsertedDt,
//                UpdatedBy = model.UpdatedBy,
//                UpdatedDt = model.UpdatedDt
//            };

//            _db.PropertyCards.Add(entity);
//            await _db.SaveChangesAsync();

//            return model;
//        });

//        public ValueTask<PropertyCardVehicleVM> UpdateAsync(PropertyCardVehicleVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
//        {
//            var entity = await _db.PropertyCards.OfType<PropertyCardVehicle>().SingleOrDefaultAsync(s => s.Id == model.Id);
//            if (entity == null)
//            {
//                throw new RecordNotFoundException(model.Id);
//            }

//            if (model.ItemCodeId == null)
//            {
//                throw new InvalidValueException("Item is Required!");
//            }

//            if (string.IsNullOrWhiteSpace(model.Description))
//            {
//                throw new InvalidValueException("Description is Required!");
//            }

//            if (string.IsNullOrWhiteSpace(model.Fund))
//            {
//                throw new InvalidValueException("Fund is Required!");
//            }

//            model.UpdatedBy = user;
//            model.UpdatedDt = date;

//            entity.ItemCodeId = model.ItemCodeId;
//            entity.Fund = model.Fund;
//            entity.Description = model.Description;
//            entity.PropNo = model.PropNo;
//            entity.PrevPropNo = model.PrevPropNo;
//            entity.AcqDate = model.AcqDate;
//            entity.AcqMode = model.AcqMode;
//            entity.Amount = model.Amount;
//            entity.Type = model.Type;
//            entity.Make = model.Make;
//            entity.Series = model.Series;
//            entity.YearModel = model.YearModel;
//            entity.PlateNo = model.PlateNo;
//            entity.BodyNo = model.BodyNo;
//            entity.Color = model.Color;
//            entity.EngineNo = model.EngineNo;
//            entity.ChassisNo = model.ChassisNo;
//            entity.UpdatedBy = user;
//            entity.UpdatedDt = date;

//            _db.PropertyCards.Attach(entity);
//            _db.Entry(entity).State = EntityState.Modified;
//            await _db.SaveChangesAsync();

//            return model;
//        });

//        public ValueTask<PropertyCardVehicleVM> DeleteAsync(PropertyCardVehicleVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
//        {

//            model.UpdatedBy = user;
//            model.UpdatedDt = date;

//            var entity = await _db.PropertyCards.OfType<PropertyCardVehicle>().SingleOrDefaultAsync(s => s.Id == model.Id);

//            entity.UpdatedBy = user;
//            entity.UpdatedDt = date;

//            _db.PropertyCards.Attach(entity);
//            _db.Entry(entity).State = EntityState.Modified;
//            await _db.SaveChangesAsync();

//            _db.PropertyCards.Remove(entity);
//            _db.Entry(entity).State = EntityState.Deleted;
//            await _db.SaveChangesAsync();

//            return model;
//        });

//        public string GetDescription(PropertyCardVehicleVM fields)
//        {
//            string description = "";
//            description += string.IsNullOrWhiteSpace(fields.Type) ? "" : fields.Type.Trim();
//            description += string.IsNullOrWhiteSpace(fields.Make) ? "" : " " + fields.Make.Trim();
//            description += string.IsNullOrWhiteSpace(fields.Series) ? "" : " " + fields.Series.Trim();
//            description += string.IsNullOrWhiteSpace(fields.YearModel.ToString()) ? "" : " " + fields.YearModel.ToString().Trim();
//            description += string.IsNullOrWhiteSpace(fields.PlateNo) ? "" : " " + fields.PlateNo.Trim();
//            description += string.IsNullOrWhiteSpace(fields.BodyNo) ? "" : " " + fields.BodyNo.Trim();
//            description += string.IsNullOrWhiteSpace(fields.Color) ? "" : " " + fields.Color.Trim();
//            description += string.IsNullOrWhiteSpace(fields.EngineNo) ? "" : " " + fields.EngineNo.Trim();
//            description += string.IsNullOrWhiteSpace(fields.ChassisNo) ? "" : " (" + fields.ChassisNo.Trim() + ")";
//            return description;

            
//        }
//    }
//}