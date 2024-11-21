using FluentValidation;
using iLgs.Models;
using iLgs.Services.AIRs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Services.Validators
{    
    public class AirItemValidator : AbstractValidator<AIRItemVM>
    {
        private readonly IAirItemService _service;
        public AirItemValidator(IAirItemService service)
        {
            _service = service;

            //RuleFor(m => m.AIRItemId)
            //    .MustAsync(async (airItemId, cancellation) =>
            //        !await _service.IsPostedAsync(airItemId))
            //    .WithMessage("Record already posted, cannot update!");

            //RuleFor(m => m.YearModel)
            //    .NotEmpty().WithMessage("Year Model is required.");

            ////RuleFor(m => m.SeriesNo)
            ////    .NotEmpty().WithMessage("Series No. is required.");

            //RuleFor(m => m)
            //    .MustAsync(async (entity, cancellation) =>
            //       await _service.IsValidItemQty(entity.AIRItemId))
            //   .WithMessage("Number of Items must not exceed the Quantity.");

            //RuleSet("Create", () => {
            //    RuleFor(x => x.PlateNo)
            //   .MustAsync(async (entity, plateNo, cancellation) =>
            //       await _service.IsUniquePlateNoAddAsync(entity.AIRItemId, plateNo))
            //   .WithMessage("The Plate No must be unique.");
            //});

            //RuleSet("Update", () => {
            //    RuleFor(x => x.PlateNo)
            //   .MustAsync(async (entity, plateNo, cancellation) =>
            //       await _service.IsUniquePlateNoUpdateAsync(entity.Id, entity.AIRItemId, plateNo))
            //   .WithMessage("The Plate No must be unique.");
            //});
        }
    }
}