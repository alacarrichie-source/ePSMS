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
    public interface IPsCardItemExtnOtherService
    {
        IQueryable<PsCardItemExtnOther> GetByPsCardItemId(Guid? psCardItemId);
        ValueTask<PsCardItemExtnOther> GetByIdAsync(Guid? id);

        ValueTask<PsCardItemExtnOther> CreateAsync(PsCardItemExtnOther model, string user, DateTime date);
        ValueTask<PsCardItemExtnOther> UpdateAsync(PsCardItemExtnOther model, string user, DateTime date);
        ValueTask<PsCardItemExtnOther> DeleteAsync(PsCardItemExtnOther model, string user, DateTime date);
    }

    public class PsCardItemExtnOtherService : IPsCardItemExtnOtherService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly IExceptionService<PsCardItemExtnOther> _exceptionService = new ExceptionService<PsCardItemExtnOther>();

        public PsCardItemExtnOtherService(AppManEntities db)
        {
            this._db = db;
        }

        public IQueryable<PsCardItemExtnOther> GetByPsCardItemId(Guid? psCardItemId)
        {
            var data = _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().Where(w => w.PsCardItemId == psCardItemId);
            return data;
        }

        public ValueTask<PsCardItemExtnOther> GetByIdAsync(Guid? id) => _exceptionService.TryCatchAsync(async () =>
        {
            var data = await _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().Where(w => w.Id == id).FirstOrDefaultAsync();
            return data;
        });

        public ValueTask<PsCardItemExtnOther> CreateAsync(PsCardItemExtnOther model, string user, DateTime date) => _exceptionService.TryCatchAsync(async () =>
        {
            //if (await IsPostedAsync(model.PsCardItemId))
            //{
            //    throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            //}

            var itemQty = (int)_db.PsCardItems.FirstOrDefault(f => f.Id == model.PsCardItemId).Qty;
            var itemExtns = _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().Where(w => w.PsCardItemId == model.PsCardItemId);

            if (itemExtns.Count() >= itemQty)
            {
                throw new InvalidValueException($"Cannot create more than {itemQty} record(s).");
            }

            if (itemExtns.Any(a => a.SerialNo == model.SerialNo))
            {
                throw new RecordAlreadyExistsException($"Serial No. {model.SerialNo} already exists!");
            }

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            model.Id = Guid.NewGuid();

            var entity = new PsCardItemExtnOther()
            {
                Id = model.Id,
                PsCardItemId = model.PsCardItemId,
                ContentNo = model.ContentNo,
                CustItemNo = model.CustItemNo,
                SerialNo = model.SerialNo,
                Condition = model.Condition,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.PsCardItemExtns.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<PsCardItemExtnOther> DeleteAsync(PsCardItemExtnOther model, string user, DateTime date) => _exceptionService.TryCatchAsync(async () =>
        {
            //if (await IsPostedAsync(model.PsCardItemId))
            //{
            //    throw new RecordAlreadyPostedException("Record already posted, cannot delete!");
            //}

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().FirstOrDefaultAsync(f => f.Id == model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItemExtns.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCardItemExtns.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<PsCardItemExtnOther> UpdateAsync(PsCardItemExtnOther model, string user, DateTime date) => _exceptionService.TryCatchAsync(async () =>
        {
            //if (await IsPostedAsync(model.PsCardItemId))
            //{
            //    throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            //}
            var itemExtn = await _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().Where(w => w.PsCardItemId == model.PsCardItemId && w.Id != model.Id).FirstOrDefaultAsync();

            if (itemExtn != null)
            {
                throw new RecordAlreadyExistsException($"Serial No. {model.SerialNo} already exists!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().FirstOrDefaultAsync(f => f.Id == model.Id);

            entity.ContentNo = model.ContentNo;
            entity.CustItemNo = model.CustItemNo;
            entity.SerialNo = model.SerialNo;
            entity.Condition = model.Condition;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PsCardItemExtns.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        private async ValueTask<bool> IsPostedAsync(Guid? PsCardItemId)
        {
            var entity = await _db.AIRs.Where(w => w.AIRItems.Any(a => a.Id == PsCardItemId)).FirstOrDefaultAsync();
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }
    }
}