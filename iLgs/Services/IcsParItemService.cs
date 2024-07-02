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
    public interface IIcsParItemService
    {
        IQueryable<IcsParItem> GetAllByIcsParId(Guid? icsParId);
        List<IcsParItem> GetAllParItems(Guid? psCardItemId);
        List<IcsParItem> GetAllIcsItems(Guid? psCardItemId);
        ValueTask<IcsParItem> CreateAsync(IcsParItem model, string user, DateTime date);
        ValueTask<IcsParItem> UpdateAsync(IcsParItem model, string user, DateTime date);
        ValueTask<IcsParItem> DeleteAsync(IcsParItem model, string user, DateTime date);        
    }

    public class IcsParItemService : IIcsParItemService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions _exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<IcsParItem> _exceptionService = new ExceptionService<IcsParItem>();        

        public IcsParItemService(AppManEntities db)
        {
            _db = db;
        }

        public IQueryable<IcsParItem> GetAllByIcsParId(Guid? icsParId) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.IcsParItems.Where(w => w.IcsParId == icsParId).AsNoTracking();
            return data;
        });

        public List<IcsParItem> GetAllParItems(Guid? psCardItemId) =>
        _exceptionService.TryCatch(() =>
        {
            var data = GetAllIcsParItems(psCardItemId, "P");
            return data;
        });

        public List<IcsParItem> GetAllIcsItems(Guid? psCardItemId) =>
        _exceptionService.TryCatch(() =>
        {
            var data = GetAllIcsParItems(psCardItemId, "I");
            return data;
        });

        private List<IcsParItem> GetAllIcsParItems(Guid? psCardItemId, string refType)         
        {
            var data = _db.IcsParItems
                .Where(w => w.PsCardItemId == psCardItemId && w.IcsPar.RefType == refType).AsNoTracking()
                .Select(s => new
                {
                    Id = s.Id,
                    IcsParId = s.IcsParId,
                    PsCardItemId = s.PsCardItemId,
                    Qty = s.Qty,
                    LocationId = s.LocationId,
                    PropNo = s.PropNo,
                    PropYear = s.PropYear,
                    PropSeq = s.PropSeq,
                    Location = s.Codextn.Description,
                    UnitCost = s.PsCardItem.UnitCost,
                    Amount = s.Amount,
                    SerialNo = s.SerialNo,
                    YearModel = s.YearModel,
                    NetWeight = s.NetWeight,
                    ConductionSticker = s.ConductionSticker,
                    BodyNo = s.BodyNo,
                    PlateNo = s.PlateNo,
                    CRN = s.CRN,
                    CRNDate = s.CRNDate,
                    MVFileNo = s.MVFileNo,
                    IcsPar = s.IcsPar
                }).ToList()
                .Select(s => new IcsParItem
                {
                    Id = s.Id,
                    IcsParId = s.IcsParId,
                    PsCardItemId = s.PsCardItemId,
                    Qty = s.Qty,
                    LocationId = s.LocationId,
                    PropNo = s.PropNo,
                    PropYear = s.PropYear,
                    PropSeq = s.PropSeq,
                    Location = s.Location,
                    UnitCost = s.UnitCost,
                    Amount = s.Amount,
                    SerialNo = s.SerialNo,
                    YearModel = s.YearModel,
                    NetWeight = s.NetWeight,
                    ConductionSticker = s.ConductionSticker,
                    BodyNo = s.BodyNo,
                    PlateNo = s.PlateNo,
                    CRN = s.CRN,
                    CRNDate = s.CRNDate,
                    MVFileNo = s.MVFileNo,
                    IcsPar = s.IcsPar
                }).ToList();
            return data;
        }


        public ValueTask<IcsParItem> CreateAsync(IcsParItem model, string user, DateTime date) =>
        _exceptionService.TryCatchAsync(async () =>
        {

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            IcsParItem entity = new IcsParItem()
            {
                Id = model.Id,
                IcsParId = model.IcsParId,
                PsCardItemId = model.PsCardItemId,
                Qty = model.Qty,
                LocationId = model.LocationId,
                PropNo = model.PropNo,
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
        _exceptionService.TryCatchAsync(async () =>
        {
            IcsParItem entity = await _db.IcsParItems.Include(i => i.IcsPar).Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

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

            entity.Qty = model.Qty;
            entity.LocationId = model.LocationId;
            entity.PropNo = model.PropNo;
            entity.PropYear = model.PropYear;
            entity.PropSeq = model.PropSeq;
            entity.SerialNo = model.SerialNo;
            entity.YearModel = model.YearModel;
            entity.NetWeight = model.NetWeight;
            entity.ConductionSticker = model.ConductionSticker;
            entity.BodyNo = model.BodyNo;
            entity.PlateNo = model.PlateNo;
            entity.CRN = model.CRN;
            entity.CRNDate = model.CRNDate;
            entity.MVFileNo = model.MVFileNo;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.IcsParItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<IcsParItem> DeleteAsync(IcsParItem model, string user, DateTime date) =>
        _exceptionService.TryCatchAsync(async () =>
        {
            IcsParItem entity = await _db.IcsParItems.FindAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

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
    }
}