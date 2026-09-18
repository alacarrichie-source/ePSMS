using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using iLgs.Models;

namespace iLgs.Services.PropertyCard
{
    public interface IPropertyCardSubItemService
    {
        IQueryable<PropertyCardComponentVM> GetComponents(Guid cardId, Guid? acquisitionId = null);
        Task<bool> CreateComponentAsync(Guid cardId, PropertyCardComponentVM model, string userName, ModelStateDictionary modelState);
        Task<bool> UpdateComponentAsync(Guid cardId, PropertyCardComponentVM model, string userName, ModelStateDictionary modelState);
        Task<bool> DeleteComponentAsync(Guid cardId, Guid id, string userName, ModelStateDictionary modelState);

        // Phase 3: Component Physical Units
        IQueryable<PropertyCardUnitVM> GetComponentUnits(Guid cardId, Guid componentId);
        Task<bool> CreateComponentUnitAsync(Guid cardId, Guid componentId, PropertyCardUnitVM model, string userName, ModelStateDictionary modelState);
        Task<bool> UpdateComponentUnitAsync(Guid cardId, PropertyCardUnitVM model, string userName, ModelStateDictionary modelState);
        Task<bool> DeleteComponentUnitAsync(Guid cardId, Guid id, string userName, ModelStateDictionary modelState);
    }

    public class PropertyCardSubItemService : IPropertyCardSubItemService
    {
        private readonly AppManEntities _db;

        public PropertyCardSubItemService(AppManEntities db)
        {
            _db = db;
        }

        #region Phase 2: Component Definitions

        public IQueryable<PropertyCardComponentVM> GetComponents(Guid cardId, Guid? acquisitionId = null)
        {
            var query = _db.PsCardSubItems.AsNoTracking()
                .Where(s => s.PsCardItem.PsCardId == cardId);

            if (acquisitionId.HasValue && acquisitionId.Value != Guid.Empty)
            {
                query = query.Where(s => s.PsCardItemId == acquisitionId.Value);
            }

            return from s in query
                   let physCount = _db.PsCardItemExtns.Count(e => e.PsCardSubItemId == s.Id)
                   let activeAssignedQty = _db.IcsParItemComponents
                       .Where(c => c.PsCardSubItemId == s.Id && (c.IcsParItem.IcsPar.PostedDt == null || !_db.IcsParItems.Any(succ => succ.PrevItemId == c.IcsParItemId && succ.IcsPar.PostedDt != null)))
                       .Sum(c => (decimal?)c.Qty) ?? 0
                   let activePhysAssignedCount = _db.IcsParItemComponents
                       .Where(c => c.PsCardSubItemId == s.Id && c.PsCardItemExtnId != null && (c.IcsParItem.IcsPar.PostedDt == null || !_db.IcsParItems.Any(succ => succ.PrevItemId == c.IcsParItemId && succ.IcsPar.PostedDt != null)))
                       .Select(c => c.PsCardItemExtnId.Value)
                       .Distinct()
                       .Count()
                   let isCardPosted = s.PsCardItem.PsCard.PostedDt != null
                   let isAcqPosted = s.PsCardItem.PostedDt != null
                   let isAir = s.AIRSubItemId != null || (s.PsCardItem.AIRItemId != null && isAcqPosted)
                   select new PropertyCardComponentVM
                   {
                       Id = s.Id,
                       PsCardItemId = s.PsCardItemId ?? Guid.Empty,
                       AcquisitionPoNo = s.PsCardItem.PoNo,
                       SubItemNo = s.SubItemNo,
                       Description = s.Description,
                       Qty = s.Qty,
                       Unit = s.Unit,
                       UnitCost = s.UnitCost,
                       QtyPerParent = s.QtyPerParent,
                       SourceType = s.SourceType,
                       IsRequiredForBundle = s.IsRequiredForBundle ?? false,
                       Remarks = s.Remarks,
                       AIRSubItemId = s.AIRSubItemId,
                       IsAirSource = isAir,
                       ReceivedQty = s.Qty,
                       PhysicalUnitCount = physCount,
                       AssignedCount = physCount > 0 ? activePhysAssignedCount : activeAssignedQty,
                       AvailableCount = physCount > 0
                           ? ((physCount - activePhysAssignedCount) > 0 ? (physCount - activePhysAssignedCount) : 0)
                           : ((s.Qty - activeAssignedQty) > 0 ? (s.Qty - activeAssignedQty) : 0),
                       CanEdit = !isCardPosted && !isAcqPosted && !isAir && activeAssignedQty == 0,
                       CanDelete = !isCardPosted && !isAcqPosted && !isAir && physCount == 0 && activeAssignedQty == 0,
                       CanCreateUnit = !isCardPosted && !isAcqPosted && physCount < s.Qty
                   };
        }

