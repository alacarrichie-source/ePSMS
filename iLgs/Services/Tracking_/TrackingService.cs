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

namespace iLgs.Services.Tracking_
{
    public interface ITrackingService
    {
        IQueryable<TrackingVM> GetByMastId(Guid mastId);
        ValueTask<TrackingVM> GetByIdAsync(Guid? id);

        ValueTask<TrackingVM> CreateAsync(TrackingVM model, string user, DateTime date);
        ValueTask<TrackingVM> UpdateAsync(TrackingVM model, string user, DateTime date);
        ValueTask<TrackingVM> DeleteAsync(TrackingVM model, string user, DateTime date);
    }

    public class TrackingService : BaseValidator, ITrackingService
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly IExceptionService<Tracking> _exceptionService;
        private readonly IExceptionService<TrackingVM> _vmExceptionService;
        private readonly IUserService _userService;

        public TrackingService(AppManEntities db, IExceptionService<Tracking> exceptionService,
            IExceptionService<TrackingVM> vmExceptionService,
            IUserService userService)
        {
            _db = db;
            _exceptionService = exceptionService;
            _vmExceptionService = vmExceptionService;
            _getDisplayName = Utility.GetDisplayName<Tracking>;
            _userService = userService;
        }

        private static Expression<Func<Tracking, TrackingVM>> Projection
        = s => new TrackingVM
        {
            Id = s.Id,
            CodeMastId = s.CodeMastId,
            TrackNo = s.TrackNo,
            TrackDate = s.TrackDate,
            RefId = s.RefId,
            Description = s.Description,
            InsertedBy = s.InsertedBy,
            InsertedDt = s.InsertedDt,
            TrackName = s.CodeMast.Description
        };

        public IQueryable<TrackingVM> GetByMastId(Guid mastId)
        {
            var data = _db.Trackings.Where(w => w.CodeMast.Id == mastId)
                .Select(Projection).AsNoTracking();
            return data;
        }

        public async ValueTask<TrackingVM> GetByIdAsync(Guid? id)
        {
            var data = await _db.Trackings.Select(Projection).FirstOrDefaultAsync(f => f.Id == id);
            return data;
        }

        public ValueTask<TrackingVM> CreateAsync(TrackingVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;
            model.TrackNo = string.IsNullOrWhiteSpace(model.TrackNo) ? (await NexTrackNo(model.CodeMastId)) : model.TrackNo;

            var entity = new Tracking();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.Trackings.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<TrackingVM> UpdateAsync(TrackingVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            var entity = _db.Trackings.Find(model.Id);
            ValidateRecord(entity, model.Id);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            ValidateFields(model, Mode.EDIT);
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.Trackings.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<TrackingVM> DeleteAsync(TrackingVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            Tracking entity = await _db.Trackings.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.Trackings.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.Trackings.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        private void MapModelToEntityFields(Tracking entity, TrackingVM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
                entity.CodeMastId = model.CodeMastId;
                entity.RefId = model.RefId;
            }

            entity.TrackNo = model.TrackNo;
            entity.TrackDate = model.TrackDate;
            entity.UpdatedDt = model.UpdatedDt;
            entity.UpdatedBy = model.UpdatedBy;
        }

        private async ValueTask<string> NexTrackNo(Guid? mastId)
        {
            var forYear = DateTime.Now.Year;
            var trackCode = (await _db.CodeMasts.FindAsync(mastId)).Code;
            var trackNo = (await _db.Trackings.Where(w => w.CodeMastId == mastId && w.TrackDate.Value.Year == forYear).FirstOrDefaultAsync())?.TrackNo;
            if (string.IsNullOrWhiteSpace(trackNo))
            {
                return trackCode.Trim() + "-" + forYear.ToString() + "-0001";
            }

            var sequence = (int.Parse(trackNo.Split('-')[2]) + 1).ToString();
            return trackCode.Trim() + "-" + forYear.ToString() + "-" + sequence.PadLeft(4, '0');
        }

        private void ValidateIfNull(TrackingVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(Tracking entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }

        private void ValidateFields(TrackingVM model, Mode mode)
        {
            if (model.CodeMastId == null)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.CodeMastId)), "Field is required.");
            }
            else
            {
                if (!_db.CodeMasts.Any(a => a.Id == model.CodeMastId))
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.CodeMastId)), "Invalid Value.");
                }
                else
                {
                    if (model.RefId == null)
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.RefId)), "Field is required.");
                    }
                    else
                    {
                        if (mode == Mode.ADD)
                        {
                            if (_db.Trackings.Where(w => w.CodeMastId == model.CodeMastId && w.RefId == model.RefId).Any())
                            {
                                _imex.UpsertDataList(_getDisplayName(nameof(model.RefId)), "Already Exists.");
                            }
                        }
                        else if (mode == Mode.EDIT)
                        {
                            if (_db.Trackings.Where(w => w.CodeMastId == model.CodeMastId && w.RefId == model.RefId && w.Id != model.Id).Any())
                            {
                                _imex.UpsertDataList(_getDisplayName(nameof(model.RefId)), "Already Exists.");
                            }
                        }
                    }
                }
            }

            _imex.ThrowIfContainsErrors();
        }
    }
}