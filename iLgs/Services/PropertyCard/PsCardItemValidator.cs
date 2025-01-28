using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Linq;
using static iLgs.Models.Enums;

namespace iLgs.Services.PropertyCard
{
    public interface IPsCardItemValidator
    {
        void ValidateOnCreate(PsCardItemVM cardItem);
        void ValidateOnUpdate(PsCardItemVM cardItem);
        void ValidateOnDelete(PsCardItemVM cardItem);
    }

    public class PsCardItemValidator : BaseValidator, IPsCardItemValidator
    {
        private delegate string GetDisplayNameDelegate(string propertyName);
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly ICodextnService _codextnService;
        public PsCardItemValidator(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<PsCardItemVM>(propertyName);
            _codextnService = new CodextnService(_db);
        }

        public void ValidateOnCreate(PsCardItemVM cardItem)
        {
            ValidateCard(cardItem);
            ValidateIfPosted(cardItem.PsCardId);
            ValidateFieldsOnCreateUpdate(cardItem, Mode.ADD);
        }

        public void ValidateOnUpdate(PsCardItemVM cardItem)
        {
            ValidateCard(cardItem);
            var entity = _db.PsCardItems.Find(cardItem.Id);
            ValidateRecord(entity, cardItem.Id);
            ValidateIfPosted(cardItem.PsCardId);
            ValidateIfPosted(cardItem);
            ValidateFieldsOnCreateUpdate(cardItem, Mode.EDIT);
        }

        public void ValidateOnDelete(PsCardItemVM cardItem)
        {
            ValidateCard(cardItem);
            var entity = _db.PsCardItems.Find(cardItem.Id);
            ValidateRecord(entity, cardItem.Id);
            ValidateIfPosted(cardItem);
            ValidateIfPosted(cardItem.PsCardId);

            if (_db.PsCardItems.Any(a => a.Id == cardItem.Id && a.PsCardItemTransfers.Any()))
            {
                throw new RecordRelationshipException("Items of this record were transfered to other department/location, cannot delete!");
            }

            if (_db.PsCardItems.Any(a => a.Id == cardItem.Id && a.PsCardItemExtns.Any(a2 => a2.IcsParItems.Any())))
            {
                throw new RecordRelationshipException("Items of this record already have PAR/ICS, cannot delete!");
            }
        }

        public void ValidateFieldsOnCreateUpdate(PsCardItemVM cardItem, Mode mode)
        {
            var ex = new InvalidModelException();
            if (cardItem.DeptId == null)
            {
                ex.UpsertDataList(_getDisplayName(nameof(cardItem.DeptId)), "Field is required.");
            }
            else
            {
                if (!_codextnService.IsValidMastCodeId("LOCATIONS", cardItem.DeptId))
                {
                    ex.UpsertDataList(_getDisplayName(nameof(cardItem.DeptId)), "Invalid value");
                }
            }

            if (string.IsNullOrWhiteSpace(cardItem.Unit))
            {
                ex.UpsertDataList(_getDisplayName(nameof(cardItem.Unit)), "Field is required.");
            }
            else
            {
                if (!_codextnService.IsValidMastCodeCode("UNIT", cardItem.Unit))
                {
                    ex.UpsertDataList(_getDisplayName(nameof(cardItem.Unit)), "Invalid value");
                }
            }

            if (!cardItem.UnitCost.HasValue)
            {
                ex.UpsertDataList(_getDisplayName(nameof(cardItem.UnitCost)), "Field is required.");
            }

            //if (cardItem.DeptId != null)
            //{
            //    if (cardItem.LocationId != null)
            //    {
            //        if (!cardItem.TransferIn.HasValue)
            //        {
            //            ex.UpsertDataList(_getDisplayName(nameof(cardItem.TransferIn)), "Field is required.");
            //        }
            //    }
            //    else
            //    {
            //        if (!cardItem.Qty.HasValue)
            //        {
            //            ex.UpsertDataList(_getDisplayName(nameof(cardItem.Qty)), "Field is required.");
            //        }
            //    }
            //}

            if (string.IsNullOrWhiteSpace(cardItem.Description))
            {
                ex.UpsertDataList(_getDisplayName(nameof(cardItem.Description)), "Field is required.");
            }

            if (string.IsNullOrWhiteSpace(cardItem.InvDist))
            {
                ex.UpsertDataList(_getDisplayName(nameof(cardItem.InvDist)), "Field is required.");
            }
            else
            {
                if (!_codextnService.IsValidMastCodeCode("PS-REMARKS", cardItem.InvDist))
                {
                    ex.UpsertDataList(_getDisplayName(nameof(cardItem.InvDist)), "Invalid value");
                }
            }

            if (string.IsNullOrWhiteSpace(cardItem.PoNo))
            {
                ex.UpsertDataList(_getDisplayName(nameof(cardItem.PoNo)), "Field is required.");
            }
            else
            {
                if (mode == Mode.ADD)
                {
                    if (_db.PsCardItems.Where(w => w.PoNo == cardItem.PoNo && w.PsCardId == cardItem.PsCardId).Any())
                    {
                        ex.UpsertDataList(_getDisplayName(nameof(cardItem.PoNo)), "Already exists under this Stock/Property No.");
                    }
                }
            }


            if (!cardItem.PoDate.HasValue)
            {
                ex.UpsertDataList(_getDisplayName(nameof(cardItem.PoDate)), "Field is required.");
            }

            ex.ThrowIfContainsErrors();
        }

        private void ValidateRecord(PsCardItem entity, Guid id)
        {
            if (entity is null)
            {
                throw new NotFoundException(id);
            }
        }

        private static void ValidateCard(PsCardItemVM card)
        {
            if (card is null)
            {
                throw new NullException();
            }
        }

        private void ValidateIfPosted(Guid? psCardId)
        {
            var entity = _db.PsCards.Find(psCardId);
            if (entity != null && entity.PostedDt != null)
            {
                var msg = $"Record already posted by {entity.PostedBy} on {entity.PostedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        private void ValidateIfPosted(PsCardItemVM model)
        {
            var entity = _db.PsCardItems.Find(model.Id);
            if (entity != null && entity.PostedDt != null)
            {
                var msg = $"Record already posted by {entity.PostedBy} on {entity.PostedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }
    }
}