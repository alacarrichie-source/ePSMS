using System.Data.Entity;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Linq;

namespace iLgs.Services.PropertyCard
{
    public interface IPsCardItemExtnOtherValidator
    {
        void ValidateOnCreate(PsCardItemExtnOtherVM model);
        void ValidateOnUpdate(PsCardItemExtnOtherVM model);
        void ValidateOnDelete(PsCardItemExtnOtherVM model);
    }

    public class PsCardItemExtnOtherValidator : BaseValidator, IPsCardItemExtnOtherValidator
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;

        public PsCardItemExtnOtherValidator(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<PsCardItemExtnOtherVM>(propertyName);
        }

        public void ValidateOnCreate(PsCardItemExtnOtherVM model)
        {
            ValidateIfNull(model);
            ValidateIfPosted(model);
            ValidateFieldsOnCreateUpdate(model);
        }

        public void ValidateOnUpdate(PsCardItemExtnOtherVM model)
        {
            ValidateIfNull(model);
            ValidateRecord((Guid)model.Id);
            ValidateIfPosted(model);
            ValidateFieldsOnCreateUpdate(model);
        }

        public void ValidateOnDelete(PsCardItemExtnOtherVM model)
        {
            ValidateIfNull(model);
            ValidateRecord((Guid)model.Id);
            ValidateIfPosted(model);

            // check in Par/Ics
            var icsParItem = _db.IcsParItems.Include(a => a.IcsPar).FirstOrDefault(a => a.PsCardItemExtnId == model.Id);
            if (icsParItem != null)
            {
                var doc = icsParItem.IcsPar != null ? (icsParItem.IcsPar.RefType + " No. " + icsParItem.IcsPar.RefNo) : "an existing PAR/ICS record";
                throw new RecordAlreadyExistsException("This physical unit cannot be deleted because it is already referenced by " + doc + ".");
            }

            var compItem = _db.IcsParItemComponents.Include(a => a.IcsParItem.IcsPar).FirstOrDefault(a => a.PsCardItemExtnId == model.Id);
            if (compItem != null)
            {
                var doc = compItem.IcsParItem != null && compItem.IcsParItem.IcsPar != null ? (compItem.IcsParItem.IcsPar.RefType + " No. " + compItem.IcsParItem.IcsPar.RefNo) : "an existing PAR/ICS bundle";
                throw new RecordAlreadyExistsException("This component unit cannot be deleted because it is assigned to " + doc + ".");
            }

            var extn = _db.PsCardItemExtns.Find(model.Id);
            if (extn != null && extn.AIRItemExtnId != null)
            {
                throw new RecordRelationshipException("This physical unit was generated from an AIR inspection and cannot be deleted here.");
            }
            
            var transfer = _db.PsCardItemTransferItems.AsNoTracking()
                    .Where(w => w.PsCardItemTransfer.ParentId != null && w.PsCardItemExtnId == model.Id);
            if (transfer.Any())
            {
                throw new RecordAlreadyExistsException("Item was already Transferred, cannot delete!");
            }

            var issuance = _db.PsCardItemTransferItems.AsNoTracking()
                    .Where(w => w.PsCardItemExtnId == model.Id && w.PsCardItemTransferIssuanceItems.Any());
            if (issuance.Any())
            {
                throw new RecordAlreadyExistsException("Item was already Issued, cannot delete!");
            }
        }

        public void ValidateFieldsOnCreateUpdate(PsCardItemExtnOtherVM model)
        {
            var ex = new InvalidModelException();

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

        private static void ValidateIfNull(PsCardItemExtnOtherVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateIfPosted(PsCardItemExtnOtherVM model)
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