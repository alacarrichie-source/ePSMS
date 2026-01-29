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
    public interface INotificationMessageService
    {
        IQueryable<NotificationMessageVM> GetAll(Guid? notificationId);
        ValueTask<NotificationMessageVM> GetByIdAsync(Guid? id);
        ValueTask<int?> GetNotificationCountAsync(string userId);

        ValueTask<NotificationMessageVM> NotifyUsers(string notificationName, string message, string description, string user, DateTime date);
        ValueTask<NotificationMessageVM> CreateAsync(NotificationMessageVM model, string user, DateTime date);
        ValueTask<NotificationMessageVM> DeleteAsync(NotificationMessageVM model, string user, DateTime date);
    }

    public class NotificationMessageService : BaseValidator, INotificationMessageService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<NotificationMessageVM> _vmExceptionService;
        private readonly IExceptionService<NotificationMessageStatuVM> _nmsExceptionService;
        private readonly IUserService _userService;
        private readonly INotificationMessageStatusService _notificationMessageStatusService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        public NotificationMessageService(AppManEntities db)
        {
            _db = db;
            _exceptions = new CreateAndLogExceptions();
            _vmExceptionService = new ExceptionService<NotificationMessageVM>();
            _nmsExceptionService = new ExceptionService<NotificationMessageStatuVM>();
            _userService = new UserService(_db);
            _notificationMessageStatusService = new NotificationMessageStatusService(_db);
            _getDisplayName = propertyName => Utility.GetDisplayName<NotificationMessageVM>(propertyName);
        }

        //public NotificationMessageService(AppManEntities db,
        //    ICreateAndLogExceptions exceptions,
        //    IExceptionService<NotificationMessageVM> vmExceptionService,
        //    IExceptionService<NotificationMessageStatuVM> nmsExceptionService,
        //    IUserService userService,
        //    INotificationMessageStatusService notificationMessageStatusService)
        //{
        //    _db = db;
        //    _exceptions = exceptions;
        //    _vmExceptionService = vmExceptionService;
        //    _nmsExceptionService = nmsExceptionService;
        //    _userService = userService;
        //    _notificationMessageStatusService = notificationMessageStatusService;
        //    _getDisplayName = propertyName => Utility.GetDisplayName<NotificationMessageVM>(propertyName);
        //}

        private Expression<Func<NotificationMessage, NotificationMessageVM>> Projection()
        {
            return s => new NotificationMessageVM
            {
                Id = s.Id,
                NotificationId = s.NotificationId,
                Message = s.Message,
                CreatedBy = s.CreatedBy,
                CreatedDt = s.CreatedDt
            };
        }

        public IQueryable<NotificationMessageVM> GetAll(Guid? notificationId)
        {
            var data = _db.NotificationMessages.Where(w => w.NotificationId == notificationId).Select(Projection()).AsNoTracking();
            return data;
        }

        public async ValueTask<NotificationMessageVM> GetByIdAsync(Guid? id)
        {
            var data = await _db.NotificationMessages
                .Where(w => w.Id == id)
                .Select(Projection()).AsNoTracking().FirstOrDefaultAsync();
            return data;
        }

        public async ValueTask<int?> GetNotificationCountAsync(string userId)
        {
            var data  = await _db.NotificationMessageStatus.Where(w => w.NotificationUser.UserId == userId && w.IsRead.Value != true).CountAsync();
            return data;
        }

        public ValueTask<NotificationMessageVM> NotifyUsers(string notificationName, string message, string description, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            var notification = await _db.Notifications.FirstOrDefaultAsync(f => f.Name == notificationName);
            if (notification == null)
            {
                throw new NotFoundException($"Notification Setup for {notificationName} not found.");
            }

            var model = new NotificationMessageVM()
            {
                NotificationId = notification.Id,
                Message = message, 
                Description = description,
                CreatedBy = user,
                CreatedDt = date
            };

            model = await CreateAsync(model, user, date);

            var hub = new NotificationHub();
            var notificationUsers = await _db.NotificationUsers.Where(w => w.NotificationId == notification.Id).ToListAsync();
            foreach(var notificationUser in notificationUsers)
            {
                var userName = _userService.GetById(notificationUser.UserId)?.UserName;
                if (!string.IsNullOrEmpty(userName) && userName != user)
                {                    
                    hub.RefreshUserNotification(userName);
                }
            }

            return model;
        });

        public ValueTask<NotificationMessageVM> CreateAsync(NotificationMessageVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateFields(model, Mode.ADD);

            model.Id = Guid.NewGuid();
            model.CreatedBy = user;
            model.CreatedDt = date;
            
            var entity = new NotificationMessage();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.NotificationMessages.Add(entity);
            await _db.SaveChangesAsync();

            await CreateNotificationMessageStatusAsync(model.Id);

            return model;
        });

        private async ValueTask CreateNotificationMessageStatusAsync(Guid? notificationMessageId)
        {
            var notificationMessage = await _db.NotificationMessages
                .Include(i => i.Notification.NotificationUsers)
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == notificationMessageId);            

            foreach (var notificationUser in notificationMessage.Notification.NotificationUsers)
            {
                var nsm = new NotificationMessageStatu()
                {
                    Id = Guid.NewGuid(),
                    NotificationMessageId = notificationMessageId,
                    NotificationUserId = notificationUser.Id,
                    IsRead = false
                };
                _db.NotificationMessageStatus.Add(nsm);
            }
            await _db.SaveChangesAsync();                
        }
        
        public ValueTask<NotificationMessageVM> DeleteAsync(NotificationMessageVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);

            var entity = await _db.NotificationMessages.FirstOrDefaultAsync(f => f.Id == model.Id);
            ValidateRecord(entity, model.Id);

            _db.NotificationMessages.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();
            return model;
        });

        private void MapModelToEntityFields(NotificationMessage entity, NotificationMessageVM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.NotificationId = model.NotificationId;
                entity.CreatedBy = model.CreatedBy;
                entity.CreatedDt = model.CreatedDt;
            }

            entity.Message = model.Message;
            entity.Description = model.Description;
        }

        private void ValidateIfNull(NotificationMessageVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(NotificationMessage entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }

        private void ValidateFields(NotificationMessageVM model, Mode mode)
        {
            _imex = new InvalidModelException();

            if (string.IsNullOrWhiteSpace(model.Message))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Message)), "Field is required.");
            }
            
            _imex.ThrowIfContainsErrors();
        }
    }
}