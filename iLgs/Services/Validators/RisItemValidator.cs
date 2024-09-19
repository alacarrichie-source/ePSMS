using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;

namespace iLgs.Services.Validators
{
    public interface IRisItemValidator
    {
        void ValidateOnCreate(RisItemEntryVM model);
        void ValidateOnUpdate(RisItemEntryVM model);
        void ValidateOnDelete(RisItemEntryVM model);        
    }

    public class RisItemValidator: BaseValidator, IRisItemValidator
    {
        private delegate string GetDisplayNameDelegate(string propertyName);
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly ICodextnService _codextnService;
        private readonly IAllFieldsValidator _allFieldsValidator;

        public RisItemValidator(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<RisItemEntryVM>(propertyName);
            _codextnService = new CodextnService(db);
            _allFieldsValidator = new AllFieldsValidator(db);
        }

        public void ValidateOnCreate(RisItemEntryVM model)
        {
            ValidateIfNull(model);            
            ValidateFieldsOnCreateUpdate(model);
        }

        public void ValidateOnUpdate(RisItemEntryVM model)
        {
            ValidateIfNull(model);            
            ValidateFieldsOnCreateUpdate(model);
        }

        public void ValidateOnDelete(RisItemEntryVM model)
        {
            ValidateIfNull(model);
            ValidateRecord(model.Id);
        }

        public void ValidateFieldsOnCreateUpdate(RisItemEntryVM model)
        {
            var ex = new InvalidModelException();
            _allFieldsValidator.ValidateAllFields(model.AllField, model.PsType, model.ItemNo, ex);
            if (!model.QtyRequest.HasValue || model.QtyRequest == 0)
            {
                ex.UpsertDataList(_getDisplayName(nameof(model.QtyRequest)), "Field is required.");
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
            
            ex.ThrowIfContainsErrors();
        }        

        private void ValidateRecord(Guid id)
        {
            if (!_db.RisItems.Any(a => a.Id == id))
            {
                throw new NotFoundException(id);
            }
        }

        private static void ValidateIfNull(RisItemEntryVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }
    }
}