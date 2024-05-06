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
//    public interface IPropertyService
//    {
//        IQueryable<PropertyVM> GetAll();
//        IQueryable<PropertyVM> GetAllByPsId(Guid? psId);
//        ValueTask<PsStock> GetByIdAsync(Guid id);
//        ValueTask<PsStock> GetByPsNoAsync(string psNo);
//        ValueTask<bool> GetAnyPsNoAsync(Guid id, string psNo);
//        ValueTask<PropertyVM> CreateAsync(PropertyVM model, string user, DateTime date);
//        ValueTask<PropertyVM> UpdateAsync(PropertyVM model, string user, DateTime date);
//        ValueTask<PropertyVM> DeleteAsync(PropertyVM model, string user, DateTime date);
//    }

//    public class PropertyService : IPropertyService
//    {
//        private readonly AppManEntities _db = new AppManEntities();
//        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
//        private readonly IExceptionService<PropertyVM> _vmExceptionService = new ExceptionService<PropertyVM>();
//        private readonly IExceptionService<PsStock> _exceptionService = new ExceptionService<PsStock>();

//        public PropertyService(AppManEntities db)
//        {
//            _db = db;            
//        }

//        public IQueryable<PropertyVM> GetAll() => _vmExceptionService.TryCatch(() =>
//        {
//            var data = _db.PsStocks
//                .Select(s => new PropertyVM
//                {
//                    Id = s.Id,
//                    PsId = s.PsId,
//                    StockNo = s.StockNo,
//                    StockName = s.StockName,
//                    Description = s.Description,
//                    Brand = s.Brand,
//                    Fund = s.Fund,
//                    UnitMeas = s.UnitMeas,
//                    InsertedDt = s.InsertedDt
//                });
//            return data;
//        });

//        public IQueryable<PropertyVM> GetAllByPsId(Guid? psId) => _vmExceptionService.TryCatch(() =>
//        {
//            var data = _db.PsStocks.Where(w => w.PsId == psId)
//                .Select(s => new PropertyVM
//                {
//                    Id = s.Id,
//                    PsId = s.PsId,
//                    StockNo = s.StockNo,
//                    StockName = s.StockName,
//                    Description = s.Description,
//                    Brand = s.Brand,
//                    Fund = s.Fund,
//                    UnitMeas = s.UnitMeas,
//                    InsertedDt = s.InsertedDt
//                });
//            return data;
//        });

//        public ValueTask<PsStock> GetByIdAsync(Guid id) => _exceptionService.TryCatch(async () =>
//        {
//            return await _db.PsStocks.FindAsync(id);
//        });

//        public async ValueTask<bool> GetAnyPsNoAsync(Guid psId, string psNo)
//        {
//            return await _db.PsStocks.AnyAsync(a => a.Id != psId && a.StockNo == psNo);
//        }

//        public ValueTask<PsStock> GetByPsNoAsync(string psNo) => _exceptionService.TryCatch(async () =>
//        {
//            return await _db.PsStocks.Where(w => w.StockNo == psNo).FirstOrDefaultAsync();
//        });

//        public ValueTask<PropertyVM> CreateAsync(PropertyVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
//        {
//            if (string.IsNullOrWhiteSpace(model.StockName))
//            {
//                throw new InvalidValueException("PPE is Required!");
//            }
            
//            model.Id = Guid.NewGuid();
//            model.InsertedBy = user;
//            model.InsertedDt = date;
//            model.UpdatedBy = user;
//            model.UpdatedDt = date;
            
//            var entity = new PsStock
//            {
//                Id = model.Id,
//                PsId = model.PsId,
//                Fund = model.Fund,
//                StockNo = model.StockNo,
//                StockName = model.StockName,
//                Description = model.Description,
//                Brand = model.Brand,
//                UnitMeas = model.UnitMeas,
//                InsertedBy = model.InsertedBy,
//                InsertedDt = model.InsertedDt,
//                UpdatedBy = model.UpdatedBy,
//                UpdatedDt = model.UpdatedDt
//            };

//            _db.PsStocks.Add(entity);
//            await _db.SaveChangesAsync();
            
//            return model;
//        });

//        public ValueTask<PropertyVM> UpdateAsync(PropertyVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
//        {
//            var entity = _db.PsStocks.Find(model.Id);
//            if (entity == null)
//            {
//                throw new RecordNotFoundException(model.Id);
//            }

//            if (string.IsNullOrWhiteSpace(model.StockName))
//            {
//                throw new InvalidValueException("Item Name is Required!");
//            }
            
//            model.UpdatedBy = user;
//            model.UpdatedDt = date;

//            entity.PsId = model.PsId;
//            entity.Fund = model.Fund;
//            entity.StockNo = model.StockNo;
//            entity.StockName = model.StockName;
//            entity.Description = model.Description;
//            entity.Brand = model.Brand;
//            entity.UnitMeas = model.UnitMeas;
//            entity.InsertedBy = model.InsertedBy;
//            entity.InsertedDt = model.InsertedDt;
//            entity.UpdatedBy = user;
//            entity.UpdatedDt = date;

//            _db.PsStocks.Attach(entity);
//            _db.Entry(model).State = EntityState.Modified;
//            await _db.SaveChangesAsync();

//            return model;
//        });

//        public ValueTask<PropertyVM> DeleteAsync(PropertyVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
//        {

//            model.UpdatedBy = user;
//            model.UpdatedDt = date;

//            var entity = await _db.PsStocks.FindAsync(model.Id);

//            entity.UpdatedBy = user;
//            entity.UpdatedDt = date;

//            _db.PsStocks.Attach(entity);
//            _db.Entry(entity).State = EntityState.Modified;
//            await _db.SaveChangesAsync();

//            _db.PsStocks.Remove(entity);
//            _db.Entry(entity).State = EntityState.Deleted;
//            await _db.SaveChangesAsync();

//            return model;
//        });
        
//        private string NextPropertyNo(string psNo)
//        {
//            string keyName = psNo;

//            var data = _db.PsStocks.Where(w => w.PsCode.PsNo == psNo)
//                .OrderByDescending(o => o.StockNo).FirstOrDefault();
//            if (data == null)
//            {
//                return keyName + "-" + "001";
//            }
//            else
//            {
//                var sequence = (int.Parse(data.StockNo.Split('-')[1]) + 1).ToString();
//                return keyName + "-" + sequence.PadLeft(3, '0');
//            }
//        }
//    }
//}