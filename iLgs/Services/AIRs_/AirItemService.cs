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

namespace iLgs.Services.AIRs_
{
    public interface IAirItemService : IAirItemAbstractService
    {
        IQueryable<AIRItemVM> GetByAirId(Guid? airId);
        ValueTask<AIRItemVM> GetByIdAsync(Guid? id);

        ValueTask<AIRItemVM> CreateAsync(AIRItemVM model, string user, DateTime date);
        ValueTask<AIRItemVM> UpdateAsync(AIRItemVM model, string user, DateTime date);
        ValueTask<AIRItemVM> DeleteAsync(AIRItemVM model, string user, DateTime date);

        void ValidAirItems(Guid? airId);
        IAirItemExtnService AirItemExtn { get; }
    }

    public class AirItemService : BaseValidator, IAirItemService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<AIRItemVM> _vmExceptionService;
        private readonly IAirItemAbstractService _airItemAbstractService;
        private readonly IAirItemExtnService _airItemExtnService;
        private readonly GetDisplayNameDelegate _getDisplayName = Utility.GetDisplayName<AIR_VM>;

        public IAirItemExtnService AirItemExtn => _airItemExtnService;

        public AirItemService(AppManEntities db)
        {
            _db = db;
            _vmExceptionService = new ExceptionService<AIRItemVM>();
            _airItemAbstractService = new AirItemAbstractService(_db);
            _airItemExtnService = new AirItemExtnService(_db);
        }

        //public AirItemService(AppManEntities db,
        //    IAppManEntitiesFactory appManEntitiesFactory,
        //    IExceptionService<AIRItemVM> vmExceptionService, IAirItemAbstractService airItemAbstractService, IAirItemExtnService airItemExtnService)
        //{
        //    _db = db;
        //    _contextFactory = appManEntitiesFactory;
        //    _vmExceptionService = vmExceptionService;
        //    _airItemAbstractService = airItemAbstractService;
        //    _airItemExtnService = airItemExtnService;            
        //}


        public string GetItemExtnName(Guid? id)
        {
            return _airItemAbstractService.GetItemExtnName(id);
        }

        public string GetItemExtnNameByCategory(string category)
        {
            return _airItemAbstractService.GetItemExtnNameByCategory(category);
        }

        private Expression<Func<AIRItem, AIRItemVM>> Projection()
        {
            return s => new AIRItemVM
            {
                Id = s.Id,
                AirId = s.AirId,
                OrderItemRequestId = s.OrderItemRequestId,
                PsType = s.OrderItemRequest.OrderItem.ItemCode.ItemType.Code,
                PsNo = s.OrderItemRequest.OrderItem.PsNo,
                PsItem = s.OrderItemRequest.OrderItem.ItemCode.Description,
                Description = s.OrderItemRequest.OrderItem.Description,
                OtherDesc = s.OrderItemRequest.OrderItem.OtherDesc,
                PsUnit = s.OrderItemRequest.OrderItem.Unit,
                Qty = s.Qty,
                Remarks = s.Remarks,
                AreaSoldDonated = s.AreaSoldDonated,
                ConstructionYear = s.ConstructionYear,
                InvDist = s.InvDist,
                InsertedDt = s.InsertedDt,
                SetLotNo = s.OrderItemRequest.OrderItem.Order.OrderItemUnitGroups.Where(w => w.OrderItemUnitGroupDescriptions.Any(a => a.OrderItemUnitGroupDescriptionItems.Any(b => b.OrderItemId == s.OrderItemRequest.OrderItemId))).FirstOrDefault().SetLotNo ?? "",
                ItemNo = s.OrderItemRequest.OrderItem.ItemNo,
                ItemNoIndex = s.OrderItemRequest.OrderItem.ItemNoIndex,
                Padding = (s.OrderItemRequest.OrderItem.ItemNo.Length - s.OrderItemRequest.OrderItem.ItemNo.Replace(".", "").Length) * 20,
                OrderItemRequest = s.OrderItemRequest,
                IsSetLot = s.OrderItemRequest.OrderItem.Unit == "set" || s.OrderItemRequest.OrderItem.Unit == "lot" ? true : false,
                IsSetLotItem = s.OrderItemRequest.OrderItem.ItemNo.Contains("."),
                AIR = s.AIR
            };
        }

