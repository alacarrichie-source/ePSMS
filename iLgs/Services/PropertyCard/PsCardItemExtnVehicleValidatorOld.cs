using FluentValidation;
using iLgs.Models;

namespace iLgs.Services.PropertyCard
{
    public class PsCardItemExtnVehicleValidatorOld : AbstractValidator<PsCardItemExtnVehicle>
    {
        private readonly IPsCardItemExtnVehicleService _psCardItemExtnVehicleService;
        public PsCardItemExtnVehicleValidatorOld(IPsCardItemExtnVehicleService psCardItemExtnVehicleService)
        {
            _psCardItemExtnVehicleService = psCardItemExtnVehicleService;

            //RuleFor(m => m.AIRItemId)
            //    .MustAsync(async (airItemId, cancellation) =>
            //        !await _airItemExtnVehicleService.IsPostedAsync(airItemId))
            //    .WithMessage("Record already posted, cannot update!");

            //RuleFor(m => m.YearModel)
            //    .NotEmpty().WithMessage("Year Model is required.");

            ////RuleFor(m => m.SeriesNo)
            ////    .NotEmpty().WithMessage("Series No. is required.");

            //RuleFor(m => m)
            //    .MustAsync(async (entity, cancellation) =>
            //       await _airItemExtnVehicleService.IsValidItemQty(entity.AIRItemId))
            //   .WithMessage("Number of Items must not exceed the Quantity.");

            //RuleSet("Create", () => {
            //    RuleFor(x => x.PlateNo)
            //   .MustAsync(async (entity, plateNo, cancellation) =>
            //       await _airItemExtnVehicleService.IsUniquePlateNoAddAsync(entity.AIRItemId, plateNo))
            //   .WithMessage("The Plate No must be unique.");
            //});

            //RuleSet("Update", () => {
            //    RuleFor(x => x.PlateNo)
            //   .MustAsync(async (entity, plateNo, cancellation) =>
            //       await _airItemExtnVehicleService.IsUniquePlateNoUpdateAsync(entity.Id, entity.AIRItemId, plateNo))
            //   .WithMessage("The Plate No must be unique.");
            //});
        }
    }
}