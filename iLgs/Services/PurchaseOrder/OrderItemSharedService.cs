using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Utilities;
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
        Task<bool> GetAnyParItemsAsync(Guid id);
        Task<bool> GetAnyAirItemsAsync(Guid id);
        //ValueTask<OrderItemVM> DeleteAsync(OrderItemVM model, string user, DateTime date);
    }

    public class OrderItemSharedService : IOrderItemSharedService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<OrderItemVM> _vmExceptionService;
        //private readonly IOrderItemUnitGroupDescriptionItemSharedService _orderItemUnitGroupDescriptionItemSharedService;
        private readonly IOrderSharedService _orderSharedService;

        public OrderItemSharedService(AppManEntities db)
        {
            _db = db;
            _vmExceptionService = new ExceptionService<OrderItemVM>();
            //_orderItemUnitGroupDescriptionItemSharedService = new OrderItemUnitGroupDescriptionItemSharedService(_db);
            _orderSharedService = new OrderSharedService(_db);
        }

        //public OrderItemSharedService(AppManEntities db,
        //    IAppManEntitiesFactory appManEntitiesFactory,
        //    IExceptionService<OrderItemVM> vmExceptionService,
        //    IOrderItemUnitGroupDescriptionItemSharedService orderItemUnitGroupDescriptionItemSharedService)
        //{
        //    _db = db;
        //    _contextFactory = appManEntitiesFactory;
        //    _vmExceptionService = vmExceptionService;
        //    _orderItemUnitGroupDescriptionItemSharedService = orderItemUnitGroupDescriptionItemSharedService;
        //}

        //public ValueTask<OrderItemVM> DeleteAsync(OrderItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        //{
        //    await ValidateOnDelete(model);

        //    model.UpdatedBy = user;
        //    model.UpdatedDt = date;

        //    await _orderItemUnitGroupDescriptionItemSharedService.DeleteGroupItemAsync(model.Id);
        //    var entity = await _db.OrderItems.FirstOrDefaultAsync(f => f.Id == model.Id);
        //    ValidateRecord(entity, model.Id);

        //    entity.UpdatedBy = model.UpdatedBy;
        //    entity.UpdatedDt = model.UpdatedDt;

        //    await _db.SaveChangesAsync();

        //    _db.OrderItems.Remove(entity);
        //    await _db.SaveChangesAsync();

        //    return model;
        //});

        //private void ValidateRecord(OrderItem entity, Guid id)
        //{
        //    if (entity == null)
        //    {
        //        throw new NotFoundException(id);
        //    }
        //}

        private async Task ValidateOnDelete(OrderItemVM model)
        {
            await _orderSharedService.ValidateStatusAsync((Guid)model.OrderId);            
        }

        public async Task<bool> GetAnyParItemsAsync(Guid id)
        {
            return await _db.PARItems.AnyAsync(a => a.OrderItemId == id);
        }

        public async Task<bool> GetAnyAirItemsAsync(Guid id)
        {
            var orderItemRequests = await _db.OrderItemRequests.Where(w => w.OrderItemId == id).ToListAsync();
            foreach (var orderItemRequest in orderItemRequests)
            {
                if (await _db.AIRItems.AnyAsync(a => a.OrderItemRequestId == orderItemRequest.Id))
                {
                    return true;
                }
            }
            return false;
        }
    }
}