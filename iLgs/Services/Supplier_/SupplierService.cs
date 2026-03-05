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

namespace iLgs.Services.Supplier_
{
    public interface ISupplierService
    {
        IQueryable<SupplierVM> GetAll();
        ValueTask<SupplierVM> GetByIdAsync(Guid? id);

        ValueTask<SupplierVM> CreateAsync(SupplierVM model, string user, DateTime date);
        ValueTask<SupplierVM> UpdateAsync(SupplierVM model, string user, DateTime date);
        ValueTask<SupplierVM> DeleteAsync(SupplierVM model, string user, DateTime date);
    }

    public class SupplierService : BaseValidator, ISupplierService
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly IExceptionService<Supplier> _exceptionService;
        private readonly IExceptionService<SupplierVM> _vmExceptionService;

        public SupplierService(AppManEntities db)
        {
            _db = db;
            _exceptionService = new ExceptionService<Supplier>();
            _vmExceptionService = new ExceptionService<SupplierVM>();
            _getDisplayName = Utility.GetDisplayName<Supplier>;            
        }

        private static Expression<Func<Supplier, SupplierVM>> Projection
        = s => new SupplierVM
        {
            Id = s.Id,
            Code = s.Code,
            Name = s.Name,
            Address = s.Address,
            BusinessName = s.BusinessName,
            DTI = s.DTI,
            SEC = s.SEC,
            BIN = s.BIN,
            TIN = s.TIN,
            ContactNos = s.ContactNos,
            Email = s.Email,
            IsCorp = s.IsCorp,
            IsVat = s.IsVat,
            ZipCode = s.ZipCode,
            InsertedBy = s.InsertedBy,
            InsertedDt = s.InsertedDt,
            UpdatedBy = s.UpdatedBy,
            UpdatedDt = s.UpdatedDt
        };

        public IQueryable<SupplierVM> GetAll()
        {
            var data = _db.Suppliers.Select(Projection).AsNoTracking();
            return data;
        }

        public async ValueTask<SupplierVM> GetByIdAsync(Guid? id)
        {
            var data = await _db.Suppliers.Select(Projection).FirstOrDefaultAsync(f => f.Id == id);
            return data;
        }

        public ValueTask<SupplierVM> CreateAsync(SupplierVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            model.Code = NextCode();

            var entity = new Supplier();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.Suppliers.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<SupplierVM> UpdateAsync(SupplierVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            var entity = _db.Suppliers.Find(model.Id);
            ValidateRecord(entity, model.Id);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            ValidateFields(model, Mode.EDIT);
            MapModelToEntityFields(entity, model, Mode.ADD);

            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<SupplierVM> DeleteAsync(SupplierVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.Suppliers.FindAsync(model.Id);
            ValidateRecord(entity, model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();

            _db.Suppliers.Remove(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        private void MapModelToEntityFields(Supplier entity, SupplierVM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
                entity.Code = model.Code;
            }


            entity.Name = model.Name?.Trim();
            entity.Address = model.Address?.Trim();
            entity.BusinessName = model.BusinessName?.Trim() ?? "";
            entity.DTI = model.DTI?.Trim() ?? "";
            entity.SEC = model.SEC?.Trim() ?? "";
            entity.BIN = model.BIN?.Trim() ?? "";
            entity.TIN = model.TIN?.Trim() ?? "";
            entity.ContactNos = model.ContactNos?.Trim() ?? "";
            entity.Email = model.Email?.Trim() ?? "";
            entity.IsCorp = model.IsCorp;
            entity.IsVat = model.IsVat;
            entity.ZipCode = model.ZipCode?.Trim() ?? "";

            entity.UpdatedDt = model.UpdatedDt;
            entity.UpdatedBy = model.UpdatedBy;
        }

        private string NextCode()
        {
            var forYear = DateTime.Now.Year.ToString();
            var code = _db.Suppliers.Where(w => w.Code.Substring(0, 4) == forYear).OrderByDescending(o => o.Code).Select(s => s.Code).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(code))
            {
                return forYear.ToString() + "00001";
            }

            var sequence = (int.Parse(code.Substring(4, 5)) + 1).ToString();
            return forYear.ToString() + sequence.PadLeft(5, '0');
        }

        private void ValidateIfNull(SupplierVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(Supplier entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }

        private void ValidateFields(SupplierVM model, Mode mode)
        {
            _imex = new InvalidModelException();

            if (string.IsNullOrWhiteSpace(model.Code))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Code)), "Field is required.");
            }
            else
            {
                if (mode == Mode.ADD)
                {
                    if (_db.Suppliers.Where(w => w.Code == model.Code).Any())
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.Code)), "Already Exists.");
                    }
                }
                else if (mode == Mode.EDIT)
                {
                    if (_db.Suppliers.Where(w => w.Code == model.Code && w.Id != model.Id).Any())
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.Code)), "Already Exists.");
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Name)), "Field is required.");
            }

            if (string.IsNullOrWhiteSpace(model.Address))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Address)), "Field is required.");
            }

            if (string.IsNullOrWhiteSpace(model.TIN))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.TIN)), "Field is required.");
            }

            _imex.ThrowIfContainsErrors();
        }
    }
}