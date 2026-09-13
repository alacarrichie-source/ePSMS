//using iLgs.Exceptions;
//using iLgs.Models;
//using iLgs.Utilities;
//using System;
//using System.Data.Entity;
//using System.Linq;
//using System.Threading.Tasks;

//namespace iLgs.Services.PurchaseOrder
//{
//    public interface IOrderItemUnitGroupService
//    {
//        IQueryable<OrderItemUnitGroupVM> GetByOrderId(Guid? orderId);
//        ValueTask<OrderItemUnitGroup> GetSetLotInfoAsync(Guid? orderId, string setLotNo);
//        ValueTask<OrderItemUnitGroup> GetByIdAsync(Guid? id);
//        ValueTask<OrderItemUnitGroupVM> CreateAsync(OrderItemUnitGroupVM model, string user, DateTime date);
//        ValueTask<OrderItemUnitGroupVM> UpdateAsync(OrderItemUnitGroupVM model, string user, DateTime date);
//        ValueTask<OrderItemUnitGroupVM> DeleteAsync(OrderItemUnitGroupVM model, string user, DateTime date);

//        Task DistributeSetAmountAsync(Guid? orderId, string itemNo);

//        IOrderItemUnitGroupDescriptionService UnitGroupDescription { get; }
//    }

//    internal class OrderItemUnitGroupService : IOrderItemUnitGroupService
//    {
//        private readonly AppManEntities _db;
//        private readonly ICreateAndLogExceptions _exceptions;
//        private readonly IExceptionService<OrderItemUnitGroupVM> _vmExceptionService;
//        private readonly IExceptionService<OrderItemUnitGroup> _exceptionService;
//        private readonly IOrderSharedService _orderSharedService;
//        //private readonly IOrderItemUnitGroupDescriptionItemService _orderItemUnitGroupDescriptionItemService;

//        private IOrderItemUnitGroupDescriptionService _unitGroupDescriptionService;

//        public OrderItemUnitGroupService(AppManEntities db)
//        {
//            _db = db;
//            _exceptions = new CreateAndLogExceptions();
//            _vmExceptionService = new ExceptionService<OrderItemUnitGroupVM>();
//            _exceptionService = new ExceptionService<OrderItemUnitGroup>();
//            _orderSharedService = new OrderSharedService(_db);
//            _unitGroupDescriptionService = new OrderItemUnitGroupDescriptionService(_db);
//            //_orderItemUnitGroupDescriptionItemService = new OrderItemUnitGroupDescriptionItemService(_db);
//        }

//        //public OrderItemUnitGroupService(AppManEntities db,
//        //    IAppManEntitiesFactory appManEntitiesFactory,
//        //    ICreateAndLogExceptions exceptions,
//        //    IExceptionService<OrderItemUnitGroupVM> vmExceptionService,
//        //    IExceptionService<OrderItemUnitGroup> exceptionService,
//        //    IOrderSharedService orderSharedService,
//        //    IOrderItemUnitGroupDescriptionItemService orderItemUnitGroupDescriptionItemService)
//        //{
//        //    _db = db;
//        //    _contextFactory = appManEntitiesFactory;
//        //    _exceptions = exceptions;
//        //    _vmExceptionService = vmExceptionService;
//        //    _exceptionService = exceptionService;
//        //    _orderSharedService = orderSharedService;
//        //    _orderItemUnitGroupDescriptionItemService = orderItemUnitGroupDescriptionItemService;
//        //}

//        public IOrderItemUnitGroupDescriptionService UnitGroupDescription => _unitGroupDescriptionService;

//        public ValueTask<OrderItemUnitGroup> GetByIdAsync(Guid? id) =>
//        _exceptionService.TryCatch(async () =>
//        {
//            var data = await _db.OrderItemUnitGroups.FindAsync(id);
//            return data;
//        });

//        public  ValueTask<OrderItemUnitGroup> GetSetLotInfoAsync(Guid? orderId, string setLotNo) =>
//        _exceptionService.TryCatch(async () =>
//        {
//            var data = await _db.OrderItemUnitGroups.FirstOrDefaultAsync(f => f.OrderId == orderId && f.SetLotNo == setLotNo);
//            return data;
//        });


//        public IQueryable<OrderItemUnitGroupVM> GetByOrderId(Guid? orderId) =>
//        _vmExceptionService.TryCatch(() =>
//        {
//            var data = _db.OrderItemUnitGroups.AsNoTracking()
//                .Where(w => w.OrderId == orderId)
//                .Select(s => new OrderItemUnitGroupVM
//                {
//                    Id = s.Id,
//                    OrderId = s.OrderId,
//                    RequestItemUnitGroupId = s.RequestItemUnitGroupId,
//                    //Qty = s.RequestItemUnitGroup.RisItemUnitGroup.Qty,
//                    //Unit = s.RequestItemUnitGroup.RisItemUnitGroup.Unit,
//                    SetLotNo = s.SetLotNo,
//                    Qty = s.Qty,
//                    Unit = s.Unit,
//                    UnitCost = s.UnitCost,
//                    TotalCost = s.TotalCost,
//                    InsertedDt = s.InsertedDt
//                });
//            return data;
//        });

