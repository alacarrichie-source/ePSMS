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
    public interface INotificationUserService
    {
        IQueryable<NotificationUserVM> GetAll(Guid? notificationId);
        ValueTask<NotificationUserVM> GetByIdAsync(Guid? id);

        ValueTask<NotificationUserVM> CreateAsync(NotificationUserVM model, string user, DateTime date);
        ValueTask<NotificationUserVM> UpdateAsync(NotificationUserVM model, string user, DateTime date);
        ValueTask<NotificationUserVM> DeleteAsync(NotificationUserVM model, string user, DateTime date);
    }

    public class NotificationUserService : BaseValidator, INotificationUserService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<NotificationUserVM> _vmExceptionService;
        private readonly IUserService _userService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        public NotificationUserService(AppManEntities db)
        {
            _db = db;
            _exceptions = new CreateAndLogExceptions();
            _vmExceptionService = new ExceptionService<NotificationUserVM>();
            _userService = new UserService(_db);
            _getDisplayName = propertyName => Utility.GetDisplayName<NotificationUserVM>(propertyName);
        }

        private Expression<Func<NotificationUser, NotificationUserVM>> Projection()
        {
            return s => new NotificationUserVM
            {
                Id = s.Id,
                NotificationId = s.NotificationId,
                UserId = s.UserId,
                InsertedBy = s.InsertedBy,
                InsertedDt = s.InsertedDt,
                UpdatedBy = s.UpdatedBy,
                UpdatedDt = s.UpdatedDt,
                UserName = s.AspNetUser.UserName,
                UserDepartment = s.AspNetUser.UserProfile.Department,
                UserFullName = s.AspNetUser.UserProfile.NameFull
            };
        }

        public IQueryable<NotificationUserVM> GetAll(Guid? notificationId)
        {
            var data = _db.NotificationUsers.Where(w => w.NotificationId == notificationId).Select(Projection()).AsNoTracking();
            return data;
        }

        public async ValueTask<NotificationUserVM> GetByIdAsync(Guid? id)
        {
            var data = await _db.NotificationUsers
                .Where(w => w.Id == id)
                .Select(Projection()).AsNoTracking().FirstOrDefaultAsync();
            return data;
        }

        public ValueTask<NotificationUserVM> CreateAsync(NotificationUserVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateFields(model, Mode.ADD);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;
            
            var entity = new NotificationUser();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.NotificationUsers.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<NotificationUserVM> UpdateAsync(NotificationUserVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateFields(model, Mode.EDIT);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.NotificationUsers.FindAsync(model.Id);
            ValidateRecord(entity, model.Id);

            MapModelToEntityFields(entity, model, Mode.EDIT);

            _db.NotificationUsers.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<NotificationUserVM> DeleteAsync(NotificationUserVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.NotificationUsers.FirstOrDefaultAsync(f => f.Id == model.Id);
            ValidateRecord(entity, model.Id);

            _db.NotificationUsers.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();
            return model;
        });

        private void MapModelToEntityFields(NotificationUser entity, NotificationUserVM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.NotificationId = model.NotificationId;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            entity.UserId = model.UserId;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        private void ValidateIfNull(NotificationUserVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(NotificationUser entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }

        private void ValidateFields(NotificationUserVM model, Mode mode)
        {
            _imex = new InvalidModelException();

            if (string.IsNullOrWhiteSpace(model.UserId))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.UserId)), "Field is required.");
            }
            else
            {
                var data = _db.NotificationUsers.FirstOrDefault(f => f.NotificationId == model.NotificationId                 
                    && f.UserId == model.UserId);
                if (data != null)
                {
                    if (mode == Mode.ADD)
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.UserId)), "Already exists.");
                    }
                    else if (data.Id != model.Id)
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.UserId)), "Already exists.");
                    }
                }
            }
            
            _imex.ThrowIfContainsErrors();
        }
    }
}