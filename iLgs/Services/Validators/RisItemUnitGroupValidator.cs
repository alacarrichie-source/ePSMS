using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Services.Validators
{
    public interface IRisItemUnitGroupValidator
    {
        void ValidateOnCreate(RisItemUnitGroupVM model);
        void ValidateOnUpdate(RisItemUnitGroupVM model);
        void ValidateOnDelete(RisItemUnitGroupVM model);
    }

    public class RisItemUnitGroupValidator : BaseValidator, IRisItemUnitGroupValidator
    {
        private delegate string GetDisplayNameDelegate(string propertyName);
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly ICodextnService _codextnService;
        private IRisService _risService;
        public RisItemUnitGroupValidator(AppManEntities db)
        {
            _db = db;
            _risService = new RisService(_db);
            _getDisplayName = propertyName => Utility.GetDisplayName<RisItemUnitGroupVM>(propertyName);
            _codextnService = new CodextnService(_db);
        }

        public void ValidateOnCreate(RisItemUnitGroupVM model)
        {
            ValidateModel(model);
            ValidateIfPosted((Guid)model.RisId);
            ValidateFieldsOnCreateUpdate(model);
        }

        public void ValidateOnUpdate(RisItemUnitGroupVM model)
        {
            ValidateModel(model);
            ValidateRecord(model.Id);
            ValidateIfPosted((Guid)model.RisId);            
            ValidateFieldsOnCreateUpdate(model);
        }

        public void ValidateOnDelete(RisItemUnitGroupVM model)
        {
            ValidateModel(model);
            ValidateRecord(model.Id);
            ValidateIfPosted((Guid)model.RisId);            
        }

        public void ValidateIfPosted(Guid risId)
        {
            if (_risService.IsPostedAsync(risId).Result)
            {
                throw new RecordAlreadyPostedException();
            }
        }

        public void ValidateFieldsOnCreateUpdate(RisItemUnitGroupVM model)
        {
            var ex = new InvalidModelException();

            //if (string.IsNullOrWhiteSpace(model.Fund))
            //{
            //    ex.UpsertDataList(_getDisplayName(nameof(model.Fund)), "Field is required.");
            //}
            //else
            //{
            //    if (!_codextnService.IsValidMastCodeCode("UNIT", model.Fund))
            //    {
            //        ex.UpsertDataList(_getDisplayName(nameof(model.Fund)), "Invalid value");
            //    }
            //}

            //if (string.IsNullOrWhiteSpace(model.Office))
            //{
            //    ex.UpsertDataList(_getDisplayName(nameof(model.Office)), "Field is required.");
            //}
            //else
            //{
            //    if (!_codextnService.IsValidCodeDesc("DEPARTMENTS", model.Office))
            //    {
            //        ex.UpsertDataList(_getDisplayName(nameof(model.Office)), "Invalid value");
            //    }
            //}

            //if (!model.RisDate.HasValue)
            //{
            //    ex.UpsertDataList(_getDisplayName(nameof(model.RisDate)), "Date is required.");
            //}

            //if (string.IsNullOrWhiteSpace(model.Purpose))
            //{
            //    ex.UpsertDataList(_getDisplayName(nameof(model.Purpose)), "Field is required.");
            //}

            ex.ThrowIfContainsErrors();
        }

        
        private void ValidateRecord(Guid id)
        {
            if (!_db.RISses.Any(a => a.Id == id))
            {
                throw new NotFoundException(id);
            }
        }

        private static void ValidateModel(RisItemUnitGroupVM card)
        {
            if (card is null)
            {
                throw new NullException();
            }
        }
    }
}