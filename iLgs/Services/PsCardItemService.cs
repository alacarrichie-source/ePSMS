using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.CategoryEnum;

namespace iLgs.Services
{
    public interface IPsCardItemService
    {
        IQueryable<PsCardItemVM> GetByCardId(Guid? cardId);
        ValueTask<PsCardItemVM> GetByIdAsync(Guid? id);
        ValueTask<string> GetCategoryAsync(Guid? psCardItemId);

        ValueTask<PsCardItemVM> CreateAsync(PsCardItemVM model, string user, DateTime date);
        ValueTask<PsCardItemVM> UpdateAsync(PsCardItemVM model, string user, DateTime date);
        ValueTask<ParIcsItemVm> UpdateIsForICSAsync(ParIcsItemVm model, string user, DateTime date);
        ValueTask<ParIcsItemVm> UpdateNoICSAsync(ParIcsItemVm model, string user, DateTime date);
        ValueTask<PsCardItemVM> DeleteAsync(PsCardItemVM model, string user, DateTime date);
        PsCardItemVM TransferItemField(PsCardItemVM sourceModel, PsCardItemVM targetModel);
        IPsCardItemExtnService PsCardItemExtn { get; }
    }

    public class PsCardItemService : IPsCardItemService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly IExceptionService<PsCardItemVM> _VmExceptionService = new ExceptionService<PsCardItemVM>();
        private readonly IExceptionService<ParIcsItemVm> _parIcsItemExceptionService = new ExceptionService<ParIcsItemVm>();
        private IPsCardItemExtnService _psCardItemExtnService;

        public PsCardItemService(AppManEntities db)
        {
            _db = db;
            _psCardItemExtnService = new PsCardItemExtnService(db);
        }

        public IPsCardItemExtnService PsCardItemExtn { get { return _psCardItemExtnService = _psCardItemExtnService ?? new PsCardItemExtnService(_db); } }

        public async ValueTask<PsCardItemVM> GetByIdAsync(Guid? id)
        {
            var data = await _db.PsCardItems
                .Include(i => i.Codextn) // Department
                .Include(i => i.Codextn1) // Location
                .Where(w => w.Id == id)
                .Select(s => new PsCardItemVM
                {
                    Id = s.Id,
                    PsCardId = s.PsCardId,
                    OrderItemId = s.OrderItemId,
                    PoDate = s.PoDate,
                    PoNo = s.PoNo,
                    AirDate = s.AirDate,
                    AirNo = s.AirNo,
                    AirIssueDate = s.AirIssueDate,
                    Qty = s.Qty,
                    QtyIss = s.QtyIss,
                    QtyBal = s.QtyBal,
                    TransferIn = s.TransferIn,
                    TransferOut = s.TransferOut,
                    TranType = s.TranType,
                    Days = s.Days,
                    Unit = s.Unit,
                    UnitCost = s.UnitCost,
                    Amount = s.Amount,
                    PriceRate = s.PriceRate,
                    Remarks = s.Remarks,
                    DeptId = s.DeptId,
                    LocationId = s.LocationId,
                    DeptDisplay = s.DeptDisplay,
                    Description = s.Description,
                    OtherDesc = s.OtherDesc,
                    IsForICS = s.IsForICS,
                    IsConsumable = s.IsConsumable,
                    IsIncorporated = s.IsIncorporated,
                    IsOthers = s.IsOthers,
                    OtherRemarks = s.OtherRemarks,
                    Type = s.Type,
                    InvDist = s.InvDist,
                    AcqMode = s.AcqMode,
                    AcqDate = s.AcqDate,
                    AreaSoldDonated = s.AreaSoldDonated,
                    ConstructionYear = s.ConstructionYear,
                    Vendor = s.Vendor,
                    OldAmount = s.OldAmount,
                    PhaseNo = s.PhaseNo,
                    PhaseAmount = s.PhaseAmount,
                    InsertedDt = s.InsertedDt,
                    Department = s.Codextn.Description,
                    Location = s.Codextn1.Description,
                    LocCode = s.Codextn1.Code,
                    PrevPsNo = s.PrevPsNo
                }).FirstOrDefaultAsync();
            return data;
        }

