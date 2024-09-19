using FluentValidation;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services.Validators
{
    public interface IStockCardValidator
    {
        void ValidateOnCreate(StockCardVM model);
        void ValidateOnUpdate(StockCardVM model);
    }

    public class StockCardValidator : BaseValidator, IStockCardValidator
    {
        private delegate string GetDisplayNameDelegate(string propertyName);
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly AppManEntities _db;
        private readonly IAllFieldsValidator _allFieldsValidator;

        public StockCardValidator(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<StockCardVM>(propertyName);
            _allFieldsValidator = new AllFieldsValidator(db);
        }
        public void ValidateOnCreate(StockCardVM model)
        {                       
            ValidateCard(model);            
            Validate(
                (Rule: IsInvalid(text: model.Fund), Parameter: _getDisplayName(nameof(StockCardVM.Fund))),
                (Rule: IsInvalid(text: model.PsNo), Parameter: _getDisplayName(nameof(StockCardVM.PsNo)))
                //(Rule: Field.IsInvalid(text: card.Description), Parameter: nameof(Course.Description)),
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
            _allFieldsValidator.ValidateAllFields(model.AllField, model.ItemTypeCode, model.ItemNo, ex);
            if (_db.PsCards.Any(a => a.PsNo == model.PsNo))
            {
                ex.UpsertDataList(_getDisplayName(nameof(model.PsNo)), "Already exits.");
            }
            ex.ThrowIfContainsErrors();
        }
        

        public void ValidateOnUpdate(StockCardVM model)
        {
            ValidateCard(model);
            Validate(
                (Rule: IsInvalid(text: model.Fund), Parameter: _getDisplayName(nameof(StockCardVM.Fund))),
                (Rule: IsInvalid(text: model.PsNo), Parameter: _getDisplayName(nameof(StockCardVM.PsNo)))                
                );
            
            var ex = new InvalidModelException();
            _allFieldsValidator.ValidateAllFields(model.AllField, model.ItemTypeCode, model.ItemNo, ex);
            if (_db.PsCards.Any(a => a.PsNo == model.PsNo && a.Id != model.Id))
            {
                ex.UpsertDataList(Utility.GetDisplayName<StockCardVM>(nameof(model.PsNo)), "Already exits.");
            }
            ex.ThrowIfContainsErrors();
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