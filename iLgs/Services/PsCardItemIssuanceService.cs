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
                    IssuedTo = s.IssuedTo,
                    IssuedDate = s.IssuedDate,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    InsertedDt = s.InsertedDt,
                    LocationId = s.LocationId,
                    OfficerId = s.OfficerId,
                    Location = s.Codextn.Description,
                    Officer = s.AccountableOfficer.Name,
                    UnitCost = s.PsCardItem.UnitCost,
                    RefNo = s.RefNo,
                    RefDate = s.RefDate,
                    RefType = s.RefType,
                    PropNo = s.PropNo
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
                    IssuedTo = s.IssuedTo,
                    IssuedDate = s.IssuedDate,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    InsertedDt = s.InsertedDt,
                    LocationId = s.LocationId,
                    OfficerId = s.OfficerId,
                    Location = s.Codextn.Description,
                    Officer = s.AccountableOfficer.Name,
                    UnitCost = s.PsCardItem.UnitCost,
                    RefNo = s.RefNo,
                    RefDate = s.RefDate,
                    RefType = s.RefType,
                    PropNo = s.PropNo
                });
            return data;
        });


        public ValueTask<PsCardItemIssuanceVM> CreateAsync(PsCardItemIssuanceVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            var totalQtyIssued = _db.PsCardItems.Find(model.PsCardItemId)?.Qty ?? 0;
            var qtyIssued = _db.PsCardItemIssuances.Where(w => w.PsCardItemId == model.PsCardItemId).Sum(s => s.Qty) ?? 0;
            var qtyBalance = totalQtyIssued - qtyIssued;
            if (model.Qty > qtyBalance)
            {
                throw new InvalidValueException(string.Format("Quantity must not exceed the remaing balance of {0}", qtyBalance));
            }

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
                IssuedTo = model.IssuedTo,
                IssuedDate = model.IssuedDate,
                Qty = model.Qty,
                Amount = cardItem.UnitCost * model.Qty,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt,
                LocationId = model.LocationId,
                OfficerId = model.OfficerId,
                RefNo = model.RefNo,
                RefDate = model.RefDate,
                RefType = model.RefType,
                PropNo = model.PropNo
            };

            _db.PsCardItemIssuances.Add(entity);
            await _db.SaveChangesAsync();

            await UpdatePsItems(model.PsCardItemId, user, date);

            return model;
        });        

        public ValueTask<PsCardItemIssuanceVM> UpdateAsync(PsCardItemIssuanceVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            var totalQtyIssued = _db.PsCardItems.Find(model.PsCardItemId)?.Qty ?? 0;
            var qtyIssued = _db.PsCardItemIssuances.Where(w => w.PsCardItemId == model.PsCardItemId && w.Id != model.Id).Sum(s => s.Qty) ?? 0;
            var qtyBalance = totalQtyIssued - qtyIssued;
            if (model.Qty > qtyBalance)
            {
                throw new InvalidValueException(string.Format("Quantity must not exceed the remaing balance of {0}", qtyBalance));
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItemIssuances.Include(i => i.PsCardItem).Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            entity.PsCardItemId = model.PsCardItemId;
            entity.RefIssuedId = model.RefIssuedId;
            entity.LocationId = model.LocationId;
            entity.OfficerId = model.OfficerId;
            entity.IssuedTo = model.IssuedTo;
            entity.IssuedDate = model.IssuedDate;
            entity.Qty = model.Qty;
            entity.Amount = entity.PsCardItem.UnitCost * model.Qty;
            entity.RefNo = model.RefNo;
            entity.RefDate = model.RefDate;
            entity.RefType = model.RefType;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
            entity.PropNo = model.PropNo;

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