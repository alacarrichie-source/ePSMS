using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.PropertyCard
{
    public interface IPsCardItemTransferIssuanceService
    {
        IQueryable<PsCardItemTransferIssuanceVM> GetByTransferId(Guid? transferId);
        ValueTask<PsCardItemTransferIssuanceVM> GetByIdAsync(Guid? id);

        ValueTask<PsCardItemTransferIssuanceVM> CreateAsync(PsCardItemTransferIssuanceVM model, string user, DateTime date);
        ValueTask<PsCardItemTransferIssuanceVM> UpdateAsync(PsCardItemTransferIssuanceVM model, string user, DateTime date);
        ValueTask<PsCardItemTransferIssuanceVM> DeleteAsync(PsCardItemTransferIssuanceVM model, string user, DateTime date);

        IQueryable<T> GetCardItemExtnForIssuance<T>(Guid? transferId) where T : PsCardItemExtn;
        IQueryable<T> GetCardItemExtnForIssuanceSelection<T>(Guid? transferId) where T : PsCardItemExtn;

        IQueryable<PsCardItemExtn> GetCardItemExtnForIssuanceByType(Guid? transferId);

        IQueryable<PsCardItemExtn> GetCardItemExtnForOtherIssuanceSelection(Guid? transferId);
        IQueryable<PsCardItemExtn> GetCardItemExtnForVehicleIssuanceSelection(Guid? transferId);
        IQueryable<PsCardItemExtn> GetCardItemExtnForLandIssuanceSelection(Guid? transferId);
        IQueryable<PsCardItemExtn> GetCardItemExtnForBldgIssuanceSelection(Guid? transferId);
    }

    public class PsCardItemTransferIssuanceService : BaseValidator, IPsCardItemTransferIssuanceService
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly IExceptionService<PsCardItemTransferIssuanceVM> _vmExceptionService;
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;
        private readonly IPsCardItemExtnService _psCardItemExtnService;
        private readonly ICodextnService _codextnService;
        private readonly IPsCardSharedService _psCardSharedService;

        public PsCardItemTransferIssuanceService(AppManEntities db,
            IExceptionService<PsCardItemTransferIssuanceVM> vmExceptionService,
            IPsCardItemTransactionService psCardItemTransactionService,
            IPsCardItemExtnService psCardItemExtnService,
            ICodextnService codextnService,
            IPsCardSharedService psCardSharedService)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<PsCardItemTransferIssuanceVM>(propertyName);
            _vmExceptionService = vmExceptionService;
            _psCardItemTransactionService = psCardItemTransactionService;
            _psCardItemExtnService = psCardItemExtnService;
            _codextnService = codextnService;
            _psCardSharedService = psCardSharedService;
        }

        private Expression<Func<PsCardItemTransferIssuance, PsCardItemTransferIssuanceVM>> GetProjection()
        {
            return s => new PsCardItemTransferIssuanceVM
            {
                Id = s.Id,
                PsCardItemTransferId = s.PsCardItemTransferId,
                LocationId = s.LocationId,
                DeptId = s.DeptId,
                IssuedToCode = s.IssuedToCode,
                IssuedToDescription = s.IssuedToDescription,
                IssuedDate = s.IssuedDate,
                Qty = s.Qty,
                UnitCost = s.PsCardItemTransfer.PsCardItem.TUnitCost,
                Amount = s.Amount,
                InsertedDt = s.InsertedDt
            };
        }

        public ValueTask<PsCardItemTransferIssuanceVM> GetByIdAsync(Guid? id) => _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.PsCardItemTransferIssuances.Where(w => w.Id == id).AsNoTracking()
                .Select(GetProjection()).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<PsCardItemTransferIssuanceVM> GetByTransferId(Guid? transferId) => _vmExceptionService.TryCatch(() =>
        {
            var data = _db.PsCardItemTransferIssuances.Where(w => w.PsCardItemTransferId == transferId).AsNoTracking()
                .Select(GetProjection());                
            return data;
        });
        
        public ValueTask<PsCardItemTransferIssuanceVM> CreateAsync(PsCardItemTransferIssuanceVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            await ValidateFieldsAsync(model, Mode.ADD);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            if (model.SelectedIds != null)
            {
                string[] selectedIds = model.SelectedIds.Split(',');

                if (selectedIds.Count() == 0)
                {
                    throw new InvalidValueException(string.Format("No selected items, cannot continue!"));
                }

                model.Qty = selectedIds.Count();
                var psCardItemTransferIssuance = await CreatePsCardItemTransferIssuanceAsync(model);

                foreach (var selectedId in selectedIds)
                {
                    var psCardItemExtnId = Guid.Parse(selectedId);
                    var psCardItemTransferItem = _db.PsCardItemTransferItems
                        .FirstOrDefault(f => f.PsCardItemExtnId == psCardItemExtnId 
                            && f.PsCardItemTransferId == model.PsCardItemTransferId);
                    var psCardItemTransferIssuanceItem = new PsCardItemTransferIssuanceItem()
                    {
                        Id = Guid.NewGuid(),
                        PsCardItemTransferIssuanceId = psCardItemTransferIssuance.Id,
                        PsCardItemTransferItemId = psCardItemTransferItem.Id,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };
                    _db.PsCardItemTransferIssuanceItems.Add(psCardItemTransferIssuanceItem);
                }
                await _db.SaveChangesAsync();
            }
            else
            {
                var psCardItemIssuance = await CreatePsCardItemTransferIssuanceAsync(model);
            }

            await UpdatePsItems(model.PsCardItemTransferId, user, date);

            return model;
        });

        public async Task<PsCardItemTransferIssuance> CreatePsCardItemTransferIssuanceAsync(PsCardItemTransferIssuanceVM model)
        {
            var entity = new PsCardItemTransferIssuance();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.PsCardItemTransferIssuances.Add(entity);
            await _db.SaveChangesAsync();

            return entity;
        }

        public ValueTask<PsCardItemTransferIssuanceVM> UpdateAsync(PsCardItemTransferIssuanceVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            var entity = await _db.PsCardItemTransferIssuances.Include(i => i.PsCardItemTransfer).Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            ValidateRecord(entity, model.Id);

            
            if (_db.RSMIs.Any(a => a.Date == entity.IssuedDate))
            {
                throw new RecordAlreadyExistsException($"RSMI already exists for the saved date, {entity.IssuedDate.Value.ToShortDateString()}, cannot update!");
            }

            await ValidateFieldsAsync(model, Mode.EDIT);

            var qtyBalance = (_db.PsCardItemTransfers.Find(model.PsCardItemTransferId)?.QtyBal ?? 0) + entity.Qty;
            if (model.Qty > qtyBalance)
            {
                throw new InvalidValueException(string.Format("Quantity must not exceed the remaing balance of {0}", qtyBalance));
            }            

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            MapModelToEntityFields(entity, model, Mode.EDIT);

            _db.PsCardItemTransferIssuances.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            await UpdatePsItems(model.PsCardItemTransferId, user, date);

            return model;
        });

        public void MapModelToEntityFields(PsCardItemTransferIssuance entity, PsCardItemTransferIssuanceVM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }
            entity.PsCardItemTransferId = model.PsCardItemTransferId;
            entity.LocationId = model.LocationId;
            entity.DeptId = model.DeptId;
            entity.IssuedToCode = model.IssuedToCode;
            entity.IssuedToDescription = model.IssuedToDescription;
            entity.IssuedDate = model.IssuedDate;
            entity.Qty = model.Qty;
            entity.Amount = model.UnitCost * model.Qty;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        public ValueTask<PsCardItemTransferIssuanceVM> DeleteAsync(PsCardItemTransferIssuanceVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            var entity = await _db.PsCardItemTransferIssuances.FindAsync(model.Id);

            //if (entity.PostedDt != null)
            //{
            //    throw new RecordAlreadyPostedException("Record Already Posted, cannot delete!");
            //}

            if (_db.RSMIs.Any(a => a.Date == model.IssuedDate))
            {
                throw new RecordAlreadyExistsException("RSMI already exists for this date, cannot delete!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItemTransferIssuances.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCardItemTransferIssuances.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            await UpdatePsItems(model.PsCardItemTransferId, user, date);

            return model;
        });        

        private async Task UpdatePsItems(Guid? transferId, string user, DateTime date)
        {
            var psCardItemTransfer = await _db.PsCardItemTransfers
                .Include(i => i.PsCardItem)
                .Include(i => i.PsCardItemTransferIssuances).Where(w => w.Id == transferId).FirstOrDefaultAsync();
            var qtyIss = psCardItemTransfer.PsCardItemTransferIssuances.Sum(s => s.Qty) ?? 0;
            var qty = (psCardItemTransfer.Qty ?? 0) + (psCardItemTransfer.TransferIn ?? 0);
            var transferOut = (psCardItemTransfer.TransferOut ?? 0);
            var qtyBal = qty - (qtyIss + transferOut);

            psCardItemTransfer.QtyIss = qtyIss;
            psCardItemTransfer.QtyBal = qtyBal;
            psCardItemTransfer.Amount = qtyBal * psCardItemTransfer.PsCardItem.TUnitCost;
            psCardItemTransfer.UpdatedBy = user;
            psCardItemTransfer.UpdatedDt = date;

            _db.PsCardItemTransfers.Attach(psCardItemTransfer);
            _db.Entry(psCardItemTransfer).State = EntityState.Modified;
            await _db.SaveChangesAsync();            
        }        

        public IQueryable<PsCardItemExtn> GetCardItemExtnForIssuanceByType(Guid? transferId)
        {
            var itemExtnName = _psCardSharedService.GetItemExtnName(transferId);
            switch (itemExtnName)
            {
                case "ItemExtnLand":
                    return GetCardItemExtnForIssuance<PsCardItemExtnLand>(transferId);
                case "ItemExtnBldg":
                    return GetCardItemExtnForIssuance<PsCardItemExtnBuilding>(transferId);
                case "ItemExtnVehicle":
                    return GetCardItemExtnForIssuance<PsCardItemExtnVehicle>(transferId);
                default:
                    return GetCardItemExtnForIssuance<PsCardItemExtnOther>(transferId);
            }
        }

        public IQueryable<T> GetCardItemExtnForIssuance<T>(Guid? transferId) where T : PsCardItemExtn
        {
            var data = _db.PsCardItemExtns.OfType<T>().AsNoTracking()
                        .Where(w => w.PsCardItemTransferItems.Any(a => a.PsCardItemTransferId == transferId))
                        .AsQueryable();
            return data;
        }

        /* 
         * Condition:
         * Not Transferred
         * Not Issueed
         */
        public IQueryable<T> GetCardItemExtnForIssuanceSelection<T>(Guid? transferId) where T : PsCardItemExtn
        {
            var data = _db.PsCardItemExtns.OfType<T>().AsNoTracking()
                        .Include(i => i.IcsParItems)
                        .Include(i => i.PsCardItemTransferItems)
                        .Where(w => w.PsCardItemTransferItems.Any(a => a.PsCardItemTransferId == transferId 
                            && !a.PsCardItemTransferIssuanceItems.Any())
                            && !w.PsCardItemTransferItems.Any(a => a.PsCardItemTransfer.ParentId == w.Id && a.PsCardItemExtnId == w.Id))
                        .AsQueryable();
            return data;
        }

        //public IQueryable<PsCardItemExtnVehicleVM> GetCardItemExtnForVehicleVmIssuanceSelection(Guid? transferId)
        //{
        //    var data = GetCardItemExtnForIssuanceSelection<PsCardItemExtnVehicle>(transferId)
        //                .Select(s => new PsCardItemExtnVehicleVM
        //                {
        //                    Id = s.Id,
        //                    YearModel = s.YearModel,
        //                    PlateNo = s.PlateNo,
        //                    BodyNo = s.BodyNo,
        //                    EngineNo = s.EngineNo,
        //                    ChasisNo = s.ChasisNo,
        //                    Color = s.Color,
        //                    CRN = s.CRN,
        //                    CRDate = s.CRDate,
        //                    MVFileNo = s.MVFileNo,
        //                    OrNo = s.OrNo,
        //                    OrDate = s.OrDate,
        //                    NetWeight = s.NetWeight,
        //                    InsPolicyNo = s.InsPolicyNo,
        //                    ParReissuance = s.ParReissuance,
        //                    Condition = s.Condition,
        //                    SubLocation = s.SubLocation,
        //                    ConductionNo = s.ConductionNo,
        //                    LocationId = s.IcsParItems.FirstOrDefault() == null ? null : s.IcsParItems.FirstOrDefault().IcsPar.LocationId,
        //                    Location = s.IcsParItems.FirstOrDefault() == null ? "" : s.IcsParItems.FirstOrDefault().IcsPar.Location,
        //                    LocationCode = s.IcsParItems.FirstOrDefault() == null ? "" : s.IcsParItems.FirstOrDefault().IcsPar.LocationCode,
        //                    ParIcsNo = s.IcsParItems.FirstOrDefault() == null ? "" : s.IcsParItems.FirstOrDefault().IcsPar.RefNo,
        //                    ParIcsDate = s.IcsParItems.FirstOrDefault() == null ? null : s.IcsParItems.FirstOrDefault().IcsPar.RefDate
        //                })
        //                .AsQueryable();
        //    return data;
        //}

        public IQueryable<PsCardItemExtn> GetCardItemExtnForOtherIssuanceSelection(Guid? transferId)
        {
            return GetCardItemExtnForIssuanceSelection<PsCardItemExtnOther>(transferId);
        }

        public IQueryable<PsCardItemExtn> GetCardItemExtnForVehicleIssuanceSelection(Guid? transferId)
        {
            return GetCardItemExtnForIssuanceSelection<PsCardItemExtnOther>(transferId);
        }

        public IQueryable<PsCardItemExtn> GetCardItemExtnForLandIssuanceSelection(Guid? transferId)
        {
            return GetCardItemExtnForIssuanceSelection<PsCardItemExtnLand>(transferId);
        }

        public IQueryable<PsCardItemExtn> GetCardItemExtnForBldgIssuanceSelection(Guid? transferId)
        {
            return GetCardItemExtnForIssuanceSelection<PsCardItemExtnBuilding>(transferId);
        }

        private void ValidateIfNull(PsCardItemTransferIssuanceVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(PsCardItemTransferIssuance entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }

        private async ValueTask ValidateFieldsAsync(PsCardItemTransferIssuanceVM model, Mode mode)
        {            
            if (model.IssuedDate == null)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.IssuedDate)), "Field is required.");
            }
            else
            {
                var cardItem = await _db.PsCardItems.Where(w => w.PsCardItemTransfers.Any(a => a.Id == model.PsCardItemTransferId)).FirstOrDefaultAsync();
                DateTime? refDate = null;
                string refName = "";
                if (cardItem.AirDate.HasValue)
                {
                    refDate = cardItem.AirDate;
                    refName = "AIR";
                }
                else
                {
                    refDate = cardItem.PoDate;
                    refName = "PO";
                }

                if (refDate.HasValue)
                {
                    if (refDate > model.IssuedDate)
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.IssuedDate)), $"Date issued must be on or after the {refName} date for this item, {refDate.Value.ToShortDateString()}");
                    }                    
                    var issuanceYears = _codextnService.GetIssuanceYears();
                    if (issuanceYears.Any())
                    {
                        var minYear = int.Parse(issuanceYears.Min(m => m.Description));
                        var maxYear = int.Parse(issuanceYears.Max(m => m.Description));

                        if (model.IssuedDate.Value.Year < minYear)
                        {
                            if (minYear == maxYear)
                            {
                                _imex.UpsertDataList(_getDisplayName(nameof(model.IssuedDate)), $"Year of date issued must be for year {minYear}.");
                            }
                            else
                            {
                                _imex.UpsertDataList(_getDisplayName(nameof(model.IssuedDate)), $"Year of date issued must be from {minYear} to {maxYear}.");
                            }
                        }

                        var year = model.IssuedDate.Value.Year.ToString().Trim();
                        var issuanceYear = issuanceYears.FirstOrDefault(f => f.Description == year);
                        if (issuanceYear == null)
                        {
                            _imex.UpsertDataList(_getDisplayName(nameof(model.IssuedDate)), $"Issuance for this year is not allowed.");
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(issuanceYear.Desc2))
                            {
                                var cutOffDate = DateTime.Parse(issuanceYear.Desc2);
                                if (model.IssuedDate.Value.Date > cutOffDate)
                                {
                                    _imex.UpsertDataList(_getDisplayName(nameof(model.IssuedDate)), $"Issuance for this year is only valid until {cutOffDate.ToShortDateString()}.");
                                }
                            }
                        }
                    }
                    else
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.IssuedDate)), $"Issuance year setup is not a available.");
                    }
                }
                else
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.Id)), "PO Date of this record is invalid!");
                }

            }

            var rsmiDate = await _db.RSMIs.MaxAsync(m => m.Date);
            if (rsmiDate != null && rsmiDate >= model.IssuedDate)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.LocationId)), string.Format("Date issued must be after the last RSMI date on {0}", rsmiDate.Value.ToShortDateString()));
            }

            if (model.LocationId == null || model.LocationId == Guid.Empty)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.LocationId)), "Field is required.");
            }
            else
            {
                if (!_codextnService.IsValidMastCodeId("LOCATIONS", model.LocationId))
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.LocationId)), "Invalid value.");
                }
            }

            if (!model.Qty.HasValue || model.Qty == 0)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Qty)), "Field is required.");
            }

            if (mode == Mode.ADD)
            {
                var qtyBalance = _db.PsCardItemTransfers.Find(model.PsCardItemTransferId)?.QtyBal ?? 0;
                if (model.Qty > qtyBalance)
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.Qty)), string.Format("Quantity must not exceed the remaing balance of {0}", qtyBalance));
                }
            }

            _imex.ThrowIfContainsErrors();
        }
    }
}