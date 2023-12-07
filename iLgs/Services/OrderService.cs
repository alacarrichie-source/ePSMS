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
using System.Web.Http.ModelBinding;

namespace iLgs.Services
{
    public class OrderService : IOrderService
    {
        private readonly AppManEntities db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<OrderVM> _orderVmExceptionService = new ExceptionService<OrderVM>();
        private readonly IExceptionService<Order> _orderExceptionService = new ExceptionService<Order>();
        private IItemService itemService;
        
        public OrderService(AppManEntities db)
        {
            this.db = db;
            this.itemService = new ItemService(db);        
        }

        public IQueryable<OrderVM> GetAll() => _orderVmExceptionService.TryCatch(() =>
        {
            var data = db.Orders.AsNoTracking()
                .Select(s => new OrderVM
                {
                    Id = s.Id,
                    PoNo = s.PoNo,
                    PrId = s.PrId,
                    PrDate = s.Request.PrDate,
                    PoDate = s.PoDate,
                    PoMode = s.PoMode,
                    PoModeDesc = db.Codextns.Where(w => w.Code == s.PoMode && w.CodeMast.Code == "PROC-MODE").FirstOrDefault().Description,
                    PrNo = s.Request.PrNo,
                    Department = s.Request.RISs.Office,
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
        });

        public IQueryable<OrderVM> GetAllParOrders() => _orderVmExceptionService.TryCatch(() =>
        {
            var data = db.Orders.AsNoTracking()
                .Where(w => w.PostedBy != null && w.AIRs.Any(a => a.PostedBy != null))
                .Select(s => new OrderVM
                {
                    Id = s.Id,
                    PoNo = s.PoNo,
                    PoDate = s.PoDate,
                    PoMode = s.PoMode,
                    SupplierId = s.SupplierId,
                    SupplierName = s.Supplier.Name,
                    SupplierAddress = s.Supplier.Address,
                    Department = s.Request.RISs.Office
                })
                .AsQueryable();
            return data;
        });

        public ValueTask<Order> GetByIdAsync(Guid orderId) => _orderExceptionService.TryCatch(async () =>
        {
            return await db.Orders.FindAsync(orderId);
        });

        public async ValueTask<bool> GetAnyPoNoAsync(Guid id, string poNo) 
        {
            return await db.Orders.AnyAsync(a => a.Id != id && a.PoNo == poNo);
        }

        public ValueTask<Order> GetByPoNoAsync(string poNo) => _orderExceptionService.TryCatch(async () =>
        {
            return await db.Orders.Where(w => w.PoNo == poNo).FirstOrDefaultAsync();
        });

        public async ValueTask<bool> GetAnyParsAsync(Guid id)
        {
            return await db.PARs.AnyAsync(a => a.OrderId == id);
        }

        public async ValueTask<bool> GetAnyAirsAsync(Guid id)
        {
            return await db.AIRs.AnyAsync(a => a.OrderId == id);
        }

        public async ValueTask<bool> IsPostedAsync(Guid orderId)
        {
            var entity = await db.Orders.FindAsync(orderId);
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }

        public async ValueTask<int> GetNotPostedAsync(DateTime asOf)
        {
            return await db.Orders.Where(w => w.PoDate <= asOf && w.PostedDt == null).CountAsync();
        }

        public ValueTask<OrderVM> CreateAsync(OrderVM model, string user, DateTime date) => _orderVmExceptionService.TryCatch(async () =>
        {
            await ValidateOnCreate(model);

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

            // include items during add, PR Items not yet in Order Items
            var requestItems = db.RequestItems.Include(i => i.RisItem.RisItemExtns)
                .Where(w => w.PrId == model.PrId && !w.OrderItems.Any()).ToList();
            foreach (var requestItem in requestItems)
            {
                OrderItem orderItem = new OrderItem()
                {
                    Id = Guid.NewGuid(),
                    OrderId = entity.Id,
                    RequestItemId = requestItem.Id,
                    Description = requestItem.RisItem.Description,
                    Qty = requestItem.Qty,
                    UnitCost = requestItem.UnitCost,
                    Amount = requestItem.TotalCost,
                    PriceRate = requestItem.PriceRate,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };

                foreach (var risItemExtn in requestItem.RisItem.RisItemExtns)
                {
                    OrderItemExtn orderItemExtn = new OrderItemExtn()
                    {
                        Id = Guid.NewGuid(),
                        OrderItemId = orderItem.Id,
                        ItemKey = risItemExtn.ItemKey,
                        ItemValue = risItemExtn.ItemValue,
                        Sequence = risItemExtn.Sequence,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };
                    orderItem.OrderItemExtns.Add(orderItemExtn);
                }

                entity.OrderItems.Add(orderItem);
            }

            // Unit Groups
            var unitGroups = await db.RequestItemUnitGroups.Include(i => i.RequestItemUnitGroupDescriptions).Where(w => w.PrId == model.PrId).OrderBy(o => o.InsertedDt).ToListAsync();
            foreach (var unitGroup in unitGroups)
            {
                var unitGroupDt = DateTime.Now;
                var orderItemUnitGroup = new OrderItemUnitGroup()
                {
                    Id = Guid.NewGuid(),
                    OrderId = model.Id,
                    RequestItemUnitGroupId = unitGroup.Id,
                    UnitCost = unitGroup.UnitCost,
                    TotalCost = unitGroup.TotalCost,
                    InsertedBy = user,
                    InsertedDt = unitGroupDt,
                    UpdatedBy = user,
                    UpdatedDt = unitGroupDt
                };

                foreach (var unitGroupDescription in unitGroup.RequestItemUnitGroupDescriptions.OrderBy(o => o.InsertedDt).ToList())
                {
                    var groupDescriptionDt = DateTime.Now;
                    var orderItemUnitGroupDescription = new OrderItemUnitGroupDescription()
                    {
                        Id = Guid.NewGuid(),
                        OrderItemUnitGroupId = orderItemUnitGroup.Id,
                        RequestItemUnitGroupDescriptionId = unitGroupDescription.Id,
                        InsertedBy = user,
                        InsertedDt = groupDescriptionDt,
                        UpdatedBy = user,
                        UpdatedDt = groupDescriptionDt
                    };

                    var requestItemUnitGroupDescriptionItems = await db.RequestItemUnitGroupDescriptionItems.Where(w => w.RequestItemUnitGroupDescriptionId == unitGroupDescription.Id).OrderBy(o => o.InsertedDt).ToListAsync();
                    foreach (var unitGroupDescriptionItem in requestItemUnitGroupDescriptionItems)
                    {
                        var groupDescriptionItemDt = DateTime.Now;
                        var orderItemUnitGroupDescriptionItem = new OrderItemUnitGroupDescriptionItem()
                        {
                            Id = Guid.NewGuid(),
                            RequestItemUnitGroupDescriptionItemId = unitGroupDescriptionItem.Id,
                            OrderItemUnitGroupDescriptionId = orderItemUnitGroupDescription.Id,
                            OrderItemId = entity.OrderItems.FirstOrDefault(f => f.RequestItemId == unitGroupDescriptionItem.RequestItemId).Id,
                            InsertedBy = user,
                            InsertedDt = groupDescriptionItemDt,
                            UpdatedBy = user,
                            UpdatedDt = groupDescriptionItemDt
                        };
                        orderItemUnitGroupDescription.OrderItemUnitGroupDescriptionItems.Add(orderItemUnitGroupDescriptionItem);
                    }
                    orderItemUnitGroup.OrderItemUnitGroupDescriptions.Add(orderItemUnitGroupDescription);
                }
                entity.OrderItemUnitGroups.Add(orderItemUnitGroup);
            }

            db.Orders.Add(entity);
            await db.SaveChangesAsync();

            return model;
        });

        public ValueTask<OrderVM> UpdateAsync(OrderVM model, string user, DateTime date) => _orderVmExceptionService.TryCatchAsync(async () =>
        {
            await ValidateOnUpdate(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await db.Orders.FindAsync(model.Id);

            // if there's a change of request item
            if (entity.PrId != model.PrId)
            {
                var orderItems = db.OrderItems.Where(w => w.OrderId == model.Id);
                await orderItems.ForEachAsync(f =>
                {
                    f.UpdatedBy = model.UpdatedBy;
                    f.UpdatedDt = model.UpdatedDt;
                });
                await db.SaveChangesAsync();

                db.OrderItems.RemoveRange(orderItems);
                await db.SaveChangesAsync();

                // include items during add, PR Items not yet in Order Items
                var prItemList = db.RequestItems.Include(i => i.RisItem.RisItemExtns)
                    .Where(w => w.PrId == model.PrId && !w.OrderItems.Any()).ToList();
                foreach (var prItem in prItemList)
                {
                    OrderItem orderItem = new OrderItem()
                    {
                        Id = Guid.NewGuid(),
                        OrderId = entity.Id,
                        RequestItemId = prItem.Id,
                        Description = prItem.RisItem.Description,
                        Qty = prItem.Qty,
                        UnitCost = prItem.UnitCost,
                        Amount = prItem.TotalCost,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };

                    foreach (var prItemExtn in prItem.RisItem.RisItemExtns)
                    {
                        OrderItemExtn orderItemExtn = new OrderItemExtn()
                        {
                            Id = Guid.NewGuid(),
                            OrderItemId = orderItem.Id,
                            ItemKey = prItemExtn.ItemKey,
                            ItemValue = prItemExtn.ItemValue,
                            Sequence = prItemExtn.Sequence,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };
                        orderItem.OrderItemExtns.Add(orderItemExtn);
                    }

                    entity.OrderItems.Add(orderItem);
                }
            }

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
        });

        public ValueTask<OrderVM> DeleteAsync(OrderVM model, string user, DateTime date) =>
        _orderVmExceptionService.TryCatchAsync(async () =>
        {

            var unitGroups = db.OrderItemUnitGroups.Where(w => w.OrderId == model.Id);
            if (unitGroups.Any())
            {
                db.OrderItemUnitGroups.RemoveRange(unitGroups);
                await db.SaveChangesAsync();
            }

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
        });

        public ValueTask PostAsync(Guid orderId, string user, DateTime date) => _orderExceptionService.TryCatch(async () =>
        {            
            var entity = await db.Orders.FindAsync(orderId);
            if (entity == null)
            {
                throw new RecordNotFoundException(orderId);
            }

            await ValidateOnPost(entity);

            entity.PostedBy = user;
            entity.PostedDt = date;

            db.Orders.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            var orderItemGroups = await db.Database.SqlQuery<OrderItemGroupVM>("Exec OrderService_GetOrderItemGroup {0}", orderId).ToListAsync();
            // create stock for each group
            foreach (var oig in orderItemGroups)
            {
                // find group in stocks
                PsStock psStock = await db.PsStocks.Where(w => w.PsId == oig.PsCodeId && w.StockNo == oig.StockNo && w.Fund == oig.Fund && w.UnitMeas == oig.Unit).FirstOrDefaultAsync();
                if (psStock == null)
                {
                    psStock = new PsStock
                    {
                        Id = Guid.NewGuid(),
                        PsId = oig.PsCodeId,
                        StockNo = oig.StockNo,
                        StockName = oig.StockName,
                        Description = oig.Description,                        
                        Brand = oig.Brand,
                        Fund = oig.Fund,
                        UnitMeas = oig.Unit,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };

                    db.PsStocks.Add(psStock);
                    await db.SaveChangesAsync();
                }

                // Post the OrderItems under the stocks having the same PsCodeId
                var orderItemList = await db.OrderItems
                    .Include(i => i.Order)
                    .Include(i => i.RequestItem.RisItem.RISs)
                    .Include(i => i.OrderItemExtns)
                    .Where(w => w.OrderId == orderId 
                        && w.StockNo == oig.StockNo
                        && w.StockName == oig.StockName
                        && w.Description == oig.Description
                        && w.Brand == oig.Brand
                        && w.RequestItem.RisItem.RISs.Fund == oig.Fund).ToListAsync();
                foreach (var orderItem in orderItemList)
                {
                    var qty = db.AIRItems.Where(w => w.OrderItemId == orderItem.Id).Sum(s => s.Qty) ?? 0;
                    var risItemId = db.OrderItems.Where(w => w.Id == orderItem.Id).FirstOrDefault()?.RequestItem?.RisItem.Id;
                    int qtyIss = 0;
                    if (risItemId != null)
                    {
                        qtyIss = db.RisIssueds.Where(w => w.RisItemId == risItemId && w.RisItem.RISs.PostedDt != null).Sum(s => s.Qty) ?? 0;
                    }
                    var psItem = new PsItem()
                    {
                        Id = Guid.NewGuid(),
                        PsStockId = psStock.Id,
                        Office = orderItem.RequestItem.RisItem.RISs.Office,
                        OrderItemId = orderItem.Id,
                        RefNo = orderItem.Order.PoNo,
                        RefDate = orderItem.Order.PoDate,
                        RefType = "PO",
                        QtyPo = orderItem.Qty,
                        Qty = qty,
                        QtyIss = qtyIss,
                        QtyBal = qty - qtyIss,
                        UnitMeas = orderItem.RequestItem.RisItem.Unit,
                        UnitCost = orderItem.UnitCost,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };
                    db.PsItems.Add(psItem);
                }
                await db.SaveChangesAsync();

                // search PsStockExtns for Field Descripsiotn
                if (!db.PsStockExtns.Any(a => a.PsStockId == psStock.Id))
                {
                    // get first orderItemExtn from orderItemList
                    var orderItemExtns = orderItemList.FirstOrDefault().OrderItemExtns;
                    foreach (var orderItemExtn in orderItemExtns)
                    {
                        var psStockExtn = new PsStockExtn()
                        {
                            Id = Guid.NewGuid(),
                            PsStockId = psStock.Id,
                            ItemNo = orderItemExtn.ItemNo,
                            ItemKey = orderItemExtn.ItemKey,
                            ItemValue = orderItemExtn.ItemValue,
                            Sequence = orderItemExtn.Sequence,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };
                        db.PsStockExtns.Add(psStockExtn);
                    }
                    await db.SaveChangesAsync();
                }
            }
        });

        public ValueTask UnpostAsync(Guid orderId, string user, DateTime date) => _orderExceptionService.TryCatch(async () =>
        {
            var entity = await db.Orders.FindAsync(orderId);

            if (entity == null)
            {
                throw new RecordNotFoundException(orderId);
            }

            await ValidateOnUnpost(entity);

            entity.PostedBy = null;
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.Orders.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            /*
                * Delete the following records onUnpost:
                * PsItem             
                * PsStocks, PsStockExtns --> if no PsItem
            */
            var orderItems = db.OrderItems.Include(i => i.RequestItem.RisItem).Where(w => w.OrderId == orderId).ToList();

            foreach (var orderItem in orderItems)
            {
                //var psItems = db.PsItems.Where(w => w.RefNo == orderItem.Order.PoNo && w.RefDate == orderItem.Order.PoDate);
                var psItems = db.PsItems.Where(w => w.OrderItemId == orderItem.Id);
                if (psItems.Count() > 0)
                {
                    var psStockId = psItems.FirstOrDefault().PsStockId;
                    await psItems.ForEachAsync(f =>
                    {
                        f.UpdatedBy = user;
                        f.UpdatedDt = date;
                    });
                    await db.SaveChangesAsync();

                    // delete each orderitem in stock psItems
                    db.PsItems.RemoveRange(psItems);
                    await db.SaveChangesAsync();

                    if (!db.PsItems.Any(a => a.PsStockId == psStockId)) // no other order item is using this item
                    {
                        var psStockEntity = await db.PsStocks.FindAsync(psStockId);
                        psStockEntity.UpdatedBy = user;
                        psStockEntity.UpdatedDt = date;

                        db.PsStocks.Attach(psStockEntity);
                        db.Entry(psStockEntity).State = EntityState.Modified;
                        await db.SaveChangesAsync();

                        // delete stock during unpost if not used by other order item
                        db.PsStocks.Remove(psStockEntity);
                        db.Entry(psStockEntity).State = EntityState.Deleted;
                        await db.SaveChangesAsync();
                    }
                }
            }
        });

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

        private async ValueTask ValidateOnCreate(OrderVM model)
        {
            if (await db.Orders.AnyAsync(a => a.PoNo == model.PoNo))
            {
                throw new RecordAlreadyExistsException(string.Format("PO Number {0} already exists", model.PoNo));
            }
            else
            {
                var pr = await db.Requests.FindAsync(model.PrId);
                if (pr == null)
                {
                    throw new RecordNotFoundException(model.PrId);
                }
                else
                {
                    if (pr.PrDate > model.PoDate)
                    {
                        throw new InvalidValueException("PO Date must be greater than or equal to PR date!");                        
                    }
                }
            }
        }

        private async ValueTask ValidateOnUpdate(OrderVM model)
        {
            var order = await db.Orders.FindAsync(model.Id);
            if (order == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (order.PostedDt != null)
            {
                throw new RecordAlreadyPostedException(string.Format("PO Number {0} already posted, cannot update!", model.PoNo));
            }


            if (await GetAnyPoNoAsync(model.Id, model.PoNo))
            {
                throw new RecordAlreadyExistsException(string.Format("PO Number {0} already exists!", model.PoNo));                
            }


            var pr = await db.Requests.FindAsync(model.PrId); 
            if (pr == null)
            {
                throw new RecordRelationshipException(string.Format("PR Number {0} does exists!", model.PrNo));                
            }
                
            else if (pr.PrDate > model.PoDate)
            {
                throw new InvalidValueException("P.O. date must be greather than or equal to P.R. date!");                
            }            
        }

        private async ValueTask ValidateOnDestroy(OrderVM model)
        {
            var order = await db.Orders.FindAsync(model.Id);
            if (order == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (await IsPostedAsync(model.Id))
            {
                throw new RecordAlreadyPostedException(string.Format("PO Number {0} already Posted, cannot delete!", model.PoNo));                 
            }

            if (await GetAnyAirsAsync(model.Id))
            {
                throw new RecordRelationshipException("PO Number already with AIR, cannot delete!");                
            }

            if (await GetAnyParsAsync(model.Id))
            {
                throw new RecordRelationshipException("PO Number already with PAR, cannot delete!");                
            }
        }

        private async ValueTask ValidateOnPost(Order entity)
        {            
            if (!string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new PoNumberAlreadyPostedException(entity.PoNo);
            }

            var idList = await db.OrderItems.Where(w => w.OrderId == entity.Id).GroupBy(g => g.RequestItem.Request.Id)
                .Select(s => s.Key).ToListAsync();

            foreach (var id in idList)
            {
                var request = await db.Requests.FindAsync(id);
                if (request == null)
                {
                    throw new RecordNotFoundException(id);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(request.SubmittedBy))
                    {
                        throw new PurchaseRequestNotYetPostedException(request.PrNo);
                    }
                }
            }            

            var orderItems = await db.OrderItems.Where(w => w.OrderId == entity.Id).ToListAsync();
            foreach(var orderItem in orderItems)
            {
                if (string.IsNullOrWhiteSpace(orderItem.Brand))
                {
                    throw new RequiredFieldException(nameof(orderItem.Brand));
                }
            }
        }

        private async ValueTask ValidateOnUnpost(Order entity)
        {
            if (string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordNotYetPostedException(string.Format("PO Number {0} not yet posted..", entity.PoNo));
            }

            var airs = await db.AIRs.Where(w => w.OrderId == entity.Id && w.PostedDt != null).ToListAsync();

            foreach (var air in airs) {
                throw new RecordRelationshipException(string.Format("AIR Number {0} of this PO is already posted.", air.AIRNo));
            }
        }
        #endregion

        #region EXCEPTIONS
        //private delegate ValueTask<OrderVM> ReturningFunction();
        //private delegate IQueryable<OrderVM> ReturningQueryableFunction();
        //private async ValueTask<OrderVM> TryCatch(ReturningFunction returningFunction)
        //{
        //    try
        //    {
        //        return await returningFunction();
        //    }
        //    catch (RecordNotFoundException notFoundException)
        //    {
        //        throw notFoundException;
        //    }
        //    catch (RecordAlreadyExistsException recordAlreadyExistsException)
        //    {
        //        throw recordAlreadyExistsException;
        //    }
        //    catch (SqlException sqlException)
        //    {
        //        throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
        //    }
        //    catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
        //    {
        //        var recordLockedException = new RecordLockedException(dbUpdateConcurrencyException);

        //        throw exceptions.CreateAndLogDependencyException(recordLockedException);
        //    }
        //    catch (DbUpdateException dbUpdateException)
        //    {
        //        throw exceptions.CreateAndLogDependencyException(dbUpdateException);
        //    }
        //    catch (Exception exception)
        //    {
        //        var failedServiceException =
        //            new FailedServiceException(exception);

        //        throw exceptions.CreateAndLogServiceException(failedServiceException);
        //    }
        //}
        
        #endregion
    }
}