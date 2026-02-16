using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.PurchaseOrder
{
    public interface IOrderItemUnitGroupDescriptionService
    {
        IQueryable<OrderItemUnitGroupDescriptionVM> GetByUnitGroupId(Guid? unitGroupId);
        IQueryable<OrderItemUnitGroupDescriptionVM> GetByOrderId(Guid? orderId);
        ValueTask<OrderItemUnitGroupDescriptionVM> GetByIdAsync(Guid? id);
        ValueTask<OrderItemUnitGroupDescriptionVM> CreateAsync(OrderItemUnitGroupDescriptionVM model, string user, DateTime date);
        ValueTask<OrderItemUnitGroupDescriptionVM> UpdateAsync(OrderItemUnitGroupDescriptionVM model, string user, DateTime date);
        ValueTask<OrderItemUnitGroupDescriptionVM> DeleteAsync(OrderItemUnitGroupDescriptionVM model, string user, DateTime date);

        IOrderItemUnitGroupDescriptionItemService UnitGroupDescriptionItem { get; }        
    }

    internal class OrderItemUnitGroupDescriptionService : BaseValidator, IOrderItemUnitGroupDescriptionService
    {
        private readonly AppManEntities _db;
        private readonly IOrderSharedService _orderSharedService;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<OrderItemUnitGroupDescriptionVM> _vmExceptionService;
        private readonly IExceptionService<OrderItemUnitGroupDescription> _exceptionService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        private IOrderItemUnitGroupDescriptionItemService _unitGroupDescriptionItemService;

        public OrderItemUnitGroupDescriptionService(AppManEntities db)
        {
            _db = db;
            _orderSharedService = new OrderSharedService(_db);
            _exceptions = new CreateAndLogExceptions();
            _vmExceptionService = new ExceptionService<OrderItemUnitGroupDescriptionVM>();
            _exceptionService = new ExceptionService<OrderItemUnitGroupDescription>();
            _getDisplayName = propertyName => Utility.GetDisplayName<OrderItemUnitGroupDescriptionVM>(propertyName);
            _unitGroupDescriptionItemService = new OrderItemUnitGroupDescriptionItemService(_db);
        }

        //public OrderItemUnitGroupDescriptionService(AppManEntities db,
        //    IAppManEntitiesFactory appManEntitiesFactory,
        //    ICreateAndLogExceptions exceptions,
        //    IExceptionService<OrderItemUnitGroupDescriptionVM> vmExceptionService,
        //    IExceptionService<OrderItemUnitGroupDescription> exceptionService)
        //{
        //    _db = db;
        //    _contextFactory = appManEntitiesFactory;
        //    _exceptions = exceptions;
        //    _vmExceptionService = vmExceptionService;
        //    _exceptionService = exceptionService;
        //}

        public IOrderItemUnitGroupDescriptionItemService UnitGroupDescriptionItem => _unitGroupDescriptionItemService;

        private static Expression<Func<OrderItemUnitGroupDescription, OrderItemUnitGroupDescriptionVM>> GetProjection()
        {
            return s => new OrderItemUnitGroupDescriptionVM
            {
                Id = s.Id,
                OrderItemUnitGroupId = s.OrderItemUnitGroupId,
                RequestItemUnitGroupDescriptionId = s.RequestItemUnitGroupDescriptionId,
                Description = s.Description,
                InsertedDt = s.InsertedDt,
                // Transient
                OrderId = s.OrderItemUnitGroup.OrderId,
                SetLotNo = s.OrderItemUnitGroup.SetLotNo,
                Qty = s.OrderItemUnitGroup.Qty,
                Unit = s.OrderItemUnitGroup.Unit,
                UnitCost = s.OrderItemUnitGroup.UnitCost,
                TotalCost = s.OrderItemUnitGroup.TotalCost                
            };
        }

        public ValueTask<OrderItemUnitGroupDescriptionVM> GetByIdAsync(Guid? id) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.OrderItemUnitGroupDescriptions.Where(w => w.Id == id).AsNoTracking()
                .Select(GetProjection()).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<OrderItemUnitGroupDescriptionVM> GetByUnitGroupId(Guid? unitGroupId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.OrderItemUnitGroupDescriptions.Where(w => w.OrderItemUnitGroupId == unitGroupId).AsNoTracking()
                .Select(GetProjection());
            return data;
        });

        public IQueryable<OrderItemUnitGroupDescriptionVM> GetByOrderId(Guid? orderId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.OrderItemUnitGroupDescriptions
                    .Include(i => i.OrderItemUnitGroup)
                    .Where(w => w.OrderItemUnitGroup.OrderId == orderId).AsNoTracking()
                .Select(GetProjection());
            return data;
        });

        public ValueTask<OrderItemUnitGroupDescriptionVM> CreateAsync(OrderItemUnitGroupDescriptionVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            await ValidateStatusAsync((Guid)model.OrderId);

            if (model.Id == null || model.Id == Guid.Empty)
            {
                model.Id = Guid.NewGuid();
            }

            var unitGroupDescription = new OrderItemUnitGroupDescription()
            {
                Id = model.Id,
                OrderItemUnitGroupId = model.OrderItemUnitGroupId,
                Description = model.Description,
                OtherParticulars = model.OtherParticulars,
                InsertedBy = user,
                InsertedDt = date,
                UpdatedBy = user,
                UpdatedDt = date
            };

            var unitGroup = await _db.OrderItemUnitGroups.FirstOrDefaultAsync(f => f.OrderId == model.OrderId
                && f.SetLotNo == model.SetLotNo);
            if (unitGroup == null)
            {
                unitGroup = new OrderItemUnitGroup()
                {
                    Id = (Guid)model.OrderItemUnitGroupId,
                    OrderId = model.OrderId,
                    SetLotNo = model.SetLotNo,
                    Qty = model.Qty,
                    Unit = model.Unit,
                    UnitCost = model.UnitCost,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };
                unitGroup.TotalCost = unitGroup.Qty * unitGroup.UnitCost;
                unitGroup.OrderItemUnitGroupDescriptions.Add(unitGroupDescription);

                _db.OrderItemUnitGroups.Add(unitGroup);
            }
            else
            {
                unitGroup.OrderItemUnitGroupDescriptions.Add(unitGroupDescription);
            }
            
            await _db.SaveChangesAsync();

            return model;
        });        

        public ValueTask<OrderItemUnitGroupDescriptionVM> UpdateAsync(OrderItemUnitGroupDescriptionVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.OrderItemUnitGroupDescriptions.Include(i => i.OrderItemUnitGroup).FirstOrDefaultAsync(f => f.Id == model.Id);
            ValidateRecord(entity, model.Id);
            await ValidateStatusAsync((Guid)model.OrderId);

            entity.OrderItemUnitGroupId = model.OrderItemUnitGroupId;
            entity.Description = model.Description;
            entity.OtherParticulars = model.OtherParticulars;
            entity.OrderItemUnitGroup.SetLotNo = model.SetLotNo;
            entity.OrderItemUnitGroup.Qty = model.Qty;
            entity.OrderItemUnitGroup.Unit = model.Unit;
            entity.OrderItemUnitGroup.UnitCost = model.UnitCost;
            entity.OrderItemUnitGroup.TotalCost = model.Qty * model.UnitCost;
            entity.OrderItemUnitGroup.UpdatedBy = model.UpdatedBy;
            entity.OrderItemUnitGroup.UpdatedDt = model.UpdatedDt;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();

            var unitGroupDescriptions = await _db.OrderItemUnitGroupDescriptions
                .Where(w => w.OrderItemUnitGroupId == model.OrderItemUnitGroupId).ToListAsync();
            foreach (var unitGroupDescription in unitGroupDescriptions)
            {
                var unitGroupDescriptionItems = await _db.OrderItemUnitGroupDescriptionItems
                    .Include(i => i.OrderItem)
                    .Where(w => w.OrderItemUnitGroupDescriptionId == unitGroupDescription.Id).ToListAsync();
                foreach (var unitGroupDescriptionItem in unitGroupDescriptionItems)
                {
                    var priceRate = unitGroupDescriptionItem.OrderItem.PriceRate ?? 0;
                    var unitCost = unitGroupDescriptionItem.OrderItem.UnitCost ?? 0;
                    await UnitGroupDescriptionItem.UpdateOrderItemAsync(unitGroupDescriptionItem.OrderItemId, priceRate, unitCost, user, date);
                }
            }

            return model;
        });

        public ValueTask<OrderItemUnitGroupDescriptionVM> UpdateAsyncOld(OrderItemUnitGroupDescriptionVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.OrderItemUnitGroupDescriptions.FindAsync(model.Id);

            entity.OrderItemUnitGroupId = model.OrderItemUnitGroupId;
            entity.RequestItemUnitGroupDescriptionId = model.RequestItemUnitGroupDescriptionId;
            entity.Description = model.Description;
            entity.OtherParticulars = model.OtherParticulars;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<OrderItemUnitGroupDescriptionVM> DeleteAsync(OrderItemUnitGroupDescriptionVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.OrderItemUnitGroupDescriptions.FindAsync(model.Id);

            ValidateRecord(entity, model.Id);
            await ValidateStatusAsync((Guid)model.OrderId);

            var unitGroupId = entity.OrderItemUnitGroupId;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();

            _db.OrderItemUnitGroupDescriptions.Remove(entity);
            await _db.SaveChangesAsync();

            var unitGroup = await _db.OrderItemUnitGroups.FirstOrDefaultAsync(w => w.Id == unitGroupId && !w.OrderItemUnitGroupDescriptions.Any());
            if (unitGroup != null)
            {
                unitGroup.UpdatedBy = model.UpdatedBy;
                unitGroup.UpdatedDt = model.UpdatedDt;

                await _db.SaveChangesAsync();

                _db.OrderItemUnitGroups.Remove(unitGroup);
                await _db.SaveChangesAsync();
            }

            return model;
        });

        private void ValidateIfNull(OrderItemUnitGroupDescriptionVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(OrderItemUnitGroupDescription entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }
        
        private async Task ValidateOnCreateUpdateAsync(OrderItemUnitGroupDescriptionVM model, Mode mode)
        {
            _imex = new InvalidModelException();
            

            _imex.ThrowIfContainsErrors();
        }

        private async Task ValidateStatusAsync(Guid orderId)
        {
            await _orderSharedService.ValidateStatusAsync(orderId);
        }
    }
}