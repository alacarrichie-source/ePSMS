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
    public class RisIssuedService : IRisIssuedService
    {
        private readonly AppManEntities db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RisIssuedVM> _vmExceptionService = new ExceptionService<RisIssuedVM>();
        private readonly IExceptionService<RisIssued> _exceptionService = new ExceptionService<RisIssued>();

        public RisIssuedService(AppManEntities db)
        {
            this.db = db;
        }

        public ValueTask<RisIssuedVM> GetVmByIdAsync(Guid? id) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            var data = await db.RisIssueds.Where(w => w.Id == id)
                .Select(s => new RisIssuedVM
                {
                    Id = s.Id,
                    RisItemId = s.RisItemId,
                    IssuedDate = s.IssuedDate,
                    IssuedBy = s.IssuedBy,
                    IssuedByDesignation = s.IssuedByDesignation,
                    Qty = s.Qty,
                    UnitCost = s.UnitCost,
                    Amount = s.Amount,
                    ReceivedBy = s.ReceivedBy,
                    ReceivedByDesignation = s.ReceivedByDesignation, 
                    ReceivedDate = s.ReceivedDate,
                    InsertedDt = s.InsertedDt,
                    Department = s.RisItem.RISs.Office
                }).FirstOrDefaultAsync();
            return data;
        });

        public ValueTask<RisIssued> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatchAsync(async () =>
        {
            var data = await db.RisIssueds.FindAsync(id);
            return data;
        });

        public IQueryable<RisIssuedVM> GetByRisItemId(Guid? risItemId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = db.RisIssueds.Where(w => w.RisItemId == risItemId)
                .Select(s => new RisIssuedVM
                {
                    Id = s.Id,
                    RisItemId = s.RisItemId,
                    IssuedDate = s.IssuedDate,
                    IssuedBy = s.IssuedBy,
                    IssuedByDesignation = s.IssuedByDesignation,
                    Qty = s.Qty,
                    UnitCost = s.UnitCost,
                    Amount = s.Amount,
                    ReceivedBy = s.ReceivedBy,
                    ReceivedByDesignation = s.ReceivedByDesignation,
                    ReceivedDate = s.ReceivedDate,
                    InsertedDt = s.InsertedDt,
                    Department = s.RisItem.RISs.Office
                });
            return data;
        });

        public IQueryable<RisIssuedVM> GetByPoNoStockNo(string poNo, string stockNo) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = db.RisIssueds.Where(w => w.RisItem.RequestItems.Any(a => a.OrderItems.Any(o => o.Order.PoNo == poNo && o.StockNo.Contains(stockNo))))
                .Select(s => new RisIssuedVM
                {
                    Id = s.Id,
                    RisItemId = s.RisItemId,
                    IssuedDate = s.IssuedDate,
                    IssuedBy = s.IssuedBy,
                    IssuedByDesignation = s.IssuedByDesignation,
                    Qty = s.Qty,
                    UnitCost = s.UnitCost,
                    Amount = s.Amount,
                    ReceivedBy = s.ReceivedBy,
                    ReceivedByDesignation = s.ReceivedByDesignation,
                    ReceivedDate = s.ReceivedDate,
                    InsertedDt = s.InsertedDt,
                    Department = s.RisItem.RISs.Office
                });
            return data;
        });

        public ValueTask<RisIssuedVM> CreateAsync(RisIssuedVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            var totalQtyIssued = db.RisItems.Find(model.RisItemId)?.QtyIssue ?? 0;
            var qtyIssued = db.RisIssueds.Where(w => w.RisItemId == model.RisItemId).Sum(s => s.Qty) ?? 0;
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

            RisIssued entity = new RisIssued()
            {
                Id = model.Id,
                RisItemId = model.RisItemId,
                IssuedDate = model.IssuedDate,
                IssuedBy = model.IssuedBy,
                IssuedByDesignation = model.IssuedByDesignation,
                Qty = model.Qty,
                UnitCost = model.UnitCost,
                Amount = model.Amount,
                ReceivedBy = model.ReceivedBy,
                ReceivedByDesignation = model.ReceivedByDesignation,
                ReceivedDate = model.ReceivedDate,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            db.RisIssueds.Add(entity);
            await db.SaveChangesAsync();
            await UpdatePsItems(model.RisItemId);
            return model;
        });

        public ValueTask<RisIssuedVM> DeleteAsync(RisIssuedVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RisIssued entity = await db.RisIssueds.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.RisIssueds.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.RisIssueds.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();
            await UpdatePsItems(model.RisItemId);
            return model;
        });

        public ValueTask<RisIssuedVM> UpdateAsync(RisIssuedVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            var totalQtyIssued = db.RisItems.Find(model.RisItemId)?.QtyIssue ?? 0;
            var qtyIssued = db.RisIssueds.Where(w => w.RisItemId == model.RisItemId && w.Id != model.Id).Sum(s => s.Qty) ?? 0;
            var qtyBalance = totalQtyIssued - qtyIssued;
            if (model.Qty > qtyBalance)
            {
                throw new InvalidValueException(string.Format("Quantity must not exceed the remaing balance of {0}", qtyBalance));
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RisIssued entity = await db.RisIssueds.FindAsync(model.Id);

            entity.RisItemId = model.RisItemId;
            entity.IssuedDate = model.IssuedDate;
            entity.IssuedBy = model.IssuedBy;
            entity.IssuedByDesignation = model.IssuedByDesignation;
            entity.Qty = model.Qty;
            entity.UnitCost = model.UnitCost;
            entity.Amount = model.Amount;
            entity.ReceivedBy = model.ReceivedBy;
            entity.ReceivedByDesignation = model.ReceivedByDesignation;
            entity.ReceivedDate = model.ReceivedDate;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.RisIssueds.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();                        
            await UpdatePsItems(model.RisItemId);
            return model;
        });

        private async ValueTask UpdatePsItems(Guid? risItemId)
        {
            var qtyReceived = await db.AIRItems.Where(w => w.OrderItem.RequestItem.RisItem.Id == risItemId).SumAsync(s => s.Qty) ?? 0;
            var qtyIssued = await db.RisIssueds.Where(w => w.RisItemId == risItemId).SumAsync(s => s.Qty) ?? 0;
            var orderItems = await db.OrderItems.Where(w => w.RequestItem.RisItem.Id == risItemId).ToListAsync();
            foreach (var orderItem in orderItems)
            {
                var psItems = db.PsItems.Where(w => w.OrderItemId == orderItem.Id);
                await psItems.ForEachAsync(f => { f.QtyIss = qtyIssued; f.Qty = qtyReceived; f.QtyBal = qtyReceived - qtyIssued; });
            }
            await db.SaveChangesAsync();            
        }
    }
}