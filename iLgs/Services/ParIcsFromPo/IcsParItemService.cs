using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.ParIcsFromPo
{
    public interface IIcsParItemService
    {
        IQueryable<IcsParItem> GetAllByIcsParId(Guid? icsParId);
        IQueryable<IcsParItemVM> GetIssuance(Guid? icsParId);
        ValueTask<IcsParItem> GetByIdAsync(Guid? id);
        IQueryable<IcsParItem> GetAllParItems(Guid? psCardItemGroupId);
        IQueryable<IcsParItem> GetAllIcsItems(Guid? psCardItemGroupId);
        ValueTask<IcsParItem> CreateAsync(IcsParItem model, string user, DateTime date);
        ValueTask<IcsParItem> UpdateAsync(IcsParItem model, string user, DateTime date);
        ValueTask<IcsParItem> DeleteAsync(IcsParItem model, string user, DateTime date);

        ValueTask<IcsParItemVM> UpdateIssuanceAsync(IcsParItemVM model, string user, DateTime date);

        bool IsPosted(Guid? id);
        bool IsExisting(Guid? id);
    }

    public class IcsParItemService : BaseValidator, IIcsParItemService
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly GetDisplayNameDelegate _getDisplayNameVM;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<IcsParItem> _exceptionService;
        private readonly IExceptionService<IcsParItemVM> _vmExceptionService;

        public IcsParItemService(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<IcsParItem>(propertyName);
            _getDisplayNameVM = propertyName => Utility.GetDisplayName<IcsParItemVM>(propertyName);
            _exceptions = new CreateAndLogExceptions();
            _exceptionService = new ExceptionService<IcsParItem>();
            _vmExceptionService = new ExceptionService<IcsParItemVM>();
        }

        //public IcsParItemService(AppManEntities db,
        //    IAppManEntitiesFactory appManEntitiesFactory,
        //    ICreateAndLogExceptions exceptions,
        //    IExceptionService<IcsParItem> exceptionService,
        //    IExceptionService<IcsParItemVM> vmExceptionService)
        //{
        //    _db = db;
        //    _contextFactory = appManEntitiesFactory;
        //    _getDisplayName = propertyName => Utility.GetDisplayName<IcsParItem>(propertyName);
        //    _getDisplayNameVM = propertyName => Utility.GetDisplayName<IcsParItemVM>(propertyName);
        //    _exceptions = exceptions;
        //    _exceptionService = exceptionService;
        //    _vmExceptionService = vmExceptionService;
        //}

        public IQueryable<IcsParItem> GetAllByIcsParId(Guid? icsParId)
        {
            var data = _db.IcsParItems.Where(w => w.IcsParId == icsParId).AsNoTracking();
            return data;
        }

        public IQueryable<IcsParItemVM> GetIssuance(Guid? icsParId)
        {
            var data = _db.IcsParItems.Include(i => i.PsCardItemExtn.PsCardItem)
                .Where(w => w.IcsParId == icsParId)
                .Select(s => new IcsParItemVM
                {
                    Id = s.Id,
                    PsCardItemExtnId = s.PsCardItemExtnId,
                    ContentNo = s.PsCardItemExtn.ContentNo,
                    TContentNo = (int?)s.PsCardItemExtn.PsCardItem.Qty,
                    PropNo = s.PsCardItemExtn.PropNo,
                    IssuedTo = s.IssuedTo,
                    Designation = s.Designation,
                    RefNo = _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().Any(a => a.Id == s.PsCardItemExtn.Id)
                        ? _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().FirstOrDefault(f => f.Id == s.PsCardItemExtn.Id).SerialNo :
                            (_db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>().Any(a => a.Id == s.PsCardItemExtn.Id)
                            ? _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>().FirstOrDefault(f => f.Id == s.PsCardItemExtn.Id).ConductionNo : "")
                })
                .AsNoTracking();
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
            ValidateIfPosted(model.Id);
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
            ValidateIfPosted(model.Id);
            ValidateFields(model, Mode.EDIT);

            var entity = await _db.IcsParItems.Include(i => i.IcsPar)
                .Include(i => i.PsCardItemExtn.PsCardItem) // Ensure related entities are included
                .Include(i => i.PsCardItemExtn.Codextn)    // Ensure Codextn is included for Location description
                .Include(i => i.PsCardItemExtn)
            .Where(w => w.Id == model.Id)
            .FirstOrDefaultAsync();

            ValidateRecord(model.Id);

            var propSplit = model.PsCardItemExtn.PropNo.Split('/');
            var propYear = model.PsCardItemExtn.PropNo.Substring(0, 4);
            var propSeq = propSplit[propSplit.Length - 2];

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.IcsPar.ReceivedByTitle = model.IcsPar.ReceivedByTitle?.Trim();
            entity.IcsPar.ReceivedByTitle2 = model.IcsPar.ReceivedByTitle2?.Trim();
            entity.IcsPar.ReceivedById = model.IcsPar.ReceivedById;
            entity.IcsPar.ReceivedBy = model.IcsPar.ReceivedBy?.Trim();
            entity.IcsPar.ReceivedDept = model.IcsPar.ReceivedDept?.Trim();
            entity.IcsPar.ReceivedDate = model.IcsPar.ReceivedDate;

            entity.IcsPar.IssuedBy = model.IcsPar.IssuedBy?.Trim();
            entity.IcsPar.IssuedByPosition = model.IcsPar.IssuedByPosition?.Trim();
            entity.IcsPar.IssuedDept = model.IcsPar.IssuedDept?.Trim();
            entity.IcsPar.IssuedDate = model.IcsPar.IssuedDate;

            entity.PsCardItemExtn.PropNo = model.PsCardItemExtn.PropNo?.Trim();
            entity.PsCardItemExtn.PropYear = propYear;
            entity.PsCardItemExtn.PropSeq = propSeq;
            entity.PsCardItemExtn.UpdatedBy = user;
            entity.PsCardItemExtn.UpdatedDt = date;

            entity.IssuedTo = model.IssuedTo?.Trim();
            entity.Designation = model.Designation?.Trim();
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();

            return model;
        });


        public ValueTask<IcsParItemVM> UpdateIssuanceAsync(IcsParItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {

            ValidateIfNull(model);
            ValidateIfPosted(model.Id);
            ValidateFields(model, Mode.EDIT);

            var entity = await _db.IcsParItems.Include(i => i.IcsPar)
                   .Include(i => i.PsCardItemExtn.PsCardItem) // Ensure related entities are included
                   .Include(i => i.PsCardItemExtn.Codextn)    // Ensure Codextn is included for Location description
                   .Include(i => i.PsCardItemExtn)
               .Where(w => w.Id == model.Id)
               .FirstOrDefaultAsync();

            ValidateRecord(model.Id);

            var propSplit = model.PropNo.Split('/');
            var propYear = model.PropNo.Substring(0, 4);
            var propSeq = propSplit[propSplit.Length - 2];


            entity.PsCardItemExtn.PropNo = model.PropNo;
            entity.PsCardItemExtn.PropYear = propYear;
            entity.PsCardItemExtn.PropSeq = propSeq;
            entity.PsCardItemExtn.UpdatedBy = user;
            entity.PsCardItemExtn.UpdatedDt = date;

            entity.IssuedTo = model.IssuedTo?.Trim();
            entity.Designation = model.Designation?.Trim();
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<IcsParItem> DeleteAsync(IcsParItem model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateIfPosted(model.Id);

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

            await _db.SaveChangesAsync();

            _db.IcsParItems.Remove(entity);
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

                await _db.SaveChangesAsync();
            }

            // remove master record if no child record exists
            if (!(await _db.IcsParItems.AnyAsync(a => a.IcsParId == model.IcsParId)))
            {
                var icsPar = await _db.IcsPars.FindAsync(model.IcsParId);
                if (icsPar != null)
                {
                    icsPar.UpdatedBy = user;
                    icsPar.UpdatedDt = date;

                    await _db.SaveChangesAsync();

                    _db.IcsPars.Remove(icsPar);
                    await _db.SaveChangesAsync();
                }
            }

            return model;
        });

        public bool IsPosted(Guid? id)
        {
            //return _db.IcsParItems.Where(w => w.Id == id
            //    && (w.PsCardItemExtn.PsCardItem.ParPostedBy != null && w.PsCardItemExtn.PsCardItem.ParPostedBy != "")).Any();

            return _db.IcsParItems.Where(w => w.Id == id && w.IcsPar.PostedDt != null).Any();
        }

        public bool IsExisting(Guid? id)
        {
            return _db.IcsParItems.Any(a => a.Id == id);
        }

        private void ValidateIfPosted(Guid id)
        {
            if (IsPosted(id))
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
                || (mode == Mode.EDIT && icsPars.Any(a => a.Id != model.IcsParId)))
            {
                if (model.IcsPar.RefType == "P")
                {
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


        public void ValidateFields(IcsParItemVM model, Mode mode)
        {

            var psCardItemExtns = _db.PsCardItemExtns.Where(f => f.PropNo == model.PropNo);
            if (psCardItemExtns.Any())
            {
                if (mode == Mode.ADD)
                {
                    _imex.UpsertDataList("Prop. No.", "Already Exists.");
                }
                else
                {
                    if (psCardItemExtns.Any(a => a.Id != model.PsCardItemExtnId))
                    {
                        _imex.UpsertDataList("Prop. No.", "Already Exists.");
                    }
                }
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

        private static void ValidateIfNull(IcsParItemVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }
    }
}