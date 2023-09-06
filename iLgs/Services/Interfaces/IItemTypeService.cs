using iLgs.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services.Interfaces
{
    public interface IItemTypeService
    {
        IQueryable<ItemTypeVM> GetAll();
        Task<ItemType> GetByIdAsync(Guid id);

        Task<ItemTypeVM> CreateAsync(ItemTypeVM model, string user, DateTime date);
        Task<ItemTypeVM> UpdateAsync(ItemTypeVM model, string user, DateTime date);
        Task<ItemTypeVM> DeleteAsync(ItemTypeVM model, string user, DateTime date);
    }
}
