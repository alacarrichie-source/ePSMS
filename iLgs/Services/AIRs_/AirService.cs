using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
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

namespace iLgs.Services.AIRs_
{
    public interface IAirService : IAirAbstractService
    {
        IQueryable<AIR_VM> GetAll();
        ValueTask<IQueryable<AIR_VM>> GetAllAsync(string userId, AirGroup airGroup);
        ValueTask<AIR> GetByIdAsync(Guid id);
        ValueTask<AIR_VM> GetVmByIdAsync(Guid id);
        ValueTask<AIR> GetByAirNoAsync(string airNo);
        ValueTask<bool> GetAnyAirNoAsync(Guid airId, string airNo);

        ValueTask<AIR_VM> CreateAsync(AIR_VM model, string user, DateTime date);
        ValueTask<AIR_VM> UpdateAsync(AIR_VM model, string user, DateTime date);
        ValueTask<AIR_VM> DeleteAsync(AIR_VM model, string user, DateTime date);
        ValueTask<AIR_VM> SaveAsync(AIR_VM model, string user, DateTime date);
        ValueTask<AIR> PostAsync(Guid airId, string user, DateTime date);
        ValueTask<AIR> UnpostAsync(Guid airId, string user, DateTime date);

        IAirItemService AirItem { get; }
        IAirInvoiceService AirInvoice { get; }
    }

    public class AirService : BaseValidator, IAirService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<AIR_VM> _vmExceptionService;
        private readonly IExceptionService<AIR> _exceptionService;
        private readonly IAirAbstractService _airAbstractService;
        private readonly IItemCodeService _itemCodeService;
        private readonly IAirUploadService _uploadService;
        private readonly IUserService _userService;
        private readonly IPriceCapService _priceCapService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        private IAirItemService _airItemService;
        private IAirInvoiceService _airInvoiceService;

        public AirService(AppManEntities db)
        {
            _db = db;
            _airAbstractService = new AirAbstractService(_db);
            _itemCodeService = new ItemCodeService(_db);
            _uploadService = new AirUploadService(_db);
            _userService = new UserService(_db);
            _priceCapService = new PriceCapService(_db);
            _vmExceptionService = new ExceptionService<AIR_VM>();
            _exceptionService = new ExceptionService<AIR>();
            _getDisplayName = Utility.GetDisplayName<AIR_VM>;
            _airItemService = new AirItemService(_db);
            _airInvoiceService = new AirInvoiceService(_db);
        }

        //public AirService(AppManEntities db,
        //    IAppManEntitiesFactory appManEntitiesFactory,
        //    IExceptionService<AIR_VM> vmExceptionService,
        //    IExceptionService<AIR> exceptionService,
        //    IAirAbstractService airAbstractService, 
        //    IAirItemService airItemService, IAirUploadService uploadService, IItemCodeService itemCodeService,
        //    IUserService userService, IPriceCapService priceCapService)
        //{
        //    _db = db;
        //    _contextFactory = appManEntitiesFactory;
        //    _vmExceptionService = vmExceptionService;
        //    _exceptionService = exceptionService;
        //    _airAbstractService = airAbstractService;
        //    _airItemService = airItemService;
        //    _itemCodeService = itemCodeService;
        //    _uploadService = uploadService;
        //    _userService = userService;
        //    _priceCapService = priceCapService;
        //    _getDisplayName = Utility.GetDisplayName<OrderVM>;            
        //}

        public IAirItemService AirItem => _airItemService;
        public IAirInvoiceService AirInvoice => _airInvoiceService;

        private static Expression<Func<AIR, AIR_VM>> Projection
        = s => new AIR_VM
        {
            Id = s.Id,
            CtrlNo = s.CtrlNo,
            Fund = s.Order.Fund,
            OrderId = s.OrderId,
            PoNo = s.Order.PoNo,
            Supplier = s.Order.SupName,
            PoDate = s.Order.PoDate,
            Department = s.Order.Department,
            AIRNo = s.AIRNo,
            AIRDate = s.AIRDate,
            InvoiceNo = s.InvoiceNo,
            InvoiceDate = s.InvoiceDate,
            AcceptedDate = s.AcceptedDate,
            IsComplete = s.IsComplete,
            IsPartial = s.IsPartial,
            Custodian = s.Custodian,
            InspectedDate = s.InspectedDate,
            IsInspected = s.IsInspected,
            Officer = s.Officer,
            InvDist = s.InvDist,
            Remarks = s.Remarks,
            PostedBy = s.PostedBy,
            PostedDt = s.PostedDt,
            AIRInvoices = s.AIRInvoices,
            InsertedDt = s.InsertedDt
        };

        public IQueryable<AIR_VM> GetAll()
        {
            var data = _db.AIRs
                .Include(i => i.AIRInvoices)
                .AsNoTracking()
                .Select(Projection);

            return data;
        }

        public async ValueTask<IQueryable<AIR_VM>> GetAllAsync(string userId, AirGroup airGroup)
        {
            IQueryable<AIR_VM> data = null;
            if (await _userService.IsAdminAsync(userId))
            {
                data = _db.AIRs
                    .Include(i => i.AIRInvoices)
                    .AsNoTracking()
                    .Select(Projection);
            }
            else
            {
                data = _db.AIRs
                    .Include(i => i.AIRInvoices)
                    .AsNoTracking()
                    .Where(w => w.Order.OrderItems.Any(a => 
                        a.OrderItemRequests.Any(b => 
                            b.RequestItem.Request.Codextn.DepartmentUsers.Any(c => c.UserId == userId))))
                                .Select(Projection);
            }

            if (airGroup == AirGroup.ACCEPTANCE)
            {
                data = data.Where(w => w.IsInspected == true);
            }

            return data;
        }

        public async ValueTask<bool> GetAnyAirNoAsync(Guid airId, string airNo)
        {
            return await _db.AIRs.AnyAsync(a => a.Id != airId && a.AIRNo == airNo);
        }

        public ValueTask<AIR> GetByIdAsync(Guid id) => _exceptionService.TryCatch(async () =>
        {
            return await _db.AIRs.FindAsync(id);
        });

