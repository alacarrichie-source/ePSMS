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
        //private delegate string GetDisplayNameDelegate(string propertyName);
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly ICodextnService _codextnService;
        private readonly IUserService _userService;

        public PsCardItemValidator(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<PsCardItemVM>(propertyName);
            _codextnService = new CodextnService(_db);
            _userService = new UserService(_db);
        }

        //public PsCardItemValidator(AppManEntities db,
        //    ICodextnService codextnService,
        //    IUserService userService)
        //{
        //    _db = db;
        //    _getDisplayName = propertyName => Utility.GetDisplayName<PsCardItemVM>(propertyName);
        //    _codextnService = codextnService;
        //    _userService = userService;
        //}

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

            var poYears = _codextnService.GetPoYears();
            var poYear = entity.PoDate.Value.Year.ToString().Trim();
            if (poYears.Where(w => w.Description == poYear && w.Desc2 == "Y").Any())
            {
                throw new RecordLockedException($"Year {poYear} is locked. Cannot delete.");
            }
            else
            {
                if (!poYears.Where(w => w.Description == poYear).Any())
                {
                    throw new RecordLockedException($"No setup found for PO Year {poYear}. Cannot delete.");
                }
            }

            if (_db.PsCardItemTransfers.Any(a => a.ParentId == cardItem.TransferId))
            {
                throw new RecordRelationshipException("Items of this record were transfered to other department/location, cannot delete!");
            }

            if (_db.PsCardItemTransfers.Any(a => a.Id == cardItem.TransferId && a.PsCardItemTransferIssuances.Any()))
            {
                throw new RecordRelationshipException("Items of this record were issued to other department/location, cannot delete!");
            }

            if (cardItem.ParentId == null) // main record
            {
                if (_db.PsCardItems.Any(a => a.Id == cardItem.Id && a.PsCardItemExtns.Any(a2 => a2.IcsParItems.Any())))
                {
                    throw new RecordRelationshipException("Items of this record already have PAR/ICS, cannot delete!");
                }
            }
        }

        public void ValidateFieldsOnCreateUpdate(PsCardItemVM model, Mode mode)
        {
            var ex = new InvalidModelException();
            if (model.DeptId == null)
            {
                ex.UpsertDataList(_getDisplayName(nameof(model.DeptId)), "Field is required.");
            }
            else
            {
                if (!_codextnService.IsValidMastCodeId("LOCATIONS", model.DeptId))
                {
                    ex.UpsertDataList(_getDisplayName(nameof(model.DeptId)), "Invalid value");
                }
            }

            if (string.IsNullOrWhiteSpace(model.Unit))
            {
                ex.UpsertDataList(_getDisplayName(nameof(model.Unit)), "Field is required.");
            }
            else
            {
                if (!_codextnService.IsValidMastCodeCode("UNIT", model.Unit))
                {
                    ex.UpsertDataList(_getDisplayName(nameof(model.Unit)), "Invalid value");
                }
            }

            if (!model.UnitCost.HasValue)
            {
                ex.UpsertDataList(_getDisplayName(nameof(model.UnitCost)), "Field is required.");
            }
            
            if (string.IsNullOrWhiteSpace(model.Description))
            {
                ex.UpsertDataList(_getDisplayName(nameof(model.Description)), "Field is required.");
            }

            if (string.IsNullOrWhiteSpace(model.InvDist))
            {
                ex.UpsertDataList(_getDisplayName(nameof(model.InvDist)), "Field is required.");
            }
            else
            {
                if (!_codextnService.IsValidMastCodeCode("PS-REMARKS", model.InvDist))
                {
                    ex.UpsertDataList(_getDisplayName(nameof(model.InvDist)), "Invalid value");
                }
            }

            if (!model.PoDate.HasValue)
            {
                ex.UpsertDataList(_getDisplayName(nameof(model.PoDate)), "Field is required.");
            }

            if (string.IsNullOrWhiteSpace(model.PoNo))
            {
                ex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), "Field is required.");
            }
            else
            {
                if (model.PoNo.Trim().Length != 12 || model.PoNo.Split('-')[2] == "0000")
                {
                    ex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), "Invalid value.");
                }
                else
                {
                    var psCardItems = _db.PsCardItems.AsNoTracking().Where(w => w.PoNo == model.PoNo);
                    if (mode == Mode.ADD)
                    {
                        // check user
                        if (psCardItems.Any(a => a.InsertedBy != model.InsertedBy))
                        {
                            var isAdmin = _userService.IsUserNameAdmin(model.InsertedBy);
                            if (!isAdmin)
                            {
                                ex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), $"Already created by other user.");
                            }
                        }
                        else
                        {
                            if (psCardItems.Any(a => a.PsCardId == model.PsCardId && a.Description == model.Description)) // check description
                            {
                                ex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), $"Already exists with same description.");
                            }
                        }

                        // check po date
                        if (psCardItems.Any(a => a.PoDate != model.PoDate))
                        {
                            ex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), $"Already exists with different date.");
                        }
                    }
                    else
                    {
                        if (psCardItems.Any(a => a.InsertedBy != model.UpdatedBy))
                        {
                            var isAdmin = _userService.IsUserNameAdmin(model.UpdatedBy);
                            if (!isAdmin)
                            {
                                ex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), $"Can only be modified by it's creator or an admin.");
                            }
                        }
                        else
                        {
                            if (psCardItems.Any(a => a.PsCardId == model.PsCardId && a.Description == model.Description && a.Id != model.Id)) // check description
                            {
                                ex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), $"Already exists with same description");
                            }
                        }
                    }
                }          
            }            

            if (!string.IsNullOrWhiteSpace(model.PoNo) && model.PoDate.HasValue)
            {
                var refNoParts = model.PoNo.Split('-');
                var refNoYear = int.Parse(refNoParts[0]);
                var refNoMonth = int.Parse(refNoParts[1]);
                if (refNoYear != model.PoDate.Value.Year || refNoMonth != model.PoDate.Value.Month)
                {
                    ex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), "Series Year and month must be same as the year and month of the PO date.");
                }                
            }

            if (model.PoDate.HasValue)
            {
                var poYears = _codextnService.GetPoYears();
                var poYear = model.PoDate.Value.Year.ToString().Trim();
                poYears = poYears.Where(w => w.Description == poYear && w.Desc2 != "Y");
                if (!poYears.Any())
                {
                    ex.UpsertDataList(_getDisplayName(nameof(model.PoDate)), $"Entry for Year {poYear} is not allowed.");
                }
            }

            if (model.TransDate.HasValue)
            {
                if (model.TransDate.Value < model.PoDate)
                {
                    ex.UpsertDataList(_getDisplayName(nameof(model.TransDate)), $"Must be on or after the PO Date.");
                }
            }


            if (mode == Mode.EDIT && model.ParentId != null)
            {
                if (!model.TransDate.HasValue)
                {
                    ex.UpsertDataList(_getDisplayName(nameof(model.TransDate)), $"Field is Required.");
                }
                else
                {
                    var issuanceYears = _codextnService.GetIssuanceYears();
                    if (issuanceYears.Any())
                    {
                        var minYear = int.Parse(issuanceYears.Min(m => m.Description));
                        var maxYear = int.Parse(issuanceYears.Max(m => m.Description));

                        if (model.TransDate.Value.Year < minYear)
                        {
                            if (minYear == maxYear)
                            {
                                ex.UpsertDataList(_getDisplayName(nameof(model.TransDate)), $"Year of date transit must be for year {minYear}.");
                            }
                            else
                            {
                                ex.UpsertDataList(_getDisplayName(nameof(model.TransDate)), $"Year of date transit must be from {minYear} to {maxYear}.");
                            }
                        }

                        var year = model.TransDate.Value.Year.ToString().Trim();
                        if (!issuanceYears.Any(a => a.Description == year))
                        {
                            ex.UpsertDataList(_getDisplayName(nameof(model.TransDate)), $"transit for this year is not allowed.");
                        }
                    }
                    else
                    {
                        ex.UpsertDataList(_getDisplayName(nameof(model.TransDate)), $"transit year setup is not a available.");
                    }
                }
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