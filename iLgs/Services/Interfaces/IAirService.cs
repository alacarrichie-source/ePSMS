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
        Task<AIR> GetByIdAsync(Guid id);
        Task<AIR> GetByAirNoAsync(string airNo);
        Task<bool> GetAnyAirNoAsync(Guid airId, string airNo);
        Task<bool> IsPostedAsync(Guid airId);
        

        Task<AIR_VM> CreateAsync(AIR_VM model, string user, DateTime date);
        Task<AIR_VM> UpdateAsync(AIR_VM model, string user, DateTime date);
        Task<AIR_VM> DeleteAsync(AIR_VM model, string user, DateTime date);

        Task PostAsync(Guid airId, string user, DateTime date);
        Task UnpostAsync(Guid airId, string user, DateTime date);
    }
}
