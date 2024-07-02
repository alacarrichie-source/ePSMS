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
        ValueTask<OrderItemUnitGroupDescriptionItemVM> CreateAsync(OrderItemUnitGroupDescriptionItemVM model, string user, DateTime date);
        ValueTask<OrderItemUnitGroupDescriptionItemVM> UpdateAsync(OrderItemUnitGroupDescriptionItemVM model, string user, DateTime date);
        ValueTask<OrderItemUnitGroupDescriptionItemVM> DeleteAsync(OrderItemUnitGroupDescriptionItemVM model, string user, DateTime date);
        ValueTask<OrderItemUnitGroupDescriptionItem> DeleteEmptyGroupsAsync(Guid? orderItemId);
    }

    public class OrderItemUnitGroupDescriptionItemService : IOrderItemUnitGroupDescriptionItemService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions _exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<OrderItemUnitGroupDescriptionItemVM> _vmExceptionService = new ExceptionService<OrderItemUnitGroupDescriptionItemVM>();
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
                    GroupCost = s.OrderItemUnitGroupDescription.OrderItemUnitGroup.TotalCost,
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

        public ValueTask<OrderItemUnitGroupDescriptionItemVM> CreateAsync(OrderItemUnitGroupDescriptionItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new OrderItemUnitGroupDescriptionItem()
            {
                Id = model.Id,
                OrderItemUnitGroupDescriptionId = model.OrderItemUnitGroupDescriptionId,
                RequestItemUnitGroupDescriptionItemId = model.RequestItemUnitGroupDescriptionItemId,
                OrderItemId = model.OrderItemId,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.OrderItemUnitGroupDescriptionItems.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<OrderItemUnitGroupDescriptionItemVM> DeleteAsync(OrderItemUnitGroupDescriptionItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            //var entity = await _db.OrderItemUnitGroupDescriptionItems.FindAsync(model.Id);

            //entity.UpdatedBy = model.UpdatedBy;
            //entity.UpdatedDt = model.UpdatedDt;

            //_db.OrderItemUnitGroupDescriptionItems.Attach(entity);
            //_db.Entry(entity).State = EntityState.Modified;
            //await _db.SaveChangesAsync();

            //_db.OrderItemUnitGroupDescriptionItems.Remove(entity);
            //_db.Entry(entity).State = EntityState.Deleted;
            //await _db.SaveChangesAsync();

            IOrderItemService orderItemService = new OrderItemService(_db);
            var orderItemVM = new OrderItemVM()
            {
                Id = (Guid)model.OrderItemId
            };

            await orderItemService.DeleteAsync(orderItemVM, user, date);

            //await DeleteEmptyGroupsAsync(model.OrderItemId);

            return model;
        });

        public ValueTask<OrderItemUnitGroupDescriptionItem> DeleteEmptyGroupsAsync(Guid? orderItemId) =>
        _exceptionService.TryCatchAsync(async () =>
        {
            var unitGroupDescriptionItems = _db.OrderItemUnitGroupDescriptionItems.Where(w => w.OrderItemId == orderItemId);
            if (unitGroupDescriptionItems.Any())
            {
                var unitGroupDescriptionId = unitGroupDescriptionItems.FirstOrDefault().OrderItemUnitGroupDescriptionId;

                _db.OrderItemUnitGroupDescriptionItems.RemoveRange(unitGroupDescriptionItems);
                await _db.SaveChangesAsync();

                var unitGroupDescriptions = _db.OrderItemUnitGroupDescriptions
                    .Where(w => w.Id == unitGroupDescriptionId && !w.OrderItemUnitGroupDescriptionItems.Any());
                if (unitGroupDescriptions.Any())
                {
                    var uniGroupId = unitGroupDescriptions.FirstOrDefault().OrderItemUnitGroupId;

                    _db.OrderItemUnitGroupDescriptions.RemoveRange(unitGroupDescriptions);
                    await _db.SaveChangesAsync();

                    var unitGroups = _db.OrderItemUnitGroups.Where(w => w.Id == uniGroupId && !w.OrderItemUnitGroupDescriptions.Any());
                    if (unitGroups.Any())
                    {
                        _db.OrderItemUnitGroups.RemoveRange(unitGroups);
                        await _db.SaveChangesAsync();
                    }
                }
                return unitGroupDescriptionItems.FirstOrDefault();
            }
            return new OrderItemUnitGroupDescriptionItem();            
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

            var item = _db.OrderItems.Find(model.OrderItemId);
            item.PriceRate = model.PriceRate;
            item.Amount = model.PriceRate == 0 ? model.UnitCost * model.QtyRequest : model.GroupCost * (model.PriceRate / 100);
            item.UnitCost = model.PriceRate == 0 ? model.UnitCost : decimal.Round((decimal)(item.Amount / model.QtyRequest), 2, MidpointRounding.AwayFromZero);
            item.UpdatedBy = model.UpdatedBy;
            item.UpdatedDt = model.UpdatedDt;
            _db.OrderItems.Attach(item);
            _db.Entry(item).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });
    }
}