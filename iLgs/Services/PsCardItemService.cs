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
    public interface IPsCardItemService
    {
        IQueryable<PsCardItemVM> GetByCardId(Guid? cardId);
        ValueTask<PsCardItemVM> GetByIdAsync(Guid? id);

        ValueTask<PsCardItemVM> CreateAsync(PsCardItemVM model, string user, DateTime date);
        ValueTask<PsCardItemVM> UpdateAsync(PsCardItemVM model, string user, DateTime date);
        ValueTask<PsCardItemVM> DeleteAsync(PsCardItemVM model, string user, DateTime date);
    }

    public class PsCardItemService : IPsCardItemService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly IExceptionService<PsCardItemVM> _VmExceptionService = new ExceptionService<PsCardItemVM>();

        public PsCardItemService(AppManEntities db)
        {
            _db = db;
        }

        public ValueTask<PsCardItemVM> GetByIdAsync(Guid? id) => _VmExceptionService.TryCatch(async () =>
        {
            var data = await _db.PsCardItems.Where(w => w.Id == id)
                .Select(s => new PsCardItemVM
                {
                    Id = s.Id,
                    PsCardId = s.PsCardId,
                    OrderItemId = s.OrderItemId,
                    PoNo = s.PoNo,
                    PoDate = s.PoDate,
                    AirDate = s.AirDate,
                    AirNo = s.AirNo,
                    AirIssueDate = s.AirIssueDate,
                    Qty = s.Qty,
                    QtyIss = s.QtyIss,
                    QtyBal = s.QtyBal,
                    TranType = s.TranType,
                    UnitCost = s.UnitCost,
                    Amount = s.Amount,
                    Days = s.Days,
                    Remarks = s.Remarks,
                    InsertedDt = s.InsertedDt
                }).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<PsCardItemVM> GetByCardId(Guid? cardId) => _VmExceptionService.TryCatch(() =>
        {
            var data = _db.PsCardItems.Where(w => w.PsCardId == cardId)
                .Select(s => new PsCardItemVM
                {
                    Id = s.Id,
                    PsCardId = s.PsCardId,
                    OrderItemId = s.OrderItemId,
                    PoNo = s.PoNo,
                    PoDate = s.PoDate,
                    AirDate = s.AirDate,
                    AirNo = s.AirNo,
                    AirIssueDate = s.AirIssueDate,
                    Qty = s.Qty,
                    QtyIss = s.QtyIss,
                    QtyBal = s.QtyBal,
                    TranType = s.TranType,
                    UnitCost = s.UnitCost,
                    Amount = s.Amount,
                    Days = s.Days,
                    Remarks = s.Remarks,
                    InsertedDt = s.InsertedDt
                });
            return data;
        });


        public ValueTask<PsCardItemVM> CreateAsync(PsCardItemVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new PsCardItem
            {
                Id = model.Id,
                PsCardId = model.PsCardId,
                OrderItemId = model.OrderItemId,
                PoDate = model.PoDate,
                PoNo = model.PoNo,
                AirDate = model.AirDate,
                AirNo = model.AirNo,
                AirIssueDate = model.AirIssueDate,
                Qty = model.Qty,
                QtyIss = model.QtyIss,
                QtyBal = model.QtyBal,
                Days = model.Days,
                TranType = model.TranType,
                UnitCost = model.UnitCost,
                Remarks = model.Remarks,
                Amount = model.Amount,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.PsCardItems.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });        

        public ValueTask<PsCardItemVM> UpdateAsync(PsCardItemVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItems.FindAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            entity.PsCardId = model.PsCardId;
            entity.OrderItemId = model.OrderItemId;
            entity.PoDate = model.PoDate;
            entity.PoNo = model.PoNo;
            entity.AirDate = model.AirDate;
            entity.AirNo = model.AirNo;
            entity.AirIssueDate = model.AirIssueDate;
            entity.Qty = model.Qty;
            entity.QtyIss = model.QtyIss;
            entity.QtyBal = model.QtyBal;
            entity.Days = model.Days;
            entity.TranType = model.TranType;
            entity.UnitCost = model.UnitCost;
            entity.Remarks = model.Remarks;
            entity.Amount = model.Amount;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<PsCardItemVM> DeleteAsync(PsCardItemVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItems.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCardItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

    }
}