        public IQueryable<AIRItemVM> GetByAirId(Guid? airId)
        {
            var data = _db.AIRItems.Where(w => w.AirId == airId)
                .Select(Projection());
            return data;
        }

        public ValueTask<AIRItemVM> GetByIdAsync(Guid? id) => _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.AIRItems.Where(w => w.Id == id)
                .Select(Projection()).FirstOrDefaultAsync();
            return data;
        });

        public ValueTask<AIRItemVM> CreateAsync(AIRItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            if (await IsPostedAsync(model.AirId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            }

            await ValidateOnCreateUpdate(model, Mode.ADD);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            model.Id = Guid.NewGuid();

            AIRItem entity = new AIRItem()
            {
                Id = model.Id,
                AirId = model.AirId,
                OrderItemRequestId = model.OrderItemRequestId,
                Qty = model.Qty,
                Remarks = model.Remarks,
                AreaSoldDonated = model.AreaSoldDonated,
                ConstructionYear = model.ConstructionYear,
                InvDist = model.InvDist,
                InsertedBy = user,
                InsertedDt = date,
                UpdatedBy = user,
                UpdatedDt = date
            };

            _db.AIRItems.Add(entity);
            await _db.SaveChangesAsync();
            
            entity = await _db.AIRItems
                .Include(i => i.AIR)
                .Include(i => i.OrderItemRequest.OrderItem)
                .Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            if (entity.OrderItemRequest.OrderItem.Unit == "set" || entity.OrderItemRequest.OrderItem.Unit == "lot")
            {
                // fetch related set/lot items within the same Order Id
                var mainItemNo = $"{entity.OrderItemRequest.OrderItem.ItemNo.Trim()}.";
                var orderItemRequests = _db.OrderItemRequests.Where(w => w.OrderItem.OrderId == entity.AIR.OrderId
                    && w.OrderItem.ItemNo.StartsWith(mainItemNo));
                foreach (var orderItemRequest in orderItemRequests)
                {
                    var memberItem = new AIRItem()
                    {
                        Id = Guid.NewGuid(),
                        AirId = model.AirId,
                        OrderItemRequestId = orderItemRequest.Id,
                        Qty = orderItemRequest.QtyApplied,
                        InvDist = model.InvDist,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };
                    _db.AIRItems.Add(memberItem);
                }
                await _db.SaveChangesAsync();                
            }

            return model;
        });

        public ValueTask<AIRItemVM> UpdateAsync(AIRItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            if (await IsPostedAsync(model.AirId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            }

            ValidateFields(model);
            await ValidateOnCreateUpdate(model, Mode.EDIT);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            AIRItem entity = await _db.AIRItems.FindAsync(model.Id);

            ValidateRecord(entity, model.Id);

            if (entity.InvDist != model.InvDist)
            {
                if (_db.AIRItemExtns.Any(w => w.AIRItemId == model.Id))
                {
                    throw new RecordRelationshipException("Serial Numbers for this item already exist, cannot change Inventory/For Distribution.");
                }
            }

            entity.AirId = model.AirId;
            entity.OrderItemRequestId = model.OrderItemRequestId;
            entity.Qty = model.Qty;
            entity.Remarks = model.Remarks;
            entity.AreaSoldDonated = model.AreaSoldDonated;
            entity.ConstructionYear = model.ConstructionYear;
            entity.InvDist = model.InvDist;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<AIRItemVM> DeleteAsync(AIRItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);

            if (await IsPostedAsync(model.AirId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot delete!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.AIRItems.Include(i => i.OrderItemRequest.OrderItem).Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            ValidateRecord(entity, model.Id);

            if (entity.OrderItemRequest.OrderItem.Unit == "set" || entity.OrderItemRequest.OrderItem.Unit == "lot")
            {
                // delete set items
                var mainItemNo = $"{entity.OrderItemRequest.OrderItem.ItemNo.Trim()}.";
                var airItems = _db.AIRItems.Where(w => w.AirId == model.AirId
                    && w.OrderItemRequest.OrderItem.ItemNo.StartsWith(mainItemNo));
                foreach(var airItem in airItems)
                {
                    airItem.UpdatedBy = user;
                    airItem.UpdatedDt = date;
                }
                await _db.SaveChangesAsync();

                _db.AIRItems.RemoveRange(airItems);
                await _db.SaveChangesAsync();
            }
            
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();

            _db.AIRItems.Remove(entity);
            await _db.SaveChangesAsync();            

            return model;
        });

        private void ValidateFields(AIRItemVM model)
        {
            if (Enum.TryParse(model.PsType, out Category c))
            {
                if (c == CatFoodsSupply() ||
                    c == CatConstructionMaterialsSupply() ||
                    c == CatDrugsSupply() ||
                    c == CatMedicalSupply() ||
                    c == CatAgriculturalSupply() ||
                    c == CatOtherSupplies() ||
                    c == CatTransportationProp())
                {
                    if (!model.IsSetLot && string.IsNullOrWhiteSpace(model.InvDist))
                    {
                        throw new InvalidValueException($"AIR Control Number {model.AIR.CtrlNo}, Inventory/For Distribution Field is Required!");            
                    }
                }
            }            
        }

        public void ValidateItemExtn(Guid? airItemId, Guid? orderItemId)
        {
            var orderItemUnitGroup = _db.OrderItemUnitGroups.Where(w => w.OrderItemUnitGroupDescriptions.Any(a => a.OrderItemUnitGroupDescriptionItems.Any(b => b.OrderItemId == orderItemId))).FirstOrDefault();
            var groupQty = orderItemUnitGroup != null ? orderItemUnitGroup.Qty : 1;
            var itemExtnName = GetItemExtnName(airItemId);
            var ctrlNo = _db.AIRs.FirstOrDefault(f => f.AIRItems.Any(a => a.Id == airItemId)).CtrlNo;
            if (itemExtnName == "ItemExtnVehicle")
            {
                if (_db.AIRItems.Any(a => a.Id == airItemId && a.InvDist == "I" && a.AIRItemExtns.OfType<AIRItemExtnVehicle>().Count() < a.Qty * groupQty))
                {
                    throw new InvalidValueException($"AIR Control No {ctrlNo}, serial numbers are not complete.");
                }

                if (_db.AIRItems.Any(a => a.Id == airItemId && a.InvDist == "I" && a.AIRItemExtns.OfType<AIRItemExtnVehicle>().Any(b => b.ConductionNo == "" || b.ConductionNo == null)))
                {
                    throw new InvalidValueException($"AIR Control No {ctrlNo}, serial numbers are not complete.");
                }
            }
            else if (itemExtnName == "ItemExtnOther")
            {
                if (_db.AIRItems.Any(a => a.Id == airItemId && a.InvDist == "I" && a.AIRItemExtns.OfType<AIRItemExtnOther>().Count() < a.Qty * groupQty))
                {
                    throw new InvalidValueException($"AIR Control No {ctrlNo}, serial numbers are not complete.");
                }

                if (_db.AIRItems.Any(a => a.Id == airItemId && a.InvDist == "I" && a.AIRItemExtns.OfType<AIRItemExtnOther>().Any(b => b.SerialNo == "" || b.SerialNo == null)))
                {
                    throw new InvalidValueException($"AIR Control No {ctrlNo}, serial numbers are not complete.");
                }
            }
        }        

        private async ValueTask ValidateOnCreateUpdate(AIRItemVM model, Mode mode)
        {
            _imex = new InvalidModelException();
            var orderItemRequest = await _db.OrderItemRequests.FindAsync(model.OrderItemRequestId);
            if (orderItemRequest == null)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.OrderItemRequestId)), "Does not Exists.");
            }
            else
            {
                var qty = orderItemRequest.QtyApplied - (_db.AIRItems.Where(w => w.AirId != model.AirId && w.OrderItemRequestId == orderItemRequest.Id).Sum(s => s.Qty) ?? 0);

                if (model.Qty > qty)
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.Qty)), $"Total quantity must not exceed {orderItemRequest.QtyApplied}.");
                }
            }

            if (model.Qty == null || model.Qty == 0)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Qty)), "Value must be greater than zero (0).");
            }

            _imex.ThrowIfContainsErrors();
        }

        private void ValidateIfNull(AIRItemVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(AIRItem entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }


        public void ValidAirItems(Guid? airId)
        {
            var airItems = GetByAirId(airId);
            foreach (var airItem in airItems)
            {
                ValidateFields(airItem);
                ValidateItemExtn(airItem.Id, airItem.OrderItemRequest.OrderItemId);
            }
        }

        private async ValueTask<bool> IsPostedAsync(Guid? airId)
        {
            var entity = await _db.AIRs.FindAsync(airId);
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }
    }
}