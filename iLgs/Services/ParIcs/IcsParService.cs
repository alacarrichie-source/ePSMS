using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services.ParIcs
{
    public interface IIcsParService
    {
        IQueryable<IcsPar> GetAll();
        IQueryable<IcsPar> GetAllPars(Guid? psCardItemGroupId);
        IQueryable<IcsPar> GetAllIcs(Guid? psCardItemGroupId);
        ValueTask<IcsPar> CreateAsync(IcsPar model, string user, DateTime date);
        ValueTask<IcsPar> UpdateAsync(IcsPar model, string user, DateTime date);
        ValueTask<IcsPar> DeleteAsync(IcsPar model, string user, DateTime date);
    }

    public class IcsParService : IIcsParService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions = new CreateAndLogExceptions();        
        private readonly IExceptionService<IcsPar> _exceptionService = new ExceptionService<IcsPar>();

        public IcsParService(AppManEntities db)
        {
            _db = db;
        }

        public IQueryable<IcsPar> GetAll() =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.IcsPars.AsNoTracking().AsQueryable();
            return data;
        });


        public IQueryable<IcsPar> GetAllIcs(Guid? psCardItemGroupId)
        {
            return GetAllIcsPars(psCardItemGroupId, "I");
        }

        public IQueryable<IcsPar> GetAllPars(Guid? psCardItemGroupId)
        {
            return GetAllIcsPars(psCardItemGroupId, "P");
        }

        private IQueryable<IcsPar> GetAllIcsPars(Guid? psCardItemGroupId, string refType) 
        {
            var data = _db.IcsPars.Where(w => w.RefType == refType 
                && w.IcsParItems.Any(a => a.PsCardItemExtn.PsCardItem.GroupId == psCardItemGroupId)).AsNoTracking().AsQueryable();            
            return data;
        }

        public ValueTask<IcsPar> CreateAsync(IcsPar model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {            
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new IcsPar();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.IcsPars.Add(entity);
            await _db.SaveChangesAsync();
            return model;
        });

        public ValueTask<IcsPar> UpdateAsync(IcsPar model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            IcsPar entity = await _db.IcsPars.FindAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            ValidateIfPosted(entity);

            MapModelToEntityFields(entity, model, Mode.EDIT);
            
            _db.IcsPars.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<IcsPar> DeleteAsync(IcsPar model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            IcsPar entity = await _db.IcsPars.FindAsync(model.Id);

            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            ValidateIfPosted(entity);

            model.UpdatedBy = user;
            model.UpdatedDt = date;            

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.IcsPars.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.IcsPars.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();
            return model;
        });

        public void MapModelToEntityFields(IcsPar entity, IcsPar model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            entity.LocationId = model.LocationId;
            entity.LocationCode = model.LocationCode;
            entity.Location = model.Location;
            entity.RefNo = model.RefNo;
            entity.RefDate = model.RefDate;
            entity.RefType = model.RefType;
            entity.ReceivedByTitle = model.ReceivedByTitle;
            entity.ReceivedByTitle2 = model.ReceivedByTitle2;
            entity.ReceivedBy = model.ReceivedBy;
            entity.ReceivedByPosition = model.ReceivedByPosition;
            entity.ReceivedDate = model.ReceivedDate;
            entity.ReceivedDept = model.ReceivedDept;
            entity.IssuedBy = model.IssuedBy;
            entity.IssuedByPosition = model.IssuedByPosition;
            entity.IssuedDate = model.IssuedDate;
            entity.IssuedDept = model.IssuedDept;
            entity.PostedBy = model.PostedBy;
            entity.PostedDt = model.PostedDt;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        public void ValidateIfPosted(IcsPar entity)
        {
            if (!string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordAlreadyPostedException($"Record was already posted by {entity.PostedBy} on {entity.PostedDt}, cannot proceed!");
            }
        }
    }
}