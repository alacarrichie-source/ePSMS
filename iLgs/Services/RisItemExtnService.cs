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
    public class RisItemExtnService : IRisItemExtnService
    {
        private readonly AppManEntities db = new AppManEntities();

        public RisItemExtnService(AppManEntities db)
        {
            this.db = db;
        }

        public IQueryable<RisItemExtnVM> GetAll()
        {
            var data = db.RisItemExtns
                .Select(s => new RisItemExtnVM
                {
                    Id = s.Id,
                    RisItemId = s.RisItemId,
                    ItemKey = s.ItemKey,
                    ItemValue = s.ItemValue,
                    Sequence = s.Sequence
                }).AsQueryable();
            return data;
        }

        public IQueryable<RisItemExtnVM> GetBatchInfo(Guid? RisItemId, Guid? psCodeId)
        {
            var data = db.Database.SqlQuery<RisItemExtnVM>("Exec RisItemExtnService_GetBatchInfo {0}, {1}", RisItemId, psCodeId).AsQueryable();
            return data;
        }

        public async Task SaveAsync(Guid risItemId, List<RisItemExtnVM> risItemExtnList, string user, DateTime date)
        {
            // log updates
            var existingRisItemExtns = db.RisItemExtns.Where(w => w.RisItemId == risItemId).ToList();
            foreach (var risItemExtn in existingRisItemExtns)
            {
                var entity = await db.RisItemExtns.FindAsync(risItemExtn.Id);
                if (entity != null)
                {
                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;
                    db.RisItemExtns.Attach(entity);
                    db.Entry(entity).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    db.RisItemExtns.Remove(entity);
                    db.Entry(entity).State = EntityState.Deleted;
                    await db.SaveChangesAsync();
                }
            }

            foreach (var risItemExtn in risItemExtnList)
            {
                var entity = await db.RisItemExtns.Where(w => w.RisItemId == risItemId && w.ItemKey == risItemExtn.ItemKey).FirstOrDefaultAsync();
                if (entity == null)
                {
                    entity = new iLgs.Models.RisItemExtn()
                    {
                        Id = Guid.NewGuid(),
                        RisItemId = risItemId,
                        ItemKey = risItemExtn.ItemKey,
                        ItemValue = risItemExtn.ItemValue ?? "",
                        Sequence = risItemExtn.Sequence,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };

                    db.RisItemExtns.Add(entity);
                }
                else
                {
                    entity.ItemValue = risItemExtn.ItemValue ?? "";
                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;

                    db.RisItemExtns.Attach(entity);
                    db.Entry(entity).State = EntityState.Modified;
                }
            }
            await db.SaveChangesAsync();
        }        
    }
}