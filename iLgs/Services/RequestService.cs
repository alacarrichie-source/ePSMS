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

        public async Task<Request> GetById(Guid? prId)
        {
            return await db.Requests.FindAsync(prId);            
        }

        public Request GetByPrNo(string prNo)
        {
            return db.Requests.Where(w => w.PrNo == prNo).FirstOrDefault();
        }

        public async Task<bool> IsPosted(Guid requestId)
        {
            var entity = await db.Requests.FindAsync(requestId);
            return !string.IsNullOrWhiteSpace(entity.SubmittedBy);
        }

        public async Task<bool> IsWithPO(Guid requestId)
        {
            return await db.Orders.AnyAsync(a => a.PrId == requestId);            
        }

        public async Task Post(Guid requestId, string user, DateTime date)
        {
            var entity = await db.Requests.FindAsync(requestId);
            entity.SubmittedBy = user;
            entity.SubmittedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.Requests.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();            
        }

        public async Task Unpost(Guid requestId, string user, DateTime date)
        {
            var entity = await db.Requests.FindAsync(requestId);
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