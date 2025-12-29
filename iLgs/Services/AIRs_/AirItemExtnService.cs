using iLgs.Models;
using iLgs.Services.Items;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services.AIRs_
{
    public interface IAirItemExtnService : IAirItemExtnAbstractService
    {
        IQueryable<T> GetAirItemExtnByItemId<T>(Guid? airItemId) where T : AIRItemExtn;
        IQueryable<T> GetAirItemExtnByOrderItemId<T>(Guid? orderItemId) where T : AIRItemExtn;
        IAirItemExtnVehicleService AirItemExtnVehicle { get; }
        IAirItemExtnOtherService AirItemExtnOther { get; }
    }

    public class AirItemExtnService : IAirItemExtnService
    {
        private readonly AppManEntities _db;
        private readonly IItemCodeService _itemCodeService;
        private readonly IAirItemAbstractService _airItemSharedService;
        private readonly IAirItemExtnAbstractService _airItemExtnSharedService;

        public AirItemExtnService(AppManEntities db, IAirItemAbstractService airItemSharedService, IAirItemExtnAbstractService airItemExtnSharedService, IAirItemExtnVehicleService airItemExtnVehicleService,
            IAirItemExtnOtherService airItemExtnOtherService, IItemCodeService itemCodeService)
        {
            _db = db;
            AirItemExtnVehicle = airItemExtnVehicleService;
            AirItemExtnOther = airItemExtnOtherService;
            _itemCodeService = itemCodeService;
            _airItemSharedService = airItemSharedService;
            _airItemExtnSharedService = airItemExtnSharedService;
        }

        public IAirItemExtnVehicleService AirItemExtnVehicle { get; }
        public IAirItemExtnOtherService AirItemExtnOther { get; }

        public async Task CreateAirItemExtnAsync(AIRItem airItem, OrderItem orderItem, string user, DateTime date)
        {
            await _airItemExtnSharedService.CreateAirItemExtnAsync(airItem, orderItem, user, date);
        }

        public void SetAirItmExtn(string itemExtnName, string setLotNo, int? setLotQtyNo, int? contentNo, int? tContentNo, AIRItem airItem, string user, DateTime date)
        {
            _airItemExtnSharedService.SetAirItmExtn(itemExtnName, setLotNo, setLotQtyNo, contentNo, tContentNo, airItem, user, date);
        }

        public IQueryable<T> GetAirItemExtnByItemId<T>(Guid? airItemId) where T : AIRItemExtn
        {
            var data = _db.AIRItemExtns.Include(i => i.AIRItem.OrderItem).OfType<T>().AsNoTracking()
                        .Where(w => w.AIRItemId == airItemId)
                        .AsQueryable();
            return data;
        }

        public IQueryable<T> GetAirItemExtnByOrderItemId<T>(Guid? orderItemId) where T : AIRItemExtn
        {
            var data = _db.AIRItemExtns.Include(i => i.AIRItem.OrderItem).OfType<T>().AsNoTracking()
                        .Where(w => w.AIRItem.OrderItemId == orderItemId)
                        .AsQueryable();
            return data;
        }        
    }
}