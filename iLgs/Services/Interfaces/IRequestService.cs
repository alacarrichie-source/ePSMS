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
        Task<Request> GetByIdAsync(Guid? prId);
        Task<Request> GetByPrNoAsync(string prNo);
        Task<bool> GetAnyPrNoAsync(Guid id, string prNo);
        Task<bool> IsPostedAsync(Guid? requestId);
        Task<bool> IsWithPOAsync(Guid? requestId);
        Task<bool> IsPoPostedAsync(Guid? requestId);

        Task<RequestVM> CreateAsync(RequestVM model, string user, DateTime date);
        Task<RequestVM> UpdateAsync(RequestVM model, string user, DateTime date);
        Task<RequestVM> DeleteAsync(RequestVM model, string user, DateTime date);
        Task PostAsync(Guid requestId, string user, DateTime date);
        Task UnpostAsync(Guid requestId, string user, DateTime date);        
    }
}
