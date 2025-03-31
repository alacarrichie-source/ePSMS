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
using static iLgs.Models.Enums;

namespace iLgs.Services.CustodianReports
{
    public interface ICustodianReportItemStockValidator
    {
        void ValidateOnCreate(CustodianReportItemStockVM model);
        void ValidateOnUpdate(CustodianReportItemStockVM model);
        void ValidateOnDelete(CustodianReportItemStockVM model);
    }

    public class CustodianReportItemStockValidator : BaseValidator, ICustodianReportItemStockValidator
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly ICodextnService _codextnService;
        private readonly IAllFieldsValidator _allFieldsValidator;
        private readonly IItemCodeService _itemCodeService;

        public CustodianReportItemStockValidator(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<CustodianReportItemStockVM>(propertyName);
            _codextnService = new CodextnService(_db);
            _allFieldsValidator = new AllFieldsValidator(_db);
            _itemCodeService = new ItemCodeService(_db);
        }

        public void ValidateOnCreate(CustodianReportItemStockVM model)
        {
            ValidateIfNull(model);
            ValidateFieldsOnCreateUpdate(model, Mode.ADD);
        }

        public void ValidateOnUpdate(CustodianReportItemStockVM model)
        {
            ValidateIfNull(model);
            ValidateFieldsOnCreateUpdate(model, Mode.EDIT);
        }

        public void ValidateOnDelete(CustodianReportItemStockVM model)
        {
            ValidateIfNull(model);            
        }

        public void ValidateFieldsOnCreateUpdate(CustodianReportItemStockVM model, Mode mode)
        {
            var ex = new InvalidModelException();
            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);
            _allFieldsValidator.ValidateAllFieldsPartial(model.AllField, partialView, ex, Module.CARD);

            if (model.MainDeptId == null || model.MainDeptId == Guid.Empty)
            {
                ex.UpsertDataList("Department", "Please select department before creating an entry.");
            }

            if (!string.IsNullOrWhiteSpace(model.ItemSerialNo))
            {
                if (mode == Mode.ADD)
                {
                    var entity = _db.CustodianReportItems.FirstOrDefault(f => f.Fund == model.Fund && f.DeptId == model.DeptId && f.ItemSerialNo == model.ItemSerialNo);
                    if (entity != null)
                    {
                        ex.UpsertDataList(_getDisplayName(nameof(model.SerialNo)), "Duplicate detected.");
                    }
                }
                else if (mode == Mode.EDIT)
                {
                    var entity = _db.CustodianReportItems.FirstOrDefault(f => f.Fund == model.Fund && f.DeptId == model.DeptId && f.ItemSerialNo == model.ItemSerialNo && f.Id != model.Id);
                    if (entity != null)
                    {
                        ex.UpsertDataList(_getDisplayName(nameof(model.SerialNo)), "Duplicate detected.");
                    }
                }
            }

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
        
        private static void ValidateIfNull(CustodianReportItemStockVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateIfPosted(CustodianReportItemStockVM entity)
        {
            if (entity.PostedDt != null)
            {
                var msg = $"Record already posted by {entity.PostedBy} on {entity.PostedDt}";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        private void ValidateIfNotPosted(CustodianReportItemStockVM entity)
        {
            if (entity.PostedDt == null)
            {
                throw new RecordNotYetPostedException($"Record is not yet posted!");
            }
        }
    }
}