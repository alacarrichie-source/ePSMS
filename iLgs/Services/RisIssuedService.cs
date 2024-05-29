using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services
{
    public class RisIssuedService : IRisIssuedService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions _exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RisIssuedVM> _vmExceptionService = new ExceptionService<RisIssuedVM>();
        private readonly IExceptionService<RisIssued> _exceptionService = new ExceptionService<RisIssued>();

        public RisIssuedService(AppManEntities db)
        {
            _db = db;
        }

        public ValueTask<RisIssuedVM> GetVmByIdAsync(Guid? id) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            var data = await _db.RisIssueds.Where(w => w.Id == id)
                .Select(s => new RisIssuedVM
                {
                    Id = s.Id,
                    RisItemId = s.RisItemId,
                    OrderItemId = s.OrderItemId,
                    IssuedTo = s.IssuedTo,
                    IssuedToPosition = s.IssuedToPosition,
                    IssuedDate = s.IssuedDate,
                    IssuedBy = s.IssuedBy,
                    IssuedByPosition = s.IssuedByPosition,
                    IssuedByDate = s.IssuedByDate,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    RefNo = s.RefNo,
                    RefDate = s.RefDate,
                    RefType = s.RefType,
                    InsertedDt = s.InsertedDt,
                    LocationId = s.LocationId,
                    OfficerId = s.OfficerId,
                    Location = s.Codextn.Description,                    
                    Officer = s.AccountableOfficer.Name,
                    PropNo = s.PropNo
                }).FirstOrDefaultAsync();
            return data;
        });

        public ValueTask<RisIssued> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatchAsync(async () =>
        {
            var data = await _db.RisIssueds.FindAsync(id);
            return data;
        });

        public IQueryable<RisIssuedVM> GetByRisItemId(Guid? risItemId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RisIssueds.Where(w => w.RisItemId == risItemId)
                .Select(s => new RisIssuedVM
                {
                    Id = s.Id,
                    RisItemId = s.RisItemId,
                    OrderItemId = s.OrderItemId,
                    IssuedTo = s.IssuedTo,
                    IssuedToPosition = s.IssuedToPosition,
                    IssuedDate = s.IssuedDate,
                    IssuedBy = s.IssuedBy,
                    IssuedByPosition = s.IssuedByPosition,
                    IssuedByDate = s.IssuedByDate,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    RefNo = s.RefNo,
                    RefDate = s.RefDate,
                    RefType = s.RefType,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt,
                    InsertedDt = s.InsertedDt,
                    LocationId = s.LocationId,
                    OfficerId = s.OfficerId,
                    Location = s.Codextn.Description,                    
                    Officer = s.AccountableOfficer.Name,
                    PropNo = s.PropNo
                });
            return data;
        });

        public IQueryable<RisIssuedVM> GetByPoNoStockNo(string poNo, string stockNo) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RisIssueds.Where(w => w.RisItem.RequestItems.Any(a => a.OrderItems.Any(o => o.Order.PoNo == poNo && o.StockNo.Contains(stockNo))))
                .Select(s => new RisIssuedVM
                {
                    Id = s.Id,
                    RisItemId = s.RisItemId,
                    OrderItemId = s.OrderItemId,
                    IssuedTo = s.IssuedTo,
                    IssuedToPosition = s.IssuedToPosition,
                    IssuedDate = s.IssuedDate,
                    IssuedBy = s.IssuedBy,
                    IssuedByPosition = s.IssuedByPosition,
                    IssuedByDate = s.IssuedByDate,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    RefNo = s.RefNo,
                    RefDate = s.RefDate,
                    RefType = s.RefType,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt,
                    InsertedDt = s.InsertedDt,
                    LocationId = s.LocationId,
                    OfficerId = s.OfficerId,
                    Location = s.Codextn.Description,                    
                    Officer = s.AccountableOfficer.Name,
                    PropNo = s.PropNo
                });
            return data;
        });

        public IQueryable<RisIssuedVM> GetByStockNo(string stockNo) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RisIssueds.Where(w => w.RisItem.RequestItems.Any(a => a.OrderItems.Any(o => o.StockNo == stockNo)))
                .Select(s => new RisIssuedVM
                {
                    Id = s.Id,
                    RisItemId = s.RisItemId,
                    OrderItemId = s.OrderItemId,
                    IssuedTo = s.IssuedTo,
                    IssuedToPosition = s.IssuedToPosition,
                    IssuedDate = s.IssuedDate,
                    IssuedBy = s.IssuedBy,
                    IssuedByPosition = s.IssuedByPosition,
                    IssuedByDate = s.IssuedByDate,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    RefNo = s.RefNo,
                    RefDate = s.RefDate,
                    RefType = s.RefType,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt,
                    InsertedDt = s.InsertedDt,
                    LocationId = s.LocationId,
                    OfficerId = s.OfficerId,
                    Location = s.Codextn.Description,                    
                    Officer = s.AccountableOfficer.Name,
                    PropNo = s.PropNo
                });
            return data;
        });

        private async ValueTask ValidateFieldsAsync(RisIssuedVM model)
        {
            if (model.LocationId == null)
            {
                throw new InvalidValueException("Location is required!");
            }

            if (model.IssuedDate == null)
            {
                throw new InvalidValueException("Issued Date is required!");
            }

            if (model.Qty == 0)
            {
                throw new InvalidValueException("Quantity is required!");
            }            

            var rsmiDate = await _db.RSMIs.MaxAsync(m => m.Date);
            if (rsmiDate != null && rsmiDate > model.IssuedDate)
            {
                throw new InvalidValueException(string.Format("Date issued must be after the last RSMI date on {0}", rsmiDate.Value.ToShortDateString()));
            }
        }

        public ValueTask<RisIssuedVM> CreateAsync(RisIssuedVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            await ValidateFieldsAsync(model);

            var totalQtyIssued = (await _db.RisItems.FindAsync(model.RisItemId))?.QtyRequest ?? 0;
            var qtyIssued = await _db.RisIssueds.Where(w => w.RisItemId == model.RisItemId).SumAsync(s => s.Qty) ?? 0;
            var qtyBalance = totalQtyIssued - qtyIssued;
            if (model.Qty > qtyBalance)
            {
                throw new InvalidValueException(string.Format("Quantity must not exceed the remaing balance of {0}", qtyBalance));
            }

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            RisIssued entity = new RisIssued()
            {
                Id = model.Id,
                RisItemId = model.RisItemId,
                OrderItemId = model.OrderItemId,
                IssuedDate = model.IssuedDate,
                IssuedBy = model.IssuedBy,
                Qty = model.Qty,
                Amount = model.Amount,
                IssuedTo = model.IssuedTo,
                OfficerId = model.OfficerId,
                LocationId = model.LocationId,
                IssuedToPosition = model.IssuedToPosition,
                IssuedByPosition = model.IssuedToPosition,
                IssuedByDate = model.IssuedByDate,
                RefNo = model.RefNo,
                RefDate = model.RefDate,
                RefType = model.RefType,
                PostedBy = model.PostedBy,
                PostedDt = model.PostedDt,
                PropNo = model.PropNo,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.RisIssueds.Add(entity);

            // update main QtyIssue aggregate
            var risItem = await _db.RisItems.FindAsync(model.RisItemId);
            risItem.QtyIssue = qtyIssued + model.Qty;
            _db.RisItems.Attach(risItem);
            _db.Entry(risItem).State = EntityState.Modified;

            await _db.SaveChangesAsync();            
            return model;
        });

        public ValueTask<RisIssuedVM> DeleteAsync(RisIssuedVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            if (_db.RSMIs.Any(a => a.Date == model.IssuedDate))
            {
                throw new RecordRelationshipException("Date Issued is already in RSMI, Cannot delete!");
            }

            RisIssued entity = await _db.RisIssueds.FindAsync(model.Id);
            if (!string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordRelationshipException("Record is already posted, Cannot delete!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RisIssueds.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();


            await DeleteStockItemIssuance(entity, user, date);

            _db.RisIssueds.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            await UpdateRisStockItems(model.RisItemId);

            return model;
        });

        public ValueTask<RisIssuedVM> UpdateAsync(RisIssuedVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {

            RisIssued entity = await _db.RisIssueds.FindAsync(model.Id);
            if (!string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordRelationshipException("Record is already posted, Cannot update!");
            }

            await ValidateFieldsAsync(model);

            var totalQtyIssued = _db.RisItems.Find(model.RisItemId)?.QtyRequest ?? 0;
            var qtyIssued = _db.RisIssueds.Where(w => w.RisItemId == model.RisItemId && w.Id != model.Id).Sum(s => s.Qty) ?? 0;
            var qtyBalance = totalQtyIssued - qtyIssued;
            if (model.Qty > qtyBalance)
            {
                throw new InvalidValueException(string.Format("Quantity must not exceed the remaing balance of {0}", qtyBalance));
            }
           
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.RisItemId = model.RisItemId;
            entity.OrderItemId = model.OrderItemId;
            entity.IssuedDate = model.IssuedDate;
            entity.IssuedBy = model.IssuedBy;
            entity.Qty = model.Qty;
            entity.Amount = model.Amount;
            entity.IssuedTo = model.IssuedTo;
            entity.OfficerId = model.OfficerId;
            entity.LocationId = model.LocationId;
            entity.IssuedToPosition = model.IssuedToPosition;
            entity.IssuedByPosition = model.IssuedToPosition;
            entity.IssuedByDate = model.IssuedByDate;
            entity.RefNo = model.RefNo;
            entity.RefDate = model.RefDate;
            entity.RefType = model.RefType;
            entity.PostedBy = model.PostedBy;
            entity.PostedDt = model.PostedDt;
            entity.PropNo = model.PropNo;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RisIssueds.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;

            // update main QtyIssue aggregate
            var risItem = await _db.RisItems.FindAsync(model.RisItemId);
            risItem.QtyIssue = qtyIssued + model.Qty;
            _db.RisItems.Attach(risItem);
            _db.Entry(risItem).State = EntityState.Modified;

            await _db.SaveChangesAsync();
            return model;
        });

        private async ValueTask UpdateRisStockItems(Guid? risItemId)
        {
            var qtyIssued = await _db.RisIssueds.Where(w => w.RisItemId == risItemId).SumAsync(s => s.Qty) ?? 0;

            var qtyReceived = await _db.AIRItems.Where(w => w.OrderItem.RequestItem.RisItem.Id == risItemId).SumAsync(s => s.Qty) ?? 0;
            var orderItems = await _db.OrderItems.Where(w => w.RequestItem.RisItem.Id == risItemId).ToListAsync();
            foreach (var orderItem in orderItems)
            {
                var stockItems = _db.PsCardItems.Where(w => w.OrderItemId == orderItem.Id);
                await stockItems.ForEachAsync(f => { f.QtyIss = (int?)qtyIssued; f.Qty = (int?)qtyReceived; f.QtyBal = (int)qtyReceived - qtyIssued; });
            }

            var risItem = await _db.RisItems.FindAsync(risItemId);
            risItem.QtyIssue = qtyIssued;
            _db.RisItems.Attach(risItem);
            _db.Entry(risItem).State = EntityState.Modified;

            await _db.SaveChangesAsync();
        }

        private async ValueTask UpdateStockItemIssuance(RisIssued risIssued, string user, DateTime? date)
        {
            var orderItem = await _db.OrderItems.FindAsync(risIssued.OrderItemId);
            var stockItemIssuance = await _db.PsCardItemIssuances.FirstOrDefaultAsync(f => f.RefIssuedId == risIssued.Id);
            if (stockItemIssuance == null)
            {
                var stockItem = await _db.PsCardItems.Where(w => w.OrderItemId == risIssued.OrderItemId).FirstOrDefaultAsync();
                stockItemIssuance = new PsCardItemIssuance()
                {
                    Id = Guid.NewGuid(),
                    PsCardItemId = stockItem.Id,
                    RefIssuedId = risIssued.Id,
                    LocationId = risIssued.LocationId,
                    OfficerId = risIssued.OfficerId,
                    IssuedTo = risIssued.IssuedTo,
                    IssuedDate = risIssued.IssuedDate,
                    Qty = risIssued.Qty,
                    Amount = risIssued.Amount,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };
                _db.PsCardItemIssuances.Add(stockItemIssuance);
                _db.Entry(stockItemIssuance).State = EntityState.Added;
            }
            else
            {
                stockItemIssuance.LocationId = risIssued.LocationId;
                stockItemIssuance.OfficerId = risIssued.OfficerId;
                stockItemIssuance.IssuedTo = risIssued.IssuedTo;
                stockItemIssuance.IssuedDate = risIssued.IssuedDate;
                stockItemIssuance.Qty = risIssued.Qty;
                stockItemIssuance.Amount = risIssued.Amount;
                stockItemIssuance.UpdatedBy = user;
                stockItemIssuance.UpdatedDt = date;
                _db.PsCardItemIssuances.Attach(stockItemIssuance);
                _db.Entry(stockItemIssuance).State = EntityState.Modified;
            }

            await _db.SaveChangesAsync();
        }

        private async ValueTask UpdatePsCard(Guid orderId, string user, DateTime? date)
        {
            var orderItemGroups = await _db.Database.SqlQuery<OrderItemGroupVM>("Exec OrderService_GetOrderItemGroup {0}", orderId).ToListAsync();
            // create stock for each group
            foreach (var oig in orderItemGroups)
            {
                // Post the OrderItems under the stocks having the same PsCodeId
                var orderItemList = await _db.OrderItems
                    .Include(i => i.Order)
                    .Include(i => i.RequestItem.RisItem.RISs)
                    .Where(w => w.OrderId == orderId
                        && w.StockNo == oig.StockNo
                        && w.StockName == oig.StockName
                        && w.Description == oig.Description
                        && w.RequestItem.RisItem.RISs.Fund == oig.Fund).ToListAsync();

                foreach (var orderItem in orderItemList)
                {
                    var psCard = await _db.PsCards.Include(i => i.PsCardItems).Where(w => w.PsNo == oig.StockNo && w.Fund == oig.Fund && w.Unit == oig.Unit).FirstOrDefaultAsync();
                    if (psCard == null)
                    {
                        var psCardId = Guid.NewGuid();
                        psCard = new PsCard()
                        {
                            Id = psCardId,
                            ItemCodeId = oig.ItemCodeId,
                            Fund = oig.Fund,
                            Description = oig.Description,
                            CardCategory = "X",
                            PsNo = oig.StockNo,
                            PsName = oig.StockName,
                            Amount = orderItem.Amount,
                            SubAccountCode = orderItem.RequestItem.RisItem.SubAccountCode,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };


                        FieldsMedicine fieldsMedicine = null;
                        FieldsVehicle fieldsVehicle = null;
                        FieldsOther fieldsOther = null;

                        if (Enum.TryParse(oig.ItemTypeCode, out Category category))
                        {
                            if (category == Category.D)
                            {
                                var risFieldsMedicine = await _db.FieldsMedicines.AsNoTracking().Where(w => w.Id == orderItem.RequestItem.RisItemId).FirstOrDefaultAsync();
                                if (risFieldsMedicine != null)
                                {
                                    //fieldsMedicine = risFieldsMedicine;
                                    //fieldsMedicine.Id = psCardId;
                                    //psCard.FieldsMedicine = risFieldsMedicine;                                    
                                    fieldsMedicine = new FieldsMedicine()
                                    {
                                        Id = psCardId,
                                        GenericName = risFieldsMedicine.GenericName,
                                        DosageForm = risFieldsMedicine.DosageForm,
                                        DosageStrength = risFieldsMedicine.DosageStrength,
                                        Brand = orderItem.Brand
                                    };

                                    psCard.FieldsMedicine = fieldsMedicine;
                                }
                            }
                            else if (category == Category.T)
                            {
                                fieldsVehicle = await _db.FieldsVehicles.AsNoTracking().Where(w => w.Id == orderItem.RequestItem.RisItemId).FirstOrDefaultAsync();
                                if (fieldsVehicle != null)
                                {
                                    fieldsVehicle.Id = psCardId;
                                    psCard.FieldsVehicle = fieldsVehicle;
                                }
                            }
                        }
                        var office = orderItem.RequestItem.RisItem.RISs.Office;
                        var deptId = _db.Codextns.Where(w => w.CodeMast.Code == "DEPARTMENTS" && w.Description.Trim() == office).FirstOrDefault()?.Id;

                        var psCardItem = await _db.PsCardItems.Where(w => w.OrderItemId == orderItem.Id).FirstOrDefaultAsync();
                        if (psCardItem == null)
                        {
                            psCardItem = new PsCardItem()
                            {
                                Id = Guid.NewGuid(),
                                PsCardId = psCard.Id,
                                OrderItemId = orderItem.Id,
                                PoDate = orderItem.Order.PoDate,
                                PoNo = orderItem.Order.PoNo,
                                DeptId = deptId,
                                Qty = (int)orderItem.Qty,
                                QtyIss = 0,
                                QtyBal = (int)orderItem.Qty,
                                Unit = orderItem.RequestItem.RisItem.Unit,
                                UnitCost = orderItem.UnitCost,
                                Amount = orderItem.Amount,
                                TranType = "I",
                                InsertedBy = user,
                                InsertedDt = date,
                                UpdatedBy = user,
                                UpdatedDt = date
                            };
                            psCard.PsCardItems.Add(psCardItem);

                        }

                        _db.PsCards.Add(psCard);
                        await _db.SaveChangesAsync();
                    }
                }
            }
        }

        private async ValueTask DeleteStockItemIssuance(RisIssued model, string user, DateTime? date)
        {
            var entity = await _db.PsCardItemIssuances.FirstOrDefaultAsync(f => f.RefIssuedId == model.Id);
            if (entity == null)
            {
                return;
            }

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItemIssuances.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCardItemIssuances.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();
        }
        
    }
}