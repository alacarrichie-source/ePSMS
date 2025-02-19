using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.PurchaseOrder
{
    public interface IOrderItemUnitGroupDescriptionItemService
    {
        IQueryable<OrderItemUnitGroupDescriptionItemVM> GetByUnitGroupDescriptionId(Guid? unitGroupDescriptionId);
        ValueTask<OrderItemUnitGroupDescriptionItem> GetByIdAsync(Guid? id);
        void UpdateOrderItem(Guid? orderItemId, decimal? priceRate, decimal? unitCost, string user, DateTime date);
        ValueTask<OrderItemUnitGroupDescriptionItemVM> CreateAsync(OrderItemUnitGroupDescriptionItemVM model, string user, DateTime date);
        ValueTask<OrderItemUnitGroupDescriptionItemVM> UpdateAsync(OrderItemUnitGroupDescriptionItemVM model, string user, DateTime date);
        ValueTask<OrderItemUnitGroupDescriptionItemVM> DeleteAsync(OrderItemUnitGroupDescriptionItemVM model, string user, DateTime date);
        ValueTask<OrderItemUnitGroupDescriptionItem> DeleteEmptyGroupsAsync(Guid? orderItemId);
    }

    public class OrderItemUnitGroupDescriptionItemService : IOrderItemUnitGroupDescriptionItemService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<OrderItemUnitGroupDescriptionItemVM> _vmExceptionService = new ExceptionService<OrderItemUnitGroupDescriptionItemVM>();
        private readonly IExceptionService<OrderItemUnitGroupDescriptionItem> _exceptionService = new ExceptionService<OrderItemUnitGroupDescriptionItem>();

        public OrderItemUnitGroupDescriptionItemService(AppManEntities db)
        {
            _db = db;
        }

        public ValueTask<OrderItemUnitGroupDescriptionItem> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
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
                    //PsNo = s.RequestItemUnitGroupDescriptionItem.RisItemUnitGroupDescriptionItem.RisItem.PsNo,
                    //ItemName = s.RequestItemUnitGroupDescriptionItem.RisItemUnitGroupDescriptionItem.RisItem.ItemName,
                    //Description = s.RequestItemUnitGroupDescriptionItem.RisItemUnitGroupDescriptionItem.RisItem.Description,
                    //Unit = s.RequestItemUnitGroupDescriptionItem.RisItemUnitGroupDescriptionItem.RisItem.Unit,
                    //QtyRequest = s.RequestItemUnitGroupDescriptionItem.RisItemUnitGroupDescriptionItem.RisItem.QtyRequest,
                    //PriceRate = s.OrderItem.PriceRate,
                    //UnitCost = s.OrderItem.UnitCost,
                    //TotalCost = s.OrderItem.Amount,
                    //GroupUnitCost = s.OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost,
                    //GroupTotalCost = s.OrderItemUnitGroupDescription.OrderItemUnitGroup.TotalCost,
                    //GroupQty = s.OrderItemUnitGroupDescription.OrderItemUnitGroup.RequestItemUnitGroup.RisItemUnitGroup.Qty,
                    PsNo = s.OrderItem.PsNo,
                    ItemName = s.OrderItem.ItemName,
                    Description = s.OrderItem.Description,
                    Unit = s.OrderItem.Unit,
                    QtyRequest = (int?)s.OrderItem.Qty,
                    PriceRate = s.OrderItem.PriceRate,
                    UnitCost = s.OrderItem.UnitCost,
                    TotalCost = s.OrderItem.Amount,
                    GroupUnitCost = s.OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost,
                    GroupTotalCost = s.OrderItemUnitGroupDescription.OrderItemUnitGroup.TotalCost,
                    GroupQty = s.OrderItemUnitGroupDescription.OrderItemUnitGroup.Qty,
                    InsertedDt = s.InsertedDt
                });
            return data;
        });
        
        public ValueTask<OrderItemUnitGroupDescriptionItemVM> CreateAsync(OrderItemUnitGroupDescriptionItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
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
        _vmExceptionService.TryCatch(async () =>
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
        _exceptionService.TryCatch(async () =>
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
        _vmExceptionService.TryCatch(async () =>
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

            //var item = _db.OrderItems.Find(model.OrderItemId);
            //var groupUnitCost = model.GroupUnitCost / model.GroupQty;
            //item.PriceRate = model.PriceRate;
            ////item.Amount = model.PriceRate == 0 ? model.UnitCost * model.QtyRequest : model.GroupCost * (model.PriceRate / 100);
            ////item.UnitCost = model.PriceRate == 0 ? model.UnitCost : decimal.Round((decimal)(item.Amount / model.QtyRequest), 2, MidpointRounding.AwayFromZero);
            //if (model.PriceRate == 0)
            //{
            //    item.UnitCost = model.UnitCost;
            //}
            //else
            //{
            //    item.UnitCost = decimal.Round((decimal)(groupUnitCost * (model.PriceRate / 100) * model.QtyRequest), 2, MidpointRounding.AwayFromZero);
            //}
            //item.Amount = model.QtyRequest * item.UnitCost;
            //item.UpdatedBy = model.UpdatedBy;
            //item.UpdatedDt = model.UpdatedDt;
            //_db.OrderItems.Attach(item);
            //_db.Entry(item).State = EntityState.Modified;
            //await _db.SaveChangesAsync();

            UpdateOrderItem(model.OrderItemId, model.PriceRate, model.UnitCost, user, date);

            return model;
        });

        public void UpdateOrderItem(Guid? orderItemId, decimal? priceRate, decimal? unitCost, string user, DateTime date)
        {
            var orderItem = _db.OrderItems.Include(i => i.OrderItemUnitGroupDescriptionItems).Where(w => w.Id == orderItemId).FirstOrDefault();
            var unitGroup = _db.OrderItemUnitGroups.Where(w => w.OrderItemUnitGroupDescriptions.Any(a => a.OrderItemUnitGroupDescriptionItems.Any(a2 => a2.OrderItemId == orderItemId))).FirstOrDefault();
            //var totalCost = orderItem.OrderItemUnitGroupDescriptionItems.FirstOrDefault().OrderItemUnitGroupDescription.OrderItemUnitGroup.TotalCost;
            //var setUnitCost = orderItem.OrderItemUnitGroupDescriptionItems.FirstOrDefault().OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost;
            //var setTotalCost = orderItem.OrderItemUnitGroupDescriptionItems.FirstOrDefault().OrderItemUnitGroupDescription.OrderItemUnitGroup.TotalCost;
            var setUnitCost = unitGroup.UnitCost;
            var setTotalCost = unitGroup.TotalCost;
            var setQty = unitGroup.Qty;
            orderItem.PriceRate = priceRate;

            if (priceRate == 0)
            {
                orderItem.UnitCost = unitCost;
                orderItem.PriceRate = decimal.Round((decimal)((unitCost * orderItem.Qty * setQty) / setTotalCost) * 100, 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                orderItem.PriceRate = priceRate;
                orderItem.UnitCost = decimal.Round((decimal)(setUnitCost * (priceRate / 100)), 2, MidpointRounding.AwayFromZero) / orderItem.Qty;
            }
            orderItem.Amount = (orderItem.Qty * orderItem.UnitCost) * setQty;
            orderItem.UpdatedBy = user;
            orderItem.UpdatedDt = date;
            _db.OrderItems.Attach(orderItem);
            _db.Entry(orderItem).State = EntityState.Modified;
            _db.SaveChanges();
        }
    }
}