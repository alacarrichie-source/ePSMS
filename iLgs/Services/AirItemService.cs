using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using iLgs.Services.Validators;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.CategoryEnum;

namespace iLgs.Services
{
    public interface IAirItemService
    {
        string GetItemExtnName(Guid? id);
        IQueryable<AIRItemVM> GetByAirId(Guid? airId);
        ValueTask<ServiceResult<AIRItemVM>> GetByIdAsync(Guid? id);

        ValueTask<ServiceResult<AIRItemVM>> CreateAsync(AIRItemVM model, string user, DateTime date);
        ValueTask<ServiceResult<AIRItemVM>> UpdateAsync(AIRItemVM model, string user, DateTime date);
        ValueTask<ServiceResult<AIRItemVM>> DeleteAsync(AIRItemVM model, string user, DateTime date);

        void ValidAirItems(Guid? airId);
        IAirItemExtnService AirItemExtn { get; }
    }

    public class AirItemService : IAirItemService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly IExceptionService<ServiceResult<AIRItemVM>> _vmExceptionService = new ExceptionService<ServiceResult<AIRItemVM>>();
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
                if (c == CatLands())
                {
                    itemExtnName = "ItemExtnLand";
                }
                else if (c == CatTransportations())
                {
                    itemExtnName = "ItemExtnVehicle";
                }
                else if (c == CatMachineries()
                    || c == CatFurnitures()
                    || c == CatOtherProperties()
                    || c == CatMedicals()
                    || c == CatAgriculturals()
                    || c == CatAnimalSupplies()
                    || c == CatConstructionMaterials()
                    || c == CatOfficeSupplies()
                    || c == CatAccountableForms()
                    || c == CatNonAccountableForns()
                    || c == CatMilitaries()
                    || c == CatOtherSupplies()
                    || c == CatDrugs()
                    || c == CatRepairs()
                    )
                {
                    itemExtnName = "ItemExtnOther";
                }
            }
            return itemExtnName;
        }        

        public IQueryable<AIRItemVM> GetByAirId(Guid? airId)
        {
            var data = _db.AIRItems.Where(w => w.AirId == airId)
                .Select(s => new AIRItemVM
                {
                    Id = s.Id,
                    AirId = s.AirId,
                    OrderItemId = s.OrderItemId,
                    PsType = s.OrderItem.RequestItem.RisItem.ItemCode.ItemType.Code,
                    PsNo = s.OrderItem.StockNo,
                    PsItem = s.OrderItem.RequestItem.RisItem.ItemName,
                    OrderDescription = s.OrderItem.Description,
                    PsUnit = s.OrderItem.RequestItem.RisItem.Unit,
                    Qty = s.Qty,
                    Remarks = s.Remarks,
                    AreaSoldDonated = s.AreaSoldDonated,
                    ConstructionYear = s.ConstructionYear,
                    InvDist = s.InvDist,
                    InsertedDt = s.InsertedDt
                });
            return data;
        }

        public ValueTask<ServiceResult<AIRItemVM>> GetByIdAsync(Guid? id) => _vmExceptionService.TryCatchAsync(async () =>
        {
            var data = await _db.AIRItems.Where(w => w.Id == id)
                .Select(s => new AIRItemVM
                {
                    Id = s.Id,
                    AirId = s.AirId,
                    OrderItemId = s.OrderItemId,
                    PsType = s.OrderItem.RequestItem.RisItem.ItemCode.ItemType.Code,
                    PsNo = s.OrderItem.StockNo,
                    PsItem = s.OrderItem.RequestItem.RisItem.ItemName,
                    OrderDescription = s.OrderItem.Description,
                    PsUnit = s.OrderItem.RequestItem.RisItem.Unit,
                    Qty = s.Qty,
                    Remarks = s.Remarks,
                    AreaSoldDonated = s.AreaSoldDonated,
                    ConstructionYear = s.ConstructionYear,
                    InvDist = s.InvDist,
                    InsertedDt = s.InsertedDt
                }).FirstOrDefaultAsync();
            return ServiceResult<AIRItemVM>.Success(data);
        });

        public ValueTask<ServiceResult<AIRItemVM>> CreateAsync(AIRItemVM model, string user, DateTime date) => _vmExceptionService.TryCatchAsync(async () =>
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

            return ServiceResult<AIRItemVM>.Success(model);
        });

        public ValueTask<ServiceResult<AIRItemVM>> DeleteAsync(AIRItemVM model, string user, DateTime date) => _vmExceptionService.TryCatchAsync(async () =>
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

            return ServiceResult<AIRItemVM>.Success(model);
        });

        private void ValidateFields(AIRItemVM model)
        {
            if (Enum.TryParse(model.PsType, out Category c))
            {
                if (c == CatFoodSupplies() ||
                    c == CatConstructionMaterials() ||
                    c == CatDrugs() ||
                    c == CatMedicals() ||
                    c == CatAgriculturals() ||
                    c == CatOtherSupplies() ||
                    c == CatTransportations())
                {
                    if (string.IsNullOrWhiteSpace(model.InvDist))
                    {
                        throw new InvalidValueException("Inventory/For Distribution Field is Required!");
                    }
                }
            }
        }
        public void ValidateItemExtn(Guid? airItemId)
        {
            var itemExtnName = GetItemExtnName(airItemId);
            if (itemExtnName == "ItemExtnVehicle")
            {                                
                if (_db.AIRItems.Any(a => a.Id == airItemId && a.InvDist == "I" && a.AIRItemExtns.OfType<AIRItemExtnVehicle>().Count() < a.Qty))
                {
                    throw new InvalidValueException("Incomplete item quantity contents detected.");
                }

                //var airItemExtnVehicles = _db.AIRItemExtns.OfType<AIRItemExtnVehicle>().Where(w => w.AIRItem.Id == airItemId).ToList();
                //foreach(var item in airItemExtnVehicles)
                //{
                //    validationService.ValidateEntity(item);
                //}
            }
            else if (itemExtnName == "ItemExtnOther")
            {
                if (_db.AIRItems.Any(a => a.Id == airItemId && a.InvDist == "I" && a.AIRItemExtns.OfType<AIRItemExtnOther>().Count() < a.Qty))
                {
                    throw new InvalidValueException("Incomplete item quantity contents detected.");
                }

                //var airItemExtnOthers = _db.AIRItemExtns.OfType<AIRItemExtnVehicle>().Where(w => w.AIRItem.Id == airItemId).ToList();
                //foreach (var item in airItemExtnOthers)
                //{
                //    validationService.ValidateEntity(item);
                //}
            }
        }

        public ValueTask<ServiceResult<AIRItemVM>> UpdateAsync(AIRItemVM model, string user, DateTime date) => _vmExceptionService.TryCatchAsync(async () =>
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

            return ServiceResult<AIRItemVM>.Success(model);
        });

        public void ValidAirItems(Guid? airId)
        {
            var airItems = GetByAirId(airId);
            foreach (var airItem in airItems)
            {
                ValidateFields(airItem);
                ValidateItemExtn(airItem.Id);
            }
        }

        private async ValueTask<bool> IsPostedAsync(Guid? airId)
        {
            var entity = await _db.AIRs.FindAsync(airId);
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }
    }
}