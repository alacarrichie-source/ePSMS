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
    public interface IOrderItemUnitGroupService
    {
        IQueryable<OrderItemUnitGroupVM> GetByOrderId(Guid? orderId);
        ValueTask<OrderItemUnitGroup> GetByIdAsync(Guid? id);
        ValueTask<OrderItemUnitGroupVM> CreateAsync(OrderItemUnitGroupVM model, string user, DateTime date);
        ValueTask<OrderItemUnitGroupVM> UpdateAsync(OrderItemUnitGroupVM model, string user, DateTime date);
        ValueTask<OrderItemUnitGroupVM> DeleteAsync(OrderItemUnitGroupVM model, string user, DateTime date);
    }

    public class OrderItemUnitGroupService : IOrderItemUnitGroupService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<OrderItemUnitGroupVM> _vmExceptionService = new ExceptionService<OrderItemUnitGroupVM>();
        private readonly IExceptionService<OrderItemUnitGroup> _exceptionService = new ExceptionService<OrderItemUnitGroup>();
        private readonly IOrderService _orderService;
        private readonly IOrderItemUnitGroupDescriptionItemService _orderItemUnitGroupDescriptionItemService;

        public OrderItemUnitGroupService(AppManEntities db)
        {
            _db = db;
            _orderService = new OrderService(_db);
            _orderItemUnitGroupDescriptionItemService = new OrderItemUnitGroupDescriptionItemService(_db);
        }

        public ValueTask<OrderItemUnitGroup> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.OrderItemUnitGroups.FindAsync(id);
            return data;
        });

        public IQueryable<OrderItemUnitGroupVM> GetByOrderId(Guid? orderId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.OrderItemUnitGroups.AsNoTracking()                
                .Where(w => w.OrderId == orderId)
                .Select(s => new OrderItemUnitGroupVM
                {
                    Id = s.Id,
                    OrderId = s.OrderId,
                    RequestItemUnitGroupId = s.RequestItemUnitGroupId,
                    //Qty = s.RequestItemUnitGroup.RisItemUnitGroup.Qty,
                    //Unit = s.RequestItemUnitGroup.RisItemUnitGroup.Unit,
                    SetLotNo = s.SetLotNo,
                    Qty = s.Qty,
                    Unit = s.Unit,
                    UnitCost = s.UnitCost,
                    TotalCost = s.TotalCost,
                    InsertedDt = s.InsertedDt
                });
            return data;
        });

        public ValueTask<OrderItemUnitGroupVM> CreateAsync(OrderItemUnitGroupVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            if (await _orderService.IsPostedAsync((Guid)model.OrderId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            }

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;
            model.TotalCost = model.Qty * model.UnitCost;

            var entity = new OrderItemUnitGroup()
            {
                Id = model.Id,
                OrderId = model.OrderId,
                RequestItemUnitGroupId = model.RequestItemUnitGroupId,
                SetLotNo = model.SetLotNo,
                Qty = model.Qty,
                Unit = model.Unit,
                UnitCost = model.UnitCost,
                TotalCost = model.TotalCost,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.OrderItemUnitGroups.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<OrderItemUnitGroupVM> DeleteAsync(OrderItemUnitGroupVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var entity = await _db.OrderItemUnitGroups.Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (await _orderService.IsPostedAsync((Guid)model.OrderId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.OrderItemUnitGroups.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.OrderItemUnitGroups.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<OrderItemUnitGroupVM> UpdateAsync(OrderItemUnitGroupVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var entity = await _db.OrderItemUnitGroups.Include(i => i.OrderItemUnitGroupDescriptions).Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (await _orderService.IsPostedAsync((Guid)model.OrderId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.OrderId = model.OrderId;
            entity.RequestItemUnitGroupId = model.RequestItemUnitGroupId;
            entity.SetLotNo = model.SetLotNo;
            entity.Qty = model.Qty;
            entity.UnitCost = model.UnitCost;
            entity.TotalCost = model.Qty * model.UnitCost;            
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.OrderItemUnitGroups.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            var unitGroupDescriptions = entity.OrderItemUnitGroupDescriptions.ToList();
            foreach (var unitGroupDescription in unitGroupDescriptions)
            {
                var unitGroupDescriptionItems = await _db.OrderItemUnitGroupDescriptionItems
                    .Include(i => i.OrderItem)
                    .Where(w => w.OrderItemUnitGroupDescriptionId == unitGroupDescription.Id).ToListAsync();
                foreach (var unitGroupDescriptionItem in unitGroupDescriptionItems)
                {
                    var priceRate = unitGroupDescriptionItem.OrderItem.PriceRate ?? 0;
                    var unitCost = unitGroupDescriptionItem.OrderItem.UnitCost ?? 0;
                    _orderItemUnitGroupDescriptionItemService.UpdateOrderItem(unitGroupDescriptionItem.OrderItemId, priceRate, unitCost, user, date);
                }
            }

            return model;
        });
    }
}