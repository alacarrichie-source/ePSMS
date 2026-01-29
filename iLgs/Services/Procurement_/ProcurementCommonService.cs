using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Logs;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.Procurement_
{
    public interface IProcurementCommonService
    {
        void MapModelToEntityFields(Procurement entity, ProcurementVM model, Mode mode);
        Task<bool> IsPostedAsync(Guid? procId);
        Task ValidateStatusAsync(Guid? procId);
        Task ValidateIfPostedAsync(Guid? procId);
        Task ValidateAirAsync(Guid? procId);
        Task UpdateProcurementItemAsync(Guid? procItemId, decimal? priceRate, decimal? unitCost, string user, DateTime date);        
    }

    internal class ProcurementCommonService : IProcurementCommonService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<Procurement> _exceptionService;
        public ProcurementCommonService(AppManEntities db)
        {
            _db = db;
            _exceptionService = new ExceptionService<Procurement>();
        }

        public void MapModelToEntityFields(Procurement entity, ProcurementVM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }
            entity.RefType = model.RefType;
            entity.RefValue = model.RefValue;
            entity.RefDate = model.RefDate;
            entity.Fund = model.Fund;
            entity.DepartmentId = model.DepartmentId;
            entity.Department = model.Department;
            entity.Division = model.Division;
            entity.FPP = model.FPP;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        public Task<bool> IsPostedAsync(Guid? procId)
        {
            return _db.Procurements.AnyAsync(p => p.Id == procId && p.PostedBy != null && p.PostedBy != "");
        }

        public async Task ValidateStatusAsync(Guid? procId)
        {
            await ValidateIfPostedAsync(procId);
            await ValidateAirAsync(procId);
        }

        public async Task ValidateAirAsync(Guid? procId)
        {
            if (await _db.AIRs.AnyAsync(a => a.OrderId == procId))
            {
                throw new RecordRelationshipException("Record already with AIR.");
            }
        }

        public async Task ValidateIfPostedAsync(Guid? procId)
        {
            var procurement = await _db.Procurements.FirstOrDefaultAsync(p => p.Id == procId);
            if (procurement != null && procurement.PostedBy != null && procurement.PostedBy != "")
            {
                throw new RecordAlreadyPostedException($"Record already posted by {procurement.PostedBy} on {procurement.PostedDt}, cannot update!");
            }
        }

        public async Task UpdateProcurementItemAsync(Guid? procItemId, decimal? priceRate, decimal? unitCost, string user, DateTime date)
        {
            var procItem = await _db.ProcurementItems.Include(i => i.ProcurementUnitGroupDescriptionItems).Where(w => w.Id == procItemId).FirstOrDefaultAsync();
            var unitGroup = await _db.ProcurementUnitGroups.Where(w => w.ProcurementUnitGroupDescriptions.Any(a => a.ProcurementUnitGroupDescriptionItems.Any(a2 => a2.ProcItemId == procItemId))).FirstOrDefaultAsync();
            var setUnitCost = unitGroup.UnitCost;
            var setTotalCost = unitGroup.TotalCost;
            var setQty = unitGroup.Qty;
            procItem.PriceRate = priceRate;

            if (priceRate == 0)
            {
                procItem.UnitCost = unitCost;
                procItem.PriceRate = decimal.Round((decimal)((unitCost * procItem.Qty * setQty) / setTotalCost) * 100, 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                procItem.PriceRate = priceRate;
                procItem.UnitCost = decimal.Round((decimal)(setUnitCost * (priceRate / 100)), 2, MidpointRounding.AwayFromZero) / procItem.Qty;
            }
            procItem.Amount = (procItem.Qty * procItem.UnitCost) * setQty;
            procItem.UpdatedBy = user;
            procItem.UpdatedDt = date;

            await _db.SaveChangesAsync();
        }        
    }
}