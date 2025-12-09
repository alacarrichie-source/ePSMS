using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.AllFields;
using iLgs.Services.Codes;
using iLgs.Services.Items;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.CustodianReports
{
    public interface ICustodianReportItemPpeValidator
    {
        Task ValidateOnCreateAsync(CustodianReportItemPpeVM model);
        Task ValidateOnUpdateAsync(CustodianReportItemPpeVM model);
        void ValidateOnDelete(CustodianReportItemPpeVM model);
    }

    public class CustodianReportItemPpeValidator : BaseValidator, ICustodianReportItemPpeValidator
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly ICodextnService _codextnService;        
        private readonly IItemCodeService _itemCodeService;
        private readonly IAllFieldsValidator _allFieldsValidator;

        public CustodianReportItemPpeValidator(AppManEntities db,
            ICodextnService codextnService,
            IItemCodeService itemCodeService,
            IAllFieldsValidator allFieldsValidator)
        {
            _db = db;
            _getDisplayName = Utility.GetDisplayName<CustodianReportItemPpeVM>;
            _codextnService = codextnService;
            _allFieldsValidator = allFieldsValidator;
            _itemCodeService = itemCodeService;
        }

        public async Task ValidateOnCreateAsync(CustodianReportItemPpeVM model)
        {
            ValidateIfNull(model);
            await ValidateFieldsOnCreateUpdateAsync(model, Mode.ADD);
        }

        public async Task ValidateOnUpdateAsync(CustodianReportItemPpeVM model)
        {
            ValidateIfNull(model);
            await ValidateFieldsOnCreateUpdateAsync(model, Mode.EDIT);
        }

        public void ValidateOnDelete(CustodianReportItemPpeVM model)
        {
            ValidateIfNull(model);            
        }

        public async Task ValidateFieldsOnCreateUpdateAsync(CustodianReportItemPpeVM model, Mode mode)
        {
            var ex = new InvalidModelException();
            //var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            //string partialView = AllFieldsUtil.GetPartialView(itemCode);
            string partialView = await _itemCodeService.GetPartialViewAsync(model.ItemCodeId);
            _allFieldsValidator.ValidateAllFieldsPartial(model.AllField, partialView, ex, Module.CARD);

            if (model.MainDeptId == null || model.MainDeptId == Guid.Empty)
            {
                ex.UpsertDataList("Department", "Please select department before creating an entry.");
            }

            if (!string.IsNullOrWhiteSpace(model.ItemSerialNo)) {
                if (mode == Mode.ADD)
                {
                    var entity = await _db.CustodianReportItems.FirstOrDefaultAsync(f => f.ReportId == model.ReportId && f.Fund == model.Fund && f.DeptId == model.DeptId && f.ItemSerialNo == model.ItemSerialNo);
                    if (entity != null)
                    {
                        ex.UpsertDataList(_getDisplayName(nameof(model.ItemSerialNo)), "Duplicate detected.");
                    }
                }
                else if (mode == Mode.EDIT)
                {
                    var entity = await _db.CustodianReportItems.FirstOrDefaultAsync(f => f.ReportId == model.ReportId &&  f.Fund == model.Fund && f.DeptId == model.DeptId && f.ItemSerialNo == model.ItemSerialNo && f.Id != model.Id);
                    if (entity != null)
                    {
                        ex.UpsertDataList(_getDisplayName(nameof(model.ItemSerialNo)), "Duplicate detected.");
                    }
                }
            }            
            ex.ThrowIfContainsErrors();
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