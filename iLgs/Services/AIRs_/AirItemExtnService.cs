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
        IQueryable<T> GetAirItemExtnByOrderItemRequestId<T>(Guid? orderItemRequestId) where T : AIRItemExtn;

        IAirItemExtnVehicleService AirItemExtnVehicle { get; }
        IAirItemExtnOtherService AirItemExtnOther { get; }
    }

    public class AirItemExtnService : IAirItemExtnService
    {
        private readonly AppManEntities _db;
        private readonly IItemCodeService _itemCodeService;
        private readonly IAirItemAbstractService _airItemSharedService;
        private readonly IAirItemExtnAbstractService _airItemExtnSharedService;

        private IAirItemExtnVehicleService _itemExtnVehicleService;
        private IAirItemExtnOtherService _itemExtnOtherService;

        public AirItemExtnService(AppManEntities db)
        {
            _db = db;
            _itemCodeService = new ItemCodeService(_db);
            _airItemSharedService = new AirItemAbstractService(_db);
            _airItemExtnSharedService = new AirItemExtnAbstractService(_db);
        }

        //public AirItemExtnService(AppManEntities db, IAirItemAbstractService airItemSharedService, IAirItemExtnAbstractService airItemExtnSharedService, IAirItemExtnVehicleService airItemExtnVehicleService,
        //    IAirItemExtnOtherService airItemExtnOtherService, IItemCodeService itemCodeService)
        //{
        //    _db = db;
        //    AirItemExtnVehicle = airItemExtnVehicleService;
        //    AirItemExtnOther = airItemExtnOtherService;
        //    _itemCodeService = itemCodeService;
        //    _airItemSharedService = airItemSharedService;
        //    _airItemExtnSharedService = airItemExtnSharedService;
        //}

        
        public IAirItemExtnVehicleService AirItemExtnVehicle { get { return _itemExtnVehicleService = _itemExtnVehicleService ?? new AirItemExtnVehicleService(_db); } }
        public IAirItemExtnOtherService AirItemExtnOther { get { return _itemExtnOtherService = _itemExtnOtherService ?? new AirItemExtnOtherService(_db); } }

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
            var data = _db.AIRItemExtns.Include(i => i.AIRItem.OrderItemRequest.OrderItem).OfType<T>().AsNoTracking()
                        .Where(w => w.AIRItemId == airItemId)
                        .AsQueryable();
            return data;
        }

        public IQueryable<T> GetAirItemExtnByOrderItemRequestId<T>(Guid? orderItemRequestId) where T : AIRItemExtn
        {
            var data = _db.AIRItemExtns.Include(i => i.AIRItem.OrderItemRequest.OrderItem).OfType<T>().AsNoTracking()
                        .Where(w => w.AIRItem.OrderItemRequestId == orderItemRequestId)
                        .AsQueryable();
            return data;
        }        
    }
}