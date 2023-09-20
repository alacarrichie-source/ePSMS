using iLgs.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services.Interfaces
{
    public interface IRisService 
    {
        IQueryable<RIS_VM> GetAll();
        ValueTask<RISs> GetByIdAsync(Guid id);
        ValueTask<RISs> GetByRisNoAsync(string risNo);
        ValueTask<RISs> GetByOrderIdAsync(Guid orderId);
        ValueTask<bool> GetAnyRisNoAsync(Guid risId, string risNo);
        ValueTask<bool> IsPostedAsync(Guid risId);
        ValueTask<bool> IsPrPostedAsync(Guid risId);
        ValueTask<bool> IsWithPrAsync(Guid risId);        


        ValueTask<RIS_VM> CreateAsync(RIS_VM model, string user, DateTime date);
        ValueTask<RIS_VM> UpdateAsync(RIS_VM model, string user, DateTime date);
        ValueTask<RIS_VM> DeleteAsync(RIS_VM model, string user, DateTime date);

        ValueTask PostAsync(Guid risId, string user, DateTime date);
        ValueTask UnpostAsync(Guid risId, string user, DateTime date);
    }
}
