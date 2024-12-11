using ClosedXML.Excel;
using iLgs.Controllers;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Interfaces;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services.Codes
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
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<ItemCodeRequestVM> _exceptionService = new ExceptionService<ItemCodeRequestVM>();
        private readonly ICodextnService _codextnService;
        private readonly IUserService _userService;

        public ItemCodeRequestService(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<ItemCodeRequestVM>(propertyName);
            _codextnService = new CodextnService(_db);
            _userService = new UserService(_db);
        }

        private static Expression<Func<ItemCodeRequest, ItemCodeRequestVM>> Projection
        = s => new ItemCodeRequestVM
        {
            Id = s.Id,
            DepartmentId = s.DepartmentId,
            Description = s.Description,
            Remarks = s.Remarks,
            EstCost = s.EstCost,
            IsConsumable = s.IsConsumable,
            IsForDistribution = s.IsForDistribution,
            IsIncorporated = s.IsIncorporated,
            Status = s.Status,
            InsertedBy = s.InsertedBy,
            InsertedDt = s.InsertedDt,
            // transient
            Department = s.Codextn.Description
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

            var entity = new ItemCodeRequest();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.ItemCodeRequests.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<ItemCodeRequestVM> UpdateAsync(ItemCodeRequestVM model, string user, DateTime date) =>
       _exceptionService.TryCatch(async () =>
       {
           ValidateIfNull(model);

           var entity = await _db.ItemCodeRequests.FindAsync(model.Id);
           ValidateRecord(entity, model.Id);
           ValidateFields(model, Mode.ADD);

           model.UpdatedBy = user;
           model.UpdatedDt = date;

           MapModelToEntityFields(entity, model, Mode.EDIT);

           _db.ItemCodeRequests.Attach(entity);
           _db.Entry(entity).State = EntityState.Modified;
           await _db.SaveChangesAsync();
           return model;
       });

        public ValueTask<ItemCodeRequestVM> DeleteAsync(ItemCodeRequestVM model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            var entity = await _db.ItemCodeRequests.Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            ValidateRecord(entity, model.Id);

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
            }

            entity.DepartmentId = model.DepartmentId;
            entity.Description = model.Description;
            entity.Remarks = model.Remarks;
            entity.EstCost = model.EstCost;
            entity.IsConsumable = model.IsConsumable;
            entity.IsForDistribution = model.IsForDistribution;
            entity.IsIncorporated = model.IsIncorporated;
            entity.Status = model.Status;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
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
    }
}