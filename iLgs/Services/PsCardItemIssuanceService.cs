using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.Interfaces;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services
{
    public interface IPsCardItemIssuanceService
    {
        IQueryable<PsCardItemIssuanceVM> GetByCardItemId(Guid? cardItemId);
        ValueTask<PsCardItemIssuanceVM> GetByIdAsync(Guid? id);

        ValueTask<PsCardItemIssuanceVM> CreateAsync(PsCardItemIssuanceVM model, string user, DateTime date);
        ValueTask<PsCardItemIssuanceVM> UpdateAsync(PsCardItemIssuanceVM model, string user, DateTime date);
        ValueTask<PsCardItemIssuanceVM> DeleteAsync(PsCardItemIssuanceVM model, string user, DateTime date);
        ValueTask<PsCardItemIssuance> PostAsync(Guid psCardItemIssuanceId, string user, DateTime date);
        ValueTask<PsCardItemIssuance> UnpostAsync(Guid psCardItemIssuanceId, string user, DateTime date);
    }

    public class PsCardItemIssuanceService : BaseValidator, IPsCardItemIssuanceService
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly IExceptionService<PsCardItemIssuanceVM> _VmExceptionService = new ExceptionService<PsCardItemIssuanceVM>();
        private readonly IExceptionService<PsCardItemIssuance> _ExceptionService = new ExceptionService<PsCardItemIssuance>();
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;
        private readonly IPsCardItemExtnService _psCardItemExtnService;
        private readonly ICodextnService _codextnService;

        public PsCardItemIssuanceService(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<PsCardItemIssuanceVM>(propertyName);
            _psCardItemTransactionService = new PsCardItemTransactionService(_db);
            _psCardItemExtnService = new PsCardItemExtnService(_db);
            _codextnService = new CodextnService(_db);
        }

        public ValueTask<PsCardItemIssuanceVM> GetByIdAsync(Guid? id) => _VmExceptionService.TryCatch(async () =>
        {
            var data = await _db.PsCardItemIssuances.Where(w => w.Id == id).AsNoTracking()
                .Select(s => new PsCardItemIssuanceVM
                {
                    Id = s.Id,
                    LocationId = s.LocationId,
                    PsCardItemId = s.PsCardItemId,
                    IssuedTo = s.IssuedTo,
                    IssuedDate = s.IssuedDate,
                    IssuedToCode = s.IssuedToCode,
                    IssuedToDescription = s.IssuedToDescription,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    InsertedDt = s.InsertedDt,
                    DeptId = s.DeptId,
                    Department = s.Codextn.Description,
                    UnitCost = s.PsCardItem.UnitCost,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt,
                    Location = s.Codextn1.Description
                    //IssuedToDesc = s.IssuedTo.Contains("Department") ? s.Codextn.Description : (s.IssuedTo == "Location" || s.IssuedTo == "Disposal") ? s.Codextn1.Description : ""
                }).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<PsCardItemIssuanceVM> GetByCardItemId(Guid? cardItemId) => _VmExceptionService.TryCatch(() =>
        {
            var data = _db.PsCardItemIssuances.Where(w => w.PsCardItemId == cardItemId).AsNoTracking()
                .Select(s => new PsCardItemIssuanceVM
                {
                    Id = s.Id,
                    LocationId = s.LocationId,
                    PsCardItemId = s.PsCardItemId,
                    IssuedTo = s.IssuedTo,
                    IssuedDate = s.IssuedDate,
                    IssuedToCode = s.IssuedToCode,
                    IssuedToDescription = s.IssuedToDescription,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    InsertedDt = s.InsertedDt,
                    DeptId = s.DeptId,
                    Department = s.Codextn.Description,
                    UnitCost = s.PsCardItem.UnitCost,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt,
                    Location = s.Codextn1.Description
                    //IssuedToDesc = s.IssuedTo.Contains("Department") ? s.Codextn.Description : (s.IssuedTo == "Location" || s.IssuedTo == "Disposal") ? s.Codextn1.Description : ""
                });
            return data;
        });

        //public IQueryable GetVehicleSelection(Guid? cardItemId)
        //{
        //    var data = _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>().Where(w => w.PsCardItemId == cardItemId);
        //}        

        public ValueTask<PsCardItemIssuanceVM> CreateAsync(PsCardItemIssuanceVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            await ValidateFieldsAsync(model, Mode.ADD);

            if (model.SelectedIds != null)
            {
                string[] selectedIds = model.SelectedIds.Split(',');

                if (selectedIds.Count() == 0)
                {
                    throw new InvalidValueException(string.Format("No selected items, cannot continue!"));
                }

                // get all selected ids, put them in a list
                List<PsCardItemExtnLocationVm> psCardItemExtnLocationList = new List<PsCardItemExtnLocationVm>();
                foreach (var selectedId in selectedIds)
                {
                    var itemExtnId = Guid.Parse(selectedId);
                    var psCardItemExtnLocation = await _psCardItemExtnService.GetCardItemExtnLocationAsync(itemExtnId);
                    psCardItemExtnLocationList.Add(psCardItemExtnLocation);
                }

                var locationCodeGroup = psCardItemExtnLocationList.GroupBy(g => new { g.LocationId, g.LocationCode, g.Location });
                foreach (var locationCode in locationCodeGroup)
                {
                    var qty = locationCode.Count();
                    var entity = new PsCardItemIssuance
                    {
                        Id = Guid.NewGuid(),
                        LocationId = locationCode.Key.LocationId,
                        PsCardItemId = model.PsCardItemId,
                        IssuedTo = model.IssuedTo,
                        IssuedDate = model.IssuedDate,
                        Qty = qty,
                        Amount = model.UnitCost * qty,
                        IssuedToCode = model.IssuedToCode,
                        IssuedToDescription = model.IssuedToDescription,
                        InsertedBy = model.InsertedBy,
                        InsertedDt = model.InsertedDt,
                        UpdatedBy = model.UpdatedBy,
                        UpdatedDt = model.UpdatedDt,
                        DeptId = model.DeptId
                    };

                    _db.PsCardItemIssuances.Add(entity);
                    await _db.SaveChangesAsync();

                    var psCardItemExtns = psCardItemExtnLocationList.Where(w => w.LocationCode == locationCode.Key.LocationCode).ToList();
                    foreach (var psCardItemExtn in psCardItemExtns)
                    {
                        await _psCardItemTransactionService.LogUpdates(psCardItemExtn.Id, entity.Id, "ISSUANCE", user, date);
                    }
                }
            }
            else
            {
                var entity = new PsCardItemIssuance
                {
                    Id = Guid.NewGuid(),
                    LocationId = model.LocationId,
                    PsCardItemId = model.PsCardItemId,
                    IssuedTo = model.IssuedTo,
                    IssuedDate = model.IssuedDate,
                    Qty = model.Qty,
                    Amount = model.UnitCost * model.Qty,
                    IssuedToCode = model.IssuedToCode,
                    IssuedToDescription = model.IssuedToDescription,
                    InsertedBy = model.InsertedBy,
                    InsertedDt = model.InsertedDt,
                    UpdatedBy = model.UpdatedBy,
                    UpdatedDt = model.UpdatedDt,
                    DeptId = model.DeptId
                };

                _db.PsCardItemIssuances.Add(entity);
                await _db.SaveChangesAsync();
            }

            await UpdatePsItems(model.PsCardItemId, user, date);

            return model;
        });

        public ValueTask<PsCardItemIssuanceVM> UpdateAsync(PsCardItemIssuanceVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            var entity = await _db.PsCardItemIssuances.Include(i => i.PsCardItem).Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            ValidateRecord(entity, model.Id);

            if (entity.PostedDt != null)
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            }

            if (_db.RSMIs.Any(a => a.Date == entity.IssuedDate))
            {
                throw new RecordAlreadyExistsException($"RSMI already exists for the saved date, {entity.IssuedDate.Value.ToShortDateString()}, cannot update!");
            }

            await ValidateFieldsAsync(model, Mode.EDIT);

            var qtyBalance = (_db.PsCardItems.Find(model.PsCardItemId)?.QtyBal ?? 0) + entity.Qty;
            if (model.Qty > qtyBalance)
            {
                throw new InvalidValueException(string.Format("Quantity must not exceed the remaing balance of {0}", qtyBalance));
            }

            //if (model.IssuedTo.Contains("Department") || model.IssuedTo == "Location" || model.IssuedTo == "Disposal")
            //{
            //    if (model.IssuedTo.Contains("Department"))
            //    {
            //        model.LocationId = null;
            //    }
            //    else if (model.IssuedTo == "Location" || model.IssuedTo == "Disposal")
            //    {
            //        model.DeptId = null;
            //    }
            //}
            //else
            //{
            //    model.LocationId = null;
            //    model.IssuedToCode = "";
            //}

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.LocationId = model.LocationId;
            entity.PsCardItemId = model.PsCardItemId;
            entity.DeptId = model.DeptId;
            entity.IssuedTo = model.IssuedTo;
            entity.IssuedDate = model.IssuedDate;
            entity.IssuedToCode = model.IssuedToCode;
            entity.IssuedToDescription = model.IssuedToDescription;
            entity.Qty = model.Qty;
            entity.Amount = entity.PsCardItem.UnitCost * model.Qty;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItemIssuances.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            await UpdatePsItems(model.PsCardItemId, user, date);

            return model;
        });

        public ValueTask<PsCardItemIssuanceVM> DeleteAsync(PsCardItemIssuanceVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            var entity = await _db.PsCardItemIssuances.FindAsync(model.Id);

            if (entity.PostedDt != null)
            {
                throw new RecordAlreadyPostedException("Record Already Posted, cannot delete!");
            }

            if (_db.RSMIs.Any(a => a.Date == model.IssuedDate))
            {
                throw new RecordAlreadyExistsException("RSMI already exists for this date, cannot delete!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItemIssuances.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCardItemIssuances.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            await UpdatePsItems(model.PsCardItemId, user, date);

            return model;
        });

        private async Task UpdatePsItems(Guid? cardItemId, string user, DateTime date)
        {
            var entity = await _db.PsCardItems.Include(i => i.PsCardItemIssuances).Where(w => w.Id == cardItemId).FirstOrDefaultAsync();
            var qtyIss = entity.PsCardItemIssuances.Sum(s => s.Qty) ?? 0;
            var qty = (entity.Qty ?? 0) + (entity.TransferIn ?? 0);
            var transferOut = (entity.TransferOut ?? 0);

            entity.QtyIss = qtyIss;
            entity.QtyBal = qty - (qtyIss + transferOut);
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PsCardItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }

        public ValueTask<PsCardItemIssuance> PostAsync(Guid psCardItemIssuanceId, string user, DateTime date) => _ExceptionService.TryCatch(async () =>
        {
            var entity = await _db.PsCardItemIssuances.FindAsync(psCardItemIssuanceId);
            if (entity == null)
            {
                throw new RecordNotFoundException(psCardItemIssuanceId);
            }

            entity.PostedBy = user;
            entity.PostedDt = date;

            _db.PsCardItemIssuances.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        public ValueTask<PsCardItemIssuance> UnpostAsync(Guid psCardItemIssuanceId, string user, DateTime date) => _ExceptionService.TryCatch(async () =>
        {
            var entity = await _db.PsCardItemIssuances.FindAsync(psCardItemIssuanceId);
            if (entity == null)
            {
                throw new RecordNotFoundException(psCardItemIssuanceId);
            }

            entity.PostedBy = null;
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PsCardItemIssuances.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        private void ValidateIfNull(PsCardItemIssuanceVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(PsCardItemIssuance entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }

        private async ValueTask ValidateFieldsAsync(PsCardItemIssuanceVM model, Mode mode)
        {
            //if (model.IssuedTo.Contains("Department"))
            //{
            //    if (model.DeptId == null)
            //    {
            //        throw new InvalidValueException("Department is required!");
            //    }                
            //}

            //if (model.IssuedTo == "Location" || model.IssuedTo == "Disposal")
            //{
            //    if (model.LocationId == null)
            //    {
            //        throw new InvalidValueException("Location is required!");
            //    }
            //}

            if (model.IssuedDate == null)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.IssuedDate)), "Field is required.");
            }
            else
            {                
                var poDate = await _db.PsCardItems.Include(i => i.PsCard).Where(w => w.Id == model.PsCardItemId).Select(s => s.PoDate).FirstOrDefaultAsync();
                if (poDate.HasValue)
                {
                    if (poDate > model.IssuedDate)
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.IssuedDate)), $"Date issued must be on or after the PO date for this item, {poDate.Value.ToShortDateString()}");
                    }
                    if (model.IssuedDate.Value.Year < DateTime.Now.Year)
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.IssuedDate)), $"Year of date issued must be on the current year, {DateTime.Now.Year}");
                    }
                }
                else
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.Id)), "PO Date of this record is invalid!");
                }

            }

            var rsmiDate = await _db.RSMIs.MaxAsync(m => m.Date);
            if (rsmiDate != null && rsmiDate > model.IssuedDate)
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
                var qtyBalance = _db.PsCardItems.Find(model.PsCardItemId)?.QtyBal ?? 0;
                if (model.Qty > qtyBalance)
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.Qty)), string.Format("Quantity must not exceed the remaing balance of {0}", qtyBalance));
                }
            }

            _imex.ThrowIfContainsErrors();
        }
    }
}