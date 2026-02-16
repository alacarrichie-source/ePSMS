using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services.Requisition
{
    public interface IRisItemUnitGroupDescriptionService
    {
        IQueryable<RisItemUnitGroupDescriptionVM> GetByUnitGroupId(Guid? unitGroupId);
        ValueTask<RisItemUnitGroupDescription> GetByIdAsync(Guid? id);
        //ValueTask<RisItemUnitGroupDescriptionVM> CreateAsync(RisItemUnitGroupDescriptionVM model, string user, DateTime date);
        //ValueTask<RisItemUnitGroupDescriptionVM> UpdateAsync(RisItemUnitGroupDescriptionVM model, string user, DateTime date);
        //ValueTask<RisItemUnitGroupDescriptionVM> DeleteAsync(RisItemUnitGroupDescriptionVM model, string user, DateTime date);

        IRisItemUnitGroupDescriptionItemService UnitGroupDescriptionItem { get; }
    }

    internal class RisItemUnitGroupDescriptionService : BaseValidator, IRisItemUnitGroupDescriptionService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<RisItemUnitGroupDescriptionVM> _vmExceptionService;
        private readonly IExceptionService<RisItemUnitGroupDescription> _exceptionService;
        private readonly IRisSharedService _risSharedService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        private IRisItemUnitGroupDescriptionItemService _risItemUnitGroupDescriptionItemService;

        public RisItemUnitGroupDescriptionService(AppManEntities db)
        {
            _db = db;
            _exceptions = new CreateAndLogExceptions();
            _vmExceptionService = new ExceptionService<RisItemUnitGroupDescriptionVM>();
            _exceptionService = new ExceptionService<RisItemUnitGroupDescription>();
            _risSharedService = new RisSharedService(_db);
            _getDisplayName = propertyName => Utility.GetDisplayName<RisItemUnitGroupDescriptionVM>(propertyName);

            _risItemUnitGroupDescriptionItemService = new RisItemUnitGroupDescriptionItemService(_db);
        }

        //public RisItemUnitGroupDescriptionService(AppManEntities db,
        //    ICreateAndLogExceptions exceptions,
        //    IExceptionService<RisItemUnitGroupDescriptionVM> vmExceptionService,
        //    IExceptionService<RisItemUnitGroupDescription> exceptionService,
        //    IRisService risService)
        //{
        //    _db = db;
        //    _exceptions = exceptions;
        //    _vmExceptionService = vmExceptionService;
        //    _exceptionService = exceptionService;
        //    _risService = risService;
        //    _getDisplayName = propertyName => Utility.GetDisplayName<RisItemUnitGroupDescriptionVM>(propertyName);
        //}

        public IRisItemUnitGroupDescriptionItemService UnitGroupDescriptionItem => _risItemUnitGroupDescriptionItemService;

        public ValueTask<RisItemUnitGroupDescription> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.RisItemUnitGroupDescriptions.FindAsync(id);
            return data;
        });

        public IQueryable<RisItemUnitGroupDescriptionVM> GetByUnitGroupId(Guid? unitGroupId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RisItemUnitGroupDescriptions.Where(w => w.UnitGroupId == unitGroupId)
                .Select(s => new RisItemUnitGroupDescriptionVM
                {
                    Id = s.Id,
                    UnitGroupId = s.UnitGroupId,
                    Description = s.Description,
                    InsertedDt = s.InsertedDt
                });
            return data;
        });

        public ValueTask<RisItemUnitGroupDescriptionVM> CreateAsync(RisItemUnitGroupDescriptionVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateOnCreate(model);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            RisItemUnitGroupDescription entity = new RisItemUnitGroupDescription()
            {
                Id = model.Id,
                UnitGroupId = model.UnitGroupId,
                Description = model.Description,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.RisItemUnitGroupDescriptions.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RisItemUnitGroupDescriptionVM> DeleteAsync(RisItemUnitGroupDescriptionVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateOnDelete(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RisItemUnitGroupDescription entity = await _db.RisItemUnitGroupDescriptions.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RisItemUnitGroupDescriptions.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.RisItemUnitGroupDescriptions.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RisItemUnitGroupDescriptionVM> UpdateAsync(RisItemUnitGroupDescriptionVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateOnUpdate(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RisItemUnitGroupDescription entity = await _db.RisItemUnitGroupDescriptions.FindAsync(model.Id);

            entity.Description = model.Description;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RisItemUnitGroupDescriptions.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return model;
        });

        public void ValidateOnCreate(RisItemUnitGroupDescriptionVM model)
        {
            ValidateModel(model);
            ValidateIfPosted(model);
            ValidateFieldsOnCreateUpdate(model);
        }

        public void ValidateOnUpdate(RisItemUnitGroupDescriptionVM model)
        {
            ValidateModel(model);
            ValidateRecord(model.Id);
            ValidateIfPosted(model);
            ValidateFieldsOnCreateUpdate(model);
        }

        public void ValidateOnDelete(RisItemUnitGroupDescriptionVM model)
        {
            ValidateModel(model);
            ValidateRecord(model.Id);
            ValidateIfPosted(model);
        }

        public void ValidateIfPosted(RisItemUnitGroupDescriptionVM model)
        {
            var isPosted = _risSharedService.IsPosted(model);
            if (isPosted)
            {
                throw new RecordAlreadyPostedException();
            }
        }

        public void ValidateFieldsOnCreateUpdate(RisItemUnitGroupDescriptionVM model)
        {
            var ex = new InvalidModelException();
            
            if (string.IsNullOrWhiteSpace(model.Description))
            {
                ex.UpsertDataList(_getDisplayName(nameof(model.Description)), "Field is required.");
            }
            
            ex.ThrowIfContainsErrors();
        }


        private void ValidateRecord(Guid id)
        {
            if (!_db.RisItemUnitGroupDescriptions.Any(a => a.Id == id))
            {
                throw new NotFoundException(id);
            }
        }

        private static void ValidateModel(RisItemUnitGroupDescriptionVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }
    }
}