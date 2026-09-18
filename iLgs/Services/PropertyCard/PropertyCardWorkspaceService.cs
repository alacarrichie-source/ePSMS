using System.Threading.Tasks;
using System.Web.Mvc;
﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using iLgs.Models;

namespace iLgs.Services.PropertyCard
{
    // Read-only projections. All changes remain in the existing PropertyCard/StockCard services.
    public class PropertyCardWorkspaceService
    {
        private readonly AppManEntities _db;
        private readonly IPsCardItemService _items;

        public PropertyCardWorkspaceService(AppManEntities db, IPsCardItemService items)
        {
            _db = db;
            _items = items;
        }

        public PropertyCardWorkspaceVM Get(Guid id)
        {
            var card = _db.PsCards.AsNoTracking().Where(x => x.Id == id && x.CardCategory == "P")
                .Select(x => new PropertyCardVM {
                    Id = x.Id, PsNo = x.PsNo, Item = x.ItemCode.Description,
                    ItemType = x.ItemCode.ItemType.Description, ItemTypeCode = x.ItemCode.ItemType.Code,
                    Fund = x.Fund, Description = x.Description, CardCategory = x.CardCategory,
                    InsertedBy = x.InsertedBy, InsertedDt = x.InsertedDt,
                    SubAccount = _db.SubAccountViews.Where(a => a.Id == x.ItemCodeId).Select(a => a.SubAccount).FirstOrDefault()
                }).FirstOrDefault();
            if (card == null) return null;
            return new PropertyCardWorkspaceVM { Card = card, Position = Position(id) };
        }

        public PropertyCardPositionVM Position(Guid id)
        {
            // Use the existing transfer projection and its balance-value formula.
            // Only root transfers contribute original receipts.
            var position = _items.GetTransitByCardId(id, null).GroupBy(x => 1)
                .Select(g => new PropertyCardPositionVM {
                    Received = g.Sum(x => x.ParentId == null ? (x.Qty ?? 0) : 0),
                    TransferIn = g.Sum(x => x.TransferIn ?? 0),
                    Issued = g.Sum(x => x.QtyIss ?? 0),
                    TransferOut = g.Sum(x => x.TransferOut ?? 0),
                    Balance = g.Sum(x => x.QtyBal ?? 0),
                    BalanceValue = g.Sum(x => x.Amount ?? 0)
                }).FirstOrDefault() ?? new PropertyCardPositionVM();
            var acquisitions = _db.PsCardItems.AsNoTracking().Where(x => x.PsCardId == id);
            position.AcquisitionCount = acquisitions.Count();
            position.UnitCount = _db.PsCardItemExtns.Count(x => x.PsCardItem.PsCardId == id && x.PsCardSubItemId == null);
            var latest = acquisitions.OrderByDescending(x => x.PoDate).ThenByDescending(x => x.Id)
                .Select(x => new { x.PoNo, x.PoDate }).FirstOrDefault();
            if (latest != null) { position.LatestPo = latest.PoNo; position.LatestPoDate = latest.PoDate; }
            position.LatestAcquisitionDate = acquisitions.Select(x => x.AcqDate).Max();
            return position;
        }

        public IQueryable<PropertyCardUnitChoiceVM> Units(Guid id)
        {
            return _db.PsCardItemExtns.AsNoTracking().Where(x => x.PsCardItem.PsCardId == id && x.PsCardSubItemId == null)
                .Select(x => new PropertyCardUnitChoiceVM {
                    Id = x.Id, AcquisitionId = x.PsCardItemId, PoNo = x.PsCardItem.PoNo,
                    PropNo = x.PropNo, CustItemNo = x.CustItemNo,
                    Label = (x.PsCardItem.PoNo ?? "No PO") + " / " 
                        + (x.PsCardItem.Description ?? " - ") + " / "
                        + (_db.PsCardItemExtns.OfType<PsCardItemExtnOther>().Where(o => o.Id == x.Id).Select(o => o.SerialNo).FirstOrDefault() ??
                          _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>().Where(o => o.Id == x.Id).Select(o => o.MVFileNo).FirstOrDefault()) ?? "Unnumbered unit",
                    Location = x.Codextn.Description, Condition = x.Condition
                });
        }
        
