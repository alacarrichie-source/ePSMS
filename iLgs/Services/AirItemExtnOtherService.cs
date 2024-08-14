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
    public interface IAirItemExtnOtherService
    {
        IQueryable<AIRItemExtnOther> GetByAirItemId(Guid? airItemId);
        ValueTask<AIRItemExtnOther> GetByIdAsync(Guid? id);

        ValueTask<AIRItemExtnOther> CreateAsync(AIRItemExtnOther model, string user, DateTime date);
        ValueTask<AIRItemExtnOther> UpdateAsync(AIRItemExtnOther model, string user, DateTime date);
        ValueTask<AIRItemExtnOther> DeleteAsync(AIRItemExtnOther model, string user, DateTime date);
    }

    public class AirItemExtnOtherService : IAirItemExtnOtherService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly IExceptionService<AIRItemExtnOther> _exceptionService = new ExceptionService<AIRItemExtnOther>();

        public AirItemExtnOtherService(AppManEntities db)
        {
            this._db = db;
        }

        public IQueryable<AIRItemExtnOther> GetByAirItemId(Guid? airItemId)
        {
            var data = _db.AIRItemExtns.OfType<AIRItemExtnOther>().Where(w => w.AIRItemId == airItemId);
            return data;
        }

        public ValueTask<AIRItemExtnOther> GetByIdAsync(Guid? id) => _exceptionService.TryCatchAsync(async () =>
        {
            var data = await _db.AIRItemExtns.OfType<AIRItemExtnOther>().Where(w => w.Id == id).FirstOrDefaultAsync();
            return data;
        });

        public ValueTask<AIRItemExtnOther> CreateAsync(AIRItemExtnOther model, string user, DateTime date) => _exceptionService.TryCatchAsync(async () =>
        {
            if (await IsPostedAsync(model.AIRItemId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            }

            var airItemQty = (int)_db.AIRItems.FirstOrDefault(f => f.Id == model.AIRItemId).Qty;
            var airItemExtnCount = _db.AIRItemExtns.OfType<AIRItemExtnOther>().Where(w => w.AIRItemId == model.AIRItemId).Count();

            if (airItemQty == airItemExtnCount)
            {
                throw new InvalidValueException($"Cannot create more than {airItemQty} record(s).");
            }

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            model.Id = Guid.NewGuid();

            var entity = new AIRItemExtnOther()
            {
                Id = model.Id,
                AIRItemId = model.AIRItemId,
                ContentNo = model.ContentNo,
                CustItemNo = model.CustItemNo,
                SerialNo = model.SerialNo,
                Condition = model.Condition,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.AIRItemExtns.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<AIRItemExtnOther> DeleteAsync(AIRItemExtnOther model, string user, DateTime date) => _exceptionService.TryCatchAsync(async () =>
        {
            if (await IsPostedAsync(model.AIRItemId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot delete!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.AIRItemExtns.OfType<AIRItemExtnOther>().FirstOrDefaultAsync(f => f.Id == model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.AIRItemExtns.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.AIRItemExtns.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<AIRItemExtnOther> UpdateAsync(AIRItemExtnOther model, string user, DateTime date) => _exceptionService.TryCatchAsync(async () =>
        {
            if (await IsPostedAsync(model.AIRItemId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.AIRItemExtns.OfType<AIRItemExtnOther>().FirstOrDefaultAsync(f => f.Id == model.Id);

            entity.ContentNo = model.ContentNo;
            entity.CustItemNo = model.CustItemNo;
            entity.SerialNo = model.SerialNo;
            entity.Condition = model.Condition;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.AIRItemExtns.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        private async ValueTask<bool> IsPostedAsync(Guid? airItemId)
        {
            var entity = await _db.AIRs.Where(w => w.AIRItems.Any(a => a.Id == airItemId)).FirstOrDefaultAsync();
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }        
    }
}