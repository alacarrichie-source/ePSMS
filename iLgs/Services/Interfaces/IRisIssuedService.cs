using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLgs.Services.Interfaces
{
    public interface IRisIssuedService
    {
        IQueryable<RisIssuedVM> GetByRisItemId(Guid? risItemId);
        ValueTask<RisIssued> GetByIdAsync(Guid? id);
        ValueTask<RisIssuedVM> GetVmByIdAsync(Guid? id);
        IQueryable<RisIssuedVM> GetByPoNoStockNo(string poNo, string stockNo);
        IQueryable<RisIssuedVM> GetByStockNo(string stockNo);

        ValueTask<RisIssuedVM> CreateAsync(RisIssuedVM model, string user, DateTime date);
        ValueTask<RisIssuedVM> UpdateAsync(RisIssuedVM model, string user, DateTime date);
        ValueTask<RisIssuedVM> DeleteAsync(RisIssuedVM model, string user, DateTime date);        
    }
}
