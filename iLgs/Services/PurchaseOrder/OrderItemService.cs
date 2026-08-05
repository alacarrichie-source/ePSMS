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
using System.Text.RegularExpressions;
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

        ValidPriceCapVM ValidatePriceCap(DateTime date, Guid? itemCodeId, decimal? unitCost);
    }

    internal class OrderItemService : BaseValidator, IOrderItemService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<OrderItemVM> _vmExceptionService;
        private readonly ICodextnService _codextnService;
        private readonly IAllFieldService _allFieldService;
        private readonly IItemCodeService _itemCodeService;
        private readonly IPriceCapService _priceCapService;
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
            _itemCodeService = new ItemCodeService(_db);
            _priceCapService = new PriceCapService(_db);
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
                //RequestItemId = s.RequestItemId,
                ItemNo = s.ItemNo,
                ItemNoIndex = s.ItemNoIndex,
                ItemCodeId = s.ItemCodeId,
                PsNo = s.PsNo,
                PsNoDisplay = s.PsNoDisplay,
                ItemName = s.ItemName,
                Description = s.Description,
                Brand = s.Brand,

                //RisItemId = s.RequestItem.RisItem.Id,
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
                SetLotNo = s.OrderItemUnitGroupDescriptionItems.FirstOrDefault().OrderItemUnitGroupDescription.OrderItemUnitGroup.SetLotNo ?? "",
                SetUnitCost = s.OrderItemUnitGroupDescriptionItems.FirstOrDefault().OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost ?? 0,
                UnitGroupDescriptionId = _db.OrderItemUnitGroupDescriptions.FirstOrDefault(f =>
                    f.OrderItemUnitGroup.OrderId == s.OrderId &&
                    f.OrderItemUnitGroup.SetLotNo == s.ItemNo &&
                    f.Description == s.Description).Id,
                PpmpCode = s.PpmpCode,
                Padding = (s.ItemNo.Length - s.ItemNo.Replace(".", "").Length) * 20,
                IsSetLot = s.Unit == "set" || s.Unit == "lot" ? true : false,
                IsSetLotItem = s.ItemNo.Contains("."),
                OriginalDescription = s.Description,
                OriginalOtherDesc = s.OtherDesc,
                ParentItemNo = s.ItemNo.Contains(".") ? s.ItemNo.Substring(0, s.ItemNo.IndexOf(".")) : s.ItemNo
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

        private async Task ValidateFieldsAsync(OrderItemVM model)
        {
            _imex = new InvalidModelException();

            if (model.IsSetLot)
            {
                if (!string.IsNullOrWhiteSpace(model.ItemNo))
                {
                    if (!int.TryParse(model.ItemNo, out var num) || num < 1)
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.ItemNo)), "Decimal places are not allowed for Set/Lot.");
                    }
                }
            }
            else
            {
                if (model.ItemCodeId.HasValue)
                {
                    if (!(await _db.ItemCodes.AnyAsync(a => a.Id == model.ItemCodeId)))
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.ItemCodeId)), "Invalid Value.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(model.ItemNo))
                {
                    var regex = new Regex(@"^(0|[1-9]\d*)(\.(0|[1-9]\d*))*$");
                    if (!regex.IsMatch(model.ItemNo))
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.ItemNo)), "Invalid Value.");
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(model.Unit))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Unit)), "Field is required.");
            }
            else
            {
                if (model.IsSetLot)
                {
                    if (!(await _db.Codextns.Where(w => w.CodeMast.Code == "UNIT-GROUP").AnyAsync()))
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.Unit)), "Invalid Value.");
                    }
                }
                else
                {
                    if (!(await _db.Codextns.Where(w => w.CodeMast.Code == "UNIT" && !(w.Code == "set" || w.Code == "lot")).AnyAsync()))
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.Unit)), "Invalid Value.");
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(model.ItemNo) && !model.ItemNo.Contains(".")) // not part of set
            {
                if (!model.UnitCost.HasValue || model.UnitCost == 0)
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.UnitCost)), "Field is required.");
                }
            }

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
            await ValidateFieldsAsync(model);
            await _orderSharedService.ValidateStatusAsync((Guid)model.OrderId);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            if (string.IsNullOrWhiteSpace(model.ItemNo))
            {
                if (model.IsSetLot)
                {
                    model.ItemNo = await NextSetItemNoAsync(model.OrderId);
                }
                else
                {
                    model.ItemNo = await NextItemNoAsync(model.OrderId);
                }
            }

            if (await _db.OrderItems.AnyAsync(a => a.OrderId == model.OrderId && a.ItemNo == model.ItemNo))
            {
                _imex = new InvalidModelException();
                _imex.UpsertDataList(_getDisplayName(nameof(model.ItemNo)), $"Already Exits.");
                _imex.ThrowIfContainsErrors();
            }

            OrderItem entity = new OrderItem();

            SetItemEntity(entity, model, Mode.ADD);

            if (model.IsSetLot)
            {
                var unitGroupId = Guid.NewGuid();
                var unitGroup = new OrderItemUnitGroup()
                {
                    Id = unitGroupId,
                    OrderId = model.OrderId,
                    SetLotNo = model.ItemNo,
                    Qty = (int?)model.Qty,
                    Unit = model.Unit,
                    UnitCost = model.UnitCost,
                    TotalCost = model.Amount,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };
                var unitGroupDescriptionId = Guid.NewGuid();
                var unitGroupDescription = new OrderItemUnitGroupDescription()
                {
                    Id = unitGroupDescriptionId,
                    OrderItemUnitGroupId = unitGroupId,
                    Description = model.Description,
                    OtherParticulars = model.OtherDesc,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };

            //var setItemContents = requestItems.Where(w => w.ItemNoIndex.Substring(0, 3) == setItem.ItemNoIndex.Substring(0, 3)
            //    && !(w.Unit == "set" || w.Unit == "lot" || w.Unit == "" || w.Unit == null)).ToList();
            //foreach (var setItemContent in setItemContents)
            //{
            //    var orderItemId = entity.OrderItems.First(f => f.RequestItemId == setItemContent.Id).Id;
            //    var unitGroupDescriptionItem = new OrderItemUnitGroupDescriptionItem()
            //    {
            //        Id = Guid.NewGuid(),
            //        OrderItemUnitGroupDescriptionId = unitGroupDescriptionId,
            //        OrderItemId = orderItemId,
            //        InsertedBy = user,
            //        InsertedDt = date,
            //        UpdatedBy = user,
            //        UpdatedDt = date
            //    };
            //    unitGroupDescription.OrderItemUnitGroupDescriptionItems.Add(unitGroupDescriptionItem);
            //}
            //unitGroup.OrderItemUnitGroupDescriptions.Add(unitGroupDescription);

            var order = await _db.Orders.FindAsync(model.OrderId);
                order.OrderItems.Add(entity);
                order.OrderItemUnitGroups.Add(unitGroup);
            }
            else
            {
                if (model.ItemNo.Contains("."))
                {
                    var setGroup = model.ItemNoIndex.Substring(0, 3);
                /*
                 * Get Parent Set/Lot in OrderIteem
                 * Locate in UnitGroup
                 * Add to Unit Group Item
                 */
                    var parentOrderItem = await _db.OrderItems.FirstOrDefaultAsync(f => f.OrderId == model.OrderId && f.ItemNoIndex == setGroup);
                    if (parentOrderItem == null)
                    {
                        throw new RecordRelationshipException("The main Item No. for this record does not exists.");
                    }
                    var itemNo = parentOrderItem.ItemNo;
                    var description = parentOrderItem.Description;
                    var unitGroupDescription = await _db.OrderItemUnitGroupDescriptions
                        .Include(i => i.OrderItemUnitGroupDescriptionItems)
                        .Where(w => w.OrderItemUnitGroup.OrderId == model.OrderId
                        && w.OrderItemUnitGroup.SetLotNo == itemNo
                        && w.Description == description
                        && !w.OrderItemUnitGroupDescriptionItems.Any(a => a.OrderItemId == entity.Id)).FirstOrDefaultAsync();
                    if (unitGroupDescription != null)
                    {
                        var unitGroupDescriptionItem = new OrderItemUnitGroupDescriptionItem()
                        {
                            Id = Guid.NewGuid(),
                            OrderItemUnitGroupDescriptionId = unitGroupDescription.Id,
                            OrderItemId = entity.Id,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };
                        unitGroupDescription.OrderItemUnitGroupDescriptionItems.Add(unitGroupDescriptionItem);
                    }
                }
                _db.OrderItems.Add(entity);
            }

            await _db.SaveChangesAsync();
            await _orderItemUnitGroupService.DistributeSetAmountAsync(model.OrderId, model.ItemNo);

            return model;
        });

        public ValueTask<OrderItemVM> UpdateAsync(OrderItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            await ValidateFieldsAsync(model);
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

            if (string.IsNullOrWhiteSpace(model.ItemNo))
            {
                model.ItemNo = await NextItemNoAsync(model.OrderId);
            }

            if (await _db.OrderItems.AnyAsync(a => a.OrderId == model.OrderId && a.ItemNo == model.ItemNo && a.Id != model.Id))
            {
                _imex = new InvalidModelException();
                _imex.UpsertDataList(_getDisplayName(nameof(model.ItemNo)), $"Already Exits.");
                _imex.ThrowIfContainsErrors();
            }

            var entity = await _db.OrderItems.Include(i => i.AllField).FirstOrDefaultAsync(f => f.Id == model.Id);

            ValidateRecord(entity, model.Id);

            SetItemEntity(entity, model, Mode.EDIT);

            await _db.SaveChangesAsync();

            if (model.IsSetLot)
            {
            /* Validate unit group record
             * Locate in UnitGroup
             * Update info
             * Update Description
             * Distribute Amount
             */
                if (await _db.OrderItemUnitGroupDescriptions.AsNoTracking()
                   .AnyAsync(f => f.OrderItemUnitGroup.OrderId == model.OrderId
                       && f.OrderItemUnitGroup.SetLotNo == model.ItemNo
                       && f.Description == model.Description && f.Description != model.OriginalDescription))
                {
                    throw new InvalidValueException("Description", "Already exists.");
                }

                var unitGroupDescription = await _db.OrderItemUnitGroupDescriptions.Include(i => i.OrderItemUnitGroup)
                    .FirstOrDefaultAsync(f => f.OrderItemUnitGroup.OrderId == model.OrderId
                        && f.OrderItemUnitGroup.SetLotNo == model.ItemNo
                        && f.Description == model.OriginalDescription);
                if (unitGroupDescription == null)
                {
                    throw new RecordRelationshipException("The Set/Lot group for this record does not exists. Please verify.");
                }

                unitGroupDescription.OrderItemUnitGroup.SetLotNo = model.ItemNo;
                unitGroupDescription.OrderItemUnitGroup.Qty = (int?)model.Qty;
                unitGroupDescription.OrderItemUnitGroup.Unit = model.Unit;
                unitGroupDescription.OrderItemUnitGroup.UnitCost = model.UnitCost;
                unitGroupDescription.OrderItemUnitGroup.TotalCost = model.Qty * model.UnitCost;
                unitGroupDescription.OrderItemUnitGroup.UpdatedBy = user;
                unitGroupDescription.OrderItemUnitGroup.UpdatedDt = date;
                unitGroupDescription.Description = model.Description;
                unitGroupDescription.OtherParticulars = model.OtherDesc;
                unitGroupDescription.UpdatedBy = user;
                unitGroupDescription.UpdatedDt = date;

                await _db.SaveChangesAsync();
            }

            await _orderItemUnitGroupService.DistributeSetAmountAsync(model.OrderId, model.ItemNo);

            return model;
        });

        public ValueTask<OrderItemVM> DeleteAsync(OrderItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.OrderItems.FirstOrDefaultAsync(f => f.Id == model.Id);
            ValidateRecord(entity, model.Id);
            await _orderSharedService.ValidateStatusAsync((Guid)entity.OrderId);

            if (model.IsSetLot)
            {
            /* Locate in UnitGroupDescription
             * Delete if without UnitGroupDescriptionItem
             * Delete (Main)UnitGroup if no more UnitGroupDescription 
             */

                var unitGroup = await _db.OrderItemUnitGroups.Include(i => i.OrderItemUnitGroupDescriptions)
                    .FirstOrDefaultAsync(f => f.OrderId == model.OrderId && f.SetLotNo == model.ItemNo);
                var unitGroupDescriptionCount = unitGroup.OrderItemUnitGroupDescriptions.Count();
                var unitGroupDescription = unitGroup.OrderItemUnitGroupDescriptions.First(f => f.Description == model.Description);
                if (await _db.OrderItemUnitGroupDescriptionItems.AnyAsync(a => a.OrderItemUnitGroupDescriptionId == unitGroupDescription.Id))
                {
                    throw new RecordRelationshipException("Delete Set/Lot items before proceeding.");
                }

                unitGroupDescription.UpdatedBy = user;
                unitGroupDescription.UpdatedDt = date;
                await _db.SaveChangesAsync();
                _db.OrderItemUnitGroupDescriptions.Remove(unitGroupDescription);
                await _db.SaveChangesAsync();

                if (unitGroupDescriptionCount == 1) // the only 1 Description was deleted above, then delete also the Main Set/Lot 
                {
                    unitGroup.UpdatedBy = user;
                    unitGroup.UpdatedDt = date;
                    await _db.SaveChangesAsync();
                    _db.OrderItemUnitGroups.Remove(unitGroup);
                    await _db.SaveChangesAsync();
                }
            }
            else
            {
                var unitGroupDescriptionItem = await _db.OrderItemUnitGroupDescriptionItems.Where(w => w.OrderItemId == model.Id).FirstOrDefaultAsync();
                if (unitGroupDescriptionItem != null)
                {
                    unitGroupDescriptionItem.UpdatedBy = model.UpdatedBy;
                    unitGroupDescriptionItem.UpdatedDt = model.UpdatedDt;
                    await _db.SaveChangesAsync();

                    _db.OrderItemUnitGroupDescriptionItems.Remove(unitGroupDescriptionItem);
                    await _db.SaveChangesAsync();
                }
            }

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();

            _db.OrderItems.Remove(entity);
            await _db.SaveChangesAsync();
            await _orderItemUnitGroupService.DistributeSetAmountAsync(model.OrderId, model.ItemNo);

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

            if (!model.IsSetLot)
            {
                model.PsNoDisplay = _allFieldService.GetOrderPsNoDisplay(model);
            }

            model.ItemNoIndex = Utility.GetItemNoIndex(model.ItemNo);

            entity.Id = model.Id;
            entity.ItemNo = model.ItemNo;
            entity.ItemNoIndex = model.ItemNoIndex;
            entity.OrderId = model.OrderId;
            //entity.RequestItemId = model.RequestItemId;
            entity.ItemCodeId = model.ItemCodeId;
            entity.PsNo = model.PsNo;
            entity.PsNoDisplay = model.PsNoDisplay;
            entity.ItemName = model.ItemName;
            entity.Unit = model.Unit;
            entity.Description = model.Description?.Trim() ?? "";
            entity.OtherDesc = model.OtherDesc?.Trim() ?? "";
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

        private async Task<string> NextItemNoAsync(Guid? orderId)
        {
            var itemNos = await _db.OrderItems
                .Where(w => w.OrderId == orderId)
                .Select(i => i.ItemNo)
                .ToListAsync();

            if (!itemNos.Any())
                return "1";

            var maxItemNo = itemNos
                .OrderByDescending(x => x.Split('.')
                    .Select(n => int.Parse(n))
                    .ToArray(), new ItemNoSequenceUtil())
                .First();

            var parts = maxItemNo.Split('.');
            int lastIndex = parts.Length - 1;

            int lastNumber;
            if (int.TryParse(parts[lastIndex], out lastNumber))
            {
                parts[lastIndex] = (lastNumber + 1).ToString();
            }

            return string.Join(".", parts);
        }

        private async Task<string> NextSetItemNoAsync(Guid? orderId)
        {
            var itemNos = await _db.OrderItems
                .Where(w => w.OrderId == orderId)
                .Select(i => i.ItemNo)
                .ToListAsync();

            if (!itemNos.Any())
                return "1";

            // Flatten all segments into integers
            var allNumbers = itemNos
                .SelectMany(x => x.Split('.'))
                .Select(n => int.TryParse(n, out var v) ? v : 0)
                .ToList();

            int nextNumber = allNumbers.Any() ? allNumbers.Max() + 1 : 1;

            return nextNumber.ToString();
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

        /*
         * Validates a given pricecap for the given parameters, returns:
         * 0 - InValid
         * priceCap - Valid
         */
        public ValidPriceCapVM ValidatePriceCap(DateTime date, Guid? itemCodeId, decimal? unitCost)
        {
            ValidPriceCapVM validPriceCap = null;
            if (unitCost.HasValue && unitCost > 0)
            {
                var priceCap = _priceCapService.GetPriceCap(date);
                if (_itemCodeService.IsProperty(itemCodeId))
                {
                    if (unitCost < priceCap)
                    {
                        validPriceCap = new ValidPriceCapVM()
                        {
                            Category = "Property",
                            PriceCap = priceCap
                        };
                    }
                }
                else
                {
                    if (unitCost >= priceCap)
                    {
                        validPriceCap = new ValidPriceCapVM()
                        {
                            Category = "Supplies",
                            PriceCap = priceCap
                        };
                    }
                }
            }
            return validPriceCap;
        }
    }
}