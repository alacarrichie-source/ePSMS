using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services
{
    public interface IRequestItemUnitGroupDescriptionItemService
    {
        IQueryable<RequestItemUnitGroupDescriptionItemVM> GetByUnitGroupDescriptionId(Guid? unitGroupDescriptionId);
        ValueTask<RequestItemUnitGroupDescriptionItem> GetByIdAsync(Guid? id);
        //IQueryable<RequestItemUnitGroupDescriptionItemVM> GetAvailableUnitGroupItem(Guid? risId);
        //ValueTask<RequestItemUnitGroupDescriptionItemVM> CreateAsync(RequestItemUnitGroupDescriptionItemVM model, string user, DateTime date);
        void UpdateRequestItem(Guid? requestItemId, decimal? priceRate, string user, DateTime date);
        ValueTask<RequestItemUnitGroupDescriptionItemVM> UpdateAsync(RequestItemUnitGroupDescriptionItemVM model, string user, DateTime date);
        ValueTask<RequestItemUnitGroupDescriptionItemVM> DeleteAsync(RequestItemUnitGroupDescriptionItemVM model, string user, DateTime date);
    }

    public class RequestItemUnitGroupDescriptionItemService : IRequestItemUnitGroupDescriptionItemService
    {
        private readonly AppManEntities _db ;
        private readonly ICreateAndLogExceptions _exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RequestItemUnitGroupDescriptionItemVM> _vmExceptionService = new ExceptionService<RequestItemUnitGroupDescriptionItemVM>();
        //private readonly IExceptionService<RisItemUnitGroupAvailableVM> _vmUnitGroupAvailableExceptionService = new ExceptionService<RisItemUnitGroupAvailableVM>();
        private readonly IExceptionService<RequestItemUnitGroupDescriptionItem> _exceptionService = new ExceptionService<RequestItemUnitGroupDescriptionItem>();

        public RequestItemUnitGroupDescriptionItemService(AppManEntities db)
        {
            _db = db;
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
            UpdateRequestItem(model.RequestItemId, model.PriceRate, user, date);

            return model;
        });

        //public void UpdateRequestItem(Guid? requestItemId, decimal? groupTotalCost, decimal? priceRate, string user, DateTime date)
        public void UpdateRequestItem(Guid? requestItemId, decimal? priceRate, string user, DateTime date)
        {
            var reqItem = _db.RequestItems.Include(i => i.RequestItemUnitGroupDescriptionItems).Where(w => w.Id == requestItemId).FirstOrDefault();
            var totalCost = reqItem.RequestItemUnitGroupDescriptionItems.FirstOrDefault().RequestItemUnitGroupDescription.RequestItemUnitGroup.UnitCost;
            reqItem.PriceRate = priceRate;

            if (priceRate == 0)
            {
                reqItem.UnitCost = 0;
            }
            else
            {
                reqItem.UnitCost = decimal.Round((decimal)(totalCost * (priceRate / 100) * reqItem.Qty), 2, MidpointRounding.AwayFromZero);
            }
            reqItem.TotalCost = reqItem.Qty * reqItem.UnitCost;
            reqItem.UpdatedBy = user;
            reqItem.UpdatedDt = date;
            _db.RequestItems.Attach(reqItem);
            _db.Entry(reqItem).State = EntityState.Modified;
            _db.SaveChanges();
        }
    }
}