using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using iLgs.Services.Requisition;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.PurchaseRequest
{
    public interface IRequestItemUnitGroupService
    {
        IQueryable<RequestItemUnitGroupVM> GetByPrId(Guid? prId);
        ValueTask<RequestItemUnitGroup> GetByIdAsync(Guid? id);
        ValueTask<RequestItemUnitGroupVM> CreateAsync(RequestItemUnitGroupVM model, string user, DateTime date);
        ValueTask<RequestItemUnitGroupVM> UpdateAsync(RequestItemUnitGroupVM model, string user, DateTime date);
        ValueTask<RequestItemUnitGroupVM> DeleteAsync(RequestItemUnitGroupVM model, string user, DateTime date);
    }

    public class RequestItemUnitGroupService : IRequestItemUnitGroupService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RequestItemUnitGroupVM> _vmExceptionService = new ExceptionService<RequestItemUnitGroupVM>();
        private readonly IExceptionService<RequestItemUnitGroup> _exceptionService = new ExceptionService<RequestItemUnitGroup>();
        private readonly IRisService _risService;
        private readonly IRequestService _requestService;
        private readonly IRequestItemUnitGroupDescriptionItemService _requestItemUnitGroupDescriptionItemService;

        public RequestItemUnitGroupService(AppManEntities db)
        {
            _db = db;
            _risService = new RisService(_db);
            _requestService = new RequestService(_db);
            _requestItemUnitGroupDescriptionItemService = new RequestItemUnitGroupDescriptionItemService(_db);
        }

        public ValueTask<RequestItemUnitGroup> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.RequestItemUnitGroups.FindAsync(id);
            return data;
        });

        public IQueryable<RequestItemUnitGroupVM> GetByPrId(Guid? prId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RequestItemUnitGroups.Include(i => i.RisItemUnitGroup).Where(w => w.PrId == prId)
                .Select(s => new RequestItemUnitGroupVM
                {
                    Id = s.Id,
                    PrId = s.PrId,
                    RisItemUnitGroupId = s.RisItemUnitGroupId,
                    RisItemUnitGroup = s.RisItemUnitGroup,
                    //Unit = s.RisItemUnitGroup.Unit,
                    //Qty = s.RisItemUnitGroup.Qty,
                    UnitCost = s.UnitCost,
                    TotalCost = s.TotalCost,
                    InsertedDt = s.InsertedDt
                });
            return data;
        });

        public ValueTask<RequestItemUnitGroupVM> CreateAsync(RequestItemUnitGroupVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            if (await _requestService.IsPostedAsync((Guid)model.PrId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            }

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;
            model.TotalCost = model.RisItemUnitGroup.Qty * model.UnitCost;

            var entity = new RequestItemUnitGroup()
            {
                Id = model.Id,
                PrId = model.PrId,
                RisItemUnitGroupId = model.RisItemUnitGroupId,
                UnitCost = model.UnitCost,
                TotalCost = model.TotalCost,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.RequestItemUnitGroups.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RequestItemUnitGroupVM> DeleteAsync(RequestItemUnitGroupVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var entity = await _db.RequestItemUnitGroups.Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (await _requestService.IsPostedAsync((Guid)model.PrId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RequestItemUnitGroups.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.RequestItemUnitGroups.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RequestItemUnitGroupVM> UpdateAsync(RequestItemUnitGroupVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var entity = await _db.RequestItemUnitGroups.Include(i => i.RequestItemUnitGroupDescriptions).Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (await _requestService.IsPostedAsync((Guid)model.PrId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.PrId = model.PrId;
            entity.RisItemUnitGroupId = model.RisItemUnitGroupId;
            
            entity.UnitCost = model.UnitCost;
            entity.TotalCost = model.RisItemUnitGroup.Qty * model.UnitCost;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RequestItemUnitGroups.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            var unitGroupDescriptions = entity.RequestItemUnitGroupDescriptions.ToList();
            foreach (var unitGroupDescription in unitGroupDescriptions)
            {
                var unitGroupDescriptionItems = await _db.RequestItemUnitGroupDescriptionItems
                    .Include(i => i.RequestItem)
                    .Where(w => w.RequestItemUnitGroupDescriptionId == unitGroupDescription.Id).ToListAsync();
                foreach (var unitGroupDescriptionItem in unitGroupDescriptionItems)
                {
                    var priceRate = unitGroupDescriptionItem.RequestItem.PriceRate ?? 0;
                    _requestItemUnitGroupDescriptionItemService.UpdateRequestItem(unitGroupDescriptionItem.RequestItemId, priceRate, user, date);
                }
            }

            return model;
        });
    }    
}