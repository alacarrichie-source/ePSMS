using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace iLgs.Services.AIRs
{
    public interface IAirItemExtnService
    {
        IQueryable<T> GetAirItemExtnByItemId<T>(Guid? airItemId) where T : AIRItemExtn;
        IQueryable<T> GetAirItemExtnByOrderItemId<T>(Guid? orderItemId) where T : AIRItemExtn;
        IAirItemExtnVehicleService AirItemExtnVehicle { get; }
        IAirItemExtnOtherService AirItemExtnOther { get; }
    }
    public class AirItemExtnService : IAirItemExtnService
    {
        private readonly AppManEntities _db;

        private IAirItemExtnVehicleService _airItemExtnVehicleService;
        private IAirItemExtnOtherService _airItemExtnOtherService;

        public AirItemExtnService(AppManEntities db)
        {
            _db = db;
            _airItemExtnVehicleService = new AirItemExtnVehicleService(_db);
            _airItemExtnOtherService = new AirItemExtnOtherService(_db);
        }

        public IAirItemExtnVehicleService AirItemExtnVehicle { get { return _airItemExtnVehicleService = _airItemExtnVehicleService ?? new AirItemExtnVehicleService(_db); } }
        public IAirItemExtnOtherService AirItemExtnOther { get { return _airItemExtnOtherService = _airItemExtnOtherService ?? new AirItemExtnOtherService(_db); } }

        public IQueryable<T> GetAirItemExtnByItemId<T>(Guid? airItemId) where T : AIRItemExtn
        {
            var data = _db.AIRItemExtns.OfType<T>().AsNoTracking()
                        .Where(w => w.AIRItemId == airItemId)
                        .AsQueryable();
            return data;
        }

        public IQueryable<T> GetAirItemExtnByOrderItemId<T>(Guid? orderItemId) where T : AIRItemExtn
        {
            var data = _db.AIRItemExtns.OfType<T>().AsNoTracking()
                        .Where(w => w.AIRItem.OrderItemId == orderItemId)
                        .AsQueryable();
            return data;
        }
    }
}