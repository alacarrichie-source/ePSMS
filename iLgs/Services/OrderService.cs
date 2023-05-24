using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Data.Entity;
using iLgs.Exceptions;
using System.Data.SqlClient;
using System.Data.Entity.Infrastructure;
using iLgs.Services.Items;

namespace iLgs.Services
{
    public class OrderService : IOrderService
    {
        private readonly AppManEntities db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private IItemService itemService;

        public OrderService(AppManEntities db)
        {
            this.db = db;
            this.itemService = new ItemService(db);
        }

        public IQueryable<OrderVM> GetAll()
        {
            var data = db.Orders
                .Select(s => new OrderVM
                {
                    Id = s.Id,
                    PoNo = s.PoNo,
                    PrId = s.PrId,
                    PrDate = s.Request.PrDate,
                    PoDate = s.PoDate,
                    PoMode = s.PoMode,
                    PoModeDesc = db.Codextns.Where(w => w.Code == s.PoMode && w.CodeMast.Code == "PROC_MODE").FirstOrDefault().Description,
                    PrNo = s.Request.PrNo,
                    SupplierId = s.SupplierId,
                    SupplierName = s.Supplier.Name,
                    SupplierAddress = s.Supplier.Address,
                    SupplierTin = s.Supplier.TIN,
                    DeliveryPlace = s.DeliveryPlace,
                    DeliveryDate = s.DeliveryDate,
                    TermDelivery = s.TermDelivery,
                    TermPayment = s.TermPayment,
                    SignedBySuppName = s.SignedBySuppName,
                    SignedBySuppDate = s.SignedBySuppDate,
                    SignedByAuthName = s.SignedByAuthName,
                    SignedByAuthDesignation = s.SignedByAuthDesignation,
                    ResoNo = s.ResoNo,
                    CertifiedCorrectBy = s.CertifiedCorrectBy,
                    CertifiedCorrectDate = s.CertifiedCorrectDate,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt,
                    IsLocked = false
                })
                .AsQueryable();
            return data;
        }

        public async Task<Models.Order> GetByIdAsync(Guid orderId)
        {
            return await db.Orders.FindAsync(orderId);
        }

        public async Task<bool> GetAnyPoNoAsync(Guid id, string poNo)
        {
            return await db.Orders.AnyAsync(a => a.Id != id && a.PoNo == poNo);
        }

        public async Task<Models.Order> GetByPoNoAsync(string poNo)
        {
            return  await db.Orders.Where(w => w.PoNo == poNo).FirstOrDefaultAsync();
        }

