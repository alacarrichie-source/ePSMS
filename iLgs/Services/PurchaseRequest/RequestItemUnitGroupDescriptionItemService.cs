//using iLgs.Exceptions;
//using iLgs.Exceptions.Service;
//using iLgs.Models;
//using iLgs.Services.Requisition;
//using iLgs.Services.Validators;
//using iLgs.Utilities;
//using System;
//using System.Data.Entity;
//using System.Linq;
//using System.Threading.Tasks;

//namespace iLgs.Services.PurchaseRequest
//{
//    public interface IRequestItemUnitGroupDescriptionItemService
//    {
//        IQueryable<RequestItemUnitGroupDescriptionItemVM> GetByUnitGroupDescriptionId(Guid? unitGroupDescriptionId);
//        ValueTask<RequestItemUnitGroupDescriptionItem> GetByIdAsync(Guid? id);
//        Task UpdateRequestItemAsync(Guid? requestItemId, decimal? priceRate, decimal? unitCost, string user, DateTime date);
//        ValueTask<RequestItemUnitGroupDescriptionItemVM> UpdateAsync(RequestItemUnitGroupDescriptionItemVM model, string user, DateTime date);
//        ValueTask<RequestItemUnitGroupDescriptionItemVM> DeleteAsync(RequestItemUnitGroupDescriptionItemVM model, string user, DateTime date);
//    }

//    public class RequestItemUnitGroupDescriptionItemService : BaseValidator, IRequestItemUnitGroupDescriptionItemService
//    {
//        private readonly AppManEntities _db;
//        private readonly ICreateAndLogExceptions _exceptions;
//        private readonly IExceptionService<RequestItemUnitGroupDescriptionItemVM> _vmExceptionService;
//        private readonly IExceptionService<RequestItemUnitGroupDescriptionItem> _exceptionService;
//        private readonly IRisService _risService;
//        private readonly GetDisplayNameDelegate _getDisplayName;

//        public RequestItemUnitGroupDescriptionItemService(AppManEntities db,
//            ICreateAndLogExceptions exceptions,
//            IExceptionService<RequestItemUnitGroupDescriptionItemVM> vmExceptionService,
//            IExceptionService<RequestItemUnitGroupDescriptionItem> exceptionService,
//            IRisService risService)
//        {
//            _db = db;
//            _getDisplayName = propertyName => Utility.GetDisplayName<RequestItemUnitGroupDescriptionItemVM>(propertyName);
//            _exceptions = exceptions;
//            _vmExceptionService = vmExceptionService;
//            _exceptionService = exceptionService;
//            _risService = risService;
//        }

//        public ValueTask<RequestItemUnitGroupDescriptionItem> GetByIdAsync(Guid? id) =>
//        _exceptionService.TryCatch(async () =>
//        {
//            var data = await _db.RequestItemUnitGroupDescriptionItems.FindAsync(id);
//            return data;
//        });

//        public IQueryable<RequestItemUnitGroupDescriptionItemVM> GetByUnitGroupDescriptionId(Guid? unitGroupDescriptionId) =>
//        _vmExceptionService.TryCatch(() =>
//        {
//            var data = _db.RequestItemUnitGroupDescriptionItems.AsNoTracking().Where(w => w.RequestItemUnitGroupDescriptionId == unitGroupDescriptionId)
//                .Select(s => new RequestItemUnitGroupDescriptionItemVM
//                {
//                    Id = s.Id,
//                    RequestItemUnitGroupDescriptionId = s.RequestItemUnitGroupDescriptionId,
//                    //RisItemUnitGroupDescriptionItemId = s.RisItemUnitGroupDescriptionItemId,
//                    RequestItemId = s.RequestItemId,
//                    //Category = s.RequestItem.RisItem.ItemCode.ItemType.Category,
//                    //PsNo = s.RisItemUnitGroupDescriptionItem.RisItem.PsNoDisplay,
//                    //ItemName = s.RisItemUnitGroupDescriptionItem.RisItem.ItemName,
//                    Description = s.RequestItem.Description,
//                    Unit = s.RequestItem.Unit,
//                    QtyRequest = (int?)s.RequestItem.Qty,
//                    PriceRate = s.RequestItem.PriceRate,
//                    UnitCost = s.RequestItem.UnitCost,
//                    TotalCost = s.RequestItem.TotalCost,
//                    GroupUnitCost = s.RequestItemUnitGroupDescription.RequestItemUnitGroup.UnitCost,
//                    GroupTotalCost = s.RequestItemUnitGroupDescription.RequestItemUnitGroup.TotalCost,
//                    GroupQty = s.RequestItemUnitGroupDescription.RequestItemUnitGroup.Qty,
//                    InsertedDt = s.InsertedDt
//                });
//            return data;
//        });
        
//        public ValueTask<RequestItemUnitGroupDescriptionItemVM> DeleteAsync(RequestItemUnitGroupDescriptionItemVM model, string user, DateTime date) =>
//        _vmExceptionService.TryCatch(async () =>
//        {
//            ValidateOnDelete(model);

//            model.UpdatedBy = user;
//            model.UpdatedDt = date;

//            var entity = await _db.RequestItemUnitGroupDescriptionItems.FindAsync(model.Id);

//            entity.UpdatedBy = model.UpdatedBy;
//            entity.UpdatedDt = model.UpdatedDt;

