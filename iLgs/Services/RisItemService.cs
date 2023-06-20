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
    public class RisItemService : IRisItemService
    {
        private readonly AppManEntities db = new AppManEntities();

        public RisItemService(AppManEntities db)
        {
            this.db = db;
        }

        public async Task<RisItemVM> CreateAsync(RisItemVM model, string user, DateTime date)
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            RisItem entity = new RisItem()
            {
                Id = model.Id,
                RisId = model.RisId,
                PsCodeId = model.PsCodeId,
                Description = model.Description,
                QtyRequest = model.QtyRequest,
                QtyIssue = model.QtyIssue,
                Remarks = model.Remarks ?? "",
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            db.RisItems.Add(entity);
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<RisItemVM> DeleteAsync(RisItemVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RisItem entity = await db.RisItems.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.RisItems.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.RisItems.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<RisItemVM> GetByIdAsync(Guid? id)
        {
            var data = await db.RisItems.Where(w => w.Id == id)
                .Select(s => new RisItemVM
                {
                    Id = s.Id,
                    RisId = s.RisId,
                    PsCodeId = s.PsCodeId,
                    PsCode = s.PsCode.PsNo,
                    PsUnit = s.PsCode.UnitMeas,
                    PsItem = s.PsCode.ItemName,
                    Description = s.Description,
                    QtyRequest = s.QtyRequest,
                    QtyIssue = s.QtyIssue,
                    Remarks = s.Remarks,
                    InsertedDt = s.InsertedDt
                }).FirstOrDefaultAsync();
            return data;
        }

        public IQueryable<RisItemVM> GetByRisId(Guid? risId)
        {
            var data = db.RisItems.Where(w => w.RisId == risId)
                .Select(s => new RisItemVM
                {
                    Id = s.Id,
                    RisId = s.RisId,
                    PsCodeId = s.PsCodeId,
                    PsCode = s.PsCode.PsNo,
                    PsUnit = s.PsCode.UnitMeas,
                    PsItem = s.PsCode.ItemName,
                    Description = s.Description,
                    QtyRequest = s.QtyRequest,
                    QtyIssue = s.QtyIssue,
                    Remarks = s.Remarks,
                    InsertedDt = s.InsertedDt
                });
            return data;
        }

        public async Task<RisItemVM> UpdateAsync(RisItemVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RisItem entity = await db.RisItems.FindAsync(model.Id);

            entity.RisId = model.RisId;
            entity.PsCodeId = model.PsCodeId;
            entity.Description = model.Description;
            entity.QtyRequest = model.QtyRequest;
            entity.QtyIssue = model.QtyIssue;
            entity.Remarks = model.Remarks ?? "";
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.RisItems.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return model;
        }        
    }
}