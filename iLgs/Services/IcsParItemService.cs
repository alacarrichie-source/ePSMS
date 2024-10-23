using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using iLgs.Services.Validators;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services
{
    public interface IIcsParItemService
    {
        IQueryable<IcsParItem> GetAllByIcsParId(Guid? icsParId);
        ValueTask<IcsParItem> GetByIdAsync(Guid? id);
        IQueryable<IcsParItem> GetAllParItems(Guid? psCardItemGroupId);
        IQueryable<IcsParItem> GetAllIcsItems(Guid? psCardItemGroupId);
        ValueTask<ServiceResult<IcsParItem>> CreateAsync(IcsParItem model, string user, DateTime date);
        ValueTask<ServiceResult<IcsParItem>> UpdateAsync(IcsParItem model, string user, DateTime date);
        ValueTask<ServiceResult<IcsParItem>> DeleteAsync(IcsParItem model, string user, DateTime date);

        ValueTask<bool> IsPostedAsync(Guid? id);
        ValueTask<bool> IsExistingAsync(Guid? id);
    }

    public class IcsParItemService : IIcsParItemService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<ServiceResult<IcsParItem>> _exceptionService = new ExceptionService<ServiceResult<IcsParItem>>();
        private readonly IValidationService<IcsParItem> _validationService;

        public IcsParItemService(AppManEntities db)
        {
            _db = db;
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
                .Include(i => i.PsCardItemExtn)
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


        public ValueTask<ServiceResult<IcsParItem>> CreateAsync(IcsParItem model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {

            //await ValidateIfPosted(model);
            var result = await _validationService.ValidateAsync(model, "Update");
            if (!result.IsSuccess)
            {
                return ServiceResult<IcsParItem>.Failure(result.Errors);
            }

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
            return ServiceResult<IcsParItem>.Success(model);
        });

        public ValueTask<ServiceResult<IcsParItem>> UpdateAsync(IcsParItem model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var result = await _validationService.ValidateAsync(model, "Update");
            if (!result.IsSuccess)
            {
                return ServiceResult<IcsParItem>.Failure(result.Errors);
            }

            var entity = await GetByIdAsync(model.Id);

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

            return ServiceResult<IcsParItem>.Success(model);
        });

        public ValueTask<ServiceResult<IcsParItem>> DeleteAsync(IcsParItem model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var result = await _validationService.ValidateAsync(model, "Delete");
            if (!result.IsSuccess)
            {
                return ServiceResult<IcsParItem>.Failure(result.Errors);
            }

            IcsParItem entity = await _db.IcsParItems.FindAsync(model.Id);
            //if (entity == null)
            //{
            //    throw new RecordNotFoundException(model.Id);
            //}

            //await ValidateIfPosted(model);

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

            
            return ServiceResult<IcsParItem>.Success(model);            
        });

        public async ValueTask<bool> IsPostedAsync(Guid? id)
        {
            return await _db.IcsParItems.Where(w => w.Id == id
                && (w.PsCardItemExtn.PsCardItem.ParPostedBy != null && w.PsCardItemExtn.PsCardItem.ParPostedBy != "")).AnyAsync();
        }

        public async ValueTask<bool> IsExistingAsync(Guid? id)
        {
            return await _db.IcsParItems.AnyAsync(a => a.Id == id);
        }

        private async Task ValidateIfPosted(IcsParItem model)
        {
            if (await IsPostedAsync(model.Id))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            }
        }
    }
}