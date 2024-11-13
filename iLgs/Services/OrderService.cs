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
using System.Web.Http.ModelBinding;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using static iLgs.Models.Enums;
using iLgs.Services.AllFields;
using iLgs.Exceptions.Service;
using iLgs.Services.Validators;
using iLgs.Utilities;

namespace iLgs.Services
{
    public interface IOrderService
    {
        IQueryable<OrderVM> GetAll();
        IQueryable<OrderVM> GetAllParOrders();
        ValueTask<Models.Order> GetByIdAsync(Guid orderId);
        ValueTask<Models.Order> GetByPoNoAsync(string poNo);
        ValueTask<bool> GetAnyPoNoAsync(Guid id, string poNo);
        ValueTask<bool> IsPostedAsync(Guid orderId);
        ValueTask<bool> GetAnyParsAsync(Guid id);
        ValueTask<bool> GetAnyAirsAsync(Guid id);

        ValueTask<int> GetNotPostedAsync(DateTime asOf);

        ValueTask<OrderVM> CreateAsync(OrderVM model, string user, DateTime date);
        ValueTask<OrderVM> UpdateAsync(OrderVM model, string user, DateTime date);
        ValueTask<OrderVM> DeleteAsync(OrderVM model, string user, DateTime date);
        ValueTask<Order> PostAsync(Guid orderId, string user, DateTime date);
        ValueTask<Order> UnpostAsync(Guid orderId, string user, DateTime date);
    }

    public class OrderService : BaseValidator, IOrderService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<OrderVM> _orderVmExceptionService = new ExceptionService<OrderVM>();
        private readonly IExceptionService<Order> _orderExceptionService = new ExceptionService<Order>();
        private IAllFieldService _allFieldService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        public OrderService(AppManEntities db)
        {
            _db = db;
            _allFieldService = new AllFieldService(_db);
            _getDisplayName = propertyName => Utility.GetDisplayName<OrderVM>(propertyName);
        }

