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
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services.CustodianReports
{
    public interface ICustodianReportItemStockValidator
    {
        Task ValidateOnCreateAsync(CustodianReportItemStockVM model);
        Task ValidateOnUpdateAsync(CustodianReportItemStockVM model);
        void ValidateOnDelete(CustodianReportItemStockVM model);
    }

    public class CustodianReportItemStockValidator : BaseValidator, ICustodianReportItemStockValidator
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly ICodextnService _codextnService;
        private readonly IAllFieldsValidator _allFieldsValidator;
        private readonly IItemCodeService _itemCodeService;

        public CustodianReportItemStockValidator(AppManEntities db,
            ICodextnService codextnService,
            IItemCodeService itemCodeService,
            IAllFieldsValidator allFieldsValidator)
        {
            _db = db;
            _getDisplayName = Utility.GetDisplayName<CustodianReportItemStockVM>;
            _codextnService = codextnService;
            _allFieldsValidator = allFieldsValidator;
            _itemCodeService = itemCodeService;
        }

        public async Task ValidateOnCreateAsync(CustodianReportItemStockVM model)
        {
            ValidateIfNull(model);
            await ValidateFieldsOnCreateUpdateAsync(model, Mode.ADD);
        }

        public async Task ValidateOnUpdateAsync(CustodianReportItemStockVM model)
        {
            ValidateIfNull(model);
            await ValidateFieldsOnCreateUpdateAsync(model, Mode.EDIT);
        }

        public void ValidateOnDelete(CustodianReportItemStockVM model)
        {
            ValidateIfNull(model);            
        }

        public async Task ValidateFieldsOnCreateUpdateAsync(CustodianReportItemStockVM model, Mode mode)
        {
            var ex = new InvalidModelException();
            var itemCode = await _itemCodeService.GetByIdAsync(model.ItemCodeId);

            if (itemCode == null)
            {
                ex.UpsertDataList("ItemCode", "Invalid Value.");
            }
            else
            {
                //string partialView = AllFieldsUtil.GetPartialView(itemCode);
                string partialView = await _itemCodeService.GetPartialViewAsync(model.ItemCodeId);
                _allFieldsValidator.ValidateAllFieldsPartial(model.AllField, partialView, ex, Module.CARD);
            }

            if (model.MainDeptId == null || model.MainDeptId == Guid.Empty)
            {
                ex.UpsertDataList("Department", "Please select department before creating an entry.");
            }

            if (!string.IsNullOrWhiteSpace(model.ItemSerialNo))
            {
                if (mode == Mode.ADD)
                {
                    var entity = await _db.CustodianReportItems.FirstOrDefaultAsync(f => f.Fund == model.Fund && f.DeptId == model.DeptId && f.ItemSerialNo == model.ItemSerialNo);
                    if (entity != null)
                    {
                        ex.UpsertDataList(_getDisplayName(nameof(model.SerialNo)), "Duplicate detected.");
                    }
                }
                else if (mode == Mode.EDIT)
                {
                    var entity = await _db.CustodianReportItems.FirstOrDefaultAsync(f => f.Fund == model.Fund && f.DeptId == model.DeptId && f.ItemSerialNo == model.ItemSerialNo && f.Id != model.Id);
                    if (entity != null)
                    {
                        ex.UpsertDataList(_getDisplayName(nameof(model.SerialNo)), "Duplicate detected.");
                    }
                }
            }
            
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