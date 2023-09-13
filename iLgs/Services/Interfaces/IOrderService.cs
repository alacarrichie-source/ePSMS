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
        Task<Models.Order> GetByIdAsync(Guid orderId);
        Task<Models.Order> GetByPoNoAsync(string poNo);
        Task<bool> GetAnyPoNoAsync(Guid id, string poNo);        
        Task<bool> IsPostedAsync(Guid orderId);
        Task<bool> GetAnyParsAsync(Guid id);
        Task<bool> GetAnyAirsAsync(Guid id);
    
        Task<OrderVM> CreateAsync(OrderVM model, string user, DateTime date);
        Task<OrderVM> UpdateAsync(OrderVM model, string user, DateTime date);
        Task<OrderVM> DeleteAsync(OrderVM model, string user, DateTime date);
        Task PostAsync(Guid orderId, string user, DateTime date);
        Task UnpostAsync(Guid orderId, string user, DateTime date);
    }
}
