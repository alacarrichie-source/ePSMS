using FluentValidation;
using iLgs.Models;
using iLgs.Services.AIRs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.Validators
{
    public class AirItemExtnVehicleValidator : AbstractValidator<AIRItemExtnVehicle>
    {
        private readonly IAirItemExtnVehicleService _airItemExtnVehicleService;
        public AirItemExtnVehicleValidator(IAirItemExtnVehicleService airItemExtnVehicleService)
        {
            _airItemExtnVehicleService = airItemExtnVehicleService;

            RuleFor(m => m.AIRItemId)
                .MustAsync(async (airItemId, cancellation) => 
                    !await _airItemExtnVehicleService.IsPostedAsync(airItemId))
                .WithMessage("Record already posted, cannot update!");

            RuleFor(m => m.YearModel)
                .NotEmpty().WithMessage("Year Model is required.");

            RuleFor(m => m)
                .MustAsync(async (entity, cancellation) =>
                   await _airItemExtnVehicleService.IsValidItemQty(entity.AIRItemId))
               .WithMessage("Number of Items must not exceed the Quantity.");

            RuleSet("Create", () => {
                RuleFor(x => x.PlateNo)                
               .MustAsync(async (entity, plateNo, cancellation) =>
                   await _airItemExtnVehicleService.IsUniquePlateNoAddAsync(entity.AIRItemId, plateNo))
               .WithMessage("The Plate No must be unique.");
            });

            RuleSet("Update", () => {
                RuleFor(x => x.PlateNo)
               .MustAsync(async (entity, plateNo, cancellation) =>
                   await _airItemExtnVehicleService.IsUniquePlateNoUpdateAsync(entity.Id, entity.AIRItemId, plateNo))
               .WithMessage("The Plate No must be unique.");
            });
        }        
    }
}