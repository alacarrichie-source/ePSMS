using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLgs.Services.Interfaces
{
    public interface IRequestService
    {        
        IQueryable<RequestVM> GetAll();
        Task<Request> GetById(Guid? prId);
        Request GetByPrNo(string prNo);
        Task Post(Guid requestId, string user, DateTime date);
        Task Unpost(Guid requestId, string user, DateTime date);
        Task<bool> IsPosted(Guid requestId);
        Task<bool> IsWithPO(Guid requestId);
    }
}
