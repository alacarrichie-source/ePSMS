using iLgs.Exceptions;
using iLgs.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace iLgs.Services.CustodianReports
{
    public interface ICustodianReportItemIssuanceRpcPpeService : ICustodianReportItemIssuanceService
    {
        new ValueTask<CustodianReportItemIssuanceRpcPpeVM> GetByIdAsync(Guid id);
        IQueryable<CustodianReportItemIssuanceRpcPpeVM> GetAll(Guid? reportItemId);
        ValueTask<CustodianReportItemIssuanceRpcPpeVM> CreateAsync(CustodianReportItemIssuanceRpcPpeVM model, string user, DateTime date);
        ValueTask<CustodianReportItemIssuanceRpcPpeVM> UpdateAsync(CustodianReportItemIssuanceRpcPpeVM model, string user, DateTime date);
        ValueTask<CustodianReportItemIssuanceRpcPpeVM> DeleteAsync(CustodianReportItemIssuanceRpcPpeVM model, string user, DateTime date);
    }

    public class CustodianReportItemIssuanceRpcPpeService : CustodianReportItemIssuanceService, ICustodianReportItemIssuanceRpcPpeService
    {
        private readonly string _refType = "RPCPPE";
        private readonly IExceptionService<CustodianReportItemIssuanceRpcPpeVM> _xtraExceptionService;

        public CustodianReportItemIssuanceRpcPpeService(AppManEntities db,
            ICreateAndLogExceptions exceptions,
            IExceptionService<CustodianReportItemIssuance> exceptionService,
            IExceptionService<CustodianReportItemIssuanceRpcPpeVM> xtraExceptionService,
            IUserService userService) : base(db, exceptions, exceptionService, userService)
        {
            _xtraExceptionService = xtraExceptionService;
        }

        private static Expression<Func<CustodianReportItemIssuance, CustodianReportItemIssuanceRpcPpeVM>> CustodianReportItemIssuanceProjection
        = s => new CustodianReportItemIssuanceRpcPpeVM
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

        public new ValueTask<CustodianReportItemIssuanceRpcPpeVM> GetByIdAsync(Guid id) =>
        _xtraExceptionService.TryCatch(async () =>
        {
            var data = await _db.CustodianReportItemIssuances
                .Where(w => w.Id == id)
                .Select(CustodianReportItemIssuanceProjection).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<CustodianReportItemIssuanceRpcPpeVM> GetAll(Guid? reportItemId)
        {
            var data = _db.CustodianReportItemIssuances
                .AsNoTracking()
                .Where(w => w.ReportItemId == reportItemId && w.RefType == _refType)
                .Select(CustodianReportItemIssuanceProjection);
            return data;
        }

        public ValueTask<CustodianReportItemIssuanceRpcPpeVM> CreateAsync(CustodianReportItemIssuanceRpcPpeVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            model.RefType = _refType;
            await base.CreateAsync(model, user, date);
            return model;
        });

        public ValueTask<CustodianReportItemIssuanceRpcPpeVM> UpdateAsync(CustodianReportItemIssuanceRpcPpeVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            model.RefType = _refType;
            await base.UpdateAsync(model, user, date);
            return model;
        });

        public ValueTask<CustodianReportItemIssuanceRpcPpeVM> DeleteAsync(CustodianReportItemIssuanceRpcPpeVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            await base.DeleteAsync(model, user, date);
            return model;
        });
    }
}