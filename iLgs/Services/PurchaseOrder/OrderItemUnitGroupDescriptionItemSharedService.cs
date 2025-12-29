using iLgs.Models;
using iLgs.Utilities;
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
        private readonly IAppManEntitiesFactory _contextFactory;
        private readonly IExceptionService<OrderItemUnitGroupDescriptionItem> _exceptionService;

        public OrderItemUnitGroupDescriptionItemSharedService(AppManEntities db,
            IAppManEntitiesFactory appManEntitiesFactory,
            IExceptionService<OrderItemUnitGroupDescriptionItem> exceptionService)
        {
            _db = db;
            _contextFactory = appManEntitiesFactory;
            _exceptionService = exceptionService;
        }

        public ValueTask<OrderItemUnitGroupDescriptionItem> DeleteEmptyGroupsAsync(Guid? orderItemId) =>
        _exceptionService.TryCatch(async () =>
        {
            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var unitGroupDescriptionItems = ctx.OrderItemUnitGroupDescriptionItems.Where(w => w.OrderItemId == orderItemId);
                if (unitGroupDescriptionItems.Any())
                {
                    var unitGroupDescriptionId = unitGroupDescriptionItems.FirstOrDefault().OrderItemUnitGroupDescriptionId;

                    ctx.OrderItemUnitGroupDescriptionItems.RemoveRange(unitGroupDescriptionItems);
                    await ctx.SaveChangesAsync();

                    var unitGroupDescriptions = ctx.OrderItemUnitGroupDescriptions
                        .Where(w => w.Id == unitGroupDescriptionId && !w.OrderItemUnitGroupDescriptionItems.Any());
                    if (unitGroupDescriptions.Any())
                    {
                        var uniGroupId = unitGroupDescriptions.FirstOrDefault().OrderItemUnitGroupId;

                        ctx.OrderItemUnitGroupDescriptions.RemoveRange(unitGroupDescriptions);
                        await ctx.SaveChangesAsync();

                        var unitGroups = ctx.OrderItemUnitGroups.Where(w => w.Id == uniGroupId && !w.OrderItemUnitGroupDescriptions.Any());
                        if (unitGroups.Any())
                        {
                            ctx.OrderItemUnitGroups.RemoveRange(unitGroups);
                            await ctx.SaveChangesAsync();
                        }
                    }
                    return unitGroupDescriptionItems.FirstOrDefault();
                }
            }
            return new OrderItemUnitGroupDescriptionItem();
        });
    }
}