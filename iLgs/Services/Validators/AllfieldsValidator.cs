using FluentValidation;
using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static iLgs.Models.CategoryEnum;

namespace iLgs.Services.Validators
{    
    public class AllFieldsValidator : AbstractValidator<PsCardVM>
    {
        private readonly AppManEntities _db;
        public AllFieldsValidator(AppManEntities db)
        {
            _db = db;

            RuleFor(x => x.AllField).Custom((af, context) =>
            {
                var model = context.InstanceToValidate as PsCardVM;

                if (Enum.TryParse(model.ItemTypeCode, out Category c))
                {
                    if (c == CatDrugs())
                    {
                        RuleFor(x => af.GenericName).NotEmpty().WithMessage("Generic Name is Required!");

                        if (model.ItemNo.StartsWith("5.1."))
                        {
                            RuleFor(x => af.DosageVolume).NotEmpty().WithMessage("Dosage Volume is Required!");
                        }
                        else
                        {
                            RuleFor(x => af.DosageStrength).NotEmpty().WithMessage("Dosage Strength is Required!");
                            RuleFor(x => af.DosageForm).NotEmpty().WithMessage("Dosage Form is Required!");
                        }
                    }
                    else if (IsMachineryOrOtherCategory(c))
                    {
                        RuleFor(x => af.Brand).NotEmpty().WithMessage("Brand is Required!");
                        RuleFor(x => af.Model_).NotEmpty().WithMessage("Model is Required!");
                        RuleFor(x => af.Dimension).NotEmpty().WithMessage("Dimension is Required!");
                        RuleFor(x => af.Size).NotEmpty().WithMessage("Size is Required!");
                        RuleFor(x => af.Weight).NotEmpty().WithMessage("Weight is Required!");
                        RuleFor(x => af.Materials).NotEmpty().WithMessage("Materials is Required!");
                        RuleFor(x => af.Capacity).NotEmpty().WithMessage("Capacity is Required!");
                        RuleFor(x => af.Color).NotEmpty().WithMessage("Color is Required!");

                        //RuleFor(x => af.Model_)
                        //    .Must((m, size) => !string.IsNullOrWhiteSpace(af.Model_) || !string.IsNullOrWhiteSpace(af.Size) ||
                        //                       !string.IsNullOrWhiteSpace(af.Dimension) || !string.IsNullOrWhiteSpace(af.Weight) ||
                        //                       !string.IsNullOrWhiteSpace(af.Materials) || !string.IsNullOrWhiteSpace(af.Capacity) ||
                        //                       !string.IsNullOrWhiteSpace(af.Color))
                        //    .WithMessage("Model or Dimension or Size or Weight or Materials or Capacity or Color is Required!");
                    }
                }
            });
        }

        private bool IsMachineryOrOtherCategory(Category c)
        {
            return c == CatMachineries()
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
                || c == CatOtherSupplies();
        }
    }

}