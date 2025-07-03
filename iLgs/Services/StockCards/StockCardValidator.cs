using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.AllFields;
using iLgs.Services.Items;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System.Linq;
using static iLgs.Models.Enums;

namespace iLgs.Services.StockCards
{
    public interface IStockCardValidator
    {
        bool IsPsNoAlreadyExists(StockCardVM model, Mode mode);
        void ValidateOnCreate(StockCardVM model);
        void ValidateOnUpdate(StockCardVM model);
        void ValidateOnDelete(StockCardVM model);
    }

    public class StockCardValidator : BaseValidator, IStockCardValidator
    {
        //private delegate string GetDisplayNameDelegate(string propertyName);
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly AppManEntities _db;
        private readonly IAllFieldsValidator _allFieldsValidator;
        private readonly IItemCodeService _itemCodeService;
        
        public StockCardValidator(AppManEntities db,
            IAllFieldsValidator allFieldsValidator,
            IItemCodeService itemCodeService)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<StockCardVM>(propertyName);
            _allFieldsValidator = allFieldsValidator;
            _itemCodeService = itemCodeService;            
        }
        public void ValidateOnCreate(StockCardVM model)
        {                       
            ValidateCard(model);            
            Validate(
                (Rule: IsInvalid(text: model.Fund), Parameter: _getDisplayName(nameof(StockCardVM.Fund))),
                (Rule: IsInvalid(text: model.PsNo), Parameter: _getDisplayName(nameof(StockCardVM.PsNo)))
                //(Rule: Field.IsInvalid(card.Status), Parameter: nameof(Course.Status)),
                //(Rule: Field.IsInvalid(card.CreatedDate), Parameter: nameof(Course.CreatedDate)),
                //(Rule: Field.IsInvalid(card.UpdatedDate), Parameter: nameof(Course.UpdatedDate)),
                //(Rule: Field.IsInvalid(id: card.CreatedBy), Parameter: nameof(Course.CreatedBy)),
                //(Rule: Field.IsInvalid(id: card.UpdatedBy), Parameter: nameof(Course.UpdatedBy)),
                //(Rule: Field.IsNotRecent(card.CreatedDate), Parameter: nameof(Course.CreatedDate)),

                //(Rule: IsNotSame(
                //    firstId: card.UpdatedBy,
                //    secondId: card.CreatedBy,
                //    secondIdName: nameof(Course.CreatedBy)),
                //Parameter: nameof(Course.UpdatedBy)),

                //(Rule: IsNotSame(
                //    firstDate: card.UpdatedDate,
                //    secondDate: card.CreatedDate,
                //    secondDateName: nameof(Course.CreatedDate)),
                //Parameter: nameof(Course.UpdatedDate))
                );
            
            //ValidateCreatedSignature(card);
            //ValidateCreatedDateIsRecent(card);
            var ex = new InvalidModelException();
            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);
            _allFieldsValidator.ValidateAllFieldsPartial(model.AllField, partialView, ex, Module.CARD);

            if (IsPsNoAlreadyExists(model, Mode.ADD))
            {
                ex.UpsertDataList(Utility.GetDisplayName<StockCardVM>(nameof(model.PsNo)), "Already exists.");
            }

            ex.ThrowIfContainsErrors();
        }
        

        public void ValidateOnUpdate(StockCardVM model)
        {
            ValidateCard(model);
            ValidateIfPosted(model);
            Validate(
                (Rule: IsInvalid(text: model.Fund), Parameter: _getDisplayName(nameof(StockCardVM.Fund))),
                (Rule: IsInvalid(text: model.PsNo), Parameter: _getDisplayName(nameof(StockCardVM.PsNo)))                
                );
            
            var ex = new InvalidModelException();
            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);
            _allFieldsValidator.ValidateAllFieldsPartial(model.AllField, partialView, ex, Module.CARD);

            //if (_db.PsCards.Any(a => a.PsNo == model.PsNo && a.Fund == model.Fund && a.Id != model.Id))
            //{
            //    ex.UpsertDataList(Utility.GetDisplayName<StockCardVM>(nameof(model.PsNo)), "Already exists.");
            //}
            if (IsPsNoAlreadyExists(model, Mode.EDIT))
            {
                ex.UpsertDataList(Utility.GetDisplayName<StockCardVM>(nameof(model.PsNo)), "Already exists.");
            }
            ex.ThrowIfContainsErrors();
        }

        public bool IsPsNoAlreadyExists(StockCardVM model, Mode mode)
        {
            if (mode == Mode.ADD) {
                return _db.PsCards.Any(a => a.PsNo == model.PsNo && a.Fund == model.Fund);
            }
            return _db.PsCards.Any(a => a.PsNo == model.PsNo && a.Fund == model.Fund && a.Id != model.Id);
        }

        public void ValidateOnDelete(StockCardVM model)
        {
            ValidateCard(model);
            ValidateIfPosted(model);
            if (_db.PsCardItems.Any(a => a.PsCardId == model.Id))
            {
                throw new RecordRelationshipException("Cannot delete card with items, please delete the items first.");
            }
        }

        public void ValidateIfPosted(StockCardVM model)
        {
            var entity = _db.PsCards.Find(model.Id);
            if (entity != null && entity.PostedDt != null)
            {
                var msg = $"Record already posted by {entity.PostedBy} on {entity.PostedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        //private void ValidateCourseOnModify(Course course)
        //{
        //    ValidateCourseOnCreate(course);
        //    ValidateCourseId(course.Id);
        //    ValidateCourseStrings(course);
        //    ValidateCourseIds(course);
        //    ValidateCourseDates(course);
        //    ValidateDatesAreNotSame(course);
        //    ValidateUpdatedDateIsRecent(course);
        //}

        private static void ValidateCard(StockCardVM card)
        {
            if (card is null)
            {
                throw new NullException();
            }
        }



        //private static void ValidateAgainstStorageOnModify(StockCardVM input, StockCardVM storage)
        //{
        //    switch (input)
        //    {
        //        case { } when input.InsertedDt != storage.InsertedDt:
        //            throw new InvalidValueException(parameterName: nameof(storage.InsertedBy),
        //                            parameterValue: inputCourse.CreatedDate);
        //        case { } when inputCourse.CreatedBy != storage.CreatedBy:
        //                                throw new InvalidCourseException(
        //                                                            parameterName: nameof(storage.CreatedBy),
        //                                                                                    parameterValue: inputCourse.CreatedBy);
        //        case { } when inputCourse.UpdatedDate == storage.UpdatedDate:
        //                                throw new InvalidCourseException(
        //                                                            parameterName: nameof(storage.UpdatedDate),
        //                                                                                    parameterValue: inputCourse.UpdatedDate);
        //    }
        //}
        //private dynamic IsNotRecent(DateTimeOffset dateTimeOffset) => new
        //{
        //    Condition = IsDateNotRecent(dateTimeOffset),
        //    Message = "Date is not recent"
        //};        
    }
}