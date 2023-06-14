using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using iLgs.Models;
using System.Data.Entity;

namespace iLgs.Services
{
    public class RequestService : IRequestService
    {
        private readonly AppManEntities db = new AppManEntities();
        public RequestService(AppManEntities db)
        {
            this.db = db;
        }
        public IQueryable<RequestVM> GetAll()
        {
            return db.Requests
                .Select(s => new RequestVM {
                    Id = s.Id,
                    Fund = s.Fund,
                    Department = s.Department,
                    Section = s.Section,
                    PrNo = s.PrNo,
                    PrDate = s.PrDate,
                    FPP = s.FPP,
                    Purpose = s.Purpose,
                    RequestedBy = s.RequestedBy,
                    RequestedDesig = s.RequestedDesig,
                    Availability = s.Availability,
                    AvaialbilityDesig = s.AvaialbilityDesig,
                    ApprovedBy = s.ApprovedBy,
                    ApprovedDesig = s.ApprovedDesig,
                    SubmittedBy = s.SubmittedBy,
                    SubmittedDt = s.SubmittedDt,
                    IsWithPO = s.Orders.Any()
                })
                .AsQueryable();
        }

        public async Task<Request> GetByIdAsync(Guid? prId)
        {
            return await db.Requests.FindAsync(prId);            
        }

        public async Task<Request> GetByPrNoAsync(string prNo)
        {
            return await db.Requests.Where(w => w.PrNo == prNo).FirstOrDefaultAsync();
        }

        public async Task<bool> IsPostedAsync(Guid? requestId)
        {
            var entity = await db.Requests.FindAsync(requestId);
            if (entity == null)
            {
                return false;
            }
            else
            {
                return !string.IsNullOrWhiteSpace(entity.SubmittedBy);
            }
        }

        public async Task<bool> IsWithPOAsync(Guid? requestId)
        {
            return await db.Orders.AnyAsync(a => a.PrId == requestId);            
        }

        public async Task<bool> IsPoPostedAsync(Guid? requestId)
        {            
            return await db.Orders.AnyAsync(a => a.PrId == requestId && !(a.PostedBy == "" || a.PostedBy == null) );
        }

        public async Task PostAsync(Guid requestId, string user, DateTime date)
        {
            var entity = await db.Requests.FindAsync(requestId);
            if (entity != null)
            {
                entity.SubmittedBy = user;
                entity.SubmittedDt = date;
                entity.UpdatedBy = user;
                entity.UpdatedDt = date;

                db.Requests.Attach(entity);
                db.Entry(entity).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }
        }

        public async Task UnpostAsync(Guid requestId, string user, DateTime date)
        {
            var entity = await db.Requests.FindAsync(requestId);
            if (entity != null)
            {
                entity.SubmittedBy = null;
                entity.SubmittedDt = null;
                entity.UpdatedBy = user;
                entity.UpdatedDt = date;

                db.Requests.Attach(entity);
                db.Entry(entity).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }
        }
    }
}