using iLgs.Exceptions;
using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.PurchaseOrder
{
    public interface IOrderSharedService
    {
        bool IsPosted(Guid orderId);
        bool IsPosted(Order order);
        bool IsPosted(OrderItem orderItem);
        //bool IsPosted(OrderItemUnitGroup orderItemUnitGroup);
        //bool IsPosted(OrderItemUnitGroupDescription orderItemunitGroupDescription);
        //bool IsPosted(OrderItemUnitGroupDescriptionItem orderItemunitGroupDescriptionItem);
        Task<bool> IsPostedAsync(Guid orderId);
        Task<bool> GetAnyAirsAsync(Guid id);
        Task<bool> GetAnyParsAsync(Guid id);
        Task ValidateStatusAsync(Guid orderId);
    }

    public class OrderSharedService : IOrderSharedService
    {
        private readonly AppManEntities _db;

        public OrderSharedService(AppManEntities db)
        {
            _db = db;
        }

        public bool IsPosted(Guid orderId)
        {
            var entity = _db.Orders.Find(orderId);
            return entity != null && (entity.PostedDt.HasValue || !string.IsNullOrWhiteSpace(entity.PostedBy));
        }

        public bool IsPosted(Order order)
        {
            return IsPosted(order.Id);
        }

        public bool IsPosted(OrderItem orderItem)
        {
            var orderId = (Guid)orderItem.OrderId;
            return IsPosted(orderId);
        }

        //public bool IsPosted(OrderItemUnitGroup orderItemUnitGroup)
        //{
        //    var orderId = (Guid)orderItemUnitGroup.OrderId;
        //    return IsPosted(orderId);
        //}

        //public bool IsPosted(OrderItemUnitGroupDescription orderItemUnitGroupDescription)
        //{
        //    var orderId = (Guid)_db.OrderItemUnitGroupDescriptions
        //        .Include(i => i.OrderItemUnitGroup)
        //        .Where(w => w.OrderItemUnitGroupId == orderItemUnitGroupDescription.OrderItemUnitGroupId)
        //        .AsNoTracking()
        //        .FirstOrDefault()?.OrderItemUnitGroup.OrderId;
        //    return IsPosted(orderId);
        //}

        //public bool IsPosted(OrderItemUnitGroupDescriptionItem orderItemUnitGroupDescriptionItem)
        //{
        //    var orderId = (Guid)_db.OrderItemUnitGroupDescriptionItems
        //        .Include(i => i.OrderItemUnitGroupDescription.OrderItemUnitGroup)
        //        .Where(w => w.OrderItemUnitGroupDescriptionId == orderItemUnitGroupDescriptionItem.OrderItemUnitGroupDescriptionId)
        //        .AsNoTracking()
        //        .FirstOrDefault()?.OrderItemUnitGroupDescription.OrderItemUnitGroup.OrderId;
        //    return IsPosted(orderId);
        //}

        public async Task<bool> IsPostedAsync(Guid orderId)
        {
            var entity = await _db.Orders.FindAsync(orderId);
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }

        public async Task ValidateStatusAsync(Guid orderId)
        {
            try { await new PurchaseOrderLifecycleService(_db).EnsureEditableAsync(orderId); }
            catch (InvalidOperationException ex) { throw new RecordRelationshipException(ex.Message); }
        }

        public async Task<bool> GetAnyAirsAsync(Guid id)
        {
            return await _db.AIRs.AnyAsync(a => a.OrderId == id);
        }

        public async Task<bool> GetAnyParsAsync(Guid id)
        {
            return await _db.PARs.AnyAsync(a => a.OrderId == id);
        }
    }
}