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
    public interface IIcsParItemService
    {
        IQueryable<IcsParItem> GetAllByIcsParId(Guid? icsParId);
        ValueTask<IcsParItem> GetByIdAsync(Guid? id);
        IQueryable<IcsParItem> GetAllParItems(Guid? psCardItemId);
        IQueryable<IcsParItem> GetAllIcsItems(Guid? psCardItemId);
        ValueTask<IcsParItem> CreateAsync(IcsParItem model, string user, DateTime date);
        ValueTask<IcsParItem> UpdateAsync(IcsParItem model, string user, DateTime date);
        ValueTask<IcsParItem> DeleteAsync(IcsParItem model, string user, DateTime date);        
    }

    public class IcsParItemService : IIcsParItemService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions _exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<IcsParItem> _exceptionService = new ExceptionService<IcsParItem>();        

        public IcsParItemService(AppManEntities db)
        {
            _db = db;
        }

        public IQueryable<IcsParItem> GetAllByIcsParId(Guid? icsParId) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.IcsParItems.Where(w => w.IcsParId == icsParId).AsNoTracking();
            return data;
        });

        public async ValueTask<IcsParItem> GetByIdAsync(Guid? id)         
        {
            var data = await _db.IcsParItems.Include(i => i.IcsPar)
                    .Include(i => i.PsCardItemExtn.PsCardItem) // Ensure related entities are included
                    .Include(i => i.PsCardItemExtn.Codextn)    // Ensure Codextn is included for Location description
                    .Include(i => i.PsCardItemExtn.PsCardItemExtnBuilding)
                    .Include(i => i.PsCardItemExtn.PsCardItemExtnLand)
                    .Include(i => i.PsCardItemExtn.PsCardItemExtnOther)
                    .Include(i => i.PsCardItemExtn.PsCardItemExtnVehicle)                    
                .Where(w => w.Id == id)
                .FirstOrDefaultAsync();
            return data;
        }

        public IQueryable<IcsParItem> GetAllParItems(Guid? psCardItemId) =>
        _exceptionService.TryCatch(() =>
        {
            var data = GetAllIcsParItems(psCardItemId, "P");
            return data;
        });

        public IQueryable<IcsParItem> GetAllIcsItems(Guid? psCardItemId) =>
        _exceptionService.TryCatch(() =>
        {
            var data = GetAllIcsParItems(psCardItemId, "I");
            return data;
        });

        private IQueryable<IcsParItem> GetAllIcsParItems(Guid? psCardItemId, string refType)         
        {
            var data = _db.IcsParItems
                .Include(i => i.IcsPar)
                .Include(i => i.PsCardItemExtn)
                .Where(w => w.PsCardItemExtn.PsCardItemId == psCardItemId && w.IcsPar.RefType == refType).AsNoTracking();
                
            return data;
        }


        public ValueTask<IcsParItem> CreateAsync(IcsParItem model, string user, DateTime date) =>
        _exceptionService.TryCatchAsync(async () =>
        {

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            IcsParItem entity = new IcsParItem()
            {
                Id = model.Id,
                IcsParId = model.IcsParId,
                PsCardItemExtnId = model.PsCardItemExtnId,
                Qty = model.Qty,
                Amount = model.Amount,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.IcsParItems.Add(entity);
            await _db.SaveChangesAsync();
            return model;
        });

        public ValueTask<IcsParItem> UpdateAsync(IcsParItem model, string user, DateTime date) =>
        _exceptionService.TryCatchAsync(async () =>
        {
            var entity = await GetByIdAsync(model.Id);

            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.IcsPar.ReceivedBy = model.IcsPar.ReceivedBy;
            entity.IcsPar.ReceivedDate = model.IcsPar.ReceivedDate;
            entity.IcsPar.ReceivedDept = model.IcsPar.ReceivedDept;
            entity.IcsPar.ReceivedByPosition = model.IcsPar.ReceivedByPosition;
            entity.IcsPar.IssuedBy = model.IcsPar.IssuedBy;
            entity.IcsPar.IssuedDate = model.IcsPar.IssuedDate;
            entity.IcsPar.IssuedDept = model.IcsPar.IssuedDept;
            entity.IcsPar.IssuedByPosition = model.IcsPar.IssuedByPosition;

            entity.IcsPar.UpdatedBy = user;
            entity.IcsPar.UpdatedDt = date;
            
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.IcsParItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<IcsParItem> DeleteAsync(IcsParItem model, string user, DateTime date) =>
        _exceptionService.TryCatchAsync(async () =>
        {
            IcsParItem entity = await _db.IcsParItems.FindAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            var psCardItemExtnId = entity.PsCardItemExtnId;

            model.UpdatedBy = user;
            model.UpdatedDt = date;
            
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.IcsParItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.IcsParItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            PsCardItemExtn psCardItemExtn = await _db.PsCardItemExtns.FindAsync(psCardItemExtnId);
            if (psCardItemExtn != null)
            {
                psCardItemExtn.UpdatedBy = user;
                psCardItemExtn.UpdatedDt = date;

                psCardItemExtn.UpdatedBy = model.UpdatedBy;
                psCardItemExtn.UpdatedDt = model.UpdatedDt;

                _db.PsCardItemExtns.Attach(psCardItemExtn);
                _db.Entry(psCardItemExtn).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                _db.PsCardItemExtns.Remove(psCardItemExtn);
                _db.Entry(psCardItemExtn).State = EntityState.Deleted;
                await _db.SaveChangesAsync();
            }

            // remove master record if no child record exists
            if (!(await _db.IcsParItems.AnyAsync(a => a.IcsParId == model.IcsParId)))
            {
                var icsPar = await _db.IcsPars.FindAsync(model.IcsParId);
                if (icsPar != null)
                {
                    icsPar.UpdatedBy = user;
                    icsPar.UpdatedDt = date;

                    _db.IcsPars.Attach(icsPar);
                    _db.Entry(icsPar).State = EntityState.Modified;
                    await _db.SaveChangesAsync();

                    _db.IcsPars.Remove(icsPar);
                    _db.Entry(icsPar).State = EntityState.Deleted;
                    await _db.SaveChangesAsync();
                }
            }

            return model;
        });

        
    }
}