using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services.PurchaseOrder
{
    public interface IOrderItemUnitGroupDescriptionItemService : IOrderItemUnitGroupDescriptionItemSharedService
    {
        IQueryable<OrderItemUnitGroupDescriptionItemVM> GetByUnitGroupDescriptionId(Guid? unitGroupDescriptionId);
        ValueTask<OrderItemUnitGroupDescriptionItem> GetByIdAsync(Guid? id);
        Task UpdateOrderItemAsync(AppManEntities ctx, Guid? orderItemId, decimal? priceRate, decimal? unitCost, string user, DateTime date);
        ValueTask<OrderItemUnitGroupDescriptionItemVM> CreateAsync(OrderItemUnitGroupDescriptionItemVM model, string user, DateTime date);
        ValueTask<OrderItemUnitGroupDescriptionItemVM> UpdateAsync(OrderItemUnitGroupDescriptionItemVM model, string user, DateTime date);
        ValueTask<OrderItemUnitGroupDescriptionItemVM> DeleteAsync(OrderItemUnitGroupDescriptionItemVM model, string user, DateTime date);
        //ValueTask<OrderItemUnitGroupDescriptionItem> DeleteEmptyGroupsAsync(Guid? orderItemId);
    }

    public class OrderItemUnitGroupDescriptionItemService : IOrderItemUnitGroupDescriptionItemService
    {
        private readonly AppManEntities _db;
        private readonly IAppManEntitiesFactory _contextFactory;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<OrderItemUnitGroupDescriptionItemVM> _vmExceptionService;
        private readonly IExceptionService<OrderItemUnitGroupDescriptionItem> _exceptionService;
        private readonly IOrderItemUnitGroupDescriptionItemSharedService _orderItemUnitGroupDescriptionItemSharedService;
        private readonly IOrderItemSharedService _orderItemSharedService;

        public OrderItemUnitGroupDescriptionItemService(AppManEntities db,
            IAppManEntitiesFactory appManEntitiesFactory,
            ICreateAndLogExceptions exceptions,
            IExceptionService<OrderItemUnitGroupDescriptionItemVM> vmExceptionService,
            IExceptionService<OrderItemUnitGroupDescriptionItem> exceptionService,
            IOrderItemUnitGroupDescriptionItemSharedService orderItemUnitGroupDescriptionItemSharedService,
            IOrderItemSharedService orderItemSharedService)
        {
            _db = db;
            _contextFactory = appManEntitiesFactory;
            _exceptions = exceptions;
            _vmExceptionService = vmExceptionService;
            _exceptionService = exceptionService;
            _orderItemUnitGroupDescriptionItemSharedService = orderItemUnitGroupDescriptionItemSharedService;
            _orderItemSharedService = orderItemSharedService;
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
                    Category = s.OrderItem.ItemCode.ItemType.Category,
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

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                ctx.OrderItemUnitGroupDescriptionItems.Add(entity);
                await ctx.SaveChangesAsync();
            }

            return model;
        });

        public ValueTask<OrderItemUnitGroupDescriptionItemVM> DeleteAsync(OrderItemUnitGroupDescriptionItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            
            var orderItemVM = new OrderItemVM()
            {
                Id = (Guid)model.OrderItemId
            };

            await _orderItemSharedService.DeleteAsync(orderItemVM, user, date);            

            return model;
        });

        public ValueTask<OrderItemUnitGroupDescriptionItem> DeleteEmptyGroupsAsync(Guid? orderItemId) =>
        _exceptionService.TryCatch(async () =>
        {
            return await _orderItemUnitGroupDescriptionItemSharedService.DeleteEmptyGroupsAsync(orderItemId);            
        });

        public ValueTask<OrderItemUnitGroupDescriptionItemVM> UpdateAsync(OrderItemUnitGroupDescriptionItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = await ctx.OrderItemUnitGroupDescriptionItems.FindAsync(model.Id);

                entity.OrderItemUnitGroupDescriptionId = model.OrderItemUnitGroupDescriptionId;
                entity.OrderItemId = model.OrderItemId;
                entity.RequestItemUnitGroupDescriptionItemId = model.RequestItemUnitGroupDescriptionItemId;
                entity.UpdatedBy = model.UpdatedBy;
                entity.UpdatedDt = model.UpdatedDt;

                //_db.OrderItemUnitGroupDescriptionItems.Attach(entity);
                //_db.Entry(entity).State = EntityState.Modified;
                await ctx.SaveChangesAsync();
                await UpdateOrderItemAsync(ctx, model.OrderItemId, model.PriceRate ?? 0, model.UnitCost ?? 0, user, date);
            }

            return model;
        });

        public async Task UpdateOrderItemAsync(AppManEntities ctx, Guid? orderItemId, decimal? priceRate, decimal? unitCost, string user, DateTime date)
        {
            var orderItem = await ctx.OrderItems.Include(i => i.OrderItemUnitGroupDescriptionItems).Where(w => w.Id == orderItemId).FirstOrDefaultAsync();
            var unitGroup = await ctx.OrderItemUnitGroups.Where(w => w.OrderItemUnitGroupDescriptions.Any(a => a.OrderItemUnitGroupDescriptionItems.Any(a2 => a2.OrderItemId == orderItemId))).FirstOrDefaultAsync();
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
            //_db.OrderItems.Attach(orderItem);
            //_db.Entry(orderItem).State = EntityState.Modified;
            await ctx.SaveChangesAsync();
        }
    }
}