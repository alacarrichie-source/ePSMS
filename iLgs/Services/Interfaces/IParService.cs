using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLgs.Services.Interfaces
{
    public interface IParService
    {
        IQueryable<PAR_VM> GetAllPars();
        Task<Models.PAR> GetParByIdAsync(Guid parId);
        Task<Models.PAR> GetParByParNoAsync(string parNo);
        Task<bool> IsAnyParNoAsync(Guid id, string parNo);
        Task<bool> IsPostedAsync(Guid parId);
        

        Task<PAR_VM> CreateParAsync(PAR_VM model, string user, DateTime date);
        Task<PAR_VM> UpdateParAsync(PAR_VM model, string user, DateTime date);
        Task<PAR_VM> DeleteParAsync(PAR_VM model, string user, DateTime date);
        Task PostAsync(Guid parId, string user, DateTime date);
        Task UnpostAsync(Guid parId, string user, DateTime date);

        #region PAR ITEMS
        IQueryable<PARItemVM> GetAllParItems();
        Task<Models.PARItem> GetParItemByIdAsync(Guid itemId);

        Task<PARItemVM> CreateParItemAsync(PARItemVM model, string user, DateTime date);
        Task<PARItemVM> UpdateParItemAsync(PARItemVM model, string user, DateTime date);
        Task<PARItemVM> DeletePaItemrAsync(PARItemVM model, string user, DateTime date);
        #endregion  
    }
}
