using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

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
        private readonly IExceptionService<CustodianReportItemIssuanceIcsVM> _xtraExceptionService;
        
        public CustodianReportItemIssuanceIcsService(AppManEntities db,
            IAppManEntitiesFactory appManEntitiesFactory,
            ICodextnService codextnService,
            ICreateAndLogExceptions exceptions,
            IExceptionService<CustodianReportItemIssuance> exceptionService,
            IExceptionService<CustodianReportItemIssuanceIcsVM> xtraExceptionService,
            IUserService userService) : base(db, appManEntitiesFactory, codextnService, exceptions, exceptionService, userService)
        {
            _xtraExceptionService = xtraExceptionService;
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
        _xtraExceptionService.TryCatch(async () =>
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

        public ValueTask<CustodianReportItemIssuanceIcsVM> CreateAsync(CustodianReportItemIssuanceIcsVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            model.RefType = _refType;
            await base.CreateAsync(model, user, date);
            return model;
        });

        public ValueTask<CustodianReportItemIssuanceIcsVM> UpdateAsync(CustodianReportItemIssuanceIcsVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            model.RefType = _refType;
            await base.UpdateAsync(model, user, date);
            return model;
        });

        public ValueTask<CustodianReportItemIssuanceIcsVM> DeleteAsync(CustodianReportItemIssuanceIcsVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            await base.DeleteAsync(model, user, date);
            return model;
        });
    }
}