        public IQueryable<PsCardItemVM> GetByCardId(Guid? cardId) => _VmExceptionService.TryCatch(() =>
        {
            var data = _db.PsCardItems
                .Include(i => i.Codextn) // Department
                .Include(i => i.Codextn1) // Location
                .Where(w => w.PsCardId == cardId).AsNoTracking()
                .Select(s => new PsCardItemVM
                {
                    Id = s.Id,
                    PsCardId = s.PsCardId,
                    OrderItemId = s.OrderItemId,
                    PoDate = s.PoDate,
                    PoNo = s.PoNo,
                    AirDate = s.AirDate,
                    AirNo = s.AirNo,
                    AirIssueDate = s.AirIssueDate,
                    Qty = s.Qty,
                    QtyIss = s.QtyIss,
                    QtyBal = s.QtyBal,
                    TransferIn = s.TransferIn,
                    TransferOut = s.TransferOut,
                    TranType = s.TranType,
                    Days = s.Days,
                    Unit = s.Unit,
                    UnitCost = s.UnitCost,
                    Amount = s.Amount,
                    PriceRate = s.PriceRate,
                    Remarks = s.Remarks,
                    DeptId = s.DeptId,
                    LocationId = s.LocationId,
                    DeptDisplay = s.DeptDisplay,
                    Description = s.Description,
                    OtherDesc = s.OtherDesc,
                    IsForICS = s.IsForICS,
                    IsConsumable = s.IsConsumable,
                    IsIncorporated = s.IsIncorporated,
                    IsOthers = s.IsOthers,
                    OtherRemarks = s.OtherRemarks,
                    Type = s.Type,
                    InvDist = s.InvDist,
                    AcqMode = s.AcqMode,
                    AcqDate = s.AcqDate,
                    AreaSoldDonated = s.AreaSoldDonated,
                    ConstructionYear = s.ConstructionYear,
                    Vendor = s.Vendor,
                    OldAmount = s.OldAmount,
                    PhaseNo = s.PhaseNo,
                    PhaseAmount = s.PhaseAmount,
                    InsertedDt = s.InsertedDt,
                    Department = s.Codextn.Description,
                    Location = s.Codextn1.Description,
                    LocCode = s.Codextn1.Code,
                    PrevPsNo = s.PrevPsNo
                });
            return data;
        });

        public async ValueTask<string> GetCategoryAsync(Guid? psCardItemId)
        {
            return await _db.PsCardItems.Where(w => w.Id == psCardItemId).Select(s => s.PsCard.ItemCode.ItemType.Code).FirstOrDefaultAsync();
        }

        private void ValidateFields(PsCardItemVM model)
        {
            if (!model.Qty.HasValue && !model.TransferIn.HasValue)
            {
                throw new InvalidValueException("Quantity or Transfer-In is Required!");
            }
            
            var category = _db.PsCards.Where(w => w.Id == model.PsCardId).Select(s => s.ItemCode.ItemType.Code).FirstOrDefault();
            if (Enum.TryParse(category, out Category c))
            {
                if (model.DeptId == null)
                {
                    if (c != CatLands() && c != CatBuildings() && c != CatLandImprovements() && c != CatInfrastructures() && c != CatOtherProperties())
                    {
                        throw new InvalidValueException("Department is Required!");
                    }
                }

                if (model.LocationId == null)
                {
                    if (c == CatLands() || c == CatBuildings() || c == CatLandImprovements() || c == CatInfrastructures() || c == CatOtherProperties())
                    {
                        throw new InvalidValueException("Location is Required!");
                    }
                }
            }
        }

