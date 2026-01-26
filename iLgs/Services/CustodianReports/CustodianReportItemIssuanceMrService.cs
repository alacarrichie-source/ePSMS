using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.CustodianReports
{
    public interface ICustodianReportItemIssuanceMrService : ICustodianReportItemIssuanceService
    {
        new ValueTask<CustodianReportItemIssuanceMrVM> GetByIdAsync(Guid id);
        IQueryable<CustodianReportItemIssuanceMrVM> GetAll(Guid? reportItemId);
        ValueTask<CustodianReportItemIssuanceMrVM> CreateAsync(CustodianReportItemIssuanceMrVM model, string user, DateTime date);
        ValueTask<CustodianReportItemIssuanceMrVM> UpdateAsync(CustodianReportItemIssuanceMrVM model, string user, DateTime date);
        ValueTask<CustodianReportItemIssuanceMrVM> DeleteAsync(CustodianReportItemIssuanceMrVM model, string user, DateTime date);
    }

    public class CustodianReportItemIssuanceMrService : CustodianReportItemIssuanceService, ICustodianReportItemIssuanceMrService
    {
        private readonly string _refType = "MR";
        private readonly IExceptionService<CustodianReportItemIssuanceMrVM> _xtraExceptionService;
        
        public CustodianReportItemIssuanceMrService(AppManEntities db,
            IAppManEntitiesFactory appManEntitiesFactory,
            ICodextnService codextnService,
            ICreateAndLogExceptions exceptions,
            IExceptionService<CustodianReportItemIssuance> exceptionService,
            IExceptionService<CustodianReportItemIssuanceMrVM> xtraExceptionService,
            IUserService userService) : base(db, appManEntitiesFactory, codextnService, exceptions, exceptionService, userService)
        {
            _xtraExceptionService = xtraExceptionService;
        }

        private static Expression<Func<CustodianReportItemIssuance, CustodianReportItemIssuanceMrVM>> CustodianReportItemIssuanceProjection
        = s => new CustodianReportItemIssuanceMrVM
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

        public new ValueTask<CustodianReportItemIssuanceMrVM> GetByIdAsync(Guid id) =>
        _xtraExceptionService.TryCatch(async () =>
        {
            var data = await _db.CustodianReportItemIssuances
                .Where(w => w.Id == id)
                .Select(CustodianReportItemIssuanceProjection).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<CustodianReportItemIssuanceMrVM> GetAll(Guid? reportItemId)
        {
            var data = _db.CustodianReportItemIssuances
                .AsNoTracking()
                .Where(w => w.ReportItemId == reportItemId && w.RefType == _refType)
                .Select(CustodianReportItemIssuanceProjection);
            return data;
        }

        public ValueTask<CustodianReportItemIssuanceMrVM> CreateAsync(CustodianReportItemIssuanceMrVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            model.RefType = _refType;
            await base.CreateAsync(model, user, date);
            return model;
        });

        public ValueTask<CustodianReportItemIssuanceMrVM> UpdateAsync(CustodianReportItemIssuanceMrVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            model.RefType = _refType;
            await base.UpdateAsync(model, user, date);
            return model;
        });

        public ValueTask<CustodianReportItemIssuanceMrVM> DeleteAsync(CustodianReportItemIssuanceMrVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            await base.DeleteAsync(model, user, date);
            return model;
        });
    }
}