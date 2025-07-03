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
    public interface IPsCardItemTransferService
    {
        IQueryable<PsCardItemTransferVM> GetByPsCardItemId(Guid? psCardItemId);
        ValueTask<PsCardItemTransferVM> GetByIdAsync(Guid? id);

        ValueTask<PsCardItemTransferVM> CreateAsync(PsCardItemTransferVM model, string user, DateTime date);
        ValueTask<PsCardItemTransferVM> UpdateAsync(PsCardItemTransferVM model, string user, DateTime date);
        ValueTask<PsCardItemTransferVM> DeleteAsync(PsCardItemTransferVM model, string user, DateTime date);
        ValueTask<PsCardItemTransfer> DeleteAsync(Guid? id, string user, DateTime date);

        ValueTask<PsCardItemTransferVM> TransferAsync(PsCardItemTransferVM model, string user, DateTime date);
        ValueTask UpdatePsCardItemTransfer(Guid? transferId, string user, DateTime date);

        IPsCardItemTransferIssuanceService PsCardItemTransferIssuance { get; }
        IPsCardItemTransferItemService PsCardItemTransferItem { get; }
    }

    public class PsCardItemTransferService : BaseValidator, IPsCardItemTransferService
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly IExceptionService<PsCardItemTransferVM> _vmExceptionService;
        private readonly IExceptionService<PsCardItemTransfer> _exceptionService;
        private readonly ICodextnService _codextnService;
        private readonly IPsCardItemTransferIssuanceService _psCardItemTransferIssuanceService;
        private readonly IPsCardItemTransferItemService _psCardItemTransferItemService;

        public PsCardItemTransferService(AppManEntities db,
            IExceptionService<PsCardItemTransferVM> vmExceptionService,
            IExceptionService<PsCardItemTransfer> exceptionService,
            ICodextnService codextnService,
            IPsCardItemTransferIssuanceService psCardItemTransferIssuanceService,
            IPsCardItemTransferItemService psCardItemTransferItemService)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<PsCardItemTransferVM>(propertyName);
            _vmExceptionService = vmExceptionService;
            _exceptionService = exceptionService;
            _codextnService = codextnService;
            _psCardItemTransferIssuanceService = psCardItemTransferIssuanceService;
            _psCardItemTransferItemService = psCardItemTransferItemService;
        }

        public IPsCardItemTransferIssuanceService PsCardItemTransferIssuance => _psCardItemTransferIssuanceService;
        public IPsCardItemTransferItemService PsCardItemTransferItem => _psCardItemTransferItemService;

        private Expression<Func<PsCardItemTransfer, PsCardItemTransferVM>> GetProjection()
        {
            return s => new PsCardItemTransferVM
            {
                Id = s.Id,
                PsCardItemId = s.PsCardItemId,
                ParentId = s.ParentId,
                Qty = s.Qty,
                TransDate = s.TransDate,
                LocationId = s.LocationId,
                Location = s.Codextn.Description,
                QtyIss = s.QtyIss,
                QtyBal = s.QtyBal,
                TransferIn = s.TransferIn,
                TransferOut = s.TransferOut,
                TranType = s.TranType,
                Amount = s.Amount,
                InsertedDt = s.InsertedDt,
                IsWithItemExtn = s.PsCardItem.PsCardItemExtns.Any(),
                PoNo = s.PsCardItem.PoNo,
                PoDate = s.PsCardItem.PoDate,
                DeptId = s.PsCardItem.DeptId,
                DeptDisplay = s.PsCardItem.DeptDisplay,
                Unit = s.PsCardItem.Unit,
                UnitCost = s.PsCardItem.UnitCost
            };
        }

        public ValueTask<PsCardItemTransferVM> GetByIdAsync(Guid? id) => _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.PsCardItemTransfers
                .Include(i => i.PsCardItem.PsCardItemExtns)
                .Where(w => w.Id == id).AsNoTracking()
                .Select(GetProjection()).FirstOrDefaultAsync();

            if (data != null)
            {
                var tContentNo = GetTContentNo(data.PsCardItemId);
                data.TContentNo = tContentNo;
            }
            return data;
        });

        private int? GetTContentNo(Guid? psCardItemId)
        {
            var unitGroup = _db.PsCardItemUnitGroups.FirstOrDefault(f => f.PsCardItemUnitGroupDescriptions.Any(a => a.PsCardItemUnitGroupDescriptionItems.Any(b => b.PsCardItemId == psCardItemId)));
            if (unitGroup == null)
            {
                return 1;
            }
            return unitGroup.Qty;
        }

        public IQueryable<PsCardItemTransferVM> GetByPsCardItemId(Guid? psCardItemId) => _vmExceptionService.TryCatch(() =>
        {
            var data = _db.PsCardItemTransfers.Where(w => w.PsCardItemId == psCardItemId).AsNoTracking()
                .Select(GetProjection());
            return data;
        });


        public ValueTask<PsCardItemTransferVM> CreateAsync(PsCardItemTransferVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            await ValidateFieldsAsync(model, Mode.ADD);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;

            var entity = new PsCardItemTransfer();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.PsCardItemTransfers.Add(entity);
            await _db.SaveChangesAsync();

            if (model.ParentId != null)
            {
                await UpdatePsCardItemTransfer(model.ParentId, user, date);
            }

            return model;
        });


        public ValueTask<PsCardItemTransferVM> UpdateAsync(PsCardItemTransferVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            var entity = await _db.PsCardItemTransfers.Include(i => i.PsCardItem).Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            ValidateRecord(entity, model.Id);
            await ValidateFieldsAsync(model, Mode.EDIT);

            var qtyBalance = (_db.PsCardItemTransfers.Find(model.Id)?.QtyBal ?? 0) + entity.Qty;
            if (model.Qty > qtyBalance)
            {
                throw new InvalidValueException(string.Format("Quantity must not exceed the remaing balance of {0}", qtyBalance));
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            MapModelToEntityFields(entity, model, Mode.EDIT);

            _db.PsCardItemTransfers.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            await UpdatePsCardItemTransfer(model.Id, user, date);
            if (model.ParentId != null)
            {
                await UpdatePsCardItemTransfer(model.ParentId, user, date);
            }

            return model;
        });

        public void MapModelToEntityFields(PsCardItemTransfer entity, PsCardItemTransferVM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }
            entity.PsCardItemId = model.PsCardItemId;
            entity.ParentId = model.ParentId;
            entity.Qty = model.Qty;
            entity.TransDate = model.TransDate;
            entity.LocationId = model.LocationId;
            entity.QtyIss = model.QtyIss;
            entity.QtyBal = model.QtyBal;
            entity.TransferIn = model.TransferIn;
            entity.TransferOut = model.TransferOut;
            entity.TranType = model.TranType;
            entity.Amount = model.Amount;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        public ValueTask<PsCardItemTransferVM> DeleteAsync(PsCardItemTransferVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            var entity = await _db.PsCardItemTransfers.FindAsync(model.Id);
            var parentId = entity.ParentId;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItemTransfers.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCardItemTransfers.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            if (parentId != null)
            {
                await UpdatePsCardItemTransfer(parentId, user, date);
            }

            return model;
        });

        public ValueTask<PsCardItemTransfer> DeleteAsync(Guid? id, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.PsCardItemTransfers.FindAsync(id);
            var parentId = entity.ParentId;

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PsCardItemTransfers.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCardItemTransfers.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            if (parentId != null)
            {
                await UpdatePsCardItemTransfer(parentId, user, date);
            }

            return entity;
        });

        public ValueTask<PsCardItemTransferVM> TransferAsync(PsCardItemTransferVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            await ValidateFieldsAsync(model, Mode.ADD);

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
                var PsCardItemTransfer = await CreateAsync(model, user, date);

                foreach (var selectedId in selectedIds)
                {
                    var PsCardItemTransferItem = new PsCardItemTransferItem()
                    {
                        Id = Guid.NewGuid(),
                        PsCardItemTransferId = PsCardItemTransfer.Id,
                        PsCardItemExtnId = Guid.Parse(selectedId),
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };
                    _db.PsCardItemTransferItems.Add(PsCardItemTransferItem);
                }
                _db.SaveChanges();
            }
            else
            {
                var PsCardItemTransfer = await CreateAsync(model, user, date);
            }

            await UpdatePsCardItemTransfer(model.Id, user, date);

            return model;
        });

        public async ValueTask UpdatePsCardItemTransfer(Guid? id, string user, DateTime date)
        {
            var psCardItemTransfer = await _db.PsCardItemTransfers
                .Include(i => i.PsCardItem)
                .Include(i => i.PsCardItemTransferIssuances).Where(w => w.Id == id).FirstOrDefaultAsync();
            var qtyIss = psCardItemTransfer.PsCardItemTransferIssuances.Sum(s => s.Qty);
            var qty = (psCardItemTransfer.Qty ?? 0) + (psCardItemTransfer.TransferIn ?? 0);
            var transferOut = _db.PsCardItemTransfers
                .Where(w => w.ParentId == id).Sum(s => s.TransferIn);
            var qtyBal = qty - ((qtyIss ?? 0) + (transferOut ?? 0));

            psCardItemTransfer.TransferOut = transferOut;
            psCardItemTransfer.QtyIss = qtyIss;
            psCardItemTransfer.QtyBal = qtyBal;
            psCardItemTransfer.Amount = qtyBal * psCardItemTransfer.PsCardItem.TUnitCost;
            psCardItemTransfer.UpdatedBy = user;
            psCardItemTransfer.UpdatedDt = date;

            _db.PsCardItemTransfers.Attach(psCardItemTransfer);
            _db.Entry(psCardItemTransfer).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }

        private void ValidateIfNull(PsCardItemTransferVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(PsCardItemTransfer entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }

        private async ValueTask ValidateFieldsAsync(PsCardItemTransferVM model, Mode mode)
        {
            if (model.TransDate == null)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.TransDate)), "Field is required.");
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

            if (mode == Mode.ADD && model.ParentId != null)
            {
                //var qtyBalance = _db.PsCardItemTransfers.Find(model.PsCardItemTransferId)?.QtyBal ?? 0;
                var qtyBalance = _db.PsCardItemTransfers.FirstOrDefault(f => f.ParentId == model.ParentId)?.QtyBal ?? 0;
                if (model.Qty > qtyBalance)
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.Qty)), string.Format("Quantity must not exceed the remaing balance of {0}", qtyBalance));
                }
            }

            _imex.ThrowIfContainsErrors();
        }
    }
}