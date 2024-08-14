using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Services
{
    public interface IAirItemExtnService
    {
        IAirItemExtnVehicleService AirItemExtnVehicle { get; }
        IAirItemExtnOtherService AirItemExtnOther { get; }
    }
    public class AirItemExtnService : IAirItemExtnService
    {
        private readonly AppManEntities _db = new AppManEntities();

        private IAirItemExtnVehicleService _airItemExtnVehicleService;
        private IAirItemExtnOtherService _airItemExtnOtherService;

        public AirItemExtnService(AppManEntities db)
        {
            _db = db;
            _airItemExtnVehicleService = new AirItemExtnVehicleService(db);
            _airItemExtnOtherService = new AirItemExtnOtherService(db);
        }

        public IAirItemExtnVehicleService AirItemExtnVehicle { get { return _airItemExtnVehicleService = _airItemExtnVehicleService ?? new AirItemExtnVehicleService(_db); } }
        public IAirItemExtnOtherService AirItemExtnOther { get { return _airItemExtnOtherService = _airItemExtnOtherService ?? new AirItemExtnOtherService(_db); } }

    }
}