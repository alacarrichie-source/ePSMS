using FluentValidation;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services.Validators
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
            ValidateFieldsOnCreateUpdate(cardItem);            
        }

        public void ValidateOnUpdate(PsCardItemVM cardItem)
        {
            ValidateCard(cardItem);
            ValidateRecord(cardItem.Id);
            ValidateFieldsOnCreateUpdate(cardItem);
        }

        public void ValidateOnDelete(PsCardItemVM cardItem)
        {
            ValidateCard(cardItem);
            ValidateRecord(cardItem.Id);

            if (_db.PsCardItems.Any(a => a.Id == cardItem.Id && a.PsCardItemTransfers.Any()))
            {
                throw new RecordRelationshipException("Items of this record were transfered to other department/location, cannot delete!");
            }

            if (_db.PsCardItems.Any(a => a.Id == cardItem.Id && a.PsCardItemExtns.Any(a2 => a2.IcsParItems.Any())))
            {
                throw new RecordRelationshipException("Items of this record already have PAR/ICS, cannot delete!");
            }
        }

        public void ValidateFieldsOnCreateUpdate(PsCardItemVM cardItem)
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
            ex.ThrowIfContainsErrors();
        }
        
        private void ValidateRecord(Guid id)
        {
            if (!_db.PsCardItems.Any(a => a.Id == id))
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
        
    }
}