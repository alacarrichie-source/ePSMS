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
        IQueryable<PsCodeVM> GetAllItems();
        IQueryable<PsCodeVM> GetStockItems();
        IQueryable<PsCodeVM> GetPropertyItems();
        ValueTask<PsCode> GetByIdAsync(Guid psId);
        ValueTask<PsCode> GetByPsNoAsync(string psNo);
        ValueTask<bool> GetAnyPsNoAsync(Guid id, string psNo);
        string PsNo(string itemCode, string itemName);

        ValueTask<PsCodeVM> CreateAsync(PsCodeVM model, string user, DateTime date);
        ValueTask<PsCodeVM> UpdateAsync(PsCodeVM model, string user, DateTime date);
        ValueTask<PsCodeVM> DeleteAsync(PsCodeVM model, string user, DateTime date);        
    }
}
