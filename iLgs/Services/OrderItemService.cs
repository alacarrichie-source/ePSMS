using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System.Data.Entity;
using iLgs.Exceptions;

namespace iLgs.Services
{
    public class OrderItemService : IOrderItemService
    {
        private readonly AppManEntities db = new AppManEntities();
        private readonly IExceptionService<OrderItemVM> _VmExceptionService = new ExceptionService<OrderItemVM>();
        private ICodextnService _codextnService;
        
        public OrderItemService(AppManEntities db)
        {
            this.db = db;
            _codextnService = new CodextnService(db);
        }

        public ValueTask<OrderItemVM> GetByIdAsync(Guid? id) => _VmExceptionService.TryCatch(async () =>
        {
            var data = await db.OrderItems.Where(w => w.Id == id)
                .Select(s => new OrderItemVM
                {
                    Id = s.Id,
                    OrderId = s.OrderId,                    
                    RequestItemId = s.RequestItemId,
                    RisItemId = s.RequestItem.RisItem.Id,
                    ItemCode = s.RequestItem.RisItem.ItemCode.Code,
                    ItemType = s.RequestItem.RisItem.ItemCode.Description,
                    PsType = s.RequestItem.RisItem.ItemCode.ItemType.Code,
                    PsNo = s.RequestItem.RisItem.PsNo,
                    Brand = s.Brand,
                    StockNo = s.StockNo,
                    StockName = s.StockName,
                    PsNoDisplay = s.RequestItem.RisItem.PsNoDisplay,
                    Unit = s.RequestItem.RisItem.Unit,
                    ItemName = s.RequestItem.RisItem.ItemName,
                    Description = s.Description,
                    Qty = s.Qty,
                    UnitCost = s.UnitCost,
                    Amount = s.Amount,
                    PriceRate = s.PriceRate,
                    InsertedDt = s.InsertedDt
                }).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<OrderItemVM> GetByPoId(Guid? poId) => _VmExceptionService.TryCatch(() =>
        {
            var data = db.OrderItems.Where(w => w.OrderId == poId)
                .Select(s => new OrderItemVM
                {
                    Id = s.Id,
                    OrderId = s.OrderId,
                    RequestItemId = s.RequestItemId,
                    RisItemId = s.RequestItem.RisItem.Id,
                    ItemCode = s.RequestItem.RisItem.ItemCode.Code,
                    ItemType = s.RequestItem.RisItem.ItemCode.Description,
                    PsType = s.RequestItem.RisItem.ItemCode.ItemType.Code,
                    PsNo = s.RequestItem.RisItem.PsNo,
                    Brand = s.Brand,
                    StockNo = s.StockNo,
                    StockName = s.StockName,
                    PsNoDisplay = s.RequestItem.RisItem.PsNoDisplay,
                    Unit = s.RequestItem.RisItem.Unit,
                    ItemName = s.RequestItem.RisItem.ItemName,
                    Description = s.Description,
                    Qty = s.Qty,
                    UnitCost = s.UnitCost,
                    Amount = s.Amount,
                    PriceRate = s.PriceRate,
                    InsertedDt = s.InsertedDt
                });
            return data;
        });

        public async ValueTask<bool> GetAnyParItemsAsync(Guid id)
        {
            return await db.PARItems.AnyAsync(a => a.OrderItemId == id);
        }

        public async ValueTask<bool> GetAnyAirItemsAsync(Guid id)
        {
            return await db.AIRItems.AnyAsync(a => a.OrderItemId == id);
        }

        public ValueTask<OrderItemVM> CreateAsync(OrderItemVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            if (string.IsNullOrWhiteSpace(model.Brand))
            {
                throw new RequiredFieldException(nameof(model.Brand));
            }

            var requestItemId = db.RequestItems.FindAsync(model.RequestItemId).Result?.RisItemId;
            if (requestItemId == null)
            {
                throw new RecordRelationshipException("Could not find request item this record!");
            }

            var risItemId = db.RisItems.FindAsync(requestItemId).Result?.Id;
            if (risItemId == null)
            {
                throw new RecordRelationshipException("Cound not find RIS item for this record!");
            }
            
            if (!(await _codextnService.IsValidCodeDescAsync("BRANDS", model.Brand)))
            {
                throw new RecordRelationshipException("Brand is not valid!");
            }

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;
            model.StockName = await StockNameAsync(model, risItemId);

            OrderItem entity = new OrderItem()
            {
                Id = model.Id,
                OrderId = model.OrderId,
                RequestItemId = model.RequestItemId,
                StockNo = model.PsNo.Trim() + model.Brand,
                StockName = model.StockName,
                Brand = model.Brand,
                Description = model.Description,
                Qty = model.Qty,
                UnitCost = model.UnitCost,
                Amount = model.Amount,
                PriceRate = model.PriceRate,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            db.OrderItems.Add(entity);
            await db.SaveChangesAsync();

            return model;
        });

        public ValueTask<OrderItemVM> DeleteAsync(OrderItemVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            await ValidateOnDelete(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            OrderItem entity = await db.OrderItems.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.OrderItems.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.OrderItems.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            return model;
        });

        public ValueTask<OrderItemVM> UpdateAsync(OrderItemVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            if (string.IsNullOrWhiteSpace(model.Brand))
            {
                throw new RequiredFieldException(nameof(model.Brand));
            }

            var requestItemId = db.RequestItems.FindAsync(model.RequestItemId).Result?.RisItemId;
            if (requestItemId == null)
            {
                throw new RecordRelationshipException("Could not find request item this record!");
            }

            var risItemId = db.RisItems.FindAsync(requestItemId).Result?.Id;
            if (risItemId == null)
            {
                throw new RecordRelationshipException("Cound not find RIS item for this record!");
            }

            if (!(await _codextnService.IsValidCodeDescAsync("BRANDS", model.Brand)))
            {
                throw new RecordRelationshipException("Brand is not valid!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            OrderItem entity = await db.OrderItems.FindAsync(model.Id);

            entity.RequestItemId = model.RequestItemId;
            entity.StockNo = model.PsNo.Trim() + model.Brand;
            entity.StockName = await StockNameAsync(model, risItemId);
            entity.Brand = model.Brand;
            entity.Description = model.Description;
            entity.Qty = model.Qty;
            entity.UnitCost = model.UnitCost;
            entity.Amount = model.Amount;
            entity.PriceRate = model.PriceRate;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.OrderItems.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return model;
        });

        private async ValueTask<string> StockNameAsync(OrderItemVM orderItem, Guid? risItemId) 
        {
            
            var psType = orderItem.PsType;
            var itemExtns = await db.Database.SqlQuery<RisItemExtnVM>("Exec RisItemExtnService_GetBatchInfo {0}, {1}", risItemId, psType).ToListAsync();
            var psCode = orderItem.ItemCode.ToString();

            string stockName = orderItem.Brand.Trim();
            string itemName = orderItem.ItemName.Substring(0, 1) + orderItem.ItemName.Substring(2, 1);

            if (psType == "M")
            {
                var df = itemExtns.FirstOrDefault(f => f.ItemKey == "Dosage Form");
                if (df != null)
                {
                    stockName += df.ItemValue.Substring(0, 3);
                }
                var ds = itemExtns.FirstOrDefault(f => f.ItemKey == "Dosage Strength");
                if (ds != null)
                {
                    stockName += ds.ItemValue.Replace(" ", "");
                }

                stockName += itemName;

                if (orderItem.ItemType == "")
                {
                    stockName += "*";
                }

                stockName += psCode;
            }
            return stockName;
        }

        //public async Task<string> StockNameAsync(Guid? orderItemId)
        //{
        //    var orderItem = await db.OrderItems.Include(i => i.RequestItem.RisItem.ItemCode.ItemType).Where(w => w.Id == orderItemId).FirstOrDefaultAsync();
        //    var psCode = orderItem.RequestItem.RisItem.ItemCode.ToString();
        //    var psType = orderItem.RequestItem.RisItem.ItemCode.ItemType.Code;
        //    var itemExtns = await db.Database.SqlQuery<OrderItemExtnVM>("Exec OrderItemExtnService_GetBatchInfo {0}, {1}", orderItemId, psType).ToListAsync();
        //    string stockName = orderItem.Brand.Trim();
        //    string itemName = orderItem.RequestItem.RisItem.ItemName.Substring(0, 1) + orderItem.RequestItem.RisItem.ItemName.Substring(2, 1);

        //    if (psType == "M")
        //    {
        //        var df = itemExtns.FirstOrDefault(f => f.ItemKey == "Dosage Form");
        //        if (df != null)
        //        {
        //            stockName += df.ItemValue.Substring(0, 3);
        //        }
        //        var ds = itemExtns.FirstOrDefault(f => f.ItemKey == "Dosage Strength");
        //        if (ds != null)
        //        {
        //            stockName += ds.ItemValue.Replace(" ", "");
        //        }

        //        stockName += itemName;

        //        if (orderItem.RequestItem.RisItem.ItemCode.Description == "")
        //        {
        //            stockName += "*";
        //        }

        //        stockName += psCode;
        //    }
        //    return stockName;
        //}

        private async ValueTask ValidateOnDelete(OrderItemVM model)
        {
            var postedBy = db.Orders.FindAsync(model.OrderId).Result?.PostedBy;
            if (!string.IsNullOrWhiteSpace(postedBy))
            {
                throw new RecordAlreadyPostedException("PO Number already Posted, cannot delete!");
            }
            if (await GetAnyAirItemsAsync(model.Id))
            {
                throw new RecordRelationshipException("PO Number already with AIR, cannot delete!");
            }
            if (await GetAnyParItemsAsync(model.Id))
            {
                throw new RecordRelationshipException("PO Number already with PAR, cannot delete!");
            }
        }
    }
}