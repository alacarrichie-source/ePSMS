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
    public interface IAirItemService
    {
        IQueryable<AIRItemVM> GetByAirId(Guid? airId);
        ValueTask<AIRItemVM> GetByIdAsync(Guid? id);

        ValueTask<AIRItemVM> CreateAsync(AIRItemVM model, string user, DateTime date);
        ValueTask<AIRItemVM> UpdateAsync(AIRItemVM model, string user, DateTime date);
        ValueTask<AIRItemVM> DeleteAsync(AIRItemVM model, string user, DateTime date);
    }

    public class AirItemService : IAirItemService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly IExceptionService<AIRItemVM> _vmExceptionService = new ExceptionService<AIRItemVM>();

        public AirItemService(AppManEntities db)
        {
            this._db = db;
        }

        public IQueryable<AIRItemVM> GetByAirId(Guid? airId)
        {
            var data = _db.AIRItems.Where(w => w.AirId == airId)
                .Select(s => new AIRItemVM
                {
                    Id = s.Id,
                    AirId = s.AirId,
                    OrderItemId = s.OrderItemId,
                    PsType = s.OrderItem.RequestItem.RisItem.ItemCode.ItemType.Code,
                    PsNo = s.OrderItem.RequestItem.RisItem.PsNo,
                    PsItem = s.OrderItem.RequestItem.RisItem.ItemName,
                    OrderDescription = s.OrderItem.RequestItem.RisItem.Description,
                    PsUnit = s.OrderItem.RequestItem.RisItem.Unit,
                    Qty = s.Qty,
                    Remarks = s.Remarks,
                    InsertedDt = s.InsertedDt
                });
            return data;
        }

        public ValueTask<AIRItemVM> GetByIdAsync(Guid? id) => _vmExceptionService.TryCatchAsync(async () =>
        {
            var data = await _db.AIRItems.Where(w => w.Id == id)
                .Select(s => new AIRItemVM
                {
                    Id = s.Id,
                    AirId = s.AirId,
                    OrderItemId = s.OrderItemId,
                    PsType = s.OrderItem.RequestItem.RisItem.ItemCode.ItemType.Code,
                    PsNo = s.OrderItem.RequestItem.RisItem.PsNo,
                    PsItem = s.OrderItem.RequestItem.RisItem.ItemName,
                    OrderDescription = s.OrderItem.RequestItem.RisItem.Description,
                    PsUnit = s.OrderItem.RequestItem.RisItem.Unit,
                    Qty = s.Qty,
                    Remarks = s.Remarks,
                    InsertedDt = s.InsertedDt
                }).FirstOrDefaultAsync();
            return data;
        });

        public ValueTask<AIRItemVM> CreateAsync(AIRItemVM model, string user, DateTime date) => _vmExceptionService.TryCatchAsync(async () =>
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            model.Id = Guid.NewGuid();

            AIRItem entity = new AIRItem()
            {
                Id = model.Id,
                AirId = model.AirId,
                OrderItemId = model.OrderItemId,
                Qty = model.Qty,
                Remarks = model.Remarks,
                InsertedBy = user,
                InsertedDt = date,
                UpdatedBy = user,
                UpdatedDt = date
            };

            _db.AIRItems.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<AIRItemVM> DeleteAsync(AIRItemVM model, string user, DateTime date) => _vmExceptionService.TryCatchAsync(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            AIRItem entity = await _db.AIRItems.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.AIRItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.AIRItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });                

        public ValueTask<AIRItemVM> UpdateAsync(AIRItemVM model, string user, DateTime date) => _vmExceptionService.TryCatchAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(model.Remarks))
            {
                throw new InvalidValueException("Remarks Field is Required!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            AIRItem entity = await _db.AIRItems.FindAsync(model.Id);

            entity.AirId = model.AirId;
            entity.OrderItemId = model.OrderItemId;
            entity.Qty = model.Qty;
            entity.Remarks = model.Remarks;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.AIRItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });
    }
}