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
    public interface IOrderItemUnitGroupDescriptionItemService
    {
        IQueryable<OrderItemUnitGroupDescriptionItemVM> GetByUnitGroupDescriptionId(Guid? unitGroupDescriptionId);
        ValueTask<OrderItemUnitGroupDescriptionItem> GetByIdAsync(Guid? id);
        //IQueryable<OrderItemUnitGroupDescriptionItemVM> GetAvailableUnitGroupItem(Guid? risId);
        //ValueTask<OrderItemUnitGroupDescriptionItemVM> CreateAsync(RequestItemUnitGroupDescriptionItemVM model, string user, DateTime date);
        ValueTask<OrderItemUnitGroupDescriptionItemVM> UpdateAsync(OrderItemUnitGroupDescriptionItemVM model, string user, DateTime date);
        ValueTask<OrderItemUnitGroupDescriptionItemVM> DeleteAsync(OrderItemUnitGroupDescriptionItemVM model, string user, DateTime date);
    }

    public class OrderItemUnitGroupDescriptionItemService : IOrderItemUnitGroupDescriptionItemService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions _exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<OrderItemUnitGroupDescriptionItemVM> _vmExceptionService = new ExceptionService<OrderItemUnitGroupDescriptionItemVM>();
        //private readonly IExceptionService<RisItemUnitGroupAvailableVM> _vmUnitGroupAvailableExceptionService = new ExceptionService<RisItemUnitGroupAvailableVM>();
        private readonly IExceptionService<OrderItemUnitGroupDescriptionItem> _exceptionService = new ExceptionService<OrderItemUnitGroupDescriptionItem>();

        public OrderItemUnitGroupDescriptionItemService(AppManEntities db)
        {
            _db = db;
        }

        public ValueTask<OrderItemUnitGroupDescriptionItem> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatchAsync(async () =>
        {
            var data = await _db.OrderItemUnitGroupDescriptionItems.FindAsync(id);
            return data;
        });

        public IQueryable<OrderItemUnitGroupDescriptionItemVM> GetByUnitGroupDescriptionId(Guid? unitGroupDescriptionId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.OrderItemUnitGroupDescriptionItems.AsNoTracking()
                .Where(w => w.OrderItemUnitGroupDescriptionId == unitGroupDescriptionId)
                .Select(s => new OrderItemUnitGroupDescriptionItemVM
                {
                    Id = s.Id,
                    OrderItemUnitGroupDescriptionId = s.OrderItemUnitGroupDescriptionId,
                    OrderItemId = s.OrderItemId,
                    RequestItemUnitGroupDescriptionItemId = s.RequestItemUnitGroupDescriptionItemId,
                    RequestItemUnitGroupDescriptionItem = s.RequestItemUnitGroupDescriptionItem,
                    PsNo = s.RequestItemUnitGroupDescriptionItem.RisItemUnitGroupDescriptionItem.RisItem.PsNo,
                    ItemName = s.RequestItemUnitGroupDescriptionItem.RisItemUnitGroupDescriptionItem.RisItem.ItemName,
                    Description = s.RequestItemUnitGroupDescriptionItem.RisItemUnitGroupDescriptionItem.RisItem.Description,
                    Unit = s.RequestItemUnitGroupDescriptionItem.RisItemUnitGroupDescriptionItem.RisItem.Unit,
                    QtyRequest = s.RequestItemUnitGroupDescriptionItem.RisItemUnitGroupDescriptionItem.RisItem.QtyRequest,
                    PriceRate = s.OrderItem.PriceRate,
                    UnitCost = s.OrderItem.UnitCost,
                    TotalCost = s.OrderItem.Amount,
                    GroupCost = s.RequestItemUnitGroupDescriptionItem.RequestItemUnitGroupDescription.RequestItemUnitGroup.TotalCost,
                    InsertedDt = s.InsertedDt
                });
            return data;
        });

        //public IQueryable<RisItemUnitGroupAvailableVM> GetAvailableUnitGroupItem(Guid? risId) =>
        //_vmUnitGroupAvailableExceptionService.TryCatch(() =>
        //{
        //    var data = _db.RisItems.Where(w => w.RisId == risId && !w.RisItemUnitGroupDescriptionItems.Any(a => a.RisItemId == w.Id))
        //    .Select(s => new RisItemUnitGroupAvailableVM
        //    {
        //        Id = s.Id,
        //        PsNo = s.PsNo,
        //        ItemName = s.ItemName,
        //        Description = s.Description,
        //        Unit = s.Unit,
        //        QtyRequest = s.QtyRequest,
        //        InsertedDt = s.InsertedDt
        //    });
        //    return data;
        //});

        //public ValueTask<RequestItemUnitGroupDescriptionItemVM> CreateAsync(RequestItemUnitGroupDescriptionItemVM model, string user, DateTime date) =>
        //_vmExceptionService.TryCatchAsync(async () =>
        //{
        //    if (string.IsNullOrWhiteSpace(model.GridItems))
        //    {
        //        throw new InvalidValueException("No selected items, cannot continue!");
        //    }

        //    model.InsertedBy = user;
        //    model.UpdatedBy = user;
        //    model.InsertedDt = date;
        //    model.UpdatedDt = date;

        //    var selectedItems = model.GridItems.Split(',');
        //    foreach (var item in selectedItems)
        //    {
        //        model.Id = Guid.NewGuid();
        //        RisItemUnitGroupDescriptionItem entity = new RisItemUnitGroupDescriptionItem()
        //        {
        //            Id = model.Id,
        //            UnitGroupDescriptionId = model.UnitGroupDescriptionId,
        //            RisItemId = Guid.Parse(item),
        //            InsertedBy = model.InsertedBy,
        //            InsertedDt = model.InsertedDt,
        //            UpdatedBy = model.UpdatedBy,
        //            UpdatedDt = model.UpdatedDt
        //        };

        //        _db.RisItemUnitGroupDescriptionItems.Add(entity);
        //    }

        //    await _db.SaveChangesAsync();
        //    return model;
        //});

        public ValueTask<OrderItemUnitGroupDescriptionItemVM> DeleteAsync(OrderItemUnitGroupDescriptionItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.OrderItemUnitGroupDescriptionItems.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.OrderItemUnitGroupDescriptionItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.OrderItemUnitGroupDescriptionItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<OrderItemUnitGroupDescriptionItemVM> UpdateAsync(OrderItemUnitGroupDescriptionItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.OrderItemUnitGroupDescriptionItems.FindAsync(model.Id);

            entity.OrderItemUnitGroupDescriptionId = model.OrderItemUnitGroupDescriptionId;
            entity.OrderItemId = model.OrderItemId;
            entity.RequestItemUnitGroupDescriptionItemId = model.RequestItemUnitGroupDescriptionItemId;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.OrderItemUnitGroupDescriptionItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return model;
        });
    }
}