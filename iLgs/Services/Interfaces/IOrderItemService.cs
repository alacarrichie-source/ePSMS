using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.Interfaces
{
    public interface IOrderItemService
    {
        IQueryable<OrderItemVM> GetByPoId(Guid? poId);
        Task<OrderItemVM> GetByIdAsync(Guid? id);
        Task<bool> GetAnyParItemsAsync(Guid id);
        Task<bool> GetAnyAirItemsAsync(Guid id);

        Task<OrderItemVM> CreateAsync(OrderItemVM model, string user, DateTime date);
        Task<OrderItemVM> UpdateAsync(OrderItemVM model, string user, DateTime date);
        Task<OrderItemVM> DeleteAsync(OrderItemVM model, string user, DateTime date);

        //Task<string> StockNameAsync(Guid? orderItemId, string brand);
    }
}