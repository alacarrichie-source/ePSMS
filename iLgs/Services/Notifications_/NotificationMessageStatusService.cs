using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.CustodianReports
{
    public interface INotificationMessageStatusService
    {
        IQueryable<NotificationMessageStatuVM> GetAllByNotificationMessageId(Guid? notificationMessageId);
        IQueryable<NotificationMessageStatuVM> GetAllByNotificationUserId(Guid? notificationUserId);
        ValueTask<NotificationMessageStatuVM> GetByIdAsync(Guid? id);

        ValueTask<NotificationMessageStatuVM> CreateAsync(NotificationMessageStatuVM model, string user, DateTime date);
        ValueTask<NotificationMessageStatuVM> UpdateAsync(NotificationMessageStatuVM model, string user, DateTime date);
        ValueTask<NotificationMessageStatuVM> DeleteAsync(NotificationMessageStatuVM model, string user, DateTime date);
    }

    public class NotificationMessageStatusService : BaseValidator, INotificationMessageStatusService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<NotificationMessageStatuVM> _vmExceptionService;
        private readonly IUserService _userService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        public NotificationMessageStatusService(AppManEntities db,
            ICreateAndLogExceptions exceptions,
            IExceptionService<NotificationMessageStatuVM> vmExceptionService,
            IUserService userService)
        {
            _db = db;
            _exceptions = exceptions;
            _vmExceptionService = vmExceptionService;
            _userService = userService;
            _getDisplayName = propertyName => Utility.GetDisplayName<NotificationMessageStatuVM>(propertyName);
        }

        private Expression<Func<NotificationMessageStatu, NotificationMessageStatuVM>> Projection()
        {
            return s => new NotificationMessageStatuVM
            {
                Id = s.Id,
                NotificationMessageId = s.NotificationMessageId,
                NotificationUserId = s.NotificationUserId,
                IsRead = s.IsRead,
                IsReadAt = s.IsReadAt,
                UpdatedBy = s.UpdatedBy,
                UpdatedDt = s.UpdatedDt                
            };
        }

        public IQueryable<NotificationMessageStatuVM> GetAllByNotificationMessageId(Guid? notificationMessageId)
        {
            var data = _db.NotificationMessageStatus
                .Where(w => w.NotificationMessageId == notificationMessageId)
                .Select(Projection()).AsNoTracking();
            return data;
        }

        public IQueryable<NotificationMessageStatuVM> GetAllByNotificationUserId(Guid? notificationUserId)
        {
            var data = _db.NotificationMessageStatus
                .Where(w => w.NotificationUserId == notificationUserId)
                .Select(Projection()).AsNoTracking();
            return data;
        }

        public async ValueTask<NotificationMessageStatuVM> GetByIdAsync(Guid? id)
        {
            var data = await _db.NotificationMessageStatus
                .Where(w => w.Id == id)
                .Select(Projection()).AsNoTracking().FirstOrDefaultAsync();
            return data;
        }

        public ValueTask<NotificationMessageStatuVM> CreateAsync(NotificationMessageStatuVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateFields(model, Mode.ADD);

            model.Id = Guid.NewGuid();
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = new NotificationMessageStatu();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.NotificationMessageStatus.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<NotificationMessageStatuVM> UpdateAsync(NotificationMessageStatuVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateFields(model, Mode.EDIT);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.NotificationMessageStatus.FindAsync(model.Id);
            ValidateRecord(entity, model.Id);

            MapModelToEntityFields(entity, model, Mode.EDIT);

            _db.NotificationMessageStatus.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<NotificationMessageStatuVM> DeleteAsync(NotificationMessageStatuVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.NotificationMessageStatus.FirstOrDefaultAsync(f => f.Id == model.Id);
            ValidateRecord(entity, model.Id);

            _db.NotificationMessageStatus.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();
            return model;
        });

        private void MapModelToEntityFields(NotificationMessageStatu entity, NotificationMessageStatuVM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.NotificationMessageId = model.NotificationMessageId;
                entity.NotificationUserId = model.NotificationUserId;
            }

            entity.IsRead = model.IsRead;
            entity.IsReadAt = model.IsReadAt;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        private void ValidateIfNull(NotificationMessageStatuVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(NotificationMessageStatu entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }

        private void ValidateFields(NotificationMessageStatuVM model, Mode mode)
        {
            _imex = new InvalidModelException();

            //if (string.IsNullOrWhiteSpace(model.UserId))
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.UserId)), "Field is required.");
            //}
            //else
            //{
            //    var data = _db.NotificationMessageStatus.FirstOrDefault(f => f.NotificationId == model.NotificationId
            //        && f.UserId == model.UserId);
            //    if (data != null)
            //    {
            //        if (mode == Mode.ADD)
            //        {
            //            _imex.UpsertDataList(_getDisplayName(nameof(model.UserId)), "Already exists.");
            //        }
            //        else if (data.Id != model.Id)
            //        {
            //            _imex.UpsertDataList(_getDisplayName(nameof(model.UserId)), "Already exists.");
            //        }
            //    }
            //}

            _imex.ThrowIfContainsErrors();
        }
    }
}