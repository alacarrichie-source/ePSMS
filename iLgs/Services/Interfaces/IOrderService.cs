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
        Task<Models.Order> GetById(Guid orderId);
        Models.Order GetByPoNo(string poNo);
        bool GetAnyPoNo(Guid id, string poNo);

        Task<OrderVM> Create(OrderVM model, string user, DateTime date);
        Task<OrderVM> Update(OrderVM model, string user, DateTime date);
        Task<OrderVM> Delete(OrderVM model, string user, DateTime date);
        Task Post(Guid orderId, string user, DateTime date);
    }
}
