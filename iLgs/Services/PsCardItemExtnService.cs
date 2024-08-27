using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services
{
    public interface IPsCardItemExtnService
    {
        //IQueryable<PsCardItemExtn> GetAllByPsCardItemId(Guid? psCardItemId);
        //ValueTask<PsCardItemExtn> GetByIdAsync(Guid? id);
        //ValueTask<PsCardItemExtn> CreateAsync(PsCardItemExtn model, string user, DateTime date);
        //ValueTask<PsCardItemExtn> UpdateAsync(PsCardItemExtn model, string user, DateTime date);
        //ValueTask<PsCardItemExtn> DeleteAsync(PsCardItemExtn model, string user, DateTime date);

        IPsCardItemExtnVehicleService PsCardItemExtnVehicle { get; }
        IPsCardItemExtnOtherService PsCardItemExtnOther { get; }

        //IQueryable<PsCardItemExtn> GetPsCardItemExtnForIcs(Guid? psCardItemId);

        IQueryable<T> GetCardItemExtnForIcsPars<T>(Guid? psCardItemId) where T : PsCardItemExtn;
        IQueryable<PsCardItemExtn> GetCardItemExtnForIcsParsByType(Guid? psCardItemId);
    }


    public class PsCardItemExtnService : IPsCardItemExtnService
    {
        private readonly AppManEntities _db = new AppManEntities();

        //private readonly ICreateAndLogExceptions _exceptions = new CreateAndLogExceptions();
        //private readonly IExceptionService<PsCardItemExtn> _exceptionService = new ExceptionService<PsCardItemExtn>();

        private IPsCardItemExtnVehicleService _psCardItemExtnVehicleService;
        private IPsCardItemExtnOtherService _psCardItemExtnOtherService;

        public PsCardItemExtnService(AppManEntities db)
        {
            _db = db;
            _psCardItemExtnVehicleService = new PsCardItemExtnVehicleService(db);
            _psCardItemExtnOtherService = new PsCardItemExtnOtherService(db);
        }

        public IPsCardItemExtnVehicleService PsCardItemExtnVehicle { get { return _psCardItemExtnVehicleService = _psCardItemExtnVehicleService ?? new PsCardItemExtnVehicleService(_db); } }
        public IPsCardItemExtnOtherService PsCardItemExtnOther { get { return _psCardItemExtnOtherService = _psCardItemExtnOtherService ?? new PsCardItemExtnOtherService(_db); } }

        public IQueryable<T> GetCardItemExtnForIcsPars<T>(Guid? psCardItemId) where T : PsCardItemExtn
        {
            var data = _db.PsCardItemExtns.OfType<T>().AsNoTracking()
                        .Where(w => w.PsCardItemId == psCardItemId && !w.IcsParItems.Any(a => a.PsCardItemExtnId == w.Id))
                        .AsQueryable();
            return data;
        }

        public IQueryable<PsCardItemExtn> GetCardItemExtnForIcsParsByType(Guid? psCardItemId)
        {
            IPsCardService psCardService = new PsCardService(_db);
            var itemExtnName = psCardService.GetItemExtnName(psCardItemId);
            switch (itemExtnName)
            {
                case "ItemExtnLand":
                    return GetCardItemExtnForIcsPars<PsCardItemExtnLand>(psCardItemId);
                case "ItemExtnBldg":
                    return GetCardItemExtnForIcsPars<PsCardItemExtnBuilding>(psCardItemId);
                case "ItemExtnVehicle":
                    return GetCardItemExtnForIcsPars<PsCardItemExtnVehicle>(psCardItemId);
                default:
                    return GetCardItemExtnForIcsPars<PsCardItemExtnOther>(psCardItemId);                
            }
        }

            //public IQueryable<PsCardItemExtnOther> GetCardItemExtn(Guid? psCardItemId)
            //{
            //    var data = _db.PsCardItemExtns.OfType<PsCardItemExtnOther>()
            //        .Where(w => w.PsCardItemId == psCardItemId)
            //        .AsQueryable();
            //    return data;
            //}

            //public IQueryable<PsCardItemExtn> GetAllByPsCardItemId(Guid? psCardItemId) =>
            //_exceptionService.TryCatch(() =>
            //{
            //    var data = _db.PsCardItemExtns.Where(w => w.PsCardItemId == psCardItemId).AsNoTracking();
            //    return data;
            //});

            //public async ValueTask<PsCardItemExtn> GetByIdAsync(Guid? id)
            //{
            //    var data = await _db.PsCardItemExtns
            //        .Include(i => i.PsCardItemExtnLand)
            //        .Include(i => i.PsCardItemExtnBuilding)
            //        .Include(i => i.PsCardItemExtnVehicle)
            //        .Include(i => i.PsCardItemExtnOther)
            //        .Where(w => w.Id == id)
            //        .FirstOrDefaultAsync();
            //    return data;
            //}

            //public ValueTask<PsCardItemExtn> CreateAsync(PsCardItemExtn model, string user, DateTime date) =>
            //_exceptionService.TryCatchAsync(async () =>
            //{

            //    model.Id = Guid.NewGuid();
            //    model.InsertedBy = user;
            //    model.UpdatedBy = user;
            //    model.InsertedDt = date;
            //    model.UpdatedDt = date;

            //    PsCardItemExtn entity = new PsCardItemExtn()
            //    {
            //        Id = model.Id,
            //        PsCardItemId = model.PsCardItemId,
            //        LocationId = model.LocationId,
            //        PropNo = model.PropNo,
            //        PropYear = model.PropYear,
            //        PropSeq = model.PropSeq,
            //        CustItemNo = model.CustItemNo,
            //        SeriesNo = model.SeriesNo,
            //        Remarks = model.Remarks,
            //        InsertedBy = model.InsertedBy,
            //        InsertedDt = model.InsertedDt,
            //        UpdatedBy = model.UpdatedBy,
            //        UpdatedDt = model.UpdatedDt
            //    };

            //    _db.PsCardItemExtns.Add(entity);
            //    await _db.SaveChangesAsync();
            //    return model;
            //});

            //public ValueTask<PsCardItemExtn> UpdateAsync(PsCardItemExtn model, string user, DateTime date) =>
            //_exceptionService.TryCatchAsync(async () =>
            //{
            //    var entity = await GetByIdAsync(model.Id);

            //    if (entity == null)
            //    {
            //        throw new RecordNotFoundException(model.Id);
            //    }

            //    model.UpdatedBy = user;
            //    model.UpdatedDt = date;

            //    entity.LocationId = model.LocationId;
            //    entity.PropNo = model.PropNo;
            //    entity.PropYear = model.PropYear;
            //    entity.PropSeq = model.PropSeq;
            //    entity.CustItemNo = model.CustItemNo;
            //    entity.SeriesNo = model.SeriesNo;
            //    entity.Remarks = model.Remarks;

            //    entity.UpdatedBy = model.UpdatedBy;
            //    entity.UpdatedDt = model.UpdatedDt;

            //    _db.PsCardItemExtns.Attach(entity);
            //    _db.Entry(entity).State = EntityState.Modified;
            //    await _db.SaveChangesAsync();

            //    return model;
            //});

            //public ValueTask<PsCardItemExtn> DeleteAsync(PsCardItemExtn model, string user, DateTime date) =>
            //_exceptionService.TryCatchAsync(async () =>
            //{
            //    PsCardItemExtn entity = await _db.PsCardItemExtns.FindAsync(model.Id);
            //    if (entity == null)
            //    {
            //        throw new RecordNotFoundException(model.Id);
            //    }

            //    model.UpdatedBy = user;
            //    model.UpdatedDt = date;

            //    entity.UpdatedBy = model.UpdatedBy;
            //    entity.UpdatedDt = model.UpdatedDt;

            //    _db.PsCardItemExtns.Attach(entity);
            //    _db.Entry(entity).State = EntityState.Modified;
            //    await _db.SaveChangesAsync();

            //    _db.PsCardItemExtns.Remove(entity);
            //    _db.Entry(entity).State = EntityState.Deleted;
            //    await _db.SaveChangesAsync();            

            //    return model;
            //});

        }
}