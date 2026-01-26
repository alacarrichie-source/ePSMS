using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.Procurement_
{
    public interface IProcurementOrderService
    {
        IQueryable<ProcurementOrderVM> GetAll();
        Task<ProcurementOrderVM> GetByIdAsync(Guid? id);        

        ValueTask<ProcurementOrderVM> CreateAsync(ProcurementOrderVM model, string user, DateTime date);
        ValueTask<ProcurementOrderVM> UpdateAsync(ProcurementOrderVM model, string user, DateTime date);
        ValueTask<ProcurementOrderVM> DeleteAsync(ProcurementOrderVM model, string user, DateTime date);

        IProcurementUnitGroupService UnitGroupService { get; }
    }

    public class ProcurementOrderService : BaseValidator, IProcurementOrderService
    {
        private readonly AppManEntities _db;
        private readonly IProcurementCommonService _procurementCommonService;
        private readonly ICodextnService _codextnService;
        private readonly ILocationBudgetService _locationBudgetService;
        private readonly IExceptionService<ProcurementOrderVM> _exceptionService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        private IProcurementUnitGroupService _unitGroupService;
        private readonly string _refType = "PO";

        public ProcurementOrderService(AppManEntities db,
            ICodextnService codextnService,
            ILocationBudgetService locationBudgetService,
            IExceptionService<ProcurementOrderVM> exceptionService
            )
        {
            _db = db;
            _codextnService = codextnService;
            _locationBudgetService = locationBudgetService;
            _exceptionService = exceptionService;
            _procurementCommonService = new ProcurementCommonService(_db);
            _getDisplayName = Utility.GetDisplayName<ProcurementOrderVM>;            
        }

        public IProcurementUnitGroupService UnitGroupService { get { return _unitGroupService = _unitGroupService ?? new ProcurementUnitGroupService(_db); } }

        private Expression<Func<ProcurementOrder, ProcurementOrderVM>> GetProjection()
        {
            return s => new ProcurementOrderVM
            {
                Id = s.Id,
                RefType = s.RefType,
                RefValue = s.RefValue,
                RefDate = s.RefDate,
                Fund = s.Fund,
                DepartmentId = s.DepartmentId,
                Department = s.Department,
                Division = s.Division,
                FPP = s.FPP,
                PostedBy = s.PostedBy,
                PostedDt = s.PostedDt,
                InsertedBy = s.InsertedBy,
                InsertedDt = s.InsertedDt,
                UpdatedBy = s.UpdatedBy,
                UpdatedDt = s.UpdatedDt,
                // Orders
                SupplierId = s.SupplierId,
                SupName = s.SupName,
                SupBusiness = s.SupBusiness,
                SupAddress = s.SupAddress,
                SupTIN = s.SupTIN,
                SupEmail = s.SupEmail,
                SupZipCode = s.SupZipCode,
                SupContactNo = s.SupContactNo,
                DeliveryPlace = s.DeliveryPlace,
                DeliveryDate = s.DeliveryDate,
                TermDelivery = s.TermDelivery,
                TermPayment = s.TermPayment,
                SignedBySuppName = s.SignedBySuppName,
                SignedBySuppDate = s.SignedBySuppDate,
                SignedByAuthName = s.SignedByAuthName,
                SignedByAuthDesignation = s.SignedByAuthDesignation,
                ResoNo = s.ResoNo,
                CertifiedCorrectBy = s.CertifiedCorrectBy,
                CertifiedCorredtDate = s.CertifiedCorredtDate
            };
        }

        public IQueryable<ProcurementOrderVM> GetAll()
        {
            var data = _db.Procurements.OfType<ProcurementOrder>().AsNoTracking()
                .Select(GetProjection());
            return data;
        }

        public Task<ProcurementOrderVM> GetByIdAsync(Guid? id)
        {
            return _db.Procurements.OfType<ProcurementOrder>().AsNoTracking()
                .Where(w => w.Id == id)
                .Select(GetProjection()).FirstOrDefaultAsync();
        }

        public ValueTask<ProcurementOrderVM> CreateAsync(ProcurementOrderVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new ProcurementOrder();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.Procurements.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<ProcurementOrderVM> UpdateAsync(ProcurementOrderVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.Procurements.OfType<ProcurementOrder>().FirstOrDefaultAsync(f => f.Id == model.Id);
            MapModelToEntityFields(entity, model, Mode.EDIT);

            await _db.SaveChangesAsync();

            return model;
        });

        public void MapModelToEntityFields(ProcurementOrder entity, ProcurementOrderVM model, Mode mode)
        {
            _procurementCommonService.MapModelToEntityFields(entity, model, mode);

            // extn
            entity.SupplierId = model.SupplierId;
            entity.SupName = model.SupName;
            entity.SupBusiness = model.SupBusiness;
            entity.SupAddress = model.SupAddress;
            entity.SupTIN = model.SupTIN;
            entity.SupEmail = model.SupEmail;
            entity.SupZipCode = model.SupZipCode;
            entity.SupContactNo = model.SupContactNo;
            entity.DeliveryPlace = model.DeliveryPlace;
            entity.DeliveryDate = model.DeliveryDate;
            entity.TermDelivery = model.TermDelivery;
            entity.TermPayment = model.TermPayment;
            entity.SignedBySuppName = model.SignedBySuppName;
            entity.SignedBySuppDate = model.SignedBySuppDate;
            entity.SignedByAuthName = model.SignedByAuthName;
            entity.SignedByAuthDesignation = model.SignedByAuthDesignation;
            entity.ResoNo = model.ResoNo;
            entity.CertifiedCorrectBy = model.CertifiedCorrectBy;
            entity.CertifiedCorredtDate = model.CertifiedCorredtDate;
        }

        public ValueTask<ProcurementOrderVM> DeleteAsync(ProcurementOrderVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.Procurements.OfType<ProcurementOrder>().FirstOrDefaultAsync(f => f.Id == model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();

            _db.Procurements.Remove(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public virtual ValueTask<ProcurementOrderVM> PostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.Procurements.OfType<ProcurementOrder>().FirstOrDefaultAsync(f => f.Id == id);

            ValidateRecord(entity, id);
            ValidateIfPosted(entity);

            entity.PostedBy = user;
            entity.PostedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            await _db.SaveChangesAsync();
            return await GetByIdAsync(id);
        });

        public virtual ValueTask<ProcurementOrderVM> UnPostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.Procurements.OfType<ProcurementOrder>().FirstOrDefaultAsync(f => f.Id == id);
            ValidateRecord(entity, id);
            ValidateIfNotPosted(entity);

            entity.PostedBy = "";
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            await _db.SaveChangesAsync();
            return await GetByIdAsync(id);
        });

        private void ValidateIfNull(ProcurementOrderVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(ProcurementOrder entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }

        private void ValidateIfPosted(ProcurementOrder entity)
        {
            if (entity.PostedDt != null)
            {
                var msg = $"Record already posted by {entity.PostedBy} on {entity.PostedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        private void ValidateIfNotPosted(ProcurementOrder entity)
        {
            if (entity.PostedDt == null)
            {
                throw new RecordNotYetPostedException($"Record is not yet posted!");
            }
        }

        private void ValidateFieldsOnCreateUpdate(ProcurementOrderVM model, Mode mode)
        {
            _imex = new InvalidModelException();

            if (string.IsNullOrWhiteSpace(model.RefValue))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.RefValue)), "Field is required.");
            }
            else
            {
                if (model.RefValue.Trim().Length != 12)
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.RefValue)), "Invalid value.");
                }
                else
                {
                    var refNoParts = model.RefValue.Split('-');
                    var refNoYear = int.Parse(refNoParts[0]);
                    var refNoMonth = int.Parse(refNoParts[1]);
                    if (refNoYear != model.RefDate.Value.Year || refNoMonth != model.RefDate.Value.Month)
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.RefValue)), "Series Year and month must be same as the year and month of the PO date.");
                    }
                    else
                    {
                        var maxNo = _db.Procurements.Where(w => DbFunctions.TruncateTime(w.RefDate) < DbFunctions.TruncateTime(model.RefDate)).Max(m => m.RefValue);
                        if (!string.IsNullOrWhiteSpace(maxNo))
                        {
                            var refNoSeq = int.Parse(refNoParts[2]);
                            var maxSeq = int.Parse(maxNo.Split('-')[2]);
                            if (refNoSeq <= maxSeq)
                            {
                                _imex.UpsertDataList(_getDisplayName(nameof(model.RefValue)), $"Serial No. must be greater than {maxSeq}");
                            }
                        }
                    }
                }
            }

            if (model.RefDate == null)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.RefDate)), "Field is required.");
            }

            if (!string.IsNullOrWhiteSpace(model.PrNo))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.PrNo)), "Field is required.");
            }            

            if (model.PrDate == null)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.PrDate)), "Field is required.");
            }

            if (model.DepartmentId.HasValue)
            {
                if (!_codextnService.IsValidMastCodeId("LOCATIONS", model.DepartmentId))
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.DepartmentId)), "Invalid value");
                }
            }
            else
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.DepartmentId)), "Field is required.");
            }

            if (string.IsNullOrWhiteSpace(model.Fund))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Fund)), "Field is required.");
            }
            else
            {
                if (!_codextnService.IsValidMastCodeCode("FUND", model.Fund))
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.Fund)), "Invalid value");
                }
            }
            
            if (string.IsNullOrWhiteSpace(model.FPP))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.FPP)), "Field is required.");
            }
            else
            {
                if (!_locationBudgetService.IsValidBudgetCode(model.DepartmentId, model.FPP))
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.FPP)), "Invalid value");
                }
            }

            if (model.SupplierId == null)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.SupplierId)), "Field is required.");
            }
            else
            {
                var supplier = _db.Database.SqlQuery<SupplierVM>("Exec Supplier_GetAll '', {0}", model.SupplierId).ToList().FirstOrDefault();
                if (supplier == null)
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.SupplierId)), "Invalid Value");
                }
            }

            if (model.RefDate != null && model.PrDate != null && model.RefDate < model.PrDate)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.RefDate)), "Must be greater or equal to PR date.");
            }
            

            _imex.ThrowIfContainsErrors();
        }
    }
}