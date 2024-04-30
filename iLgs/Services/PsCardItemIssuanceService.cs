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
    public interface IPsCardItemIssuanceService
    {
        IQueryable<PsCardItemIssuanceVM> GetByCardItemId(Guid? cardItemId);
        ValueTask<PsCardItemIssuanceVM> GetByIdAsync(Guid? id);

        ValueTask<PsCardItemIssuanceVM> CreateAsync(PsCardItemIssuanceVM model, string user, DateTime date);
        ValueTask<PsCardItemIssuanceVM> UpdateAsync(PsCardItemIssuanceVM model, string user, DateTime date);
        ValueTask<PsCardItemIssuanceVM> DeleteAsync(PsCardItemIssuanceVM model, string user, DateTime date);
    }

    public class PsCardItemIssuanceService : IPsCardItemIssuanceService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly IExceptionService<PsCardItemIssuanceVM> _VmExceptionService = new ExceptionService<PsCardItemIssuanceVM>();

        public PsCardItemIssuanceService(AppManEntities db)
        {
            _db = db;
        }

        public ValueTask<PsCardItemIssuanceVM> GetByIdAsync(Guid? id) => _VmExceptionService.TryCatch(async () =>
        {
            var data = await _db.PsCardItemIssuances.Where(w => w.Id == id)
                .Select(s => new PsCardItemIssuanceVM
                {
                    Id = s.Id,
                    PsCardItemId = s.PsCardItemId,
                    RefIssuedId = s.RefIssuedId,
                    Location = s.Location,
                    Officer = s.Officer,
                    IssuedTo = s.IssuedTo,
                    IssuedDate = s.IssuedDate,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    InsertedDt = s.InsertedDt
                }).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<PsCardItemIssuanceVM> GetByCardItemId(Guid? PsCardItemId) => _VmExceptionService.TryCatch(() =>
        {
            var data = _db.PsCardItemIssuances.Where(w => w.PsCardItemId == PsCardItemId)
                .Select(s => new PsCardItemIssuanceVM
                {
                    Id = s.Id,
                    PsCardItemId = s.PsCardItemId,
                    RefIssuedId = s.RefIssuedId,
                    Location = s.Location,
                    Officer = s.Officer,
                    IssuedTo = s.IssuedTo,
                    IssuedDate = s.IssuedDate,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    InsertedDt = s.InsertedDt
                });
            return data;
        });


        public ValueTask<PsCardItemIssuanceVM> CreateAsync(PsCardItemIssuanceVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var cardItem = await _db.PsCardItems.FindAsync(model.PsCardItemId);

            var entity = new PsCardItemIssuance
            {
                Id = model.Id,
                PsCardItemId = model.PsCardItemId,
                RefIssuedId = model.RefIssuedId,
                Location = model.Location,
                Officer = model.Officer,
                IssuedTo = model.IssuedTo,
                IssuedDate = model.IssuedDate,
                Qty = model.Qty,
                Amount = cardItem.UnitCost * model.Qty,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.PsCardItemIssuances.Add(entity);
            await _db.SaveChangesAsync();

            await UpdatePsItems(model.PsCardItemId, user, date);

            return model;
        });        

        public ValueTask<PsCardItemIssuanceVM> UpdateAsync(PsCardItemIssuanceVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItemIssuances.Include(i => i.PsCardItem).Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            entity.PsCardItemId = model.PsCardItemId;
            entity.RefIssuedId = model.RefIssuedId;
            entity.Location = model.Location;
            entity.Officer = model.Officer;
            entity.IssuedTo = model.IssuedTo;
            entity.IssuedDate = model.IssuedDate;
            entity.Qty = model.Qty;
            entity.Amount = entity.PsCardItem.UnitCost * model.Qty;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItemIssuances.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            await UpdatePsItems(model.PsCardItemId, user, date);

            return model;
        });

        public ValueTask<PsCardItemIssuanceVM> DeleteAsync(PsCardItemIssuanceVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItemIssuances.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItemIssuances.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCardItemIssuances.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            await UpdatePsItems(model.PsCardItemId, user, date);

            return model;
        });

        public async Task UpdatePsItems(Guid? cardItemId, string user, DateTime date)
        {
            var entity = await _db.PsCardItems.Include(i => i.PsCardItemIssuances).Where(w => w.Id == cardItemId).FirstOrDefaultAsync();
            var qtyIss = entity.PsCardItemIssuances.Sum(s => s.Qty);

            entity.QtyIss = qtyIss;
            entity.QtyBal = entity.Qty - qtyIss;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PsCardItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }

    }
}