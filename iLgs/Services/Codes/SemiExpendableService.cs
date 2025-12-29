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
    public interface ISemiExpendableService
    {
        IQueryable<SemiExpendableVM> GetAll();
        decimal? GetSPHV();
        decimal? GetSPHV(DateTime? asOfDate);
        ValueTask<SemiExpendableVM> GetByIdAsync(Guid id);
        ValueTask<SemiExpendableVM> CreateAsync(SemiExpendableVM model, string user, DateTime date);
        ValueTask<SemiExpendableVM> UpdateAsync(SemiExpendableVM model, string user, DateTime date);
        ValueTask<SemiExpendableVM> DeleteAsync(SemiExpendableVM model, string user, DateTime date);
    }

    public class SemiExpendableService : CodextnService, ISemiExpendableService
    {
        private readonly IExceptionService<SemiExpendableVM> _xtraExceptionService;

        public SemiExpendableService(AppManEntities db,
            IAppManEntitiesFactory appManEntitiesFactory,
            IExceptionService<Codextn> exceptionService,
            IExceptionService<CodextnVM> vmExceptionService,
            IExceptionService<SemiExpendableVM> xtraExceptionService,
            IUserService userService)
        : base(db, appManEntitiesFactory, exceptionService, vmExceptionService, userService)
        {
            _xtraExceptionService = xtraExceptionService;
        }

        private static Expression<Func<Codextn, SemiExpendableVM>> CodextnProjection
        = s => new SemiExpendableVM
        {
            Id = s.Id,
            MastId = s.MastId,
            Code = s.Code,
            Description = s.Description,
            InsertedBy = s.InsertedBy,
            InsertedDt = s.InsertedDt
        };

        public IQueryable<SemiExpendableVM> GetAll()
        {
            var data = _db.Codextns.Where(w => w.CodeMast.Code == "SPHV")
                .Select(CodextnProjection);
            return data;
        }

        public new async ValueTask<SemiExpendableVM> GetByIdAsync(Guid id)
        {
            var data = await _db.Codextns.Where(w => w.CodeMast.Code == "SPHV" && w.Id == id)
                .Select(CodextnProjection).FirstOrDefaultAsync();
            return data;
        }

        public decimal? GetSPHV()
        {
            return GetSPHV(DateTime.Now);
        }

        public decimal? GetSPHVOld(DateTime? asOfDate)
        {
            var data = _db.Database.SqlQuery<decimal?>("Select top 1 convert(numeric(18, 2), Description) as PriceCap From Codextn " +
                "Where MastId in (Select Id From CodeMast Where Code = 'SPHV') " +
                "and convert(varchar(10), Code, 102) <= convert(varchar(10), {0}, 102)", asOfDate).FirstOrDefault();
            return data ?? 5000;
        }

        public decimal? GetSPHV(DateTime? asOfDate)
        {
            var data = _db.Database.SqlQuery<decimal?>("Select dbo.fn_SPHV({0})", asOfDate).FirstOrDefault();
            return data ?? 5000;
        }

        public ValueTask<SemiExpendableVM> CreateAsync(SemiExpendableVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateFields(model, Mode.ADD);

            await base.CreateAsync((Codextn)model, user, date);
            return model;
        });

        public ValueTask<SemiExpendableVM> UpdateAsync(SemiExpendableVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateRecord(model.Id);
            ValidateFields(model, Mode.EDIT);

            await base.UpdateAsync((Codextn)model, user, date);
            return model;
        });

        public ValueTask<SemiExpendableVM> DeleteAsync(SemiExpendableVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateRecord(model.Id);
            await base.DeleteAsync((Codextn)model, user, date);
            return model;
        });


        private void ValidateIfNull(SemiExpendableVM model)
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

        private void ValidateFields(SemiExpendableVM model, Mode mode)
        {
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