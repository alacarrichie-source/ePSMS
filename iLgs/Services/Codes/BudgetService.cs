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
    public interface IBudgetService
    {
        IQueryable<BudgetCodeVM> GetAll();
        ValueTask<BudgetCodeVM> GetByIdAsync(Guid id);
        ValueTask<BudgetCodeVM> CreateAsync(BudgetCodeVM model, string user, DateTime date);
        ValueTask<BudgetCodeVM> UpdateAsync(BudgetCodeVM model, string user, DateTime date);
        ValueTask<BudgetCodeVM> DeleteAsync(BudgetCodeVM model, string user, DateTime date);
    }

    public class BudgetService : CodextnService, IBudgetService
    {
        private readonly IExceptionService<BudgetCodeVM> _xtraExceptionService;
        
        public BudgetService(AppManEntities db,
            IAppManEntitiesFactory appManEntitiesFactory,
            IExceptionService<Codextn> exceptionService,
            IExceptionService<CodextnVM> vmExceptionService,
            IExceptionService<BudgetCodeVM> xtraExceptionService,
            IUserService userService)
        : base(db, appManEntitiesFactory, exceptionService, vmExceptionService, userService)
        {
            _xtraExceptionService = xtraExceptionService;
        }


        private static Expression<Func<Codextn, BudgetCodeVM>> CodextnProjection
        = s => new BudgetCodeVM
        {
            Id = s.Id,
            MastId = s.MastId,
            Code = s.Code,
            Description = s.Description,
            Desc2 = s.Desc2,
            Desc3 = s.Desc3,
            Desc4 = s.Desc4,
            Pad = "", // s.Code.Trim().Substring(s.Code.Trim().Length - 2),
            Padding = s.Desc2 == "Y" ? 0 : 30,
            InsertedBy = s.InsertedBy,
            InsertedDt = s.InsertedDt            
        };

        public IQueryable<BudgetCodeVM> GetAll()
        {
            var data = _db.Codextns.Where(w => w.CodeMast.Code == "BUDGET-CODE").AsNoTracking()
                .Select(CodextnProjection).OrderBy(o => o.Desc4);                
            return data;
        }

        public new async ValueTask<BudgetCodeVM> GetByIdAsync(Guid id)
        {
            var data = await _db.Codextns.Where(w => w.CodeMast.Code == "BUDGET-CODE" && w.Id == id).AsNoTracking()
                .Select(CodextnProjection).FirstOrDefaultAsync();
            return data;
        }

        public ValueTask<BudgetCodeVM> CreateAsync(BudgetCodeVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateFields(model, Mode.ADD);

            model.Desc2 = ToUpper(model.Desc2);
            model.Desc4 = GetDesc4(model.Code);

            await base.CreateAsync((Codextn)model, user, date);
            return model;
        });

        public ValueTask<BudgetCodeVM> UpdateAsync(BudgetCodeVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateRecord(model.Id);
            ValidateFields(model, Mode.EDIT);

            model.Desc2 = ToUpper(model.Desc2);
            model.Desc4 = GetDesc4(model.Code);

            await base.UpdateAsync((Codextn)model, user, date);
            return model;
        });

        public ValueTask<BudgetCodeVM> DeleteAsync(BudgetCodeVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateRecord(model.Id);
            await base.DeleteAsync((Codextn)model, user, date);
            return model;
        });


        private string ToUpper(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? "" : text.ToUpper();
        }

        private string GetDesc4(string code)
        {
            var aCode = code.Split('-');
            string retCode = aCode[0];
            if (aCode.Length > 1) {
                for (int i = 1; i <= aCode.Length -1; i++)
                {
                    retCode += "-" + aCode[i].PadLeft(4, '0');
                }
            }

            return retCode;
        }

        private void ValidateIfNull(BudgetCodeVM model)
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

        private void ValidateFields(BudgetCodeVM model, Mode mode)
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

            if (string.IsNullOrWhiteSpace(model.Desc2))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Desc2)), "Field is required.");
            }

            _imex.ThrowIfContainsErrors();
        }
    }
}