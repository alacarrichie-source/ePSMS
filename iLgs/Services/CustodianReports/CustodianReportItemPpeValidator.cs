using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.AllFields;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Services.CustodianReports
{
    public interface ICustodianReportItemPpeValidator
    {
        void ValidateOnCreate(CustodianReportItemPpeVM model);
        void ValidateOnUpdate(CustodianReportItemPpeVM model);
        void ValidateOnDelete(CustodianReportItemPpeVM model);
    }

    public class CustodianReportItemPpeValidator : BaseValidator, ICustodianReportItemPpeValidator
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly ICodextnService _codextnService;
        private readonly IAllFieldsValidator _allFieldsValidator;

        public CustodianReportItemPpeValidator(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<CustodianReportItemPpeVM>(propertyName);
            _codextnService = new CodextnService(_db);
            _allFieldsValidator = new AllFieldsValidator(_db);
        }

        public void ValidateOnCreate(CustodianReportItemPpeVM model)
        {
            ValidateIfNull(model);
            ValidateFieldsOnCreateUpdate(model);
        }

        public void ValidateOnUpdate(CustodianReportItemPpeVM model)
        {
            ValidateIfNull(model);
            ValidateFieldsOnCreateUpdate(model);
        }

        public void ValidateOnDelete(CustodianReportItemPpeVM model)
        {
            ValidateIfNull(model);
            ValidateRecord(model.Id);
        }

        public void ValidateFieldsOnCreateUpdate(CustodianReportItemPpeVM model)
        {
            var ex = new InvalidModelException();
            _allFieldsValidator.ValidateAllFields(model.AllField, model.ItemType_Code, model.Item_Code, ex, Enums.Module.CARD);


            if (model.DeptId == null)
            {
                ex.UpsertDataList(_getDisplayName(nameof(model.DeptId)), "Field is required.");
            }
            else
            {
                if (!_codextnService.IsValidMastCodeId("DEPARTMENTS", model.DeptId))
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

            //if (model.DeptId != null)
            //{
            //    if (model.LocationId != null)
            //    {
            //        if (!model.TransferIn.HasValue)
            //        {
            //            ex.UpsertDataList(_getDisplayName(nameof(model.TransferIn)), "Field is required.");
            //        }
            //    }
            //    else
            //    {
            //        if (!model.Qty.HasValue)
            //        {
            //            ex.UpsertDataList(_getDisplayName(nameof(model.Qty)), "Field is required.");
            //        }
            //    }
            //}

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

            ex.ThrowIfContainsErrors();
        }

        private void ValidateRecord(Guid id)
        {
            if (!_db.CustodianReportItems.Any(a => a.Id == id))
            {
                throw new NotFoundException(id);
            }
        }

        private static void ValidateIfNull(CustodianReportItemPpeVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }
    }
}