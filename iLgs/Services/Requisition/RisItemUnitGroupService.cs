//using iLgs.Exceptions;
//using iLgs.Models;
//using iLgs.Services.Validators;
//using System;
//using System.Data.Entity;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Threading.Tasks;

//namespace iLgs.Services.Requisition
//{
//    public interface IRisItemUnitGroupService
//    {
//        IQueryable<RisItemUnitGroupVM> GetByRisId(Guid? risId);
//        ValueTask<RisItemUnitGroupVM> GetByIdAsync(Guid? id);
//        //ValueTask<RisItemUnitGroupVM> CreateAsync(RisItemUnitGroupVM model, string user, DateTime date);
//        //ValueTask<RisItemUnitGroupVM> UpdateAsync(RisItemUnitGroupVM model, string user, DateTime date);
//        //ValueTask<RisItemUnitGroupVM> DeleteAsync(RisItemUnitGroupVM model, string user, DateTime date);

//        IRisItemUnitGroupDescriptionService UnitGroupDescription { get; }
//    }

//    internal class RisItemUnitGroupService : IRisItemUnitGroupService
//    {
//        private readonly AppManEntities _db;
//        private readonly ICreateAndLogExceptions _exceptions;
//        private readonly IExceptionService<RisItemUnitGroupVM> _vmExceptionService;
//        private readonly IExceptionService<RisItemUnitGroup> _exceptionService;
//        private readonly IRisItemUnitGroupValidator _validator;

//        private IRisItemUnitGroupDescriptionService _risItemUnitGroupDescriptionService;

//        public RisItemUnitGroupService(AppManEntities db)
//        {
//            _db = db;
//            _exceptions = new CreateAndLogExceptions();
//            _vmExceptionService = new ExceptionService<RisItemUnitGroupVM>();
//            _exceptionService = new ExceptionService<RisItemUnitGroup>();
//            _validator = new RisItemUnitGroupValidator(_db);

//            _risItemUnitGroupDescriptionService = new RisItemUnitGroupDescriptionService(_db);
//        }

//        public IRisItemUnitGroupDescriptionService UnitGroupDescription => _risItemUnitGroupDescriptionService;

//        //public RisItemUnitGroupService(AppManEntities db,
//        //    ICreateAndLogExceptions exceptions,
//        //    IExceptionService<RisItemUnitGroupVM> vmExceptionService,
//        //    IExceptionService<RisItemUnitGroup> exceptionService,
//        //    IRisItemUnitGroupValidator validator)
//        //{
//        //    _db = db;
//        //    _exceptions = exceptions;
//        //    _vmExceptionService = vmExceptionService;
//        //    _exceptionService = exceptionService;
//        //    _validator = validator;
//        //}

//        private static Expression<Func<RisItemUnitGroup, RisItemUnitGroupVM>> Projection
//        = s => new RisItemUnitGroupVM
//        {
//            Id = s.Id,
//            RisId = s.RisId,
//            SetLotNo = s.SetLotNo,
//            Qty = s.Qty,
//            Unit = s.Unit,
//            InsertedDt = s.InsertedDt
//        };

//        public ValueTask<RisItemUnitGroupVM> GetByIdAsync(Guid? id) => _vmExceptionService.TryCatch(async () =>
//        {
//            return await _db.RisItemUnitGroups.Where(w => w.Id == id).Select(Projection).FirstOrDefaultAsync();
//        });

//        public IQueryable<RisItemUnitGroupVM> GetByRisId(Guid? risId) =>
//        _vmExceptionService.TryCatch(() =>
//        {
//            var data = _db.RisItemUnitGroups.Where(w => w.RisId == risId).Select(Projection);                
//            return data;
//        });

//        public ValueTask<RisItemUnitGroupVM> CreateAsync(RisItemUnitGroupVM model, string user, DateTime date) =>
//        _vmExceptionService.TryCatch(async () =>
//        {
//            _validator.ValidateOnCreate(model);            
//            model.Id = Guid.NewGuid();
//            model.InsertedBy = user;
//            model.UpdatedBy = user;
//            model.InsertedDt = date;
//            model.UpdatedDt = date;

//            model.SetLotNo = SetLotNo(model.RisId);

//            RisItemUnitGroup entity = new RisItemUnitGroup()
//            {
//                Id = model.Id,
//                RisId = model.RisId,
//                SetLotNo = model.SetLotNo,
//                Qty = model.Qty,
//                Unit = model.Unit,
//                InsertedBy = model.InsertedBy,
//                InsertedDt = model.InsertedDt,
//                UpdatedBy = model.UpdatedBy,
//                UpdatedDt = model.UpdatedDt
//            };

//            _db.RisItemUnitGroups.Add(entity);
//            await _db.SaveChangesAsync();

//            return model;
//        });

//        public ValueTask<RisItemUnitGroupVM> DeleteAsync(RisItemUnitGroupVM model, string user, DateTime date) =>
//        _vmExceptionService.TryCatch(async () =>
//        {
//            _validator.ValidateOnDelete(model);

//            RisItemUnitGroup entity = await _db.RisItemUnitGroups.Where(w => w.Id == model.Id).FirstOrDefaultAsync();

//            model.UpdatedBy = user;
//            model.UpdatedDt = date;            

//            entity.UpdatedBy = model.UpdatedBy;
//            entity.UpdatedDt = model.UpdatedDt;

//            _db.RisItemUnitGroups.Attach(entity);
//            _db.Entry(entity).State = EntityState.Modified;
//            await _db.SaveChangesAsync();

//            _db.RisItemUnitGroups.Remove(entity);
//            _db.Entry(entity).State = EntityState.Deleted;
//            await _db.SaveChangesAsync();

//            return model;
//        });

//        public ValueTask<RisItemUnitGroupVM> UpdateAsync(RisItemUnitGroupVM model, string user, DateTime date) =>
//        _vmExceptionService.TryCatch(async () =>
//        {
//            _validator.ValidateOnUpdate(model);

//            RisItemUnitGroup entity = await _db.RisItemUnitGroups.Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            
//            model.UpdatedBy = user;
//            model.UpdatedDt = date;            

//            entity.RisId = model.RisId;
//            entity.SetLotNo = model.SetLotNo;
//            entity.Unit = model.Unit;
//            entity.Qty = model.Qty;
//            entity.UpdatedBy = model.UpdatedBy;
//            entity.UpdatedDt = model.UpdatedDt;

//            _db.RisItemUnitGroups.Attach(entity);
//            _db.Entry(entity).State = EntityState.Modified;
//            await _db.SaveChangesAsync();            

//            return model;
//        });

//        private string SetLotNo(Guid? risId)
//        {
//            var count = _db.RisItemUnitGroups.Where(w => w.RisId == risId).Count();
//            count++;
//            return count.ToString();            
//        }
//    }
//}