//            _db.RequestItemUnitGroupDescriptionItems.Attach(entity);
//            _db.Entry(entity).State = EntityState.Modified;
//            await _db.SaveChangesAsync();

//            _db.RequestItemUnitGroupDescriptionItems.Remove(entity);
//            _db.Entry(entity).State = EntityState.Deleted;
//            await _db.SaveChangesAsync();

//            return model;
//        });

//        public ValueTask<RequestItemUnitGroupDescriptionItemVM> UpdateAsync(RequestItemUnitGroupDescriptionItemVM model, string user, DateTime date) =>
//        _vmExceptionService.TryCatch(async () =>
//        {
//            ValidateOnUpdate(model);

//            model.UpdatedBy = user;
//            model.UpdatedDt = date;

//            var entity = await _db.RequestItemUnitGroupDescriptionItems.Include(i => i.RequestItemUnitGroupDescription.RequestItemUnitGroup).Where(w => w.Id == model.Id).FirstOrDefaultAsync();

//            entity.RequestItemUnitGroupDescriptionId = model.RequestItemUnitGroupDescriptionId;
//            //entity.RisItemUnitGroupDescriptionItemId = model.RisItemUnitGroupDescriptionItemId;
//            entity.RequestItemId = model.RequestItemId;
//            entity.UpdatedBy = model.UpdatedBy;
//            entity.UpdatedDt = model.UpdatedDt;

//            _db.RequestItemUnitGroupDescriptionItems.Attach(entity);
//            _db.Entry(entity).State = EntityState.Modified;
//            await _db.SaveChangesAsync();

//            //var totalCost = entity.RequestItemUnitGroupDescription.RequestItemUnitGroup.UnitCost;
//            await UpdateRequestItemAsync(model.RequestItemId, model.PriceRate ?? 0, model.UnitCost ?? 0, user, date);

//            return model;
//        });

//        //public void UpdateRequestItem(Guid? requestItemId, decimal? groupTotalCost, decimal? priceRate, string user, DateTime date)
//        public async Task UpdateRequestItemAsync(Guid? requestItemId, decimal? priceRate, decimal? unitCost, string user, DateTime date)
//        {
//            var reqItem = await _db.RequestItems.Include(i => i.RequestItemUnitGroupDescriptionItems).Where(w => w.Id == requestItemId).FirstOrDefaultAsync();
//            var unitGroup = await _db.RequestItemUnitGroups.Where(w => w.RequestItemUnitGroupDescriptions.Any(a => a.RequestItemUnitGroupDescriptionItems.Any(a2 => a2.RequestItemId == requestItemId))).FirstOrDefaultAsync();            
//            var setUnitCost = unitGroup.UnitCost;
//            var setTotalCost = unitGroup.TotalCost;
//            var setQty = unitGroup.Qty;

//            if (priceRate == 0)
//            {
//                reqItem.UnitCost = unitCost;
//                reqItem.PriceRate = decimal.Round((decimal)((unitCost * reqItem.Qty * setQty) / setTotalCost) * 100, 2, MidpointRounding.AwayFromZero);
//            }
//            else
//            {
//                reqItem.PriceRate = priceRate;
//                reqItem.UnitCost = decimal.Round((decimal)(setUnitCost * (priceRate / 100)), 2, MidpointRounding.AwayFromZero) / reqItem.Qty;
//            }
            
//            reqItem.TotalCost = (reqItem.Qty * reqItem.UnitCost) * setQty;
//            reqItem.UpdatedBy = user;
//            reqItem.UpdatedDt = date;
//            _db.RequestItems.Attach(reqItem);
//            _db.Entry(reqItem).State = EntityState.Modified;
//            await _db.SaveChangesAsync();
//        }

//        public void ValidateOnUpdate(RequestItemUnitGroupDescriptionItemVM model)
//        {
//            ValidateModel(model);
//            ValidateRecord(model.Id);
//            ValidateIfPosted(model);
//            ValidateFieldsOnCreateUpdate(model);
//        }

//        public void ValidateOnDelete(RequestItemUnitGroupDescriptionItemVM model)
//        {
//            ValidateModel(model);
//            ValidateRecord(model.Id);
//            ValidateIfPosted(model);
//        }

//        public void ValidateIfPosted(RequestItemUnitGroupDescriptionItemVM model)
//        {
//            //var isPosted = _risService.IsPosted(model);
//            //if (isPosted)
//            //{
//            //    throw new RecordAlreadyPostedException();
//            //}
//        }

//        public void ValidateFieldsOnCreateUpdate(RequestItemUnitGroupDescriptionItemVM model)
//        {
//            var ex = new InvalidModelException();

//            if (!model.PriceRate.HasValue)
//            {
//                ex.UpsertDataList(_getDisplayName(nameof(model.PriceRate)), "Field is required.");
//            }

//            ex.ThrowIfContainsErrors();
//        }


//        private void ValidateRecord(Guid id)
//        {
//            if (!_db.RequestItemUnitGroupDescriptionItems.Any(a => a.Id == id))
//            {
//                throw new NotFoundException(id);
//            }
//        }

//        private static void ValidateModel(RequestItemUnitGroupDescriptionItemVM model)
//        {
//            if (model is null)
//            {
//                throw new NullException();
//            }
//        }
//    }
//}