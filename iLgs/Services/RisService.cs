using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Data.Entity;

namespace iLgs.Services
{
    public class RisService : IRisService
    {
        private readonly AppManEntities db = new AppManEntities();
        
        public RisService(AppManEntities db)
        {
            this.db = db;
        }
        
        public IQueryable<RIS_VM> GetAll()
        {
            var data = db.RISses
                .Select(s => new RIS_VM {
                    Id = s.Id,
                    Fund = s.Fund,
                    Division = s.Division,
                    Office = s.Office,
                    FPP = s.FPP,
                    RisNo = s.RisNo,
                    RisDate = s.RisDate,
                    Purpose = s.Purpose,
                    RequestedBy = s.RequestedBy,
                    RequestedByDesignation = s.RequestedByDesignation,
                    RequestedDate = s.RequestedDate,
                    ApprovedBy = s.ApprovedBy,
                    ApprovedByDesignation = s.ApprovedByDesignation,
                    ApprovedDate = s.ApprovedDate,
                    IssuedBy = s.IssuedBy,
                    IssuedByDesignation = s.IssuedByDesignation,
                    IssuedDate = s.IssuedDate,
                    ReceivedBy = s.ReceivedBy,
                    ReceivedByDesignation = s.ReceivedByDesignation,
                    ReceivedDate = s.ReceivedDate,
                    InsertedBy = s.InsertedBy,
                    InsertedDt = s.InsertedDt,
                    UpdatedBy = s.UpdatedBy,
                    UpdatedDt = s.UpdatedDt,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt
                });
            return data;
        }

        public async Task<bool> GetAnyRisNoAsync(Guid risId, string risNo)
        {
            return await db.RISses.AnyAsync(a => a.Id != risId && a.RisNo == risNo);
        }

        public async Task<RISs> GetByIdAsync(Guid id)
        {
            return await db.RISses.FindAsync(id);
        }

        public async Task<RISs> GetByRisNoAsync(string risNo)
        {
            return await db.RISses.Where(w => w.RisNo == risNo).FirstOrDefaultAsync();
        }

        public async Task<bool> IsPostedAsync(Guid risId)
        {
            var entity = await db.RISses.FindAsync(risId);
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }

        public async Task<bool> IsPrPostedAsync(Guid risId)
        {
            var pr = await db.Requests.Where(a => a.RisId == risId).FirstOrDefaultAsync();
            if (pr != null)
            {
                return !string.IsNullOrWhiteSpace(pr.SubmittedBy);
            }
            return false;            
        }

        public async Task PostAsync(Guid risId, string user, DateTime date)
        {
            var entity = await db.RISses.FindAsync(risId);
            if (entity != null)
            {
                entity.PostedBy = user;
                entity.PostedDt = date;
                entity.UpdatedBy = user;
                entity.UpdatedDt = date;

                db.RISses.Attach(entity);
                db.Entry(entity).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }
        }

        public async Task UnpostAsync(Guid risId, string user, DateTime date)
        {
            var entity = await db.RISses.FindAsync(risId);
            if (entity != null)
            {
                entity.PostedBy = null;
                entity.PostedDt = null;
                entity.UpdatedBy = user;
                entity.UpdatedDt = date;

                db.RISses.Attach(entity);
                db.Entry(entity).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }
        }

        public async Task<RIS_VM> CreateAsync(RIS_VM model, string user, DateTime date)
        {
            model.Id = Guid.NewGuid();
            if (string.IsNullOrWhiteSpace(model.RisNo))
            {
                model.RisNo = NextRisNo((DateTime)model.RisDate);
            }
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = new iLgs.Models.RISs()
            {
                Id = model.Id,
                Fund = model.Fund,
                Division = model.Division ?? "",
                Office = model.Office,
                FPP = model.FPP,
                RisNo = model.RisNo,
                RisDate = model.RisDate,
                Purpose = model.Purpose,
                RequestedBy = model.RequestedBy ?? "",
                RequestedByDesignation = model.RequestedByDesignation ?? "",
                RequestedDate = model.RequestedDate,
                ApprovedBy = model.ApprovedBy ?? "",
                ApprovedByDesignation = model.ApprovedByDesignation ?? "",
                ApprovedDate = model.ApprovedDate,
                IssuedBy = model.IssuedBy ?? "",
                IssuedByDesignation = model.IssuedByDesignation ?? "",
                IssuedDate = model.IssuedDate,
                ReceivedBy = model.ReceivedBy ?? "",
                ReceivedByDesignation = model.ReceivedByDesignation ?? "",
                ReceivedDate = model.ReceivedDate,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            db.RISses.Add(entity);
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<RIS_VM> DeleteAsync(RIS_VM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await db.RISses.FindAsync(model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.RISses.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.RISses.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<RIS_VM> UpdateAsync(RIS_VM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await db.RISses.FindAsync(model.Id);

            entity.Fund = model.Fund;
            entity.Division = model.Division ?? "";
            entity.Office = model.Office;
            entity.FPP = model.FPP;
            entity.RisNo = model.RisNo;
            entity.RisDate = model.RisDate;
            entity.Purpose = model.Purpose;
            entity.RequestedBy = model.RequestedBy ?? "";
            entity.RequestedByDesignation = model.RequestedByDesignation ?? "";
            entity.RequestedDate = model.RequestedDate;
            entity.ApprovedBy = model.ApprovedBy ?? "";
            entity.ApprovedByDesignation = model.ApprovedByDesignation ?? "";
            entity.ApprovedDate = model.ApprovedDate;
            entity.IssuedBy = model.IssuedBy ?? "";
            entity.IssuedByDesignation = model.IssuedByDesignation ?? "";
            entity.IssuedDate = model.IssuedDate;
            entity.ReceivedBy = model.ReceivedBy ?? "";
            entity.ReceivedByDesignation = model.ReceivedByDesignation ?? "";
            entity.ReceivedDate = model.ReceivedDate;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
            
            db.RISses.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return model;
        }

        private string NextRisNo(DateTime date)
        {
            string yyyy = date.Year.ToString().Trim();
            string mm = date.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = db.RISses.Where(w => w.RisDate.Value.Year == date.Year).OrderByDescending(o => o.RisNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(data.RisNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }
    }
}