using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.AllFields;
using iLgs.Services.Codes;
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
    public interface IOrderItemService
    {
        IQueryable<OrderItemVM> GetByPoId(Guid? poId);
        ValueTask<OrderItemVM> GetByIdAsync(Guid? id);
        ValueTask<OrderItemVM> CreateAsync(OrderItemVM model, string user, DateTime date);
        ValueTask<OrderItemVM> UpdateAsync(OrderItemVM model, string user, DateTime date);
        ValueTask<OrderItemVM> DeleteAsync(OrderItemVM model, string user, DateTime date);
    }

    internal class OrderItemService : BaseValidator, IOrderItemService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<OrderItemVM> _vmExceptionService;
        private readonly ICodextnService _codextnService;
        private readonly IAllFieldService _allFieldService;
        private readonly IOrderSharedService _orderSharedService;
        private readonly IOrderItemSharedService _orderItemSharedService;
        private readonly IOrderItemUnitGroupService _orderItemUnitGroupService;
        private readonly IOrderItemUnitGroupDescriptionService _orderItemUnitGroupDescriptionService;
        private readonly IOrderItemUnitGroupDescriptionItemService _orderItemUnitGroupDescriptionItemService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        public OrderItemService(AppManEntities db)
        {
            _db = db;
            _vmExceptionService = new ExceptionService<OrderItemVM>();
            _codextnService = new CodextnService(_db);
            _allFieldService = new AllFieldService(_db);
            _orderSharedService = new OrderSharedService(_db);
            _orderItemSharedService = new OrderItemSharedService(_db);
            _orderItemUnitGroupService = new OrderItemUnitGroupService(_db);
            _orderItemUnitGroupDescriptionService = new OrderItemUnitGroupDescriptionService(_db);
            _orderItemUnitGroupDescriptionItemService = new OrderItemUnitGroupDescriptionItemService(_db);
            _getDisplayName = propertyName => Utility.GetDisplayName<OrderItemVM>(propertyName);
        }

        //public OrderItemService(AppManEntities db,       
        //    IAppManEntitiesFactory appManEntitiesFactory,
        //    IExceptionService<OrderItemVM> vmExceptionService,
        //    ICodextnService codextnService,
        //    IAllFieldService allFieldService,
        //    IOrderItemSharedService orderItemSharedService,
        //    IOrderItemUnitGroupService orderItemUnitGroupService,
        //    IOrderItemUnitGroupDescriptionService orderItemUnitGroupDescriptionService,
        //    IOrderItemUnitGroupDescriptionItemService orderItemUnitGroupDescriptionItemService)
        //{
        //    _db = db;
        //    _contextFactory = appManEntitiesFactory;
        //    _vmExceptionService = vmExceptionService;
        //    _codextnService = codextnService;
        //    _allFieldService = allFieldService;
        //    _orderItemSharedService = orderItemSharedService;
        //    _orderItemUnitGroupService = orderItemUnitGroupService;
        //    _orderItemUnitGroupDescriptionService = orderItemUnitGroupDescriptionService;
        //    _orderItemUnitGroupDescriptionItemService = orderItemUnitGroupDescriptionItemService;
        //    _getDisplayName = propertyName => Utility.GetDisplayName<OrderItemVM>(propertyName);
        //}

        private Expression<Func<OrderItem, OrderItemVM>> Projection()
        {
            return s => new OrderItemVM
            {
                Id = s.Id,
                OrderId = s.OrderId,
                RequestItemId = s.RequestItemId,
                ItemNo = s.ItemNo,
                ItemCodeId = s.ItemCodeId,
                PsNo = s.PsNo,
                PsNoDisplay = s.PsNoDisplay,
                ItemName = s.ItemName,
                Description = s.Description,
                Brand = s.Brand,

                RisItemId = s.RequestItem.RisItem.Id,
                Category = s.ItemCode.ItemType.Category,

                ItemCode = s.ItemCode.Code,
                ItemType = s.ItemCode.Description,
                PsType = s.ItemCode.ItemType.Code,
                PsTypeDesc = s.ItemCode.ItemType.Description,

                Unit = s.Unit,
                OtherDesc = s.OtherDesc,

                Qty = s.Qty,
                UnitCost = s.UnitCost,
                Amount = s.Amount,
                PriceRate = s.PriceRate,
                InsertedDt = s.InsertedDt,
                SetLotNo = s.OrderItemUnitGroupDescriptionItems.FirstOrDefault().OrderItemUnitGroupDescription.OrderItemUnitGroup.SetLotNo,
                PpmpCode = s.PpmpCode
            };
        }

        public ValueTask<OrderItemVM> GetByIdAsync(Guid? id) => _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.OrderItems.AsNoTracking().Where(w => w.Id == id)
                .Select(Projection()).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<OrderItemVM> GetByPoId(Guid? poId) => _vmExceptionService.TryCatch(() =>
        {
            var data = _db.OrderItems.AsNoTracking().Where(w => w.OrderId == poId)
                .Select(Projection());
            return data;
        });

        private async Task<bool> GetAnyParItemsAsync(Guid id)
        {
            return await _orderItemSharedService.GetAnyParItemsAsync(id);
        }

        private async Task<bool> GetAnyAirItemsAsync(Guid id)
        {
            return await _orderItemSharedService.GetAnyAirItemsAsync(id);
        }

        private void ValidateFields(OrderItemVM model)
        {
            _imex = new InvalidModelException();

            //if (Enum.TryParse(model.PsType, out Category c))
            //{
            //    if (_allFieldService.IsBrandRequired(c))
            //    {
            //        if (string.IsNullOrWhiteSpace(model.AllField.Brand))
            //        {
            //            _imex.UpsertDataList(_getDisplayName(nameof(model.AllField.Brand)), "Field is required.");
            //        }
            //    }                                   
            //}

            _imex.ThrowIfContainsErrors();
        }

        public ValueTask<OrderItemVM> CreateAsync(OrderItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateFields(model);
            await _orderSharedService.ValidateStatusAsync((Guid)model.OrderId);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            if (model.ItemNo == null || model.ItemNo == 0)
            {
                model.ItemNo = await NextItemNoAsync(model.OrderId);
            }
            
            if (await _db.OrderItems.AnyAsync(a => a.OrderId == model.OrderId && a.ItemNo == model.ItemNo))
            {
                _imex = new InvalidModelException();
                _imex.UpsertDataList(_getDisplayName(nameof(model.ItemNo)), $"Already Exits.");
                _imex.ThrowIfContainsErrors();
            }

            OrderItem entity = new OrderItem();

            SetItemEntity(entity, model, Mode.ADD);

            _db.OrderItems.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        private void SetItemEntity(OrderItem entity, OrderItemVM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                model.Id = Guid.NewGuid();
                model.AllField.Id = model.Id;
                model.AllField.InsertedBy = model.InsertedBy;
                model.AllField.InsertedDt = model.InsertedDt;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
                //model.PsNo = "";
            }

            model.PsNoDisplay = _allFieldService.GetOrderPsNoDisplay(model);

            entity.Id = model.Id;
            entity.ItemNo = model.ItemNo;
            entity.OrderId = model.OrderId;
            entity.RequestItemId = model.RequestItemId;
            entity.ItemCodeId = model.ItemCodeId;
            entity.PsNo = model.PsNo;
            entity.PsNoDisplay = model.PsNoDisplay;
            entity.ItemName = model.ItemName;
            entity.Unit = model.Unit;
            entity.Description = model.Description;
            entity.OtherDesc = model.OtherDesc;
            entity.Unit = model.Unit;
            entity.Qty = model.Qty;
            entity.UnitCost = model.UnitCost;
            entity.Amount = model.Amount;
            entity.PriceRate = model.PriceRate;
            entity.PpmpCode = model.PpmpCode;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            model.AllField.UpdatedBy = model.UpdatedBy;
            model.AllField.UpdatedDt = model.UpdatedDt;

            entity.AllField = model.AllField;

            _allFieldService.SetEntity(entity.AllField, model.AllField, mode);
        }

        public ValueTask<OrderItemVM> DeleteAsync(OrderItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;
            
            var entity = await _db.OrderItems.FirstOrDefaultAsync(f => f.Id == model.Id);
            ValidateRecord(entity, model.Id);
            await _orderSharedService.ValidateStatusAsync((Guid)entity.OrderId);

            var unitGroupDescriptionItem = await _db.OrderItemUnitGroupDescriptionItems.Where(w => w.OrderItemId == model.Id).FirstOrDefaultAsync();

            if (unitGroupDescriptionItem != null)
            {
                unitGroupDescriptionItem.UpdatedBy = model.UpdatedBy;
                unitGroupDescriptionItem.UpdatedDt = model.UpdatedDt;
                await _db.SaveChangesAsync();

                _db.OrderItemUnitGroupDescriptionItems.Remove(unitGroupDescriptionItem);
                await _db.SaveChangesAsync();
            }

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();

            _db.OrderItems.Remove(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<OrderItemVM> UpdateAsync(OrderItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            await _orderSharedService.ValidateStatusAsync((Guid)model.OrderId);

            //var requestItemId = _db.RequestItems.FindAsync(model.RequestItemId).Result?.RisItemId;
            //if (requestItemId == null)
            //{
            //    throw new RecordRelationshipException("Could not find request item this record!");
            //}

            //var risItemId = _db.RisItems.FindAsync(requestItemId).Result?.Id;
            //if (risItemId == null)
            //{
            //    throw new RecordRelationshipException("Cound not find RIS item for this record!");
            //}

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            if (model.ItemNo == null || model.ItemNo == 0)
            {
                model.ItemNo = await NextItemNoAsync(model.OrderId);
            }

            if (await _db.OrderItems.AnyAsync(a => a.OrderId == model.OrderId && a.ItemNo == model.ItemNo && a.Id != model.Id))
            {
                _imex = new InvalidModelException();
                _imex.UpsertDataList(_getDisplayName(nameof(model.ItemNo)), $"Already Exits.");
                _imex.ThrowIfContainsErrors();
            }

            var entity = await _db.OrderItems.FirstOrDefaultAsync(f => f.Id == model.Id);

            ValidateRecord(entity, model.Id);

            SetItemEntity(entity, model, Mode.EDIT);

            await _db.SaveChangesAsync();

            return model;
        });

        private async Task<int?> NextItemNoAsync(Guid? orderId)
        {
            var nextItemNo = await _db.OrderItems.Where(w => w.OrderId == orderId)
                    .Select(i => (int?)i.ItemNo).MaxAsync() ?? 0;
            return nextItemNo + 1;
        }

        private void ValidateIfNull(OrderItemVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(OrderItem entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }
    }
}