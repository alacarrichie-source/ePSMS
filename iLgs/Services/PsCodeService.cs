//using iLgs.Exceptions;
//using iLgs.Models;
//using iLgs.Services.Interfaces;
//using System;
//using System.Data.Entity;
//using System.Linq;
//using System.Threading.Tasks;

//namespace iLgs.Services
//{
//    public class PsCodeService : IPsCodeService
//    {
//        private readonly AppManEntities _db = new AppManEntities();
//        private readonly IRisItemService _risItemService;
//        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
//        private readonly IExceptionService<PsCodeVM> _vmExceptionService = new ExceptionService<PsCodeVM>();
//        private readonly IExceptionService<PsCode> _exceptionService = new ExceptionService<PsCode>();
//        //private IDirectoryService directoryService;
//        //private string imageDirectory;
//        public PsCodeService(AppManEntities db)
//        {
//            _db = db;
//            _risItemService = new RisItemService(db);
//            //this.directoryService = new DirectoryService();
//            //this.imageDirectory = directoryService.GetItemImageDirectory();
//        }

//        public IQueryable<PsCode> GetAll() => _exceptionService.TryCatch(() =>
//        {
//            var data = _db.PsCodes.AsQueryable();
//            return data;
//        });

//        public IQueryable<PsCodeVM> GetAllItems() => _vmExceptionService.TryCatch(() =>
//        {
//            var data = _db.PsCodes
//                .Select(s => new PsCodeVM
//                {
//                    Id = s.Id,
//                    ItemCodeId = s.ItemCodeId,
//                    ItemCode = s.ItemCode.Code,
//                    ItemCodeDesc = s.ItemCode.Description,
//                    PsType = s.PsType,
//                    PsNo = s.PsNo,
//                    ItemName = s.ItemName,
//                    UnitMeas = s.UnitMeas,
//                    PsTypeDesc = _db.ItemTypes.FirstOrDefault(f => f.Code == s.PsType).Description,
//                    FileName = _db.Uploads.Any(a => a.ImageId == s.Id) ? _db.Uploads.FirstOrDefault(f => f.ImageId == s.Id).FileName : ""                    
//                    //ImageUrl = this.imageDirectory + (db.Uploads.Any(a => a.ImageId == s.Id) ?
//                    //    db.Uploads.FirstOrDefault(f => f.ImageId == s.Id).FileName : "")
//                });
//            return data;
//        });

//        public IQueryable<PsCodeVM> GetStockItems() => _vmExceptionService.TryCatch(() =>
//        {
//            var data = _db.PsCodes
//                .Select(s => new PsCodeVM
//                {
//                    Id = s.Id,
//                    ItemCodeId = s.ItemCodeId,
//                    ItemCode = s.ItemCode.Code,
//                    ItemCodeDesc = s.ItemCode.Description,
//                    PsType = s.PsType,
//                    PsNo = s.PsNo,
//                    ItemName = s.ItemName,
//                    UnitMeas = s.UnitMeas,
//                    PsTypeDesc = _db.ItemTypes.FirstOrDefault(f => f.Code == s.PsType).Description,
//                    FileName = _db.Uploads.Any(a => a.ImageId == s.Id) ? _db.Uploads.FirstOrDefault(f => f.ImageId == s.Id).FileName : ""
//                    //ImageUrl = this.imageDirectory + (db.Uploads.Any(a => a.ImageId == s.Id) ?
//                    //    db.Uploads.FirstOrDefault(f => f.ImageId == s.Id).FileName : "")
//                });
//            return data;
//        });

//        public IQueryable<PsCodeVM> GetPropertyItems() => _vmExceptionService.TryCatch(() =>
//        {
//            var data = _db.PsCodes
//                .Select(s => new PsCodeVM
//                {
//                    Id = s.Id,
//                    ItemCodeId = s.ItemCodeId,
//                    ItemCode = s.ItemCode.Code,
//                    ItemCodeDesc = s.ItemCode.Description,
//                    PsType = s.PsType,
//                    PsNo = s.PsNo,
//                    ItemName = s.ItemName,
//                    UnitMeas = s.UnitMeas,
//                    PsTypeDesc = _db.ItemTypes.FirstOrDefault(f => f.Code == s.PsType).Description,
//                    FileName = _db.Uploads.Any(a => a.ImageId == s.Id) ? _db.Uploads.FirstOrDefault(f => f.ImageId == s.Id).FileName : ""
//                    //ImageUrl = this.imageDirectory + (db.Uploads.Any(a => a.ImageId == s.Id) ?
//                    //    db.Uploads.FirstOrDefault(f => f.ImageId == s.Id).FileName : "")
//                });
//            return data;
//        });

//        public ValueTask<PsCode> GetByIdAsync(Guid psId) => _exceptionService.TryCatch(async () =>
//        {
//            return await _db.PsCodes.FindAsync(psId);
//        });

