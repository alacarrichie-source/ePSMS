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
    public interface ICustodianReportItemIssuanceParService : ICustodianReportItemIssuanceService
    {
        new ValueTask<CustodianReportItemIssuanceParVM> GetByIdAsync(Guid id);
        IQueryable<CustodianReportItemIssuanceParVM> GetAll(Guid? reportItemId);
        ValueTask<CustodianReportItemIssuanceParVM> CreateAsync(CustodianReportItemIssuanceParVM model, string user, DateTime date);
        ValueTask<CustodianReportItemIssuanceParVM> UpdateAsync(CustodianReportItemIssuanceParVM model, string user, DateTime date);
        ValueTask<CustodianReportItemIssuanceParVM> DeleteAsync(CustodianReportItemIssuanceParVM model, string user, DateTime date);
    }

    public class CustodianReportItemIssuanceParService : CustodianReportItemIssuanceService, ICustodianReportItemIssuanceParService
    {
        private readonly string _refType = "PAR";
        private readonly IExceptionService<CustodianReportItemIssuanceParVM> _vmExceptionService = new ExceptionService<CustodianReportItemIssuanceParVM>();
        
        public CustodianReportItemIssuanceParService(AppManEntities db) : base(db)
        {
            
        }

        private static Expression<Func<CustodianReportItemIssuance, CustodianReportItemIssuanceParVM>> CustodianReportItemIssuanceProjection
        = s => new CustodianReportItemIssuanceParVM
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

        public new ValueTask<CustodianReportItemIssuanceParVM> GetByIdAsync(Guid id) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.CustodianReportItemIssuances
                .Where(w => w.Id == id)
                .Select(CustodianReportItemIssuanceProjection).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<CustodianReportItemIssuanceParVM> GetAll(Guid? reportItemId)
        {
            var data = _db.CustodianReportItemIssuances
                .AsNoTracking()
                .Where(w => w.ReportItemId == reportItemId && w.RefType == _refType)
                .Select(CustodianReportItemIssuanceProjection);
            return data;
        }

        public ValueTask<CustodianReportItemIssuanceParVM> CreateAsync(CustodianReportItemIssuanceParVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            model.RefType = _refType;
            await base.CreateAsync(model, user, date);
            return model;
        });

        public ValueTask<CustodianReportItemIssuanceParVM> UpdateAsync(CustodianReportItemIssuanceParVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            model.RefType = _refType;
            await base.UpdateAsync(model, user, date);
            return model;
        });

        public ValueTask<CustodianReportItemIssuanceParVM> DeleteAsync(CustodianReportItemIssuanceParVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            await base.DeleteAsync(model, user, date);
            return model;
        });        
    }
}