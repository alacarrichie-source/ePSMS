using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.PurchaseOrder
{
    public interface IOrderItemUnitGroupDescriptionItemSharedService
    {
        ValueTask<OrderItemUnitGroupDescriptionItem> DeleteEmptyGroupsAsync(Guid? orderItemId);
    }

    public class OrderItemUnitGroupDescriptionItemSharedService : IOrderItemUnitGroupDescriptionItemSharedService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<OrderItemUnitGroupDescriptionItem> _exceptionService;

        public OrderItemUnitGroupDescriptionItemSharedService(AppManEntities db,
            IExceptionService<OrderItemUnitGroupDescriptionItem> exceptionService)
        {
            _db = db;
            _exceptionService = exceptionService;
        }

        public ValueTask<OrderItemUnitGroupDescriptionItem> DeleteEmptyGroupsAsync(Guid? orderItemId) =>
        _exceptionService.TryCatch(async () =>
        {
            var unitGroupDescriptionItems = _db.OrderItemUnitGroupDescriptionItems.Where(w => w.OrderItemId == orderItemId);
            if (unitGroupDescriptionItems.Any())
            {
                var unitGroupDescriptionId = unitGroupDescriptionItems.FirstOrDefault().OrderItemUnitGroupDescriptionId;

                _db.OrderItemUnitGroupDescriptionItems.RemoveRange(unitGroupDescriptionItems);
                await _db.SaveChangesAsync();

                var unitGroupDescriptions = _db.OrderItemUnitGroupDescriptions
                    .Where(w => w.Id == unitGroupDescriptionId && !w.OrderItemUnitGroupDescriptionItems.Any());
                if (unitGroupDescriptions.Any())
                {
                    var uniGroupId = unitGroupDescriptions.FirstOrDefault().OrderItemUnitGroupId;

                    _db.OrderItemUnitGroupDescriptions.RemoveRange(unitGroupDescriptions);
                    await _db.SaveChangesAsync();

                    var unitGroups = _db.OrderItemUnitGroups.Where(w => w.Id == uniGroupId && !w.OrderItemUnitGroupDescriptions.Any());
                    if (unitGroups.Any())
                    {
                        _db.OrderItemUnitGroups.RemoveRange(unitGroups);
                        await _db.SaveChangesAsync();
                    }
                }
                return unitGroupDescriptionItems.FirstOrDefault();
            }
            return new OrderItemUnitGroupDescriptionItem();
        });
    }
}