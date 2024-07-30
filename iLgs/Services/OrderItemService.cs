using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System.Data.Entity;
using iLgs.Exceptions;
using static iLgs.Models.CategoryEnum;

namespace iLgs.Services
{
    public class OrderItemService : IOrderItemService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly IExceptionService<OrderItemVM> _VmExceptionService = new ExceptionService<OrderItemVM>();
        private ICodextnService _codextnService;
        private IAllFieldService _allFieldService;
        private IOrderItemUnitGroupDescriptionItemService _orderItemUnitGroupDescriptionItemService;
        
        public OrderItemService(AppManEntities db)
        {
            this._db = db;
            _codextnService = new CodextnService(db);
            _allFieldService = new AllFieldService(db);
            _orderItemUnitGroupDescriptionItemService = new OrderItemUnitGroupDescriptionItemService(db);
        }

        public ValueTask<OrderItemVM> GetByIdAsync(Guid? id) => _VmExceptionService.TryCatch(async () =>
        {
            var data = await _db.OrderItems.Where(w => w.Id == id)
                .Select(s => new OrderItemVM
                {
                    Id = s.Id,
                    OrderId = s.OrderId,                    
                    RequestItemId = s.RequestItemId,
                    RisItemId = s.RequestItem.RisItem.Id,
                    ItemCode = s.RequestItem.RisItem.ItemCode.Code,
                    ItemType = s.RequestItem.RisItem.ItemCode.Description,
                    PsType = s.RequestItem.RisItem.ItemCode.ItemType.Code,
                    PsTypeDesc = s.RequestItem.RisItem.ItemCode.ItemType.Description,
                    //PsNo = s.RequestItem.RisItem.PsNo,
                    PsNo = s.StockNo,
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
            var data = _db.OrderItems.Where(w => w.OrderId == poId)
                .Select(s => new OrderItemVM
                {
                    Id = s.Id,
                    OrderId = s.OrderId,
                    RequestItemId = s.RequestItemId,
                    RisItemId = s.RequestItem.RisItem.Id,
                    ItemCode = s.RequestItem.RisItem.ItemCode.Code,
                    ItemType = s.RequestItem.RisItem.ItemCode.Description,
                    PsType = s.RequestItem.RisItem.ItemCode.ItemType.Code,
                    PsTypeDesc = s.RequestItem.RisItem.ItemCode.ItemType.Description,
                    //PsNo = s.RequestItem.RisItem.PsNo,
                    PsNo = s.StockNo,
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
            return await _db.PARItems.AnyAsync(a => a.OrderItemId == id);
        }

        public async ValueTask<bool> GetAnyAirItemsAsync(Guid id)
        {
            return await _db.AIRItems.AnyAsync(a => a.OrderItemId == id);
        }

        private OrderItemVM ValidateBrand(OrderItemVM model)
        {
            if (Enum.TryParse(model.PsType, out Category c))
            {
                if (string.IsNullOrWhiteSpace(model.Brand) && _allFieldService.IsBrandRequired(c))
                {
                    throw new RequiredFieldException(nameof(model.Brand));
                }                                   
            }
            return model;
        }

        public ValueTask<OrderItemVM> CreateAsync(OrderItemVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            model = ValidateBrand(model);

            var requestItem = await _db.RequestItems.Include(i => i.RisItem.AllField)
                .Include(i => i.RisItem.ItemCode)
                .Where(w => w.Id == model.RequestItemId).FirstOrDefaultAsync();
            if (requestItem == null)
            {
                throw new RecordRelationshipException("Could not find request item this record!");
            }

            
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;
            //model.PsNo = _allFieldService.GetStockNo(model.PsType);
            model.StockName = StockNameAsync(model, requestItem.RisItemId);
            
            OrderItem entity = new OrderItem()
            {
                Id = model.Id,
                OrderId = model.OrderId,
                RequestItemId = model.RequestItemId,
                //StockNo = model.PsNo,
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
                UpdatedDt = model.UpdatedDt,
                RequestItem = requestItem
            };

            entity = SetItemEntity(entity, model);
            model.PsNo = requestItem.RisItem.ItemCode.Code  + _allFieldService.GetStockNo(entity.RequestItem.RisItem.AllField, model.PsType, model.ItemCode);
            entity.StockNo = model.PsNo;

            _db.OrderItems.Add(entity);
            await _db.SaveChangesAsync();

            // include unit group if any
            // check if requestItemId in RequestItemUnitGroup
            IOrderItemUnitGroupService orderItemUnitGroupService = new OrderItemUnitGroupService(_db);
            IOrderItemUnitGroupDescriptionService orderItemUnitGroupDescriptionService = new OrderItemUnitGroupDescriptionService(_db);
            IOrderItemUnitGroupDescriptionItemService orderItemUnitGroupDescriptionItemService = new OrderItemUnitGroupDescriptionItemService(_db);

            var requestItemUnitGroupDescriptionItem = await _db.RequestItemUnitGroupDescriptionItems
                .Include(i => i.RequestItemUnitGroupDescription.RequestItemUnitGroup).Where(w => w.RequestItemId == model.RequestItemId)
                .FirstOrDefaultAsync();
            if (requestItemUnitGroupDescriptionItem != null)
            {
                var orderItemUnitGroupDescriptionItem = await _db.OrderItemUnitGroupDescriptionItems
                    .Where(w => w.RequestItemUnitGroupDescriptionItemId == requestItemUnitGroupDescriptionItem.RequestItemUnitGroupDescriptionId)
                    .FirstOrDefaultAsync();
                if (orderItemUnitGroupDescriptionItem == null)
                {
                    var orderItemUnitGroupDescription = await _db.OrderItemUnitGroupDescriptions
                        .Where(w => w.RequestItemUnitGroupDescriptionId == requestItemUnitGroupDescriptionItem.RequestItemUnitGroupDescriptionId)
                        .FirstOrDefaultAsync();
                    if (orderItemUnitGroupDescription == null)
                    {
                        var orderItemUnitGroup = await _db.OrderItemUnitGroups
                            .Where(w => w.RequestItemUnitGroupId == requestItemUnitGroupDescriptionItem.RequestItemUnitGroupDescription.RequestItemUnitGroupId)
                            .FirstOrDefaultAsync();
                        if (orderItemUnitGroup == null)
                        {
                            var orderItemUnitGroupVM = new OrderItemUnitGroupVM()
                            {
                                OrderId = model.OrderId,
                                RequestItemUnitGroupId = requestItemUnitGroupDescriptionItem.RequestItemUnitGroupDescription.RequestItemUnitGroupId,
                                UnitCost = model.UnitCost,
                                TotalCost = model.Amount
                            };
                            orderItemUnitGroupVM = await orderItemUnitGroupService.CreateAsync(orderItemUnitGroupVM, user, date);

                            var orderItemUnitGroupDescriptionVM = new OrderItemUnitGroupDescriptionVM()
                            {
                                OrderItemUnitGroupId = orderItemUnitGroupVM.Id,
                                RequestItemUnitGroupDescriptionId = requestItemUnitGroupDescriptionItem.RequestItemUnitGroupDescriptionId
                            };
                            orderItemUnitGroupDescriptionVM = await orderItemUnitGroupDescriptionService.CreateAsync(orderItemUnitGroupDescriptionVM, user, date);

                            var orderItemUnitGroupDescriptionItemVM = new OrderItemUnitGroupDescriptionItemVM()
                            {
                                OrderItemUnitGroupDescriptionId = orderItemUnitGroupDescriptionVM.Id,
                                RequestItemUnitGroupDescriptionItemId = requestItemUnitGroupDescriptionItem.Id,
                                OrderItemId = model.Id
                            };
                            orderItemUnitGroupDescriptionItemVM = await orderItemUnitGroupDescriptionItemService.CreateAsync(orderItemUnitGroupDescriptionItemVM, user, date);
                        }
                        else
                        {
                            var orderItemUnitGroupDescriptionVM = new OrderItemUnitGroupDescriptionVM()
                            {
                                OrderItemUnitGroupId = orderItemUnitGroup.Id,
                                RequestItemUnitGroupDescriptionId = requestItemUnitGroupDescriptionItem.RequestItemUnitGroupDescriptionId
                            };
                            orderItemUnitGroupDescriptionVM = await orderItemUnitGroupDescriptionService.CreateAsync(orderItemUnitGroupDescriptionVM, user, date);

                            var orderItemUnitGroupDescriptionItemVM = new OrderItemUnitGroupDescriptionItemVM()
                            {
                                OrderItemUnitGroupDescriptionId = orderItemUnitGroupDescriptionVM.Id,
                                RequestItemUnitGroupDescriptionItemId = requestItemUnitGroupDescriptionItem.Id,
                                OrderItemId = model.Id
                            };
                            orderItemUnitGroupDescriptionItemVM = await orderItemUnitGroupDescriptionItemService.CreateAsync(orderItemUnitGroupDescriptionItemVM, user, date);
                        }
                    }
                    else
                    {
                        var orderItemUnitGroupDescriptionItemVM = new OrderItemUnitGroupDescriptionItemVM()
                        {
                            OrderItemUnitGroupDescriptionId = orderItemUnitGroupDescription.Id,
                            RequestItemUnitGroupDescriptionItemId = requestItemUnitGroupDescriptionItem.Id,
                            OrderItemId = model.Id
                        };
                        orderItemUnitGroupDescriptionItemVM = await orderItemUnitGroupDescriptionItemService.CreateAsync(orderItemUnitGroupDescriptionItemVM, user, date);
                    }
                }
            }

            return model;
        });

        private OrderItem SetItemEntity(OrderItem entity, OrderItemVM model)
        {
            var allField = entity.RequestItem.RisItem.AllField;
            if (Enum.TryParse(model.PsType, out Category c))
            {
                if (_allFieldService.IsBrandRequired(c))
                {
                    allField.Brand = model.Brand;
                    allField.UpdatedBy = model.UpdatedBy;
                    allField.UpdatedDt = model.UpdatedDt;
                }                
            }

            return entity;
        }

        public ValueTask<OrderItemVM> DeleteAsync(OrderItemVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            await ValidateOnDelete(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var orderItemGroupDescriptionItem = await _orderItemUnitGroupDescriptionItemService.DeleteEmptyGroupsAsync(model.Id);            

            OrderItem entity = await _db.OrderItems.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.OrderItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.OrderItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();            

            return model;
        });

        public ValueTask<OrderItemVM> UpdateAsync(OrderItemVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            model = ValidateBrand(model);

            var requestItemId = _db.RequestItems.FindAsync(model.RequestItemId).Result?.RisItemId;
            if (requestItemId == null)
            {
                throw new RecordRelationshipException("Could not find request item this record!");
            }

            var risItemId = _db.RisItems.FindAsync(requestItemId).Result?.Id;
            if (risItemId == null)
            {
                throw new RecordRelationshipException("Cound not find RIS item for this record!");
            }

            //if (!(await _codextnService.IsValidCodeDescAsync("BRANDS", model.Brand)))
            //{
            //    throw new RecordRelationshipException("Brand is not valid!");
            //}

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            OrderItem entity = await _db.OrderItems
                .Include(i => i.RequestItem.RisItem.AllField)
                .Include(i => i.RequestItem.RisItem.ItemCode)
                .Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            entity.RequestItem.RisItem.AllField.Brand = model.Brand;            
            entity.RequestItemId = model.RequestItemId;
            entity.StockName = StockNameAsync(model, risItemId);
            entity.Brand = model.Brand;
            entity.Description = model.Description;
            entity.Qty = model.Qty;
            entity.UnitCost = model.UnitCost;
            entity.Amount = model.Amount;
            entity.PriceRate = model.PriceRate;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            entity = SetItemEntity(entity, model);
            model.PsNo = entity.RequestItem.RisItem.ItemCode.Code + _allFieldService.GetStockNo(entity.RequestItem.RisItem.AllField, model.PsType, model.ItemCode);
            entity.StockNo = model.PsNo;


            _db.OrderItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        private string StockNameAsync(OrderItemVM orderItem, Guid? risItemId) 
        {            
            string stockName = "";
                       
            //if (Enum.TryParse(orderItem.PsType, out Category category))
            //{
            //    if (category == Category.T)
            //    {
                 
            //    }
            //    else if (category == Category.D)
            //    {
            //        stockName = orderItem.Brand.Replace(" ", "").Trim();
            //        var fieldsMedicine = await _db.FieldsMedicines.FindAsync(risItemId);

            //        if (!string.IsNullOrWhiteSpace(fieldsMedicine.DosageForm))
            //        {
            //            stockName += fieldsMedicine.DosageForm.Substring(0, 3);
            //        }

            //        if (!string.IsNullOrWhiteSpace(fieldsMedicine.DosageStrength))
            //        {
            //            stockName += fieldsMedicine.DosageStrength.Replace(" ", "");
            //        }

            //        stockName += fieldsMedicine.GenericName.Replace(" ", "");
            //        stockName += orderItem.ItemCode.ToString(); ;
            //    }
            //    else if (category == Category.U)
            //    {
                    
            //    }
            //}

            return stockName;
        }
        
        private async ValueTask ValidateOnDelete(OrderItemVM model)
        {
            var postedBy = _db.Orders.FindAsync(model.OrderId).Result?.PostedBy;
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