        public async Task<OrderVM> CreateAsync(OrderVM model, string user, DateTime date)
        {
            model.Id = Guid.NewGuid();
            if (string.IsNullOrWhiteSpace(model.PoNo))
            {
                model.PoNo = NextPoNo((DateTime)model.PoDate);
            }
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = new iLgs.Models.Order()
            {
                Id = model.Id,
                SupplierId = model.SupplierId,
                PoNo = model.PoNo,
                PoDate = model.PoDate,
                PoMode = model.PoMode,
                PrId = model.PrId,
                DeliveryPlace = model.DeliveryPlace,
                DeliveryDate = model.DeliveryDate,
                TermDelivery = model.TermDelivery,
                TermPayment = model.TermPayment,
                SignedByAuthDesignation = model.SignedByAuthDesignation,
                SignedByAuthName = model.SignedByAuthName,
                SignedBySuppDate = model.SignedBySuppDate,
                SignedBySuppName = model.SignedBySuppName,
                ResoNo = model.ResoNo,
                CertifiedCorrectBy = model.CertifiedCorrectBy,
                CertifiedCorrectDate = model.CertifiedCorrectDate,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            // include items during add
            var requestItems = db.RequestItems.Where(w => w.PrId == model.PrId).ToList();
            foreach (var requestItem in requestItems)
            {
                OrderItem orderItem = new OrderItem()
                {
                    Id = Guid.NewGuid(),
                    OrderId = entity.Id,
                    RequestItemId = requestItem.Id,
                    Description = requestItem.Description,
                    BrandName = requestItem.BrandName,
                    OtherSpecs = requestItem.OtherSpecs,
                    Qty = requestItem.Qty,
                    UnitCost = requestItem.UnitCost,
                    Amount = requestItem.TotalCost,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };

                entity.OrderItems.Add(orderItem);
            }

            db.Orders.Add(entity);
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<OrderVM> UpdateAsync(OrderVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await db.Orders.FindAsync(model.Id);

            entity.SupplierId = model.SupplierId;
            entity.PoNo = model.PoNo;
            entity.PoDate = model.PoDate;
            entity.PoMode = model.PoMode;
            entity.PrId = model.PrId;
            entity.DeliveryPlace = model.DeliveryPlace;
            entity.DeliveryDate = model.DeliveryDate;
            entity.TermDelivery = model.TermDelivery;
            entity.TermPayment = model.TermPayment;
            entity.SignedByAuthDesignation = model.SignedByAuthDesignation;
            entity.SignedByAuthName = model.SignedByAuthName;
            entity.SignedBySuppDate = model.SignedBySuppDate;
            entity.SignedBySuppName = model.SignedBySuppName;
            entity.ResoNo = model.ResoNo;
            entity.CertifiedCorrectBy = model.CertifiedCorrectBy;
            entity.CertifiedCorrectDate = model.CertifiedCorrectDate;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.Orders.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<OrderVM> DeleteAsync(OrderVM model, string user, DateTime date)
        {

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await db.Orders.FindAsync(model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.Orders.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.Orders.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            return model;
        }

        public async Task PostAsync(Guid orderId, string user, DateTime date)
        {
            var orderItemGroups = await db.Database.SqlQuery<OrderItemGroupVM>("Exec OrderService_GetOrderItemGroup {0}", orderId).ToListAsync();
            // create stock for each group
            foreach (var oig in orderItemGroups)
            {
                // find group in stocks
                if (!db.PsStocks.Where(w => w.PsId == oig.PsCodeId
                    && w.BrandName == oig.BrandName
                    && w.Description == oig.Description && w.OtherSpecs == oig.OtherSpecs).Any())
                {
                    var stockNo = db.PsCodes.Find(oig.PsCodeId).PsNo;
                    var nextStockNo = this.itemService.NextStockNo(stockNo);
                    var psStock = new PsStock
                    {
                        Id = Guid.NewGuid(),
                        PsId = oig.PsCodeId,
                        StockNo = nextStockNo,
                        Description = oig.Description,
                        BrandName = oig.BrandName,
                        OtherSpecs = oig.OtherSpecs,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date                       
                    };

                    // Post the OrderItems under the stocks having the same PsCodeId
                    //var orderItemList = order.OrderItems.Where(w => db.RequestItems.Any(a => a.PsCodeId == oig.PsCodeId)).ToList();
                    var orderItemList = await db.OrderItems.Include(i => i.RequestItem.PsCode)
                        .Where(w => w.OrderId == orderId && w.RequestItem.PsCodeId == oig.PsCodeId).ToListAsync();
                    foreach (var orderItem in orderItemList)
                    {
                        var psItem = new PsItem()
                        {
                            Id = Guid.NewGuid(),
                            PsStockId = psStock.Id,
                            OrderItemId = orderItem.Id,
                            RefNo = orderItem.Order.PoNo,
                            RefDate = orderItem.Order.PoDate,
                            RefType = "PO",
                            Qty = orderItem.Qty,
                            QtyIss = 0,
                            QtyBal = orderItem.Qty,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };
                        psStock.PsItems.Add(psItem);
                    }

                    db.PsStocks.Add(psStock);
                    await db.SaveChangesAsync();
                }
            }

            var entity = await db.Orders.FindAsync(orderId);
            entity.PostedBy = user;
            entity.PostedDt = date;

            db.Orders.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();
        }

        public async Task UnpostAsync(Guid orderId, string user, DateTime date)
        {
            var entity = await db.Orders.FindAsync(orderId);
            entity.PostedBy = null;
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.Orders.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();
        }

        private string NextPoNo(DateTime poDate)
        {
            string yyyy = poDate.Year.ToString().Trim();
            string mm = poDate.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var order = db.Orders.Where(w => w.PoDate.Value.Year == poDate.Year).OrderByDescending(o => o.PoNo).FirstOrDefault();
            if (order == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(order.PoNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }

        #region VALIDATION
        private void ValidateOnCreate(OrderVM model)
        {
            //var uniqueId = _ra.CodeMast.GetByIdAsync(model.Id).Result;
            //if (uniqueId != null)
            //{
            //    throw new RecordAlreadyExistsException(model.Id);
            //}
            //else
            //{
            //    var record = _ra.CodeMast.GetByIdAndCodeAsync(model.Id, model.Code).Result;
            //    if (record != null)
            //    {
            //        throw new RecordAlreadyExistsException(model.Code);
            //    }
            //}
        }
        #endregion

        #region EXCEPTIONS
        private delegate iLgs.Models.OrderVM ReturningFunction();
        private delegate IQueryable<iLgs.Models.OrderVM> ReturningQueryableFunction();
        private iLgs.Models.OrderVM TryCatch(ReturningFunction returningFunction)
        {
            try
            {
                return returningFunction();
            }
            //catch (ModelIsNullException nullException)
            //{
            //    throw nullException;
            //}
            //catch (InvalidRecordException invalidException)
            //{
            //    throw invalidException;
            //}
            catch (RecordNotFoundException notFoundException)
            {
                throw notFoundException;
            }
            catch (RecordAlreadyExistsException recordAlreadyExistsException)
            {
                throw recordAlreadyExistsException;
            }
            catch (SqlException sqlException)
            {
                throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var recordLockedException = new RecordLockedException(dbUpdateConcurrencyException);

                throw exceptions.CreateAndLogDependencyException(recordLockedException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                throw exceptions.CreateAndLogDependencyException(dbUpdateException);
            }
            catch (Exception exception)
            {
                var failedServiceException =
                    new FailedServiceException(exception);

                throw exceptions.CreateAndLogServiceException(failedServiceException);
            }
        }

        public async Task<bool> IsPostedAsync(Guid orderId)
        {
            var entity = await db.Orders.FindAsync(orderId);
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }       
        #endregion
    }
}