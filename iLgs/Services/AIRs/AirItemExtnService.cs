using iLgs.Models;
using iLgs.Services.Items;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.AIRs
{
    public interface IAirItemExtnService
    {
        IQueryable<T> GetAirItemExtnByItemId<T>(Guid? airItemId) where T : AIRItemExtn;
        IQueryable<T> GetAirItemExtnByOrderItemId<T>(Guid? orderItemId) where T : AIRItemExtn;
        IAirItemExtnVehicleService AirItemExtnVehicle { get; }
        IAirItemExtnOtherService AirItemExtnOther { get; }
        Task CreateAirItemExtnAsync(AIRItem airItem, OrderItem orderItem, string user, DateTime date);
    }
    public class AirItemExtnService : IAirItemExtnService
    {
        private readonly AppManEntities _db;

        private IAirItemExtnVehicleService _airItemExtnVehicleService;
        private IAirItemExtnOtherService _airItemExtnOtherService;
        private IItemCodeService _itemCodeService;
        private readonly IAirItemService _airItemService;

        public AirItemExtnService(AppManEntities db, AirItemService airItemService)
        {
            _db = db;
            _airItemExtnVehicleService = new AirItemExtnVehicleService(_db);
            _airItemExtnOtherService = new AirItemExtnOtherService(_db, this);
            _itemCodeService = new ItemCodeService(_db);
            _airItemService = airItemService;
        }

        public IAirItemExtnVehicleService AirItemExtnVehicle { get { return _airItemExtnVehicleService = _airItemExtnVehicleService ?? new AirItemExtnVehicleService(_db); } }
        public IAirItemExtnOtherService AirItemExtnOther { get { return _airItemExtnOtherService = _airItemExtnOtherService ?? new AirItemExtnOtherService(_db, this); } }

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

        public async Task CreateAirItemExtnAsync(AIRItem airItem, OrderItem orderItem, string user, DateTime date)
        {
            if (airItem.InvDist != "I")
            {
                return;
            }

            var isWithParIcs = _itemCodeService.IsWithParIcs(orderItem.ItemCodeId);
            if (isWithParIcs != true)
            {
                return;
            }

            var unitGroupDescriptionItem = await _db.OrderItemUnitGroupDescriptionItems
                       .Include(i => i.OrderItemUnitGroupDescription.OrderItemUnitGroup)
                       .Where(w => w.OrderItemId == orderItem.Id)
                       .FirstOrDefaultAsync();
            var qty = (int?)orderItem.Qty;
            string category = orderItem.RequestItem.RisItem.ItemCode.ItemType.Code;
            string itemExtnName = _airItemService.GetItemExtnNameByCategory(category);

            // create template based on number of qty
            if (unitGroupDescriptionItem != null)
            {
                var setLotNo = unitGroupDescriptionItem.OrderItemUnitGroupDescription.OrderItemUnitGroup.SetLotNo;
                var groupQty = unitGroupDescriptionItem.OrderItemUnitGroupDescription.OrderItemUnitGroup.Qty;
                var tQty = 0; // qty * groupQty;
                for (int gQty = 1; gQty <= groupQty; gQty++)
                {                    
                    for (int q = 1; q <= qty; q++)
                    {
                        tQty++;
                        var airItemExtns = _db.AIRItemExtns.Where(w => w.AIRItemId == airItem.Id && w.SetLotNo == setLotNo && w.SetLotQtyNo == gQty && w.ContentNo == q);
                        if (!airItemExtns.Any())
                        {
                            //SetAirItmExtn(itemExtnName, setLotNo, gQty, q, qty, airItem, user, date);
                            SetAirItmExtn(itemExtnName, setLotNo, gQty, tQty, qty, airItem, user, date);
                        }
                    }
                }
                //qty = qty * groupQty;
                //for (int q = 1; q <= qty; q++)
                //{
                //    for (int gQty = 1; gQty <= groupQty; gQty++)
                //    {

                //        var airItemExtns = _db.AIRItemExtns.Where(w => w.AIRItemId == airItem.Id && w.SetLotNo == setLotNo && w.SetLotQtyNo == gQty && w.ContentNo == q);
                //        if (!airItemExtns.Any())
                //        {
                //            SetAirItmExtn(itemExtnName, setLotNo, gQty, q, qty, airItem, user, date);
                //        }
                //    }
                //}
            }
            else
            {
                for (int q = 1; q <= qty; q++)
                {
                    var airItemExtns = _db.AIRItemExtns
                        .Where(w => w.AIRItemId == airItem.Id
                            && (w.SetLotNo == "" || w.SetLotNo == null)
                            && w.SetLotQtyNo == null
                            && w.ContentNo == q);
                    if (!airItemExtns.Any())
                    {
                        SetAirItmExtn(itemExtnName, "", null, q, qty, airItem, user, date);
                    }
                }
            }
        }

        private void SetAirItmExtn(string itemExtnName, string setLotNo, int? setLotQtyNo, int? contentNo, int? tContentNo, AIRItem airItem, string user, DateTime date)
        {
            if (itemExtnName == "ItemExtnLand")
            {
                // To do: Add Land process here
            }
            else if (itemExtnName == "ItemExtnVehicle")
            {
                var airItemExtnVehicle = new AIRItemExtnVehicle()
                {
                    Id = Guid.NewGuid(),
                    AIRItemId = airItem.Id,
                    SetLotNo = setLotNo,
                    SetLotQtyNo = setLotQtyNo,
                    ContentNo = contentNo,
                    TContentNo = tContentNo,
                    ConductionNo = "",
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };

                airItem.AIRItemExtns.Add(airItemExtnVehicle);
            }
            else if (itemExtnName == "ItemExtnOther")
            {
                var airItemExtnOther = new AIRItemExtnOther()
                {
                    Id = Guid.NewGuid(),
                    AIRItemId = airItem.Id,
                    SetLotNo = setLotNo,
                    SetLotQtyNo = setLotQtyNo,
                    ContentNo = contentNo,
                    TContentNo = tContentNo,
                    SerialNo = "",
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };

                airItem.AIRItemExtns.Add(airItemExtnOther);
            }
        }
    }
}