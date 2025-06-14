using iLgs.Exceptions;
using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.PropertyCard
{
    public interface IPsCardItemExtnBlgService
    {
        IQueryable<PsCardItemExtnBldgVM> GetByPsCardItemId(Guid? psCardItemId);
        IQueryable<PsCardItemExtnBldgVM> GetByPsCardItemIdWithTransferId(Guid? psCardItemId, Guid? trasferId);
        ValueTask<PsCardItemExtnBldgVM> GetByIdAsync(Guid? id);

        ValueTask<PsCardItemExtnBldgVM> CreateAsync(PsCardItemExtnBldgVM model, string user, DateTime date);
        ValueTask<PsCardItemExtnBldgVM> UpdateAsync(PsCardItemExtnBldgVM model, string user, DateTime date);
        ValueTask<PsCardItemExtnBldgVM> DeleteAsync(PsCardItemExtnBldgVM model, string user, DateTime date);
    }

    public class PsCardItemExtnBlgService : IPsCardItemExtnBlgService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<PsCardItemExtnBldgVM> _exceptionService = new ExceptionService<PsCardItemExtnBldgVM>();
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;
        private readonly IPsCardItemExtnBldgValidator _psCardItemExtnBlgValidator;
        private readonly IPsCardItemExtnService _psCardItemExtnService;

        public PsCardItemExtnBlgService(AppManEntities db, IPsCardItemExtnService psCardItemExtnService)
        {
            _db = db;
            _psCardItemTransactionService = new PsCardItemTransactionService(_db);
            _psCardItemExtnBlgValidator = new PsCardItemExtnBldgValidator(_db);
            _psCardItemExtnService = psCardItemExtnService;
        }

        private Expression<Func<PsCardItemExtnBuilding, PsCardItemExtnBldgVM>> GetProjection()
        {
            return s => new PsCardItemExtnBldgVM
            {
                Location = s.Codextn.Description,
                Id = s.Id,
                PsCardItemExtnId = s.Id,
                PsCardItemId = s.PsCardItemId,
                AIRItemExtnId = s.AIRItemExtnId,
                SetLotNo = s.SetLotNo,
                SetLotQtyNo = s.SetLotQtyNo,
                ContentNo = s.ContentNo,
                CustItemNo = s.CustItemNo,
                IsAutoGen = s.IsAutoGen,
                LocationId = s.LocationId,
                PropNo = s.PropNo,
                PropYear = s.PropYear,
                PropSeq = s.PropSeq,
                SeriesNo = s.SeriesNo,
                Remarks = s.Remarks,
                Annex = s.Annex,
                OldAmount = s.OldAmount,
                OldPropNo = s.OldPropNo,
                UpcomingOfficer = s.UpcomingOfficer,
                SubLocation = s.SubLocation,
                Condition = s.Condition,
                AddCost = s.AddCost,
                AcqCost = s.AcqCost,
                AcqDate = s.AcqDate,
                InsertedBy = s.InsertedBy,
                InsertedDt = s.InsertedDt,
                // Extn
                Address = s.Address,
                BuildingItem = s.BuildingItem,
                ProjectName = s.ProjectName,
                BuildingType = s.BuildingType,
                Area = s.Area,
                AppraisedValue = s.AppraisedValue,
                TotalAmount = s.TotalAmount,
                PhaseNo = s.PhaseNo,
                PhaseAmountCo = s.PhaseAmountCo,
                PhaseAmountMooe = s.PhaseAmountMooe,
                StartDate = s.StartDate,
                TargetDate = s.TargetDate,
                PercentComplete = s.PercentComplete,
                CompletionDate = s.CompletionDate,
                Status = s.Status,                
                Latitude = s.Latitude,
                Longitude = s.Longitude
            };
        }

        public IQueryable<PsCardItemExtnBldgVM> GetByPsCardItemId(Guid? psCardItemId)
        {
            var data = _db.PsCardItemExtns.OfType<PsCardItemExtnBuilding>().AsNoTracking()
                .Where(w => w.PsCardItemId == psCardItemId)
                .Select(GetProjection());
            return data;
        }

        public IQueryable<PsCardItemExtnBldgVM> GetByPsCardItemIdWithTransferId(Guid? psCardItemId, Guid? transferId)
        {
            var data = _db.PsCardItemExtns.OfType<PsCardItemExtnBuilding>().AsNoTracking()
                .Where(w => w.PsCardItemId == psCardItemId && w.PsCardItemTransferItems.Any(a => a.PsCardItemTransferId == transferId))
                .Select(GetProjection());
            return data;
        }

        public ValueTask<PsCardItemExtnBldgVM> GetByIdAsync(Guid? id) => _exceptionService.TryCatch(async () =>
        {
            var data = await _db.PsCardItemExtns.OfType<PsCardItemExtnBuilding>().AsNoTracking()
                .Where(w => w.Id == id)
                .Select(GetProjection())
                .FirstOrDefaultAsync();
            return data;
        });        

        public ValueTask<PsCardItemExtnBldgVM> CreateAsync(PsCardItemExtnBldgVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnBlgValidator.ValidateOnCreate(model);

            //var psCardItem = await _db.PsCardItems.FirstOrDefaultAsync(f => f.Id == model.PsCardItemId);
            //var itemQty = (int)(psCardItem.Qty ?? 0); // + (int)(psCardItem.TransferIn ?? 0);
            //var itemExtns = _db.PsCardItemExtns.OfType<PsCardItemExtnBuilding>().Where(w => w.PsCardItemId == model.PsCardItemId);
            //var itemExtnCount = itemExtns.Count();

            //if (itemExtnCount >= itemQty)
            //{
            //    throw new InvalidValueException($"Cannot create more than {itemQty} record(s).");
            //}
            
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new PsCardItemExtnBuilding();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.PsCardItemExtns.Add(entity);
            await _db.SaveChangesAsync();

            await _psCardItemTransactionService.LogUpdates(model.Id, model.PsCardItemId, "CARD", user, date);            
            return model;
        });

        public ValueTask<PsCardItemExtnBldgVM> UpdateAsync(PsCardItemExtnBldgVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnBlgValidator.ValidateOnUpdate(model);
            
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItemExtns.OfType<PsCardItemExtnBuilding>().FirstOrDefaultAsync(f => f.Id == model.Id);
            MapModelToEntityFields(entity, model, Mode.EDIT);

            _db.PsCardItemExtns.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            await _psCardItemTransactionService.LogUpdates(model.Id, model.PsCardItemId, "CARD", user, date);
            return model;
        });

        public void MapModelToEntityFields(PsCardItemExtnBuilding entity, PsCardItemExtnBldgVM model, Mode mode)
        {
            _psCardItemExtnService.MapModelToEntityFields(entity, model, mode);
            
            entity.Address = model.Address;
            entity.BuildingItem = model.BuildingItem;
            entity.ProjectName = model.ProjectName;
            entity.BuildingType = model.BuildingType;
            entity.Area = model.Area;
            entity.AppraisedValue = model.AppraisedValue;
            entity.TotalAmount = model.TotalAmount;
            entity.PhaseNo = model.PhaseNo;
            entity.PhaseAmountCo = model.PhaseAmountCo;
            entity.PhaseAmountMooe = model.PhaseAmountMooe;
            entity.StartDate = model.StartDate;
            entity.TargetDate = model.TargetDate;
            entity.PercentComplete = model.PercentComplete;
            entity.CompletionDate = model.CompletionDate;
            entity.Status = model.Status;
            entity.Latitude = model.Latitude;
            entity.Longitude = model.Longitude;
        }

        public ValueTask<PsCardItemExtnBldgVM> DeleteAsync(PsCardItemExtnBldgVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnBlgValidator.ValidateOnDelete(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = _db.PsCardItemExtns.OfType<PsCardItemExtnBuilding>().FirstOrDefault(f => f.Id == model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItemExtns.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCardItemExtns.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });        
    }
}