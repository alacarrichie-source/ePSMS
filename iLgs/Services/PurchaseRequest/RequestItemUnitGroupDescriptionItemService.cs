using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Interfaces;
using iLgs.Services.Requisition;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.PurchaseRequest
{
    public interface IRequestItemUnitGroupDescriptionItemService
    {
        IQueryable<RequestItemUnitGroupDescriptionItemVM> GetByUnitGroupDescriptionId(Guid? unitGroupDescriptionId);
        ValueTask<RequestItemUnitGroupDescriptionItem> GetByIdAsync(Guid? id);
        //IQueryable<RequestItemUnitGroupDescriptionItemVM> GetAvailableUnitGroupItem(Guid? risId);
        //ValueTask<RequestItemUnitGroupDescriptionItemVM> CreateAsync(RequestItemUnitGroupDescriptionItemVM model, string user, DateTime date);
        void UpdateRequestItem(Guid? requestItemId, decimal? priceRate, decimal? unitCost, string user, DateTime date);
        ValueTask<RequestItemUnitGroupDescriptionItemVM> UpdateAsync(RequestItemUnitGroupDescriptionItemVM model, string user, DateTime date);
        ValueTask<RequestItemUnitGroupDescriptionItemVM> DeleteAsync(RequestItemUnitGroupDescriptionItemVM model, string user, DateTime date);
    }

    public class RequestItemUnitGroupDescriptionItemService : BaseValidator, IRequestItemUnitGroupDescriptionItemService
    {
        private readonly AppManEntities _db ;
        private readonly ICreateAndLogExceptions _exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RequestItemUnitGroupDescriptionItemVM> _vmExceptionService = new ExceptionService<RequestItemUnitGroupDescriptionItemVM>();
        //private readonly IExceptionService<RisItemUnitGroupAvailableVM> _vmUnitGroupAvailableExceptionService = new ExceptionService<RisItemUnitGroupAvailableVM>();
        private readonly IExceptionService<RequestItemUnitGroupDescriptionItem> _exceptionService = new ExceptionService<RequestItemUnitGroupDescriptionItem>();
        private readonly IRisService _risService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        public RequestItemUnitGroupDescriptionItemService(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<RequestItemUnitGroupDescriptionItemVM>(propertyName);
            _risService = new RisService(_db);
        }

        public ValueTask<RequestItemUnitGroupDescriptionItem> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.RequestItemUnitGroupDescriptionItems.FindAsync(id);
            return data;
        });

        public IQueryable<RequestItemUnitGroupDescriptionItemVM> GetByUnitGroupDescriptionId(Guid? unitGroupDescriptionId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RequestItemUnitGroupDescriptionItems.Where(w => w.RequestItemUnitGroupDescriptionId == unitGroupDescriptionId)
                .Select(s => new RequestItemUnitGroupDescriptionItemVM
                {
                    Id = s.Id,
                    RequestItemUnitGroupDescriptionId = s.RequestItemUnitGroupDescriptionId,
                    RisItemUnitGroupDescriptionItemId = s.RisItemUnitGroupDescriptionItemId,
                    RequestItemId = s.RequestItemId,
                    PsNo = s.RisItemUnitGroupDescriptionItem.RisItem.PsNoDisplay,
                    ItemName = s.RisItemUnitGroupDescriptionItem.RisItem.ItemName,
                    Description = s.RisItemUnitGroupDescriptionItem.RisItem.Description,
                    Unit = s.RisItemUnitGroupDescriptionItem.RisItem.Unit,
                    QtyRequest = s.RisItemUnitGroupDescriptionItem.RisItem.QtyRequest,
                    PriceRate = s.RequestItem.PriceRate,
                    UnitCost = s.RequestItem.UnitCost,
                    TotalCost = s.RequestItem.TotalCost,
                    GroupUnitCost = s.RequestItemUnitGroupDescription.RequestItemUnitGroup.UnitCost,
                    GroupTotalCost = s.RequestItemUnitGroupDescription.RequestItemUnitGroup.TotalCost,
                    GroupQty = s.RequestItemUnitGroupDescription.RequestItemUnitGroup.RisItemUnitGroup.Qty,
                    InsertedDt = s.InsertedDt
                });
            return data;
        });

        //public IQueryable<RisItemUnitGroupAvailableVM> GetAvailableUnitGroupItem(Guid? risId) =>
        //_vmUnitGroupAvailableExceptionService.TryCatch(() =>
        //{
        //    var data = _db.RisItems.Where(w => w.RisId == risId && !w.RisItemUnitGroupDescriptionItems.Any(a => a.RisItemId == w.Id))
        //    .Select(s => new RisItemUnitGroupAvailableVM
        //    {
        //        Id = s.Id,
        //        PsNo = s.PsNo,
        //        ItemName = s.ItemName,
        //        Description = s.Description,
        //        Unit = s.Unit,
        //        QtyRequest = s.QtyRequest,
        //        InsertedDt = s.InsertedDt
        //    });
        //    return data;
        //});

        //public ValueTask<RequestItemUnitGroupDescriptionItemVM> CreateAsync(RequestItemUnitGroupDescriptionItemVM model, string user, DateTime date) =>
        //_vmExceptionService.TryCatch(async () =>
        //{
        //    if (string.IsNullOrWhiteSpace(model.GridItems))
        //    {
        //        throw new InvalidValueException("No selected items, cannot continue!");
        //    }

        //    model.InsertedBy = user;
        //    model.UpdatedBy = user;
        //    model.InsertedDt = date;
        //    model.UpdatedDt = date;

        //    var selectedItems = model.GridItems.Split(',');
        //    foreach (var item in selectedItems)
        //    {
        //        model.Id = Guid.NewGuid();
        //        RisItemUnitGroupDescriptionItem entity = new RisItemUnitGroupDescriptionItem()
        //        {
        //            Id = model.Id,
        //            UnitGroupDescriptionId = model.UnitGroupDescriptionId,
        //            RisItemId = Guid.Parse(item),
        //            InsertedBy = model.InsertedBy,
        //            InsertedDt = model.InsertedDt,
        //            UpdatedBy = model.UpdatedBy,
        //            UpdatedDt = model.UpdatedDt
        //        };

        //        _db.RisItemUnitGroupDescriptionItems.Add(entity);
        //    }

        //    await _db.SaveChangesAsync();
        //    return model;
        //});

        public ValueTask<RequestItemUnitGroupDescriptionItemVM> DeleteAsync(RequestItemUnitGroupDescriptionItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateOnDelete(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.RequestItemUnitGroupDescriptionItems.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RequestItemUnitGroupDescriptionItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.RequestItemUnitGroupDescriptionItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RequestItemUnitGroupDescriptionItemVM> UpdateAsync(RequestItemUnitGroupDescriptionItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateOnUpdate(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.RequestItemUnitGroupDescriptionItems.Include(i => i.RequestItemUnitGroupDescription.RequestItemUnitGroup).Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            entity.RequestItemUnitGroupDescriptionId = model.RequestItemUnitGroupDescriptionId;
            entity.RisItemUnitGroupDescriptionItemId = model.RisItemUnitGroupDescriptionItemId;
            entity.RequestItemId = model.RequestItemId;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RequestItemUnitGroupDescriptionItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            //var totalCost = entity.RequestItemUnitGroupDescription.RequestItemUnitGroup.UnitCost;
            UpdateRequestItem(model.RequestItemId, model.PriceRate, model.UnitCost, user, date);

            return model;
        });

        //public void UpdateRequestItem(Guid? requestItemId, decimal? groupTotalCost, decimal? priceRate, string user, DateTime date)
        public void UpdateRequestItem(Guid? requestItemId, decimal? priceRate, decimal? unitCost, string user, DateTime date)
        {
            var reqItem = _db.RequestItems.Include(i => i.RequestItemUnitGroupDescriptionItems).Where(w => w.Id == requestItemId).FirstOrDefault();
            var unitGroup = _db.RequestItemUnitGroups.Where(w => w.RequestItemUnitGroupDescriptions.Any(a => a.RequestItemUnitGroupDescriptionItems.Any(a2 => a2.RequestItemId == requestItemId))).FirstOrDefault();
            var setCost = reqItem.RequestItemUnitGroupDescriptionItems.FirstOrDefault().RequestItemUnitGroupDescription.RequestItemUnitGroup.UnitCost;
            var setQty = _db.RisItemUnitGroups.Where(w => w.Id == unitGroup.RisItemUnitGroupId).FirstOrDefault().Qty;                
           
            if (priceRate == 0)
            {
                reqItem.UnitCost = unitCost;
            }
            else
            {
                reqItem.PriceRate = priceRate;
                reqItem.UnitCost = decimal.Round((decimal)(setCost * (priceRate / 100)), 2, MidpointRounding.AwayFromZero) / reqItem.Qty;
            }
            
            reqItem.TotalCost = (reqItem.Qty * reqItem.UnitCost) * setQty;
            reqItem.UpdatedBy = user;
            reqItem.UpdatedDt = date;
            _db.RequestItems.Attach(reqItem);
            _db.Entry(reqItem).State = EntityState.Modified;
            _db.SaveChanges();
        }

        public void ValidateOnUpdate(RequestItemUnitGroupDescriptionItemVM model)
        {
            ValidateModel(model);
            ValidateRecord(model.Id);
            ValidateIfPosted(model);
            ValidateFieldsOnCreateUpdate(model);
        }

        public void ValidateOnDelete(RequestItemUnitGroupDescriptionItemVM model)
        {
            ValidateModel(model);
            ValidateRecord(model.Id);
            ValidateIfPosted(model);
        }

        public void ValidateIfPosted(RequestItemUnitGroupDescriptionItemVM model)
        {
            //var isPosted = _risService.IsPosted(model);
            //if (isPosted)
            //{
            //    throw new RecordAlreadyPostedException();
            //}
        }

        public void ValidateFieldsOnCreateUpdate(RequestItemUnitGroupDescriptionItemVM model)
        {
            var ex = new InvalidModelException();

            if (!model.PriceRate.HasValue)
            {
                ex.UpsertDataList(_getDisplayName(nameof(model.PriceRate)), "Field is required.");
            }

            ex.ThrowIfContainsErrors();
        }


        private void ValidateRecord(Guid id)
        {
            if (!_db.RequestItemUnitGroupDescriptionItems.Any(a => a.Id == id))
            {
                throw new NotFoundException(id);
            }
        }

        private static void ValidateModel(RequestItemUnitGroupDescriptionItemVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }
    }
}