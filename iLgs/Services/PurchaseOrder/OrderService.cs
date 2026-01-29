using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.AllFields;
using iLgs.Services.Codes;
using iLgs.Services.Items;
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
    public interface IOrderService : IOrderSharedService
    {
        IQueryable<OrderVM> GetAll();
        ValueTask<IQueryable<OrderVM>> GetAllAsync(string userId);
        IQueryable<OrderVM> GetAllParOrders();
        ValueTask<Models.Order> GetByIdAsync(Guid orderId);
        ValueTask<Models.Order> GetByPoNoAsync(string poNo);
        bool GetAnyPoNo(Guid id, string poNo);
        ValueTask<bool> GetAnyPoNoAsync(Guid id, string poNo);
        //bool IsPosted(Guid orderId);
        //bool IsPosted(Order order);
        //bool IsPosted(OrderItem orderItem);
        //bool IsPosted(OrderItemUnitGroup orderItemUnitGroup);
        //bool IsPosted(OrderItemUnitGroupDescription orderItemunitGroupDescription);
        //bool IsPosted(OrderItemUnitGroupDescriptionItem orderItemunitGroupDescriptionItem);
        //ValueTask<bool> IsPostedAsync(Guid orderId);
        ValueTask<bool> GetAnyParsAsync(Guid id);
        ValueTask<bool> GetAnyAirsAsync(Guid id);

        ValueTask<int> GetNotPostedAsync(DateTime asOf);

        ValueTask<OrderVM> CreateAsync(OrderVM model, string user, DateTime date);
        ValueTask<OrderVM> UpdateAsync(OrderVM model, string user, DateTime date);
        ValueTask<OrderVM> DeleteAsync(OrderVM model, string user, DateTime date);
        ValueTask<Order> PostAsync(Guid orderId, string user, DateTime date);
        ValueTask<Order> UnpostAsync(Guid orderId, string user, DateTime date);
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

        private static Expression<Func<Order, OrderVM>> Projection(AppManEntities db)
        {
            return s => new OrderVM
            {
                Id = s.Id,
                CtrlNo = s.CtrlNo,
                Fund = s.Fund,      
                FPP = s.FPP,
                PrId = s.PrId,
                PrNo = s.PrNo,
                PrDate = s.PrDate,
                DeptId = s.DeptId,
                Department = s.Department,
                Section = s.Section,
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

        public async ValueTask<IQueryable<OrderVM>> GetAllAsync(string userId)
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
                    .Where(w => w.Codextn.DepartmentUsers.Any(a => a.UserId == userId))
                    .Select(Projection(_db)).OrderByDescending(o => o.PoNo);
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
                    Department = s.Request.RISs.Office
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

        public async ValueTask<bool> GetAnyParsAsync(Guid id)
        {
            return await _db.PARs.AnyAsync(a => a.OrderId == id);
        }

        public async ValueTask<bool> GetAnyAirsAsync(Guid id)
        {
            return await _db.AIRs.AnyAsync(a => a.OrderId == id);
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

        public bool IsPosted(OrderItemUnitGroup orderItemUnitGroup)
        {
            return _orderSharedService.IsPosted(orderItemUnitGroup);            
        }

        public bool IsPosted(OrderItemUnitGroupDescription orderItemUnitGroupDescription)
        {
            return _orderSharedService.IsPosted(orderItemUnitGroupDescription);            
        }

        public bool IsPosted(OrderItemUnitGroupDescriptionItem orderItemUnitGroupDescriptionItem)
        {
            return _orderSharedService.IsPosted(orderItemUnitGroupDescriptionItem);            
        }

        public async ValueTask<bool> IsPostedAsync(Guid orderId)
        {
            return await _orderSharedService.IsPostedAsync(orderId);            
        }

        public async ValueTask<int> GetNotPostedAsync(DateTime asOf)
        {
            return await _db.Orders.Where(w => w.PoDate <= asOf && w.PostedDt == null).CountAsync();
        }

        public ValueTask<OrderVM> CreateAsync(OrderVM model, string user, DateTime date) => _orderVmExceptionService.TryCatch(async () =>
        {
            ValidateOnCreate(model);
            ValidateOnCreateUpdate(model, Mode.ADD);

            model.Id = Guid.NewGuid();
            if (string.IsNullOrWhiteSpace(model.PoNo))
            {
                model.PoNo = NextPoNo((DateTime)model.PoDate);
            }
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = new iLgs.Models.Order()
            {
                Id = model.Id,
                SupplierId = model.SupplierId,
                SupName = model.SupName,
                SupBusiness = model.SupBusiness,
                SupAddress = model.SupAddress,
                SupContactNo = model.SupContactNo,
                SupZipCode = model.SupZipCode,
                SupEmail = model.SupEmail,
                SupTIN = model.SupTIN,
                PoNo = model.PoNo,
                PoDate = model.PoDate,
                PoMode = model.PoMode,
                PrId = model.PrId,
                DeliveryPlace = model.DeliveryPlace,
                DeliveryDate = model.DeliveryDate,
                TermDelivery = model.TermDelivery,
                TermPayment = model.TermPayment,
                SignedByAuthDesignation = model.SignedByAuthDesignation,
                SignedByAuthName = model.SignedByAuthName,
                SignedBySuppDate = model.SignedBySuppDate,
                SignedBySuppName = model.SignedBySuppName,
                ResoNo = model.ResoNo,
                CertifiedCorrectBy = model.CertifiedCorrectBy,
                CertifiedCorrectDate = model.CertifiedCorrectDate,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            //using (var ctx = await _contextFactory.CreateContextAsync())
            //{

                // include items during add, PR Items not yet in Order Items
                var requestItems = await _db.RequestItems.Include(i => i.RisItem.AllField).AsNoTracking()
                    .Where(w => w.PrId == model.PrId && !w.OrderItems.Any()).OrderBy(o => o.InsertedDt).ToListAsync();
                foreach (var requestItem in requestItems)
                {
                    var insertedDt = DateTime.Now;
                    OrderItem orderItem = new OrderItem()
                    {
                        Id = Guid.NewGuid(),
                        OrderId = entity.Id,
                        ItemCodeId = requestItem.RisItem.ItemCodeId,
                        PsNo = requestItem.RisItem.PsNo,
                        PsNoDisplay = requestItem.RisItem.PsNoDisplay,
                        RequestItemId = requestItem.Id,
                        ItemName = requestItem.RisItem.ItemName,
                        Description = requestItem.RisItem.Description,
                        OtherDesc = requestItem.RisItem.OtherDesc,
                        Unit = requestItem.RisItem.Unit,
                        Qty = requestItem.Qty,
                        UnitCost = requestItem.UnitCost,
                        Amount = requestItem.TotalCost,
                        PriceRate = requestItem.PriceRate,
                        PpmpCode = requestItem.RisItem.PpmpCode,
                        InsertedBy = user,
                        InsertedDt = insertedDt,
                        UpdatedBy = user,
                        UpdatedDt = insertedDt
                    };

                    //Map Fields
                    var reqAllField = await _db.Database.SqlQuery<AllField>("Select * From AllFields where Id = {0}", requestItem.RisItem.Id).FirstOrDefaultAsync();
                    reqAllField.Id = orderItem.Id;
                    reqAllField.UpdatedBy = user;
                    reqAllField.UpdatedDt = date;
                    orderItem.AllField = reqAllField;

                    entity.OrderItems.Add(orderItem);
                }

                // Unit Groups
                var unitGroups = await _db.RequestItemUnitGroups
                    .Include(i => i.RisItemUnitGroup.RisItemUnitGroupDescriptions)
                    .Include(i => i.RequestItemUnitGroupDescriptions).Where(w => w.PrId == model.PrId && !w.OrderItemUnitGroups.Any())
                    .OrderBy(o => o.InsertedDt).ToListAsync();
                foreach (var unitGroup in unitGroups)
                {
                    var unitGroupDt = DateTime.Now;
                    var orderItemUnitGroup = new OrderItemUnitGroup()
                    {
                        Id = Guid.NewGuid(),
                        OrderId = model.Id,
                        RequestItemUnitGroupId = unitGroup.Id,
                        SetLotNo = unitGroup.RisItemUnitGroup.SetLotNo,
                        Qty = unitGroup.RisItemUnitGroup.Qty,
                        Unit = unitGroup.RisItemUnitGroup.Unit,
                        UnitCost = unitGroup.UnitCost,
                        TotalCost = unitGroup.TotalCost,
                        InsertedBy = user,
                        InsertedDt = unitGroupDt,
                        UpdatedBy = user,
                        UpdatedDt = unitGroupDt
                    };

                    foreach (var unitGroupDescription in unitGroup.RequestItemUnitGroupDescriptions.OrderBy(o => o.InsertedDt).ToList())
                    {
                        var groupDescriptionDt = DateTime.Now;
                        var orderItemUnitGroupDescription = new OrderItemUnitGroupDescription()
                        {
                            Id = Guid.NewGuid(),
                            OrderItemUnitGroupId = orderItemUnitGroup.Id,
                            RequestItemUnitGroupDescriptionId = unitGroupDescription.Id,
                            Description = unitGroupDescription.RisItemUnitGroupDescription.Description,
                            InsertedBy = user,
                            InsertedDt = groupDescriptionDt,
                            UpdatedBy = user,
                            UpdatedDt = groupDescriptionDt
                        };

                        var requestItemUnitGroupDescriptionItems = await _db.RequestItemUnitGroupDescriptionItems
                            .Where(w => w.RequestItemUnitGroupDescriptionId == unitGroupDescription.Id).OrderBy(o => o.InsertedDt).ToListAsync();
                        foreach (var unitGroupDescriptionItem in requestItemUnitGroupDescriptionItems)
                        {
                            var groupDescriptionItemDt = DateTime.Now;
                            var orderItemUnitGroupDescriptionItem = new OrderItemUnitGroupDescriptionItem()
                            {
                                Id = Guid.NewGuid(),
                                RequestItemUnitGroupDescriptionItemId = unitGroupDescriptionItem.Id,
                                OrderItemUnitGroupDescriptionId = orderItemUnitGroupDescription.Id,
                                OrderItemId = entity.OrderItems.FirstOrDefault(f => f.RequestItemId == unitGroupDescriptionItem.RequestItemId).Id,
                                InsertedBy = user,
                                InsertedDt = groupDescriptionItemDt,
                                UpdatedBy = user,
                                UpdatedDt = groupDescriptionItemDt
                            };
                            orderItemUnitGroupDescription.OrderItemUnitGroupDescriptionItems.Add(orderItemUnitGroupDescriptionItem);
                        }
                        orderItemUnitGroup.OrderItemUnitGroupDescriptions.Add(orderItemUnitGroupDescription);
                    }
                    entity.OrderItemUnitGroups.Add(orderItemUnitGroup);
                }

                _db.Orders.Add(entity);
                await _db.SaveChangesAsync();
            //}

            return model;
        });

        public ValueTask<OrderVM> UpdateAsync(OrderVM model, string user, DateTime date) => _orderVmExceptionService.TryCatch(async () =>
        {
            ValidateOnUpdate(model);
            ValidateOnCreateUpdate(model, Mode.EDIT);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            //using (var ctx = await _contextFactory.CreateContextAsync())
            //{
                var entity = await _db.Orders.FindAsync(model.Id);

                // if there's a change of request item
                if (entity.PrId != model.PrId)
                {
                    var orderItems = _db.OrderItems.Where(w => w.OrderId == model.Id);
                    await orderItems.ForEachAsync(f =>
                    {
                        f.UpdatedBy = model.UpdatedBy;
                        f.UpdatedDt = model.UpdatedDt;
                    });
                    await _db.SaveChangesAsync();

                    _db.OrderItems.RemoveRange(orderItems);
                    await _db.SaveChangesAsync();

                    // include items during add, PR Items not yet in Order Items
                    var prItemList = await _db.RequestItems.Include(i => i.RisItem.AllField)
                        .Where(w => w.PrId == model.PrId && !w.OrderItems.Any()).ToListAsync();
                    foreach (var prItem in prItemList)
                    {
                        OrderItem orderItem = new OrderItem()
                        {
                            Id = Guid.NewGuid(),
                            OrderId = entity.Id,
                            ItemCodeId = prItem.RisItem.ItemCodeId,
                            PsNo = prItem.RisItem.PsNo,
                            PsNoDisplay = prItem.RisItem.PsNoDisplay,
                            RequestItemId = prItem.Id,
                            ItemName = prItem.RisItem.ItemName,
                            Description = prItem.RisItem.Description,
                            OtherDesc = prItem.RisItem.OtherDesc,
                            Unit = prItem.RisItem.Unit,
                            Qty = prItem.Qty,
                            UnitCost = prItem.UnitCost,
                            Amount = prItem.TotalCost,
                            PriceRate = prItem.PriceRate,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };

                        AllField allfield = prItem.RisItem.AllField;
                        allfield.Id = orderItem.Id;
                        orderItem.AllField = allfield;

                        entity.OrderItems.Add(orderItem);
                    }
                }

                entity.SupplierId = model.SupplierId;
                entity.SupName = model.SupName;
                entity.SupBusiness = model.SupBusiness;
                entity.SupAddress = model.SupAddress;
                entity.SupContactNo = model.SupContactNo;
                entity.SupZipCode = model.SupZipCode;
                entity.SupEmail = model.SupEmail;
                entity.SupTIN = model.SupTIN;
                entity.PoNo = model.PoNo;
                entity.PoDate = model.PoDate;
                entity.PoMode = model.PoMode;
                entity.PrId = model.PrId;
                entity.DeliveryPlace = model.DeliveryPlace;
                entity.DeliveryDate = model.DeliveryDate;
                entity.TermDelivery = model.TermDelivery;
                entity.TermPayment = model.TermPayment;
                entity.SignedByAuthDesignation = model.SignedByAuthDesignation;
                entity.SignedByAuthName = model.SignedByAuthName;
                entity.SignedBySuppDate = model.SignedBySuppDate;
                entity.SignedBySuppName = model.SignedBySuppName;
                entity.ResoNo = model.ResoNo;
                entity.CertifiedCorrectBy = model.CertifiedCorrectBy;
                entity.CertifiedCorrectDate = model.CertifiedCorrectDate;
                entity.UpdatedBy = model.UpdatedBy;
                entity.UpdatedDt = model.UpdatedDt;

                await _db.SaveChangesAsync();
            //}

            return model;
        });

        public ValueTask<OrderVM> DeleteAsync(OrderVM model, string user, DateTime date) =>
        _orderVmExceptionService.TryCatch(async () =>
        {
            await ValidateOnDestroy(model);

            //using (var ctx = await _contextFactory.CreateContextAsync())
            //{
                var unitGroups = _db.OrderItemUnitGroups.Where(w => w.OrderId == model.Id);
                if (unitGroups.Any())
                {
                    _db.OrderItemUnitGroups.RemoveRange(unitGroups);
                    await _db.SaveChangesAsync();
                }

                model.UpdatedBy = user;
                model.UpdatedDt = date;

                var entity = await _db.Orders.FindAsync(model.Id);

                entity.UpdatedBy = user;
                entity.UpdatedDt = date;

                await _db.SaveChangesAsync();

                _db.Orders.Remove(entity);
                await _db.SaveChangesAsync();
            //}

            return model;
        });

        public ValueTask<Order> PostAsync(Guid orderId, string user, DateTime date) => _orderExceptionService.TryCatch(async () =>
        {
            //using (var ctx = await _contextFactory.CreateContextAsync())
            //{
                var entity = await _db.Orders.Include(i => i.Request.RISs.RisItems).Where(w => w.Id == orderId).FirstOrDefaultAsync();
                if (entity == null)
                {
                    throw new RecordNotFoundException(orderId);
                }

                await ValidateOnPost(entity);
                await ValidateUploadAsync(orderId, entity.PoNo);

                entity.PostedBy = user;
                entity.PostedDt = date;

                await _db.SaveChangesAsync();

                return entity;
            //}
        });

        public ValueTask<Order> UnpostAsync(Guid orderId, string user, DateTime date) => _orderExceptionService.TryCatch(async () =>
        {
            //using (var ctx = await _contextFactory.CreateContextAsync())
            //{
                var entity = await _db.Orders.FindAsync(orderId);

                if (entity == null)
                {
                    throw new RecordNotFoundException(orderId);
                }

                await ValidateOnUnpost(entity);

                entity.PostedBy = null;
                entity.PostedDt = null;
                entity.UpdatedBy = user;
                entity.UpdatedDt = date;

                await _db.SaveChangesAsync();

                return entity;
            //}
        });

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

        #region VALIDATION

        private void ValidateOnCreate(OrderVM model)
        {
            if (!string.IsNullOrWhiteSpace(model.PoNo) && _db.Orders.Any(a => a.PoNo == model.PoNo))
            {
                throw new RecordAlreadyExistsException(string.Format("PO Number {0} already exists", model.PoNo));
            }
            else
            {
                var pr = _db.Requests.Find(model.PrId);
                if (pr == null)
                {
                    throw new RecordNotFoundException(model.PrId);
                }
                else
                {
                    if (pr.PrDate > model.PoDate)
                    {
                        throw new InvalidValueException("PO Date must be greater than or equal to PR date!");
                    }
                }
            }
        }

        private void ValidateOnCreateUpdate(OrderVM model, Mode mode)
        {
            if (!string.IsNullOrWhiteSpace(model.PoNo))
            {
                if (model.PoNo.Trim().Length != 12)
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), "Invalid value.");
                }
                else
                {
                    var refNoParts = model.PoNo.Split('-');
                    var refNoYear = int.Parse(refNoParts[0]);
                    var refNoMonth = int.Parse(refNoParts[1]);
                    if (refNoYear != model.PoDate.Value.Year || refNoMonth != model.PoDate.Value.Month)
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), "Series Year and month must be same as the year and month of the PO date.");
                    }
                    else
                    {
                        var maxNo = _db.Orders.Where(w => DbFunctions.TruncateTime(w.PoDate) < DbFunctions.TruncateTime(model.PoDate)).Max(m => m.PoNo);
                        if (!string.IsNullOrWhiteSpace(maxNo))
                        {
                            var refNoSeq = int.Parse(refNoParts[2]);
                            var maxSeq = int.Parse(maxNo.Split('-')[2]);
                            if (refNoSeq <= maxSeq)
                            {
                                _imex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), $"Serial No. must be greater than {maxSeq}");
                            }
                        }
                    }
                }
            }
            _imex.ThrowIfContainsErrors();
        }

        private void ValidateOnUpdate(OrderVM model)
        {
            var order = _db.Orders.Find(model.Id);
            if (order == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (order.PostedDt != null)
            {
                throw new RecordAlreadyPostedException(string.Format("PO Number {0} already posted, cannot update!", model.PoNo));
            }

            if (GetAnyPoNo(model.Id, model.PoNo))
            {
                throw new RecordAlreadyExistsException(string.Format("PO Number {0} already exists!", model.PoNo));
            }

            var pr = _db.Requests.Find(model.PrId);
            if (pr == null)
            {
                throw new RecordRelationshipException(string.Format("PR Number {0} does exists!", model.PrNo));
            }
            else 
            {
                if (pr.PrDate > model.PoDate)
                {
                    throw new InvalidValueException("PO date must be greather than or equal to PR date!");
                }
            }
        }

        private async ValueTask ValidateOnDestroy(OrderVM model)
        {
            var order = await _db.Orders.FindAsync(model.Id);
            if (order == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (await IsPostedAsync(model.Id))
            {
                throw new RecordAlreadyPostedException(string.Format("PO Number {0} already Posted, cannot delete!", model.PoNo));
            }

            if (await GetAnyAirsAsync(model.Id))
            {
                throw new RecordRelationshipException("PO Number already with AIR, cannot delete!");
            }

            if (await GetAnyParsAsync(model.Id))
            {
                throw new RecordRelationshipException("PO Number already with PAR, cannot delete!");
            }
        }

        private async ValueTask ValidateOnPost(Order entity)
        {
            if (!string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                var msg = $"Record already posted by {entity.PostedBy} on {entity.PostedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }

            var idList = await _db.OrderItems.Where(w => w.OrderId == entity.Id).GroupBy(g => g.RequestItem.Request.Id)
                .Select(s => s.Key).ToListAsync();

            foreach (var id in idList)
            {
                var request = await _db.Requests.FindAsync(id);
                if (request == null)
                {
                    throw new NotFoundException(id);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(request.SubmittedBy))
                    {
                        throw new RecordNotYetPostedException("Record is not yet posted.");
                    }
                }
            }

            var unitGroupItems = _db.OrderItemUnitGroupDescriptionItems.Include(i => i.OrderItem)
                .Where(w => w.OrderItemUnitGroupDescription.OrderItemUnitGroup.OrderId == entity.Id).ToList();
            if (unitGroupItems.Any())
            {
                var rate = unitGroupItems.Sum(s => s.OrderItem.PriceRate) ?? 0;
                if (rate != 100)
                {
                    throw new InvalidValueException("Price rate must be 100%");
                }
            }

            string brandMsg = "";
            var orderItems = await _db.OrderItems
                .Include(i => i.ItemCode.ItemType)
                .Include(i => i.AllField)
                .Where(w => w.OrderId == entity.Id).ToListAsync();
            foreach (var orderItem in orderItems)
            {
                //if (Enum.TryParse(orderItem.ItemCode.ItemType.Code, out Category c))
                //{
                //    if (_allFieldService.IsBrandRequired(c))
                //    {
                //        var allfield = await _db.AllFields.FirstOrDefaultAsync(f => f.Id == orderItem.Id);
                //        if (allfield == null)
                //        {
                //            throw new RecordRelationshipException("Required fields is missing, please recreate this Order.");
                //        }
                //        {
                //            if (string.IsNullOrWhiteSpace(allfield.Brand))
                //            {
                //                brandMsg = brandMsg == "" ? $"{orderItem.ItemCode.Description}" : brandMsg += ", " + $"{orderItem.ItemCode.Description}";
                //            }
                //        }
                //    }
                //}

                if (orderItem.ItemCode.ItemType.PartialPage.Contains("Brand") || orderItem.ItemCode.ItemType.PartialPage.Contains("Drugs"))
                {
                    var allfield = await _db.AllFields.FirstOrDefaultAsync(f => f.Id == orderItem.Id);
                    if (allfield == null)
                    {
                        throw new RecordRelationshipException("Required fields is missing, please recreate this Order.");
                    }
                    {
                        if (string.IsNullOrWhiteSpace(allfield.Brand))
                        {
                            brandMsg = brandMsg == "" ? $"{orderItem.ItemCode.Description}" : brandMsg += ", " + $"{orderItem.ItemCode.Description}";
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(orderItem.PsNo))
                {
                    throw new InvalidValueException("All items must have a valid Property/Stock No.");
                }

                // validate unit cost
                if (!orderItem.UnitCost.HasValue || orderItem.UnitCost == 0)
                {
                    throw new InvalidValueException("All items must unit cost.");
                }

                decimal? unitCost = 0;
                var unitGroup = _db.OrderItemUnitGroups.Where(w => w.OrderItemUnitGroupDescriptions.Any(a => a.OrderItemUnitGroupDescriptionItems.Any(b => b.OrderItemId == orderItem.Id))).FirstOrDefault();
                if (unitGroup != null)
                {
                    unitCost = unitGroup.UnitCost;
                    if (_itemCodeService.IsProperty(orderItem.ItemCodeId))
                    {
                        if (unitCost < _priceCap)
                        {
                            throw new InvalidValueException($"Please use supplies code for items with a group unit cost below {_priceCap:n0}.");
                        }
                    }
                    else
                    {
                        if (unitCost >= _priceCap)
                        {
                            throw new InvalidValueException($"Please use property code for items with a group unit cost of {_priceCap:n0} and above.");
                        }
                    }
                }
                else
                {
                    unitCost = orderItem.UnitCost;
                    if (_itemCodeService.IsProperty(orderItem.ItemCodeId))
                    {
                        if (unitCost < _priceCap)
                        {
                            throw new InvalidValueException($"Please use supplies code for items with a unit cost below {_priceCap:n0}.");
                        }
                    }
                    else
                    {
                        if (unitCost >= _priceCap)
                        {
                            throw new InvalidValueException($"Please use property code for items with a unit cost of {_priceCap:n0} and above.");
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(brandMsg))
            {
                throw new InvalidValueException($"Brand is required for {brandMsg}.");
            }
        }

        private async ValueTask ValidateOnUnpost(Order entity)
        {
            if (string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordNotYetPostedException(string.Format("PO Number {0} not yet posted..", entity.PoNo));
            }

            var airs = await _db.AIRs.Where(w => w.OrderId == entity.Id && w.PostedDt != null).ToListAsync();

            foreach (var air in airs)
            {
                throw new RecordRelationshipException(string.Format("AIR Number {0} of this PO is already posted.", air.AIRNo));
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
                throw new InvalidValueException($"No PO attachments found for PO No. {poNo}, cannot post!");
            }

            if (!await IsWwithCafoaUploadAsync(id))
            {
                throw new InvalidValueException($"No CAFOA attachments found for PO No. {poNo}, cannot post!");
            }
        }
        #endregion

        #region EXCEPTIONS
        //private delegate ValueTask<OrderVM> ReturningFunction();
        //private delegate IQueryable<OrderVM> ReturningQueryableFunction();
        //private async ValueTask<OrderVM> TryCatch(ReturningFunction returningFunction)
        //{
        //    try
        //    {
        //        return await returningFunction();
        //    }
        //    catch (RecordNotFoundException notFoundException)
        //    {
        //        throw notFoundException;
        //    }
        //    catch (RecordAlreadyExistsException recordAlreadyExistsException)
        //    {
        //        throw recordAlreadyExistsException;
        //    }
        //    catch (SqlException sqlException)
        //    {
        //        throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
        //    }
        //    catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
        //    {
        //        var recordLockedException = new RecordLockedException(dbUpdateConcurrencyException);

        //        throw exceptions.CreateAndLogDependencyException(recordLockedException);
        //    }
        //    catch (DbUpdateException dbUpdateException)
        //    {
        //        throw exceptions.CreateAndLogDependencyException(dbUpdateException);
        //    }
        //    catch (Exception exception)
        //    {
        //        var failedServiceException =
        //            new FailedServiceException(exception);

        //        throw exceptions.CreateAndLogServiceException(failedServiceException);
        //    }
        //}

        #endregion
    }
}