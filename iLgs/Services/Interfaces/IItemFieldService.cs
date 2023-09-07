using iLgs.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services.Interfaces
{
    public interface IItemFieldService
    {
        IQueryable<ItemFieldVM> GetAll();
        IQueryable<ItemFieldVM> GetAllbyItemTypeId(Guid? itemTypeId);
        Task<ItemField> GetByIdAsync(Guid id);

        Task<ItemFieldVM> CreateAsync(ItemFieldVM model, string user, DateTime date);
        Task<ItemFieldVM> UpdateAsync(ItemFieldVM model, string user, DateTime date);
        Task<ItemFieldVM> DeleteAsync(ItemFieldVM model, string user, DateTime date);
    }
}
