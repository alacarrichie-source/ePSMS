using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.PurchaseOrder;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.RPC
{
    public interface IRpcPpeService
    {
        IQueryable<RpcPpe> GetAll();
        IQueryable<RpcPpe> GetAllByAccountGroup(int? accountGroup);
        IQueryable<RpcPpe> GetAllByDepartmentAccountGroup(Guid? deptId, int? accountGroup);
        ValueTask<RpcPpe> GetByIdAsync(Guid? id);
        string GetAccountGroupMenuId(AccountGroup accountGroup);
        string GetAccountGroupMenuId(int? accountGroup);
        ValueTask<RpcPpe> PostAsync(Guid id, string user, DateTime date);
        ValueTask<RpcPpe> UnPostAsync(Guid id, string user, DateTime date);
        ValueTask<RpcPpe> CreateAsync(RpcPpe model, string user, DateTime date);
        ValueTask<RpcPpe> UpdateAsync(RpcPpe model, string user, DateTime date);
        ValueTask<RpcPpe> DeleteAsync(RpcPpe model, string user, DateTime date);
    }

    public class RpcPpeService : BaseValidator, IRpcPpeService
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RpcPpe> _exceptionService = new ExceptionService<RpcPpe>();
        private readonly IOrderService _orderService;
        private readonly ICodextnService _codextnService;

        public RpcPpeService(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<RpcPpe>(propertyName);
            _orderService = new OrderService(_db);
            _codextnService = new CodextnService(_db);
        }

        public ValueTask<RpcPpe> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.RpcPpes.FindAsync(id);
            return data;
        });

        public ValueTask<RpcPpe> GetByAsOfAsync(DateTime? AsOf) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.RpcPpes.Where(w => w.AsOf == AsOf).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<RpcPpe> GetAll() =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.RpcPpes.AsNoTracking().AsQueryable();
            return data;
        });

        public IQueryable<RpcPpe> GetAllByAccountGroup(int? accountGroup) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.RpcPpes.AsNoTracking().Where(w => w.AccountGroup == accountGroup).AsQueryable();
            return data;
        });

        public IQueryable<RpcPpe> GetAllByDepartmentAccountGroup(Guid? deptId, int? accountGroup) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.RpcPpes.AsNoTracking().Where(w => w.DeptId == deptId && w.AccountGroup == accountGroup).AsQueryable();
            return data;
        });

        public string GetAccountGroupMenuId(AccountGroup accountGroup)
        {
            return GetAccountGroupMenuId((int?)accountGroup);
        }

        public string GetAccountGroupMenuId(int? accountGroup)
        {
            string menuId = "";
            if (accountGroup == (int?)AccountGroup.PPE)
            {
                menuId = "rpc_report_equipment";
            }
            else if (accountGroup == (int?)AccountGroup.SUPPLIES)
            {
                menuId = "rpc_report_supplies";
            }
            else if (accountGroup == (int?)AccountGroup.VEHICLE)
            {
                menuId = "rpc_report_vehicle";
            }
            return menuId;
        }

        public ValueTask<RpcPpe> PostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateOnPost(id);
            var entity = await _db.RpcPpes.FindAsync(id);

            entity.PostedBy = user;
            entity.PostedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.RpcPpes.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        public ValueTask<RpcPpe> UnPostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateOnUnpost(id);
            var entity = await _db.RpcPpes.FindAsync(id);

            //// check if in disposal
            //if(_db.RpcPpeItems.Where(w => w.Report`Id == id && w.CustodianDisposalItems.Any()).Any())
            //{
            //    throw new RecordAlreadyExistsException("Record already in disposal entry, cannot unpost!");
            //}

            entity.PostedBy = "";
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.RpcPpes.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        public ValueTask<RpcPpe> CreateAsync(RpcPpe model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {

            ValidateOnCreate(model);      
            if (!model.DeptId.HasValue)
            {
                model.Department = null;
            }
            await _db.Database.ExecuteSqlCommandAsync("Exec RpcPpe_Generate {0}, {1}, {2}, {3}, {4}", model.AccountGroup, model.AsOf, model.DeptId, user, date);            

            return model;
        });

        public ValueTask<RpcPpe> UpdateAsync(RpcPpe model, string user, DateTime date) =>
       _exceptionService.TryCatch(async () =>
       {
           ValidateOnUpdate(model);

           var entity = await _db.RpcPpes.FindAsync(model.Id);

           model.UpdatedBy = user;
           model.UpdatedDt = date;

           MapModelToEntityFields(entity, model, Mode.EDIT);

           _db.RpcPpes.Attach(entity);
           _db.Entry(entity).State = EntityState.Modified;
           await _db.SaveChangesAsync();
           return model;
       });

        public ValueTask<RpcPpe> DeleteAsync(RpcPpe model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateOnDelete(model);

            var entity = await _db.RpcPpes.Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RpcPpes.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.RpcPpes.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public void MapModelToEntityFields(RpcPpe entity, RpcPpe model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            entity.AsOf = model.AsOf;
            entity.Fund = model.Fund;
            entity.AccountGroup = model.AccountGroup;
            entity.DeptId = model.DeptId;
            entity.Department = model.Department;
            entity.CertifiedCorrectBy = model.CertifiedCorrectBy;
            entity.ApprovedBy = model.ApprovedBy;
            entity.VerifiedBy = model.VerifiedBy;
            entity.PostedBy = model.PostedBy;
            entity.PostedDt = model.PostedDt;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        #region VALIDATORS
        public void ValidateOnCreate(RpcPpe model)
        {
            ValidateIfNull(model);
            ValidateDuplicateOnCreate(model);
            ValidateFieldsOnCreateUpdate(model);
        }

        public void ValidateOnUpdate(RpcPpe model)
        {
            ValidateIfNull(model);
            ValidateIfPosted(model.Id);
            ValidateDuplicateOnUpdate(model);
            ValidateFieldsOnCreateUpdate(model);
        }

        public void ValidateOnDelete(RpcPpe model)
        {
            ValidateIfNull(model);
            ValidateRecord(model.Id);
            ValidateIfPosted(model.Id);
        }

        public void ValidateDuplicateOnCreate(RpcPpe model)
        {
            if (_db.RpcPpes.Any(w => w.AsOf == model.AsOf
                && w.Fund == model.Fund
                && w.DeptId == model.DeptId
                && w.AccountGroup == model.AccountGroup))
            {
                throw new RecordAlreadyExistsException();
            }
        }

        public void ValidateIfPosted(Guid id)
        {
            var data = _db.RpcPpes.AsNoTracking().Where(w => w.Id == id && w.PostedDt != null).FirstOrDefault();
            if (data != null)
            {
                var msg = $"Record already posted by {data.PostedBy} on {data.PostedDt}";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        public void ValidateDuplicateOnUpdate(RpcPpe model)
        {
            if (_db.RpcPpes.Any(w => w.Id != model.Id
                && w.AsOf == model.AsOf
                && w.Fund == model.Fund
                && w.DeptId == model.DeptId
                && w.AccountGroup == model.AccountGroup))
            {
                throw new RecordAlreadyExistsException();
            }
        }

        public void ValidateFieldsOnCreateUpdate(RpcPpe model)
        {
            if (!model.AsOf.HasValue)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.AsOf)), "Field is required.");
            }

            //if (string.IsNullOrWhiteSpace(model.Fund))
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.Fund)), "Field is required.");
            //}

            //if (!model.DeptId.HasValue)
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.DeptId)), "Field is required.");
            //}
            //else
            //{
            //    if (!_codextnService.IsValidMastCodeId("DEPARTMENTS", model.DeptId))
            //    {
            //        _imex.UpsertDataList(_getDisplayName(nameof(model.DeptId)), "Invalid value");
            //    }
            //}

            if (model.DeptId.HasValue)
            {
                if (!_codextnService.IsValidMastCodeId("DEPARTMENTS", model.DeptId))
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.DeptId)), "Invalid value");
                }
            }

            if (!model.AccountGroup.HasValue)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.AccountGroup)), "Field is required.");
            }

            _imex.ThrowIfContainsErrors();
        }

        public void ValidateOnPost(Guid id)
        {
            var entity = _db.RpcPpes.Find(id);
            if (entity == null)
            {
                throw new NotFoundException(id);
            }

            if (!string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(entity.PostedBy)), "Field is required.");
            }

            if (string.IsNullOrWhiteSpace(entity.CertifiedCorrectBy))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(entity.CertifiedCorrectBy)), "Field is required.");
            }

            if (string.IsNullOrWhiteSpace(entity.ApprovedBy))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(entity.ApprovedBy)), "Field is required.");
            }

            if (string.IsNullOrWhiteSpace(entity.VerifiedBy))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(entity.VerifiedBy)), "Field is required.");
            }

            _imex.ThrowIfContainsErrors();
        }

        public void ValidateOnUnpost(Guid id)
        {
            var entity = _db.RpcPpes.Find(id);
            if (entity == null)
            {
                throw new NotFoundException(id);
            }

            if (string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordNotYetPostedException($"Record is not yet posted!");
            }
        }

        private void ValidateRecord(Guid id)
        {
            if (!_db.RpcPpes.Any(a => a.Id == id))
            {
                throw new NotFoundException(id);
            }
        }

        private static void ValidateIfNull(RpcPpe model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }
        #endregion
    }
}