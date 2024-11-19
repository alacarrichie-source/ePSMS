using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services.Codes
{
    public interface IPsCategoryService : ICodextnService
    {
        
    }

    public class PsCategoryService : CodextnService, IPsCategoryService
    {
        public PsCategoryService(AppManEntities db) : base(db)
        {
        }        

        //public override ValueTask<CodextnVM> CreateAsync(CodextnVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        //{
        //    ValidateIfNull(model);
        //    ValidateRecord(model.Id);
        //    ValidateFields(model, Mode.ADD);

        //    model.Desc4 = GetDesc4(model.Code);

        //    return await base.CreateAsync(model, user, date);
        //});

        //public override ValueTask<CodextnVM> UpdateAsync(CodextnVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        //{
        //    ValidateIfNull(model);
        //    ValidateRecord(model.Id);
        //    ValidateFields(model, Mode.EDIT);

        //    model.Desc4 = GetDesc4(model.Code);

        //    return await base.UpdateAsync(model, user, date);
        //});

        //private string GetDesc4(string code)
        //{
        //    return code.Substring(0, 2) + code.Substring(code.Length - 2, 2);
        //}

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