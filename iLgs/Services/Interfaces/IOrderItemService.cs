//using iLgs.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Web;

//namespace iLgs.Services.Interfaces
//{
//    public interface IOrderItemService
//    {
//        IQueryable<OrderItemVM> GetByPoId(Guid? poId);
//        ValueTask<OrderItemVM> GetByIdAsync(Guid? id);
//        ValueTask<bool> GetAnyParItemsAsync(Guid id);
//        ValueTask<bool> GetAnyAirItemsAsync(Guid id);

//        ValueTask<OrderItemVM> CreateAsync(OrderItemVM model, string user, DateTime date);
//        ValueTask<OrderItemVM> UpdateAsync(OrderItemVM model, string user, DateTime date);
//        ValueTask<OrderItemVM> DeleteAsync(OrderItemVM model, string user, DateTime date);

//        //Task<string> StockNameAsync(Guid? orderItemId, string brand);
//    }
//}