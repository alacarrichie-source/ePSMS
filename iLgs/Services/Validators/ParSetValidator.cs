using FluentValidation;
using iLgs.Models;
using iLgs.Services.ParIcs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Services.Validators
{
    public class ParSetValator : AbstractValidator<IcsParItem>
    {
        private readonly IParService _parService;
        public ParSetValator(IParService parService)
        {
            _parService = parService;

            RuleFor(m => m.Id)
                .MustAsync(async (id, cancellation) =>
                    !await _parService.IcsParItem.IsPostedAsync(id))
                .WithMessage("Record already posted, cannot update!");

            
            //RuleSet("Create", () => {
            //    RuleFor(x => x.PlateNo)
            //   .MustAsync(async (entity, plateNo, cancellation) =>
            //       await _airItemExtnVehicleService.IsUniquePlateNoAddAsync(entity.AIRItemId, plateNo))
            //   .WithMessage("The Plate No must be unique.");
            //});

            RuleSet("Update", () => {
                RuleFor(x => x.Id)
               .MustAsync(async (entity, id, cancellation) =>
                   !await _parService.IcsParItem.IsExistingAsync(entity.Id))
               .WithMessage("Record id no longer exists!");
            });
        }
    }
}