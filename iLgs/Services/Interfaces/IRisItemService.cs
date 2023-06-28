using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLgs.Services.Interfaces
{
    public interface IRisItemService
    {
        IQueryable<RisItemVM> GetByRisId(Guid? risId);
        Task<RisItem> GetByIdAsync(Guid? id);
        Task<RisItemVM> GetVmByIdAsync(Guid? id);

        Task<RisItemVM> CreateAsync(RisItemVM model, string user, DateTime date);
        Task<RisItemVM> UpdateAsync(RisItemVM model, string user, DateTime date);
        Task<RisItemVM> DeleteAsync(RisItemVM model, string user, DateTime date);
    }
}
