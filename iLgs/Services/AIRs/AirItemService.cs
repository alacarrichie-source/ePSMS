using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using iLgs.Services.Validators;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services.AIRs
{
    public interface IAirItemService
    {
        string GetItemExtnName(Guid? id);
        IQueryable<AIRItemVM> GetByAirId(Guid? airId);
        ValueTask<AIRItemVM> GetByIdAsync(Guid? id);

        ValueTask<AIRItemVM> CreateAsync(AIRItemVM model, string user, DateTime date);
        ValueTask<AIRItemVM> UpdateAsync(AIRItemVM model, string user, DateTime date);
        ValueTask<AIRItemVM> DeleteAsync(AIRItemVM model, string user, DateTime date);

        void ValidAirItems(Guid? airId);
        IAirItemExtnService AirItemExtn { get; }
    }

    public class AirItemService : IAirItemService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<AIRItemVM> _vmExceptionService = new ExceptionService<AIRItemVM>();
        private readonly IValidationService<AIRItemVM> _validationService;

        private IAirItemExtnService _airItemExtnService;

        public AirItemService(AppManEntities db)
        {
            _db = db;
            _validationService = new ValidationService<AIRItemVM>(new AirItemValidator(this));
        }

        public IAirItemExtnService AirItemExtn { get { return _airItemExtnService = _airItemExtnService ?? new AirItemExtnService(_db); } }

        public string GetItemExtnName(Guid? id)
        {
            var category = _db.AIRItems.Where(w => w.Id == id).Select(s => s.OrderItem.RequestItem.RisItem.ItemCode.ItemType.Code).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(category))
            {
                return "";
            }

            string itemExtnName = "";
            if (Enum.TryParse(category, out Category c))
            {
                if (c == CatLandsProp())
                {
                    itemExtnName = "ItemExtnLand";
                }
                else if (c == CatTransportationProp())
                {
                    itemExtnName = "ItemExtnVehicle";
                }
                else if (c == CatMachineriesProp()
                    || c == CatFurnituresProp()
                    || c == CatOtherProperties()
                    || c == CatMedicalSupply()
                    || c == CatAgriculturalSupply()
                    || c == CatAnimalSupplies()
                    || c == CatConstructionMaterialsSupply()
                    || c == CatOfficeSupplies()
                    || c == CatAccountableFormsSupply()
                    || c == CatNonAccountableFornsSupply()
                    || c == CatMilitarySupply()
                    || c == CatOtherSupplies()
                    || c == CatDrugsSupply()
                    || c == CatRepairSupply()
                    )
                {
                    itemExtnName = "ItemExtnOther";
                }
            }
            return itemExtnName;
        }

        private Expression<Func<AIRItem, AIRItemVM>> Projection()
        {
            return s => new AIRItemVM
            {
                Id = s.Id,
                AirId = s.AirId,
                OrderItemId = s.OrderItemId,
                //PsType = s.OrderItem.RequestItem.RisItem.ItemCode.ItemType.Code,
                //PsNo = s.OrderItem.StockNo,
                //PsItem = s.OrderItem.RequestItem.RisItem.ItemName,
                PsType = s.OrderItem.ItemCode.ItemType.Code,
                PsNo = s.OrderItem.PsNo,
                PsItem = s.OrderItem.ItemCode.Description,
                OrderDescription = s.OrderItem.Description,
                PsUnit = s.OrderItem.RequestItem.RisItem.Unit,
                Qty = s.Qty,
                Remarks = s.Remarks,
                AreaSoldDonated = s.AreaSoldDonated,
                ConstructionYear = s.ConstructionYear,
                InvDist = s.InvDist,
                InsertedDt = s.InsertedDt,
                SetLotNo = s.OrderItem.Order.OrderItemUnitGroups.Where(w => w.OrderItemUnitGroupDescriptions.Any(a => a.OrderItemUnitGroupDescriptionItems.Any(b => b.OrderItemId == s.OrderItemId))).FirstOrDefault().SetLotNo ?? ""
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
            if (await IsPostedAsync(model.AirId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            }

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
                OrderItemId = model.OrderItemId,
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

            return model;
        });

        public ValueTask<AIRItemVM> DeleteAsync(AIRItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            if (await IsPostedAsync(model.AirId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot delete!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            AIRItem entity = await _db.AIRItems.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.AIRItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.AIRItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
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
                    if (string.IsNullOrWhiteSpace(model.InvDist))
                    {
                        throw new InvalidValueException("Inventory/For Distribution Field is Required!");
                    }
                }
            }
        }
        public void ValidateItemExtn(Guid? airItemId, Guid? orderItemId)
        {
            var orderItemUnitGroup = _db.OrderItemUnitGroups.Where(w => w.OrderItemUnitGroupDescriptions.Any(a => a.OrderItemUnitGroupDescriptionItems.Any(b => b.OrderItemId == orderItemId))).FirstOrDefault();
            var groupQty = orderItemUnitGroup != null ? orderItemUnitGroup.Qty : 1;
            var itemExtnName = GetItemExtnName(airItemId);
            if (itemExtnName == "ItemExtnVehicle")
            {
                if (_db.AIRItems.Any(a => a.Id == airItemId && a.InvDist == "I" && a.AIRItemExtns.OfType<AIRItemExtnVehicle>().Count() < a.Qty * groupQty))
                {
                    throw new InvalidValueException("Please complete the entry of all serial numbers before posting.");
                }

                //var airItemExtnVehicles = _db.AIRItemExtns.OfType<AIRItemExtnVehicle>().Where(w => w.AIRItem.Id == airItemId).ToList();
                //foreach(var item in airItemExtnVehicles)
                //{
                //    validationService.ValidateEntity(item);
                //}
            }
            else if (itemExtnName == "ItemExtnOther")
            {
                if (_db.AIRItems.Any(a => a.Id == airItemId && a.InvDist == "I" && a.AIRItemExtns.OfType<AIRItemExtnOther>().Count() < a.Qty * groupQty))
                {
                    throw new InvalidValueException("Please complete the entry of all serial numbers before posting.");
                }

                //var airItemExtnOthers = _db.AIRItemExtns.OfType<AIRItemExtnVehicle>().Where(w => w.AIRItem.Id == airItemId).ToList();
                //foreach (var item in airItemExtnOthers)
                //{
                //    validationService.ValidateEntity(item);
                //}
            }
        }

        public ValueTask<AIRItemVM> UpdateAsync(AIRItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            if (await IsPostedAsync(model.AirId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            }

            ValidateFields(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            AIRItem entity = await _db.AIRItems.FindAsync(model.Id);

            entity.AirId = model.AirId;
            entity.OrderItemId = model.OrderItemId;
            entity.Qty = model.Qty;
            entity.Remarks = model.Remarks;
            entity.AreaSoldDonated = model.AreaSoldDonated;
            entity.ConstructionYear = model.ConstructionYear;
            entity.InvDist = model.InvDist;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.AIRItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public void ValidAirItems(Guid? airId)
        {
            var airItems = GetByAirId(airId);
            foreach (var airItem in airItems)
            {
                ValidateFields(airItem);
                ValidateItemExtn(airItem.Id, airItem.OrderItemId);
            }
        }

        private async ValueTask<bool> IsPostedAsync(Guid? airId)
        {
            var entity = await _db.AIRs.FindAsync(airId);
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }
    }
}