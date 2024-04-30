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
//    public interface IStockCardService
//    {
//        IQueryable<StockCardVM> GetAll();
//        IQueryable<StockCardVM> GetAllByItemCodeId(Guid? itemCodeId);
//        ValueTask<StockCardVM> GetVmByIdAsync(Guid? id);
//        ValueTask<StockCard> GetByIdAsync(Guid id);
//        ValueTask<StockCard> GetByStockNoAsync(string stockNo);
//        ValueTask<bool> GetAnyStockNoAsync(Guid id, string stockNo);
//        ValueTask<StockCardVM> CreateAsync(StockCardVM model, string user, DateTime date);
//        ValueTask<StockCardVM> UpdateAsync(StockCardVM model, string user, DateTime date);
//        ValueTask<StockCardVM> DeleteAsync(StockCardVM model, string user, DateTime date);
//        string GetDescription(StockCardVM fields);
//        //string GetPropNo(StockCardVM model);
//    }

//    public class StockCardService : IStockCardService
//    {
//        private readonly AppManEntities _db = new AppManEntities();
//        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
//        private readonly IExceptionService<StockCardVM> _vmExceptionService = new ExceptionService<StockCardVM>();
//        private readonly IExceptionService<StockCard> _exceptionService = new ExceptionService<StockCard>();

//        public StockCardService(AppManEntities db)
//        {
//            _db = db;
//        }

//        public IQueryable<StockCardVM> GetAll() => _vmExceptionService.TryCatch(() =>
//        {
//            var data = _db.StockCards
//                .Select(s => new StockCardVM
//                {
//                    Id = s.Id,
//                    ItemCodeId = s.ItemCodeId,
//                    Item = s.ItemCode.Description,
//                    ItemCode = s.ItemCode.Code,
//                    ItemType = s.ItemCode.ItemType.Description,
//                    ItemTypeCode = s.ItemCode.ItemType.Code,
//                    Description = s.Description,
//                    Fund = s.Fund,
//                    Unit = s.Unit,
//                    StockNo = s.StockNo,
//                    StockName = s.StockName,
//                    ReorderPoint = s.ReorderPoint,
//                    PrevStockNo = s.PrevStockNo,
//                    AcqDate = s.AcqDate,
//                    AcqMode = s.AcqMode,
//                    Amount = s.Amount,
//                    GenericName = s.GenericName,
//                    DosageStrength = s.DosageStrength,
//                    DosageForm = s.DosageForm,
//                    Brand = s.Brand,
//                    Others = s.Others,  
//                    OtherDesc = s.OtherDesc,
//                    InsertedDt = s.InsertedDt
//                });
//            return data;
//        });

//        public IQueryable<StockCardVM> GetAllByItemCodeId(Guid? itemCodeId) => _vmExceptionService.TryCatch(() =>
//        {
//            var data = _db.StockCards.Where(w => w.ItemCodeId == itemCodeId)
//                .Select(s => new StockCardVM
//                {
//                    Id = s.Id,
//                    ItemCodeId = s.ItemCodeId,
//                    Item = s.ItemCode.Description,
//                    ItemCode = s.ItemCode.Code,
//                    ItemType = s.ItemCode.ItemType.Description,
//                    ItemTypeCode = s.ItemCode.ItemType.Code,
//                    Description = s.Description,
//                    Fund = s.Fund,
//                    Unit = s.Unit,
//                    StockNo = s.StockNo,
//                    StockName = s.StockName,
//                    ReorderPoint = s.ReorderPoint,
//                    PrevStockNo = s.PrevStockNo,
//                    AcqDate = s.AcqDate,
//                    AcqMode = s.AcqMode,
//                    Amount = s.Amount,
//                    GenericName = s.GenericName,
//                    DosageStrength = s.DosageStrength,
//                    DosageForm = s.DosageForm,
//                    Brand = s.Brand,
//                    Others = s.Others,
//                    OtherDesc = s.OtherDesc,
//                    InsertedDt = s.InsertedDt
//                });
//            return data;
//        });

//        public ValueTask<StockCardVM> GetVmByIdAsync(Guid? id) => _vmExceptionService.TryCatch(async () =>
//        {
//            var data = await _db.StockCards.Where(w => w.Id == id)
//                .Select(s => new StockCardVM
//                {
//                    Id = s.Id,
//                    ItemCodeId = s.ItemCodeId,
//                    Item = s.ItemCode.Description,
//                    ItemCode = s.ItemCode.Code,
//                    ItemType = s.ItemCode.ItemType.Description,
//                    ItemTypeCode = s.ItemCode.ItemType.Code,
//                    Description = s.Description,
//                    Fund = s.Fund,
//                    Unit = s.Unit,
//                    StockNo = s.StockNo,
//                    StockName = s.StockName,
//                    ReorderPoint = s.ReorderPoint,
//                    PrevStockNo = s.PrevStockNo,
//                    AcqDate = s.AcqDate,
//                    AcqMode = s.AcqMode,
//                    Amount = s.Amount,
//                    GenericName = s.GenericName,
//                    DosageStrength = s.DosageStrength,
//                    DosageForm = s.DosageForm,
//                    Brand = s.Brand,
//                    Others = s.Others,
//                    OtherDesc = s.OtherDesc,
//                    InsertedDt = s.InsertedDt
//                }).FirstOrDefaultAsync();
//            return data;
//        });

