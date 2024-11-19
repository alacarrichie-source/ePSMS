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
    public interface ICustodianReportItemIssuanceIcsService : ICustodianReportItemIssuanceService
    {
        new ValueTask<CustodianReportItemIssuanceIcsVM> GetByIdAsync(Guid id);
        IQueryable<CustodianReportItemIssuanceIcsVM> GetAll(Guid? reportItemId);
        ValueTask<CustodianReportItemIssuanceIcsVM> CreateAsync(CustodianReportItemIssuanceIcsVM model, string user, DateTime date);
        ValueTask<CustodianReportItemIssuanceIcsVM> UpdateAsync(CustodianReportItemIssuanceIcsVM model, string user, DateTime date);
        ValueTask<CustodianReportItemIssuanceIcsVM> DeleteAsync(CustodianReportItemIssuanceIcsVM model, string user, DateTime date);
    }

    public class CustodianReportItemIssuanceIcsService : CustodianReportItemIssuanceService, ICustodianReportItemIssuanceIcsService
    {
        private readonly string _refType = "ICS";
        private readonly IExceptionService<CustodianReportItemIssuanceIcsVM> _vmExceptionService = new ExceptionService<CustodianReportItemIssuanceIcsVM>();

        public CustodianReportItemIssuanceIcsService(AppManEntities db) : base(db)
        {

        }

        private static Expression<Func<CustodianReportItemIssuance, CustodianReportItemIssuanceIcsVM>> CustodianReportItemIssuanceProjection
        = s => new CustodianReportItemIssuanceIcsVM
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

        public new ValueTask<CustodianReportItemIssuanceIcsVM> GetByIdAsync(Guid id) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.CustodianReportItemIssuances
                .Where(w => w.Id == id)
                .Select(CustodianReportItemIssuanceProjection).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<CustodianReportItemIssuanceIcsVM> GetAll(Guid? reportItemId)
        {
            var data = _db.CustodianReportItemIssuances
                .AsNoTracking()
                .Where(w => w.ReportItemId == reportItemId && w.RefType == _refType)
                .Select(CustodianReportItemIssuanceProjection);
            return data;
        }

        public ValueTask<CustodianReportItemIssuanceIcsVM> CreateAsync(CustodianReportItemIssuanceIcsVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            model.RefType = _refType;
            await base.CreateAsync(model, user, date);
            return model;
        });

        public ValueTask<CustodianReportItemIssuanceIcsVM> UpdateAsync(CustodianReportItemIssuanceIcsVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            model.RefType = _refType;
            await base.UpdateAsync(model, user, date);
            return model;
        });

        public ValueTask<CustodianReportItemIssuanceIcsVM> DeleteAsync(CustodianReportItemIssuanceIcsVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            await base.DeleteAsync(model, user, date);
            return model;
        });
    }
}