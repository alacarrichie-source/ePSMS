using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.CustodianDisposal_
{
    public interface ICustodianDisposalService
    {
        IQueryable<CustodianDisposal> GetAll();
        IQueryable<CustodianDisposal> GetAllByDeptId(Guid? deptId);
        ValueTask<CustodianDisposal> GetByIdAsync(Guid? id);
        IQueryable<CustodianDisposal> GetAvailableForIirup();
        ValueTask<CustodianDisposal> PostAsync(Guid id, string user, DateTime date);
        ValueTask<CustodianDisposal> UnPostAsync(Guid id, string user, DateTime date);
        ValueTask<CustodianDisposal> CreateAsync(CustodianDisposal model, string user, DateTime date);
        ValueTask<CustodianDisposal> UpdateAsync(CustodianDisposal model, string user, DateTime date);
        ValueTask<CustodianDisposal> DeleteAsync(CustodianDisposal model, string user, DateTime date);
    }

    public class CustodianDisposalService : BaseValidator, ICustodianDisposalService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<CustodianDisposal> _exceptionService;
        private readonly GetDisplayNameDelegate _getDisplayName;
        
        public CustodianDisposalService(AppManEntities db,
            ICreateAndLogExceptions exceptions,
            IExceptionService<CustodianDisposal> exceptionService)
        {
            _db = db;
            _exceptions = exceptions;
            _exceptionService = exceptionService;
            _getDisplayName = Utility.GetDisplayName<CustodianDisposal>;
        }

        public IQueryable<CustodianDisposal> GetAll() =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.CustodianDisposals.AsNoTracking().AsQueryable();
            return data;
        });

        public ValueTask<CustodianDisposal> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.CustodianDisposals.FindAsync(id);
            return data;
        });
            
        public IQueryable<CustodianDisposal> GetAllByDeptId(Guid? deptId) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.CustodianDisposals.AsNoTracking().Where(w => w.DeptId == deptId).AsQueryable();
            return data;
        });

        public IQueryable<CustodianDisposal> GetAvailableForIirup() =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.CustodianDisposals.AsNoTracking()
                .Where(w => w.PostedDt != null && !w.CustodianIirupItems.Any())
                .AsQueryable();

            return data;
        });

        public ValueTask<CustodianDisposal> PostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.CustodianDisposals.FindAsync(id);

            ValidateRecord(entity);
            ValidateIfPosted(entity);

            if (!_db.CustodianDisposalItems.Any(a  => a.CustodianDisposalId == entity.Id))
            {
                throw new NotFoundException("Disposal Items Not Found!");
            }

            entity.PostedBy = user;
            entity.PostedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.CustodianDisposals.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        public ValueTask<CustodianDisposal> UnPostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.CustodianDisposals.FindAsync(id);
            ValidateRecord(entity);
            ValidateIfNotPosted(entity);

            if (_db.CustodianDisposals.Any(a => a.CustodianIirupItems.Any()))
            {
                throw new RecordAlreadyExistsException("Record already in IIRUP, cannot unpost!");
            }

            entity.PostedBy = "";
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.CustodianDisposals.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        public ValueTask<CustodianDisposal> CreateAsync(CustodianDisposal model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateFields(model);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            if (string.IsNullOrWhiteSpace(model.TransmittalNo))
            {
                model.TransmittalNo = NextTransmittalNo((DateTime)model.RequestedDt);
            }

            var entity = new CustodianDisposal();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.CustodianDisposals.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<CustodianDisposal> UpdateAsync(CustodianDisposal model, string user, DateTime date) =>
       _exceptionService.TryCatch(async () =>
       {
           ValidateIfNull(model);           

           var entity = await _db.CustodianDisposals.FindAsync(model.Id);                   
           ValidateRecord(entity);
           ValidateIfPosted(entity);
           ValidateFields(model);

           model.UpdatedBy = user;
           model.UpdatedDt = date;

           if (string.IsNullOrWhiteSpace(model.TransmittalNo) 
                || model.RequestedDt.Value.Year != entity.RequestedDt.Value.Year
                || model.RequestedDt.Value.Month != entity.RequestedDt.Value.Month)
           {
               model.TransmittalNo = NextTransmittalNo((DateTime)model.RequestedDt);
           }

           MapModelToEntityFields(entity, model, Mode.EDIT);

           _db.CustodianDisposals.Attach(entity);
           _db.Entry(entity).State = EntityState.Modified;
           await _db.SaveChangesAsync();
           return model;
       });

        public ValueTask<CustodianDisposal> DeleteAsync(CustodianDisposal model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);

            var entity = await _db.CustodianDisposals.Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            ValidateRecord(entity);
            ValidateIfPosted(entity);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.CustodianDisposals.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.CustodianDisposals.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public void MapModelToEntityFields(CustodianDisposal entity, CustodianDisposal model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }
            entity.DeptId = model.DeptId;
            entity.Department = model.Department;
            entity.TransmittalNo = model.TransmittalNo;
            entity.RequestedBy = model.RequestedBy;
            entity.RequestedDt = model.RequestedDt;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;            
        }

        private string NextTransmittalNo(DateTime requestedDt)
        {
            string yyyy = requestedDt.Year.ToString().Trim();
            string mm = requestedDt.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = _db.CustodianDisposals.Where(w => w.RequestedDt.Value.Year == requestedDt.Year).OrderByDescending(o => o.TransmittalNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(data.TransmittalNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }

        private void ValidateFields(CustodianDisposal model)
        {
            //if (string.IsNullOrWhiteSpace(model.TransmittalNo))
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.TransmittalNo)), "Field is required.");
            //}

            if (string.IsNullOrWhiteSpace(model.RequestedBy))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.RequestedBy)), "Field is required.");
            }

            if (!model.RequestedDt.HasValue)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.RequestedDt)), "Field is required.");
            }

            _imex.ThrowIfContainsErrors();
        }

        private void ValidateIfNull(CustodianDisposal model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(CustodianDisposal entity)
        {
            if (entity == null)
            {
                throw new NotFoundException(entity.Id);
            }
        }

        private void ValidateIfPosted(CustodianDisposal entity)
        {
            if (entity.PostedDt != null)
            {
                var msg = $"Record already posted by {entity.PostedBy} on {entity.PostedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        private void ValidateIfNotPosted(CustodianDisposal entity)
        {
            if (entity.PostedDt == null)
            {
                throw new RecordNotYetPostedException($"Record is not yet posted!");
            }
        }
    }
}