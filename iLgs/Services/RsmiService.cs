using iLgs.Exceptions;
using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Services
{
    public interface IRsmiService
    {
        IQueryable<RsmiVM> GetAll();        
    }

    public class RsmiService : IRsmiService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RsmiVM> _vmExceptionService = new ExceptionService<RsmiVM>();

        public RsmiService(AppManEntities db)
        {
            _db = db;
            _db.Database.CommandTimeout = 3000;         
        }
        
        public IQueryable<RsmiVM> GetAll() =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RSMIs
                .Select(s => new RsmiVM
                {
                    Id = s.Id,
                    Date = s.Date,
                    SerialNo = s.SerialNo,
                    Fund = s.Fund,
                    Custodian = s.Custodian,
                    Qty = s.RSMIItems.Sum(x => x.Qty),
                    Amount = s.RSMIItems.Sum(x => x.Amount),
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt,
                    InsertedBy = s.InsertedBy,
                    InsertedDt = s.InsertedDt                    
                });
            return data;
        });        
    }
}