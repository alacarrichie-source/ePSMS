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
        IQueryable<RisItemEntryVM> GetByRisId(Guid? risId);
        ValueTask<RisItem> GetByIdAsync(Guid? id);
        ValueTask<RisItemEntryVM> GetVmByIdAsync(Guid? id);
        ValueTask<RisItemEntryVM> GetEntryVmByIdAsync(Guid? id);
        string PsNoDisplay(RisItemEntryVM model);
        string GetDescription(RisItemEntryVM entry);
        //string GetPsDescription(PsCardVM entry);

        ValueTask<RisItemEntryVM> CreateAsync(RisItemEntryVM model, string user, DateTime date);
        ValueTask<RisItemEntryVM> UpdateAsync(RisItemEntryVM model, string user, DateTime date);
        ValueTask<RisItemEntryVM> DeleteAsync(RisItemEntryVM model, string user, DateTime date);
    }
}
