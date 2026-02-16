using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services.Validators
{
    public interface IRisValidator
    {
        void ValidateOnCreate(RIS_VM model);
        void ValidateOnUpdate(RIS_VM model);
        void ValidateOnDelete(RIS_VM model);
        void ValidateOnPost(Guid risId);
        void ValidateOnUnpost(Guid risId);
    }

    public class RisValidator : BaseValidator, IRisValidator
    {
        //private delegate string GetDisplayNameDelegate(string propertyName);
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly ICodextnService _codextnService;
        private readonly ILocationBudgetService _locationBudgetService;

        public RisValidator(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<RIS_VM>(propertyName);
            _codextnService = new CodextnService(_db);
            _locationBudgetService = new LocationBudgetService(_db);
        }

        //public RisValidator(AppManEntities db,
        //    ICodextnService codextnService,
        //    ILocationBudgetService locationBudgetService)
        //{
        //    _db = db;
        //    _getDisplayName = propertyName => Utility.GetDisplayName<RIS_VM>(propertyName);
        //    _codextnService = codextnService;
        //    _locationBudgetService = locationBudgetService;
        //}

        public void ValidateOnCreate(RIS_VM model)
        {
            ValidateModel(model);
            if (_db.RISses.Any(a => a.RisNo == model.RisNo))
            {
                throw new RecordAlreadyExistsException(string.Format("RIS Number {0} already exists", model.RisNo));
            }
            ValidateFieldsOnCreateUpdate(model, Mode.ADD);
        }

        public void ValidateOnUpdate(RIS_VM model)
        {
            ValidateModel(model);

            var rec = _db.RISses.Find(model.Id);
            if (rec == null)
            {
                throw new NotFoundException(model.Id);
            }

            if (!model.IssuanceSw)
            {
                if (!string.IsNullOrWhiteSpace(rec.PostedBy))
                {
                    throw new RecordAlreadyPostedException(string.Format("RIS No {0} already posted. Cannot update!", rec.RisNo));
                }

                //var pr = _db.Requests.Where(a => a.RisId == model.Id).FirstOrDefault();
                //if (pr != null)
                //{
                //    if (!string.IsNullOrWhiteSpace(pr.SubmittedBy))
                //    {
                //        throw new RecordRelationshipException("This RIS No has a posted PR, cannot update!");
                //    }
                //}                
            }

            if (_db.RISses.Any(a => a.RisNo == model.RisNo && a.Id != model.Id))
            {
                throw new RecordAlreadyExistsException(string.Format("RIS Number {0} already exists", model.RisNo));
            }
            
            ValidateFieldsOnCreateUpdate(model, Mode.EDIT);
        }

        public void ValidateOnDelete(RIS_VM model)
        {
            ValidateModel(model);
            ValidateRecord(model.Id);            
        }
        
        public void ValidateFieldsOnCreateUpdate(RIS_VM model, Mode mode)
        {
            var ex = new InvalidModelException();

            if (!string.IsNullOrWhiteSpace(model.RisNo))
            {
                if (model.RisNo.Trim().Length != 12)
                {
                    ex.UpsertDataList(_getDisplayName(nameof(model.RisNo)), "Invalid value.");
                }
                else
                {
                    var refNoParts = model.RisNo.Split('-');
                    var refNoYear = int.Parse(refNoParts[0]);
                    var refNoMonth = int.Parse(refNoParts[1]);
                    if (refNoYear != model.RisDate.Value.Year || refNoMonth != model.RisDate.Value.Month)
                    {
                        ex.UpsertDataList(_getDisplayName(nameof(model.RisNo)), "Series Year and month must be same as the year and month of the RIS date.");
                    }
                    else
                    {
                        var maxNo = _db.RISses.Where(w => DbFunctions.TruncateTime(w.RisDate) < DbFunctions.TruncateTime(model.RisDate)).Max(m => m.RisNo);                        
                        if (!string.IsNullOrWhiteSpace(maxNo))
                        {
                            var refNoSeq = int.Parse(refNoParts[2]);
                            var maxSeq = int.Parse(maxNo.Split('-')[2]);
                            if (refNoSeq <= maxSeq)
                            {
                                ex.UpsertDataList(_getDisplayName(nameof(model.RisNo)), $"Serial No. must be greater than {maxSeq}");
                            }
                        }
                    }
                }
            }

            if (mode == Mode.ADD) {
                if (model.RisDate.Value.Date > DateTime.Now.Date)
                {
                    ex.UpsertDataList(_getDisplayName(nameof(model.RisDate)), "Future Date is not allowed.");
                }

                //if (model.RisDate.Value.Date < DateTime.Now.Date)
                //{
                //    ex.UpsertDataList(_getDisplayName(nameof(model.OfficeId)), "Past Date is not allowed.");
                //}
            }

            if (!model.OrderId.HasValue)
            {
                ex.UpsertDataList(_getDisplayName(nameof(model.OrderId)), "Field is required.");
            }
            else
            {
                var order = _db.Orders.Find(model.OrderId);
                if (order == null)
                {
                    ex.UpsertDataList(_getDisplayName(nameof(model.OrderId)), "Record not found.");
                }
                else
                {
                    if (model.RisDate < order.PoDate)
                    {
                        ex.UpsertDataList(_getDisplayName(nameof(model.RisDate)), "Date must be on or after the PO Date.");
                    }
                }
            }

            if (model.OfficeId.HasValue)
            {
                if (!_codextnService.IsValidMastCodeId("LOCATIONS", model.OfficeId))
                {
                    ex.UpsertDataList(_getDisplayName(nameof(model.OfficeId)), "Invalid value");
                }
            }

            if (string.IsNullOrWhiteSpace(model.Fund))
            {
                ex.UpsertDataList(_getDisplayName(nameof(model.Fund)), "Field is required.");
            }
            else
            {
                if (!_codextnService.IsValidMastCodeCode("FUND", model.Fund))
                {
                    ex.UpsertDataList(_getDisplayName(nameof(model.Fund)), "Invalid value");
                }
            }

            if (string.IsNullOrWhiteSpace(model.Office))
            {
                ex.UpsertDataList(_getDisplayName(nameof(model.Office)), "Field is required.");
            }            

            if (string.IsNullOrWhiteSpace(model.FPP))
            {
                ex.UpsertDataList(_getDisplayName(nameof(model.FPP)), "Field is required.");
            }
            else
            {
                if (!_locationBudgetService.IsValidBudgetCode(model.OfficeId, model.FPP))
                {
                    ex.UpsertDataList(_getDisplayName(nameof(model.FPP)), "Invalid value");
                }
            }

            if (!model.RisDate.HasValue)
            {
                ex.UpsertDataList(_getDisplayName(nameof(model.RisDate)), "Date is required.");
            }

            if (string.IsNullOrWhiteSpace(model.Purpose))
            {
                ex.UpsertDataList(_getDisplayName(nameof(model.Purpose)), "Field is required.");
            }            

            if (model.RisDate.HasValue && model.RequestedDate.HasValue)
            {
                if (model.RequestedDate < model.RisDate)
                {
                    ex.UpsertDataList("Requested Date", "Must be on or after the RIS Date.");
                }
            }

            if (model.ApprovedDate.HasValue && model.RequestedDate.HasValue)
            {
                if (model.ApprovedDate < model.RequestedDate)
                {
                    ex.UpsertDataList("Approved Date", "Must be on or after the Requested Date.");
                }
            }

            if (string.IsNullOrWhiteSpace(model.IssuedBy))
            {
                ex.UpsertDataList(_getDisplayName(nameof(model.IssuedBy)), "Field is required.");
            }
            else
            {
                if (!_codextnService.GetByMastCode("ISSUED-BY").Where(w => w.Description == model.IssuedBy).Any())
                {
                    ex.UpsertDataList(_getDisplayName(nameof(model.IssuedBy)), "Invalid value");
                }                
            }

            ex.ThrowIfContainsErrors();
        }

        public void ValidateOnPost(Guid risId)
        {
            var rec = _db.RISses.Find(risId);
            if (rec == null)
            {
                throw new NotFoundException(risId);
            }

            if (!string.IsNullOrWhiteSpace(rec.PostedBy))
            {
                throw new RecordAlreadyPostedException(string.Format("RIS No {0} already posted. Please verify!", rec.RisNo));
            }

            if (!_db.RisItems.Any(a => a.RisId == risId))
            {
                throw new NotFoundException("No RIS Items found, cannot post.");
            }
        }
        public void ValidateOnUnpost(Guid risId)
        {
            var rec = _db.RISses.Find(risId);
            if (rec == null)
            {
                throw new NotFoundException(risId);
            }

            if (string.IsNullOrWhiteSpace(rec.PostedBy))
            {
                throw new RecordAlreadyPostedException(string.Format("RIS No {0} is not yet posted. Please verify!", rec.RisNo));
            }
            
            //var pr = _db.Requests.Where(a => a.RisId == risId).FirstOrDefault();
            //if (pr != null)
            //{
            //    if (!string.IsNullOrWhiteSpace(pr.SubmittedBy))
            //    {
            //        throw new RecordRelationshipException("This RIS No has a posted PR, cannot unpost!");
            //    }
            //}
        }

        private void ValidateRecord(Guid id)
        {
            if (!_db.RISses.Any(a => a.Id == id))
            {
                throw new NotFoundException(id);
            }
        }

        private static void ValidateModel(RIS_VM card)
        {
            if (card is null)
            {
                throw new NullException();
            }
        }
    }
}