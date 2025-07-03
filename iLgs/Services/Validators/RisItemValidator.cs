using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.AllFields;
using iLgs.Services.Codes;
using iLgs.Services.Items;
using iLgs.Services.Requisition;
using iLgs.Utilities;
using System;
using System.Linq;
using static iLgs.Models.Enums;

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
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly ICodextnService _codextnService;
        private readonly IItemCodeService _itemCodeService;
        private readonly IRisService _risService;
        private readonly IAllFieldsValidator _allFieldsValidator;


        public RisItemValidator(AppManEntities db,
            ICodextnService codextnService,
            IItemCodeService itemCodeService,
            IRisService risService,
            IAllFieldsValidator allFieldsValidator)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<RisItemEntryVM>(propertyName);
            _codextnService = codextnService;
            _itemCodeService = itemCodeService;
            _risService = risService;
            _allFieldsValidator = allFieldsValidator;
        }

        public void ValidateOnCreate(RisItemEntryVM model)
        {
            ValidateModel(model);
            ValidateIfPosted((Guid)model.RisId, Mode.ADD);
            ValidateFieldsOnCreateUpdate(model);
        }

        public void ValidateOnUpdate(RisItemEntryVM model)
        {
            ValidateModel(model);
            ValidateRecord(model.Id);
            ValidateIfPosted((Guid)model.RisId, Mode.EDIT);
            ValidateFieldsOnCreateUpdate(model);
        }

        public void ValidateOnDelete(RisItemEntryVM model)
        {
            ValidateModel(model);
            ValidateRecord(model.Id);
            ValidateIfPosted((Guid)model.RisId, Mode.DELETE);

            var pr = _db.Requests.Where(w => w.RequestItems.Any(a => a.RisItemId == model.Id)).FirstOrDefault();
            if (pr != null)
            {
                throw new RecordRelationshipException($"Record is in use by PR No. {pr.PrNo}, cannot delete!");
            }

            if (_db.RisItemUnitGroupDescriptionItems.Any(a => a.RisItemId == model.Id))
            {
                throw new RecordRelationshipException("Record is part of a group, cannot delete!");
            }
        }

        public void ValidateFieldsOnCreateUpdate(RisItemEntryVM model)
        {
            var ex = new InvalidModelException();
            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);
            _allFieldsValidator.ValidateAllFieldsPartial(model.AllField, partialView, ex);

            //_allFieldsValidator.ValidateAllFields(model.AllField, model.PsType, model.ItemCode, ex);
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

        private static void ValidateModel(RisItemEntryVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        public void ValidateIfPosted(Guid risId, Mode mode)
        {
            var isPosted = _risService.IsPosted(risId);
            if (isPosted)
            {
                if (mode == Mode.DELETE)
                {
                    throw new RecordAlreadyPostedException("Record already posted, cannot delete!");
                }                
                else
                {
                    throw new RecordAlreadyPostedException("Record already posted, cannot update!");
                }
            }
        }
    }
}