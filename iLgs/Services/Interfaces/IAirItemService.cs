using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLgs.Services.Interfaces
{
    public interface IAirItemService
    {
        IQueryable<AIRItemVM> GetByAirId(Guid? airId);
        Task<AIRItemVM> GetByIdAsync(Guid? id);

        Task<AIRItemVM> CreateAsync(AIRItemVM model, string user, DateTime date);
        Task<AIRItemVM> UpdateAsync(AIRItemVM model, string user, DateTime date);
        Task<AIRItemVM> DeleteAsync(AIRItemVM model, string user, DateTime date);
    }
}