        public IQueryable<PropertyCardHistoryVM> History(Guid id)
        {
            // Stored transaction records for both unit-level and acquisition-level events under this card.
            return from transaction in _db.PsCardItemTransactions.AsNoTracking()
                   join unit in _db.PsCardItemExtns on transaction.PsCardItemExtnId equals unit.Id into unitGroup
                   from unit in unitGroup.DefaultIfEmpty()
                   join item in _db.PsCardItems on transaction.PsCardItemId equals item.Id into itemGroup
                   from item in itemGroup.DefaultIfEmpty()
                   where (unit != null && unit.PsCardItem.PsCardId == id) || (item != null && item.PsCardId == id)
                   select new PropertyCardHistoryVM {
                       Id = transaction.Id,
                       PropNo = unit != null ? unit.PropNo : null,
                       PoNo = unit != null ? unit.PsCardItem.PoNo : (item != null ? item.PoNo : null),
                       Remarks = transaction.Remarks,
                       TransferId = transaction.PsCardItemTransferId,
                       IssuanceId = transaction.PsCardItemIssuanceId,
                       IcsParId = transaction.IcsParId,
                       InsertedBy = transaction.InsertedBy,
                       InsertedDt = transaction.InsertedDt,
                       UpdatedDt = transaction.UpdatedDt
                   };
        }

