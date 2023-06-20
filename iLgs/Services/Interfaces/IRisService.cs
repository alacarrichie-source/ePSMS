using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLgs.Services.Interfaces
{
    public interface IRisService
    {
        IQueryable<RIS_VM> GetAll();
        Task<RISs> GetByIdAsync(Guid id);
        Task<RISs> GetByRisNoAsync(string risNo);
        Task<bool> GetAnyRisNoAsync(Guid risId, string risNo);
        Task<bool> IsPostedAsync(Guid risId);
        Task<bool> IsPrPostedAsync(Guid risId);

        Task<RIS_VM> CreateAsync(RIS_VM model, string user, DateTime date);
        Task<RIS_VM> UpdateAsync(RIS_VM model, string user, DateTime date);
        Task<RIS_VM> DeleteAsync(RIS_VM model, string user, DateTime date);

        Task PostAsync(Guid risId, string user, DateTime date);
        Task UnpostAsync(Guid risId, string user, DateTime date);
    }
}
