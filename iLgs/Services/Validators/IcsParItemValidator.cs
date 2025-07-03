//using FluentValidation;
//using iLgs.Models;
//using System;
//using System.Collections.Generic;
//using System.Data.Entity;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Web;

//namespace iLgs.Services.Validators
//{    
//    public class IcsParItemValidator : AbstractValidator<IcsParItem>
//    {
//        private readonly AppManEntities _db;
//        public IcsParItemValidator(AppManEntities db)
//        {
//            _db = db;

//            //RuleFor(m => m.AIRItemId)
//            //    .MustAsync(async (airItemId, cancellation) =>
//            //        !await _service.IsPostedAsync(airItemId))
//            //    .WithMessage("Record already posted, cannot update!");

//            //RuleFor(m => m.YearModel)
//            //    .NotEmpty().WithMessage("Year Model is required.");

//            ////RuleFor(m => m.SeriesNo)
//            ////    .NotEmpty().WithMessage("Series No. is required.");

//            RuleFor(x => x.PsCardItemExtnId)
//                .MustAsync(IsNotPosted).WithMessage("PO Item already posted.");                

//            //RuleSet("Create", () => {
//            //    RuleFor(x => x.PlateNo)
//            //   .MustAsync(async (entity, plateNo, cancellation) =>
//            //       await _service.IsUniquePlateNoAddAsync(entity.AIRItemId, plateNo))
//            //   .WithMessage("The Plate No must be unique.");
//            //});

//            //RuleSet("Update", () => {
//            //    RuleFor(x => x.PlateNo)
//            //   .MustAsync(async (entity, plateNo, cancellation) =>
//            //       await _service.IsUniquePlateNoUpdateAsync(entity.Id, entity.AIRItemId, plateNo))
//            //   .WithMessage("The Plate No must be unique.");
//            //});
//        }

//        private async Task<bool> IsNotPosted(Guid? psCardItemExtnId, CancellationToken cancellationToken)
//        {
//            return !await _db.PsCardItemExtns.Where(w => w.Id == psCardItemExtnId && w.PsCardItem.ParPostedBy != null && w.PsCardItem.ParPostedBy != "").AnyAsync();
//        }
//    }
//}