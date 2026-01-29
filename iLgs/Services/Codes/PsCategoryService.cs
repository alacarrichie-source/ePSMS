using iLgs.Models;
using iLgs.Utilities;

namespace iLgs.Services.Codes
{
    public interface IPsCategoryService : ICodextnService
    {
        
    }

    public class PsCategoryService : CodextnService, IPsCategoryService
    {
        public PsCategoryService(AppManEntities db)
        : base(db)
        {
        }

        //public PsCategoryService(AppManEntities db,
        //    IAppManEntitiesFactory appManEntitiesFactory,
        //    IExceptionService<Codextn> exceptionService,
        //    IExceptionService<CodextnVM> vmExceptionService,
        //    IUserService userService)
        //: base(db, appManEntitiesFactory, exceptionService, vmExceptionService, userService)
        //{            
        //}

        //public ValueTask<CodextnVM> CreateAsync(CodextnVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        //{
        //    ValidateIfNull(model);
        //    ValidateRecord(model.Id);
        //    ValidateFields(model, Mode.ADD);

        //    await base.CreateAsync(model, user, date);
        //    return model;
        //});

        //public ValueTask<CodextnVM> UpdateAsync(CodextnVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        //{
        //    ValidateIfNull(model);
        //    ValidateRecord(model.Id);
        //    ValidateFields(model, Mode.EDIT);

        //    await base.UpdateAsync(model, user, date);
        //    return model;
        //});

        //public ValueTask<CodextnVM> DeleteAsync(CodextnVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        //{
        //    ValidateIfNull(model);
        //    ValidateRecord(model.Id);

        //    await base.DeleteAsync(model, user, date);
        //    return model;
        //});

        //private void ValidateIfNull(CodextnVM model)
        //{
        //    if (model is null)
        //    {
        //        throw new NullException();
        //    }
        //}

        //private void ValidateRecord(Guid id)
        //{
        //    if (!_db.Codextns.Any(a => a.Id == id))
        //    {
        //        throw new NotFoundException(id);
        //    }
        //}

        //private void ValidateFields(CodextnVM model, Mode mode)
        //{
        //    if (string.IsNullOrWhiteSpace(model.Code))
        //    {
        //        _imex.UpsertDataList(_getDisplayName(nameof(model.Code)), "Field is required.");
        //    }
        //    else
        //    {
        //        if (mode == Mode.ADD)
        //        {
        //            if (_db.Codextns.Any(a => a.MastId == model.MastId && a.Code == model.Code))
        //            {
        //                _imex.UpsertDataList(_getDisplayName(nameof(model.Code)), "Already Exists.");
        //            }
        //        }
        //        else if (mode == Mode.EDIT)
        //        {
        //            if (_db.Codextns.Any(a => a.MastId == model.MastId && a.Code == model.Code && a.Id != model.Id))
        //            {
        //                _imex.UpsertDataList(_getDisplayName(nameof(model.Code)), "Already Exists.");
        //            }
        //        }
        //    }
        //    //if (!model.PhaseAmountCo.HasValue)
        //    //{
        //    //    _imex.UpsertDataList(_getDisplayName(nameof(model.PhaseAmountCo)), "Field is required.");
        //    //}

        //    _imex.ThrowIfContainsErrors();
        //}
    }
}