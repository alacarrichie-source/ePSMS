//using iLgs.Exceptions;
//using iLgs.Exceptions.Service;
//using iLgs.Models;
//using iLgs.Services.Codes;
//using iLgs.Services.Requisition;
//using iLgs.Utilities;
//using System;
//using System.Linq;

//namespace iLgs.Services.Validators
//{
//    public interface IRisItemUnitGroupValidator
//    {
//        void ValidateOnCreate(RisItemUnitGroupVM model);
//        void ValidateOnUpdate(RisItemUnitGroupVM model);
//        void ValidateOnDelete(RisItemUnitGroupVM model);
//    }

//    public class RisItemUnitGroupValidator : BaseValidator, IRisItemUnitGroupValidator
//    {        
//        private readonly AppManEntities _db;
//        private readonly GetDisplayNameDelegate _getDisplayName;
//        private readonly ICodextnService _codextnService;
//        private readonly IRisSharedService _risSharedService;

//        public RisItemUnitGroupValidator(AppManEntities db)
//        {
//            _db = db;
//            _getDisplayName = propertyName => Utility.GetDisplayName<RisItemUnitGroupVM>(propertyName);
//            _risSharedService = new RisSharedService(_db);
//            _codextnService = new CodextnService(_db);
//        }

//        //public RisItemUnitGroupValidator(AppManEntities db,
//        //    ICodextnService codextnService,
//        //    IRisService risService)
//        //{
//        //    _db = db;            
//        //    _getDisplayName = propertyName => Utility.GetDisplayName<RisItemUnitGroupVM>(propertyName);
//        //    _risService = risService;
//        //    _codextnService = codextnService;
//        //}

//        //public void ValidateOnCreate(RisItemUnitGroupVM model)
//        //{
//        //    ValidateModel(model);
//        //    ValidateIfPosted((Guid)model.RisId);
//        //    ValidateFieldsOnCreateUpdate(model);
//        //}

//        //public void ValidateOnUpdate(RisItemUnitGroupVM model)
//        //{
//        //    ValidateModel(model);
//        //    ValidateRecord(model.Id);
//        //    ValidateIfPosted((Guid)model.RisId);            
//        //    ValidateFieldsOnCreateUpdate(model);
//        //}

//        //public void ValidateOnDelete(RisItemUnitGroupVM model)
//        //{
//        //    ValidateModel(model);
//        //    ValidateRecord(model.Id);
//        //    ValidateIfPosted((Guid)model.RisId);            
//        //}

//        public void ValidateIfPosted(Guid risId)
//        {
//            var isPosted = _risSharedService.IsPosted(risId);
//            if (isPosted)
//            {
//                throw new RecordAlreadyPostedException();
//            }
//        }

//        //public void ValidateFieldsOnCreateUpdate(RisItemUnitGroupVM model)
//        //{
//        //    var ex = new InvalidModelException();

//        //    if (!model.Qty.HasValue || model.Qty == 0)
//        //    {
//        //        ex.UpsertDataList(_getDisplayName(nameof(model.Qty)), "Field is required.");
//        //    }


//        //    if (string.IsNullOrWhiteSpace(model.Unit))
//        //    {
//        //        ex.UpsertDataList(_getDisplayName(nameof(model.Unit)), "Field is required.");
//        //    }
//        //    else
//        //    {
//        //        if (!_codextnService.IsValidMastCodeCode("UNIT-GROUP", model.Unit))
//        //        {
//        //            ex.UpsertDataList(_getDisplayName(nameof(model.Unit)), "Invalid value");
//        //        }
//        //    }

//        //    //if (string.IsNullOrWhiteSpace(model.Office))
//        //    //{
//        //    //    ex.UpsertDataList(_getDisplayName(nameof(model.Office)), "Field is required.");
//        //    //}
//        //    //else
//        //    //{
//        //    //    if (!_codextnService.IsValidCodeDesc("DEPARTMENTS", model.Office))
//        //    //    {
//        //    //        ex.UpsertDataList(_getDisplayName(nameof(model.Office)), "Invalid value");
//        //    //    }
//        //    //}

//        //    //if (!model.RisDate.HasValue)
//        //    //{
//        //    //    ex.UpsertDataList(_getDisplayName(nameof(model.RisDate)), "Date is required.");
//        //    //}

//        //    //if (string.IsNullOrWhiteSpace(model.Purpose))
//        //    //{
//        //    //    ex.UpsertDataList(_getDisplayName(nameof(model.Purpose)), "Field is required.");
//        //    //}

//        //    ex.ThrowIfContainsErrors();
//        //}

        
//        private void ValidateRecord(Guid id)
//        {
//            if (!_db.RisItemUnitGroups.Any(a => a.Id == id))
//            {
//                throw new NotFoundException(id);
//            }
//        }

//        private static void ValidateModel(RisItemUnitGroupVM card)
//        {
//            if (card is null)
//            {
//                throw new NullException();
//            }
//        }
//    }
//}