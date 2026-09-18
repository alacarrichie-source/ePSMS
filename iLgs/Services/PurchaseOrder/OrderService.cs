using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.AllFields;
using iLgs.Services.Codes;
using iLgs.Services.Items;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.PurchaseOrder
{
    public interface IOrderService
    {
        IQueryable<OrderVM> GetAll();
        Task<IQueryable<OrderVM>> GetAllAsync(string userId);
        Task<IQueryable<OrderVM>> GetAllForAirAsync(string userId);
        IQueryable<OrderVM> GetAllParOrders();
        ValueTask<Models.Order> GetByIdAsync(Guid orderId);
        ValueTask<Models.Order> GetByPoNoAsync(string poNo);
        bool GetAnyPoNo(Guid id, string poNo);
        ValueTask<bool> GetAnyPoNoAsync(Guid id, string poNo);
        ValueTask<int> GetNotPostedAsync(DateTime asOf);
        IQueryable<PrItemSelectionVM> GetPrItemSelection(Guid? orderId);
        IQueryable<PrItemSelectionVM> GetItemPR(Guid? orderItemId);

        string GetDepartment(List<Request> requestList);
        string GetDeliveryPlace(List<Request> requestList);
        string GetPrNo(List<Request> requestList);
        string GetFund(List<Request> requestList);

        ValueTask<OrderVM> CreateAsync(OrderVM model, string user, DateTime date);
        ValueTask<OrderVM> UpdateAsync(OrderVM model, string user, DateTime date);
        ValueTask<OrderVM> DeleteAsync(OrderVM model, string user, DateTime date);
        ValueTask<Order> PostAsync(Guid orderId, string user, DateTime date);
        ValueTask<Order> UnpostAsync(Guid orderId, string user, DateTime date);

        Guid CreatePurchaseOrder(OrderVM header, IEnumerable<OrderItemVM> initialItems, string user, DateTime date);
        ValueTask<OrderVM> ItemSelectionSaveAsync(Guid? orderId, string selectedIds, string user, DateTime date);

        IOrderItemService OrderItem { get; }
        //IOrderItemUnitGroupService UnitGroup { get; }
    }

    public class OrderService : BaseValidator, IOrderService
    {
        private readonly decimal? _priceCap;
        private readonly AppManEntities _db;
        //private readonly IAppManEntitiesFactory _contextFactory;
        private readonly IOrderSharedService _orderSharedService;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<OrderVM> _orderVmExceptionService;
        private readonly IExceptionService<Order> _orderExceptionService;
        private readonly IAllFieldService _allFieldService;
        private readonly IItemCodeService _itemCodeService;
        private readonly IUserService _userService;
        private readonly IOrderUploadService _uploadPoService;
        private readonly IOrderUploadService _uploadCafoaService;
        private readonly IPriceCapService _priceCapService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        private IOrderItemService _orderItemService;
        //private IOrderItemUnitGroupService _unitGroupService;

        public OrderService(AppManEntities db)
        {
            _db = db;
            _orderSharedService = new OrderSharedService(_db);
            _exceptions = new CreateAndLogExceptions();
            _allFieldService = new AllFieldService(_db);
            _itemCodeService = new ItemCodeService(_db);
            _userService = new UserService(_db);
            _uploadPoService = new OrderUploadService(_db);
            _uploadCafoaService = _uploadPoService.Create("CAFOA");
            _priceCapService = new PriceCapService(_db);

            _orderVmExceptionService = new ExceptionService<OrderVM>();
            _orderExceptionService = new ExceptionService<Order>();
            _getDisplayName = propertyName => Utility.GetDisplayName<OrderVM>(propertyName);

            _priceCap = _priceCapService.GetPriceCap();

            _orderItemService = new OrderItemService(_db);
            //_unitGroupService = new OrderItemUnitGroupService(_db);
        }

        //public OrderService(AppManEntities db,
        //    IAppManEntitiesFactory appManEntitiesFactory,
        //    IOrderSharedService orderSharedService,
        //    ICreateAndLogExceptions exceptions,
        //    IExceptionService<OrderVM> orderVmExceptionService,
        //    IExceptionService<Order> orderExceptionService,
        //    IAllFieldService allFieldService,
        //    IItemCodeService itemCodeService,
        //    IUserService userService,
        //    IOrderUploadService uploadService,
        //    IPriceCapService priceCapService)
        //{
        //    _db = db;
        //    _contextFactory = appManEntitiesFactory;
        //    _orderSharedService = orderSharedService;
        //    _exceptions = exceptions;
        //    _orderVmExceptionService = orderVmExceptionService;
        //    _orderExceptionService = orderExceptionService;
        //    _allFieldService = allFieldService;
        //    _itemCodeService = itemCodeService;            
        //    _userService = userService;
        //    _uploadPoService = uploadService;
        //    _uploadCafoaService = uploadService.Create("CAFOA");
        //    _priceCapService = priceCapService;
        //    _getDisplayName = propertyName => Utility.GetDisplayName<OrderVM>(propertyName);

        //    _priceCap = _priceCapService.GetPriceCap();
        //}

        public IOrderItemService OrderItem => _orderItemService;
        //public IOrderItemUnitGroupService UnitGroup => _unitGroupService;

        private static Expression<Func<Order, OrderVM>> Projection(AppManEntities db)
        {
            return s => new OrderVM
            {
                Id = s.Id,
                CtrlNo = s.CtrlNo,
                PrNo = s.PrNo,
                PrNoList = s.OrderRequests.OrderBy(o => o.Request.PrNo).Select(s2 => s2.Request.PrNo).ToList(),
                //PrDate = s.Request.PrDate,
                DeptId = s.DeptId,
                Department = s.Department,
                Fund = s.Fund,
                //PrId = s.PrId,
                //PrNo = s.Request.PrNo,
                //PrDate = s.Request.PrDate,
                //Department = s.Request.Department,
                //Fund = s.Request.Fund,                                
                PoNo = s.PoNo,
                PoDate = s.PoDate,
                PoMode = s.PoMode,
                PoModeDesc = db.Codextns.Where(w => w.Code == s.PoMode && w.CodeMast.Code == "PROC-MODE").FirstOrDefault().Description,
                SupplierId = s.SupplierId,
                SupName = s.SupName,
                SupBusiness = s.SupBusiness,
                SupAddress = s.SupAddress,
                SupTIN = s.SupTIN,
                SupContactNo = s.SupContactNo,
                SupEmail = s.SupEmail,
                SupZipCode = s.SupZipCode,
                DeliveryPlace = s.DeliveryPlace,
                DeliveryDate = s.DeliveryDate,
                TermDelivery = s.TermDelivery,
                TermPayment = s.TermPayment,
                SignedBySuppName = s.SignedBySuppName,
                SignedBySuppDate = s.SignedBySuppDate,
                SignedByAuthName = s.SignedByAuthName,
                SignedByAuthDesignation = s.SignedByAuthDesignation,
                ResoNo = s.ResoNo,
                CertifiedCorrectBy = s.CertifiedCorrectBy,
                CertifiedCorrectDate = s.CertifiedCorrectDate,
                PostedBy = s.PostedBy,
                PostedDt = s.PostedDt,
                IsLocked = false,
                InsertedDt = s.InsertedDt
            };
        }

        public IQueryable<OrderVM> GetAll()
        {
            var data = _db.Orders.AsNoTracking()
                .Select(Projection(_db))
                .AsQueryable();
            return data;
        }

        public async Task<IQueryable<OrderVM>> GetAllAsync(string userId)
        {
            IQueryable<OrderVM> data = null;
            if (await _userService.IsAdminAsync(userId))
            {
                data = _db.Orders.AsNoTracking()
                    .Select(Projection(_db)).OrderByDescending(o => o.PoNo);
            }
            else
            {
                data = _db.Orders.AsNoTracking()
                    .Where(w => w.OrderItems.Any(a =>
                        a.OrderItemRequests.Any(b =>
                            b.RequestItem.Request.Codextn.DepartmentUsers.Any(c =>
                                c.UserId == userId)))).Select(Projection(_db)).OrderByDescending(o => o.PoNo);
            }
            return data;
        }

        public async Task<IQueryable<OrderVM>> GetAllForAirAsync(string userId)
        {
            IQueryable<OrderVM> data = _db.Orders.Where(w => w.OrderItems
                .Any(a => a.OrderItemRequests
                    .Any(b => (b.AIRItems.Sum(s => s.Qty) ?? 0) < b.QtyApplied))).AsNoTracking().Select(Projection(_db));
            if (!await _userService.IsAdminAsync(userId))
            {
                data = _db.Orders.AsNoTracking()
                    .Where(w => w.OrderItems.Any(a =>
                        a.OrderItemRequests.Any(b =>
                            b.RequestItem.Request.Codextn.DepartmentUsers.Any(c =>
                                c.UserId == userId)))).Select(Projection(_db));
            }            
            return data;
        }

        public IQueryable<OrderVM> GetAllParOrders() => _orderVmExceptionService.TryCatch(() =>
        {
            var data = _db.Orders.AsNoTracking()
                .Where(w => w.PostedBy != null && w.AIRs.Any(a => a.PostedBy != null))
                .Select(s => new OrderVM
                {
                    Id = s.Id,
                    PoNo = s.PoNo,
                    PoDate = s.PoDate,
                    PoMode = s.PoMode,
                    SupplierId = s.SupplierId,
                    SupName = s.SupName,
                    SupBusiness = s.SupBusiness,
                    SupAddress = s.SupAddress,
                    SupTIN = s.SupTIN,
                    SupContactNo = s.SupContactNo,
                    SupEmail = s.SupEmail,
                    SupZipCode = s.SupZipCode,
                    Department = s.Department
                })
                .AsQueryable();
            return data;
        });

        public ValueTask<Order> GetByIdAsync(Guid orderId) => _orderExceptionService.TryCatch(async () =>
        {
            return await _db.Orders.FindAsync(orderId);
        });

        public async ValueTask<bool> GetAnyPoNoAsync(Guid id, string poNo)
        {
            return await _db.Orders.AnyAsync(a => a.Id != id && a.PoNo == poNo);
        }

        public bool GetAnyPoNo(Guid id, string poNo)
        {
            return _db.Orders.Any(a => a.Id != id && a.PoNo == poNo);
        }

        public ValueTask<Order> GetByPoNoAsync(string poNo) => _orderExceptionService.TryCatch(async () =>
        {
            return await _db.Orders.Where(w => w.PoNo == poNo).FirstOrDefaultAsync();
        });        

        public IQueryable<PrItemSelectionVM> GetPrItemSelection(Guid? orderId)
        {
            var data = _db.RequestItems.AsNoTracking().Where(w => w.Request.OrderRequests.Any(a => a.OrderId == orderId))
                .Select(s => new PrItemSelectionVM
                {
                    Id = s.Id,
                    PrId = s.PrId,
                    ItemNo = s.ItemNo,
                    ItemNoIndex = s.ItemNoIndex,
                    Description = s.Description,
                    OtherDesc = s.OtherDesc,
                    Remarks = s.Remarks,
                    Qty = s.Qty,
                    Unit = s.Unit,
                    UnitCost = s.UnitCost,
                    TotalCost = s.TotalCost,
                    PriceRate = s.PriceRate,
                    PpmpCode = s.PpmpCode,
                    InsertedBy = s.InsertedBy,
                    InsertedDt = s.InsertedDt,
                    Padding = (s.ItemNo.Length - s.ItemNo.Replace(".", "").Length) * 20,
                    IsSetLotItem = s.ItemNo.Contains("."),
                    Request = s.Request,
                    IsSelected = s.OrderItemRequests.Any(a => a.OrderItem.OrderId == orderId),
                    ParentItemNo = s.ItemNo.Contains(".") ? s.ItemNo.Substring(0, s.ItemNo.IndexOf(".")) : s.ItemNo,
                    PoCtrlNo = s.OrderItemRequests.FirstOrDefault().OrderItem.Order.CtrlNo,
                    PoNo = s.OrderItemRequests.FirstOrDefault().OrderItem.Order.PoNo,
                    IsAvailable = s.OrderItemRequests.Any(a => a.OrderItem.OrderId == orderId) || !s.OrderItemRequests.Any()
                });
            return data;
        }

        public IQueryable<PrItemSelectionVM> GetItemPR(Guid? orderItemId)
        {
            var data = _db.RequestItems.AsNoTracking().Where(w => w.OrderItemRequests.Any(a => a.OrderItemId == orderItemId))
                .Select(s => new PrItemSelectionVM
                {
                    Id = s.Id,
                    PrId = s.PrId,
                    ItemNo = s.ItemNo,
                    ItemNoIndex = s.ItemNoIndex,
                    Description = s.Description,
                    OtherDesc = s.OtherDesc,
                    Remarks = s.Remarks,
                    Qty = s.Qty,
                    Unit = s.Unit,
                    UnitCost = s.UnitCost,
                    TotalCost = s.TotalCost,
                    PriceRate = s.PriceRate,
                    PpmpCode = s.PpmpCode,
                    InsertedBy = s.InsertedBy,
                    InsertedDt = s.InsertedDt,
                    Padding = (s.ItemNo.Length - s.ItemNo.Replace(".", "").Length) * 20,
                    IsSetLotItem = s.ItemNo.Contains("."),
                    Request = s.Request,
                    IsSelected = s.OrderItemRequests.Any(),
                    ParentItemNo = s.ItemNo.Contains(".") ? s.ItemNo.Substring(0, s.ItemNo.IndexOf(".")) : s.ItemNo
                });
            return data;
        }

        public bool IsPosted(Guid orderId)
        {
            return _orderSharedService.IsPosted(orderId);
        }

        public bool IsPosted(Order order)
        {
            return _orderSharedService.IsPosted(order);
        }

        public bool IsPosted(OrderItem orderItem)
        {
            return _orderSharedService.IsPosted(orderItem);
        }

        //public bool IsPosted(OrderItemUnitGroup orderItemUnitGroup)
        //{
        //    return _orderSharedService.IsPosted(orderItemUnitGroup);
        //}

        //public bool IsPosted(OrderItemUnitGroupDescription orderItemUnitGroupDescription)
        //{
        //    return _orderSharedService.IsPosted(orderItemUnitGroupDescription);
        //}

        //public bool IsPosted(OrderItemUnitGroupDescriptionItem orderItemUnitGroupDescriptionItem)
        //{
        //    return _orderSharedService.IsPosted(orderItemUnitGroupDescriptionItem);
        //}

        private async Task<bool> IsPostedAsync(Guid orderId)
        {
            return await _orderSharedService.IsPostedAsync(orderId);
        }

        public async ValueTask<int> GetNotPostedAsync(DateTime asOf)
        {
            return await _db.Orders.Where(w => w.PoDate <= asOf && w.PostedDt == null).CountAsync();
        }

        public Guid CreatePurchaseOrder(OrderVM header, IEnumerable<OrderItemVM> initialItems, string user, DateTime date)
        {
            if (header == null) throw new ArgumentNullException(nameof(header));

            using (DbContextTransaction transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    var poEntity = new Order
                    {
                        Id = header.Id == null ? Guid.NewGuid() : header.Id,
                        CtrlNo = NextCtrlNo(date),
                        //PoNumber = header.PoNumber ?? GeneratePoNumber(),
                        //PoDate = header.PoDate == default ? DateTime.UtcNow : header.PoDate,
                        //Supplier = header.Supplier,
                        //SupplierAddress = header.SupplierAddress,
                        //ModeOfProcurement = header.ModeOfProcurement ?? "Public Bidding",
                        //Status = "Draft",
                        //CreatedDate = DateTime.UtcNow,
                        //TotalAmount = 0,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };

                    _db.Orders.Add(poEntity);
                    _db.SaveChanges();

                    int itemIdx = 1;
                    decimal? runningTotal = 0;

                    if (initialItems != null)
                    {
                        foreach (var itm in initialItems)
                        {
                            var lineEntity = new OrderItem
                            {
                                Id = Guid.NewGuid(),
                                OrderId = poEntity.Id,
                                ItemNo = (itemIdx++).ToString(),
                                PpmpCode = itm.PpmpCode,
                                Description = itm.Description,
                                Unit = itm.Unit,
                                Qty = itm.Qty,
                                UnitCost = itm.UnitCost,
                                //SourcePrs = itm.SourcePrs,
                                //TechnicalSpecs = itm.TechnicalSpecs,
                                InsertedBy = user,
                                InsertedDt = date,
                                UpdatedBy = user,
                                UpdatedDt = date
                            };
                            _db.OrderItems.Add(lineEntity);
                            runningTotal += (itm.Qty * itm.UnitCost);
                        }
                    }

                    //poEntity.TotalAmount = runningTotal;
                    _db.SaveChanges();
                    transaction.Commit();

                    return poEntity.Id;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    //_auditLogger.LogError("Error creating Purchase Order in EF6", ex);
                    throw;
                }
            }
        }

        public ValueTask<OrderVM> CreateAsync(OrderVM model, string user, DateTime date) => _orderVmExceptionService.TryCatch((Func<ValueTask<OrderVM>>)(async () =>
        {
            ValidateIfNull(model);
            await ValidateOnCreateUpdateAsync(model, Mode.ADD);

            model.Id = Guid.NewGuid();
            model.CtrlNo = NextCtrlNo(date);
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            // Load PrId where RequestItem without OrderItemRequest            
            var requestList = await _db.Requests.Include(i => i.RequestItems)
                .Where(w => model.PrNoList.Contains(w.PrNo) && w.RequestItems.Any(a => !a.OrderItemRequests.Any()))
                .OrderBy(o => o.PrNo).ToListAsync();            
            var order = new Order();
            SetModel(model, requestList);
            MapModelToEntityFields(order, model, Mode.ADD, requestList);

            ProcessOrderRequest(order, requestList, user, date);
            // load actual list, exclude items used by other PO.
            var requestItemList = requestList.SelectMany(r => r.RequestItems)
                //.Where(w => !w.OrderItemRequests.Any())
                .Where(w => !_db.OrderItemRequests.Any(a => a.RequestItemId == w.Id))
                .ToList();
            ProcessOrderItems(order, requestList, requestItemList, user, date);

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            return model;
        }));

        public ValueTask<OrderVM> UpdateAsync(OrderVM model, string user, DateTime date) => _orderVmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var order = await _db.Orders.FindAsync(model.Id);
            ValidateRecord(order, model.Id);
            await ValidateStatusAsync(model.Id);
            await ValidateOnCreateUpdateAsync(model, Mode.EDIT);            

            var requestList = await _db.Requests.Include(i => i.RequestItems)
                .Where(w => model.PrNoList.Contains(w.PrNo))
                .OrderBy(o => o.PrNo).ToListAsync();
            SetModel(model, requestList);
            if (model.PrNo != order.PrNo)
            {
                MapModelToEntityFields(order, model, Mode.EDIT, requestList);

                //await DeleteUnitGroupAsync(model.Id);
                await DeleteOrderItemsAsync(model.Id); // delete existing orderitems            
                ProcessOrderRequest(order, requestList, user, date);

                // load actual list, exclude request items used by other PO
                var requestItemList = requestList.SelectMany(r => r.RequestItems)
                    .Where(w => !_db.OrderItemRequests.Any(a => a.RequestItemId == w.Id))
                    .ToList();
                ProcessOrderItems(order, requestList, requestItemList, user, date);
            }
            else
            {
                MapModelToEntityFields(order, model, Mode.EDIT, requestList);
            }            

            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<OrderVM> ItemSelectionSaveAsync(Guid? orderId, string selectedIds, string user, DateTime date) => _orderVmExceptionService.TryCatch(async () =>
        {
            var model = await GetAll().Where(w => w.Id == orderId).FirstOrDefaultAsync();
            var order = await _db.Orders.FindAsync(model.Id);
            ValidateRecord(order, (Guid)orderId);
            await ValidateStatusAsync((Guid)model.Id);

            //await DeleteUnitGroupAsync(model.Id);
            var orderItems = _db.OrderItems.Where(w => w.OrderId == orderId);
            _db.OrderItems.RemoveRange(orderItems);
            await _db.SaveChangesAsync();            

            var prNoList = order.PrNo.Split(',').Select(x => x.Trim()).ToList();
            var selectedIdList = selectedIds.Split(',').ToList(); // PR ItemId            
            var requestList = await _db.Requests.Include(i => i.RequestItems)
                .Where(w => prNoList.Contains(w.PrNo)
                    && w.RequestItems.Any(a => selectedIdList.Contains(a.Id.ToString())))
                .OrderBy(o => o.PrNo).ToListAsync();

            // load selected items, not in other PO
            var requestItemList = await _db.RequestItems
                .Where(w => selectedIdList.Contains(w.Id.ToString()) 
                    && !w.OrderItemRequests.Any(a => a.OrderItem.OrderId != orderId))
                .ToListAsync();
            
            //MapModelToEntityFields(order, model, Mode.ADD, requestList); // do not remove original PR No Selections
            ProcessOrderItems(order, requestList, requestItemList, user, date);

            await _db.SaveChangesAsync();

            return model;
        });

        public void ProcessOrderRequest(Order order, List<Request> requestList, string user, DateTime date)
        {
            foreach (var request in requestList)
            {
                var orderRequest = new OrderRequest()
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    PrId = request.Id,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };

                order.OrderRequests.Add(orderRequest);
            }
        }

        public void ProcessOrderItems(Order order, List<Request> requestList,  List<RequestItem> requestItemList, string user, DateTime date)
        {
            decimal itemNo = 0;
            var itemNoIndex = string.Empty;
            var requestItemGroups = requestItemList.Where(w => !(w.Unit == "set" || w.Unit == "lot" || w.ItemNo.Contains("."))).GroupBy(g => new { g.PpmpCode, g.Description, g.Unit })
                    .Select(s => new
                    {
                        s.Key.PpmpCode,
                        s.Key.Description,
                        s.Key.Unit,
                        UnitCost = s.Max(x => x.UnitCost),
                        Qty = s.Sum(x => x.Qty),
                        TotalCost = s.Max(x => x.UnitCost) * s.Sum(x => x.Qty)
                    }).ToList();
                    
            foreach (var requestItemGroup in requestItemGroups)
            {
                itemNo++;
                var orderItemId = Guid.NewGuid();
                var orderItem = new OrderItem()
                {
                    Id = orderItemId,
                    OrderId = order.Id,
                    ItemNo = itemNo.ToString(),
                    ItemNoIndex = Utility.GetItemNoIndex(itemNo),
                    Description = requestItemGroup.Description,
                    //OtherDesc = requestItem.OtherDesc,
                    Unit = requestItemGroup.Unit,
                    Qty = requestItemGroup.Qty,
                    UnitCost = requestItemGroup.UnitCost,
                    Amount = requestItemGroup.TotalCost,
                    PriceRate = null,
                    PpmpCode = requestItemGroup.PpmpCode,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date,
                    AllField = new AllField() { Id = orderItemId }
                };
                
                var requestItems = requestItemList.Where(w => w.Description == requestItemGroup.Description
                    && w.PpmpCode == requestItemGroup.PpmpCode && w.Unit == requestItemGroup.Unit).ToList();

                foreach (var requestItem in requestItems)
                {
                    var orderItemRequest = new OrderItemRequest()
                    {
                        Id = Guid.NewGuid(),
                        OrderItemId = orderItemId,
                        RequestItemId = requestItem.Id,
                        QtyApplied = (int?)requestItem.Qty,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };
                    orderItem.OrderItemRequests.Add(orderItemRequest);
                }

                order.OrderItems.Add(orderItem);
            }

            decimal itemSetNo = 0;
            var sIndexNo = string.Empty;
            var setList = requestItemList.Where(w => w.Unit == "set" || w.Unit == "lot" || w.ItemNo.Contains(".")).OrderBy(o => o.ItemNoIndex).ToList();
            foreach (var set in setList)
            {
                if (set.ItemNo.Contains("."))
                {
                    itemSetNo++;
                    sIndexNo = $"{itemNo}.{itemSetNo}";
                }
                else
                {
                    itemSetNo = 0;
                    itemNo++;
                    sIndexNo = $"{itemNo}";
                }
                
                var orderItemId = Guid.NewGuid();
                var orderItem = new OrderItem()
                {
                    Id = orderItemId,
                    OrderId = order.Id,
                    ItemNo = sIndexNo,
                    ItemNoIndex = Utility.GetItemNoIndex(sIndexNo),
                    Description = set.Description,
                    Unit = set.Unit,
                    Qty = set.Qty,
                    UnitCost = set.UnitCost,
                    Amount = set.TotalCost,
                    PriceRate = null,
                    PpmpCode = set.PpmpCode,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date,
                    AllField = new AllField() { Id = orderItemId }
                };

                var orderItemRequest = new OrderItemRequest()
                {
                    Id = Guid.NewGuid(),
                    OrderItemId = orderItemId,
                    RequestItemId = set.Id,
                    QtyApplied = (int?)set.Qty,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };

                orderItem.OrderItemRequests.Add(orderItemRequest);
                order.OrderItems.Add(orderItem);                
            }

            //var setItems = order.OrderItems.Where(w => w.Unit == "set" || w.Unit == "lot").OrderBy(o => o.ItemNoIndex).ToList();
            //foreach (var setItem in setItems)
            //{
            //    var unitGroupId = Guid.NewGuid();
            //    var unitGroup = new OrderItemUnitGroup()
            //    {
            //        Id = unitGroupId,
            //        OrderId = order.Id,
            //        SetLotNo = setItem.ItemNo,
            //        Qty = (int?)setItem.Qty,
            //        Unit = setItem.Unit,
            //        UnitCost = setItem.UnitCost,
            //        TotalCost = setItem.Amount,
            //        InsertedBy = user,
            //        InsertedDt = date,
            //        UpdatedBy = user,
            //        UpdatedDt = date
            //    };
            //    var unitGroupDescriptionId = Guid.NewGuid();
            //    var unitGroupDescription = new OrderItemUnitGroupDescription()
            //    {
            //        Id = unitGroupDescriptionId,
            //        OrderItemUnitGroupId = unitGroupId,
            //        Description = setItem.Description,
            //        OtherParticulars = setItem.OtherDesc,
            //        InsertedBy = user,
            //        InsertedDt = date,
            //        UpdatedBy = user,
            //        UpdatedDt = date
            //    };

            //    var setItemContents = order.OrderItems.Where(w => w.ItemNoIndex.Substring(0, 3) == setItem.ItemNoIndex.Substring(0, 3)
            //        && !(w.Unit == "set" || w.Unit == "lot" || w.Unit == "" || w.Unit == null)).OrderBy(o => o.ItemNoIndex).ToList();
            //    foreach (var setItemContent in setItemContents)
            //    {
            //        var orderItemId = setItemContent.Id;
            //        var unitGroupDescriptionItem = new OrderItemUnitGroupDescriptionItem()
            //        {
            //            Id = Guid.NewGuid(),
            //            OrderItemUnitGroupDescriptionId = unitGroupDescriptionId,
            //            OrderItemId = orderItemId,
            //            InsertedBy = user,
            //            InsertedDt = date,
            //            UpdatedBy = user,
            //            UpdatedDt = date
            //        };
            //        unitGroupDescription.OrderItemUnitGroupDescriptionItems.Add(unitGroupDescriptionItem);
            //    }
            //    unitGroup.OrderItemUnitGroupDescriptions.Add(unitGroupDescription);
            //    order.OrderItemUnitGroups.Add(unitGroup);
            //}


            //var setItems = requestItems.Where(w => w.Unit == "set" || w.Unit == "lot").ToList();
            //foreach (var setItem in setItems)
            //{
            //    var unitGroupId = Guid.NewGuid();
            //    var unitGroup = new OrderItemUnitGroup()
            //    {
            //        Id = unitGroupId,
            //        OrderId = entity.Id,
            //        SetLotNo = setItem.ItemNo,
            //        Qty = (int?)setItem.Qty,
            //        Unit = setItem.Unit,
            //        UnitCost = setItem.UnitCost,
            //        TotalCost = setItem.TotalCost,
            //        InsertedBy = user,
            //        InsertedDt = date,
            //        UpdatedBy = user,
            //        UpdatedDt = date
            //    };
            //    var unitGroupDescriptionId = Guid.NewGuid();
            //    var unitGroupDescription = new OrderItemUnitGroupDescription()
            //    {
            //        Id = unitGroupDescriptionId,
            //        OrderItemUnitGroupId = unitGroupId,
            //        Description = setItem.Description,
            //        OtherParticulars = setItem.OtherDesc,
            //        InsertedBy = user,
            //        InsertedDt = date,
            //        UpdatedBy = user,
            //        UpdatedDt = date
            //    };

            //    var setItemContents = requestItems.Where(w => w.ItemNoIndex.Substring(0, 3) == setItem.ItemNoIndex.Substring(0, 3)
            //        && !(w.Unit == "set" || w.Unit == "lot" || w.Unit == "" || w.Unit == null)).ToList();
            //    foreach (var setItemContent in setItemContents)
            //    {
            //        var orderItemId = entity.OrderItems.First(f => f.RequestItemId == setItemContent.Id).Id;
            //        var unitGroupDescriptionItem = new OrderItemUnitGroupDescriptionItem()
            //        {
            //            Id = Guid.NewGuid(),
            //            OrderItemUnitGroupDescriptionId = unitGroupDescriptionId,
            //            OrderItemId = orderItemId,
            //            InsertedBy = user,
            //            InsertedDt = date,
            //            UpdatedBy = user,
            //            UpdatedDt = date
            //        };
            //        unitGroupDescription.OrderItemUnitGroupDescriptionItems.Add(unitGroupDescriptionItem);
            //    }
            //    unitGroup.OrderItemUnitGroupDescriptions.Add(unitGroupDescription);
            //    entity.OrderItemUnitGroups.Add(unitGroup);
            //}            
        }

        //private async Task DeleteOderItemsAsync(Guid? orderId)
        //{
        //    var orderItems = _db.OrderItems.Where(w => w.OrderId == orderId);
        //    _db.OrderItems.RemoveRange(orderItems);
        //    await _db.SaveChangesAsync();
        //}

        //private async Task DeleteUnitGroupAsync(Guid? orderId)
        //{
        //    var unitGroups = _db.OrderItemUnitGroups.Where(w => w.OrderId == orderId);
        //    _db.OrderItemUnitGroups.RemoveRange(unitGroups);            
        //    await _db.SaveChangesAsync();
        //}

        private async Task DeleteOrderItemsAsync(Guid? orderId)
        {
            var orderRequest = _db.OrderRequests.Where(w => w.OrderId == orderId);
            _db.OrderRequests.RemoveRange(orderRequest);

            var orderItems = _db.OrderItems.Where(w => w.OrderId == orderId);
            _db.OrderItems.RemoveRange(orderItems);
            
            await _db.SaveChangesAsync();
        }

        private void SetModel(OrderVM model, List<Request> requestList)
        {
            model.Department = GetDepartment(requestList);
            model.DeliveryPlace = GetDeliveryPlace(requestList);
            model.PrNo = GetPrNo(requestList);
            model.Fund = GetFund(requestList);
        }

        public string GetDepartment(List<Request> requestList)
        {
            return string.Join(" / ", requestList.Select(s => s.Department));
        }

        public string GetDeliveryPlace(List<Request> requestList)
        {
            return string.Join(" / ", requestList.Select(s => s.Department));
        }

        public string GetPrNo(List<Request> requestList)
        {
            return string.Join(" / ", requestList.Select(s => s.PrNo));
        }

        public string GetFund(List<Request> requestList)
        {
            return string.Join(", ", requestList.GroupBy(g => g.Fund).Select(s => s.Key));
        }

        private void MapModelToEntityFields(Order entity, OrderVM model, Mode mode, List<Request> requestList)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.CtrlNo = model.CtrlNo;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }            

            entity.Department = model.Department;
            entity.DeliveryPlace = model.DeliveryPlace;
            entity.PrNo = model.PrNo;
            entity.Fund = model.Fund;

            //entity.PrNo = model.PrNo;            
            entity.DeptId = model.DeptId;
            //entity.Department = model.Department?.Trim();
            //entity.Section = model.Section?.Trim();
            //entity.FPP = model.FPP?.Trim();
            //entity.Fund = model.Fund?.Trim().ToUpper();
            //entity.FundSpecific = model.FundSpecific;
            //entity.PrNo = model.PrNo;
            //entity.PrDate = model.PrDate;

            entity.SupplierId = model.SupplierId;
            entity.SupName = model.SupName?.Trim();
            entity.SupBusiness = model.SupBusiness?.Trim();
            entity.SupAddress = model.SupAddress?.Trim();
            entity.SupContactNo = model.SupContactNo?.Trim();
            entity.SupZipCode = model.SupZipCode?.Trim();
            entity.SupEmail = model.SupEmail?.Trim();
            entity.SupTIN = model.SupTIN?.Trim();
            entity.PoNo = model.PoNo;
            entity.PoDate = model.PoDate;
            entity.PoMode = model.PoMode;
            //entity.PrId = model.PrId;
            //entity.DeliveryPlace = model.DeliveryPlace?.Trim();
            entity.DeliveryDate = model.DeliveryDate;
            entity.TermDelivery = model.TermDelivery?.Trim();
            entity.TermPayment = model.TermPayment;
            entity.SignedByAuthDesignation = model.SignedByAuthDesignation?.Trim();
            entity.SignedByAuthName = model.SignedByAuthName?.Trim();
            entity.SignedBySuppDate = model.SignedBySuppDate;
            entity.SignedBySuppName = model.SignedBySuppName?.Trim();
            entity.ResoNo = model.ResoNo?.Trim();
            entity.CertifiedCorrectBy = model.CertifiedCorrectBy?.Trim();
            entity.CertifiedCorrectDate = model.CertifiedCorrectDate;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        public ValueTask<OrderVM> DeleteAsync(OrderVM model, string user, DateTime date) =>
        _orderVmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            await ValidateStatusAsync(model.Id);

            //var unitGroups = _db.OrderItemUnitGroups.Where(w => w.OrderId == model.Id);
            //if (unitGroups.Any())
            //{
            //    _db.OrderItemUnitGroups.RemoveRange(unitGroups);
            //    await _db.SaveChangesAsync();
            //}

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.Orders.FindAsync(model.Id);
            ValidateRecord(entity, model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            await _db.SaveChangesAsync();

            _db.Orders.Remove(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<Order> PostAsync(Guid orderId, string user, DateTime date) => _orderExceptionService.TryCatch(async () =>
        {
            var entity = await _db.Orders.Where(w => w.Id == orderId).FirstOrDefaultAsync();

            ValidateRecord(entity, orderId);
            await ValidateOnPostAsync(entity);
            await ValidateUploadAsync(orderId, entity.PoNo);

            entity.PostedBy = user;
            entity.PostedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            await _db.SaveChangesAsync();

            return entity;
        });

        public ValueTask<Order> UnpostAsync(Guid orderId, string user, DateTime date) => _orderExceptionService.TryCatch(async () =>
            await new PurchaseOrderLifecycleService(_db).UnpostAsync(orderId, user, date));

        private string NextPoNo(DateTime poDate)
        {
            string yyyy = poDate.Year.ToString().Trim();
            string mm = poDate.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var order = _db.Orders.Where(w => w.PoDate.Value.Year == poDate.Year).OrderByDescending(o => o.PoNo).FirstOrDefault();
            if (order == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(order.PoNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }

        private string NextCtrlNo(DateTime date)
        {
            string yyyy = date.Year.ToString().Trim();
            string mm = date.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var order = _db.Orders.Where(w => w.CtrlNo.Substring(0, 4) == yyyy).OrderByDescending(o => o.CtrlNo).FirstOrDefault();
            if (order == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(order.CtrlNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }

        #region VALIDATION

        private void ValidateIfNull(OrderVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(Order entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }

        private void ValidateIfPosted(Order entity)
        {
            if (entity.PostedDt != null)
            {
                var msg = $"Record already posted by {entity.PostedBy} on {entity.PostedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        private async Task ValidateOnCreateUpdateAsync(OrderVM model, Mode mode)
        {
            _imex = new InvalidModelException();
            if (model.PrNoList == null || !model.PrNoList.Any())
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.PrNoList)), "Field is required.");
            }
            else
            {
                var existingPrNos = await _db.Requests
                    .Where(r => model.PrNoList.Contains(r.PrNo))
                    .Select(r => r.PrNo)
                    .ToListAsync();

                var missingPrNos = model.PrNoList.Except(existingPrNos).ToList();

                if (missingPrNos.Any())
                {
                    var prNoList = string.Join(", ", missingPrNos);
                    _imex.UpsertDataList(_getDisplayName(nameof(model.PrNoList)), $"{prNoList} not found.");
                }

                var funds = await _db.Requests.Where(w => model.PrNoList.Contains(w.PrNo)).GroupBy(g => g.Fund).ToListAsync();
                if (funds.Count() > 1)
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.Fund)), "Multiple funds are not allowed.");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.PoNo))
            {
                if (model.PoNo.Trim().Length != 12)
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), "Invalid value.");
                }
                else
                {
                    if (!model.PoDate.HasValue)
                    {
                        _imex.UpsertDataList("PO Date", "Field is required.");
                    }
                    else
                    {
                        var refNoParts = model.PoNo.Split('-');
                        var refNoYear = int.Parse(refNoParts[0]);
                        var refNoMonth = int.Parse(refNoParts[1]);
                        var refNoSeq = int.Parse(refNoParts[2]);
                        if (refNoSeq == 0)
                        {
                            _imex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), "Invalid sequence number.");
                        }
                        else
                        {
                            if (refNoYear != model.PoDate.Value.Year || refNoMonth != model.PoDate.Value.Month)
                            {
                                _imex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), "Series year and month must match the PO date’s year and month.");
                            }
                            //else
                            //{
                            //    //var maxNo = _db.Orders.Where(w => DbFunctions.TruncateTime(w.PoDate) < DbFunctions.TruncateTime(model.PoDate)).Max(m => m.PoNo);
                            //    var maxNo = _db.Orders.Where(w => w.PoDate.Value.Year == model.PoDate.Value.Year).Max(m => m.PoNo);
                            //    if (!string.IsNullOrWhiteSpace(maxNo))
                            //    {
                            //        var maxSeq = int.Parse(maxNo.Split('-')[2]);
                            //        if (refNoSeq <= maxSeq)
                            //        {
                            //            _imex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), $"Sequence No. must be greater than {maxSeq}");
                            //        }
                            //    }
                            //}
                        }

                        if (mode == Mode.ADD)
                        {
                            if (await _db.Orders.AnyAsync(a => a.PoNo == model.PoNo))
                            {
                                _imex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), "Already Exists.");
                            }
                        }
                        else
                        {
                            if (await _db.Orders.AnyAsync(a => a.PoNo == model.PoNo && a.Id != model.Id))
                            {
                                _imex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), "Already Exists.");
                            }
                        }
                    }
                }
            }

            if (model.PoDate.HasValue)
            {
                if (model.PoDate > model.UpdatedDt)
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.PoDate)), $"Future date is not allowed.");
                }

                var maxDate = await _db.Requests.Where(w => model.PrNoList.Contains(w.PrNo)).Select(s => s.PrDate).MaxAsync();

                if (maxDate != null)
                {
                    if (maxDate > model.PoDate)
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.PoDate)), "PO date must be on or after the PR date.");
                    }
                }
            }

            _imex.ThrowIfContainsErrors();
        }

        private async Task ValidateStatusAsync(Guid orderId)
        {
            await _orderSharedService.ValidateStatusAsync(orderId);
        }

        private async Task ValidateOnPostAsync(Order entity)
        {
            if (string.IsNullOrEmpty(entity.PoNo))
            {
                throw new InvalidValueException("PO Number is required.");
            }

            if (string.IsNullOrEmpty(entity.DeliveryDate))
            {
                throw new InvalidValueException("Delivery Date is required.");
            }

            if (string.IsNullOrEmpty(entity.TermDelivery))
            {
                throw new InvalidValueException("Delivery Term is required.");
            }

            if (string.IsNullOrEmpty(entity.TermPayment))
            {
                throw new InvalidValueException("Payment Term is required.");
            }

            if (!string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                var msg = $"Record already posted by {entity.PostedBy} on {entity.PostedDt}. Please Verify.";
                throw new RecordAlreadyPostedException(msg);
            }

            if (!(await _db.OrderItems.AnyAsync(a => a.OrderId == entity.Id)))
            {
                throw new RecordRelationshipException("No items were found. Cannot proceed.");
            }


            //var idList = await _db.OrderItems.Where(w => w.OrderId == entity.Id).GroupBy(g => g.OrderItem.Order.Id)
            //    .Select(s => s.Key).ToListAsync();

            //foreach (var id in idList)
            //{
            //    var Order = await _db.Orders.FindAsync(id);
            //    if (Order == null)
            //    {
            //        throw new NotFoundException(id);
            //    }
            //    else
            //    {
            //        if (string.IsNullOrWhiteSpace(Order.SubmittedBy))
            //        {
            //            throw new RecordNotYetPostedException("Record is not yet posted.");
            //        }
            //    }
            //}

            //var unitGroupItems = await _db.OrderItemUnitGroupDescriptionItems.Include(i => i.OrderItem)
            //    .Where(w => w.OrderItemUnitGroupDescription.OrderItemUnitGroup.OrderId == entity.Id).ToListAsync();
            //if (unitGroupItems.Any())
            //{
            //    var rate = unitGroupItems.Sum(s => s.OrderItem.PriceRate) ?? 0;
            //    if (rate != 100)
            //    {
            //        throw new InvalidValueException("Total price rate of Set/Lot items must be 100%");
            //    }
            //}

            string brandMsg = "";
            var orderItems = await _db.OrderItems
                .Include(i => i.ItemCode.ItemType)
                .Include(i => i.AllField)
                .Where(w => w.OrderId == entity.Id).ToListAsync();
            foreach (var orderItem in orderItems)
            {
                //if (orderItem.Unit == "set" || orderItem.Unit == "lot")
                //{
                //    decimal? unitCost = 0;
                //    var unitGroup = _db.OrderItemUnitGroups.Where(w => w.OrderItemUnitGroupDescriptions.Any(a => a.OrderItemUnitGroupDescriptionItems.Any(b => b.OrderItemId == orderItem.Id))).FirstOrDefault();
                //    if (unitGroup != null)
                //    {
                //        unitCost = unitGroup.UnitCost;
                //        if (_itemCodeService.IsProperty(orderItem.ItemCodeId))
                //        {
                //            if (unitCost < _priceCap)
                //            {
                //                throw new InvalidValueException($"Please use the Supplies Code for items with a group unit cost below {_priceCap:n0}.”");
                //            }
                //        }
                //        else
                //        {
                //            if (unitCost >= _priceCap)
                //            {
                //                throw new InvalidValueException($"Please use the Property Ccode for items with a group unit cost of {_priceCap:n0} and above.");
                //            }
                //        }
                //    }
                //}
                //else
                //{
                    if (!orderItem.ItemCodeId.HasValue)
                    {
                        throw new InvalidValueException("Item code is not configured properly.");
                    }
                    else
                    {
                        if (orderItem.ItemCode.ItemType.PartialPage.Contains("Brand") || orderItem.ItemCode.ItemType.PartialPage.Contains("Drugs"))
                        {
                            var allfield = await _db.AllFields.FirstOrDefaultAsync(f => f.Id == orderItem.Id);
                            if (allfield == null)
                            {
                                throw new RecordRelationshipException("Required fields are missing. Please recreate the order.");
                            }
                            {
                                if (string.IsNullOrWhiteSpace(allfield.Brand))
                                {
                                    brandMsg = brandMsg == "" ? $"{orderItem.ItemCode.Description}" : brandMsg += ", " + $"{orderItem.ItemCode.Description}";
                                }
                            }
                        }
                    }

                    if (string.IsNullOrWhiteSpace(orderItem.PsNo))
                    {
                        throw new InvalidValueException("All items must have a valid Property/Stock No.");
                    }
                //}

                // validate unit cost
                if (!orderItem.UnitCost.HasValue || orderItem.UnitCost == 0)
                {
                    throw new InvalidValueException("All items must unit cost.");
                }
            }

            if (!string.IsNullOrWhiteSpace(brandMsg))
            {
                throw new InvalidValueException($"Brand is required for {brandMsg}.");
            }
        }

        private async Task ValidateOnUnpostAsync(Order entity)
        {
            if (string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordNotYetPostedException(string.Format("PO Number {0} is not yet posted.", entity.PoNo));
            }

            var airs = await _db.AIRs.Where(w => w.OrderId == entity.Id && w.PostedDt != null).ToListAsync();
            foreach (var air in airs)
            {
                throw new RecordRelationshipException(string.Format("AIR Number {0} of this PO is already posted.", air.AIRNo));
            }

            var riss = await _db.RISses.Where(w => w.OrderRequest.OrderId == entity.Id && w.PostedDt != null).ToListAsync();
            foreach (var ris in riss)
            {
                throw new RecordRelationshipException(string.Format("RIS Number {0} of this PO is already posted.", ris.RisNo));
            }
        }

        private async Task<bool> IsWwithPoUploadAsync(Guid? id)
        {
            var result = await _uploadPoService.GetAllByImageId(id).AnyAsync();
            return result;
        }

        private async Task<bool> IsWwithCafoaUploadAsync(Guid? id)
        {
            var result = await _uploadCafoaService.GetAllByImageId(id).AnyAsync();
            return result;
        }

        private async Task ValidateUploadAsync(Guid? id, string poNo)
        {
            if (!await IsWwithPoUploadAsync(id))
            {
                throw new InvalidValueException($"No PO attachments found for PO No. {poNo}. Cannot post.");
            }

            if (!await IsWwithCafoaUploadAsync(id))
            {
                throw new InvalidValueException($"No CAFOA attachments found for PO No. {poNo}. Cannot post.");
            }
        }
        #endregion        
    }
}