using FluentValidation;
using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.Validators
{
    public class StockCardValidator : AbstractValidator<StockCardVM>
    {
        private readonly AppManEntities _db;
        public StockCardValidator(AppManEntities db)
        {
            _db = db;

            RuleFor(m => m.ItemCodeId)
                .NotEmpty().WithMessage("Article is required.");

            RuleFor(m => m.Fund)
                .NotEmpty().WithMessage("Fund is required.");

            RuleFor(m => m.PsNo)
                .NotEmpty().WithMessage("Stock Number is required.");

            //RuleFor(x => x.PsNo)
            //    .MustAsync(IsUnique).WithMessage("Stock Number already exists.");

            //RuleFor(m => m.Id)
            //    .MustAsync(async (id, cancellation) =>
            //        !await _service.IcsParItem.IsPostedAsync(id))
            //    .WithMessage("Record already posted, cannot update!");


            RuleSet("Create", () =>
            {
                RuleFor(x => x.PsNo)
               .MustAsync(async (psNo, cancellation) =>
                   !await _db.PsCards.AnyAsync(a => a.PsNo == psNo))
                .WithMessage(m => $"Stock Number '{m.PsNo}' already exists.");
            });

            RuleSet("Update, Delete", () =>
            {
                RuleFor(m => m.Id)
                    .MustAsync(async (id, cancellation) =>
                        await _db.PsCards.AnyAsync(a => a.Id == id))
                    .WithMessage("Record does not exists!");
            });

            RuleSet("Update", () =>
            {                
                RuleFor(x => x.PsNo)
                    .MustAsync(async (entity, psNo, cancellation) =>
                        !await _db.PsCards.AnyAsync(a => a.PsNo == psNo && a.Id != entity.Id))
                    .WithMessage(m => $"Stock Number '{m.PsNo}' already exists.");
            });
        }

        //private async Task<bool> IsUnique(string psNo, CancellationToken cancellationToken)
        //{
        //    return await _db.PsCards.AnyAsync(a => a.PsNo == psNo);
        //}

        private async Task<bool> IsExists(string psNo, CancellationToken cancellationToken)
        {
            return await _db.PsCards.AnyAsync(a => a.PsNo == psNo);
        }
    }
}