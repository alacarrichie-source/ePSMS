using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Linq;

namespace iLgs.Services.CustodianReports
{
    public interface ICustodianReportValidator
    {
        void ValidateOnCreate(CustodianReport model);
        void ValidateOnUpdate(CustodianReport model);
        void ValidateOnDelete(CustodianReport model);
        void ValidateOnPost(Guid id);
        void ValidateOnUnpost(Guid id);        
    }

    public class CustodianReportValidator : BaseValidator, ICustodianReportValidator
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly ICodextnService _codextnService;        

        public CustodianReportValidator(AppManEntities db, ICodextnService codextnService)
        {
            _db = db;
            _getDisplayName = Utility.GetDisplayName<CustodianReport>;
            _codextnService = codextnService;
        }

        public void ValidateOnCreate(CustodianReport model)
        {
            ValidateIfNull(model);
            ValidateDuplicateOnCreate(model);
            ValidateFieldsOnCreateUpdate(model);
        }

        public void ValidateOnUpdate(CustodianReport model)
        {
            ValidateIfNull(model);
            ValidateIfPosted(model.Id);
            ValidateDuplicateOnUpdate(model);
            ValidateFieldsOnCreateUpdate(model);
        }

        public void ValidateOnDelete(CustodianReport model)
        {
            ValidateIfNull(model);            
            ValidateRecord(model.Id);
            ValidateIfPosted(model.Id);
        }

        public void ValidateDuplicateOnCreate(CustodianReport model)
        {
            if (_db.CustodianReports.Any(w => w.AsOf == model.AsOf 
                && w.Fund == model.Fund 
                && w.DeptId == model.DeptId 
                && w.AccountGroup == model.AccountGroup))
            {
                throw new RecordAlreadyExistsException();
            }
        }

        public void ValidateIfPosted(Guid id)
        {
            var data = _db.CustodianReports.AsNoTracking().Where(w => w.Id == id && w.PostedDt != null).First();
            if (data != null)
            {
                var msg = $"Record already posted by {data.PostedBy} on {data.PostedDt}";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        public void ValidateDuplicateOnUpdate(CustodianReport model)
        {
            if (_db.CustodianReports.Any(w => w.Id != model.Id 
                && w.AsOf == model.AsOf 
                && w.Fund == model.Fund 
                && w.DeptId == model.DeptId
                && w.AccountGroup == model.AccountGroup))
            {
                throw new RecordAlreadyExistsException();
            }
        }        

        public void ValidateFieldsOnCreateUpdate(CustodianReport model)
        {
            _imex = new InvalidModelException();

            if (!model.AsOf.HasValue)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.AsOf)), "Field is required.");
            }

            if (string.IsNullOrWhiteSpace(model.Fund))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Fund)), "Field is required.");
            }

            if (!model.DeptId.HasValue)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.DeptId)), "Field is required.");
            }
            else
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
            _imex = new InvalidModelException();

            var entity = _db.CustodianReports.Find(id);
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
            var entity = _db.CustodianReports.Find(id);
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
            if (!_db.CustodianReports.Any(a => a.Id == id))
            {
                throw new NotFoundException(id);
            }
        }

        private static void ValidateIfNull(CustodianReport model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }
    }
}