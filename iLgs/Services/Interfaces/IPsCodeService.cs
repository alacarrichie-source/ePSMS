using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLgs.Services.Interfaces
{
    public interface IPsCodeService
    {
        IQueryable<PsCode> GetAll();
        IQueryable<PsCodeVM> GetMaintenanceView();
        Task<PsCode> GetByIdAsync(Guid psId);
        Task<PsCode> GetByPsNoAsync(string psNo);
        Task<bool> GetAnyPsNoAsync(Guid id, string psNo);
        
        Task<PsCode> CreateAsync(PsCode model, string user, DateTime date);
        Task<PsCode> UpdateAsync(PsCode model, string user, DateTime date);
        Task<PsCode> DeleteAsync(PsCode model, string user, DateTime date);        
    }
}