        public async Task<bool> CreateComponentAsync(Guid cardId, PropertyCardComponentVM model, string userName, ModelStateDictionary modelState)
        {
            if (model == null)
            {
                modelState.AddModelError("", "Invalid component payload.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                modelState.AddModelError("Description", "Description is required.");
            }

            if (model.Qty <= 0)
            {
                modelState.AddModelError("Qty", "Quantity must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(model.Unit))
            {
                model.Unit = "pc";
            }

            // Validate parent acquisition ownership
            var acquisition = await _db.PsCardItems.Include(i => i.PsCard).FirstOrDefaultAsync(x => x.Id == model.PsCardItemId && x.PsCardId == cardId);
            if (acquisition == null)
            {
                modelState.AddModelError("PsCardItemId", "The selected acquisition was not found or does not belong to this Property Card.");
                return false;
            }

            if (acquisition.PsCard.PostedDt != null)
            {
                modelState.AddModelError("", "Cannot add components to a posted Property Card.");
                return false;
            }

            if (acquisition.PostedDt != null)
            {
                modelState.AddModelError("", "This acquisition is posted. Components are read-only.");
                return false;
            }

            if (!modelState.IsValid) return false;

            // Auto-assign SubItemNo if empty
            if (string.IsNullOrWhiteSpace(model.SubItemNo))
            {
                var existingNos = await _db.PsCardSubItems
                    .Where(s => s.PsCardItemId == acquisition.Id)
                    .Select(s => s.SubItemNo)
                    .ToListAsync();

                int maxNo = 0;
                foreach (var sn in existingNos)
                {
                    int n;
                    if (int.TryParse(sn, out n) && n > maxNo) maxNo = n;
                }
                model.SubItemNo = (maxNo + 1).ToString();
            }

            if (string.IsNullOrWhiteSpace(model.SourceType))
            {
                model.SourceType = "ORDERED";
            }

            if (!model.QtyPerParent.HasValue || model.QtyPerParent.Value <= 0)
            {
                if (acquisition.Qty.HasValue && acquisition.Qty.Value > 0)
                {
                    model.QtyPerParent = Math.Max(1, Math.Floor(model.Qty / acquisition.Qty.Value));
                }
                else
                {
                    model.QtyPerParent = 1;
                }
            }

            var entity = new PsCardSubItem
            {
                Id = Guid.NewGuid(),
                PsCardItemId = acquisition.Id,
                AIRSubItemId = null,
                SubItemNo = model.SubItemNo,
                Description = model.Description,
                Qty = model.Qty,
                Unit = model.Unit,
                UnitCost = model.UnitCost,
                QtyPerParent = model.QtyPerParent,
                SourceType = model.SourceType,
                IsRequiredForBundle = model.IsRequiredForBundle,
                Remarks = model.Remarks,
                InsertedBy = userName,
                InsertedDt = DateTime.Now
            };

            _db.PsCardSubItems.Add(entity);
            await _db.SaveChangesAsync();

            model.Id = entity.Id;
            model.AcquisitionPoNo = acquisition.PoNo;
            model.ReceivedQty = entity.Qty;
            model.PhysicalUnitCount = 0;
            model.AssignedCount = 0;
            model.AvailableCount = entity.Qty;
            model.IsAirSource = false;
            model.CanEdit = true;
            model.CanDelete = true;
            model.CanCreateUnit = true;

            return true;
        }

        public async Task<bool> UpdateComponentAsync(Guid cardId, PropertyCardComponentVM model, string userName, ModelStateDictionary modelState)
        {
            if (model == null)
            {
                modelState.AddModelError("", "Invalid component payload.");
                return false;
            }

            var existing = await _db.PsCardSubItems.Include(i => i.PsCardItem.PsCard).FirstOrDefaultAsync(s => s.Id == model.Id && s.PsCardItem.PsCardId == cardId);
            if (existing == null)
            {
                modelState.AddModelError("", "Component not found or does not belong to this Property Card.");
                return false;
            }

            if (existing.PsCardItem.PsCard.PostedDt != null)
            {
                modelState.AddModelError("", "Cannot modify components on a posted Property Card.");
                return false;
            }

            if (existing.PsCardItem.PostedDt != null)
            {
                modelState.AddModelError("", "This acquisition is posted. Components are read-only.");
                return false;
            }

            bool isAir = existing.AIRSubItemId != null || (existing.PsCardItem.AIRItemId != null && existing.PsCardItem.PostedDt != null);
            if (isAir)
            {
                if (existing.Description != model.Description ||
                    existing.Qty != model.Qty ||
                    existing.QtyPerParent != model.QtyPerParent ||
                    existing.SourceType != model.SourceType ||
                    (existing.IsRequiredForBundle ?? false) != model.IsRequiredForBundle)
                {
                    modelState.AddModelError("", "This component originated from a posted AIR. Description, Quantity, Qty Per Parent, Source Type, and Bundle Requirement cannot be modified.");
                    return false;
                }
            }

            int physicalCount = await _db.PsCardItemExtns.CountAsync(e => e.PsCardSubItemId == model.Id);
            if (physicalCount > 0 && model.Qty < physicalCount)
            {
                modelState.AddModelError("Qty", $"Quantity cannot be reduced below the number of existing individual physical units ({physicalCount}).");
                return false;
            }

            decimal activeAssignedQty = await _db.IcsParItemComponents
                .Where(c => c.PsCardSubItemId == model.Id && (c.IcsParItem.IcsPar.PostedDt == null || !_db.IcsParItems.Any(succ => succ.PrevItemId == c.IcsParItemId && succ.IcsPar.PostedDt != null)))
                .SumAsync(c => (decimal?)c.Qty) ?? 0;

            if (activeAssignedQty > 0)
            {
                if (model.Qty < activeAssignedQty)
                {
                    modelState.AddModelError("Qty", $"Quantity cannot be reduced below the actively assigned quantity ({activeAssignedQty:n0}).");
                    return false;
                }

                if ((existing.IsRequiredForBundle ?? false) != model.IsRequiredForBundle)
                {
                    modelState.AddModelError("IsRequiredForBundle", "Bundle requirement cannot be changed for a component that is actively assigned in PAR/ICS.");
                    return false;
                }

                if (existing.SourceType != model.SourceType)
                {
                    modelState.AddModelError("SourceType", "Source type cannot be changed for a component that is actively assigned in PAR/ICS.");
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                modelState.AddModelError("Description", "Description is required.");
                return false;
            }

            if (model.Qty <= 0)
            {
                modelState.AddModelError("Qty", "Quantity must be greater than zero.");
                return false;
            }

            if (!modelState.IsValid) return false;

            if (!isAir && activeAssignedQty == 0)
            {
                existing.SubItemNo = model.SubItemNo;
                existing.Description = model.Description;
                existing.Qty = model.Qty;
                existing.Unit = model.Unit;
                existing.QtyPerParent = model.QtyPerParent;
                existing.SourceType = model.SourceType;
                existing.IsRequiredForBundle = model.IsRequiredForBundle;
            }
            else if (!isAir && activeAssignedQty > 0)
            {
                // Qty can be increased if valid
                existing.Qty = model.Qty;
                existing.Unit = model.Unit;
            }

            existing.UnitCost = model.UnitCost;
            existing.Remarks = model.Remarks;
            existing.UpdatedBy = userName;
            existing.UpdatedDt = DateTime.Now;

            await _db.SaveChangesAsync();

            model.AcquisitionPoNo = existing.PsCardItem.PoNo;
            model.ReceivedQty = existing.Qty;
            model.PhysicalUnitCount = physicalCount;
            model.AssignedCount = activeAssignedQty;
            model.AvailableCount = Math.Max(0, existing.Qty - activeAssignedQty);
            model.IsAirSource = isAir;
            model.CanEdit = !isAir && activeAssignedQty == 0;
            model.CanDelete = !isAir && physicalCount == 0 && activeAssignedQty == 0;
            model.CanCreateUnit = physicalCount < existing.Qty;

            return true;
        }

        public async Task<bool> DeleteComponentAsync(Guid cardId, Guid id, string userName, ModelStateDictionary modelState)
        {
            var existing = await _db.PsCardSubItems.FirstOrDefaultAsync(s => s.Id == id && s.PsCardItem.PsCardId == cardId);
            if (existing == null)
            {
                modelState.AddModelError("", "Component not found or does not belong to this Property Card.");
                return false;
            }

            if (existing.PsCardItem.PsCard.PostedDt != null)
            {
                modelState.AddModelError("", "Cannot delete components from a posted Property Card.");
                return false;
            }

            if (existing.PsCardItem.PostedDt != null)
            {
                modelState.AddModelError("", "This acquisition is posted. Components are read-only.");
                return false;
            }

            int physicalCount = await _db.PsCardItemExtns.CountAsync(e => e.PsCardSubItemId == id);
            if (physicalCount > 0)
            {
                modelState.AddModelError("", "This component cannot be deleted because individual physical units already exist for it.");
                return false;
            }

            var assigned = await (from c in _db.IcsParItemComponents
                                  where c.PsCardSubItemId == id
                                  select new { c.IcsParItem.IcsPar.RefType, c.IcsParItem.IcsPar.RefNo })
                                 .FirstOrDefaultAsync();
            if (assigned != null)
            {
                var doc = (assigned.RefType == "P" ? "PAR" : (assigned.RefType == "I" ? "ICS" : (assigned.RefType ?? "PAR/ICS"))) + " No. " + assigned.RefNo;
                modelState.AddModelError("", $"This component cannot be deleted because it is already referenced by {doc}.");
                return false;
            }

            bool isAir = existing.AIRSubItemId != null || (existing.PsCardItem.AIRItemId != null && existing.PsCardItem.PostedDt != null);
            if (isAir)
            {
                modelState.AddModelError("", "This component cannot be deleted because it belongs to an authoritative AIR source.");
                return false;
            }

            _db.PsCardSubItems.Remove(existing);
            await _db.SaveChangesAsync();

            return true;
        }

        #endregion

        #region Phase 3: Component Physical Units

        public IQueryable<PropertyCardUnitVM> GetComponentUnits(Guid cardId, Guid componentId)
        {
            var query = _db.PsCardItemExtns.AsNoTracking()
                .Where(e => e.PsCardSubItemId == componentId && e.PsCardItem.PsCardId == cardId);

            return from u in query
                   let other = _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().Where(o => o.Id == u.Id).Select(o => o.SerialNo).FirstOrDefault()
                   let veh = _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>().Where(v => v.Id == u.Id).Select(v => new { v.MVFileNo, v.PlateNo, v.EngineNo }).FirstOrDefault()
                   let activeComp = (from c in _db.IcsParItemComponents
                                     where c.PsCardItemExtnId == u.Id
                                       && (c.IcsParItem.IcsPar.PostedDt == null || !_db.IcsParItems.Any(succ => succ.PrevItemId == c.IcsParItemId && succ.IcsPar.PostedDt != null))
                                     select new { c.IcsParItem.IcsPar.RefType, c.IcsParItem.IcsPar.RefNo, c.IcsParItem.IcsPar.PostedDt }).FirstOrDefault()
                   let transferredComp = (from c in _db.IcsParItemComponents
                                          where c.PsCardItemExtnId == u.Id
                                            && _db.IcsParItems.Any(succ => succ.PrevItemId == c.IcsParItemId && succ.IcsPar.PostedDt != null)
                                          select new { c.IcsParItem.IcsPar.RefType, c.IcsParItem.IcsPar.RefNo }).FirstOrDefault()
                   let isCardPosted = u.PsCardItem.PsCard.PostedDt != null
                   let isAcqPostedCU = u.PsCardItem.PostedDt != null
                   orderby u.ContentNo
                   select new PropertyCardUnitVM
                   {
                       Id = u.Id,
                       PsCardItemId = u.PsCardItemId ?? Guid.Empty,
                       PsCardSubItemId = u.PsCardSubItemId,
                       ContentNo = u.ContentNo,
                       Description = u.PsCardSubItem.Description,
                       PropertyNo = u.PropNo,
                       CustItemNo = u.CustItemNo,
                       SerialNo = other,
                       PlateNo = veh != null ? veh.MVFileNo : null,
                       Location = u.Codextn != null ? u.Codextn.Description : null,
                       LocationId = u.LocationId,
                       Condition = u.Condition,
                       AcquisitionCost = u.AcqCost ?? u.PsCardSubItem.UnitCost,
                       AccountabilityStatus = activeComp != null
                            ? (activeComp.PostedDt != null
                                ? ((activeComp.RefType == "P" || activeComp.RefType == "PAR") ? "PAR ASSIGNED" : "ICS ASSIGNED")
                                : ((activeComp.RefType == "P" || activeComp.RefType == "PAR") ? "PAR DRAFT" : "ICS DRAFT"))
                            : (transferredComp != null
                                ? "TRANSFERRED WITH BUNDLE"
                                : "AVAILABLE"),
                       CanEdit = !isCardPosted && !isAcqPostedCU && activeComp == null,
                       CanDelete = !isCardPosted && !isAcqPostedCU && activeComp == null && transferredComp == null && u.AIRItemExtnId == null
                   };
        }

        public async Task<bool> CreateComponentUnitAsync(Guid cardId, Guid componentId, PropertyCardUnitVM model, string userName, ModelStateDictionary modelState)
        {
            if (model == null)
            {
                modelState.AddModelError("", "Invalid unit payload.");
                return false;
            }

            if (componentId == Guid.Empty)
            {
                modelState.AddModelError("", "Component reference is required.");
                return false;
            }

            var component = await _db.PsCardSubItems
                .Include(s => s.PsCardItem.PsCard)
                .FirstOrDefaultAsync(s => s.Id == componentId && s.PsCardItem.PsCardId == cardId);

            if (component == null)
            {
                modelState.AddModelError("", "Parent component definition not found or does not belong to this Property Card.");
                return false;
            }

            if (component.PsCardItemId == null || component.PsCardItemId == Guid.Empty)
            {
                modelState.AddModelError("", "Component parent acquisition record is missing.");
                return false;
            }

            if (component.PsCardItem.PsCard.PostedDt != null)
            {
                modelState.AddModelError("", "Cannot add physical units to a posted Property Card.");
                return false;
            }

            if (component.PsCardItem.PostedDt != null)
            {
                modelState.AddModelError("", "This acquisition is posted. Components are read-only.");
                return false;
            }

            // Enforce quantity limit
            int existingUnits = await _db.PsCardItemExtns.CountAsync(e => e.PsCardSubItemId == componentId);
            if (existingUnits >= component.Qty)
            {
                modelState.AddModelError("", $"Cannot create another physical unit for this component. The component quantity is {component.Qty:n0} and already has {existingUnits} physical units.");
                return false;
            }

            if (!modelState.IsValid) return false;

            int maxContentNo = await _db.PsCardItemExtns
                .Where(e => e.PsCardSubItemId == componentId)
                .Select(e => (int?)e.ContentNo)
                .MaxAsync() ?? 0;
            int nextContentNo = maxContentNo + 1;

            var unit = new PsCardItemExtnOther
            {
                Id = Guid.NewGuid(),
                PsCardItemId = component.PsCardItemId,
                PsCardSubItemId = component.Id,
                ContentNo = nextContentNo,
                PropNo = model.PropertyNo,
                CustItemNo = model.CustItemNo,
                LocationId = model.LocationId,
                Condition = string.IsNullOrWhiteSpace(model.Condition) ? "Good" : model.Condition,
                AcqCost = model.AcquisitionCost ?? component.UnitCost,
                SerialNo = model.SerialNo,
                InsertedBy = userName,
                InsertedDt = DateTime.Now
            };

            _db.PsCardItemExtns.Add(unit);
            await _db.SaveChangesAsync();

            model.Id = unit.Id;
            model.PsCardItemId = component.PsCardItemId ?? Guid.Empty;
            model.PsCardSubItemId = component.Id;
            model.ContentNo = nextContentNo;
            model.Description = component.Description;
            model.AcquisitionCost = unit.AcqCost;
            model.AccountabilityStatus = "AVAILABLE";
            model.CanEdit = true;
            model.CanDelete = true;

            return true;
        }

        public async Task<bool> UpdateComponentUnitAsync(Guid cardId, PropertyCardUnitVM model, string userName, ModelStateDictionary modelState)
        {
            if (model == null)
            {
                modelState.AddModelError("", "Invalid unit payload.");
                return false;
            }

            var existing = await _db.PsCardItemExtns
                .Include(e => e.PsCardItem.PsCard)
                .FirstOrDefaultAsync(e => e.Id == model.Id && e.PsCardItem.PsCardId == cardId);

            if (existing == null || existing.PsCardSubItemId == null)
            {
                modelState.AddModelError("", "Component physical unit not found or does not belong to this Property Card.");
                return false;
            }

            if (existing.PsCardItem.PsCard.PostedDt != null)
            {
                modelState.AddModelError("", "Cannot modify units on a posted Property Card.");
                return false;
            }

            if (existing.PsCardItem.PostedDt != null)
            {
                modelState.AddModelError("", "This acquisition is posted. Components are read-only.");
                return false;
            }

            var assigned = await (from c in _db.IcsParItemComponents
                                  where c.PsCardItemExtnId == model.Id
                                  select new { c.IcsParItem.IcsPar.RefType, c.IcsParItem.IcsPar.RefNo, c.IcsParItem.IcsPar.PostedDt })
                                 .FirstOrDefaultAsync();
            bool isAccountable = assigned != null;

            if (isAccountable)
            {
                var other = await _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().FirstOrDefaultAsync(o => o.Id == model.Id);
                if (existing.PropNo != model.PropertyNo || (other != null && other.SerialNo != model.SerialNo))
                {
                    var doc = (assigned.RefType == "P" ? "PAR" : (assigned.RefType == "I" ? "ICS" : (assigned.RefType ?? "PAR/ICS"))) + " No. " + assigned.RefNo;
                    modelState.AddModelError("", $"This component physical unit is assigned to {doc}. Identity fields (Property No., Serial No.) cannot be modified.");
                    return false;
                }
            }

            if (!isAccountable)
            {
                existing.PropNo = model.PropertyNo;
                existing.CustItemNo = model.CustItemNo;
                var other = await _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().FirstOrDefaultAsync(o => o.Id == model.Id);
                if (other != null)
                {
                    other.SerialNo = model.SerialNo;
                }
            }

            existing.Condition = model.Condition;
            existing.LocationId = model.LocationId;
            existing.AcqCost = model.AcquisitionCost;
            existing.UpdatedBy = userName;
            existing.UpdatedDt = DateTime.Now;

            await _db.SaveChangesAsync();

            model.AccountabilityStatus = isAccountable
                ? ((assigned.RefType == "P" ? "PAR" : (assigned.RefType == "I" ? "ICS" : (assigned.RefType ?? "PAR/ICS"))) + (assigned.PostedDt != null ? " ASSIGNED" : " DRAFT"))
                : "AVAILABLE";
            model.CanEdit = !isAccountable;
            model.CanDelete = !isAccountable && existing.AIRItemExtnId == null;

            return true;
        }

        public async Task<bool> DeleteComponentUnitAsync(Guid cardId, Guid id, string userName, ModelStateDictionary modelState)
        {
            var existing = await _db.PsCardItemExtns
                .Include(e => e.PsCardItem.PsCard)
                .FirstOrDefaultAsync(e => e.Id == id && e.PsCardItem.PsCardId == cardId);

            if (existing == null || existing.PsCardSubItemId == null)
            {
                modelState.AddModelError("", "Component physical unit not found.");
                return false;
            }

            if (existing.PsCardItem.PsCard.PostedDt != null)
            {
                modelState.AddModelError("", "Cannot delete units from a posted Property Card.");
                return false;
            }

            if (existing.PsCardItem.PostedDt != null)
            {
                modelState.AddModelError("", "This acquisition is posted. Components are read-only.");
                return false;
            }

            // Downstream check: check if referenced in IcsParItemComponents
            var assigned = await (from c in _db.IcsParItemComponents
                                  where c.PsCardItemExtnId == id
                                  select new { c.IcsParItem.IcsPar.RefType, c.IcsParItem.IcsPar.RefNo })
                                 .FirstOrDefaultAsync();
            if (assigned != null)
            {
                var doc = (assigned.RefType == "P" ? "PAR" : (assigned.RefType == "I" ? "ICS" : (assigned.RefType ?? "PAR/ICS"))) + " No. " + assigned.RefNo;
                modelState.AddModelError("", $"This component unit cannot be deleted because it is assigned to {doc}.");
                return false;
            }

            if (existing.AIRItemExtnId != null)
            {
                modelState.AddModelError("", "This component unit cannot be deleted because it originated from an authoritative AIR record.");
                return false;
            }

            // Clean up any child bridge records if present
            var bridge = _db.PsCardItemTransferItems.Where(t => t.PsCardItemExtnId == id).ToList();
            if (bridge.Any())
            {
                _db.PsCardItemTransferItems.RemoveRange(bridge);
            }

            _db.PsCardItemExtns.Remove(existing);
            await _db.SaveChangesAsync();

            return true;
        }

        #endregion
    }
}
