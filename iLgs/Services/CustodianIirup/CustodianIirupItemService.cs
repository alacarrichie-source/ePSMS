using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.CustodianDisposal_;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.CustodianIirup
{
    public interface ICustodianIirupItemService
    {
        IQueryable<CustodianIirupItem> GetAllByCustodianIirupId(Guid? custodianIirupId);
        ValueTask<CustodianIirupItem> GetByIdAsync(Guid? id);

        ValueTask<CustodianIirupItem> SaveAsync(CustodianIirupItem model, string user, DateTime date);
        ValueTask<CustodianIirupItem> DeleteAsync(CustodianIirupItem model, string user, DateTime date);
    }

    public class CustodianIirupItemService : BaseValidator, ICustodianIirupItemService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<CustodianIirupItem> _exceptionService;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly ICustodianDisposalItemService _custodianDisposalItemService;
        
        public CustodianIirupItemService(AppManEntities db)
        {
            _db = db;
            _exceptionService = new ExceptionService<CustodianIirupItem>();
            _custodianDisposalItemService = new CustodianDisposalItemService(_db);
            _getDisplayName = Utility.GetDisplayName<CustodianIirupItem>;
        }

        public ValueTask<CustodianIirupItem> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.CustodianIirupItems.Include(i => i.CustodianIIRUP).Where(w => w.Id == id).SingleOrDefaultAsync();
            return data;
        });

        public IQueryable<CustodianIirupItem> GetAllByCustodianIirupId(Guid? custodianIirupId) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.CustodianIirupItems.AsNoTracking()
                .Include(i => i.CustodianDisposal)
                .Where(w => w.CustodianIirupId == custodianIirupId).AsQueryable();
            return data;
        });

        public ValueTask<CustodianIirupItem> SaveAsync(CustodianIirupItem model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var custodianIirup = await _db.CustodianIIRUPs.FindAsync(model.CustodianIirupId);
            ValidateIfPosted(custodianIirup);

            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var gridItems = model.GridItems.Split(',');
            if (gridItems.Count() == 0)
            {
                throw new RecordNotFoundException(string.Format("No selected items, cannot generate."));
            }

            foreach (var gridItem in gridItems)
            {
                model.Id = Guid.NewGuid();
                model.CustodianDisposalId = Guid.Parse(gridItem);
                var entity = new CustodianIirupItem();
                MapModelToEntityFields(entity, model, Mode.ADD);

                _db.CustodianIirupItems.Add(entity);
                await _db.SaveChangesAsync();

                // process disposal Items
                await _custodianDisposalItemService.ProcessForIirupAsync(model.CustodianDisposalId, user, date);                
            }

            return model;
        });


        public ValueTask<CustodianIirupItem> DeleteAsync(CustodianIirupItem model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            var custodianIirup = await _db.CustodianIIRUPs.FindAsync(model.CustodianIirupId);
            ValidateIfPosted(custodianIirup);

            var entity = await GetByIdAsync(model.Id);
            ValidateRecord(entity);
            
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.CustodianIirupItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.CustodianIirupItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public void MapModelToEntityFields(CustodianIirupItem entity, CustodianIirupItem model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }
            entity.CustodianIirupId = model.CustodianIirupId;
            entity.CustodianDisposalId = model.CustodianDisposalId;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        private void ValidateFields(CustodianIirupItem model)
        {
            _imex.ThrowIfContainsErrors();
        }

        private void ValidateIfNull(CustodianIirupItem model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(CustodianIirupItem entity)
        {
            if (entity == null)
            {
                throw new NotFoundException(entity.Id);
            }
        }

        private void ValidateIfPosted(CustodianIIRUP entity)
        {
            if (entity.PostedDt != null)
            {
                var msg = $"Record already posted by {entity.PostedBy} on {entity.PostedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        private void ValidateIfNotPosted(CustodianIirupItem entity)
        {
            if (entity.CustodianDisposal.PostedDt == null)
            {
                throw new RecordNotYetPostedException($"Record is not yet posted!");
            }
        }
    }
}