using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Interfaces;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services.ParIcs
{
    public interface IIcsParItemService
    {
        IQueryable<IcsParItem> GetAllByIcsParId(Guid? icsParId);
        ValueTask<IcsParItem> GetByIdAsync(Guid? id);
        IQueryable<IcsParItem> GetAllParItems(Guid? psCardItemGroupId);
        IQueryable<IcsParItem> GetAllIcsItems(Guid? psCardItemGroupId);
        ValueTask<IcsParItem> CreateAsync(IcsParItem model, string user, DateTime date);
        ValueTask<IcsParItem> UpdateAsync(IcsParItem model, string user, DateTime date);
        ValueTask<IcsParItem> DeleteAsync(IcsParItem model, string user, DateTime date);

        bool IsPosted(Guid? id);
        bool IsExisting(Guid? id);
    }

    public class IcsParItemService : BaseValidator, IIcsParItemService
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly ICreateAndLogExceptions _exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<IcsParItem> _exceptionService = new ExceptionService<IcsParItem>();
        private readonly IValidationService<IcsParItem> _validationService;

        public IcsParItemService(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<IcsParItem>(propertyName);
            _validationService = new ValidationService<IcsParItem>(new IcsParItemValidator(_db));
        }

        public IQueryable<IcsParItem> GetAllByIcsParId(Guid? icsParId) 
        {
            var data = _db.IcsParItems.Where(w => w.IcsParId == icsParId).AsNoTracking();
            return data;
        }

        public async ValueTask<IcsParItem> GetByIdAsync(Guid? id)
        {
            var data = await _db.IcsParItems.Include(i => i.IcsPar)
                    .Include(i => i.PsCardItemExtn.PsCardItem) // Ensure related entities are included
                    .Include(i => i.PsCardItemExtn.Codextn)    // Ensure Codextn is included for Location description
                    .Include(i => i.PsCardItemExtn)
                //.Include(i => i.PsCardItemExtn.PsCardItemExtnBuilding)
                //.Include(i => i.PsCardItemExtn.PsCardItemExtnLand)
                //.Include(i => i.PsCardItemExtn.PsCardItemExtnOther)
                //.Include(i => i.PsCardItemExtn.PsCardItemExtnVehicle)                    
                .Where(w => w.Id == id)
                .FirstOrDefaultAsync();
            return data;
        }

        public IQueryable<IcsParItem> GetAllParItems(Guid? psCardItemGroupId) 
        {
            var data = GetAllIcsParItems(psCardItemGroupId, "P");
            return data;
        }

        public IQueryable<IcsParItem> GetAllIcsItems(Guid? psCardItemGroupId) 
        {
            var data = GetAllIcsParItems(psCardItemGroupId, "I");
            return data;
        }

        private IQueryable<IcsParItem> GetAllIcsParItems(Guid? psCardItemGroupId, string refType)
        {
            var data = _db.IcsParItems
                .Include(i => i.IcsPar)
                .Include(i => i.PsCardItemExtn.PsCardItem)
                .Where(w => w.IcsPar.RefType == refType && (w.PsCardItemExtn.PsCardItem.GroupId == psCardItemGroupId)
                // Get items from same PO of different CardItem (Due to Transfer of Item)
                //|| _db.PsCardItems.Any(a => a.PoNo == w.PsCardItemExtn.PsCardItem.PoNo
                //    && a.PoDate == w.PsCardItemExtn.PsCardItem.PoDate
                //    && a.DeptId == w.PsCardItemExtn.PsCardItem.DeptId
                //    && a.PsCardId == w.PsCardItemExtn.PsCardItem.PsCardId
                //    && a.Id != psCardItemId))
                );

            return data;
        }


        public ValueTask<IcsParItem> CreateAsync(IcsParItem model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {

            //await ValidateIfPosted(model);
            //var result = await _validationService.ValidateAsync(model, "Update");
            //if (!result.IsSuccess)
            //{
            //    return ServiceResult<IcsParItem>.Failure(result.Errors);
            //}
            ValidateIfNull(model);
            ValidateIfPosted(model);
            ValidateFields(model, Mode.ADD);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            IcsParItem entity = new IcsParItem()
            {
                Id = model.Id,
                IcsParId = model.IcsParId,
                PsCardItemExtnId = model.PsCardItemExtnId,
                Qty = model.Qty,
                Amount = model.Amount,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.IcsParItems.Add(entity);
            await _db.SaveChangesAsync();
            return model;
        });

        public ValueTask<IcsParItem> UpdateAsync(IcsParItem model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {

            ValidateIfNull(model);
            ValidateIfPosted(model);
            ValidateFields(model, Mode.EDIT);

            var entity = await GetByIdAsync(model.Id);
            ValidateRecord(model.Id);
            
            var propSplit = model.PsCardItemExtn.PropNo.Split('/');
            var propYear = model.PsCardItemExtn.PropNo.Substring(0, 4);
            var propSeq = propSplit[propSplit.Length - 2];

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.IcsPar.ReceivedBy = model.IcsPar.ReceivedBy;
            entity.IcsPar.ReceivedDate = model.IcsPar.ReceivedDate;
            entity.IcsPar.ReceivedDept = model.IcsPar.ReceivedDept;
            entity.IcsPar.ReceivedByPosition = model.IcsPar.ReceivedByPosition;
            entity.IcsPar.IssuedBy = model.IcsPar.IssuedBy;
            entity.IcsPar.IssuedDate = model.IcsPar.IssuedDate;
            entity.IcsPar.IssuedDept = model.IcsPar.IssuedDept;
            entity.IcsPar.IssuedByPosition = model.IcsPar.IssuedByPosition;

            entity.IcsPar.UpdatedBy = user;
            entity.IcsPar.UpdatedDt = date;

            entity.PsCardItemExtn.PropNo = model.PsCardItemExtn.PropNo;
            entity.PsCardItemExtn.PropYear = propYear;
            entity.PsCardItemExtn.PropSeq = propSeq;
            entity.PsCardItemExtn.UpdatedBy = user;
            entity.PsCardItemExtn.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.IcsParItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<IcsParItem> DeleteAsync(IcsParItem model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateIfPosted(model);
            IcsParItem entity = await _db.IcsParItems.Include(i => i.IcsPar).FirstOrDefaultAsync(f => f.Id == model.Id);            

            if (_db.IcsParUpdates.Any(a => a.PrevRefNo == entity.IcsPar.RefNo && a.RefType == entity.IcsPar.RefType))
            {
                throw new RecordRelationshipException("This record was already updated or transfered to other ICS/PAR, cannot continue.");
            }

            if (_db.PsCardItemTransferItems.Any(a => a.IcsParItemId == entity.Id))
            {
                throw new RecordRelationshipException("Transit/Issuance was already made for this record, cannot continue.");
            }

            var psCardItemExtnId = entity.PsCardItemExtnId;

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.IcsParItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.IcsParItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            PsCardItemExtn psCardItemExtn = await _db.PsCardItemExtns.FindAsync(psCardItemExtnId);
            if (psCardItemExtn != null)
            {
                psCardItemExtn.UpdatedBy = user;
                psCardItemExtn.UpdatedDt = date;

                psCardItemExtn.LocationId = null;
                psCardItemExtn.PropNo = null;
                psCardItemExtn.PropSeq = null;
                psCardItemExtn.PropYear = null;
                psCardItemExtn.SeriesNo = null;

                _db.PsCardItemExtns.Attach(psCardItemExtn);
                _db.Entry(psCardItemExtn).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                /*
                 * TO DO:
                 * Delete PsCardItemExnLocations
                 */

                //_db.PsCardItemExtns.Remove(psCardItemExtn);
                //_db.Entry(psCardItemExtn).State = EntityState.Deleted;
                //await _db.SaveChangesAsync();
            }

            // remove master record if no child record exists
            if (!(await _db.IcsParItems.AnyAsync(a => a.IcsParId == model.IcsParId)))
            {
                var icsPar = await _db.IcsPars.FindAsync(model.IcsParId);
                if (icsPar != null)
                {
                    icsPar.UpdatedBy = user;
                    icsPar.UpdatedDt = date;

                    _db.IcsPars.Attach(icsPar);
                    _db.Entry(icsPar).State = EntityState.Modified;
                    await _db.SaveChangesAsync();

                    _db.IcsPars.Remove(icsPar);
                    _db.Entry(icsPar).State = EntityState.Deleted;
                    await _db.SaveChangesAsync();
                }
            }
            
            return model;            
        });

        public bool IsPosted(Guid? id)
        {
            return _db.IcsParItems.Where(w => w.Id == id
                && (w.PsCardItemExtn.PsCardItem.ParPostedBy != null && w.PsCardItemExtn.PsCardItem.ParPostedBy != "")).Any();
        }

        public bool IsExisting(Guid? id)
        {
            return _db.IcsParItems.Any(a => a.Id == id);
        }

        private void ValidateIfPosted(IcsParItem model)
        {
            if (IsPosted(model.Id))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            }
        }

        public void ValidateFields(IcsParItem model, Mode mode)
        {
            if (!model.IcsPar.RefDate.HasValue)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.IcsPar.RefDate)), "Field is required.");
            }

            var icsPars = _db.IcsPars.Where(a => a.RefNo == model.IcsPar.RefNo && a.RefType == model.IcsPar.RefType).AsNoTracking();
            if ((mode == Mode.ADD && icsPars.Any()) 
                || (mode == Mode.EDIT && icsPars.Any(a => a.Id != model.IcsParId))) {                
                if (model.IcsPar.RefType == "P") {
                    _imex.UpsertDataList("PAR No.", "Already Exists.");
                }
                else
                {
                    _imex.UpsertDataList("ICS No.", "Already Exists.");
                }                
            }

            var icsPar = _db.IcsPars.Where(w => w.IcsParItems.Any(a => a.PsCardItemExtn.PropNo == model.PsCardItemExtn.PropNo)).AsNoTracking().FirstOrDefault();
            if ((mode == Mode.ADD && icsPar != null) 
                || (mode == Mode.EDIT && icsPar != null && icsPar.Id != model.IcsParId))
            {
                if (model.IcsPar.RefType == "P")
                {
                    _imex.UpsertDataList("Property No.", $"Already exists under PAR No. {icsPar.RefNo}");
                }
                else
                {
                    _imex.UpsertDataList("Stock No.", $"Already exists under ICS No. {icsPar.RefNo}");
                }
            }

            if (string.IsNullOrWhiteSpace(model.IcsPar.ReceivedBy))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.IcsPar.ReceivedBy)), "Field is required.");
            }

            if (string.IsNullOrWhiteSpace(model.IcsPar.ReceivedByPosition))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.IcsPar.ReceivedByPosition)), "Field is required.");
            }

            if (string.IsNullOrWhiteSpace(model.IcsPar.ReceivedDept))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.IcsPar.ReceivedDept)), "Field is required.");
            }

            if (!model.IcsPar.ReceivedDate.HasValue)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.IcsPar.ReceivedDate)), "Field is required.");
            }

            if (string.IsNullOrWhiteSpace(model.IcsPar.IssuedBy))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.IcsPar.IssuedBy)), "Field is required.");
            }

            if (string.IsNullOrWhiteSpace(model.IcsPar.IssuedByPosition))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.IcsPar.IssuedByPosition)), "Field is required.");
            }

            if (string.IsNullOrWhiteSpace(model.IcsPar.IssuedDept))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.IcsPar.IssuedDept)), "Field is required.");
            }

            if (!model.IcsPar.IssuedDate.HasValue)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.IcsPar.IssuedDate)), "Field is required.");
            }
            
            _imex.ThrowIfContainsErrors();
        }

        private void ValidateRecord(Guid id)
        {
            if (!_db.IcsParItems.Any(a => a.Id == id))
            {
                throw new NotFoundException(id);
            }
        }

        private static void ValidateIfNull(IcsParItem model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }        
    }
}