        public ValueTask<PsCardItemVM> CreateAsync(PsCardItemVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            ValidateFields(model);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new PsCardItem
            {
                Id = model.Id,
                PsCardId = model.PsCardId,
                OrderItemId = model.OrderItemId,
                PoDate = model.PoDate,
                PoNo = model.PoNo,
                AirDate = model.AirDate,
                AirNo = model.AirNo,
                AirIssueDate = model.AirIssueDate,
                Qty = model.Qty,
                QtyIss = model.QtyIss,
                QtyBal = model.QtyBal,
                TransferIn = model.TransferIn,
                TransferOut = model.TransferOut,
                Days = model.Days,
                TranType = model.TranType,
                Unit = model.Unit,
                UnitCost = model.UnitCost,
                Remarks = model.Remarks,
                Amount = model.Amount,
                PriceRate = model.PriceRate,
                DeptId = model.DeptId,
                LocationId = model.LocationId,
                Description = model.Description,
                OtherDesc = model.OtherDesc,
                DeptDisplay = model.DeptDisplay,
                Type = model.Type,
                InvDist = model.InvDist,
                AcqDate = model.AcqDate,
                AcqMode = model.AcqMode,
                AreaSoldDonated = model.AreaSoldDonated,
                ConstructionYear = model.ConstructionYear,
                Vendor = model.Vendor,
                OldAmount = model.OldAmount,
                PhaseNo = model.PhaseNo,
                PhaseAmount = model.PhaseAmount,
                PrevPsNo = model.PrevPsNo,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.PsCardItems.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<PsCardItemVM> UpdateAsync(PsCardItemVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            ValidateFields(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItems.FindAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            entity.PsCardId = model.PsCardId;
            entity.OrderItemId = model.OrderItemId;
            entity.PoDate = model.PoDate;
            entity.PoNo = model.PoNo;
            entity.AirDate = model.AirDate;
            entity.AirNo = model.AirNo;
            entity.AirIssueDate = model.AirIssueDate;
            entity.Qty = model.Qty;
            entity.QtyIss = model.QtyIss;
            entity.QtyBal = model.QtyBal;
            entity.TransferIn = model.TransferIn;
            entity.TransferOut = model.TransferOut;
            entity.Days = model.Days;
            entity.TranType = model.TranType;
            entity.Unit = model.Unit;
            entity.UnitCost = model.UnitCost;
            entity.Remarks = model.Remarks;
            entity.Amount = model.Amount;
            entity.PriceRate = model.PriceRate;
            entity.DeptId = model.DeptId;
            entity.LocationId = model.LocationId;
            entity.Description = model.Description;
            entity.OtherDesc = model.OtherDesc;
            entity.DeptDisplay = model.DeptDisplay;
            entity.Type = model.Type;
            entity.InvDist = model.InvDist;
            entity.AcqDate = model.AcqDate;
            entity.AcqMode = model.AcqMode;
            entity.AreaSoldDonated = model.AreaSoldDonated;
            entity.ConstructionYear = model.ConstructionYear;
            entity.Vendor = model.Vendor;
            entity.OldAmount = model.OldAmount;
            entity.PhaseNo = model.PhaseNo;
            entity.PhaseAmount = model.PhaseAmount;
            entity.PrevPsNo = model.PrevPsNo;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        //PsCardItemUnitGroupDescriptionItem

        public ValueTask<ParIcsItemVm> UpdateIsForICSAsync(ParIcsItemVm model, string user, DateTime date) => _parIcsItemExceptionService.TryCatch(async () =>
        {
            var entity = await _db.PsCardItems.FindAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (entity.IsForICS != model.IsForICS) // change in isForICS
            {
                var icsParItem = await _db.IcsParItems.Include(i => i.IcsPar).FirstOrDefaultAsync(f => f.PsCardItemExtn.PsCardItemId == model.Id);
                if (icsParItem != null)
                {
                    if (model.IsForICS == true && icsParItem.IcsPar.RefType == "P")
                    {
                        throw new InvalidValueException("Item with PAR already exists, cannot make this as For ICS.");
                    }
                    else
                    {
                        if (model.IsForICS == false && icsParItem.IcsPar.RefType == "I")
                        {
                            throw new InvalidValueException("Item with ICS already exists, cannot remove this as For ICS.");
                        }
                    }
                }
            }

            entity.IsForICS = model.IsForICS;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PsCardItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });


        public ValueTask<ParIcsItemVm> UpdateNoICSAsync(ParIcsItemVm model, string user, DateTime date) => _parIcsItemExceptionService.TryCatch(async () =>
        {
            if (model.IsOthers == true && string.IsNullOrWhiteSpace(model.OtherRemarks))
            {
                throw new InvalidValueException("Remarks field is required if Others is selected!");
            }

            var entity = await _db.PsCardItems.FindAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (entity.IsConsumable != model.IsConsumable || entity.IsIncorporated != model.IsIncorporated
                || entity.IsOthers != model.IsOthers) // change in decision
            {
                var icsParItem = await _db.IcsParItems.Include(i => i.IcsPar).FirstOrDefaultAsync(f => f.PsCardItemExtn.PsCardItemId == model.Id);
                if (icsParItem != null)
                {
                    if ((model.IsConsumable == true || model.IsIncorporated == true || model.IsOthers == true) && icsParItem.IcsPar.RefType == "P")
                    {
                        throw new InvalidValueException("Item with PAR already exists, cannot make this as For ICS.");
                    }
                    else
                    {
                        if ((model.IsConsumable == false || model.IsIncorporated == false || model.IsOthers == false) && icsParItem.IcsPar.RefType == "I")
                        {
                            throw new InvalidValueException("Item with ICS already exists, cannot remove this as For ICS.");
                        }
                    }
                }
            }

            entity.IsConsumable = model.IsConsumable;
            entity.IsIncorporated = model.IsIncorporated;
            entity.IsOthers = model.IsOthers;
            entity.OtherRemarks = model.IsOthers == true ? model.OtherRemarks : "";
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PsCardItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });


        public ValueTask<PsCardItemVM> DeleteAsync(PsCardItemVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            if (_db.PsCardItems.Any(a => a.Id == model.Id && a.PsCardItemTransfers.Any()))
            {
                throw new RecordRelationshipException("Items of this record was transfered to other department/location, cannot delete!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItems.FindAsync(model.Id);

            // Get transfer record if any
            PsCardItemTransfer psCardItemTransfer = null;
            if (entity.TransferRefId != null)
            {
                psCardItemTransfer = await _db.PsCardItemTransfers.FindAsync(entity.TransferRefId);
            }
            
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCardItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            // put back transferred items
            if (psCardItemTransfer != null)
            {
                var psCardItem = await _db.PsCardItems.FindAsync(psCardItemTransfer.PsCardItemId);
                if (psCardItem != null)
                {
                    psCardItem.TransferOut -= (psCardItemTransfer.Qty ?? 0);
                    psCardItem.QtyBal = ((psCardItem.Qty ?? 0) + (psCardItem.TransferIn ?? 0)) - ((psCardItem.TransferOut ?? 0) + (psCardItem.QtyIss ?? 0));
                    psCardItem.UpdatedBy = user;
                    psCardItem.UpdatedDt = date;

                    _db.PsCardItems.Attach(psCardItem);
                    _db.Entry(psCardItem).State = EntityState.Modified;
                    await _db.SaveChangesAsync();

                    // delete transfer record
                    psCardItemTransfer.UpdatedBy = user;
                    psCardItemTransfer.UpdatedDt = date;
                    _db.PsCardItemTransfers.Attach(psCardItemTransfer);
                    _db.Entry(psCardItemTransfer).State = EntityState.Modified;
                    await _db.SaveChangesAsync();

                    _db.PsCardItemTransfers.Remove(psCardItemTransfer);
                    _db.Entry(psCardItemTransfer).State = EntityState.Deleted;
                    await _db.SaveChangesAsync();
                }
            }

            return model;
        });

        public PsCardItemVM TransferItemField(PsCardItemVM sourceModel, PsCardItemVM targetModel)
        {
            targetModel.Type = sourceModel.Type;
            targetModel.AcqDate = sourceModel.AcqDate;
            targetModel.AcqMode = sourceModel.AcqMode;
            targetModel.InvDist = sourceModel.InvDist;
            targetModel.AreaSoldDonated = sourceModel.AreaSoldDonated;
            targetModel.ConstructionYear = sourceModel.ConstructionYear;
            targetModel.Vendor = sourceModel.Vendor;
            return targetModel;
        }
    }
}