using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Items;
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
    public interface INotificationService
    {
        IQueryable<NotificationVM> GetAll();
        ValueTask<NotificationVM> GetByIdAsync(Guid? id);

        ValueTask<NotificationVM> CreateAsync(NotificationVM model, string user, DateTime date);
        ValueTask<NotificationVM> UpdateAsync(NotificationVM model, string user, DateTime date);
        ValueTask<NotificationVM> DeleteAsync(NotificationVM model, string user, DateTime date);        
    }

    public class NotificationService : BaseValidator, INotificationService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<NotificationVM> _vmExceptionService;
        private readonly IUserService _userService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        public NotificationService(AppManEntities db,
            ICreateAndLogExceptions exceptions,
            IExceptionService<NotificationVM> vmExceptionService,
            IUserService userService)
        {
            _db = db;
            _exceptions = exceptions;
            _vmExceptionService = vmExceptionService;
            _userService = userService;            
            _getDisplayName = propertyName => Utility.GetDisplayName<NotificationVM>(propertyName);
        }

        private Expression<Func<Notification, NotificationVM>> Projection()
        {
            return s => new NotificationVM
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                InsertedBy = s.InsertedBy,
                InsertedDt = s.InsertedDt,
                UpdatedBy = s.UpdatedBy,
                UpdatedDt = s.UpdatedDt                
            };
        }

        public IQueryable<NotificationVM> GetAll()
        {
            var data = _db.Notifications.Select(Projection()).AsNoTracking();
            return data;
        }

        public async ValueTask<NotificationVM> GetByIdAsync(Guid? id)
        {
            var data = await _db.Notifications
                .Where(w => w.Id == id)
                .Select(Projection()).AsNoTracking().FirstOrDefaultAsync();
            return data;
        }

        public ValueTask<NotificationVM> CreateAsync(NotificationVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateFields(model, Mode.ADD);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;
            
            var entity = new Notification();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.Notifications.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<NotificationVM> UpdateAsync(NotificationVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateFields(model, Mode.EDIT);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.Notifications.FindAsync(model.Id);
            ValidateRecord(entity, model.Id);
            
            MapModelToEntityFields(entity, model, Mode.EDIT);

            _db.Notifications.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<NotificationVM> DeleteAsync(NotificationVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.Notifications.FirstOrDefaultAsync(f => f.Id == model.Id);
            ValidateRecord(entity, model.Id);

            _db.Notifications.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();
            return model;
        });

        private void MapModelToEntityFields(Notification entity, NotificationVM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            entity.Name = model.Name;
            entity.Description = model.Description;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        private void ValidateIfNull(NotificationVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(Notification entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }

        private void ValidateFields(NotificationVM model, Mode mode)
        {
            _imex = new InvalidModelException();

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Name)), "Field is required.");
            }
            else
            {
                var data = _db.Notifications.FirstOrDefault(f => f.Name == model.Name);
                if (data != null)
                {
                    if (mode == Mode.ADD)
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.Name)), "Already exists.");
                    }
                    else if (data.Id != model.Id)
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.Name)), "Already exists.");
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Description)), "Field is required.");
            }
                        
            _imex.ThrowIfContainsErrors();
        }
    }
}