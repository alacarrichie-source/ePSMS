using Dapper;
using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services
{
    public interface IRsmiService
    {
        IQueryable<RsmiVM> GetAll();
        IQueryable<RSMITotalVM> GetTotal(string type, DateTime? startDate, DateTime? endDate);
        Task<IEnumerable<RSMITotalVM>> GetTotalAsync(string type, DateTime? startDate, DateTime? endDate);
        IList<RSMITotalVM> GetTotalList(string type, DateTime? startDate, DateTime? endDate, string fund, string fromDonation, string invDist, Guid? deptId);

        ValueTask<RSMIProcessVM> GenerateAsync(RSMIProcessVM model, string user, DateTime date);
        ValueTask<RSMIProcessVM> DeleteRangeAsync(RSMIProcessVM model, string user, DateTime date);
        ValueTask<RsmiVM> UpdateAsync(RsmiVM model, string user, DateTime date);
        ValueTask<RsmiVM> DeleteAsync(RsmiVM model, string user, DateTime date);

        ValueTask<RSMI> PostAsync(Guid? id, string user, DateTime date);
        ValueTask<RSMI> UnpostAsync(Guid? id, string user, DateTime date);

        ValueTask<RSMIProcessVM> PostBatchAsync(RSMIProcessVM model, string user, DateTime date);
        ValueTask<RSMIProcessVM> UnpostBatchAsync(RSMIProcessVM model, string user, DateTime date);
    }

    public class RsmiService : IRsmiService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<RSMI> _exceptionService;
        private readonly IExceptionService<RsmiVM> _vmExceptionService;
        private readonly IExceptionService<RSMIProcessVM> _processExceptionService;
        private readonly IPriceCapService _priceCapService;
        private readonly ISemiExpendableService _semiExpendableService;

        private decimal? _priceCap;
        private decimal? _SPHV;

        public RsmiService(AppManEntities db)
        {
            _db = db;
            _db.Database.CommandTimeout = 3000;
            _exceptions = new CreateAndLogExceptions();
            _exceptionService = new ExceptionService<RSMI>();
            _vmExceptionService = new ExceptionService<RsmiVM>();
            _processExceptionService = new ExceptionService<RSMIProcessVM>();
            _priceCapService = new PriceCapService(_db);
            _semiExpendableService = new SemiExpendableService(_db);
        }

        //public RsmiService(AppManEntities db, 
        //    IAppManEntitiesFactory appManEntitiesFactory,
        //    ICreateAndLogExceptions exceptions,
        //    IExceptionService<RSMI> exceptionService,
        //    IExceptionService<RsmiVM> vmExceptionService,
        //    IExceptionService<RSMIProcessVM> processExceptionService,
        //    IPriceCapService priceCapService,
        //    ISemiExpendableService semiExpendableService)
        //{
        //    _db = db;
        //    _contextFactory = appManEntitiesFactory;
        //    _db.Database.CommandTimeout = 3000;
        //    _exceptions = exceptions;
        //    _exceptionService = exceptionService;
        //    _vmExceptionService = vmExceptionService;
        //    _processExceptionService = processExceptionService;
        //    _priceCapService = priceCapService;
        //    _semiExpendableService = semiExpendableService;
        //}

        private decimal GetPriceCap()
        {
            return _priceCap ?? (_priceCap = _priceCapService.GetPriceCap()).Value;
        }

        private decimal GetSPHV()
        {
            return _SPHV ?? (_SPHV = _semiExpendableService.GetSPHV()).Value;
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
                    QtyCons = s.RSMIItems.Where(w => w.ItemType == "C").Sum(x => x.Qty),
                    AmountCons = s.RSMIItems.Where(w => w.ItemType == "C").Sum(x => x.Amount),
                    QtySPHV = s.RSMIItems.Where(w => w.ItemType == "SPHV").Sum(x => x.Qty),
                    AmountSPHV = s.RSMIItems.Where(w => w.ItemType == "SPHV").Sum(x => x.Amount),
                    QtySPLV = s.RSMIItems.Where(w => w.ItemType == "SPLV").Sum(x => x.Qty),
                    AmountSPLV = s.RSMIItems.Where(w => w.ItemType == "SPLV").Sum(x => x.Amount),
                    QtySE = s.RSMIItems.Where(w => w.ItemType.Substring(0, 1) == "S").Sum(x => x.Qty),
                    AmountSE = s.RSMIItems.Where(w => w.ItemType.Substring(0, 1) == "S").Sum(x => x.Amount),
                    Qty = s.RSMIItems.Sum(x => x.Qty),
                    Amount = s.RSMIItems.Sum(x => x.Amount),
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt,
                    InsertedBy = s.InsertedBy,
                    InsertedDt = s.InsertedDt
                });
            return data;
        });

        public IQueryable<RSMITotalVM> GetTotal(string type, DateTime? startDate, DateTime? endDate)
        {
            var spvh = _semiExpendableService.GetSPHV(startDate);
            var data = _db.Database.SqlQuery<RSMITotalVM>("Exec REPORTS_RSMI_GetTotal {0}, {1}, {2}, {3}"
                , string.IsNullOrWhiteSpace(type) || type == "ALL" ? null : type
                , startDate
                , endDate
                , spvh).AsQueryable();
            return data;
        }

        public async Task<IEnumerable<RSMITotalVM>> GetTotalAsync(string type, DateTime? startDate, DateTime? endDate)
        {
            var spvh = _semiExpendableService.GetSPHV(startDate);
            var data = await _db.Database.SqlQuery<RSMITotalVM>("Exec REPORTS_RSMI_GetTotal {0}, {1}, {2}, {3}"
                , string.IsNullOrWhiteSpace(type) || type == "ALL" ? null : type
                , startDate
                , endDate
                , spvh).ToListAsync();
            return data;
        }

        public IList<RSMITotalVM> GetTotalList(string type, DateTime? startDate, DateTime? endDate, string fund, string fromDonation, string invDist, Guid? deptId)
        {
            var spvh = _semiExpendableService.GetSPHV(startDate);
            var data = _db.Database.Connection.Query<RSMITotalVM>("Exec REPORTS_RSMI_GetTotal @p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7"
                , new
                {
                    p0 = string.IsNullOrWhiteSpace(type) || type == "ALL" ? null : type,
                    p1 = startDate,
                    p2 = endDate,
                    p3 = string.IsNullOrWhiteSpace(fund) || fund == "ALL" ? null : fund,
                    p4 = spvh,
                    p5 = string.IsNullOrWhiteSpace(fromDonation) || fromDonation == "ALL" ? (bool?)null : (fromDonation == "D"),
                    p6 = string.IsNullOrWhiteSpace(invDist) || invDist == "ALL" ? null : invDist,
                    p7 = deptId == Guid.Empty ? null : deptId                    
                }).ToList();
            return data;
        }        

        public ValueTask<RSMIProcessVM> GenerateAsync(RSMIProcessVM model, string user, DateTime date) =>
        _processExceptionService.TryCatch(async () =>
        {
            if (model.DateFrom.Value.Year != model.DateTo.Value.Year)
            {
                throw new InvalidValueException("Year or date range must be the same.");
            }

            if (await _db.RSMIs
                        .Where(w => w.Date >= model.DateFrom && w.Date <= model.DateTo).AnyAsync())
            {
                throw new InvalidValueException("Period", "RSMI already exists within the period entered.");
            }

            //var priceCap = _priceCapService.GetPriceCap(model.DateFrom);
            var sphv = _semiExpendableService.GetSPHV(model.DateFrom);

            if (!(await _db.PsCardItemTransferIssuances
                        .Where(w => w.IssuedDate >= model.DateFrom && w.IssuedDate <= model.DateTo).AnyAsync()))
            {
                throw new InvalidValueException("Period", "No Issuances found on period entered.");
            }

            await _db.Database.ExecuteSqlCommandAsync("Exec RSMI_Generate {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}",
                model.DateFrom, model.DateTo, sphv, model.Custodian, model.PostedBy, model.PostedDt, user, date);

            return model;
        });

        public ValueTask<RSMIProcessVM> DeleteRangeAsync(RSMIProcessVM model, string user, DateTime date) =>
        _processExceptionService.TryCatch(async () =>
        {
            if (!model.DateFrom.HasValue)
            {
                throw new InvalidValueException("Date From is required.");
            }

            if (!model.DateTo.HasValue)
            {
                throw new InvalidValueException("Date To is required.");
            }

            if (model.DateFrom > model.DateTo)
            {
                throw new InvalidValueException("Invalid date range.");
            }

            if (model.DateFrom.Value.Year != model.DateTo.Value.Year)
            {
                throw new InvalidValueException("Dates must be of the same year.");
            }

            var lastDate = await _db.RSMIs.MaxAsync(m => m.Date);

            if (model.DateTo < lastDate)
            {
                throw new InvalidValueException($"Date To must be the last date of RSMI ({ lastDate.Value.Date.ToLongDateString() })");
            }

            var rsmiList = await _db.RSMIs.Where(w => w.Date >= model.DateFrom && w.Date <= model.DateTo).ToListAsync();

            if (!rsmiList.Any())
            {
                throw new InvalidValueException("No RSMI where found within the period entered.");
            }

            if (rsmiList.Any(a => a.PostedDt != null))
            {
                throw new InvalidValueException("Posted RSMI where found within the period entered.");
            }

            _db.RSMIs.RemoveRange(rsmiList);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RsmiVM> UpdateAsync(RsmiVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var entity = await _db.RSMIs.FindAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (!string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordAlreadyPostedException($"Record Already Posted by {entity.PostedBy}, cannot update!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.Custodian = model.Custodian;
            entity.PostedBy = model.PostedBy;
            entity.PostedDt = model.PostedDt;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RsmiVM> DeleteAsync(RsmiVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var entity = await _db.RSMIs.FindAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (!string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordAlreadyPostedException($"Record Already Posted by {entity.PostedBy}, cannot proceed.");
            }

            if (await _db.RSMIs.AnyAsync(a => a.Date > entity.Date))
            {
                throw new RecordAlreadyExistsException("RSMI already exists after this date, cannot proceed.");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();

            _db.RSMIs.Remove(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RSMI> PostAsync(Guid? id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.RSMIs.FindAsync(id);
            if (entity == null)
            {
                throw new RecordNotFoundException(id);
            }

            if (!string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordAlreadyPostedException($"Record Already Posted by {entity.PostedBy}");
            }

            entity.PostedBy = user;
            entity.PostedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            await _db.SaveChangesAsync();

            return entity;
        });

        public ValueTask<RSMI> UnpostAsync(Guid? id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.RSMIs.FindAsync(id);
            if (entity == null)
            {
                throw new RecordNotFoundException(id);
            }

            if (string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordNotYetPostedException($"Record is not yet posted!");
            }

            entity.PostedBy = "";
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            await _db.SaveChangesAsync();

            return entity;
        });

        public ValueTask<RSMIProcessVM> PostBatchAsync(RSMIProcessVM model, string user, DateTime date) =>
        _processExceptionService.TryCatch(async () =>
        {
            if (!model.DateFrom.HasValue)
            {
                throw new InvalidValueException("Date From is required.");
            }

            if (!model.DateTo.HasValue)
            {
                throw new InvalidValueException("Date To is required.");
            }

            if (model.DateFrom > model.DateTo)
            {
                throw new InvalidValueException("Invalid date range.");
            }

            if (model.DateFrom.Value.Year != model.DateTo.Value.Year)
            {
                throw new InvalidValueException("Dates must be of the same year.");
            }

            
            var rsmiList = await _db.RSMIs.Where(w => w.Date >= model.DateFrom && w.Date <= model.DateTo && w.PostedDt == null).ToListAsync();

            if (!rsmiList.Any())
            {
                throw new InvalidValueException("No Posted RSMI where found within the period entered.");
            }

            
            foreach (var rsmi in rsmiList)
            {
                rsmi.PostedBy = user;
                rsmi.PostedDt = date;
                rsmi.UpdatedBy = user;
                rsmi.UpdatedDt = date;
            }
            
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RSMIProcessVM> UnpostBatchAsync(RSMIProcessVM model, string user, DateTime date) =>
        _processExceptionService.TryCatch(async () =>
        {
            if (!model.DateFrom.HasValue)
            {
                throw new InvalidValueException("Date From is required.");
            }

            if (!model.DateTo.HasValue)
            {
                throw new InvalidValueException("Date To is required.");
            }

            if (model.DateFrom > model.DateTo)
            {
                throw new InvalidValueException("Invalid date range.");
            }

            if (model.DateFrom.Value.Year != model.DateTo.Value.Year)
            {
                throw new InvalidValueException("Dates must be of the same year.");
            }

            var rsmiList = await _db.RSMIs.Where(w => w.Date >= model.DateFrom && w.Date <= model.DateTo && w.PostedDt != null).ToListAsync();

            if (!rsmiList.Any())
            {
                throw new InvalidValueException("No Posted RSMI where found within the period entered.");
            }


            foreach (var rsmi in rsmiList)
            {
                rsmi.PostedBy = null;
                rsmi.PostedDt = null;
                rsmi.UpdatedBy = user;
                rsmi.UpdatedDt = date;
            }

            await _db.SaveChangesAsync();

            return model;
        });

        private string NextSerialNoOld(string fund, DateTime? date)
        {
            string yyyy = date.Value.Year.ToString().Trim();
            string mm = date.Value.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = _db.RSMIs.Where(w => w.Fund == fund && w.Date.Value.Year == date.Value.Year && w.Date.Value.Month == date.Value.Month).OrderByDescending(o => o.SerialNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(data.SerialNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }

        private string NextSerialNo(string fund, DateTime? date)
        {
            return _db.Database.SqlQuery<string>("Select dbo.fn_NextRsmiNo({0}, {1})", fund, date).FirstOrDefault();
        }
    }
}