        public IQueryable<OrderVM> GetAll() => _orderVmExceptionService.TryCatch(() =>
        {
            var data = _db.Orders.AsNoTracking()
                .Select(s => new OrderVM
                {
                    Id = s.Id,
                    PoNo = s.PoNo,
                    PrId = s.PrId,
                    PrDate = s.Request.PrDate,
                    PoDate = s.PoDate,
                    PoMode = s.PoMode,
                    PoModeDesc = _db.Codextns.Where(w => w.Code == s.PoMode && w.CodeMast.Code == "PROC-MODE").FirstOrDefault().Description,
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
            var data = _db.Orders.AsNoTracking()
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
            return await _db.Orders.FindAsync(orderId);
        });

        public async ValueTask<bool> GetAnyPoNoAsync(Guid id, string poNo)
        {
            return await _db.Orders.AnyAsync(a => a.Id != id && a.PoNo == poNo);
        }

        public ValueTask<Order> GetByPoNoAsync(string poNo) => _orderExceptionService.TryCatch(async () =>
        {
            return await _db.Orders.Where(w => w.PoNo == poNo).FirstOrDefaultAsync();
        });

        public async ValueTask<bool> GetAnyParsAsync(Guid id)
        {
            return await _db.PARs.AnyAsync(a => a.OrderId == id);
        }

        public async ValueTask<bool> GetAnyAirsAsync(Guid id)
        {
            return await _db.AIRs.AnyAsync(a => a.OrderId == id);
        }

        public async ValueTask<bool> IsPostedAsync(Guid orderId)
        {
            var entity = await _db.Orders.FindAsync(orderId);
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }

        public async ValueTask<int> GetNotPostedAsync(DateTime asOf)
        {
            return await _db.Orders.Where(w => w.PoDate <= asOf && w.PostedDt == null).CountAsync();
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
            var requestItems = _db.RequestItems.Include(i => i.RisItem)
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
                
                entity.OrderItems.Add(orderItem);
            }

            // Unit Groups
            var unitGroups = await _db.RequestItemUnitGroups.Include(i => i.RequestItemUnitGroupDescriptions).Where(w => w.PrId == model.PrId).OrderBy(o => o.InsertedDt).ToListAsync();
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

                    var requestItemUnitGroupDescriptionItems = await _db.RequestItemUnitGroupDescriptionItems.Where(w => w.RequestItemUnitGroupDescriptionId == unitGroupDescription.Id).OrderBy(o => o.InsertedDt).ToListAsync();
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

            _db.Orders.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<OrderVM> UpdateAsync(OrderVM model, string user, DateTime date) => _orderVmExceptionService.TryCatch(async () =>
        {
            await ValidateOnUpdate(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.Orders.FindAsync(model.Id);

            // if there's a change of request item
            if (entity.PrId != model.PrId)
            {
                var orderItems = _db.OrderItems.Where(w => w.OrderId == model.Id);
                await orderItems.ForEachAsync(f =>
                {
                    f.UpdatedBy = model.UpdatedBy;
                    f.UpdatedDt = model.UpdatedDt;
                });
                await _db.SaveChangesAsync();

                _db.OrderItems.RemoveRange(orderItems);
                await _db.SaveChangesAsync();

                // include items during add, PR Items not yet in Order Items
                var prItemList = _db.RequestItems.Include(i => i.RisItem)
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
                        PriceRate = prItem.PriceRate,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };                    

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

            _db.Orders.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<OrderVM> DeleteAsync(OrderVM model, string user, DateTime date) =>
        _orderVmExceptionService.TryCatch(async () =>
        {
            await ValidateOnDestroy(model);

            var unitGroups = _db.OrderItemUnitGroups.Where(w => w.OrderId == model.Id);
            if (unitGroups.Any())
            {
                _db.OrderItemUnitGroups.RemoveRange(unitGroups);
                await _db.SaveChangesAsync();
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.Orders.FindAsync(model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.Orders.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.Orders.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<Order> PostAsync(Guid orderId, string user, DateTime date) => _orderExceptionService.TryCatch(async () =>
        {
            //var entity = await db.Orders.FindAsync(orderId);
            var entity = await _db.Orders.Include(i => i.Request.RISs.RisItems).Where(w => w.Id == orderId).FirstOrDefaultAsync();
            if (entity == null)
            {
                throw new RecordNotFoundException(orderId);
            }

            await ValidateOnPost(entity);

            entity.PostedBy = user;
            entity.PostedDt = date;
            
            _db.Orders.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        public ValueTask<Order> UnpostAsync(Guid orderId, string user, DateTime date) => _orderExceptionService.TryCatch(async () =>
        {
            var entity = await _db.Orders.FindAsync(orderId);

            if (entity == null)
            {
                throw new RecordNotFoundException(orderId);
            }

            await ValidateOnUnpost(entity);

            entity.PostedBy = null;
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.Orders.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
            ///*
            //    * Delete the following records onUnpost:
            //    //* PsCardItems, Fields...
            //    //* PsCards --> if no PsItem                
            //*/
            //var orderItems = db.OrderItems.Include(i => i.RequestItem.RisItem.ItemCode.ItemType).Where(w => w.OrderId == orderId).ToList();

            //foreach (var orderItem in orderItems)
            //{

            //    if (Enum.TryParse(orderItem.RequestItem.RisItem.ItemCode.ItemType.Code, out Category category))
            //    {
            //        if (category == Category.D)
            //        {
            //            var psCardItems = db.PsCardItems.Where(w => w.OrderItemId == orderItem.Id);
            //            if (psCardItems.Any())
            //            {
            //                var psCardId = psCardItems.FirstOrDefault().PsCardId;
            //                // log updates
            //                await psCardItems.ForEachAsync(f =>
            //                {
            //                    f.UpdatedBy = user;
            //                    f.UpdatedDt = date;
            //                });
            //                await db.SaveChangesAsync();

            //                // delete all stockitems
            //                db.PsCardItems.RemoveRange(psCardItems);
            //                await db.SaveChangesAsync();

            //                if (!db.PsCardItems.Any(a => a.PsCardId == psCardId)) // no other  order item is using this item
            //                {
            //                    var psCard = await db.PsCards.Where(w => w.Id == psCardId).FirstOrDefaultAsync();
            //                    psCard.UpdatedBy = user;
            //                    psCard.UpdatedDt = date;

            //                    db.PsCards.Attach(psCard);
            //                    db.Entry(psCard).State = EntityState.Modified;
            //                    await db.SaveChangesAsync();

            //                    // delete stock during unpost if not used by other order item
            //                    db.PsCards.Remove(psCard);
            //                    db.Entry(psCard).State = EntityState.Deleted;
            //                    await db.SaveChangesAsync();
            //                }
            //            }
            //        }
            //    }                
            //}
        });

        private string NextPoNo(DateTime poDate)
        {
            string yyyy = poDate.Year.ToString().Trim();
            string mm = poDate.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var order = _db.Orders.Where(w => w.PoDate.Value.Year == poDate.Year).OrderByDescending(o => o.PoNo).FirstOrDefault();
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
            if (await _db.Orders.AnyAsync(a => a.PoNo == model.PoNo))
            {
                throw new RecordAlreadyExistsException(string.Format("PO Number {0} already exists", model.PoNo));
            }
            else
            {
                var pr = await _db.Requests.FindAsync(model.PrId);
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
            var order = await _db.Orders.FindAsync(model.Id);
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


            var pr = await _db.Requests.FindAsync(model.PrId);
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
            var order = await _db.Orders.FindAsync(model.Id);
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
            var ex = new InvalidModelException();
            if (!string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                var msg = $"Record already posted by {entity.PostedBy} on {entity.PostedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }

            var idList = await _db.OrderItems.Where(w => w.OrderId == entity.Id).GroupBy(g => g.RequestItem.Request.Id)
                .Select(s => s.Key).ToListAsync();

            foreach (var id in idList)
            {
                var request = await _db.Requests.FindAsync(id);
                if (request == null)
                {
                    throw new NotFoundException(id);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(request.SubmittedBy))
                    {
                        throw new RecordNotYetPostedException("Record is not yet posted.");
                    }
                }
            }

            var orderItems = await _db.OrderItems.Include(i => i.RequestItem.RisItem.ItemCode.ItemType).Where(w => w.OrderId == entity.Id).ToListAsync();        
            foreach (var orderItem in orderItems)
            {
                if (Enum.TryParse(orderItem.RequestItem.RisItem.ItemCode.ItemType.Code, out Category c))
                {
                    if (string.IsNullOrWhiteSpace(orderItem.Brand) && _allFieldService.IsBrandRequired(c))
                    {
                        var msg = $"Field is Required for {orderItem.RequestItem.RisItem.ItemName}";
                        ex.UpsertDataList(_getDisplayName(nameof(orderItem.Brand)), msg);                        
                    }                    
                }        
                if (string.IsNullOrWhiteSpace(orderItem.StockNo))
                {
                    throw new InvalidValueException("All items must have a valid Stock No.");
                }
            }
            ex.ThrowIfContainsErrors();
        }

        private async ValueTask ValidateOnUnpost(Order entity)
        {
            if (string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordNotYetPostedException(string.Format("PO Number {0} not yet posted..", entity.PoNo));
            }

            var airs = await _db.AIRs.Where(w => w.OrderId == entity.Id && w.PostedDt != null).ToListAsync();

            foreach (var air in airs)
            {
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