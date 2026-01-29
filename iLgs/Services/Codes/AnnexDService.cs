using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services.Codes
{
    public interface IAnnexDService
    {
        IQueryable<AnnexDVM> GetAll();
        ValueTask<AnnexDVM> GetByIdAsync(Guid id);
        bool IsAny(string userName);
        ValueTask<AnnexDVM> CreateAsync(AnnexDVM model, string user, DateTime date);
        ValueTask<AnnexDVM> UpdateAsync(AnnexDVM model, string user, DateTime date);
        ValueTask<AnnexDVM> DeleteAsync(AnnexDVM model, string user, DateTime date);
    }

    public class AnnexDService : CodextnService, IAnnexDService
    {
        private readonly string _mastCode = "ANNEX-D";
        private readonly IExceptionService<AnnexDVM> _xtraExceptionService;

        public AnnexDService(AppManEntities db)
        : base(db)
        {
            _xtraExceptionService = new ExceptionService<AnnexDVM>();
        }

        //public AnnexDService(AppManEntities db,
        //    IAppManEntitiesFactory appManEntitiesFactory,
        //    IExceptionService<Codextn> exceptionService,
        //    IExceptionService<CodextnVM> vmExceptionService,
        //    IExceptionService<AnnexDVM> xtraExceptionService,
        //    IUserService userService)
        //: base(db, appManEntitiesFactory, exceptionService, vmExceptionService, userService)
        //{
        //    _xtraExceptionService = xtraExceptionService;
        //}

        private static Expression<Func<Codextn, AnnexDVM>> CodextnProjection
        = s => new AnnexDVM
        {
            Id = s.Id,
            MastId = s.MastId,
            Code = s.Code,
            Description = s.Description,
            InsertedBy = s.InsertedBy,
            InsertedDt = s.InsertedDt
        };

        public bool IsAny(string userName)
        {
            var data = _db.Codextns.Where(w => w.CodeMast.Code == _mastCode && w.Code == userName).Any();
            return data;
        }

        public IQueryable<AnnexDVM> GetAll()
        {
            var data = _db.Codextns.Where(w => w.CodeMast.Code == _mastCode).AsNoTracking()
                .Select(CodextnProjection).OrderBy(o => o.Description);
            return data;
        }

        public new async ValueTask<AnnexDVM> GetByIdAsync(Guid id)
        {
            var data = await _db.Codextns.Where(w => w.CodeMast.Code == _mastCode && w.Id == id)
                .Select(CodextnProjection).FirstOrDefaultAsync();
            return data;
        }

        public ValueTask<AnnexDVM> CreateAsync(AnnexDVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateFields(model, Mode.ADD);

            await base.CreateAsync((Codextn)model, user, date);
            return model;
        });

        public ValueTask<AnnexDVM> UpdateAsync(AnnexDVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateRecord(model.Id);
            ValidateFields(model, Mode.EDIT);

            await base.UpdateAsync((Codextn)model, user, date);
            return model;
        });

        public ValueTask<AnnexDVM> DeleteAsync(AnnexDVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateRecord(model.Id);
            await base.DeleteAsync((Codextn)model, user, date);
            return model;
        });

        private void ValidateIfNull(AnnexDVM model)
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

        private void ValidateFields(AnnexDVM model, Mode mode)
        {
            if (string.IsNullOrWhiteSpace(model.Code))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Code)), "Field is required.");
            }
            else
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

            //if (string.IsNullOrWhiteSpace(model.Desc2))
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.Desc2)), "Field is required.");
            //}

            _imex.ThrowIfContainsErrors();
        }
    }
}