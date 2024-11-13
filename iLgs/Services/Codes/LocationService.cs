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
    public interface ILocationService : ICodextnService
    {
        ValueTask<CodextnVM> UpdateIndexNo(string user, DateTime date);
    }

    public class LocationService : CodextnService, ILocationService
    {
        public LocationService(AppManEntities db) : base(db)
        {
        }

        public override IQueryable<CodextnVM> GetByMastId(Guid mastId)
        {
            var data = _db.Codextns.Where(w => w.MastId == mastId)
                .Select(s => new CodextnVM
                {
                    Id = s.Id,
                    Code = s.Code,
                    MastId = s.MastId,
                    Description = s.Description,
                    Desc2 = s.Desc2,
                    Desc3 = s.Desc3,
                    Desc4 = s.Desc4,
                    Desc5 = s.Desc5,
                    CodeHdg = s.CodeMast.CodeHdg,
                    Desc1Hdg = s.CodeMast.Desc1Hdg,
                    Desc2Hdg = s.CodeMast.Desc2Hdg,
                    Desc3Hdg = s.CodeMast.Desc3Hdg,
                    Desc4Hdg = s.CodeMast.Desc4Hdg,
                    Desc5Hdg = s.CodeMast.Desc5Hdg,
                    Pad = s.Code.Trim().Substring(s.Code.Trim().Length - 2),
                    Padding = s.Code.Trim().Substring(s.Code.Trim().Length - 2) == "00" ? 0 : 30
                }).OrderBy(o => o.Desc4);
            return data;
        }

        public ValueTask<CodextnVM> UpdateIndexNo(string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            var locations = GetByMastCode("Locations");
            using (var db = new AppManEntities())
            {
                foreach (var location in locations)
                {
                    var entity = db.Codextns.Find(location.Id);

                    if (entity != null)
                    {
                        entity.Desc4 = GetDesc4(entity.Code);
                        entity.UpdatedBy = user;
                        entity.UpdatedDt = date;

                        db.Codextns.Attach(entity);
                        db.Entry(entity).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }
            
            return new CodextnVM();    
        });

        public override ValueTask<CodextnVM> CreateAsync(CodextnVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateRecord(model.Id);
            ValidateFields(model, Mode.ADD);

            model.Desc4 = GetDesc4(model.Code);

            return await base.CreateAsync(model, user, date);
        });

        public override ValueTask<CodextnVM> UpdateAsync(CodextnVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateRecord(model.Id);
            ValidateFields(model, Mode.EDIT);

            model.Desc4 = GetDesc4(model.Code);

            return await base.UpdateAsync(model, user, date);
        });

        private string GetDesc4(string code)
        {
            return code.Substring(0, 2) + code.Substring(code.Length - 2, 2);
        }

        private void ValidateIfNull(CodextnVM model)
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

        private void ValidateFields(CodextnVM model, Mode mode)
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
            //if (!model.PhaseAmountCo.HasValue)
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.PhaseAmountCo)), "Field is required.");
            //}

            _imex.ThrowIfContainsErrors();
        }
    }
}