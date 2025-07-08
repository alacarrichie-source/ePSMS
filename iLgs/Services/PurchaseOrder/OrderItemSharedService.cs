using iLgs.Exceptions;
using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.PurchaseOrder
{
    public interface IOrderItemSharedService
    {
        ValueTask<bool> GetAnyParItemsAsync(Guid id);
        ValueTask<bool> GetAnyAirItemsAsync(Guid id);
        ValueTask<OrderItemVM> DeleteAsync(OrderItemVM model, string user, DateTime date);
    }

    public class OrderItemSharedService : IOrderItemSharedService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<OrderItemVM> _vmExceptionService;
        private readonly IOrderItemUnitGroupDescriptionItemSharedService _orderItemUnitGroupDescriptionItemSharedService;

        public OrderItemSharedService(AppManEntities db,
            IExceptionService<OrderItemVM> vmExceptionService,
            IOrderItemUnitGroupDescriptionItemSharedService orderItemUnitGroupDescriptionItemSharedService)
        {
            _db = db;
            _vmExceptionService = vmExceptionService;
            _orderItemUnitGroupDescriptionItemSharedService = orderItemUnitGroupDescriptionItemSharedService;
        }

        public ValueTask<OrderItemVM> DeleteAsync(OrderItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            await ValidateOnDelete(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var orderItemGroupDescriptionItem = await _orderItemUnitGroupDescriptionItemSharedService.DeleteEmptyGroupsAsync(model.Id);

            OrderItem entity = await _db.OrderItems.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.OrderItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.OrderItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        private async ValueTask ValidateOnDelete(OrderItemVM model)
        {
            var postedBy = _db.Orders.FindAsync(model.OrderId).Result?.PostedBy;
            if (!string.IsNullOrWhiteSpace(postedBy))
            {
                throw new RecordAlreadyPostedException("PO Number already Posted, cannot delete!");
            }
            if (await GetAnyAirItemsAsync(model.Id))
            {
                throw new RecordRelationshipException("PO Number already with AIR, cannot delete!");
            }
            if (await GetAnyParItemsAsync(model.Id))
            {
                throw new RecordRelationshipException("PO Number already with PAR, cannot delete!");
            }
            //if (!string.IsNullOrEmpty(model.SetLotNo))
            //{
            //    throw new RecordRelationshipException("Item belongs to a set, cannot delete!");
            //}
        }

        public async ValueTask<bool> GetAnyParItemsAsync(Guid id)
        {
            return await _db.PARItems.AnyAsync(a => a.OrderItemId == id);
        }

        public async ValueTask<bool> GetAnyAirItemsAsync(Guid id)
        {
            return await _db.AIRItems.AnyAsync(a => a.OrderItemId == id);
        }
    }
}