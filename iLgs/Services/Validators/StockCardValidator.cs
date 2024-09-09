using FluentValidation;
using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.CategoryEnum;

namespace iLgs.Services.Validators
{
    public interface IStockCardValidator
    {
        void ValidateOnCreate(StockCardVM card);
    }

    public class StockCardValidator : BaseValidator, IStockCardValidator
    {
        private readonly AppManEntities _db;
        public StockCardValidator(AppManEntities db)
        {
            _db = db;
        }
        public void ValidateOnCreate(StockCardVM card)
        {
            ValidateCard(card);
            Validate(
                (Rule: IsInvalid(text: card.Fund), Parameter: Utility.GetDisplayName<StockCardVM>(nameof(StockCardVM.Fund))),
                (Rule: IsInvalid(text: card.PsNo), Parameter: Utility.GetDisplayName<StockCardVM>(nameof(StockCardVM.PsNo)))
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
            ValidateAllFields(card);
            //ValidateCreatedSignature(card);
            //ValidateCreatedDateIsRecent(card);
        }

        public void ValidateAllFields(StockCardVM model)
        {
            var ex = new InvalidModelException();
            var af = model.AllField;

            if (Enum.TryParse(model.ItemTypeCode, out Category c))
            {
                if (c == CatDrugs())
                {
                    if (string.IsNullOrWhiteSpace(af.GenericName))
                    {
                        ex.UpsertDataList(Utility.GetDisplayName<AllField>(nameof(af.GenericName)), "Text is required.");
                    }

                    if (model.ItemNo.Substring(0, 4) == "5.1.") // Alcoh1ol
                    {
                        if (string.IsNullOrWhiteSpace(af.DosageVolume))
                        {
                            ex.UpsertDataList(Utility.GetDisplayName<AllField>(nameof(af.DosageVolume)), "Text is required.");
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(af.DosageStrength))
                        {
                            ex.UpsertDataList(Utility.GetDisplayName<AllField>(nameof(af.DosageStrength)), "Text is required.");
                        }
                        if (string.IsNullOrWhiteSpace(af.DosageForm))
                        {
                            ex.UpsertDataList(Utility.GetDisplayName<AllField>(nameof(af.DosageForm)), "Text is required.");
                        }
                    }
                }
                else if (c == CatMachineries()
                    || c == CatTransportations()
                    || c == CatFurnitures()
                    || c == CatOtherProperties()
                    || c == CatMedicals()
                    || c == CatAgriculturals()
                    || c == CatAnimalSupplies()
                    || c == CatConstructionMaterials()
                    || c == CatOfficeSupplies()
                    || c == CatAccountableForms()
                    || c == CatNonAccountableForns()
                    || c == CatMilitaries()
                    || c == CatOtherSupplies())
                {
                    if (string.IsNullOrWhiteSpace(af.Brand))
                    {
                        ex.UpsertDataList(Utility.GetDisplayName<AllField>(nameof(af.Brand)), "Text is required.");
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(af.Model_))
                        {
                            ex.UpsertDataList(Utility.GetDisplayName<AllField>(nameof(af.Model_)), "Text is required.");
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(af.Dimension))
                            {
                                ex.UpsertDataList(Utility.GetDisplayName<AllField>(nameof(af.Dimension)), "Text is required.");
                            }
                            else
                            {
                                if (string.IsNullOrWhiteSpace(af.Size))
                                {
                                    ex.UpsertDataList(Utility.GetDisplayName<AllField>(nameof(af.Size)), "Text is required.");
                                }
                                else
                                {
                                    if (string.IsNullOrWhiteSpace(af.Weight))
                                    {
                                        ex.UpsertDataList(Utility.GetDisplayName<AllField>(nameof(af.Weight)), "Text is required.");
                                    }
                                    else
                                    {
                                        if (string.IsNullOrWhiteSpace(af.Materials))
                                        {
                                            ex.UpsertDataList(Utility.GetDisplayName<AllField>(nameof(af.Materials)), "Text is required.");
                                        }
                                        else
                                        {
                                            if (string.IsNullOrWhiteSpace(af.Capacity))
                                            {
                                                ex.UpsertDataList(Utility.GetDisplayName<AllField>(nameof(af.Capacity)), "Text is required.");
                                            }
                                            else
                                            {
                                                if (af.Color.IsNullOrWhiteSpaceX())
                                                {
                                                    ex.UpsertDataList(Utility.GetDisplayName<AllField>(nameof(af.Color)), "Text is required.");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
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