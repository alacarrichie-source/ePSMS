using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLgs.Services.Interfaces
{
    public interface IAirInvoiceService
    {
        IQueryable<AIRInvoiceVM> GetVmByAirId(Guid? airId);
        ValueTask<AIRInvoiceVM> GetVmByIdAsync(Guid? id);

        ValueTask<AIRInvoiceVM> CreateAsync(AIRInvoiceVM model, string user, DateTime date);
        ValueTask<AIRInvoiceVM> UpdateAsync(AIRInvoiceVM model, string user, DateTime date);
        ValueTask<AIRInvoiceVM> DeleteAsync(AIRInvoiceVM model, string user, DateTime date);
    }
}
