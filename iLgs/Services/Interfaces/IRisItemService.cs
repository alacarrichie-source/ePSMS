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
        ValueTask<RisItem> GetByIdAsync(Guid? id);
        ValueTask<RisItemVM> GetVmByIdAsync(Guid? id);

        string PsNoDisplay(string itemCode, string itemName);

        ValueTask<RisItemVM> CreateAsync(RisItemVM model, string user, DateTime date);
        ValueTask<RisItemVM> UpdateAsync(RisItemVM model, string user, DateTime date);
        ValueTask<RisItemVM> DeleteAsync(RisItemVM model, string user, DateTime date);
    }
}
