using iLgs.Models;
using iLgs.Services.Items;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services.AIRs_
{
    public interface IAirItemExtnAbstractService
    {
        Task CreateAirItemExtnAsync(AIRItem airItem, OrderItem orderItem, string user, DateTime date);
        void SetAirItmExtn(string itemExtnName, string setLotNo, int? setLotQtyNo, int? contentNo, int? tContentNo, AIRItem airItem, string user, DateTime date);
    }

    public class AirItemExtnAbstractService : IAirItemExtnAbstractService
    {
        private readonly AppManEntities _db;
        private readonly IAirItemAbstractService _airItemSharedService;
        private readonly IItemCodeService _itemCodeService;

        public AirItemExtnAbstractService(AppManEntities db)
        {
            _db = db;
            _airItemSharedService = new AirItemAbstractService(_db);
            _itemCodeService = new ItemCodeService(_db);
        }

        //public AirItemExtnAbstractService(AppManEntities db, IAirItemAbstractService airItemSharedService, IItemCodeService itemCodeService)
        //{
        //    _db = db;
        //    _airItemSharedService = airItemSharedService;
        //    _itemCodeService = itemCodeService;
        //}

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
            //var qty = (int?)orderItem.Qty;
            var qty = (int?)airItem.Qty;
            string category = orderItem.ItemCode.ItemType.Code;
            string itemExtnName = _airItemSharedService.GetItemExtnNameByCategory(category);

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
                            SetAirItmExtn(itemExtnName, setLotNo, gQty, tQty, qty, airItem, user, date);
                        }
                    }
                }
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

                // remove excess if any. This happens when the user edit the previous Qty. Ex, from 50 down to 10. Items 11 to 50 will be deleted.
                var excessItems = _db.AIRItemExtns
                        .Where(w => w.AIRItemId == airItem.Id
                            && (w.SetLotNo == "" || w.SetLotNo == null)
                            && w.SetLotQtyNo == null
                            && w.ContentNo > qty);
                if (excessItems.Any())
                {
                    _db.AIRItemExtns.RemoveRange(excessItems);
                }
            }
        }

        public void SetAirItmExtn(string itemExtnName, string setLotNo, int? setLotQtyNo, int? contentNo, int? tContentNo, AIRItem airItem, string user, DateTime date)
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