//        public ValueTask<OrderItemUnitGroupVM> CreateAsync(OrderItemUnitGroupVM model, string user, DateTime date) =>
//        _vmExceptionService.TryCatch(async () =>
//        {
//            if (await _orderSharedService.IsPostedAsync((Guid)model.OrderId))
//            {
//                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
//            }

//            model.Id = Guid.NewGuid();
//            model.InsertedBy = user;
//            model.UpdatedBy = user;
//            model.InsertedDt = date;
//            model.UpdatedDt = date;
//            model.TotalCost = model.Qty * model.UnitCost;

//            var entity = new OrderItemUnitGroup()
//            {
//                Id = model.Id,
//                OrderId = model.OrderId,
//                RequestItemUnitGroupId = model.RequestItemUnitGroupId,
//                SetLotNo = model.SetLotNo,
//                Qty = model.Qty,
//                Unit = model.Unit,
//                UnitCost = model.UnitCost,
//                TotalCost = model.TotalCost,
//                InsertedBy = model.InsertedBy,
//                InsertedDt = model.InsertedDt,
//                UpdatedBy = model.UpdatedBy,
//                UpdatedDt = model.UpdatedDt
//            };

//            _db.OrderItemUnitGroups.Add(entity);
//            await _db.SaveChangesAsync();

//            return model;
//        });

//        public ValueTask<OrderItemUnitGroupVM> DeleteAsync(OrderItemUnitGroupVM model, string user, DateTime date) =>
//        _vmExceptionService.TryCatch(async () =>
//        {
//            var entity = await _db.OrderItemUnitGroups.Where(w => w.Id == model.Id).FirstOrDefaultAsync();

//            if (entity == null)
//            {
//                throw new RecordNotFoundException(model.Id);
//            }

//            if (await _orderSharedService.IsPostedAsync((Guid)model.OrderId))
//            {
//                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
//            }

//            model.UpdatedBy = user;
//            model.UpdatedDt = date;

//            entity.UpdatedBy = model.UpdatedBy;
//            entity.UpdatedDt = model.UpdatedDt;

//            await _db.SaveChangesAsync();

//            _db.OrderItemUnitGroups.Remove(entity);
//            await _db.SaveChangesAsync();

//            return model;
//        });

//        public ValueTask<OrderItemUnitGroupVM> UpdateAsync(OrderItemUnitGroupVM model, string user, DateTime date) =>
//        _vmExceptionService.TryCatch(async () =>
//        {
//            var entity = await _db.OrderItemUnitGroups.Include(i => i.OrderItemUnitGroupDescriptions).Where(w => w.Id == model.Id).FirstOrDefaultAsync();
//            if (entity == null)
//            {
//                throw new RecordNotFoundException(model.Id);
//            }

//            if (await _orderSharedService.IsPostedAsync((Guid)model.OrderId))
//            {
//                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
//            }

//            model.UpdatedBy = user;
//            model.UpdatedDt = date;

//            entity.OrderId = model.OrderId;
//            entity.RequestItemUnitGroupId = model.RequestItemUnitGroupId;
//            entity.SetLotNo = model.SetLotNo;
//            entity.Qty = model.Qty;
//            entity.UnitCost = model.UnitCost;
//            entity.TotalCost = model.Qty * model.UnitCost;
//            entity.UpdatedBy = model.UpdatedBy;
//            entity.UpdatedDt = model.UpdatedDt;

//            await _db.SaveChangesAsync();
//            await DistributeSetAmountAsync(model.OrderId, model.SetLotNo);            

//            return model;
//        });

//        public async Task DistributeSetAmountAsync(Guid? orderId, string itemNo)
//        {
//            var unitGroup = await _db.OrderItemUnitGroups.AsNoTracking()
//                .Include(i => i.OrderItemUnitGroupDescriptions).FirstOrDefaultAsync(f => f.OrderId == orderId && f.SetLotNo == itemNo);
//            if (unitGroup != null)
//            {
//                var unitGroupDescriptions = unitGroup.OrderItemUnitGroupDescriptions.ToList();
//                foreach (var unitGroupDescription in unitGroupDescriptions)
//                {
//                    var unitGroupDescriptionItems = await _db.OrderItemUnitGroupDescriptionItems
//                        .Include(i => i.OrderItem)
//                        .Where(w => w.OrderItemUnitGroupDescriptionId == unitGroupDescription.Id).ToListAsync();
//                    foreach (var unitGroupDescriptionItem in unitGroupDescriptionItems)
//                    {
//                        var priceRate = unitGroupDescriptionItem.OrderItem.PriceRate ?? 0;
//                        var unitCost = unitGroupDescriptionItem.OrderItem.UnitCost ?? 0;
//                        await _unitGroupDescriptionService.UnitGroupDescriptionItem.UpdateOrderItemAsync(unitGroupDescriptionItem.OrderItemId, priceRate, unitCost, unitGroup.UpdatedBy, (DateTime)unitGroup.UpdatedDt);
//                    }
//                }
//            }
//        }
//    }
//}