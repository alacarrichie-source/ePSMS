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
        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions _exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RisIssuedVM> _vmExceptionService = new ExceptionService<RisIssuedVM>();
        private readonly IExceptionService<RisIssued> _exceptionService = new ExceptionService<RisIssued>();
        
        public RisIssuedService(AppManEntities db)
        {
            _db = db;        
        }

        public ValueTask<RisIssuedVM> GetVmByIdAsync(Guid? id) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            var data = await _db.RisIssueds.Where(w => w.Id == id)
                .Select(s => new RisIssuedVM
                {
                    Id = s.Id,
                    RisItemId = s.RisItemId,
                    OrderItemId = s.OrderItemId,
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
            var data = await _db.RisIssueds.FindAsync(id);
            return data;
        });

        public IQueryable<RisIssuedVM> GetByRisItemId(Guid? risItemId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RisIssueds.Where(w => w.RisItemId == risItemId)
                .Select(s => new RisIssuedVM
                {
                    Id = s.Id,
                    RisItemId = s.RisItemId,
                    OrderItemId = s.OrderItemId,
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
            var data = _db.RisIssueds.Where(w => w.RisItem.RequestItems.Any(a => a.OrderItems.Any(o => o.Order.PoNo == poNo && o.StockNo.Contains(stockNo))))
                .Select(s => new RisIssuedVM
                {
                    Id = s.Id,
                    RisItemId = s.RisItemId,
                    OrderItemId = s.OrderItemId,
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

        public IQueryable<RisIssuedVM> GetByStockNo(string stockNo) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RisIssueds.Where(w => w.RisItem.RequestItems.Any(a => a.OrderItems.Any(o => o.StockNo == stockNo)))
                .Select(s => new RisIssuedVM
                {
                    Id = s.Id,
                    RisItemId = s.RisItemId,
                    OrderItemId = s.OrderItemId,
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
            var totalQtyIssued = _db.RisItems.Find(model.RisItemId)?.QtyRequest ?? 0;
            var qtyIssued = _db.RisIssueds.Where(w => w.RisItemId == model.RisItemId).Sum(s => s.Qty) ?? 0;
            var qtyBalance = totalQtyIssued - qtyIssued;
            if (model.Qty > qtyBalance)
            {
                throw new InvalidValueException(string.Format("Quantity must not exceed the remaing balance of {0}", qtyBalance));
            }

            var rsmiDate = _db.RSMIs.Max(m => m.Date);
            if (rsmiDate != null && rsmiDate > model.IssuedDate)
            {
                throw new InvalidValueException(string.Format("Date issued must be after the last RSMI date on {0}", rsmiDate.Value.ToShortDateString()));
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
                OrderItemId = model.OrderItemId,
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

            _db.RisIssueds.Add(entity);
            await _db.SaveChangesAsync();
            await UpdateRisPsItems(model.RisItemId);
            await UpdatePsItemIssuance(entity, user, date);
            return model;
        });

        public ValueTask<RisIssuedVM> DeleteAsync(RisIssuedVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {            
            if (_db.RSMIs.Any(a => a.Date == model.IssuedDate))
            {
                throw new RecordRelationshipException("Date Issued is already in RSMI, Cannot delete!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RisIssued entity = await _db.RisIssueds.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RisIssueds.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.RisIssueds.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();
            await UpdateRisPsItems(model.RisItemId);
            await DeletePsItemIssuance(entity, user, date);
            return model;
        });

        public ValueTask<RisIssuedVM> UpdateAsync(RisIssuedVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            var totalQtyIssued = _db.RisItems.Find(model.RisItemId)?.QtyRequest ?? 0;
            var qtyIssued = _db.RisIssueds.Where(w => w.RisItemId == model.RisItemId && w.Id != model.Id).Sum(s => s.Qty) ?? 0;
            var qtyBalance = totalQtyIssued - qtyIssued;
            if (model.Qty > qtyBalance)
            {
                throw new InvalidValueException(string.Format("Quantity must not exceed the remaing balance of {0}", qtyBalance));
            }

            var rsmiDate = _db.RSMIs.Max(m => m.Date);
            if (rsmiDate != null && rsmiDate >= model.IssuedDate)
            {
                throw new InvalidValueException(string.Format("Date issued must be after the last RSMI date on {0}", rsmiDate.Value.ToShortDateString()));
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RisIssued entity = await _db.RisIssueds.FindAsync(model.Id);

            entity.RisItemId = model.RisItemId;
            entity.OrderItemId = model.OrderItemId;
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

            _db.RisIssueds.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();                        
            await UpdateRisPsItems(model.RisItemId);
            await UpdatePsItemIssuance(entity, user, date);
            return model;
        });

        private async ValueTask UpdateRisPsItems(Guid? risItemId)
        {
            var qtyReceived = await _db.AIRItems.Where(w => w.OrderItem.RequestItem.RisItem.Id == risItemId).SumAsync(s => s.Qty) ?? 0;
            var qtyIssued = await _db.RisIssueds.Where(w => w.RisItemId == risItemId).SumAsync(s => s.Qty) ?? 0;
            var orderItems = await _db.OrderItems.Where(w => w.RequestItem.RisItem.Id == risItemId).ToListAsync();
            foreach (var orderItem in orderItems)
            {
                var psItems = _db.PsItems.Where(w => w.OrderItemId == orderItem.Id);
                await psItems.ForEachAsync(f => { f.QtyIss = qtyIssued; f.Qty = qtyReceived; f.QtyBal = qtyReceived - qtyIssued; });
            }
            var risItem = await _db.RisItems.FindAsync(risItemId);
            risItem.QtyIssue = qtyIssued;
            _db.RisItems.Attach(risItem);
            _db.Entry(risItem).State = EntityState.Modified;

            await _db.SaveChangesAsync();            
        }

        private async ValueTask UpdatePsItemIssuance(RisIssued risIssued, string user, DateTime? date)
        {
            var psItemIssuance = await _db.PsItemIssuances.FindAsync(risIssued.Id);
            if (psItemIssuance == null)
            {
                var psItem = await _db.PsItems.Where(w => w.OrderItemId == risIssued.OrderItemId).FirstOrDefaultAsync();
                psItemIssuance = new PsItemIssuance()
                {
                    Id = Guid.NewGuid(),
                    TranCode = "I",
                    PsItemId = psItem.Id,
                    RisIssuedId = risIssued.Id,
                    IssuedDate = risIssued.ReceivedDate,
                    IssuedTo = risIssued.ReceivedBy,
                    Qty = risIssued.Qty,
                    UnitCost = risIssued.UnitCost,
                    //SourceId = model.SourceId,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };
                _db.PsItemIssuances.Add(psItemIssuance);
                _db.Entry(psItemIssuance).State = EntityState.Added;
            }
            else
            {

                psItemIssuance.IssuedDate = risIssued.ReceivedDate;
                psItemIssuance.IssuedTo = risIssued.ReceivedBy;
                psItemIssuance.Qty = risIssued.Qty;
                psItemIssuance.UnitCost = risIssued.UnitCost;
                //psItemIssuance.SourceId = model.SourceId;
                psItemIssuance.UpdatedBy = user;
                psItemIssuance.UpdatedDt = date;
                _db.PsItemIssuances.Attach(psItemIssuance);
                _db.Entry(psItemIssuance).State = EntityState.Modified;
            }

            await _db.SaveChangesAsync();
        }

        private async ValueTask DeletePsItemIssuance(RisIssued model, string user, DateTime? date)
        {
            var entity = await _db.PsItemIssuances.FindAsync(model.Id);
            if (entity == null)
            {
                return;
            }
            
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsItemIssuances.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsItemIssuances.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();            
        }
    }
}