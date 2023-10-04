using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLgs.Services.Interfaces
{    
    public interface IPsItemService
    {
        IQueryable<PsItemVM> GetAll();
        ValueTask<PsItem> GetByIdAsync(Guid psId);
        
        ValueTask<PsItemVM> CreateAsync(PsItemVM model, string user, DateTime date);
        ValueTask<PsItemVM> UpdateAsync(PsItemVM model, string user, DateTime date);
        ValueTask<PsItemVM> DeleteAsync(PsItemVM model, string user, DateTime date);

        //ValueTask Summarize()
    }
}
