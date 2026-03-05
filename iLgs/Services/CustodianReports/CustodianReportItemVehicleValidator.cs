using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.AllFields;
using iLgs.Services.Codes;
using iLgs.Services.Items;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.CustodianReports
{
    public interface ICustodianReportItemVehicleValidator
    {
        Task ValidateOnCreateAsync(CustodianReportItemVehicleVM model);
        Task ValidateOnUpdateAsync(CustodianReportItemVehicleVM model);
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
            _getDisplayName = Utility.GetDisplayName<CustodianReportItemVehicleVM>;
            _codextnService = new CodextnService(_db);
            _allFieldsValidator = new AllFieldsValidator(_db);
            _itemCodeService = new ItemCodeService(_db);
        }

        //public CustodianReportItemVehicleValidator(AppManEntities db,
        //    ICodextnService codextnService,
        //    IItemCodeService itemCodeService,
        //    IAllFieldsValidator allFieldsValidator)
        //{
        //    _db = db;
        //    _getDisplayName = Utility.GetDisplayName<CustodianReportItemVehicleVM>;
        //    _codextnService = codextnService;
        //    _allFieldsValidator = allFieldsValidator;
        //    _itemCodeService = itemCodeService;
        //}

        public async Task ValidateOnCreateAsync(CustodianReportItemVehicleVM model)
        {
            ValidateIfNull(model);
            await ValidateFieldsOnCreateUpdateAsync(model, Mode.ADD);
        }

        public async Task ValidateOnUpdateAsync(CustodianReportItemVehicleVM model)
        {
            ValidateIfNull(model);
            await ValidateFieldsOnCreateUpdateAsync(model, Mode.EDIT);
        }

        public void ValidateOnDelete(CustodianReportItemVehicleVM model)
        {
            ValidateIfNull(model);
            ValidateRecord(model.Id);
        }

        public async Task ValidateFieldsOnCreateUpdateAsync(CustodianReportItemVehicleVM model, Mode mode)
        {
            var ex = new InvalidModelException();
            //var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            //string partialView = AllFieldsUtil.GetPartialView(itemCode);
            string partialView = await _itemCodeService.GetPartialViewAsync(model.ItemCodeId);
            _allFieldsValidator.ValidateAllFieldsPartial(model.AllField, partialView, ex, Enums.Module.CARD);

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