        public ValueTask<AIR_VM> GetVmByIdAsync(Guid id) => _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.AIRs
                .Include(i => i.AIRInvoices)
                .Where(w => w.Id == id)
                .Select(Projection).FirstOrDefaultAsync();
            return data;
        });

        public ValueTask<AIR> GetByAirNoAsync(string airNo) => _exceptionService.TryCatch(async () =>
        {
            return await _db.AIRs.Where(w => w.AIRNo == airNo).FirstOrDefaultAsync();
        });

        public ValueTask<AIR> PostAsync(Guid airId, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.AIRs.Include(i => i.AIRItems).Include(i => i.AIRInvoices).FirstOrDefaultAsync(f => f.Id == airId);
            if (entity == null)
            {
                throw new NotFoundException(airId);
            }

            //if (string.IsNullOrWhiteSpace(entity.AIRNo))
            //{
            //    throw new InvalidValueException("AIR No is required.");
            //}

            if (await IsPostedAsync(airId))
            {
                throw new RecordAlreadyPostedException();
            }

            await ValidateCompleteAsync(entity.OrderId);
            await ValidateQtyAsync(entity.OrderId);

            var airs = await _db.AIRs
                .Include(i => i.Order)
                .Include(i => i.AIRInvoices)
                .Where(w => w.AIRItems.Any(a => a.OrderItemRequest.OrderItem.OrderId == entity.OrderId)                    
                ).ToListAsync();
            foreach(var air in airs)
            {
                if (!air.AIRInvoices.Any())
                {
                    throw new InvalidValueException($"AIR Control Number {air.CtrlNo} does not have Invoice Records.");
                }

                if (string.IsNullOrWhiteSpace(air.AIRNo))
                {
                    throw new InvalidValueException($"AIR Control Number {air.CtrlNo} does not have AIR Number.");
                }

                if (!air.AIRDate.HasValue)
                {
                    throw new InvalidValueException($"AIR Control Number {air.CtrlNo} does not have AIR Date.");
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(air.AIRNo) && air.Order.PoDate > air.AIRDate)
                    {
                        throw new InvalidValueException($"AIR Control Number {air.CtrlNo} Date Must be on or after the PO Date.");
                    }
                }

                if (!air.AcceptedDate.HasValue)
                {
                    throw new InvalidValueException($"AIR Control Number {air.CtrlNo} Date received is required.");
                }

                if (!air.IsComplete == true && !air.IsPartial == true)
                {
                    throw new InvalidValueException($"Please select if Acceptance is Complete or Partial for AIR Control Number {air.CtrlNo} .");
                }

                if (string.IsNullOrWhiteSpace(air.Custodian))
                {
                    throw new InvalidValueException($"AIR Control Number {air.CtrlNo} Acceptance Custodian is required.");
                }

                if (!air.InspectedDate.HasValue)
                {
                    throw new InvalidValueException($"AIR Control Number {air.CtrlNo} Date inspected is required.");
                }

                if (!air.IsInspected == true)
                {
                    throw new InvalidValueException($"Please mark Inspected checkbox as checked for AIR Control Number {air.CtrlNo}.");
                }

                if (string.IsNullOrWhiteSpace(air.Officer))
                {
                    throw new InvalidValueException($"AIR Control Number {air.CtrlNo} Inspection Officer is required.");
                }

                _airItemService.ValidAirItems(air.Id);
                await ValidateUploadAsync(air.Id, air.CtrlNo);
                air.PostedBy = user;
                air.PostedDt = date;
            }                                              

            //entity.PostedBy = user;
            //entity.PostedDt = date;
            //entity.UpdatedBy = user;
            //entity.UpdatedDt = date;
            //_db.AIRs.Attach(entity);
            //_db.Entry(entity).State = EntityState.Modified;

            List<Guid> psCardIdList = new List<Guid>();
            //var priceCap = _priceCapService.GetPriceCap(order.PoDate);
            var orderItemGroups = await _db.Database.SqlQuery<OrderItemGroupVM>("Exec OrderService_GetOrderItemGroup {0}, {1}", entity.OrderId, 0).ToListAsync();
            // create stock for each group
            foreach (var oig in orderItemGroups)
            {
                // Post the OrderItems under the stocks having the same PsCodeId                
                var orderItemList = await _db.OrderItems.AsNoTracking()
                    .Include(i => i.ItemCode)
                    .Include(i => i.Order)
                    .Include(i => i.AllField)
                    .Where(w => w.OrderId == entity.OrderId
                        && w.PsNo == oig.StockNo
                        && w.Order.Fund == oig.Fund).ToListAsync();

                foreach (var orderItem in orderItemList)
                {
                    //var orderItemRequestList = orderItem.OrderItemRequests.ToList();
                    var orderItemRequestList = await _db.OrderItemRequests
                        .Include(i => i.AIRItems)
                        .Include(i => i.RequestItem.Request)
                        .Where(w => w.OrderItemId == orderItem.Id).ToListAsync();
                    foreach (var orderItemRequest in orderItemRequestList)
                    {

                        var isNew = false;
                        var psCard = await _db.PsCards
                            .Include(i => i.PsCardItems)
                            .Where(w => w.PsNo == oig.StockNo && w.Fund == oig.Fund
                                && w.FromDonation != true
                            ).FirstOrDefaultAsync();

                        var oAf = await _db.AllFields.AsNoTracking().Where(w => w.Id == orderItem.Id).FirstOrDefaultAsync();
                        //var office = orderItem.Order.Department;
                        //var deptId = orderItem.Order.DeptId;

                        var office = orderItemRequest.RequestItem.Request.Department;
                        var deptId = orderItemRequest.RequestItem.Request.DeptId;

                        if (psCard == null)
                        {
                            isNew = true;
                            var psCardId = Guid.NewGuid();
                            var subAccountCode = _itemCodeService.GetSubAccountCode(oig.ItemCodeId);
                            psCard = new PsCard()
                            {
                                Id = psCardId,
                                ItemCodeId = oig.ItemCodeId,
                                Fund = oig.Fund,
                                Description = "Please see attachment.",
                                PsNo = oig.StockNo,
                                SubAccountCode = subAccountCode,
                                CardCategory = oig.CardCategory,
                                InsertedBy = user,
                                InsertedDt = date,
                                UpdatedBy = user,
                                UpdatedDt = date
                            };

                            AllField af = null;
                            if (oAf != null)
                            {
                                string partialView = await _itemCodeService.GetPartialViewAsync(oig.ItemCodeId);

                                //_FieldAlcohol
                                //_FieldBrand
                                //_FieldBrand_A
                                //_FieldBrand_B
                                //_FieldDrugs
                                //_FieldLand
                                //_FieldMultiple
                                //_FieldMultiple_A
                                //_FieldSerial_A
                                //_FieldSerial
                                //_FieldSerial_B
                                //_FieldSerial_C
                                //_FieldSerial_D
                                //_FieldSerial_E
                                //_FieldSerial_F
                                af = new AllField()
                                {
                                    Id = psCardId,
                                    AcqMode = oAf.AcqMode,
                                    InvDist = oAf.InvDist,
                                    InsertedBy = user,
                                    InsertedDt = date,
                                    UpdatedBy = user,
                                    UpdatedDt = date
                                };

                                if (partialView == "_FieldAlcohol")
                                {
                                    af.GenericName = oAf.GenericName;
                                    af.DosageVolume = oAf.DosageVolume;
                                    af.Multipliers = oAf.Multipliers;
                                    af.Brand = oAf.Brand;
                                }
                                else if (partialView.Contains("_FieldBrand"))
                                {
                                    if (partialView == "_FieldBrand")
                                    {
                                        af.Multipliers = oAf.Multipliers;
                                    }
                                    af.Brand = oAf.Brand;

                                    if (partialView == "_FieldBrand_A")
                                    {
                                        if (oAf.Model_.IsNullOrWhiteSpaceX())
                                        {
                                            if (oAf.Dimension.IsNullOrWhiteSpaceX())
                                            {
                                                if (oAf.Size.IsNullOrWhiteSpaceX())
                                                {
                                                    if (oAf.Weight.IsNullOrWhiteSpaceX())
                                                    {
                                                        if (oAf.Materials.IsNullOrWhiteSpaceX())
                                                        {
                                                            if (oAf.Capacity.IsNullOrWhiteSpaceX())
                                                            {
                                                                af.Color = oAf.Color;
                                                            }
                                                            else
                                                            {
                                                                af.Capacity = oAf.Capacity;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            af.Materials = oAf.Materials;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        af.Weight = oAf.Weight;
                                                    }
                                                }
                                                else
                                                {
                                                    af.Size = oAf.Size;
                                                }
                                            }
                                            else
                                            {
                                                af.Dimension = oAf.Dimension;
                                            }
                                        }
                                        else
                                        {
                                            af.Model_ = oAf.Model_;
                                        }
                                    }
                                    else
                                    {
                                        af.Model_ = oAf.Model_;
                                    }
                                }
                                else if (partialView == "_FieldDrugs")
                                {
                                    af.GenericName = oAf.GenericName;
                                    af.DosageStrength = oAf.DosageStrength;
                                    af.DosageForm = oAf.DosageForm;
                                    af.DosageVolume = oAf.DosageVolume;
                                    af.Multipliers = oAf.Multipliers;
                                    af.Brand = oAf.Brand;
                                }
                                else if (partialView == "_FieldLand")
                                {
                                    af.Area = oAf.Area;
                                }
                                else if (partialView == "_FieldMultiple")
                                {
                                    af.Multipliers = oAf.Multipliers;
                                }
                                else if (partialView == "_FieldMultiple_A")
                                {
                                    af.Multipliers = oAf.Multipliers;
                                    af.Brand = oAf.Brand;
                                }
                                else if (partialView == "_FieldSerial")
                                {
                                    if (oAf.Model_.IsNullOrWhiteSpaceX())
                                    {
                                        af.PropNo = oAf.PropNo;
                                    }
                                    else
                                    {
                                        af.SerialNo = oAf.SerialNo;
                                    }

                                    af.Multipliers = oAf.Multipliers;
                                    af.Brand = oAf.Brand;

                                    if (oAf.Model_.IsNullOrWhiteSpaceX())
                                    {
                                        if (oAf.Dimension.IsNullOrWhiteSpaceX())
                                        {
                                            if (oAf.Size.IsNullOrWhiteSpaceX())
                                            {
                                                if (oAf.Weight.IsNullOrWhiteSpaceX())
                                                {
                                                    if (oAf.Materials.IsNullOrWhiteSpaceX())
                                                    {
                                                        if (oAf.Capacity.IsNullOrWhiteSpaceX())
                                                        {
                                                            af.Color = oAf.Color;
                                                        }
                                                        else
                                                        {
                                                            af.Capacity = oAf.Capacity;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        af.Materials = oAf.Materials;
                                                    }
                                                }
                                                else
                                                {
                                                    af.Weight = oAf.Weight;
                                                }
                                            }
                                            else
                                            {
                                                af.Size = oAf.Size;
                                            }
                                        }
                                        else
                                        {
                                            af.Dimension = oAf.Dimension;
                                        }
                                    }
                                    else
                                    {
                                        af.Model_ = oAf.Model_;
                                    }
                                }
                                else if (partialView == "_FieldSerial_A")
                                {
                                    if (oAf.PlateNo.IsNullOrWhiteSpaceX())
                                    {
                                        if (oAf.BodyNo.IsNullOrWhiteSpaceX())
                                        {
                                            if (!oAf.MVFileNo.IsNullOrWhiteSpaceX())
                                            {
                                                af.MVFileNo = oAf.MVFileNo;
                                            }
                                        }
                                        else
                                        {
                                            af.BodyNo = oAf.BodyNo;
                                        }
                                    }
                                    else
                                    {
                                        af.PlateNo = oAf.PlateNo;
                                    }

                                    af.Multipliers = oAf.Multipliers;
                                    af.Brand = oAf.Brand;

                                    if (oAf.Model_.IsNullOrWhiteSpaceX())
                                    {
                                        if (oAf.Dimension.IsNullOrWhiteSpaceX())
                                        {
                                            if (oAf.Size.IsNullOrWhiteSpaceX())
                                            {
                                                if (oAf.Weight.IsNullOrWhiteSpaceX())
                                                {
                                                    if (oAf.Materials.IsNullOrWhiteSpaceX())
                                                    {
                                                        if (oAf.Capacity.IsNullOrWhiteSpaceX())
                                                        {
                                                            af.Color = oAf.Color;
                                                        }
                                                        else
                                                        {
                                                            af.Capacity = oAf.Capacity;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        af.Materials = oAf.Materials;
                                                    }
                                                }
                                                else
                                                {
                                                    af.Weight = oAf.Weight;
                                                }
                                            }
                                            else
                                            {
                                                af.Size = oAf.Size;
                                            }
                                        }
                                        else
                                        {
                                            af.Dimension = oAf.Dimension;
                                        }
                                    }
                                    else
                                    {
                                        af.Model_ = oAf.Model_;
                                    }
                                }
                                else if (partialView == "_FieldSerial_B")
                                {
                                    if (oAf.PlateNo.IsNullOrWhiteSpaceX())
                                    {
                                        af.PropNo = oAf.PropNo;
                                    }
                                    else
                                    {
                                        af.SerialNo = oAf.SerialNo;
                                    }

                                    af.Multipliers = oAf.Multipliers;
                                }
                                else if (partialView == "_FieldSerial_C")
                                {
                                    if (oAf.PlateNo.IsNullOrWhiteSpaceX())
                                    {
                                        if (oAf.BodyNo.IsNullOrWhiteSpaceX())
                                        {
                                            if (!oAf.MVFileNo.IsNullOrWhiteSpaceX())
                                            {
                                                af.MVFileNo = oAf.MVFileNo;
                                            }
                                        }
                                        else
                                        {
                                            af.BodyNo = oAf.BodyNo;
                                        }
                                    }
                                    else
                                    {
                                        af.PlateNo = oAf.PlateNo;
                                    }

                                    af.Multipliers = oAf.Multipliers;
                                }
                                else if (partialView == "_FieldSerial_D")
                                {
                                    if (oAf.PlateNo.IsNullOrWhiteSpaceX())
                                    {
                                        af.PropNo = oAf.PropNo;
                                    }
                                    else
                                    {
                                        af.SerialNo = oAf.SerialNo;
                                    }

                                    af.Brand = oAf.Brand;

                                    if (oAf.Model_.IsNullOrWhiteSpaceX())
                                    {
                                        if (oAf.Dimension.IsNullOrWhiteSpaceX())
                                        {
                                            if (oAf.Size.IsNullOrWhiteSpaceX())
                                            {
                                                if (oAf.Weight.IsNullOrWhiteSpaceX())
                                                {
                                                    if (oAf.Materials.IsNullOrWhiteSpaceX())
                                                    {
                                                        if (oAf.Capacity.IsNullOrWhiteSpaceX())
                                                        {
                                                            af.Color = oAf.Color;
                                                        }
                                                        else
                                                        {
                                                            af.Capacity = oAf.Capacity;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        af.Materials = oAf.Materials;
                                                    }
                                                }
                                                else
                                                {
                                                    af.Weight = oAf.Weight;
                                                }
                                            }
                                            else
                                            {
                                                af.Size = oAf.Size;
                                            }
                                        }
                                        else
                                        {
                                            af.Dimension = oAf.Dimension;
                                        }
                                    }
                                    else
                                    {
                                        af.Model_ = oAf.Model_;
                                    }
                                }
                                else if (partialView == "_FieldSerial_E")
                                {
                                    if (oAf.PlateNo.IsNullOrWhiteSpaceX())
                                    {
                                        if (oAf.BodyNo.IsNullOrWhiteSpaceX())
                                        {
                                            if (!oAf.MVFileNo.IsNullOrWhiteSpaceX())
                                            {
                                                af.MVFileNo = oAf.MVFileNo;
                                            }
                                        }
                                        else
                                        {
                                            af.BodyNo = oAf.BodyNo;
                                        }
                                    }
                                    else
                                    {
                                        af.PlateNo = oAf.PlateNo;
                                    }
                                }
                                else if (partialView == "_FieldSerial_F")
                                {
                                    if (oAf.PlateNo.IsNullOrWhiteSpaceX())
                                    {
                                        af.PropNo = oAf.PropNo;
                                    }
                                    else
                                    {
                                        af.SerialNo = oAf.SerialNo;
                                    }
                                }

                                psCard.AllField = af;
                            }
                        }

                        var psCardItem = await _db.PsCardItems.Where(w => w.OrderItemRequestId == orderItemRequest.Id).FirstOrDefaultAsync();
                        if (psCardItem == null)
                        {
                            //var unitGroupDescriptionItem = _db.OrderItemUnitGroupDescriptionItems.Include(i => i.OrderItemUnitGroupDescription.OrderItemUnitGroup).Where(w => w.OrderItemId == orderItem.Id).FirstOrDefault();
                            //var setQty = unitGroupDescriptionItem == null ? 1 : unitGroupDescriptionItem.OrderItemUnitGroupDescription.OrderItemUnitGroup.Qty;
                            //var setLotNo = unitGroupDescriptionItem == null ? "" : orderItem.Order.PoNo.Trim() + "-" + unitGroupDescriptionItem.OrderItemUnitGroupDescription.OrderItemUnitGroup.SetLotNo;
                            //var setLotAmount = unitGroupDescriptionItem == null ? 0 : unitGroupDescriptionItem.OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost;
                            //var setLotRemarks = unitGroupDescriptionItem == null ? "" : unitGroupDescriptionItem.OrderItemUnitGroupDescription.Description;
                            var psCardItemId = Guid.NewGuid();
                            psCardItem = new PsCardItem()
                            {
                                Id = psCardItemId,
                                GroupId = psCardItemId,
                                PsCardId = psCard.Id,
                                OrderItemRequestId = orderItemRequest.Id,
                                PoDate = orderItem.Order.PoDate,
                                PoNo = orderItem.Order.PoNo,
                                AirDate = entity.AIRDate,
                                AirNo = entity.AIRNo,
                                //Qty = (int)orderItem.Qty * setQty,
                                Qty = (int)orderItem.Qty,
                                QtyIss = 0,
                                //QtyBal = (int)orderItem.Qty * setQty,
                                QtyBal = (int)orderItem.Qty,
                                Amount = orderItem.Amount,
                                Unit = orderItem.Unit,
                                UnitCost = orderItem.UnitCost,
                                PriceRate = orderItem.PriceRate,
                                AddCost = 0,
                                TUnitCost = orderItem.UnitCost,
                                GTotalCost = orderItem.Amount,
                                DeptId = deptId,
                                DeptDisplay = office,
                                Description = oig.Description,
                                OtherDesc = orderItem.OtherDesc,
                                Type = oAf.Type,
                                InvDist = oig.InvDist,
                                FPP = orderItemRequest.RequestItem.Request.FPP,
                                //SetLotNo = setLotNo,
                                //SetLotAmount = setLotAmount,
                                //SetLotRemarks = setLotRemarks,
                                InsertedBy = user,
                                InsertedDt = date,
                                UpdatedBy = user,
                                UpdatedDt = date,
                                PostedBy = user,
                                PostedDt = date,
                                PpmpCode = orderItem.PpmpCode,
                                //IsConsumable = _itemCodeService.GetIsConsumable(orderItem.ItemCode.IsConsumable)
                                //IsConsumable = orderItem.AIRItems.FirstOrDefault()?.IsConsumable
                                IsConsumable = orderItemRequest.AIRItems.FirstOrDefault()?.IsConsumable
                            };

                            var psCardItemTransfer = new PsCardItemTransfer()
                            {
                                Id = Guid.NewGuid(),
                                PsCardItemId = psCardItem.Id,
                                //Qty = (int)orderItem.Qty * setQty,
                                Qty = (int)orderItem.Qty,
                                QtyIss = 0,
                                //QtyBal = (int)orderItem.Qty * setQty,
                                QtyBal = (int)orderItem.Qty,
                                Amount = orderItem.Amount,
                                TranType = "I",
                                InsertedBy = user,
                                InsertedDt = date,
                                UpdatedBy = user,
                                UpdatedDt = date
                            };

                            // include ItemExtns
                            var airItemExtnOthers = _airItemService.AirItemExtn.GetAirItemExtnByOrderItemRequestId<AIRItemExtnOther>(orderItemRequest.Id);
                            foreach (var airItemExtnOther in airItemExtnOthers)
                            {
                                var psCardItemExtnOther = await _db.PsCardItemExtns.OfType<PsCardItemExtnOther>()
                                    .FirstOrDefaultAsync(f => f.AIRItemExtnId == airItemExtnOther.Id);
                                if (psCardItemExtnOther == null)
                                {
                                    var psCardItemExtnId = Guid.NewGuid();
                                    psCardItemExtnOther = new PsCardItemExtnOther()
                                    {
                                        Id = psCardItemExtnId,
                                        GroupId = psCardItemExtnId,
                                        PsCardItemId = psCardItem.Id,
                                        AIRItemExtnId = airItemExtnOther.Id,
                                        SetLotNo = airItemExtnOther.SetLotNo,
                                        SetLotQtyNo = airItemExtnOther.SetLotQtyNo,
                                        ContentNo = airItemExtnOther.ContentNo,
                                        SerialNo = airItemExtnOther.SerialNo,
                                        AddCost = 0,
                                        AcqCost = airItemExtnOther.AIRItem.OrderItemRequest.OrderItem.UnitCost,
                                        InsertedBy = user,
                                        InsertedDt = date,
                                        UpdatedBy = user,
                                        UpdatedDt = date
                                    };
                                    psCardItem.PsCardItemExtns.Add(psCardItemExtnOther);
                                }
                            }

                            var airItemExtnVehicles = _airItemService.AirItemExtn.GetAirItemExtnByOrderItemRequestId<AIRItemExtnVehicle>(orderItemRequest.Id);
                            foreach (var airItemExtnVehicle in airItemExtnVehicles)
                            {
                                var psCardItemExtnVehicle = await _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>()
                                        .FirstOrDefaultAsync(f => f.AIRItemExtnId == airItemExtnVehicle.Id);
                                if (psCardItemExtnVehicle == null)
                                {
                                    var psCardItemExtnId = Guid.NewGuid();
                                    psCardItemExtnVehicle = new PsCardItemExtnVehicle()
                                    {
                                        Id = psCardItemExtnId,
                                        GroupId = psCardItemExtnId,
                                        PsCardItemId = psCardItem.Id,
                                        AIRItemExtnId = airItemExtnVehicle.Id,
                                        SetLotNo = airItemExtnVehicle.SetLotNo,
                                        SetLotQtyNo = airItemExtnVehicle.SetLotQtyNo,
                                        ContentNo = airItemExtnVehicle.ContentNo,
                                        YearModel = airItemExtnVehicle.YearModel,
                                        PlateNo = airItemExtnVehicle.PlateNo,
                                        BodyNo = airItemExtnVehicle.BodyNo,
                                        EngineNo = airItemExtnVehicle.EngineNo,
                                        ChasisNo = airItemExtnVehicle.ChasisNo,
                                        Color = airItemExtnVehicle.Color,
                                        CRN = airItemExtnVehicle.CRN,
                                        CRDate = airItemExtnVehicle.CRDate,
                                        MVFileNo = airItemExtnVehicle.MVFileNo,
                                        OrNo = airItemExtnVehicle.OrNo,
                                        OrDate = airItemExtnVehicle.OrDate,
                                        NetWeight = airItemExtnVehicle.NetWeight,
                                        InsPolicyNo = airItemExtnVehicle.InsPolicyNo,
                                        ConductionNo = airItemExtnVehicle.ConductionNo,
                                        //ParReissuance = airItemExtnVehicle.ParReissuance,
                                        //Condition = airItemExtnVehicle.Condition,
                                        SubLocation = airItemExtnVehicle.SubLocation,
                                        AddCost = 0,
                                        AcqCost = airItemExtnVehicle.AIRItem.OrderItemRequest.OrderItem.UnitCost,
                                        InsertedBy = user,
                                        InsertedDt = date,
                                        UpdatedBy = user,
                                        UpdatedDt = date
                                    };
                                    psCardItem.PsCardItemExtns.Add(psCardItemExtnVehicle);
                                }
                            }

                            foreach (var psCardItemExtn in psCardItem.PsCardItemExtns)
                            {
                                var psCardItemTransferItem = new PsCardItemTransferItem()
                                {
                                    Id = Guid.NewGuid(),
                                    PsCardItemTransferId = psCardItemTransfer.Id,
                                    PsCardItemExtnId = psCardItemExtn.Id,
                                    InsertedBy = user,
                                    InsertedDt = date,
                                    UpdatedBy = user,
                                    UpdatedDt = date
                                };
                                psCardItemTransfer.PsCardItemTransferItems.Add(psCardItemTransferItem);
                            }
                            psCardItem.PsCardItemTransfers.Add(psCardItemTransfer);
                            psCard.PsCardItems.Add(psCardItem);
                        }

                        if (isNew == true)
                        {
                            _db.PsCards.Add(psCard);
                        }

                        psCardIdList.Add(psCard.Id);
                        await _db.SaveChangesAsync();
                    }
                }
            }

            //await _db.SaveChangesAsync();

            //foreach (var psCardId in psCardIdList)
            //{
            //    var psCardItemList = _db.PsCardItems.Include(i => i.OrderItemRequest).Where(w => w.PsCardId == psCardId).ToList();
            //    foreach (var psCardItem in psCardItemList)
            //    {
            //        // search unit group if any
            //        var orderItemUnitGroupDescriptionItem = await _db.OrderItemUnitGroupDescriptionItems
            //            .Include(i => i.OrderItemUnitGroupDescription.OrderItemUnitGroup)
            //            .Include(i => i.OrderItem)
            //            .Where(w => w.OrderItemId == psCardItem.OrderItemRequest.OrderItemId).FirstOrDefaultAsync();
            //        if (orderItemUnitGroupDescriptionItem != null)
            //        {
            //            // search in psCard unit group
            //            if (!await _db.PsCardItemUnitGroupDescriptionItems.Where(w => w.PsCardItemId == psCardItem.Id).AnyAsync())
            //            {
            //                var psCardItemUnitGroup = await _db.PsCardItemUnitGroups.Include(i => i.PsCardItemUnitGroupDescriptions).Where(w => w.PoNo == psCardItem.PoNo).FirstOrDefaultAsync();
            //                if (psCardItemUnitGroup == null)
            //                {
            //                    psCardItemUnitGroup = new PsCardItemUnitGroup()
            //                    {
            //                        Id = Guid.NewGuid(),
            //                        PoNo = psCardItem.PoNo,
            //                        SetLotNo = orderItemUnitGroupDescriptionItem.OrderItemUnitGroupDescription.OrderItemUnitGroup.SetLotNo,
            //                        Qty = orderItemUnitGroupDescriptionItem.OrderItemUnitGroupDescription.OrderItemUnitGroup.Qty,
            //                        Unit = orderItemUnitGroupDescriptionItem.OrderItemUnitGroupDescription.OrderItemUnitGroup.Unit,
            //                        UnitCost = orderItemUnitGroupDescriptionItem.OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost,
            //                        TotalCost = orderItemUnitGroupDescriptionItem.OrderItemUnitGroupDescription.OrderItemUnitGroup.TotalCost,
            //                        AddCost = 0,
            //                        TUnitCost = 0,
            //                        GTotalCost = orderItemUnitGroupDescriptionItem.OrderItemUnitGroupDescription.OrderItemUnitGroup.TotalCost,
            //                        InsertedBy = user,
            //                        InsertedDt = date,
            //                        UpdatedBy = user,
            //                        UpdatedDt = date
            //                    };
            //                    var psCardItemUnitGroupDescription = new PsCardItemUnitGroupDescription()
            //                    {
            //                        Id = Guid.NewGuid(),
            //                        UnitGroupId = psCardItemUnitGroup.Id,
            //                        Description = orderItemUnitGroupDescriptionItem.OrderItemUnitGroupDescription.Description,
            //                        InsertedBy = user,
            //                        InsertedDt = date,
            //                        UpdatedBy = user,
            //                        UpdatedDt = date
            //                    };
            //                    var psCardItemUnitGroupDescriptionItem = new PsCardItemUnitGroupDescriptionItem()
            //                    {
            //                        Id = Guid.NewGuid(),
            //                        UnitGroupDescriptionId = psCardItemUnitGroupDescription.Id,
            //                        PsCardItemId = psCardItem.Id,
            //                        PoQty = (int?)orderItemUnitGroupDescriptionItem.OrderItem.Qty,
            //                        InsertedBy = user,
            //                        InsertedDt = date,
            //                        UpdatedBy = user,
            //                        UpdatedDt = date
            //                    };
            //                    psCardItemUnitGroupDescription.PsCardItemUnitGroupDescriptionItems.Add(psCardItemUnitGroupDescriptionItem);
            //                    psCardItemUnitGroup.PsCardItemUnitGroupDescriptions.Add(psCardItemUnitGroupDescription);
            //                    _db.PsCardItemUnitGroups.Add(psCardItemUnitGroup);
            //                }
            //                else
            //                {
            //                    // if with psCardItemUnitGroup, check UnitGroupDescription
            //                    var psCardItemUnitGroupDescription = psCardItemUnitGroup.PsCardItemUnitGroupDescriptions
            //                        .Where(w => w.Description == orderItemUnitGroupDescriptionItem.OrderItemUnitGroupDescription.Description)
            //                        .FirstOrDefault();
            //                    if (psCardItemUnitGroupDescription == null)
            //                    {
            //                        psCardItemUnitGroupDescription = new PsCardItemUnitGroupDescription()
            //                        {
            //                            Id = Guid.NewGuid(),
            //                            UnitGroupId = psCardItemUnitGroup.Id,
            //                            Description = orderItemUnitGroupDescriptionItem.OrderItemUnitGroupDescription.Description,
            //                            InsertedBy = user,
            //                            InsertedDt = date,
            //                            UpdatedBy = user,
            //                            UpdatedDt = date
            //                        };
            //                        var psCardItemUnitGroupDescriptionItem = new PsCardItemUnitGroupDescriptionItem()
            //                        {
            //                            Id = Guid.NewGuid(),
            //                            UnitGroupDescriptionId = psCardItemUnitGroupDescription.Id,
            //                            PsCardItemId = psCardItem.Id,
            //                            PoQty = (int?)orderItemUnitGroupDescriptionItem.OrderItem.Qty,
            //                            InsertedBy = user,
            //                            InsertedDt = date,
            //                            UpdatedBy = user,
            //                            UpdatedDt = date
            //                        };
            //                        psCardItemUnitGroupDescription.PsCardItemUnitGroupDescriptionItems.Add(psCardItemUnitGroupDescriptionItem);
            //                        _db.PsCardItemUnitGroupDescriptions.Add(psCardItemUnitGroupDescription);
            //                    }
            //                    else
            //                    {
            //                        // if with UnitGroupDescription, check UnitGroupDescriptionItem
            //                        var psCardItemUnitGroupDescriptionItem = psCardItemUnitGroupDescription.PsCardItemUnitGroupDescriptionItems
            //                            .Where(w => w.PsCardItemId == psCardItem.Id).FirstOrDefault();
            //                        if (psCardItemUnitGroupDescriptionItem == null)
            //                        {
            //                            psCardItemUnitGroupDescriptionItem = new PsCardItemUnitGroupDescriptionItem()
            //                            {
            //                                Id = Guid.NewGuid(),
            //                                UnitGroupDescriptionId = psCardItemUnitGroupDescription.Id,
            //                                PsCardItemId = psCardItem.Id,
            //                                PoQty = (int?)orderItemUnitGroupDescriptionItem.OrderItem.Qty,
            //                                InsertedBy = user,
            //                                InsertedDt = date,
            //                                UpdatedBy = user,
            //                                UpdatedDt = date
            //                            };
            //                            _db.PsCardItemUnitGroupDescriptionItems.Add(psCardItemUnitGroupDescriptionItem);
            //                        }
            //                    }
            //                }
            //                await _db.SaveChangesAsync();
            //            }
            //        }
            //    }
            //}
            return entity;
        });


        private async Task ValidateCompletePartialAsync(AIR_VM model)
        {
            if (model.AirGroup == (int)AirGroup.ACCEPTANCE)
            {
                if (model.IsComplete.Value == true)
                {
                    var airNo = (await _db.AIRs.FirstOrDefaultAsync(a => a.OrderId == model.OrderId && a.Id != model.Id && a.IsComplete.Value == true))?.AIRNo;
                    if (!string.IsNullOrWhiteSpace(airNo)) {
                        throw new InvalidValueException($"An exissting AIR No. {airNo} has already been marked complete for this PO No.");
                    }
                    await ValidateQtyAsync(model.OrderId);
                }
            }
        }

        private async Task ValidateCompleteAsync(Guid? orderId)
        {
            if (!(await _db.AIRs.AnyAsync(a => a.OrderId == orderId && a.IsComplete.Value == true)))
            {                
                throw new InvalidValueException($"No AIR Number has been marked complete for this PO No.");                
            }
        }

        private async Task ValidateQtyAsync(Guid? orderId)
        {
            var orderItemRequests = await _db.OrderItemRequests
                .Include(i => i.OrderItem.Order)
                .Where(w => w.OrderItem.OrderId == orderId).AsNoTracking().ToListAsync();

            foreach(var orderItemRequest in orderItemRequests)
            {
                var airQty = _db.AIRItems.Include(i => i.AIR.AIRInvoices).Where(w => w.OrderItemRequestId == orderItemRequest.Id).Sum(s => s.Qty) ?? 0;
                if (airQty < orderItemRequest.QtyApplied)
                {
                    throw new InvalidValueException($"The total quantity for {orderItemRequest.OrderItem.Description} must be {orderItemRequest.QtyApplied}.");
                }                
            }
        }

        private async Task<bool> IsWwithUploadAsync(Guid? id)
        {
            var result = await _uploadService.GetAllByImageId(id).AnyAsync();
            return result;
        }

        private async Task ValidateUploadAsync(Guid? id, string ctrlNo)
        {
            if (!await IsWwithUploadAsync(id))
            {
                throw new InvalidValueException($"No attachments found for AIR Control No. {ctrlNo}.");
            }
        }

        public ValueTask<AIR> UnpostAsync(Guid airId, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.AIRs.FindAsync(airId);
            if (entity == null)
            {
                throw new NotFoundException(airId);
            }

            if (!await IsPostedAsync(airId))
            {
                throw new RecordNotYetPostedException();
            }

            //if (await _db.PsCardItemIssuances.AsNoTracking().AnyAsync(a => a.PsCardItem.OrderItemId == entity.OrderId))
            //{
            //    throw new RecordRelationshipException("Items were already issued, cannot unpost!");
            //}

            /*
             * Check if in transit or issued
             */                       

            var psCardItemTransfers = _db.PsCardItemTransfers.Where(w => w.PsCardItem.OrderItemRequest.OrderItem.OrderId == entity.OrderId);
            if (await psCardItemTransfers.AnyAsync(a => a.ParentId != null))
            {
                throw new RecordRelationshipException("Items were already transitted/transferred, cannot unpost!");
            }

            if (await psCardItemTransfers.AnyAsync(a => a.PsCardItemTransferIssuances.Any()))
            {
                throw new RecordRelationshipException("Items were already issued, cannot unpost!");
            }

            if (await _db.IcsParItems.Include(i => i.PsCardItemExtn.PsCardItem).AsNoTracking()
                .AnyAsync(a => a.PsCardItemExtn.PsCardItem.OrderItemRequest.OrderItem.OrderId == entity.OrderId))
            {
                throw new RecordRelationshipException("PAR/ICS already issued, cannot unpost!");
            }

            // recheck
            if (await _db.AIRItems.AnyAsync(a => a.AirId == entity.Id && a.AIRItemExtns
                .Any(b => b.PsCardItemExtns
                    .Any(c => c.IcsParItems.Any()))))
            {
                throw new RecordRelationshipException("PAR/ICS already issued, cannot unpost!");
            }

            /*
                * Delete the following records onUnpost:
                * PsCardItems, Fields..., PsCardItemExtns
                * PsCards --> if no PsItem                
            */

            var orderItemRequests = await _db.OrderItemRequests
                .Include(i => i.OrderItem)
                .Where(w => w.OrderItem.OrderId == entity.OrderId).ToListAsync();

            foreach (var orderItemRequest in orderItemRequests)
            {
                var psCardItems = await _db.PsCardItems
                    .Include(i => i.PsCardItemExtns)
                    .Include(i => i.PsCardItemTransfers)
                    .Where(w => w.OrderItemRequestId == orderItemRequest.Id).ToListAsync();
                Guid? psCardId = psCardItems?.FirstOrDefault()?.PsCardId;

                foreach (var psCardItem in psCardItems)
                {
                    //// check unit groups           
                    //var unitGroupDescriptionItems = _db.PsCardItemUnitGroupDescriptionItems.Where(w => w.PsCardItemId == psCardItem.Id);
                    //if (unitGroupDescriptionItems.Any())
                    //{
                    //    _db.PsCardItemUnitGroupDescriptionItems.RemoveRange(unitGroupDescriptionItems);
                    //    await _db.SaveChangesAsync();
                    //}

                    //var unitGroupDescriptions = _db.PsCardItemUnitGroupDescriptions.Where(w => w.PsCardItemUnitGroup.PoNo == psCardItem.PoNo && !w.PsCardItemUnitGroupDescriptionItems.Any());
                    //if (unitGroupDescriptions.Any())
                    //{
                    //    _db.PsCardItemUnitGroupDescriptions.RemoveRange(unitGroupDescriptions);
                    //    await _db.SaveChangesAsync();
                    //}

                    //var unitGroups = _db.PsCardItemUnitGroups.Where(w => w.PoNo == psCardItem.PoNo && !w.PsCardItemUnitGroupDescriptions.Any());
                    //if (unitGroups.Any())
                    //{
                    //    _db.PsCardItemUnitGroups.RemoveRange(unitGroups);
                    //    await _db.SaveChangesAsync();
                    //}

                    if (psCardItem.PsCardItemTransfers.Any())
                    {
                        _db.PsCardItemTransfers.RemoveRange(psCardItem.PsCardItemTransfers);
                        await _db.SaveChangesAsync();
                    }

                    if (psCardItem.PsCardItemExtns.Any())
                    {
                        _db.PsCardItemExtns.RemoveRange(psCardItem.PsCardItemExtns);
                        await _db.SaveChangesAsync();
                    }

                    var item = await _db.PsCardItems.FirstOrDefaultAsync(f => f.Id == psCardItem.Id);
                    if (item != null)
                    {
                        if (item.OrderItemRequestId != null) // update ris
                        {
                            var risItem = await _db.RisItems.FirstOrDefaultAsync(f => f.OrderItemRequestId == item.OrderItemRequestId);
                            if (risItem != null)
                            {
                                risItem.QtyIssue = null;
                                risItem.UpdatedBy = user;
                                risItem.UpdatedDt = date;
                            }
                        }

                        item.UpdatedBy = user;
                        item.UpdatedDt = date;

                        //_db.PsCardItems.Attach(item);
                        //_db.Entry(item).State = EntityState.Modified;
                        await _db.SaveChangesAsync();

                        _db.PsCardItems.Remove(item);
                        //_db.Entry(item).State = EntityState.Deleted;
                        await _db.SaveChangesAsync();
                    }
                }

                if (psCardId != null)
                {
                    if (!_db.PsCardItems.Any(a => a.PsCardId == psCardId)) // no other  order item is using this item
                    {
                        var psCard = await _db.PsCards.Include(i => i.AllField).Where(w => w.Id == psCardId).FirstOrDefaultAsync();
                        psCard.UpdatedBy = user;
                        psCard.UpdatedDt = date;

                        await _db.SaveChangesAsync();

                        // delete stock during unpost if not used by other order item
                        _db.PsCards.Remove(psCard);
                        await _db.SaveChangesAsync();
                    }
                }
            }

            var airs = await _db.AIRs
                .Where(w => w.AIRItems.Any(a => a.OrderItemRequest.OrderItem.OrderId == entity.OrderId)).ToListAsync();
            foreach (var air in airs)
            {
                air.PostedBy = null;
                air.PostedDt = null;
            }
            
            //entity.UpdatedBy = user;
            //entity.UpdatedDt = date;

            //_db.AIRs.Attach(entity);
            //_db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        //private ValueTask<AIR> UpdatePsItem(AIR entity, string user, DateTime date, bool post) => _exceptionService.TryCatch(async () =>
        //{
        //    var orderItemIdList = await _db.AIRItems.Where(w => w.AirId == entity.Id).GroupBy(g => g.OrderItemId)
        //        .Select(s => s.Key).ToListAsync();
        //    foreach (var orderItemId in orderItemIdList)
        //    {
        //        decimal? qtyAccepted = 0;
        //        if (post)
        //        {
        //            qtyAccepted = _db.AIRItems.Where(w => w.OrderItemId == orderItemId).Sum(s => s.Qty);
        //        }
        //        var psCardItem = await _db.PsCardItems
        //            // .Include(i => i.OrderItem.RequestItem.RisItem)
        //            .Include(i => i.OrderItem)
        //            .Where(w => w.OrderItemId == orderItemId).FirstOrDefaultAsync();
        //        if (psCardItem != null)
        //        {
        //            psCardItem.AirNo = entity.AIRNo;
        //            psCardItem.AirDate = entity.AIRDate;
        //            psCardItem.Qty = (int)qtyAccepted;
        //            psCardItem.QtyBal = (int)qtyAccepted - psCardItem.QtyIss;
        //            psCardItem.UpdatedBy = user;
        //            psCardItem.UpdatedDt = date;
        //            //psCardItem.Description = psCardItem.OrderItem.RequestItem.RisItem.Description;
        //            //psCardItem.OtherDesc = psCardItem.OrderItem.RequestItem.RisItem.OtherDesc;
        //            psCardItem.Description = psCardItem.OrderItem.Description;
        //            psCardItem.OtherDesc = psCardItem.OrderItem.OtherDesc;
        //            _db.PsCardItems.Attach(psCardItem);
        //            _db.Entry(psCardItem).State = EntityState.Modified;
        //            await _db.SaveChangesAsync();
        //        }
        //    }
        //    return entity;
        //});

        public ValueTask<AIR_VM> SaveAsync(AIR_VM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            //if (model.Id == Guid.Empty || model.Id == null)
            //{
            //    return await CreateAsync(model, user, date);
            //}
            if (await _db.AIRs.AnyAsync(a => a.Id == model.Id))
            {
                return await UpdateAsync(model, user, date);
            }
            else
            {
                return await CreateAsync(model, user, date);
            }
        });

        public ValueTask<AIR_VM> CreateAsync(AIR_VM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            await ValidateOnCreate(model);
            await ValidateOnCreateUpdate(model, Mode.ADD);
            ValidateAirNo(model);

            model.Id = (model.Id == Guid.Empty || model.Id == null) ? Guid.NewGuid() : model.Id;
            //if (string.IsNullOrWhiteSpace(model.AIRNo))
            //{
            //    model.AIRNo = NextAirNo((DateTime)model.AIRDate);
            //}            

            model.CtrlNo = NextCtrlNo(date);
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = new AIR();
            MapModelToEntityFields(entity, model, Mode.ADD);
            await SetAirItemsAsync(entity, model, user, date);

            _db.AIRs.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<AIR_VM> UpdateAsync(AIR_VM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            await ValidateOnUpdate(model);
            await ValidateOnCreateUpdate(model, Mode.EDIT);
            ValidateAirNo(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.AIRs.FindAsync(model.Id);            

            MapModelToEntityFields(entity, model, Mode.EDIT);

            if (model.AirGroup == (int)AirGroup.ACCEPTANCE)
            {
                await ValidateCompletePartialAsync(model);
            }

            if (model.AirGroup == (int)AirGroup.INSPECTION)
            {
                // if there's a change of Order item
                if (entity.OrderId != model.OrderId)
                {
                    var airItems = _db.AIRItems.Where(w => w.AirId == model.Id);
                    await airItems.ForEachAsync(f =>
                    {
                        f.UpdatedBy = model.UpdatedBy;
                        f.UpdatedDt = model.UpdatedDt;
                    });
                    await _db.SaveChangesAsync();

                    _db.AIRItems.RemoveRange(airItems);
                    await _db.SaveChangesAsync();
                }

                await SetAirItemsAsync(entity, model, user, date);
            }            

            await _db.SaveChangesAsync();

            return model;
        });

        private async Task SetAirItemsAsync(AIR entity, AIR_VM model, string user, DateTime date)
        {
            // include items during add except null ItemCodeId (set/lot items)
            var orderItemRequests = await _db.OrderItemRequests
                //.Include(i => i.OrderItem.Order.OrderItemUnitGroups)
                .Include(i => i.OrderItem.ItemCode.ItemType)
                .Include(i => i.AIRItems)
                .Where(w => w.OrderItem.OrderId == model.OrderId && (w.AIRItems.Sum(s => s.Qty) ?? 0) < w.QtyApplied)
                .AsNoTracking().OrderBy(o => o.InsertedDt).ToListAsync();
            foreach (var orderItemRequest in orderItemRequests)
            {
                var insertedDt = DateTime.Now;
                var invDist = string.Empty;
                bool? isConsumable = null;

                // this will have value if not set or lot item
                if (orderItemRequest.OrderItem.ItemCodeId.HasValue)
                {
                    invDist = _itemCodeService.GetInvDist(orderItemRequest.OrderItem.ItemCodeId);
                    isConsumable = _itemCodeService.GetIsConsumable(orderItemRequest.OrderItem.ItemCode.IsConsumable);

                    if (string.IsNullOrWhiteSpace(invDist))
                    {
                        invDist = model.InvDist;
                    }
                }

                var qty = orderItemRequest.QtyApplied - (_db.AIRItems.Where(w => w.OrderItemRequestId == orderItemRequest.Id).Sum(s => s.Qty) ?? 0);

                AIRItem airItem = new AIRItem()
                {
                    Id = Guid.NewGuid(),
                    AirId = entity.Id,
                    OrderItemRequestId = orderItemRequest.Id,
                    Qty = qty,
                    InvDist = invDist,
                    IsConsumable = isConsumable,
                    InsertedBy = user,
                    InsertedDt = insertedDt,
                    UpdatedBy = user,
                    UpdatedDt = insertedDt
                };

                if (orderItemRequest.OrderItem.ItemCodeId.HasValue)
                {
                    await _airItemService.AirItemExtn.CreateAirItemExtnAsync(airItem, orderItemRequest.OrderItem, user, date);
                }
                entity.AIRItems.Add(airItem);
            }
        }

        private void MapModelToEntityFields(AIR entity, AIR_VM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.CtrlNo = model.CtrlNo;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            entity.AIRNo = model.AIRNo;
            entity.AIRDate = model.AIRDate;
            entity.OrderId = model.OrderId;
            entity.AcceptedDate = model.AcceptedDate;
            entity.IsComplete = model.IsComplete;
            entity.IsPartial = model.IsPartial;
            entity.Custodian = model.Custodian?.Trim() ?? "";
            entity.InspectedDate = model.InspectedDate;
            entity.IsInspected = model.IsInspected;
            entity.Officer = model.Officer?.Trim() ?? "";
            entity.Remarks = model.Remarks?.Trim() ?? "";
            entity.InvDist = model.InvDist;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        public ValueTask<AIR_VM> DeleteAsync(AIR_VM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            await ValidateOnDelete(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.AIRs.FindAsync(model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            await _db.SaveChangesAsync();

            _db.AIRs.Remove(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        private string NextAirNo(DateTime date)
        {
            string yyyy = date.Year.ToString().Trim();
            string mm = date.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = _db.AIRs.Where(w => w.AIRDate.Value.Year == date.Year).OrderByDescending(o => o.AIRNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(data.AIRNo.Split('-')[2]) + 1).ToString();
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

            var order = _db.AIRs.Where(w => w.CtrlNo.Substring(0, 4) == yyyy).OrderByDescending(o => o.CtrlNo).FirstOrDefault();
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

        private async ValueTask ValidateOnCreate(AIR_VM model)
        {
            _imex = new InvalidModelException();
            if (!string.IsNullOrWhiteSpace(model.AIRNo))
            {
                if (await _db.AIRs.AnyAsync(a => a.AIRNo == model.AIRNo))
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.AIRNo)), "Already Exists.");
                }
            }
            _imex.ThrowIfContainsErrors();
        }

        private async ValueTask ValidateOnCreateUpdate(AIR_VM model, Mode mode)
        {
            _imex = new InvalidModelException();
            var order = await _db.Orders.FindAsync(model.OrderId);
            if (order == null)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.OrderId)), "Does not Exists.");
            }
            else
            {
                if (order.PoDate > model.AIRDate)
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.AIRDate)), "AIR Date must be on or after the PO Date.");
                }
            }

            // Multiple AIR per PO is allowed as per COA
            ///*
            // * only 1 AIR per PO
            // */
            //var airOrder = await _db.AIRs.Where(w => w.OrderId == model.OrderId).FirstOrDefaultAsync();
            //if (airOrder != null)
            //{
            //    if (mode == Mode.ADD)
            //    {
            //        _imex.UpsertDataList(_getDisplayName(nameof(model.OrderId)), $"Already used by AIR No. {airOrder.AIRNo}.");
            //    }
            //    else
            //    {
            //        if (airOrder.Id != model.Id)
            //        {
            //            _imex.UpsertDataList(_getDisplayName(nameof(model.OrderId)), $"Already used by AIR No. {airOrder.AIRNo}.");
            //        }
            //    }
            //}

            if (model.IsInspected.HasValue && model.IsInspected.Value == true)
            {
                if (!model.InspectedDate.HasValue)
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.InspectedDate)), "Field is required when inspected.");
                }

                if (string.IsNullOrWhiteSpace(model.Officer))
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.Officer)), "Field is required when inspected.");
                }
            }

            if (model.IsComplete.HasValue && model.IsComplete.Value == true)
            {
                if (!model.IsInspected.HasValue || model.IsInspected.Value == false)
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.IsComplete)), "Cannot mark as complete when not yet inspected.");
                }

                if (!model.AcceptedDate.HasValue)
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.AcceptedDate)), "Field is required when inspected.");
                }

                if (string.IsNullOrWhiteSpace(model.Custodian))
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.Custodian)), "Field is required when accepted.");
                }
            }

            if (model.InspectedDate.HasValue)
            {
                if (model.InspectedDate < model.PoDate)
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.InspectedDate)), "Date Inspected must be on or after the PO Date.");
                }
            }

            if (model.AcceptedDate.HasValue)
            {
                if (!model.InspectedDate.HasValue)
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.AcceptedDate)), "Inspection Date is required.");
                }
                else
                {
                    if (model.AcceptedDate < model.InspectedDate)
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.AcceptedDate)), "Date Received must be on or after the Inspection Date.");
                    }
                }
            }

            _imex.ThrowIfContainsErrors();
        }

        private void ValidateAirNo(AIR_VM model)
        {
            _imex = new InvalidModelException();
            if (!string.IsNullOrWhiteSpace(model.AIRNo))
            {
                if (model.AIRNo.Trim().Length != 12)
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.AIRNo)), "Invalid value.");
                    //throw new InvalidValueException("Invalid AIR No.");
                }
                else
                {
                    if (!model.AIRDate.HasValue)
                    {
                        _imex.UpsertDataList("AIR Date", "Field is required.");
                    }
                    else
                    {
                        var refNoParts = model.AIRNo.Split('-');
                        var refNoYear = int.Parse(refNoParts[0]);
                        var refNoMonth = int.Parse(refNoParts[1]);
                        var refNoSeq = int.Parse(refNoParts[2]);
                        if (refNoSeq == 0)
                        {
                            _imex.UpsertDataList(_getDisplayName(nameof(model.AIRNo)), "Invalid sequence number.");
                        }
                        else
                        {
                            if (refNoYear != model.PoDate.Value.Year || refNoMonth != model.AIRDate.Value.Month)
                            {
                                _imex.UpsertDataList(_getDisplayName(nameof(model.AIRNo)), "Series year and month must match the AIR date’s year and month.");
                                //throw new InvalidValueException("Series Year and month of AIR No. must be same as the year and month of the AIR date.");
                            }
                            //else
                            //{
                            //    //var maxNo = _db.AIRs.Where(w => DbFunctions.TruncateTime(w.AIRDate) <= DbFunctions.TruncateTime(model.AIRDate)).Max(m => m.AIRNo);
                            //    var maxNo = _db.AIRs.Where(w => w.AIRDate.Value.Year == model.AIRDate.Value.Year).Max(m => m.AIRNo);
                            //    if (!string.IsNullOrWhiteSpace(maxNo))
                            //    {
                            //        var maxSeq = int.Parse(maxNo.Split('-')[2]);
                            //        if (refNoSeq < maxSeq)
                            //        {
                            //            _imex.UpsertDataList(_getDisplayName(nameof(model.AIRNo)), $"Series No. must be greater than {maxSeq}");
                            //        }
                            //    }
                            //}
                        }
                    }
                }
            }

            if (model.AIRDate.HasValue)
            {
                if (model.AIRDate > model.UpdatedDt)
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.AIRDate)), $"Future date is not allowed.");
                }

                if (model.PoDate.HasValue)
                {
                    if (model.PoDate > model.AIRDate)
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.AIRDate)), "AIR date must be on or after the PO date.");
                    }
                }
            }

            if (!model.OrderId.HasValue)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.OrderId)), "Field is required.");
            }
            else
            {
                var order = _db.Orders.Find(model.OrderId);
                if (order == null)
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.OrderId)), "Record not found.");
                }
            }

            _imex.ThrowIfContainsErrors();
        }

        private async ValueTask ValidateOnUpdate(AIR_VM model)
        {
            _imex = new InvalidModelException();
            if (await _db.AIRs.FindAsync(model.Id) == null)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.AIRNo)), "Record does not exists.");
            }

            if (!string.IsNullOrWhiteSpace(model.AIRNo))
            {
                if (await _db.AIRs.AnyAsync(a => a.AIRNo == model.AIRNo && a.Id != model.Id))
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.AIRNo)), "Record already exists.");
                }
            }

            if (await IsPostedAsync(model.Id))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.AIRNo)), "Record has been posted, cannot update.");
            }

            _imex.ThrowIfContainsErrors();
        }

        private async ValueTask ValidateOnDelete(AIR_VM model)
        {
            if (await _db.AIRs.FindAsync(model.Id) == null)
            {
                throw new NotFoundException(model.Id);
            }

            if (await IsPostedAsync(model.Id))
            {
                throw new RecordAlreadyPostedException("Record has ben posted, cannot delete.");
            }
        }

        public bool IsPosted(Guid airId)
        {
            return _airAbstractService.IsPosted(airId);
        }

        public bool IsPosted(AIR air)
        {
            return _airAbstractService.IsPosted(air);
        }

        public bool IsPosted(AIRItem airItem)
        {
            return _airAbstractService.IsPosted(airItem);
        }

        public bool IsPosted(AIRItemExtn airItemExtn)
        {
            return _airAbstractService.IsPosted(airItemExtn);
        }

        public async ValueTask<bool> IsPostedAsync(Guid airId)
        {
            return await _airAbstractService.IsPostedAsync(airId);
        }
    }
}