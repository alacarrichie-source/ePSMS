using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.AllFields;
using iLgs.Services.Codes;
using iLgs.Services.Items;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Services.CustodianReports
{
    public interface ICustodianReportItemVehicleValidator
    {
        void ValidateOnCreate(CustodianReportItemVehicleVM model);
        void ValidateOnUpdate(CustodianReportItemVehicleVM model);
        void ValidateOnDelete(CustodianReportItemVehicleVM model);
    }

    public class CustodianReportItemVehicleValidator : BaseValidator, ICustodianReportItemVehicleValidator
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly ICodextnService _codextnService;
        private readonly IAllFieldsValidator _allFieldsValidator;
        private readonly IItemCodeService _itemCodeService;


        public CustodianReportItemVehicleValidator(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<CustodianReportItemVehicleVM>(propertyName);
            _codextnService = new CodextnService(_db);
            _allFieldsValidator = new AllFieldsValidator(_db);
            _itemCodeService = new ItemCodeService(_db);
        }

        public void ValidateOnCreate(CustodianReportItemVehicleVM model)
        {
            ValidateIfNull(model);
            ValidateFieldsOnCreateUpdate(model);
        }

        public void ValidateOnUpdate(CustodianReportItemVehicleVM model)
        {
            ValidateIfNull(model);
            ValidateFieldsOnCreateUpdate(model);
        }

        public void ValidateOnDelete(CustodianReportItemVehicleVM model)
        {
            ValidateIfNull(model);
            ValidateRecord(model.Id);
        }

        public void ValidateFieldsOnCreateUpdate(CustodianReportItemVehicleVM model)
        {
            var ex = new InvalidModelException();
            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);
            _allFieldsValidator.ValidateAllFieldsPartial(model.AllField, partialView, ex, Enums.Module.CARD);

            //_allFieldsValidator.ValidateAllFields(model.AllField, model.ItemType_Code, model.Item_Code, ex, Enums.Module.CARD);


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

            //if (string.IsNullOrWhiteSpace(model.PlateNo) && string.IsNullOrWhiteSpace(model.ConductionNo))
            //{
            //    ex.UpsertDataList($"{_getDisplayName(nameof(model.PlateNo))} or {_getDisplayName(nameof(model.ConductionNo))}", "Field is required.");
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
            if (!_db.CustodianReportItems.Any(a => a.Id == id))
            {
                throw new NotFoundException(id);
            }
        }

        private static void ValidateIfNull(CustodianReportItemVehicleVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }
    }
}