//        public async ValueTask<bool> GetAnyPsNoAsync(Guid psId, string psNo) 
//        {
//            return await _db.PsCodes.AnyAsync(a => a.Id != psId && a.PsNo == psNo);
//        }

//        public ValueTask<PsCode> GetByPsNoAsync(string psNo) => _exceptionService.TryCatch(async () =>
//        {
//            return await _db.PsCodes.Where(w => w.PsNo == psNo).FirstOrDefaultAsync();
//        });

//        public ValueTask<PsCodeVM> CreateAsync(PsCodeVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
//        {
//            if (string.IsNullOrWhiteSpace(model.ItemName))
//            {
//                throw new InvalidValueException("Item Name is Required!");
//            }

//            model.PsNo = PsNo(model.ItemCode, model.ItemName);

//            if (_db.PsCodes.Where(w => w.PsNo == model.PsNo && w.ItemName == model.ItemName).Any())
//            {
//                throw new RecordAlreadyExistsException("Item Number already exists!");
//            }

//            model.Id = Guid.NewGuid();
//            model.InsertedBy = user;
//            model.InsertedDt = date;
//            model.UpdatedBy = user;
//            model.UpdatedDt = date;            
//            model.ItemName = model.ItemName.Trim();
//            model.ItemDescription = model.ItemDescription ?? "";
//            model.ReorderPoint = model.ReorderPoint ?? 0;
//            model.DaysToConsume = model.DaysToConsume ?? 0;


//            var entity = new PsCode()
//            {
//                Id = model.Id,
//                ItemCodeId = model.ItemCodeId,
//                PsNo = model.PsNo,
//                PsType = model.PsType,
//                ItemName = model.ItemName,
//                ItemDescription = model.ItemDescription,
//                ReorderPoint = model.ReorderPoint,
//                UnitMeas = model.UnitMeas,
//                DaysToConsume = model.DaysToConsume,
//                InsertedBy = model.InsertedBy,
//                InsertedDt = model.InsertedDt,
//                UpdatedBy = model.UpdatedBy,
//                UpdatedDt = model.UpdatedDt
//            };
            
//            _db.PsCodes.Add(entity);
//            await _db.SaveChangesAsync();

//            return model;
//        });

//        public ValueTask<PsCodeVM> UpdateAsync(PsCodeVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
//        {
//            var entity = _db.PsCodes.Find(model.Id);
//            if (entity == null)
//            {
//                throw new RecordNotFoundException(model.Id);
//            }

//            if (string.IsNullOrWhiteSpace(model.ItemName))
//            {
//                throw new InvalidValueException("Item Name is Required!");
//            }


//            model.PsNo = PsNo(model.ItemCode, model.ItemName);

//            if (_db.PsCodes.Where(w => w.PsNo == model.PsNo && w.ItemName == model.ItemName && w.Id != model.Id).Any())
//            {
//                throw new RecordAlreadyExistsException("Item Number already exists!");
//            }

//            model.UpdatedBy = user;
//            model.UpdatedDt = date;            
//            model.PsType = model.PsType.ToUpper();
//            model.ItemDescription = model.ItemDescription ?? "";
//            model.ReorderPoint = model.ReorderPoint ?? 0;
//            model.DaysToConsume = model.DaysToConsume ?? 0;

//            entity.PsNo = model.PsNo;
//            entity.PsType = model.PsType;
//            entity.ItemName = model.ItemName.Trim();
//            entity.ItemDescription = model.ItemDescription;
//            entity.ReorderPoint = model.ReorderPoint;
//            entity.DaysToConsume = model.DaysToConsume;
//            entity.UnitMeas = model.UnitMeas;
//            entity.UpdatedBy = user;
//            entity.UpdatedDt = date;

//            _db.PsCodes.Attach(entity);
//            _db.Entry(model).State = EntityState.Modified;
//            await _db.SaveChangesAsync();

//            return model;
//        });

//        public ValueTask<PsCodeVM> DeleteAsync(PsCodeVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
//        {

//            model.UpdatedBy = user;
//            model.UpdatedDt = date;

//            var entity = await _db.PsCodes.FindAsync(model.Id);

//            entity.UpdatedBy = user;
//            entity.UpdatedDt = date;

//            _db.PsCodes.Attach(entity);
//            _db.Entry(entity).State = EntityState.Modified;
//            await _db.SaveChangesAsync();

//            _db.PsCodes.Remove(entity);
//            _db.Entry(entity).State = EntityState.Deleted;
//            await _db.SaveChangesAsync();

//            return model;
//        });

//        public string PsNo(string itemCode, string itemName)
//        {
//            return _risItemService.PsNoDisplay(itemCode, itemName);
//        }

//        //public string GetImageUrl(Guid imageId)
//        //{
//        //    string imageUrl = this.imageDirectory + 
//        //        (db.Uploads.Any(a => a.ImageId == imageId) ? 
//        //            db.Uploads.FirstOrDefault(f => f.ImageId == imageId).FileName : "");
//        //    return imageUrl;
//        //}
//    }
//}