using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLgs.Services.Interfaces
{
    public interface IOrderService
    {
        IQueryable<OrderVM> GetAll();
        IQueryable<OrderVM> GetAllParOrders();
        ValueTask<Models.Order> GetByIdAsync(Guid orderId);
        ValueTask<Models.Order> GetByPoNoAsync(string poNo);
        ValueTask<bool> GetAnyPoNoAsync(Guid id, string poNo);
        ValueTask<bool> IsPostedAsync(Guid orderId);
        ValueTask<bool> GetAnyParsAsync(Guid id);
        ValueTask<bool> GetAnyAirsAsync(Guid id);

        ValueTask<int> GetNotPostedAsync(DateTime asOf);

        ValueTask<OrderVM> CreateAsync(OrderVM model, string user, DateTime date);
        ValueTask<OrderVM> UpdateAsync(OrderVM model, string user, DateTime date);
        ValueTask<OrderVM> DeleteAsync(OrderVM model, string user, DateTime date);
        ValueTask PostAsync(Guid orderId, string user, DateTime date);
        ValueTask UnpostAsync(Guid orderId, string user, DateTime date);
    }
}
