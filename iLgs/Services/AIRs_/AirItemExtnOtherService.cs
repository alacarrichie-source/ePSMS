using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Items;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services.AIRs_
{
    public interface IAirItemExtnOtherService
    {
        IQueryable<AIRItemExtnOther> GetByAirItemId(Guid? airItemId);
        ValueTask<AIRItemExtnOther> GetByIdAsync(Guid? id);

        ValueTask<AIRItemExtnOther> CreateAsync(AIRItemExtnOther model, string user, DateTime date);
        ValueTask<AIRItemExtnOther> UpdateAsync(AIRItemExtnOther model, string user, DateTime date);
        ValueTask<AIRItemExtnOther> DeleteAsync(AIRItemExtnOther model, string user, DateTime date);

        ValueTask<AIRItemExtnOther> GenerateSerialAsync(Guid airItemId, string user, DateTime date);
        ValueTask<AIRItemExtnOther> GenerateSerialItemExtnAsync(Guid airItemExtnId, string user, DateTime date);
    }

    public class AirItemExtnOtherService : IAirItemExtnOtherService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<AIRItemExtnOther> _exceptionService;
        private readonly IAirItemExtnAbstractService _airItemExtnSharedService;
        private readonly IItemCodeService _itemCodeService;

        public AirItemExtnOtherService(AppManEntities db, 
            IExceptionService<AIRItemExtnOther> exceptionService,
            IAirItemExtnAbstractService airItemExtnSharedService,
            IItemCodeService itemCodeService)
        {
            _db = db;
            _exceptionService = exceptionService;
            _airItemExtnSharedService = airItemExtnSharedService;
            _itemCodeService = itemCodeService;
        }

        public IQueryable<AIRItemExtnOther> GetByAirItemId(Guid? airItemId)
        {
            var data = _db.AIRItemExtns.OfType<AIRItemExtnOther>().Where(w => w.AIRItemId == airItemId);
            return data;
        }

        public ValueTask<AIRItemExtnOther> GetByIdAsync(Guid? id) => _exceptionService.TryCatch(async () =>
        {
            var data = await _db.AIRItemExtns.OfType<AIRItemExtnOther>().Where(w => w.Id == id).FirstOrDefaultAsync();
            return data;
        });

        public ValueTask<AIRItemExtnOther> CreateAsync(AIRItemExtnOther model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            if (await IsPostedAsync(model.AIRItemId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            }

            var airItem = _db.AIRItems.FirstOrDefault(f => f.Id == model.AIRItemId);
            var orderItemUnitGroup = _db.OrderItemUnitGroups.Where(w => w.OrderItemUnitGroupDescriptions.Any(a => a.OrderItemUnitGroupDescriptionItems.Any(b => b.OrderItemId == airItem.OrderItemId))).FirstOrDefault();
            var groupQty = orderItemUnitGroup == null ? 1 : orderItemUnitGroup.Qty;
            var airItemQty = (int)_db.AIRItems.FirstOrDefault(f => f.Id == model.AIRItemId).Qty;
            var airItemExtnCount = _db.AIRItemExtns.OfType<AIRItemExtnOther>().Where(w => w.AIRItemId == model.AIRItemId).Count();
            var totalQty = airItemQty * groupQty;

            if (airItemExtnCount == totalQty)
            {
                throw new InvalidValueException($"Cannot create more than {totalQty} record(s).");
            }

            if (!string.IsNullOrWhiteSpace(model.SerialNo))
            {
                if (await _db.AIRItemExtns.OfType<AIRItemExtnOther>().AnyAsync(f => f.AIRItemId == model.AIRItemId && f.SerialNo == model.SerialNo))
                {
                    throw new RecordAlreadyExistsException("Serial No. already exists!");
                }
            }

            var contentNo = _db.AIRItemExtns.Where(w => w.AIRItemId == model.AIRItemId).Max(m => m.ContentNo) ?? 0;
            model.TContentNo = airItemQty;
            model.ContentNo = contentNo + 1;
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            model.Id = Guid.NewGuid();

            var entity = new AIRItemExtnOther()
            {
                Id = model.Id,
                AIRItemId = model.AIRItemId,
                ContentNo = model.ContentNo,
                TContentNo = model.TContentNo,
                CustItemNo = model.CustItemNo,
                IsAutoGen = model.IsAutoGen,
                SerialNo = model.SerialNo,
                Condition = model.Condition,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.AIRItemExtns.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<AIRItemExtnOther> DeleteAsync(AIRItemExtnOther model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            if (await IsPostedAsync(model.AIRItemId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot delete!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.AIRItemExtns.OfType<AIRItemExtnOther>().FirstOrDefaultAsync(f => f.Id == model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.AIRItemExtns.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.AIRItemExtns.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<AIRItemExtnOther> UpdateAsync(AIRItemExtnOther model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            if (await IsPostedAsync(model.AIRItemId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            }

            if (!string.IsNullOrWhiteSpace(model.SerialNo))
            {
                if (await _db.AIRItemExtns.OfType<AIRItemExtnOther>().AnyAsync(f => f.AIRItemId == model.AIRItemId && f.SerialNo == model.SerialNo && f.Id != model.Id))
                {
                    throw new RecordAlreadyExistsException("Serial No.", "Serial No. already exists!");
                }
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.AIRItemExtns.OfType<AIRItemExtnOther>().FirstOrDefaultAsync(f => f.Id == model.Id);

            entity.ContentNo = model.ContentNo;
            entity.CustItemNo = model.CustItemNo;            
            entity.IsAutoGen = model.IsAutoGen;
            entity.SerialNo = model.SerialNo;
            entity.Condition = model.Condition;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.AIRItemExtns.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<AIRItemExtnOther> GenerateSerialAsync(Guid airItemId, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            if (await IsPostedAsync(airItemId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            }
            
            var airItem = await _db.AIRItems.FirstOrDefaultAsync(f => f.Id == airItemId);

            if (airItem.InvDist != "I")
            {
                throw new InvalidValueException("Item is not For Inventory, cannot proceed.");
            }            

            var orderItem = await _db.OrderItems
                .Include(i => i.Order.OrderItemUnitGroups)
                .Include(i => i.RequestItem.RisItem.ItemCode.ItemType)
                .Where(w => w.Id == airItem.OrderItemId).FirstOrDefaultAsync();

            var isWithParIcs = _itemCodeService.IsWithParIcs(orderItem.ItemCodeId);
            if (isWithParIcs != true)
            {
                throw new InvalidValueException("Item is not For PAR/ICS, cannot proceed.");
            }

            // complete the serial number template here, new airitemExtn is injected inside airItem
            await _airItemExtnSharedService.CreateAirItemExtnAsync(airItem, orderItem, user, date);
            _db.AIRItems.Attach(airItem);
            _db.Entry(airItem).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            //var airItemExtns = await _db.AIRItemExtns
            //    .Include(i => i.AIRItem.OrderItem.Order)
            //    .OfType<AIRItemExtnOther>().Where(w => w.AIRItemId == airItemId && (w.SerialNo == "" || w.SerialNo == null)).ToListAsync();

            //if (airItemExtns.Count() == 0)
            //{
            //    throw new NotFoundException("No items found without a serial number.");
            //}

            //foreach (var airItemExtn in airItemExtns)
            //{
            //    var serialNo = airItemExtn.AIRItem.OrderItem.Order.PoNo.Trim() + "-" + airItemExtn.AIRItem.OrderItem.PsNo.Trim() + "-" +
            //        (string.IsNullOrWhiteSpace(airItemExtn.SetLotNo) ? "0" : airItemExtn.SetLotNo.Trim()) + "-" +
            //        (!airItemExtn.SetLotQtyNo.HasValue ? "0" : airItemExtn.SetLotQtyNo.ToString().Trim()) + "-" + airItemExtn.ContentNo.ToString().Trim();

            //    airItemExtn.IsAutoGen = true;
            //    airItemExtn.SerialNo = serialNo;
            //    airItemExtn.UpdatedBy = user;
            //    airItemExtn.UpdatedDt = date;

            //    _db.AIRItemExtns.Attach(airItemExtn);
            //    _db.Entry(airItemExtn).State = EntityState.Modified;
            //}

            //await _db.SaveChangesAsync();

            return new AIRItemExtnOther();
        });


        public ValueTask<AIRItemExtnOther> GenerateSerialItemExtnAsync(Guid airItemExtnId, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {

            var entity = _db.AIRItemExtns.Include(i => i.AIRItem).FirstOrDefault(f => f.Id == airItemExtnId);

            if (entity == null)
            {
                throw new NotFoundException(airItemExtnId);
            }

            if (await IsPostedAsync(entity.AIRItemId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            }

            
            if (entity.AIRItem.InvDist != "I")
            {
                throw new InvalidValueException("Item is not For Inventory, cannot proceed.");
            }

            var orderItem = await _db.OrderItems
                .Include(i => i.Order.OrderItemUnitGroups)
                .Include(i => i.RequestItem.RisItem.ItemCode.ItemType)
                .Where(w => w.Id == entity.AIRItem.OrderItemId).FirstOrDefaultAsync();

            var isWithParIcs = _itemCodeService.IsWithParIcs(orderItem.ItemCodeId);
            if (isWithParIcs != true)
            {
                throw new InvalidValueException("Item is not For PAR/ICS, cannot proceed.");
            }
            
            var airItemExtns = await _db.AIRItemExtns
                .Include(i => i.AIRItem.OrderItem.Order)
                .OfType<AIRItemExtnOther>().Where(w => w.Id == airItemExtnId && (w.SerialNo == "" || w.SerialNo == null)).ToListAsync();

            if (airItemExtns.Count() == 0)
            {
                throw new NotFoundException("No items found without a serial number.");
            }

            foreach (var airItemExtn in airItemExtns)
            {
                var serialNo = airItemExtn.AIRItem.OrderItem.Order.PoNo.Trim() + "-" + airItemExtn.AIRItem.OrderItem.PsNo.Trim() + "-" +
                    (string.IsNullOrWhiteSpace(airItemExtn.SetLotNo) ? "0" : airItemExtn.SetLotNo.Trim()) + "-" +
                    (!airItemExtn.SetLotQtyNo.HasValue ? "0" : airItemExtn.SetLotQtyNo.ToString().Trim()) + "-" + airItemExtn.ContentNo.ToString().Trim();

                airItemExtn.IsAutoGen = true;
                airItemExtn.SerialNo = serialNo;
                airItemExtn.UpdatedBy = user;
                airItemExtn.UpdatedDt = date;

                _db.AIRItemExtns.Attach(airItemExtn);
                _db.Entry(airItemExtn).State = EntityState.Modified;
            }

            await _db.SaveChangesAsync();

            return new AIRItemExtnOther();
        });

        private async ValueTask<bool> IsPostedAsync(Guid? airItemId)
        {
            var entity = await _db.AIRs.Where(w => w.AIRItems.Any(a => a.Id == airItemId)).FirstOrDefaultAsync();
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }
    }
}