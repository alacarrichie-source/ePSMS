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
        IQueryable<Request> GetAll();
        Task<Request> GetById(Guid? prId);
        Request GetByPrNo(string prNo);
    }
}
