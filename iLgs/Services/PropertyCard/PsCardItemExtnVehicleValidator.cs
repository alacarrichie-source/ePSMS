using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using static iLgs.Models.Enums;

namespace iLgs.Services.PropertyCard
{
    public interface IPsCardItemExtnVehicleValidator
    {
        void ValidateOnCreate(PsCardItemExtnVehicleVM model);
        void ValidateOnUpdate(PsCardItemExtnVehicleVM model);
        void ValidateOnDelete(PsCardItemExtnVehicleVM model);
    }

    public class PsCardItemExtnVehicleValidator : BaseValidator, IPsCardItemExtnVehicleValidator
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;

        public PsCardItemExtnVehicleValidator(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<PsCardItemExtnVehicleVM>(propertyName);
        }

        public void ValidateOnCreate(PsCardItemExtnVehicleVM model)
        {
            ValidateIfNull(model);
            ValidateIfPosted(model);
            ValidateFieldsOnCreateUpdate(model, Mode.ADD);
        }

        public void ValidateOnUpdate(PsCardItemExtnVehicleVM model)
        {
            ValidateIfNull(model);
            ValidateRecord((Guid)model.PsCardItemExtnId);
            ValidateIfPosted(model);
            ValidateFieldsOnCreateUpdate(model, Mode.EDIT);
        }

        public void ValidateOnDelete(PsCardItemExtnVehicleVM model)
        {
            ValidateIfNull(model);
            ValidateRecord((Guid)model.PsCardItemExtnId);
            ValidateIfPosted(model);

            // check in Par/Ics
            if (_db.IcsParItems.Any(a => a.PsCardItemExtnId == model.PsCardItemId))
            {
                throw new RecordAlreadyExistsException("PAR/ICS already exists for this record, cannot delete!");
            }

            //var transactions = _db.PsCardItemTransactions.Where(a => a.PsCardItemExtnId == a.Id && a.Remarks != "CARD")
            //    .GroupBy(g => g.Remarks)
            //    .Select(s => s.Key);
            //if (transactions.Any())
            //{
            //    string remarks = string.Join("/", transactions);
            //    throw new RecordAlreadyExistsException("PAR/ICS already exists for this record, cannot delete!");
            //}

            var transfer = _db.PsCardItemTransferItems.AsNoTracking()
                    .Where(w => w.PsCardItemTransfer.ParentId != null && w.PsCardItemExtnId == model.PsCardItemExtnId);
            if (transfer.Any())
            {
                throw new RecordAlreadyExistsException("Item was already Transferred, cannot delete!");
            }

            var issuance = _db.PsCardItemTransferItems.AsNoTracking()
                    .Where(w => w.PsCardItemExtnId == model.PsCardItemExtnId && w.PsCardItemTransferIssuanceItems.Any());
            if (issuance.Any())
            {
                throw new RecordAlreadyExistsException("Item was already Issued, cannot delete!");
            }
        }

        public void ValidateFieldsOnCreateUpdate(PsCardItemExtnVehicleVM model, Mode mode)
        {
            var ex = new InvalidModelException();

            if (!string.IsNullOrWhiteSpace(model.ConductionNo))
            {
                var data = _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>().FirstOrDefault(f => f.ConductionNo == model.ConductionNo);
                if (data != null) {
                    if (mode == Mode.ADD)
                    {
                        ex.UpsertDataList(_getDisplayName(nameof(model.ConductionNo)), "Already exists.");
                    }
                    else
                    {
                        if (data.Id != model.PsCardItemExtnId)
                        {
                            ex.UpsertDataList(_getDisplayName(nameof(model.ConductionNo)), "Already exists.");
                        }
                    }
                }
            }

            //if (model.DeptId == null)
            //{
            //    ex.UpsertDataList(_getDisplayName(nameof(model.DeptId)), "Field is required.");
            //}
            //else
            //{
            //    if (!_codextnService.IsValidMastCodeId("DEPARTMENTS", model.DeptId))
            //    {
            //        ex.UpsertDataList(_getDisplayName(nameof(model.DeptId)), "Invalid value");
            //    }
            //}

            //if (string.IsNullOrWhiteSpace(model.Unit))
            //{
            //    ex.UpsertDataList(_getDisplayName(nameof(model.Unit)), "Field is required.");
            //}
            //else
            //{
            //    if (!_codextnService.IsValidMastCodeCode("UNIT", model.Unit))
            //    {
            //        ex.UpsertDataList(_getDisplayName(nameof(model.Unit)), "Invalid value");
            //    }
            //}

            //if (!model.UnitCost.HasValue)
            //{
            //    ex.UpsertDataList(_getDisplayName(nameof(model.UnitCost)), "Field is required.");
            //}

            //if (string.IsNullOrWhiteSpace(model.Description))
            //{
            //    ex.UpsertDataList(_getDisplayName(nameof(model.Description)), "Field is required.");
            //}

            //if (string.IsNullOrWhiteSpace(model.InvDist))
            //{
            //    ex.UpsertDataList(_getDisplayName(nameof(model.InvDist)), "Field is required.");
            //}
            //else
            //{
            //    if (!_codextnService.IsValidMastCodeCode("PS-REMARKS", model.InvDist))
            //    {
            //        ex.UpsertDataList(_getDisplayName(nameof(model.InvDist)), "Invalid value");
            //    }
            //}

            ex.ThrowIfContainsErrors();
        }

        private void ValidateRecord(Guid id)
        {
            if (!_db.PsCardItemExtns.Any(a => a.Id == id))
            {
                throw new NotFoundException(id);
            }
        }

        private static void ValidateIfNull(PsCardItemExtnVehicleVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateIfPosted(PsCardItemExtnVehicleVM model)
        {
            var entity = _db.PsCards.Where(w => w.PsCardItems.Any(a => a.Id == model.PsCardItemId)).FirstOrDefault();
            if (entity != null && entity.PostedDt != null)
            {
                var msg = $"Record already posted by {entity.PostedBy} on {entity.PostedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }
    }
}