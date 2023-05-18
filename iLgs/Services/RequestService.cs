using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using iLgs.Models;

namespace iLgs.Services
{
    public class RequestService : IRequestService
    {
        private readonly AppManEntities db = new AppManEntities();
        public RequestService(AppManEntities db)
        {
            this.db = db;
        }
        public IQueryable<Request> GetAll()
        {
            return db.Requests.AsQueryable();
        }

        public async Task<Request> GetById(Guid? prId)
        {
            return await db.Requests.FindAsync(prId);            
        }

        public Request GetByPrNo(string prNo)
        {
            return db.Requests.Where(w => w.PrNo == prNo).FirstOrDefault();
        }
    }
}