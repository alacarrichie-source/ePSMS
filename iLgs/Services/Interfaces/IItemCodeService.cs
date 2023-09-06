using iLgs.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services.Interfaces
{
    public interface IItemCodeService
    {
        IQueryable<ItemCodeVM> GetAll();
        IQueryable<ItemCodeVM> GetAllByItemTypeId(Guid? itemTypeId);
        Task<ItemCode> GetByIdAsync(Guid id);        

        Task<ItemCodeVM> CreateAsync(ItemCodeVM model, string user, DateTime date);
        Task<ItemCodeVM> UpdateAsync(ItemCodeVM model, string user, DateTime date);
        Task<ItemCodeVM> DeleteAsync(ItemCodeVM model, string user, DateTime date);
    }
}
