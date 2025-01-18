using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using iLgs.Services.Validators;
using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.Requisition
{
    public interface IRisService
    {
        IQueryable<RIS_VM> GetAll();
        ValueTask<RISs> GetByIdAsync(Guid id);
        ValueTask<RISs> GetByRisNoAsync(string risNo);
        ValueTask<RISs> GetByOrderIdAsync(Guid orderId);
        ValueTask<bool> GetAnyRisNoAsync(Guid risId, string risNo);
        bool IsPosted(Guid risId);
        bool IsPosted(RISs ris);
        bool IsPosted(RisItem risItem);
        bool IsPosted(RisItemUnitGroup risItemUnitGroup);
        bool IsPosted(RisItemUnitGroupDescription risItemunitGroupDescription);
        bool IsPosted(RisItemUnitGroupDescriptionItem risItemunitGroupDescriptionItem);

        ValueTask<bool> IsPostedAsync(Guid risId);
        ValueTask<bool> IsPrPostedAsync(Guid risId);
        ValueTask<bool> IsWithPrAsync(Guid risId);

        void ValidateIfPosted(Guid risId);

        ValueTask<RIS_VM> CreateAsync(RIS_VM model, string user, DateTime date);
        ValueTask<RIS_VM> UpdateAsync(RIS_VM model, string user, DateTime date);
        ValueTask<RIS_VM> DeleteAsync(RIS_VM model, string user, DateTime date);

        ValueTask<RISs> PostAsync(Guid risId, string user, DateTime date);
        ValueTask<RISs> UnpostAsync(Guid risId, string user, DateTime date);

        //IRisItemService RisItem { get; }
        //IRisItemUnitGroupService RisItemUnitGroup { get; }
        //IRisItemUnitGroupDescriptionService RisItemUnitGroupDescription { get; }
        //IRisItemUnitGroupDescriptionItemService RisItemUnitGroupDescriptionItem { get; }
    }

    public class RisService : IRisService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RIS_VM> _risVmExceptionService = new ExceptionService<RIS_VM>();
        private readonly IExceptionService<RISs> _risExceptionService = new ExceptionService<RISs>();
        private readonly IRisValidator _validator;

        //private IRisItemService _risItemService;
        //private IRisItemUnitGroupService _risItemUnitGroupService;
        //private IRisItemUnitGroupDescriptionService _risItemUnitGroupDescriptionService;
        //private IRisItemUnitGroupDescriptionItemService _risItemUnitGroupDescriptionItemService;

        public RisService(AppManEntities db)
        {
            _db = db;            
            _validator = new RisValidator(_db);
            //_risItemService = new RisItemService(db);
            //_risItemUnitGroupService = new RisItemUnitGroupService(db);
            //_risItemUnitGroupDescriptionService = new RisItemUnitGroupDescriptionService(db);
            //_risItemUnitGroupDescriptionItemService = new RisItemUnitGroupDescriptionItemService(db);
        }

        //public IRisItemService RisItem { get { return _risItemService = _risItemService ?? new RisItemService(_db); } }
        //public IRisItemUnitGroupService RisItemUnitGroup { get { return _risItemUnitGroupService = _risItemUnitGroupService ?? new RisItemUnitGroupService(_db); } }
        //public IRisItemUnitGroupDescriptionService RisItemUnitGroupDescription
        //{
        //    get
        //    {
        //        return _risItemUnitGroupDescriptionService = _risItemUnitGroupDescriptionService ?? new RisItemUnitGroupDescriptionService(_db);
        //    }
        //}
        //public IRisItemUnitGroupDescriptionItemService RisItemUnitGroupDescriptionItem
        //{
        //    get
        //    {
        //        return _risItemUnitGroupDescriptionItemService = _risItemUnitGroupDescriptionItemService ?? new RisItemUnitGroupDescriptionItemService(_db);
        //    }
        //}


        public IQueryable<RIS_VM> GetAll() =>
        _risVmExceptionService.TryCatch(() =>
        {
            var data = _db.RISses
                .Select(s => new RIS_VM
                {
                    Id = s.Id,
                    Fund = s.Fund,
                    Division = s.Division,
                    OfficeId = s.OfficeId,
                    Office = s.Office,
                    FPP = s.FPP,
                    RisNo = s.RisNo,
                    RisDate = s.RisDate,
                    Purpose = s.Purpose,
                    RequestedBy = s.RequestedBy,
                    RequestedByDesignation = s.RequestedByDesignation,
                    RequestedDate = s.RequestedDate,
                    ApprovedBy = s.ApprovedBy,
                    ApprovedByDesignation = s.ApprovedByDesignation,
                    ApprovedDate = s.ApprovedDate,
                    IssuedBy = s.IssuedBy,
                    IssuedByDesignation = s.IssuedByDesignation,
                    IssuedDate = s.IssuedDate,
                    ReceivedBy = s.ReceivedBy,
                    ReceivedByDesignation = s.ReceivedByDesignation,
                    ReceivedDate = s.ReceivedDate,
                    InsertedBy = s.InsertedBy,
                    InsertedDt = s.InsertedDt,
                    UpdatedBy = s.UpdatedBy,
                    UpdatedDt = s.UpdatedDt,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt,
                    IsPosted = s.PostedDt != null,
                    IssuanceSw = false
                }).OrderByDescending(o => o.RisNo);
            return data;
        });

        public async ValueTask<bool> GetAnyRisNoAsync(Guid risId, string risNo)
        {
            return await _db.RISses.AnyAsync(a => a.Id != risId && a.RisNo == risNo);
        }

        public ValueTask<RISs> GetByIdAsync(Guid id) =>
        _risExceptionService.TryCatch(async () =>
        {
            var data = await _db.RISses.FindAsync(id);
            return data;
        });

        public ValueTask<RISs> GetByRisNoAsync(string risNo) =>
        _risExceptionService.TryCatch(async () =>
        {
            return await _db.RISses.Where(w => w.RisNo == risNo).FirstOrDefaultAsync();
        });

        public ValueTask<RISs> GetByOrderIdAsync(Guid orderId) =>
        _risExceptionService.TryCatch(async () =>
        {
            return await _db.RISses.Where(w => w.Requests.Any(a => a.Orders.Any(b => b.Id == orderId))).FirstOrDefaultAsync();
        });

        public async ValueTask<bool> IsPostedAsync(Guid risId)
        {
            var entity = await _db.RISses.FindAsync(risId);
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }

        public bool IsPosted(Guid risId)
        {
            var entity = _db.RISses.Find(risId);
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }

        public bool IsPosted(RISs ris)
        {
            return IsPosted(ris.Id);
        }

        public bool IsPosted(RisItem risItem)
        {
            var risId = (Guid)risItem.RisId;
            return IsPosted(risId);
        }

        public bool IsPosted(RisItemUnitGroup risItemUnitGroup)
        {
            var risId = (Guid)risItemUnitGroup.RisId;
            return IsPosted(risId);
        }

        public bool IsPosted(RisItemUnitGroupDescription risItemUnitGroupDescription)
        {            
            var risId = (Guid)_db.RisItemUnitGroups.Where(w => w.Id == risItemUnitGroupDescription.UnitGroupId).AsNoTracking().FirstOrDefault()?.RisId;
            return IsPosted(risId);
        }

        public bool IsPosted(RisItemUnitGroupDescriptionItem risItemUnitGroupDescriptionItem)
        {
            var risId = (Guid)_db.RisItemUnitGroups.Where(w => w.RisItemUnitGroupDescriptions.Any(a => a.Id == risItemUnitGroupDescriptionItem.UnitGroupDescriptionId)).AsNoTracking().FirstOrDefault()?.RisId;
            return IsPosted(risId);
        }

        public async ValueTask<bool> IsPrPostedAsync(Guid risId)
        {
            var pr = await _db.Requests.Where(a => a.RisId == risId).AsNoTracking().FirstOrDefaultAsync();
            if (pr != null)
            {
                return !string.IsNullOrWhiteSpace(pr.SubmittedBy);
            }
            return false;
        }

        public async ValueTask<bool> IsWithPrAsync(Guid risId)
        {
            return await _db.Requests.AnyAsync(a => a.RisId == risId);
        }

        public ValueTask<RISs> PostAsync(Guid risId, string user, DateTime date) =>
        _risExceptionService.TryCatch(async () =>
        {
            //await ValidateOnPost(risId);
            _validator.ValidateOnPost(risId);

            var entity = await _db.RISses.FindAsync(risId);

            entity.PostedBy = user;
            entity.PostedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.RISses.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;

            //// crate psCode foreach item (problem in unpost, sequence number will rumble)
            //var risItemList = await db.RisItems.Include(i => i.ItemCode.ItemType).Where(w => w.RisId == entity.Id).ToListAsync();
            //foreach (var risItem in risItemList)
            //{
            //    if (!db.PsCodes.Any(a => a.PsType == risItem.ItemCode.ItemType.Code && a.PsNo == risItem.PsNoDisplay && a.ItemName == risItem.ItemName))
            //    {
            //        var psCode = new PsCode()
            //        {
            //            Id = Guid.NewGuid(),
            //            ItemCodeId = risItem.ItemCodeId,
            //            PsNo = risItem.PsNoDisplay, //NextPsNo(risItem.ItemCode.ItemType.Code),
            //            PsType = risItem.ItemCode.ItemType.Code,
            //            ItemName = risItem.ItemName,
            //            InsertedBy = user,
            //            InsertedDt = date,
            //            UpdatedBy = user,
            //            UpdatedDt = date
            //        };

            //        db.PsCodes.Add(psCode);
            //        await db.SaveChangesAsync();
            //    }
            //}
        });
        
        public ValueTask<RISs> UnpostAsync(Guid risId, string user, DateTime date) =>
        _risExceptionService.TryCatch(async () =>
        {
            _validator.ValidateOnUnpost(risId);

            var entity = await _db.RISses.FindAsync(risId);

            entity.PostedBy = null;
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.RISses.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;

            //// delete un-used psCode foreach item (problem in unpost, sequence number will rumble)
            //var risItemList = await db.RisItems.Include(i => i.ItemCode.ItemType).Where(w => w.RisId == entity.Id).ToListAsync();
            //foreach (var risItem in risItemList)
            //{
            //    var psCode = await db.PsCodes.Where(w => w.PsNo == risItem.PsNoDisplay && w.ItemName == risItem.ItemName && !w.PsStocks.Any()).FirstOrDefaultAsync();
            //    if (psCode != null)
            //    {
            //        db.PsCodes.Remove(psCode);
            //        db.Entry(psCode).State = EntityState.Deleted;
            //        await db.SaveChangesAsync();
            //    }
            //}            
        });

        public ValueTask<RIS_VM> CreateAsync(RIS_VM model, string user, DateTime date) =>
        _risVmExceptionService.TryCatch(async () =>
            {
                _validator.ValidateOnCreate(model);
                //await ValidateOnCreate(model);

                model.Id = Guid.NewGuid();
                if (string.IsNullOrWhiteSpace(model.RisNo))
                {
                    model.RisNo = NextRisNo((DateTime)model.RisDate);
                }
                model.InsertedBy = user;
                model.InsertedDt = date;
                model.UpdatedBy = user;
                model.UpdatedDt = date;

                var entity = new iLgs.Models.RISs();                

                MapModelToEntityFields(entity, model, Mode.ADD);

                _db.RISses.Add(entity);
                await _db.SaveChangesAsync();

                return model;
            });

        public ValueTask<RIS_VM> DeleteAsync(RIS_VM model, string user, DateTime date) =>
        _risVmExceptionService.TryCatch(async () =>
        {
            //await ValidateOnDelete(model);
            _validator.ValidateOnDelete(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.RISses.FindAsync(model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.RISses.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.RISses.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RIS_VM> UpdateAsync(RIS_VM model, string user, DateTime date) =>
        _risVmExceptionService.TryCatch(async () =>
        {
            _validator.ValidateOnUpdate(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            if (string.IsNullOrWhiteSpace(model.RisNo))
            {
                model.RisNo = NextRisNo((DateTime)model.RisDate);
            }

            var entity = await _db.RISses.FindAsync(model.Id);

            MapModelToEntityFields(entity, model, Mode.EDIT);
            
            _db.RISses.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public void MapModelToEntityFields(RISs entity, RIS_VM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            entity.Fund = model.Fund;
            entity.Division = model.Division ?? "";
            entity.OfficeId = model.OfficeId;
            entity.Office = model.Office;
            entity.FPP = model.FPP;
            entity.RisNo = model.RisNo;
            entity.RisDate = model.RisDate;
            entity.Purpose = model.Purpose;
            entity.RequestedBy = model.RequestedBy ?? "";
            entity.RequestedByDesignation = model.RequestedByDesignation ?? "";
            entity.RequestedDate = model.RequestedDate;
            entity.ApprovedBy = model.ApprovedBy ?? "";
            entity.ApprovedByDesignation = model.ApprovedByDesignation ?? "";
            entity.ApprovedDate = model.ApprovedDate;
            entity.IssuedBy = model.IssuedBy ?? "";
            entity.IssuedByDesignation = model.IssuedByDesignation ?? "";
            entity.IssuedDate = model.IssuedDate;
            entity.ReceivedBy = model.ReceivedBy ?? "";
            entity.ReceivedByDesignation = model.ReceivedByDesignation ?? "";
            entity.ReceivedDate = model.ReceivedDate;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        private string NextRisNo(DateTime date)
        {
            string yyyy = date.Year.ToString().Trim();
            string mm = date.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = _db.RISses.Where(w => w.RisDate.Value.Year == date.Year).OrderByDescending(o => o.RisNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(data.RisNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }

        #region VALIDATION
        
        //private async ValueTask ValidateOnUpdate(RIS_VM model)
        //{
        //    var rec = await _db.RISses.FindAsync(model.Id);
        //    if (rec == null)
        //    {
        //        throw new RecordNotFoundException(model.Id);
        //    }

        //    if (!model.IssuanceSw)
        //    {
        //        if (!string.IsNullOrWhiteSpace(rec.PostedBy))
        //        {
        //            throw new RecordAlreadyPostedException(string.Format("RIS No {0} already posted. Cannot update!", rec.RisNo));
        //        }

        //        if (await IsPrPostedAsync(model.Id))
        //        {
        //            throw new RecordRelationshipException("This RIS No has a posted PR, cannot update!");
        //        }
        //    }

        //    if (await GetAnyRisNoAsync(model.Id, model.RisNo))
        //    {
        //        throw new RecordAlreadyExistsException(string.Format("RIS No {0} already exists!", model.RisNo));
        //    }
        //}

        public void ValidateIfPosted(Guid risId)
        {
            if (IsPosted(risId))
            {
                throw new RecordAlreadyPostedException(string.Format("RIS No. is already posted, cannot update!"));
            }
        }


        private async Task ValidateOnDelete(RIS_VM model)
        {
            if (await IsPostedAsync(model.Id))
            {
                throw new RecordAlreadyPostedException(string.Format("RIS No. {0} already Posted, cannot delete!", model.RisNo));
            }

            if (await IsPrPostedAsync(model.Id))
            {
                throw new RecordRelationshipException("This RIS Number has a posted PR, cannot delete!");
            }
        }
        
        #endregion

        #region EXCEPTION
        //private delegate ValueTask NonReturningFunction();
        //private delegate ValueTask<RIS_VM> ReturningVMFunction();
        //private delegate ValueTask<RISs> ReturningFunction();
        //private delegate IQueryable<RIS_VM> ReturningQueryableVMFunction();
        //private delegate IQueryable<RISs> ReturningQueryableFunction();

        //private async ValueTask TryCatch(NonReturningFunction nonReturningFunction)
        //{
        //    try
        //    {
        //        await nonReturningFunction();
        //    }
        //    catch (RecordNotFoundException notFoundException)
        //    {
        //        throw notFoundException;
        //    }
        //    catch (RecordAlreadyExistsException recordAlreadyExistsException)
        //    {
        //        throw recordAlreadyExistsException;
        //    }
        //    catch (InvalidValueException invalidValueException)
        //    {
        //        throw invalidValueException;
        //    }
        //    catch (RecordAlreadyPostedException recordAlreadyPostedException)
        //    {
        //        throw recordAlreadyPostedException;
        //    }
        //    catch (RecordRelationshipException recordRelationshipExistsException)
        //    {
        //        throw recordRelationshipExistsException;
        //    }
        //    catch (SqlException sqlException)
        //    {
        //        throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
        //    }
        //    catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
        //    {
        //        var recordLockedException = new RecordLockedException(dbUpdateConcurrencyException);

        //        throw exceptions.CreateAndLogDependencyException(recordLockedException);
        //    }
        //    catch (DbUpdateException dbUpdateException)
        //    {
        //        throw exceptions.CreateAndLogDependencyException(dbUpdateException);
        //    }
        //    catch (Exception exception)
        //    {
        //        var failedServiceException =
        //            new FailedServiceException(exception);

        //        throw exceptions.CreateAndLogServiceException(failedServiceException);
        //    }
        //}
        //private async ValueTask<RIS_VM> TryCatch(ReturningVMFunction returningVMFunction)
        //{
        //    try
        //    {
        //        return await returningVMFunction();
        //    }
        //    catch (RecordNotFoundException notFoundException)
        //    {
        //        throw notFoundException;
        //    }
        //    catch (RecordAlreadyExistsException recordAlreadyExistsException)
        //    {
        //        throw recordAlreadyExistsException;
        //    }
        //    catch (InvalidValueException invalidValueException)
        //    {
        //        throw invalidValueException;
        //    }
        //    catch (RecordAlreadyPostedException recordAlreadyPostedException)
        //    {
        //        throw recordAlreadyPostedException;
        //    }
        //    catch (RecordRelationshipException recordRelationshipExistsException)
        //    {
        //        throw recordRelationshipExistsException;
        //    }
        //    catch (SqlException sqlException)
        //    {
        //        throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
        //    }
        //    catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
        //    {
        //        var recordLockedException = new RecordLockedException(dbUpdateConcurrencyException);

        //        throw exceptions.CreateAndLogDependencyException(recordLockedException);
        //    }
        //    catch (DbUpdateException dbUpdateException)
        //    {
        //        throw exceptions.CreateAndLogDependencyException(dbUpdateException);
        //    }
        //    catch (Exception exception)
        //    {
        //        var failedServiceException =
        //            new FailedServiceException(exception);

        //        throw exceptions.CreateAndLogServiceException(failedServiceException);
        //    }
        //}
        //private async ValueTask<RISs> TryCatch(ReturningFunction returningFunction)
        //{
        //    try
        //    {
        //        return await returningFunction();
        //    }
        //    catch (RecordNotFoundException notFoundException)
        //    {
        //        throw notFoundException;
        //    }
        //    catch (RecordAlreadyExistsException recordAlreadyExistsException)
        //    {
        //        throw recordAlreadyExistsException;
        //    }
        //    catch (InvalidValueException invalidValueException)
        //    {
        //        throw invalidValueException;
        //    }
        //    catch (RecordAlreadyPostedException recordAlreadyPostedException)
        //    {
        //        throw recordAlreadyPostedException;
        //    }
        //    catch (RecordRelationshipException recordRelationshipExistsException)
        //    {
        //        throw recordRelationshipExistsException;
        //    }
        //    catch (SqlException sqlException)
        //    {
        //        throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
        //    }
        //    catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
        //    {
        //        var recordLockedException = new RecordLockedException(dbUpdateConcurrencyException);

        //        throw exceptions.CreateAndLogDependencyException(recordLockedException);
        //    }
        //    catch (DbUpdateException dbUpdateException)
        //    {
        //        throw exceptions.CreateAndLogDependencyException(dbUpdateException);
        //    }
        //    catch (Exception exception)
        //    {
        //        var failedServiceException =
        //            new FailedServiceException(exception);

        //        throw exceptions.CreateAndLogServiceException(failedServiceException);
        //    }
        //}
        //private IQueryable<RIS_VM> TryCatch(ReturningQueryableVMFunction returningQueryableVMFunction)
        //{
        //    try
        //    {
        //        return returningQueryableVMFunction();
        //    }
        //    catch (SqlException sqlException)
        //    {
        //        throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
        //    }
        //    catch (Exception exception)
        //    {
        //        var failedServiceException =
        //            new FailedServiceException(exception);

        //        throw exceptions.CreateAndLogServiceException(failedServiceException);
        //    }
        //}
        //private IQueryable<RISs> TryCatch(ReturningQueryableFunction returningQueryableFunction)
        //{
        //    try
        //    {
        //        return returningQueryableFunction();
        //    }
        //    catch (SqlException sqlException)
        //    {
        //        throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
        //    }
        //    catch (Exception exception)
        //    {
        //        var failedServiceException =
        //            new FailedServiceException(exception);

        //        throw exceptions.CreateAndLogServiceException(failedServiceException);
        //    }
        //}        
        #endregion
    }
}