        public IQueryable<PropertyCardUnitVM> WorkspaceUnits(Guid cardId, Guid? acquisitionId = null)
        {
            var units = _db.PsCardItemExtns.AsNoTracking()
                .Where(x => x.PsCardItem.PsCardId == cardId && x.PsCardSubItemId == null);

            if (acquisitionId.HasValue && acquisitionId.Value != Guid.Empty)
            {
                units = units.Where(x => x.PsCardItemId == acquisitionId.Value);
            }

            return from u in units
                   let other = _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().Where(o => o.Id == u.Id).Select(o => o.SerialNo).FirstOrDefault()
                   let veh = _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>().Where(v => v.Id == u.Id).Select(v => new { v.PlateNo, v.MVFileNo, v.ConductionNo }).FirstOrDefault()
                   let land = _db.PsCardItemExtns.OfType<PsCardItemExtnLand>().Where(l => l.Id == u.Id).Select(l => l.PIN).FirstOrDefault()
                   let bldg = _db.PsCardItemExtns.OfType<PsCardItemExtnBuilding>().Where(b => b.Id == u.Id).Select(b => b.BuildingItem).FirstOrDefault()
                   let hasIcsPar = _db.IcsParItems.Any(i => i.PsCardItemExtnId == u.Id)
                   let hasPosted = _db.IcsParItems.Any(i => i.PsCardItemExtnId == u.Id && i.IcsPar.PostedDt != null)
                   let draftRef = _db.IcsParItems.Where(i => i.PsCardItemExtnId == u.Id && i.IcsPar.PostedDt == null).Select(i => i.IcsPar.RefType).FirstOrDefault()
                   let currentPosted = _db.IcsParItems.Where(i => i.PsCardItemExtnId == u.Id && i.IcsPar.PostedDt != null && !_db.IcsParItems.Any(s => s.PrevItemId == i.Id && s.IcsPar.PostedDt != null)).OrderByDescending(i => i.IcsPar.PostedDt).Select(i => new { i.IcsPar.RefType, i.Id }).FirstOrDefault()
                   let hasDraftSuccessor = currentPosted != null && _db.IcsParItems.Any(s => s.PrevItemId == currentPosted.Id && s.IcsPar.PostedDt == null)
                   let isTransferred = _db.PsCardItemTransferItems.Any(t => t.PsCardItemExtnId == u.Id && t.PsCardItemTransfer.ParentId != null)
                   let isIssued = _db.PsCardItemTransferItems.Any(t => t.PsCardItemExtnId == u.Id && t.PsCardItemTransferIssuanceItems.Any())
                   let isCardPosted = u.PsCardItem.PsCard.PostedDt != null
                   let isAcqPosted = u.PsCardItem.PostedDt != null
                   select new PropertyCardUnitVM
                   {
                       Id = u.Id,
                       PsCardItemId = u.PsCardItemId ?? Guid.Empty,
                       LocationId = u.LocationId,
                       Remarks = u.Remarks,
                       ContentNo = u.ContentNo,
                       Description = u.PsCardItem.Description,
                       PropertyNo = u.PropNo,
                       CustItemNo = u.CustItemNo,
                       SerialNo = other ?? (veh != null ? (veh.PlateNo ?? veh.ConductionNo) : (land ?? bldg)),
                       PlateNo = veh != null ? veh.PlateNo : null,
                       Location = u.Codextn != null ? u.Codextn.Description : null,
                       Condition = u.Condition,
                       AcquisitionCost = u.AcqCost ?? u.PsCardItem.UnitCost,
                       AccountabilityStatus = !hasIcsPar
                            ? (isIssued ? "Issued" : (isTransferred ? "Transferred" : "AVAILABLE"))
                            : (!hasPosted
                                ? ((draftRef == "P" || draftRef == "PAR") ? "PAR DRAFT" : "ICS DRAFT")
                                : (hasDraftSuccessor
                                    ? "TRANSFER PENDING"
                                    : ((currentPosted != null && (currentPosted.RefType == "P" || currentPosted.RefType == "PAR")) ? "PAR POSTED" : "ICS POSTED"))),
                       CanEdit = !isCardPosted && !isAcqPosted && !hasIcsPar && !isTransferred && !isIssued,
                       CanDelete = !isCardPosted && !isAcqPosted && !hasIcsPar && !isTransferred && !isIssued && u.AIRItemExtnId == null
                   };
        }

        
        public async Task<bool> CreateUnitAsync(Guid cardId, PropertyCardUnitVM model, string userName, ModelStateDictionary modelState, Guid? selectedAcquisitionId = null)
        {
            if (model == null)
            {
                modelState.AddModelError("", "Invalid unit payload.");
                return false;
            }

            Guid targetAcqId = (selectedAcquisitionId.HasValue && selectedAcquisitionId.Value != Guid.Empty)
                ? selectedAcquisitionId.Value
                : (model.PsCardItemId != Guid.Empty ? model.PsCardItemId : Guid.Empty);

            if (targetAcqId == Guid.Empty)
            {
                modelState.AddModelError("", "Selected acquisition is missing.");
                return false;
            }

            // Find parent acquisition:
            // 1. Direct PsCardItem.Id or GroupId match on this card
            var acquisition = await _db.PsCardItems.Include(x => x.PsCard)
                .FirstOrDefaultAsync(x => (x.Id == targetAcqId || x.GroupId == targetAcqId) && x.PsCardId == cardId);

            // 2. If not found, resolve through PsCardItemTransfer.Id on this card
            if (acquisition == null)
            {
                var transferBridge = await _db.PsCardItemTransfers
                    .Include(t => t.PsCardItem.PsCard)
                    .FirstOrDefaultAsync(t => t.Id == targetAcqId && t.PsCardItem.PsCardId == cardId);
                if (transferBridge != null)
                {
                    acquisition = transferBridge.PsCardItem;
                }
            }

            if (acquisition == null)
            {
                modelState.AddModelError("", "The selected acquisition does not belong to this Property Card.");
                return false;
            }

            // Authoritatively assign parent IDs for main physical unit
            model.PsCardItemId = acquisition.Id;
            model.PsCardSubItemId = null;

            if (acquisition.PsCard.PostedDt != null)
            {
                modelState.AddModelError("", "Cannot add physical units to a posted Property Card.");
                return false;
            }

            if (acquisition.PostedDt != null)
            {
                modelState.AddModelError("", "This acquisition is posted. Individual units are read-only.");
                return false;
            }

            // Strict quantity limit enforcement:
            int existingUnits = await _db.PsCardItemExtns.CountAsync(e => e.PsCardItemId == acquisition.Id && e.PsCardSubItemId == null);
            int maxQty = (int)(acquisition.Qty ?? 0);
            if (existingUnits >= maxQty)
            {
                modelState.AddModelError("", $"Cannot add another individual unit. The acquisition already has the maximum number of physical units ({maxQty}).");
                return false;
            }

            // Check duplicate Serial No if provided
            if (!string.IsNullOrWhiteSpace(model.SerialNo))
            {
                bool serialExists = await (from o in _db.PsCardItemExtns.OfType<PsCardItemExtnOther>()
                                           where o.PsCardItemId == acquisition.Id && o.PsCardSubItemId == null && o.SerialNo == model.SerialNo
                                           select o).AnyAsync()
                                 || await (from v in _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>()
                                           where v.PsCardItemId == acquisition.Id && v.PsCardSubItemId == null && (v.PlateNo == model.SerialNo || v.ConductionNo == model.SerialNo || v.MVFileNo == model.SerialNo)
                                           select v).AnyAsync();
                if (serialExists)
                {
                    modelState.AddModelError("SerialNo", $"Serial / Vehicle No. '{model.SerialNo}' already exists for this acquisition.");
                    return false;
                }
            }

            if (!modelState.IsValid) return false;

            int maxContentNo = await _db.PsCardItemExtns
                .Where(e => e.PsCardItemId == acquisition.Id && e.PsCardSubItemId == null)
                .Select(e => (int?)e.ContentNo)
                .MaxAsync() ?? 0;
            int nextContentNo = maxContentNo + 1;

            // Determine category / subtype
            var itemTypeCode = acquisition.PsCard.ItemCode?.ItemType?.Code;
            PsCardItemExtn entity;
            if (itemTypeCode == "TRANS_VEHICLE" || (acquisition.PsCard.ItemCode?.Description != null && acquisition.PsCard.ItemCode.Description.IndexOf("Vehicle", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                var veh = new PsCardItemExtnVehicle();
                veh.MVFileNo = model.SerialNo;
                entity = veh;
            }
            else if (itemTypeCode == "LAND")
            {
                var land = new PsCardItemExtnLand();
                land.PIN = model.SerialNo;
                entity = land;
            }
            else if (itemTypeCode == "BUILDING")
            {
                var bldg = new PsCardItemExtnBuilding();
                bldg.BuildingItem = model.SerialNo;
                entity = bldg;
            }
            else
            {
                var other = new PsCardItemExtnOther();
                other.SerialNo = model.SerialNo;
                entity = other;
            }

            entity.Id = Guid.NewGuid();
            entity.PsCardItemId = acquisition.Id;
            entity.PsCardSubItemId = null; // Strictly force NULL for main units!
            entity.ContentNo = nextContentNo;
            //entity.PropNo = model.PropertyNo;
            //entity.CustItemNo = model.CustItemNo;
            //entity.LocationId = model.LocationId;
            //entity.Condition = string.IsNullOrWhiteSpace(model.Condition) ? "Good" : model.Condition;
            entity.AcqCost = model.AcquisitionCost ?? acquisition.UnitCost;
            entity.Remarks = model.Remarks;
            entity.InsertedBy = userName;
            entity.InsertedDt = DateTime.Now;
            entity.UpdatedBy = userName;
            entity.UpdatedDt = DateTime.Now;

            // Link transfer item bridge if transfer exists
            var transfer = await _db.PsCardItemTransfers
                .Where(t => t.PsCardItemId == acquisition.Id && t.ParentId == null)
                .OrderBy(t => t.TransDate)
                .FirstOrDefaultAsync() ?? await _db.PsCardItemTransfers.FirstOrDefaultAsync(t => t.PsCardItemId == acquisition.Id);

            if (transfer != null)
            {
                entity.PsCardItemTransferItems.Add(new PsCardItemTransferItem
                {
                    Id = Guid.NewGuid(),
                    PsCardItemTransferId = transfer.Id,
                    PsCardItemExtnId = entity.Id,
                    InsertedBy = userName,
                    InsertedDt = DateTime.Now,
                    UpdatedBy = userName,
                    UpdatedDt = DateTime.Now
                });
            }

            _db.PsCardItemExtns.Add(entity);
            await _db.SaveChangesAsync();

            model.Id = entity.Id;
            model.PsCardItemId = acquisition.Id;
            model.PsCardSubItemId = null;
            model.ContentNo = nextContentNo;
            model.Description = acquisition.Description;
            model.AcquisitionCost = entity.AcqCost;
            model.AccountabilityStatus = "AVAILABLE";
            model.CanEdit = true;
            model.CanDelete = true;

            return true;
        }

        public async Task<bool> UpdateUnitAsync(Guid cardId, PropertyCardUnitVM model, string userName, ModelStateDictionary modelState)
        {
            if (model == null)
            {
                modelState.AddModelError("", "Invalid unit payload.");
                return false;
            }

            var existing = await _db.PsCardItemExtns
                .Include(e => e.PsCardItem.PsCard)
                .FirstOrDefaultAsync(e => e.Id == model.Id && e.PsCardItem.PsCardId == cardId && e.PsCardSubItemId == null);

            if (existing == null)
            {
                modelState.AddModelError("", "Physical unit not found or does not belong to this Property Card.");
                return false;
            }

            if (existing.PsCardItem.PsCard.PostedDt != null)
            {
                modelState.AddModelError("", "Cannot modify units on a posted Property Card.");
                return false;
            }

            if (existing.PsCardItem.PostedDt != null)
            {
                modelState.AddModelError("", "This acquisition is posted. Individual units are read-only.");
                return false;
            }

            // Check accountability lock
            bool hasIcsPar = await _db.IcsParItems.AnyAsync(i => i.PsCardItemExtnId == model.Id);
            bool hasCompIcs = await _db.IcsParItemComponents.AnyAsync(c => c.PsCardItemExtnId == model.Id);
            bool isTransferred = await _db.PsCardItemTransferItems.AnyAsync(t => t.PsCardItemExtnId == model.Id && t.PsCardItemTransfer.ParentId != null);
            bool isIssued = await _db.PsCardItemTransferItems.AnyAsync(t => t.PsCardItemExtnId == model.Id && t.PsCardItemTransferIssuanceItems.Any());
            bool isAccountable = hasIcsPar || hasCompIcs || isTransferred || isIssued;

            if (isAccountable)
            {
                var other = await _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().FirstOrDefaultAsync(o => o.Id == model.Id);
                var veh = await _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>().FirstOrDefaultAsync(v => v.Id == model.Id);
                string currentSerial = other?.SerialNo ?? (veh?.PlateNo ?? veh?.ConductionNo);

                if (!string.Equals(existing.PropNo?.Trim(), model.PropertyNo?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(model.SerialNo) && !string.Equals(currentSerial?.Trim(), model.SerialNo?.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    modelState.AddModelError("", "Cannot modify identity-sensitive fields (Property No. / Serial / Plate No.) because this physical unit is already assigned to an accountability record.");
                    return false;
                }
            }
            else
            {
                existing.PropNo = model.PropertyNo;
                existing.CustItemNo = model.CustItemNo;
                var other = await _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().FirstOrDefaultAsync(o => o.Id == model.Id);
                if (other != null) other.SerialNo = model.SerialNo;
                var veh = await _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>().FirstOrDefaultAsync(v => v.Id == model.Id);
                if (veh != null) veh.PlateNo = model.PlateNo ?? model.SerialNo;
                var land = await _db.PsCardItemExtns.OfType<PsCardItemExtnLand>().FirstOrDefaultAsync(l => l.Id == model.Id);
                if (land != null && !string.IsNullOrWhiteSpace(model.SerialNo)) land.PIN = model.SerialNo;
                var bldg = await _db.PsCardItemExtns.OfType<PsCardItemExtnBuilding>().FirstOrDefaultAsync(b => b.Id == model.Id);
                if (bldg != null && !string.IsNullOrWhiteSpace(model.SerialNo)) bldg.BuildingItem = model.SerialNo;
            }

            //existing.Condition = model.Condition;
            //existing.LocationId = model.LocationId;
            existing.AcqCost = model.AcquisitionCost;
            existing.Remarks = model.Remarks;
            existing.PsCardSubItemId = null; // Always force null!
            existing.UpdatedBy = userName;
            existing.UpdatedDt = DateTime.Now;

            await _db.SaveChangesAsync();

            model.CanEdit = !isAccountable;
            model.CanDelete = !isAccountable && existing.AIRItemExtnId == null;

            return true;
        }

        public async Task<bool> DestroyUnitAsync(Guid cardId, PropertyCardUnitVM model, string userName, ModelStateDictionary modelState)
        {
            var existing = await _db.PsCardItemExtns
                .Include(e => e.PsCardItem.PsCard)
                .FirstOrDefaultAsync(e => e.Id == model.Id && e.PsCardItem.PsCardId == cardId && e.PsCardSubItemId == null);

            if (existing == null)
            {
                modelState.AddModelError("", "Physical unit not found.");
                return false;
            }

            if (existing.PsCardItem.PsCard.PostedDt != null)
            {
                modelState.AddModelError("", "Cannot delete units from a posted Property Card.");
                return false;
            }

            if (existing.PsCardItem.PostedDt != null)
            {
                modelState.AddModelError("", "This acquisition is posted. Individual units are read-only.");
                return false;
            }

            // Downstream accountability check: IcsParItem
            var icsPar = await (from i in _db.IcsParItems
                                where i.PsCardItemExtnId == model.Id
                                select new { i.IcsPar.RefType, i.IcsPar.RefNo })
                               .FirstOrDefaultAsync();
            if (icsPar != null)
            {
                string docType = (icsPar.RefType == "P" || icsPar.RefType == "PAR") ? "PAR" : "ICS";
                modelState.AddModelError("", $"This physical unit cannot be deleted because it is already referenced by {docType} No. {icsPar.RefNo}.");
                return false;
            }

            // Downstream check: IcsParItemComponents
            var compIcs = await (from c in _db.IcsParItemComponents
                                 where c.PsCardItemExtnId == model.Id
                                 select new { c.IcsParItem.IcsPar.RefType, c.IcsParItem.IcsPar.RefNo })
                                .FirstOrDefaultAsync();
            if (compIcs != null)
            {
                string docType = (compIcs.RefType == "P" || compIcs.RefType == "PAR") ? "PAR" : "ICS";
                modelState.AddModelError("", $"This physical unit cannot be deleted because it is referenced in an accountability bundle ({docType} No. {compIcs.RefNo}).");
                return false;
            }

            // Downstream check: transfers
            bool isTransferred = await _db.PsCardItemTransferItems.AnyAsync(t => t.PsCardItemExtnId == model.Id && t.PsCardItemTransfer.ParentId != null);
            if (isTransferred)
            {
                modelState.AddModelError("", "This physical unit cannot be deleted because it has downstream transfer records.");
                return false;
            }

            // Downstream check: issuances
            bool isIssued = await _db.PsCardItemTransferItems.AnyAsync(t => t.PsCardItemExtnId == model.Id && t.PsCardItemTransferIssuanceItems.Any());
            if (isIssued)
            {
                modelState.AddModelError("", "This physical unit cannot be deleted because it has issuance records.");
                return false;
            }

            // AIR protection
            if (existing.AIRItemExtnId != null)
            {
                modelState.AddModelError("", "This physical unit cannot be deleted because it originated from an authoritative AIR (Acceptance and Inspection Report) record.");
                return false;
            }

            // Clean up bridge records
            var bridge = await _db.PsCardItemTransferItems.Where(t => t.PsCardItemExtnId == model.Id).ToListAsync();
            if (bridge.Any())
            {
                _db.PsCardItemTransferItems.RemoveRange(bridge);
            }

            _db.PsCardItemExtns.Remove(existing);
            await _db.SaveChangesAsync();

            return true;
        }

        public PropertyCardUnitAccountabilityVM GetUnitAccountabilityDetails(Guid cardId, Guid unitId)
        {
            var unit = _db.PsCardItemExtns
                .Include(u => u.PsCardItem.PsCard)
                .Include(u => u.Codextn)
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == unitId && u.PsCardItem.PsCardId == cardId && u.PsCardSubItemId == null);

            if (unit == null) return null;

            var other = _db.PsCardItemExtns.OfType<PsCardItemExtnOther>()
                .Where(o => o.Id == unitId).Select(o => o.SerialNo).FirstOrDefault();
            var veh = _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>()
                .Where(v => v.Id == unitId).Select(v => v.MVFileNo ?? v.ConductionNo).FirstOrDefault();
            string serialOrPlate = !string.IsNullOrWhiteSpace(other) ? other : (!string.IsNullOrWhiteSpace(veh) ? veh : unit.SeriesNo);

            var result = new PropertyCardUnitAccountabilityVM
            {
                UnitId = unit.Id,
                PsCardId = cardId,
                PsCardItemId = unit.PsCardItemId ?? Guid.Empty,
                PropertyNo = unit.PropNo,
                CustItemNo = unit.CustItemNo,
                SerialOrPlateNo = serialOrPlate,
                Description = unit.PsCardItem.Description,
                ContentNo = unit.ContentNo,
                PoNo = unit.PsCardItem.PoNo,
                Remarks = unit.Remarks,
                Location = unit.Codextn != null ? unit.Codextn.Description : null,
                Condition = unit.Condition,
                AcquisitionCost = unit.AcqCost ?? unit.PsCardItem.UnitCost
            };

            // Query all IcsParItem records for this unit
            var items = _db.IcsParItems
                .Include(i => i.IcsPar)
                .Include(i => i.IcsParItemComponents)
                .AsNoTracking()
                .Where(i => i.PsCardItemExtnId == unitId)
                .ToList();

            if (!items.Any())
            {
                result.HasAccountability = false;
                result.DerivedStatus = "AVAILABLE";
                return result;
            }

            result.HasAccountability = true;

            // Build dictionary of items
            var byId = items.ToDictionary(i => i.Id);

            // Find root (item with no PrevItemId or PrevItemId not in items)
            var root = items.FirstOrDefault(i => !i.PrevItemId.HasValue || !byId.ContainsKey(i.PrevItemId.Value)) ?? items.First();

            // Trace backwards to ensure earliest root
            var ancestorGuard = new HashSet<Guid>();
            while (root.PrevItemId.HasValue && byId.ContainsKey(root.PrevItemId.Value) && ancestorGuard.Add(root.Id))
            {
                root = byId[root.PrevItemId.Value];
            }

            // Trace forward to build history and find current
            var history = new List<IcsParAccountabilityHistoryVM>();
            var visited = new HashSet<Guid>();
            var current = root;

            IcsParItem currentAccountableItem = null;
            IcsParItem previousAccountableItem = null;
            IcsParItem draftSuccessorItem = null;

            while (current != null && visited.Add(current.Id))
            {
                var postedSuccessors = items
                    .Where(i => i.PrevItemId == current.Id && i.IcsPar != null && i.IcsPar.PostedDt.HasValue)
                    .OrderBy(i => i.IcsPar.PostedDt)
                    .ThenBy(i => i.IcsPar.RefDate)
                    .ThenBy(i => i.Id)
                    .ToList();

                var draftSuccessors = items
                    .Where(i => i.PrevItemId == current.Id && (i.IcsPar == null || !i.IcsPar.PostedDt.HasValue))
                    .OrderBy(i => i.IcsPar == null ? null : i.IcsPar.RefDate)
                    .ThenBy(i => i.InsertedDt)
                    .ThenBy(i => i.Id)
                    .ToList();

                var hasPostedSuccessor = postedSuccessors.Any();
                var hasDraftTransfer = draftSuccessors.Any();
                var isPosted = current.IcsPar != null && current.IcsPar.PostedDt.HasValue;
                var isCurrent = isPosted && !hasPostedSuccessor;

                if (isCurrent)
                {
                    currentAccountableItem = current;
                    if (hasDraftTransfer)
                    {
                        draftSuccessorItem = draftSuccessors.FirstOrDefault();
                    }
                }

                history.Add(new IcsParAccountabilityHistoryVM
                {
                    Sequence = history.Count + 1,
                    IcsParItemId = current.Id,
                    PrevItemId = current.PrevItemId,
                    PsCardItemExtnId = current.PsCardItemExtnId,
                    RefNo = current.IcsPar == null ? null : current.IcsPar.RefNo,
                    RefDate = current.IcsPar == null ? null : current.IcsPar.RefDate,
                    RefType = current.IcsPar == null ? null : (current.IcsPar.RefType == "P" ? "PAR" : (current.IcsPar.RefType == "I" ? "ICS" : current.IcsPar.RefType)),
                    AccountableOfficer = !string.IsNullOrWhiteSpace(current.IssuedTo) ? current.IssuedTo : (current.IcsPar == null ? null : current.IcsPar.ReceivedBy),
                    AccountableOfficerPosition = !string.IsNullOrWhiteSpace(current.Designation) ? current.Designation : (current.IcsPar == null ? null : current.IcsPar.ReceivedByPosition),
                    AccountableOfficerDepartment = current.IcsPar == null ? null : current.IcsPar.ReceivedDept,
                    PostedBy = current.IcsPar == null ? null : current.IcsPar.PostedBy,
                    PostedDt = current.IcsPar == null ? null : current.IcsPar.PostedDt,
                    HasDraftTransfer = hasDraftTransfer,
                    HasPostedSuccessor = hasPostedSuccessor,
                    IsCurrent = isCurrent,
                    ComponentCount = current.IcsParItemComponents.Count,
                    TransferStatus = !isPosted ? "DRAFT TRANSFER" : (hasPostedSuccessor ? "TRANSFERRED" : "CURRENT")
                });

                current = postedSuccessors.FirstOrDefault() ?? draftSuccessors.FirstOrDefault();
            }

            result.History = history;

            // If no posted item was marked current (e.g. only draft exists)
            if (currentAccountableItem == null)
            {
                currentAccountableItem = history.OrderByDescending(h => h.RefDate).ThenByDescending(h => h.Sequence)
                    .Select(h => byId[h.IcsParItemId]).FirstOrDefault() ?? items.First();
            }

            // Previous officer: look at the predecessor of currentAccountableItem
            if (currentAccountableItem.PrevItemId.HasValue && byId.ContainsKey(currentAccountableItem.PrevItemId.Value))
            {
                previousAccountableItem = byId[currentAccountableItem.PrevItemId.Value];
            }

            var rootOfficer = !string.IsNullOrWhiteSpace(root.IssuedTo) ? root.IssuedTo : (root.IcsPar != null ? root.IcsPar.ReceivedBy : null);
            var prevOfficer = previousAccountableItem != null
                ? (!string.IsNullOrWhiteSpace(previousAccountableItem.IssuedTo) ? previousAccountableItem.IssuedTo : (previousAccountableItem.IcsPar != null ? previousAccountableItem.IcsPar.ReceivedBy : null))
                : null;
            var currOfficer = !string.IsNullOrWhiteSpace(currentAccountableItem.IssuedTo) ? currentAccountableItem.IssuedTo : (currentAccountableItem.IcsPar != null ? currentAccountableItem.IcsPar.ReceivedBy : null);

            var currPar = currentAccountableItem.IcsPar;
            bool isCurrentPosted = currPar != null && currPar.PostedDt.HasValue;
            bool hasDraftSuccessorOnCurrent = draftSuccessorItem != null;

            result.IcsParItemId = currentAccountableItem.Id;
            result.IcsParId = currPar != null ? (Guid?)currPar.Id : null;
            result.RefType = currPar != null ? (currPar.RefType == "P" ? "PAR" : (currPar.RefType == "I" ? "ICS" : currPar.RefType)) : null;
            result.RefNo = currPar != null ? currPar.RefNo : null;
            result.RefDate = currPar != null ? currPar.RefDate : null;
            result.CurrentOfficer = currOfficer;
            result.CurrentPosition = !string.IsNullOrWhiteSpace(currentAccountableItem.Designation) ? currentAccountableItem.Designation : (currPar != null ? currPar.ReceivedByPosition : null);
            result.CurrentDepartment = currPar != null ? currPar.ReceivedDept : null;
            result.CurrentLocation = currPar != null ? (currPar.Location ?? currPar.LocationCode) : null;
            result.OriginalOfficer = rootOfficer;
            result.PreviousOfficer = prevOfficer;
            result.PostedBy = currPar != null ? currPar.PostedBy : null;
            result.PostedDt = currPar != null ? currPar.PostedDt : null;
            result.IsPosted = isCurrentPosted;
            result.HasDraftSuccessor = hasDraftSuccessorOnCurrent;
            result.DraftSuccessorRefNo = draftSuccessorItem != null && draftSuccessorItem.IcsPar != null ? draftSuccessorItem.IcsPar.RefNo : null;

            if (!isCurrentPosted)
            {
                result.DerivedStatus = (result.RefType == "PAR") ? "PAR DRAFT" : "ICS DRAFT";
                result.TransferStatus = "DRAFT";
            }
            else if (hasDraftSuccessorOnCurrent)
            {
                result.DerivedStatus = "TRANSFER PENDING";
                result.TransferStatus = "TRANSFER PENDING";
            }
            else
            {
                result.DerivedStatus = (result.RefType == "PAR") ? "PAR POSTED" : "ICS POSTED";
                result.TransferStatus = "CURRENT";
            }

            // Fetch active component bundle for currentAccountableItem
            var currentItemId = currentAccountableItem.Id;
            var components = (from c in _db.IcsParItemComponents.AsNoTracking()
                              where c.IcsParItemId == currentItemId
                              join sub in _db.PsCardSubItems.AsNoTracking() on c.PsCardSubItemId equals sub.Id
                              join extn in _db.PsCardItemExtns.AsNoTracking() on c.PsCardItemExtnId equals extn.Id into extnGroup
                              from extn in extnGroup.DefaultIfEmpty()
                              select new
                              {
                                  c.Id,
                                  c.PsCardSubItemId,
                                  c.PsCardItemExtnId,
                                  sub.SubItemNo,
                                  sub.Description,
                                  extnPropNo = extn != null ? extn.PropNo : null,
                                  extnCustItemNo = extn != null ? extn.CustItemNo : null,
                                  extnContentNo = extn != null ? extn.ContentNo : (int?)null,
                                  extnSeriesNo = extn != null ? extn.SeriesNo : null,
                                  c.Qty,
                                  sub.Unit,
                                  sub.SourceType,
                                  IsRequired = sub.IsRequiredForBundle == true,
                                  c.Remarks
                              }).ToList();

            var bundleList = new List<PropertyCardBundleItemVM>();
            foreach (var comp in components)
            {
                string compSerial = null;
                if (comp.PsCardItemExtnId.HasValue)
                {
                    var compOther = _db.PsCardItemExtns.OfType<PsCardItemExtnOther>()
                        .Where(o => o.Id == comp.PsCardItemExtnId.Value).Select(o => o.SerialNo).FirstOrDefault();
                    var compVeh = _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>()
                        .Where(v => v.Id == comp.PsCardItemExtnId.Value).Select(v => v.PlateNo ?? v.ConductionNo).FirstOrDefault();
                    compSerial = !string.IsNullOrWhiteSpace(compOther) ? compOther : (!string.IsNullOrWhiteSpace(compVeh) ? compVeh : comp.extnSeriesNo);
                }

                bundleList.Add(new PropertyCardBundleItemVM
                {
                    Id = comp.Id,
                    PsCardSubItemId = comp.PsCardSubItemId,
                    PsCardItemExtnId = comp.PsCardItemExtnId,
                    SubItemNo = comp.SubItemNo,
                    Description = comp.Description,
                    PhysicalUnitCode = comp.PsCardItemExtnId.HasValue
                        ? (comp.extnPropNo ?? comp.extnCustItemNo ?? (comp.extnContentNo.HasValue ? ("Unit #" + comp.extnContentNo.Value) : "Individually Tracked"))
                        : "Quantity Allocated",
                    SerialNo = compSerial,
                    Qty = comp.Qty,
                    Unit = comp.Unit,
                    SourceType = comp.SourceType,
                    IsRequiredForBundle = comp.IsRequired,
                    IsIndividuallyTracked = comp.PsCardItemExtnId.HasValue,
                    Remarks = comp.Remarks
                });
            }

            result.Components = bundleList.OrderBy(b => b.Description).ThenBy(b => b.PhysicalUnitCode).ToList();
            return result;
        }
    }
}
