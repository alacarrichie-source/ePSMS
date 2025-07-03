using iLgs.Exceptions;
using iLgs.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.PropertyCard
{
    public interface IPsCardItemTransactionService
    {
        IQueryable<PsCardItemTransaction> GetAllByPsCardItemExtnId(Guid? psCardItemExtnId);
        ValueTask<PsCardItemTransaction> GetByIdAsync(Guid? id);
        ValueTask<bool> IsSelectedIssuanceAsync(Guid? psCardItemExtnId, Guid? refId, string remarks);
        ValueTask<PsCardItemTransaction> LogUpdates(Guid? psCardItemExtnId, Guid? refId, string remarks, string user, DateTime date);
        ValueTask<PsCardItemTransaction> CreateAsync(PsCardItemTransaction model, string user, DateTime date);
        ValueTask<PsCardItemTransaction> UpdateAsync(PsCardItemTransaction model, string user, DateTime date);
        ValueTask<PsCardItemTransaction> DeleteAsync(PsCardItemTransaction model, string user, DateTime date);
    }

    public class PsCardItemTransactionService : IPsCardItemTransactionService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<PsCardItemTransaction> _exceptionService;

        public PsCardItemTransactionService(AppManEntities db,
            ICreateAndLogExceptions exceptions,
            IExceptionService<PsCardItemTransaction> exceptionService)
        {
            _db = db;
            _exceptions = exceptions;
            _exceptionService = exceptionService;
        }

        public IQueryable<PsCardItemTransaction> GetAllByPsCardItemExtnId(Guid? psCardItemExtnId) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.PsCardItemTransactions.Where(w => w.PsCardItemExtnId == psCardItemExtnId).AsNoTracking();
            return data;
        });

        public async ValueTask<bool> IsSelectedIssuanceAsync(Guid? psCardItemExtnId, Guid? refId, string remarks)
        {
            bool isSelected = false;
            if (remarks == "CARD") {
                isSelected = await _db.PsCardItemTransactions.AnyAsync(a => a.PsCardItemExtnId == psCardItemExtnId && a.PsCardItemId == refId);
            }
            else if (remarks == "TRANSIT")
            {
                isSelected = await _db.PsCardItemTransactions.AnyAsync(a => a.PsCardItemExtnId == psCardItemExtnId && a.PsCardItemTransferId == refId);
            }
            else if (remarks == "ISSUANCE" || remarks == "TRANSFER")
            {
                isSelected = await _db.PsCardItemTransactions.AnyAsync(a => a.PsCardItemExtnId == psCardItemExtnId && a.PsCardItemIssuanceId == refId);
            }
            else if (remarks == "PAR" || remarks == "ICS")
            {
                isSelected = await _db.PsCardItemTransactions.AnyAsync(a => a.PsCardItemExtnId == psCardItemExtnId && a.IcsParId == refId);
            }
            return isSelected;
        }

        public async ValueTask<PsCardItemTransaction> GetByIdAsync(Guid? id)
        {
            var data = await _db.PsCardItemTransactions
                //.Include(i => i.PsCardItem)
                //.Include(i => i.PsCardItemExtn)
                //.Include(i => i.PsCardItemIssuance)
                //.Include(i => i.IcsPar)
                .Where(w => w.Id == id)
                .FirstOrDefaultAsync();
            return data;
        }

        public ValueTask<PsCardItemTransaction> LogUpdates(Guid? psCardItemExtnId, Guid? refId, string remarks, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            PsCardItemTransaction entity = null;
            Guid? psCardItemId = null;
            Guid? psCardItemTransferId = null;
            Guid? psCardItemIssuanceId = null;
            Guid? icsParId = null;

            if (remarks == "CARD" || remarks == "TRANSIT")
            {
                entity = await _db.PsCardItemTransactions
                    .Where(w => w.PsCardItemExtnId == psCardItemExtnId && w.PsCardItemId == refId)
                    .SingleOrDefaultAsync();
                psCardItemId = refId;
            }
            //else if (remarks == "TRANSIT")
            //{
            //    entity = await _db.PsCardItemTransactions
            //        .Where(w => w.PsCardItemExtnId == psCardItemExtnId && w.PsCardItemTransferId == refId)
            //        .SingleOrDefaultAsync();
            //    psCardItemTransferId = refId;
            //}
            else if (remarks == "ISSUANCE" || remarks == "TRANSFER")
            {
                entity = await _db.PsCardItemTransactions
                    .Where(w => w.PsCardItemExtnId == psCardItemExtnId && w.PsCardItemIssuanceId == refId)
                    .SingleOrDefaultAsync();
                psCardItemIssuanceId = refId;
            }
            else if (remarks == "PAR" || remarks == "ICS")
            {
                entity = await _db.PsCardItemTransactions
                    .Where(w => w.PsCardItemExtnId == psCardItemExtnId && w.IcsParId == refId)
                    .SingleOrDefaultAsync();
                icsParId = refId;
            }
            else
            {
                throw new InvalidValueException($"Value for Remarks [{remarks}] is unknown");
            }

            if (entity == null)
            {
                entity = new PsCardItemTransaction
                {
                    Id = Guid.NewGuid(),
                    PsCardItemExtnId = psCardItemExtnId,
                    PsCardItemTransferId = psCardItemTransferId,
                    PsCardItemId = psCardItemId,
                    PsCardItemIssuanceId = psCardItemIssuanceId,
                    IcsParId = icsParId,
                    Remarks = remarks,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };                

                _db.PsCardItemTransactions.Add(entity);                
            }
            else
            {
                entity.UpdatedBy = user;
                entity.UpdatedDt = date;

                _db.PsCardItemTransactions.Attach(entity);
                _db.Entry(entity).State = EntityState.Modified;
            }

            await _db.SaveChangesAsync();
            return entity;
        });
        
        public ValueTask<PsCardItemTransaction> CreateAsync(PsCardItemTransaction model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new PsCardItemTransaction();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.PsCardItemTransactions.Add(entity);
            await _db.SaveChangesAsync();
            return model;
        });

        public ValueTask<PsCardItemTransaction> UpdateAsync(PsCardItemTransaction model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await GetByIdAsync(model.Id);

            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            MapModelToEntityFields(entity, model, Mode.EDIT);

            _db.PsCardItemTransactions.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<PsCardItemTransaction> DeleteAsync(PsCardItemTransaction model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            PsCardItemTransaction entity = await _db.PsCardItemTransactions.FindAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItemTransactions.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCardItemTransactions.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public void MapModelToEntityFields(PsCardItemTransaction entity, PsCardItemTransaction model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            entity.PsCardItemExtnId = model.PsCardItemExtnId;
            entity.PsCardItemTransferId = model.PsCardItemTransferId;
            entity.PsCardItemId = model.PsCardItemId;
            entity.PsCardItemIssuanceId = model.PsCardItemIssuanceId;
            entity.IcsParId = model.IcsParId;
            entity.Remarks = model.Remarks;
            entity.TransDate = model.TransDate;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }
    }
}