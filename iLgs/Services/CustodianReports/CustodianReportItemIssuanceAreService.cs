using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.CustodianReports
{
    public interface ICustodianReportItemIssuanceAreService : ICustodianReportItemIssuanceService
    {
        new ValueTask<CustodianReportItemIssuanceAreVM> GetByIdAsync(Guid id);
        IQueryable<CustodianReportItemIssuanceAreVM> GetAll(Guid? reportItemId);
        ValueTask<CustodianReportItemIssuanceAreVM> CreateAsync(CustodianReportItemIssuanceAreVM model, string user, DateTime date);
        ValueTask<CustodianReportItemIssuanceAreVM> UpdateAsync(CustodianReportItemIssuanceAreVM model, string user, DateTime date);
        ValueTask<CustodianReportItemIssuanceAreVM> DeleteAsync(CustodianReportItemIssuanceAreVM model, string user, DateTime date);
    }

    public class CustodianReportItemIssuanceAreService : CustodianReportItemIssuanceService, ICustodianReportItemIssuanceAreService
    {
        private readonly string _refType = "ARE";
        private readonly IExceptionService<CustodianReportItemIssuanceAreVM> _vmExceptionService = new ExceptionService<CustodianReportItemIssuanceAreVM>();

        public CustodianReportItemIssuanceAreService(AppManEntities db) : base(db)
        {

        }

        private static Expression<Func<CustodianReportItemIssuance, CustodianReportItemIssuanceAreVM>> CustodianReportItemIssuanceProjection
        = s => new CustodianReportItemIssuanceAreVM
        {
            Id = s.Id,
            ReportItemId = s.ReportItemId,
            RefType = s.RefType,
            RefNo = s.RefNo,
            IssuedTo = s.IssuedTo,
            AccountableOfficer = s.AccountableOfficer,
            InsertedBy = s.InsertedBy,
            InsertedDt = s.InsertedDt
        };

        public new ValueTask<CustodianReportItemIssuanceAreVM> GetByIdAsync(Guid id) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.CustodianReportItemIssuances
                .Where(w => w.Id == id)
                .Select(CustodianReportItemIssuanceProjection).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<CustodianReportItemIssuanceAreVM> GetAll(Guid? reportItemId)
        {
            var data = _db.CustodianReportItemIssuances
                .AsNoTracking()
                .Where(w => w.ReportItemId == reportItemId && w.RefType == _refType)
                .Select(CustodianReportItemIssuanceProjection);
            return data;
        }

        public ValueTask<CustodianReportItemIssuanceAreVM> CreateAsync(CustodianReportItemIssuanceAreVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            model.RefType = _refType;
            await base.CreateAsync(model, user, date);
            return model;
        });

        public ValueTask<CustodianReportItemIssuanceAreVM> UpdateAsync(CustodianReportItemIssuanceAreVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            model.RefType = _refType;
            await base.UpdateAsync(model, user, date);
            return model;
        });

        public ValueTask<CustodianReportItemIssuanceAreVM> DeleteAsync(CustodianReportItemIssuanceAreVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            await base.DeleteAsync(model, user, date);
            return model;
        });
    }
}