using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLgs.Services
{
    public interface IOrderItemExtnService
    {
        IQueryable<OrderItemExtnVM> GetAll();
        IQueryable<OrderItemExtnVM> GetBatchInfo(Guid? orderItemId, string psType);
        IQueryable<OrderItemExtnVM> GetBatchInfo(string mode, Guid? requestItemId, Guid? orderItemId, string psType);
        Task UpdateBatchAsync(List<OrderItemExtnVM> orderExtnList, string user, DateTime date);
        Task SaveAsync(Guid orderItemId, List<OrderItemExtnVM> orderItemExtnList, string user, DateTime date);

        //Task<RequestItemExtnVM> CreateAsync(RequestItemExtnVM model, string user, DateTime date);
        //Task<RequestItemExtnVM> UpdateAsync(RequestItemExtnVM model, string user, DateTime date);
        //Task<RequestItemExtnVM> DeleteAsync(RequestItemExtnVM model, string user, DateTime date);
    }
}
