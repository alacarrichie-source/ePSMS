//using iLgs.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace iLgs.Services.Interfaces
//{
//    public interface IParItemService
//    {        
//        IQueryable<PARItemVM> GetAll(Guid? parId);
//        Task<PARItemVM> GetVmByIdAsync(Guid? itemId);
//        Task<Models.PARItem> GetByIdAsync(Guid? itemId);

//        Task<PARItemVM> CreateAsync(PARItemVM model, string user, DateTime date);
//        Task<PARItemVM> UpdateAsync(PARItemVM model, string user, DateTime date);
//        Task<PARItemVM> DeleteAsync(PARItemVM model, string user, DateTime date);        
//    }
//}
