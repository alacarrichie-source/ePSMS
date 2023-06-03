using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services
{
    public interface IRequestItemExtnService
    {
        IQueryable<RequestItemExtnVM> GetAll();
        IQueryable<RequestItemExtnVM> GetBatchInfo(Guid? requestItemId, Guid? psCodeId);
        Task UpdateBatchAsync(List<RequestItemExtnVM> itemExtnList, string user, DateTime date);
        Task SaveAsync(Guid requestItemId, List<RequestItemExtnVM> requestItemExtnList, string user, DateTime date);
        Task<RequestItemExtnVM> CreateAsync(RequestItemExtnVM model, string user, DateTime date);
        Task<RequestItemExtnVM> UpdateAsync(RequestItemExtnVM model, string user, DateTime date);
        Task<RequestItemExtnVM> DeleteAsync(RequestItemExtnVM model, string user, DateTime date);
    }

    public class RequestItemExtnService : IRequestItemExtnService
    {
        private readonly AppManEntities db = new AppManEntities();

        public RequestItemExtnService(AppManEntities db)
        {
            this.db = db;
        }

        public IQueryable<RequestItemExtnVM> GetAll()
        {
            var data = db.RequestItemExtns
                .Select(s => new RequestItemExtnVM
                {
                    Id = s.Id,
                    RequestItemId = s.RequestItemId,
                    ItemKey = s.ItemKey,
                    ItemValue = s.ItemValue
                }).AsQueryable();
            return data;
        }

        public IQueryable<RequestItemExtnVM> GetBatchInfo(Guid? requestItemId, Guid? psCodeId)
        {
            var data = db.Database.SqlQuery<RequestItemExtnVM>("Exec RequestItemExtnService_GetBatchInfo {0}, {1}", requestItemId, psCodeId).AsQueryable();
            return data;
        }

        public async Task SaveAsync(Guid requestItemId, List<RequestItemExtnVM> requestItemExtnList, string user, DateTime date)
        {
            // log updates
            var existingRequestItemExtns = db.RequestItemExtns.Where(w => w.RequestItemId == requestItemId).ToList();
            foreach(var requestItemExtn in existingRequestItemExtns)
            {
                var entity = await db.RequestItemExtns.FindAsync(requestItemExtn.Id);
                if (entity != null)
                {
                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;
                    db.RequestItemExtns.Attach(entity);
                    db.Entry(entity).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    db.RequestItemExtns.Remove(entity);
                    db.Entry(entity).State = EntityState.Deleted;
                    await db.SaveChangesAsync();
                }
            }
            
            foreach (var requestItemExtn in requestItemExtnList)
            {
                var entity = await db.RequestItemExtns.Where(w => w.RequestItemId == requestItemId && w.ItemKey == requestItemExtn.ItemKey).FirstOrDefaultAsync();
                if (entity == null)
                {
                    entity = new iLgs.Models.RequestItemExtn()
                    {
                        Id = Guid.NewGuid(),
                        RequestItemId = requestItemExtn.RequestItemId,
                        ItemKey = requestItemExtn.ItemKey,
                        ItemValue = requestItemExtn.ItemValue,
                        InsertedBy = user, 
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };

                    db.RequestItemExtns.Add(entity);                    
                } else
                {
                    entity.ItemValue = requestItemExtn.ItemValue;
                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;

                    db.RequestItemExtns.Attach(entity);
                    db.Entry(entity).State = EntityState.Modified;
                }                
            }
            await db.SaveChangesAsync();
        }

        public async Task<RequestItemExtnVM> CreateAsync(RequestItemExtnVM model, string user, DateTime date)
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = new iLgs.Models.RequestItemExtn()
            {
                Id = model.Id,
                RequestItemId = model.RequestItemId,
                ItemKey = model.ItemKey,
                ItemValue = model.ItemValue,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            db.RequestItemExtns.Add(entity);
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<RequestItemExtnVM> DeleteAsync(RequestItemExtnVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await db.RequestItemExtns.FindAsync(model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.RequestItemExtns.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.RequestItemExtns.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<RequestItemExtnVM> UpdateAsync(RequestItemExtnVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await db.RequestItemExtns.FindAsync(model.Id);

            entity.RequestItemId = model.RequestItemId;
            entity.ItemKey = model.ItemKey;
            entity.ItemValue = model.ItemValue;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.RequestItemExtns.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return model;
        }

        public async Task UpdateBatchAsync(List<RequestItemExtnVM> itemExtnList, string user, DateTime date)
        {
            // Filter out null values
            var filteredList = itemExtnList.Where(w => !string.IsNullOrWhiteSpace(w.ItemValue)).ToList();
            var description = string.Join(" ", filteredList);
        }
    }
}