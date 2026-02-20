using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Items;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.PurchaseOrder
{
    public interface IOrderItemUnitGroupDescriptionItemService : IOrderItemUnitGroupDescriptionItemSharedService
    {
        IQueryable<OrderItemUnitGroupDescriptionItemVM> GetByUnitGroupDescriptionId(Guid? unitGroupDescriptionId);
        IQueryable<OrderItemUnitGroupDescriptionItemVM> GetAvailableUnitGroupItem(Guid? orderId);
        ValueTask<OrderItemUnitGroupDescriptionItem> GetByIdAsync(Guid? id);
        Task UpdateOrderItemAsync(Guid? orderItemId, decimal? priceRate, decimal? unitCost, string user, DateTime date);
        ValueTask<OrderItemUnitGroupDescriptionItemVM> CreateAsync(OrderItemUnitGroupDescriptionItemVM model, string user, DateTime date);
        ValueTask<OrderItemUnitGroupDescriptionItemVM> UpdateAsync(OrderItemUnitGroupDescriptionItemVM model, string user, DateTime date);
        ValueTask<OrderItemUnitGroupDescriptionItemVM> DeleteAsync(OrderItemUnitGroupDescriptionItemVM model, string user, DateTime date);
        //ValueTask<OrderItemUnitGroupDescriptionItem> DeleteEmptyGroupsAsync(Guid? orderItemId);
    }

    internal class OrderItemUnitGroupDescriptionItemService : BaseValidator,  IOrderItemUnitGroupDescriptionItemService
    {
        private readonly AppManEntities _db;
        private readonly IOrderSharedService _orderSharedService;
        private readonly IItemCodeService _itemCodeService;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<OrderItemUnitGroupDescriptionItemVM> _vmExceptionService;
        private readonly IExceptionService<OrderItemUnitGroupDescriptionItem> _exceptionService;
        private readonly IOrderItemUnitGroupDescriptionItemSharedService _orderItemUnitGroupDescriptionItemSharedService;
        private readonly IOrderItemSharedService _orderItemSharedService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        public OrderItemUnitGroupDescriptionItemService(AppManEntities db)
        {
            _db = db;
            _orderSharedService = new OrderSharedService(_db);
            _itemCodeService = new ItemCodeService(_db);
            _exceptions = new CreateAndLogExceptions();
            _vmExceptionService = new ExceptionService<OrderItemUnitGroupDescriptionItemVM>();
            _exceptionService = new ExceptionService<OrderItemUnitGroupDescriptionItem>();
            _orderItemUnitGroupDescriptionItemSharedService = new OrderItemUnitGroupDescriptionItemSharedService(_db);
            _orderItemSharedService = new OrderItemSharedService(_db);
            _getDisplayName = propertyName => Utility.GetDisplayName<OrderItemUnitGroupDescriptionItemVM>(propertyName);
        }

        //public OrderItemUnitGroupDescriptionItemService(AppManEntities db,
        //    IAppManEntitiesFactory appManEntitiesFactory,
        //    ICreateAndLogExceptions exceptions,
        //    IExceptionService<OrderItemUnitGroupDescriptionItemVM> vmExceptionService,
        //    IExceptionService<OrderItemUnitGroupDescriptionItem> exceptionService,
        //    IOrderItemUnitGroupDescriptionItemSharedService orderItemUnitGroupDescriptionItemSharedService,
        //    IOrderItemSharedService orderItemSharedService)
        //{
        //    _db = db;
        //    _contextFactory = appManEntitiesFactory;
        //    _exceptions = exceptions;
        //    _vmExceptionService = vmExceptionService;
        //    _exceptionService = exceptionService;
        //    _orderItemUnitGroupDescriptionItemSharedService = orderItemUnitGroupDescriptionItemSharedService;
        //    _orderItemSharedService = orderItemSharedService;
        //}

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
                    //RequestItemUnitGroupDescriptionItemId = s.RequestItemUnitGroupDescriptionItemId,
                    //RequestItemUnitGroupDescriptionItem = s.RequestItemUnitGroupDescriptionItem,
                    ItemNo = s.OrderItem.ItemNo,
                    ItemNoIndex = s.OrderItem.ItemNoIndex,
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

        public IQueryable<OrderItemUnitGroupDescriptionItemVM> GetAvailableUnitGroupItem(Guid? orderId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.OrderItems
            .Where(w => w.OrderId == orderId && !w.OrderItemUnitGroupDescriptionItems.Any(a => a.OrderItemId == w.Id))
            .AsNoTracking()
            .Select(s => new OrderItemUnitGroupDescriptionItemVM
            {
                Id = s.Id,
                Category = s.ItemCode.ItemType.Category,
                PsNo = s.PsNoDisplay,
                ItemName = s.ItemName,
                Description = s.Description,
                Unit = s.Unit,
                QtyRequest = (int?)s.Qty,
                InsertedDt = s.InsertedDt
            });
            return data;
        });

        public ValueTask<OrderItemUnitGroupDescriptionItemVM> CreateAsync(OrderItemUnitGroupDescriptionItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            await ValidateStatusAsync(model.OrderItemUnitGroupDescriptionId);

            /*
             * All items must be of same category
             */

            bool? isProperty = null;
            var selectedItems = model.GridItems.Split(',');

            foreach (var item in selectedItems)
            {
                var itemId = Guid.Parse(item);
                var risItem = await _db.OrderItems
                    .AsNoTracking()
                    .Include(i => i.ItemCode.ItemType)
                    .FirstOrDefaultAsync(f => f.Id == itemId);

                if (isProperty == null)
                {
                    isProperty = _itemCodeService.IsProperty(risItem.ItemCodeId);
                }
                else
                {
                    if (_itemCodeService.IsProperty(risItem.ItemCodeId) != isProperty)
                    {

                        throw new InvalidValueException("Selected Items must be of same category.");
                    }
                }
            }

            // verify selected items vs existing item
            var orderItemUnitGroupDescriptionItem = _db.OrderItemUnitGroupDescriptionItems
                .AsNoTracking()
                .Include(i => i.OrderItem)
                .FirstOrDefault(f => f.OrderItemUnitGroupDescription.Id == model.OrderItemUnitGroupDescriptionId);
            if (orderItemUnitGroupDescriptionItem != null)
            {
                if (_itemCodeService.IsProperty(orderItemUnitGroupDescriptionItem.OrderItem.ItemCodeId) != isProperty)
                {
                    throw new InvalidValueException("The category of the Selected Items must be the same as category of the Existing items.");
                }
            }

            foreach (var item in selectedItems)
            {
                model.Id = Guid.NewGuid();
                var entity = new OrderItemUnitGroupDescriptionItem()
                {
                    Id = model.Id,
                    OrderItemUnitGroupDescriptionId = model.OrderItemUnitGroupDescriptionId,
                    OrderItemId = Guid.Parse(item),
                    InsertedBy = model.InsertedBy,
                    InsertedDt = model.InsertedDt,
                    UpdatedBy = model.UpdatedBy,
                    UpdatedDt = model.UpdatedDt
                };

                _db.OrderItemUnitGroupDescriptionItems.Add(entity);
            }
            
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<OrderItemUnitGroupDescriptionItemVM> DeleteAsync(OrderItemUnitGroupDescriptionItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);            
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.OrderItemUnitGroupDescriptionItems.FirstOrDefaultAsync(f => f.Id == model.Id);

            ValidateRecord(entity, model.Id);
            await ValidateOrderItemStatusAsync(entity.OrderItemId);
            
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();

            _db.OrderItemUnitGroupDescriptionItems.Remove(entity);
            await _db.SaveChangesAsync();
            
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
            ValidateIfNull(model);
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.OrderItemUnitGroupDescriptionItems.FindAsync(model.Id);
            ValidateRecord(entity, model.Id);
            await ValidateStatusAsync(model.OrderItemUnitGroupDescriptionId);

            entity.OrderItemUnitGroupDescriptionId = model.OrderItemUnitGroupDescriptionId;
            entity.OrderItemId = model.OrderItemId;
            entity.RequestItemUnitGroupDescriptionItemId = model.RequestItemUnitGroupDescriptionItemId;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();
            await UpdateOrderItemAsync(model.OrderItemId, model.PriceRate ?? 0, model.UnitCost ?? 0, user, date);

            return model;
        });

        public async Task UpdateOrderItemAsync(Guid? orderItemId, decimal? priceRate, decimal? unitCost, string user, DateTime date)
        {
            var orderItem = await _db.OrderItems.Where(w => w.Id == orderItemId).FirstOrDefaultAsync();
            var unitGroup = await _db.OrderItemUnitGroups.Where(w => w.OrderItemUnitGroupDescriptions.Any(a => a.OrderItemUnitGroupDescriptionItems.Any(a2 => a2.OrderItemId == orderItemId))).FirstOrDefaultAsync();
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

            await _db.SaveChangesAsync();
        }

        private void ValidateIfNull(OrderItemUnitGroupDescriptionItemVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(OrderItemUnitGroupDescriptionItem entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }
        
        private async Task ValidateOnCreateUpdateAsync(OrderItemUnitGroupDescriptionItemVM model, Mode mode)
        {
            _imex = new InvalidModelException();


            _imex.ThrowIfContainsErrors();
        }

        private async Task ValidateStatusAsync(Guid? unitGroupDescriptionId)
        {
            var orderId = (await _db.OrderItemUnitGroups.FirstOrDefaultAsync(f => f.OrderItemUnitGroupDescriptions.Any(a => a.Id == unitGroupDescriptionId)))?.OrderId;
            await _orderSharedService.ValidateStatusAsync((Guid)orderId);
        }

        private async Task ValidateOrderItemStatusAsync(Guid? orderItemId)
        {
            var orderId = (await _db.OrderItems.FirstOrDefaultAsync(f => f.Id == orderItemId))?.OrderId;
            await _orderSharedService.ValidateStatusAsync((Guid)orderId);
        }

        private void InvalidKeyValueException(string key, string message)
        {
            _imex = new InvalidModelException(); ;
            _imex.UpsertDataList(key, message);
            _imex.ThrowIfContainsErrors();
        }
    }
}