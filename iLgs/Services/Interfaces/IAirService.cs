using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLgs.Services.Interfaces
{
    public interface IAirService
    {
        IQueryable<AIR_VM> GetAll();
        ValueTask<AIR> GetByIdAsync(Guid id);
        ValueTask<AIR_VM> GetVmByIdAsync(Guid id);
        ValueTask<AIR> GetByAirNoAsync(string airNo);
        ValueTask<bool> GetAnyAirNoAsync(Guid airId, string airNo);
        ValueTask<bool> IsPostedAsync(Guid airId);


        ValueTask<AIR_VM> CreateAsync(AIR_VM model, string user, DateTime date);
        ValueTask<AIR_VM> UpdateAsync(AIR_VM model, string user, DateTime date);
        ValueTask<AIR_VM> DeleteAsync(AIR_VM model, string user, DateTime date);
        ValueTask<AIR_VM> SaveAsync(AIR_VM model, string user, DateTime date);

        ValueTask PostAsync(Guid airId, string user, DateTime date);
        ValueTask UnpostAsync(Guid airId, string user, DateTime date);
    }
}
