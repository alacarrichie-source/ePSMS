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
        ValueTask<ItemCode> GetByIdAsync(Guid id);
        IQueryable<ItemCodeVM> GetItems(string item);
        IQueryable<ItemCodeVM> GetItemsByCategory(string category, string item);
        IQueryable<ItemCodeVM> GetItemsByTypeCode(string typeCode, string item);

        ValueTask<ItemCodeVM> CreateAsync(ItemCodeVM model, string user, DateTime date);
        ValueTask<ItemCodeVM> UpdateAsync(ItemCodeVM model, string user, DateTime date);
        ValueTask<ItemCodeVM> DeleteAsync(ItemCodeVM model, string user, DateTime date);
    }
}
