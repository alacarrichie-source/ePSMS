using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLgs.Services.Interfaces
{
    public interface IRequestItemService
    {
        IQueryable<RequestItemVM> GetByPrId(Guid? prId);
        Task<RequestItemVM> GetByIdAsync(Guid? id);

        Task<RequestItemVM> CreateAsync(RequestItemVM model, string user, DateTime date);
        Task<RequestItemVM> UpdateAsync(RequestItemVM model, string user, DateTime date);
        Task<RequestItemVM> DeleteAsync(RequestItemVM model, string user, DateTime date);
    }
}
