using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.CustodianReports;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.Items
{
    public interface IItemCodeRequestService
    {
        IQueryable<ItemCodeRequestVM> GetAll();
        IQueryable<ItemCodeRequestVM> GetAllByUser(string user);
        ValueTask<ItemCodeRequest> GetByIdAsync(Guid? id);
        ValueTask<ItemCodeRequestVM> CreateAsync(ItemCodeRequestVM model, string user, DateTime date);
        ValueTask<ItemCodeRequestVM> UpdateAsync(ItemCodeRequestVM model, string user, DateTime date);        
        ValueTask<ItemCodeRequestVM> DeleteAsync(ItemCodeRequestVM model, string user, DateTime date);
    }

    public class ItemCodeRequestService : BaseValidator, IItemCodeRequestService
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<ItemCodeRequestVM> _exceptionService;
        private readonly ICodextnService _codextnService;
        private readonly IUserService _userService;
        private readonly INotificationMessageService _notificationMessageService;

        public ItemCodeRequestService(AppManEntities db,
            ICreateAndLogExceptions exceptions,
            IExceptionService<ItemCodeRequestVM> exceptionService,
            ICodextnService codextnService,
            IUserService userService,
            INotificationMessageService notificationMessageService)
        {
            _db = db;
            _getDisplayName = Utility.GetDisplayName<ItemCodeRequestVM>;
            _exceptions = exceptions;
            _exceptionService = exceptionService;
            _codextnService = codextnService;
            _userService = userService;
            _notificationMessageService = notificationMessageService;
        }

        private static Expression<Func<ItemCodeRequest, ItemCodeRequestVM>> Projection
        = s => new ItemCodeRequestVM
        {
            Id = s.Id,
            RequestNo = s.RequestNo,
            DepartmentId = s.DepartmentId,
            Description = s.Description,
            Remarks = s.Remarks,
            EstCost = s.EstCost,
            IsConsumable = s.IsConsumable,
            IsForDistribution = s.IsForDistribution,
            IsIncorporated = s.IsIncorporated,
            Status = s.Status,
            StatusRemarks = s.StatusRemarks,
            InsertedBy = s.InsertedBy,
            InsertedDt = s.InsertedDt,
            // transient
            Department = s.Codextn.Description,
            StatusSw = false
        };

        public async ValueTask<ItemCodeRequest> GetByIdAsync(Guid? id)
        {
            var data = await _db.ItemCodeRequests.FindAsync(id);
            return data;
        }

        public IQueryable<ItemCodeRequestVM> GetAll() =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.ItemCodeRequests.AsNoTracking()
                .Include(i => i.Codextn)
                .Select(Projection).OrderByDescending(o => o.InsertedDt)
                .AsQueryable();
            return data;
        });

        public IQueryable<ItemCodeRequestVM> GetAllByUser(string user) =>
        _exceptionService.TryCatch(() =>
        {
            IQueryable<ItemCodeRequestVM> data = null;
            var isAdmin = _userService.IsUserNameAdmin(user);
            if (isAdmin)
            {
                data = GetAll();
            }
            else
            {
                data = _db.ItemCodeRequests.AsNoTracking()
                    .Include(i => i.Codextn)
                    .Where(w => w.InsertedBy == user)
                    .Select(Projection).OrderByDescending(o => o.InsertedDt)
                    .AsQueryable();
            }
            return data;
        });

        public ValueTask<ItemCodeRequestVM> CreateAsync(ItemCodeRequestVM model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateFields(model, Mode.ADD);
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.RequestNo = NextRequestNo((DateTime)model.InsertedDt);

            var entity = new ItemCodeRequest();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.ItemCodeRequests.Add(entity);
            await _db.SaveChangesAsync();

            var description = $"Source: {model.Url}, Request No: {model.RequestNo}";
            await _notificationMessageService.NotifyUsers("Item Code Request", "New", description, user, date);

            return model;
        });

        public ValueTask<ItemCodeRequestVM> UpdateAsync(ItemCodeRequestVM model, string user, DateTime date) =>
       _exceptionService.TryCatch(async () =>
       {
           ValidateIfNull(model);

           var entity = await _db.ItemCodeRequests.FindAsync(model.Id);
           ValidateRecord(entity, model.Id);
           model.UpdatedBy = user;
           model.UpdatedDt = date;

           if (model.StatusSw == true)
           {
               ValidateUser(model);

               entity.Status = model.Status;
               entity.StatusRemarks = model.StatusRemarks;
               entity.UpdatedBy = model.UpdatedBy;
               entity.UpdatedDt = model.UpdatedDt;
           }
           else
           {
               ValidateFields(model, Mode.ADD);
               ValidateStatus(entity);
               
               MapModelToEntityFields(entity, model, Mode.EDIT);
           }

           _db.ItemCodeRequests.Attach(entity);
           _db.Entry(entity).State = EntityState.Modified;
           await _db.SaveChangesAsync();

           var description = $"Source: {model.Url}, Request No: {model.RequestNo}";
           await _notificationMessageService.NotifyUsers("Item Code Request", "Update", description, user, date);

           return model;
       });

        
        public ValueTask<ItemCodeRequestVM> DeleteAsync(ItemCodeRequestVM model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            var entity = await _db.ItemCodeRequests.Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            ValidateRecord(entity, model.Id);
            ValidateStatus(entity);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.ItemCodeRequests.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.ItemCodeRequests.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public void MapModelToEntityFields(ItemCodeRequest entity, ItemCodeRequestVM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
                entity.RequestNo = model.RequestNo;
            }

            entity.DepartmentId = model.DepartmentId;
            entity.Description = model.Description;
            entity.Remarks = model.Remarks;
            entity.EstCost = model.EstCost;
            entity.IsConsumable = model.IsConsumable;
            entity.IsForDistribution = model.IsForDistribution;
            entity.IsIncorporated = model.IsIncorporated;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        private string NextRequestNo(DateTime date)
        {
            string yyyy = date.Year.ToString().Trim();
            string mm = date.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = _db.ItemCodeRequests.Where(w => w.InsertedDt.Value.Year == date.Year).OrderByDescending(o => o.RequestNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(data.RequestNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }

        private void ValidateIfNull(ItemCodeRequestVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(ItemCodeRequest entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }

        private void ValidateFields(ItemCodeRequestVM model, Mode mode)
        {
            if (model.DepartmentId == null || model.DepartmentId == Guid.Empty)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.DepartmentId)), "Field is required.");
            }
            else
            {
                if (!_codextnService.IsValidMastCodeId("LOCATIONS", model.DepartmentId))
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.DepartmentId)), "Invalid value.");
                }                
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Description)), "Field is required.");
            }

            if (!model.EstCost.HasValue)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.EstCost)), "Field is required.");
            }

            _imex.ThrowIfContainsErrors();
        }

        private void ValidateStatus(ItemCodeRequest entity)
        {
            if (!string.IsNullOrWhiteSpace(entity.Status) || !string.IsNullOrWhiteSpace(entity.StatusRemarks))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(entity.Status)), "Request already with status, cannot update.");
            }

            _imex.ThrowIfContainsErrors();
        }

        private void ValidateUser(ItemCodeRequestVM model)
        {
            var isAdmin = _userService.IsUserNameAdmin(model.UpdatedBy);
            if (!isAdmin)
            {
                throw new RecordLockedException($"Record can only be updated by an Admin.");
            }            
        }
    }
}