//        public ValueTask<StockCard> GetByIdAsync(Guid id) => _exceptionService.TryCatch(async () =>
//        {
//            return await _db.StockCards.FindAsync(id);
//        });

//        public async ValueTask<bool> GetAnyStockNoAsync(Guid id, string stockNo)
//        {
//            return await _db.StockCards.AnyAsync(a => a.Id != id && a.StockNo == stockNo);
//        }

//        public ValueTask<StockCard> GetByStockNoAsync(string stockNo) => _exceptionService.TryCatch(async () =>
//        {
//            return await _db.StockCards.Where(w => w.StockNo == stockNo).FirstOrDefaultAsync();
//        });

//        public ValueTask<StockCardVM> CreateAsync(StockCardVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
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

//            var entity = new StockCard
//            {
//                Id = model.Id,                
//                ItemCodeId = model.ItemCodeId,
//                Description = model.Description,
//                Fund = model.Fund,
//                Unit = model.Unit,
//                StockNo = model.StockNo,
//                StockName = model.StockName,
//                ReorderPoint = model.ReorderPoint,
//                PrevStockNo = model.PrevStockNo,
//                AcqDate = model.AcqDate,
//                AcqMode = model.AcqMode,
//                Amount = model.Amount,
//                GenericName = model.GenericName,
//                DosageStrength = model.DosageStrength,
//                DosageForm = model.DosageForm,
//                Brand = model.Brand,
//                Others = model.Others,
//                OtherDesc = model.OtherDesc,
//                InsertedBy = model.InsertedBy,
//                InsertedDt = model.InsertedDt,
//                UpdatedBy = model.UpdatedBy,
//                UpdatedDt = model.UpdatedDt
//            };

//            _db.StockCards.Add(entity);
//            await _db.SaveChangesAsync();

//            return model;
//        });

//        public ValueTask<StockCardVM> UpdateAsync(StockCardVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
//        {
//            var entity = await _db.StockCards.SingleOrDefaultAsync(s => s.Id == model.Id);
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
//            entity.Description = model.Description;
//            entity.Fund = model.Fund;
//            entity.Unit = model.Unit;
//            entity.StockNo = model.StockNo;
//            entity.StockName = model.StockName;
//            entity.ReorderPoint = model.ReorderPoint;
//            entity.PrevStockNo = model.PrevStockNo;
//            entity.AcqDate = model.AcqDate;
//            entity.AcqMode = model.AcqMode;
//            entity.Amount = model.Amount;
//            entity.GenericName = model.GenericName;
//            entity.DosageStrength = model.DosageStrength;
//            entity.DosageForm = model.DosageForm;
//            entity.Brand = model.Brand;
//            entity.Others = model.Others;
//            entity.OtherDesc = model.OtherDesc;
//            entity.UpdatedBy = user;
//            entity.UpdatedDt = date;

//            _db.StockCards.Attach(entity);
//            _db.Entry(entity).State = EntityState.Modified;
//            await _db.SaveChangesAsync();

//            return model;
//        });

//        public ValueTask<StockCardVM> DeleteAsync(StockCardVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
//        {

//            model.UpdatedBy = user;
//            model.UpdatedDt = date;

//            var entity = await _db.StockCards.SingleOrDefaultAsync(s => s.Id == model.Id);

//            entity.UpdatedBy = user;
//            entity.UpdatedDt = date;

//            _db.StockCards.Attach(entity);
//            _db.Entry(entity).State = EntityState.Modified;
//            await _db.SaveChangesAsync();

//            _db.StockCards.Remove(entity);
//            _db.Entry(entity).State = EntityState.Deleted;
//            await _db.SaveChangesAsync();

//            return model;
//        });

//        public string GetDescription(StockCardVM fields)
//        {
//            string description = "";
//            description += string.IsNullOrWhiteSpace(fields.GenericName) ? "" : fields.GenericName.Trim();
//            description += string.IsNullOrWhiteSpace(fields.DosageStrength) ? "" : " " + fields.DosageStrength.Trim();
//            description += string.IsNullOrWhiteSpace(fields.DosageForm) ? "" : " " + fields.DosageForm.Trim();
//            description += string.IsNullOrWhiteSpace(fields.Others) ? "" : " " + fields.Others.Trim();
//            description += string.IsNullOrWhiteSpace(fields.Brand) ? "" : " (" + fields.Brand.Trim() + ")";
//            return description;
//        }
//    }
//}