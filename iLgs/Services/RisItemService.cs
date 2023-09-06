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
        public async Task<RisItemVM> GetVmByIdAsync(Guid? id)
        {
            var data = await db.RisItems.Where(w => w.Id == id)
                .Select(s => new RisItemVM
                {
                    Id = s.Id,
                    RisId = s.RisId,
                    PsType = s.ItemCode.ItemType.Description,
                    PsNo = s.PsNo,
                    Unit = s.Unit,
                    ItemName = s.ItemName,
                    Description = s.Description,
                    QtyRequest = s.QtyRequest,
                    QtyIssue = s.QtyIssue,
                    Remarks = s.Remarks,
                    InsertedDt = s.InsertedDt
                }).FirstOrDefaultAsync();
            return data;
        }

        public async Task<RisItem> GetByIdAsync(Guid? id)
        {
            var data = await db.RisItems.FindAsync(id);
            return data;
        }

        public IQueryable<RisItemVM> GetByRisId(Guid? risId)
        {
            var data = db.RisItems.Where(w => w.RisId == risId)
                .Select(s => new RisItemVM
                {
                    Id = s.Id,
                    RisId = s.RisId,
                    PsType = s.ItemCode.ItemType.Description,
                    PsNo = s.PsNo,
                    Unit = s.Unit,
                    ItemName = s.ItemName,
                    Description = s.Description,
                    QtyRequest = s.QtyRequest,
                    QtyIssue = s.QtyIssue,
                    Remarks = s.Remarks,
                    InsertedDt = s.InsertedDt
                });
            return data;
        }

        public async Task<RisItemVM> CreateAsync(RisItemVM model, string user, DateTime date)
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;                       

            model.PsNo = GeneratePsNo(model);

            RisItem entity = new RisItem()
            {
                Id = model.Id,
                RisId = model.RisId,
                PsNo = model.PsNo,
                ItemName = model.ItemName,
                Unit = model.Unit,
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
        
        public async Task<RisItemVM> UpdateAsync(RisItemVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RisItem entity = await db.RisItems.FindAsync(model.Id);

            model.PsNo = GeneratePsNo(model);

            entity.RisId = model.RisId;
            entity.PsNo = model.PsNo;
            entity.ItemName = model.ItemName;
            entity.Unit = model.Unit;
            entity.Description = model.Description;
            entity.QtyRequest = model.QtyRequest;
            entity.QtyIssue = model.QtyIssue;
            entity.Remarks = model.Remarks ?? "";
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.RisItems.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            // cascade updates
            // PR, Description, Qty
            // PO, Description, Qty
            // AIR, Qty            

            return model;
        }        

        private string GeneratePsNo(RisItemVM model)
        {
            var risItemExtns = (List<RisItemExtnVM>)Newtonsoft.Json.JsonConvert.DeserializeObject(model.GridRisItemExtns, typeof(List<RisItemExtnVM>));
            string psNo = model.PsType.Trim() + "-" + model.ItemName.Substring(0, 3) + "-";
            foreach(var risItemExtn in risItemExtns)
            {
                if (!string.IsNullOrWhiteSpace(risItemExtn.ItemValue))
                {
                    psNo += risItemExtn.ItemValue.Substring(0, 1);
                }
            }
            return psNo.ToUpper();
        }
    }
}