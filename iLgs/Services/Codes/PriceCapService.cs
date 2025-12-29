using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.Codes
{
    public interface IPriceCapService
    {
        IQueryable<PriceCapVM> GetAll();
        decimal? GetPriceCap();
        decimal? GetPriceCap(DateTime? asOfDate);
        ValueTask<PriceCapVM> GetByIdAsync(Guid id);
        ValueTask<PriceCapVM> CreateAsync(PriceCapVM model, string user, DateTime date);
        ValueTask<PriceCapVM> UpdateAsync(PriceCapVM model, string user, DateTime date);
        ValueTask<PriceCapVM> DeleteAsync(PriceCapVM model, string user, DateTime date);
    }

    public class PriceCapService : CodextnService, IPriceCapService
    {
        private readonly IExceptionService<PriceCapVM> _xtraExceptionService;
        
        public PriceCapService(AppManEntities db,
            IAppManEntitiesFactory appManEntitiesFactory,
            IExceptionService<Codextn> exceptionService,
            IExceptionService<CodextnVM> vmExceptionService,
            IExceptionService<PriceCapVM> xtraExceptionService,
            IUserService userService)
        : base(db, appManEntitiesFactory, exceptionService, vmExceptionService, userService)
        {
            _xtraExceptionService = xtraExceptionService;
        }

        private static Expression<Func<Codextn, PriceCapVM>> CodextnProjection
        = s => new PriceCapVM
        {
            Id = s.Id,
            MastId = s.MastId,
            Code = s.Code,
            Description = s.Description,
            InsertedBy = s.InsertedBy,
            InsertedDt = s.InsertedDt
        };

        public IQueryable<PriceCapVM> GetAll()
        {
            var data = _db.Codextns.Where(w => w.CodeMast.Code == "PRICE-CAP")
                .Select(CodextnProjection);
            return data;
        }

        public async ValueTask<PriceCapVM> GetByIdAsync(Guid id)
        {
            var data = await _db.Codextns.Where(w => w.CodeMast.Code == "PRICE-CAP" && w.Id == id)
                .Select(CodextnProjection).FirstOrDefaultAsync();
            return data;
        }

        public decimal? GetPriceCap()
        {
            return GetPriceCap(DateTime.Now);
        }

        public decimal? GetPriceCapOld(DateTime? asOfDate)
        {
            var data = _db.Database.SqlQuery<decimal?>("Select top 1 convert(numeric(18, 2), Description) as PriceCap From Codextn " +
                "Where MastId in (Select Id From CodeMast Where Code = 'PRICE-CAP') " +
                "and convert(varchar(10), Description, 102) <= convert(varchar(10), {0}, 102)", asOfDate).FirstOrDefault();
            return data ?? 50000;
        }

        public decimal? GetPriceCap(DateTime? asOfDate)
        {
            var data = _db.Database.SqlQuery<decimal?>("Select dbo.fn_PriceCap({0})", asOfDate).FirstOrDefault();
            return data ?? 50000;
        }

        public ValueTask<PriceCapVM> CreateAsync(PriceCapVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateFields(model, Mode.ADD);

            await base.CreateAsync((Codextn)model, user, date);
            return model;
        });

        public ValueTask<PriceCapVM> UpdateAsync(PriceCapVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateRecord(model.Id);
            ValidateFields(model, Mode.EDIT);

            await base.UpdateAsync((Codextn)model, user, date);
            return model;
        });

        public ValueTask<PriceCapVM> DeleteAsync(PriceCapVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateRecord(model.Id);
            await base.DeleteAsync((Codextn)model, user, date);
            return model;
        });


        private void ValidateIfNull(PriceCapVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(Guid id)
        {
            if (!_db.Codextns.Any(a => a.Id == id))
            {
                throw new NotFoundException(id);
            }
        }

        private void ValidateFields(PriceCapVM model, Mode mode)
        {
            _imex = new InvalidModelException();
            if (string.IsNullOrWhiteSpace(model.Code))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Code)), "Field is required.");
            }
            else
            {
                DateTime effectivity;
                if (DateTime.TryParse(model.Code, out effectivity))
                {
                    if (mode == Mode.ADD)
                    {
                        if (_db.Codextns.Any(a => a.MastId == model.MastId && a.Code == model.Code))
                        {
                            _imex.UpsertDataList(_getDisplayName(nameof(model.Code)), "Already Exists.");
                        }
                    }
                    else if (mode == Mode.EDIT)
                    {
                        if (_db.Codextns.Any(a => a.MastId == model.MastId && a.Code == model.Code && a.Id != model.Id))
                        {
                            _imex.UpsertDataList(_getDisplayName(nameof(model.Code)), "Already Exists.");
                        }
                    }
                }
                else
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.Code)), "Invalid Date.");
                }
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Description)), "Field is required.");
            }
            else
            {
                decimal priceCap;
                if (!decimal.TryParse(model.Description, out priceCap))
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.Description)), "Invalid Value.");
                }
            }

            _imex.ThrowIfContainsErrors();
        }
    }
}