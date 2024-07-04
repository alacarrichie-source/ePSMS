using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.CategoryEnum;

namespace iLgs.Services
{
    public class AirService : IAirService
    {
        private readonly AppManEntities db = new AppManEntities();
        private readonly IExceptionService<AIR_VM> _VmExceptionService = new ExceptionService<AIR_VM>();
        private readonly IExceptionService<AIR> _ExceptionService = new ExceptionService<AIR>();

        public AirService(AppManEntities db)
        {
            this.db = db;
        }

        public IQueryable<AIR_VM> GetAll() => _VmExceptionService.TryCatch(() =>
        {
            var data = db.AIRs
                .AsNoTracking()
                .Select(s => new AIR_VM
                {
                    Id = s.Id,
                    OrderId = s.OrderId,
                    PoNo = s.Order.PoNo,
                    Supplier = s.Order.Supplier.BusinessName,
                    PoDate = s.Order.PoDate,
                    Department = s.Order.DeliveryPlace,
                    Fund = s.Fund,
                    AIRNo = s.AIRNo,
                    AIRDate = s.AIRDate,
                    InvoiceNo = s.InvoiceNo,
                    InvoiceDate = s.InvoiceDate,
                    AcceptedDate = s.AcceptedDate,
                    IsComplete = s.IsComplete,
                    IsPartial = s.IsPartial,
                    Custodian = s.Custodian,
                    InspectedDate = s.InspectedDate,
                    IsInspected = s.IsInspected,
                    Officer = s.Officer,
                    Remarks = s.Remarks,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt
                });
            return data;
        });

        public async ValueTask<bool> GetAnyAirNoAsync(Guid airId, string airNo)
        {
            return await db.AIRs.AnyAsync(a => a.Id != airId && a.AIRNo == airNo);
        }

        public ValueTask<AIR> GetByIdAsync(Guid id) => _ExceptionService.TryCatch(async () =>
        {
            return await db.AIRs.FindAsync(id);
        });

        public ValueTask<AIR_VM> GetVmByIdAsync(Guid id) => _VmExceptionService.TryCatch(async () =>
        {
            var data = await db.AIRs
                .Where(w => w.Id == id)
                .Select(s => new AIR_VM
                {
                    Id = s.Id,
                    OrderId = s.OrderId,
                    PoNo = s.Order.PoNo,
                    Supplier = s.Order.Supplier.BusinessName,
                    PoDate = s.Order.PoDate,
                    Department = s.Order.DeliveryPlace,
                    Fund = s.Fund,
                    AIRNo = s.AIRNo,
                    AIRDate = s.AIRDate,
                    InvoiceNo = s.InvoiceNo,
                    InvoiceDate = s.InvoiceDate,
                    AcceptedDate = s.AcceptedDate,
                    IsComplete = s.IsComplete,
                    IsPartial = s.IsPartial,
                    Custodian = s.Custodian,
                    InspectedDate = s.InspectedDate,
                    IsInspected = s.IsInspected,
                    Officer = s.Officer,
                    Remarks = s.Remarks,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt
                }).FirstOrDefaultAsync();
            return data;
        });

        public ValueTask<AIR> GetByAirNoAsync(string airNo) => _ExceptionService.TryCatch(async () =>
        {
            return await db.AIRs.Where(w => w.AIRNo == airNo).FirstOrDefaultAsync();
        });

        public async ValueTask<bool> IsPostedAsync(Guid airId)
        {
            var entity = await db.AIRs.FindAsync(airId);
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }

        //public async Task<bool> IsPrPostedAsync(Guid risId)
        //{
        //    var pr = await db.Requests.Where(a => a.RisId == risId).FirstOrDefaultAsync();
        //    if (pr != null)
        //    {
        //        return !string.IsNullOrWhiteSpace(pr.SubmittedBy);
        //    }
        //    return false;
        //}

        public ValueTask PostAsync(Guid airId, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            var entity = await db.AIRs.Include(i => i.AIRItems).FirstOrDefaultAsync(f => f.Id == airId);
            if (entity == null)
            {
                throw new RecordNotFoundException(airId);
            }

            if (entity.AIRItems.Any(a => string.IsNullOrEmpty(a.Remarks)))
            {
                throw new InvalidValueException("Remarks is required for all items!");
            }

            entity.PostedBy = user;
            entity.PostedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.AIRs.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;

            List<Guid> psCardIdList = new List<Guid>(); 
            var orderItemGroups = await db.Database.SqlQuery<OrderItemGroupVM>("Exec OrderService_GetOrderItemGroup {0}", entity.OrderId).ToListAsync();
            // create stock for each group
            foreach (var oig in orderItemGroups)
            {
                // Post the OrderItems under the stocks having the same PsCodeId
                var orderItemList = await db.OrderItems
                    .Include(i => i.Order)
                    .Include(i => i.RequestItem.RisItem.RISs)
                    .Where(w => w.OrderId == entity.OrderId
                        && w.StockNo == oig.StockNo
                        && w.StockName == oig.StockName
                        && w.Description == oig.Description
                        && w.RequestItem.RisItem.RISs.Fund == oig.Fund).ToListAsync();

                foreach (var orderItem in orderItemList)
                {
                    var psCard = await db.PsCards.Include(i => i.PsCardItems)
                        .Include(i => i.AllField)
                        .Where(w => w.PsNo == oig.StockNo && w.Fund == oig.Fund
                            && w.FromDonation != true
                        //&& w.Unit == oig.Unit
                        ).FirstOrDefaultAsync();

                    var office = orderItem.RequestItem.RisItem.RISs.Office;
                    var deptId = db.Codextns.Where(w => w.CodeMast.Code == "DEPARTMENTS" && w.Description.Trim() == office).FirstOrDefault()?.Id;

                    if (psCard == null)
                    {
                        var psCardId = Guid.NewGuid();
                        psCard = new PsCard()
                        {
                            Id = psCardId,
                            ItemCodeId = oig.ItemCodeId,
                            Fund = oig.Fund,
                            //Description = oig.Description,                            
                            Description = "Please see attachment.",
                            PsNo = oig.StockNo,
                            PsName = oig.StockName,
                            Amount = orderItem.Amount,
                            SubAccountCode = orderItem.RequestItem.RisItem.SubAccountCode,
                            CardCategory = oig.CardCategory,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };

                        AllField allField = null;
                        AllField risAllField = await db.AllFields.AsNoTracking().Where(w => w.Id == orderItem.RequestItem.RisItemId).FirstOrDefaultAsync();
                        if (risAllField != null)
                        {
                            allField = new AllField()
                            {
                                Id = psCardId,
                                AcqMode = risAllField.AcqMode,
                                InvDist = risAllField.InvDist,
                                GenericName = risAllField.GenericName,
                                DosageStrength = risAllField.DosageStrength,
                                DosageForm = risAllField.DosageForm,
                                DosageVolume = risAllField.DosageVolume,
                                Others = risAllField.Others,
                                Brand = risAllField.Brand,
                                Multipliers = risAllField.Multipliers,
                                Model_ = risAllField.Model_,
                                Area = risAllField.Area,
                                Barangay = risAllField.Barangay,
                                DateSale = risAllField.DateSale,
                                DateDonation = risAllField.DateDonation,
                                DateAcquisition = risAllField.DateAcquisition,
                                DateConstruction = risAllField.DateConstruction,
                                AreaSoldDonated = risAllField.AreaSoldDonated,
                                PricePerSqm = risAllField.PricePerSqm,
                                AcqCost = risAllField.AcqCost,
                                VendorDonor = risAllField.VendorDonor,
                                Type = risAllField.Type,
                                Dimension = risAllField.Dimension,
                                Size = risAllField.Size,
                                Weight = risAllField.Weight,
                                Materials = risAllField.Materials,
                                Capacity = risAllField.Capacity,
                                Color = risAllField.Color,
                                SerialNo = risAllField.SerialNo,
                                PropNo = risAllField.PropNo,
                                PlateNo = risAllField.PlateNo,
                                BodyNo = risAllField.BodyNo,
                                MVFileNo = risAllField.MVFileNo,
                                InsertedBy = user,
                                InsertedDt = date,
                                UpdatedBy = user,
                                UpdatedDt = date
                            };

                            psCard.AllField = allField;
                        }
                        

                        var psCardItem = await db.PsCardItems.Where(w => w.OrderItemId == orderItem.Id).FirstOrDefaultAsync();
                        if (psCardItem == null)
                        {
                            psCardItem = new PsCardItem()
                            {
                                Id = Guid.NewGuid(),
                                PsCardId = psCard.Id,
                                OrderItemId = orderItem.Id,
                                PoDate = orderItem.Order.PoDate,
                                PoNo = orderItem.Order.PoNo,
                                AirNo = entity.AIRNo,
                                AirDate = entity.AIRDate,
                                DeptId = deptId,
                                Qty = (int)orderItem.Qty,
                                QtyIss = 0,
                                QtyBal = (int)orderItem.Qty,
                                Unit = orderItem.RequestItem.RisItem.Unit,
                                UnitCost = orderItem.UnitCost,
                                Amount = orderItem.Amount,
                                PriceRate = orderItem.PriceRate,
                                TranType = "I",
                                Remarks = oig.Remarks,
                                InsertedBy = user,
                                InsertedDt = date,
                                UpdatedBy = user,
                                UpdatedDt = date,
                                Description = oig.Description
                            };
                            psCard.PsCardItems.Add(psCardItem);
                        }
                        db.PsCards.Add(psCard);
                    }
                    else
                    {
                        var psCardItem = await db.PsCardItems.Where(w => w.OrderItemId == orderItem.Id).FirstOrDefaultAsync();
                        if (psCardItem == null)
                        {
                            psCardItem = new PsCardItem()
                            {
                                Id = Guid.NewGuid(),
                                PsCardId = psCard.Id,
                                OrderItemId = orderItem.Id,
                                PoDate = orderItem.Order.PoDate,
                                PoNo = orderItem.Order.PoNo,
                                AirNo = entity.AIRNo,
                                AirDate = entity.AIRDate,
                                DeptId = deptId,
                                Qty = (int)orderItem.Qty,
                                QtyIss = 0,
                                QtyBal = (int)orderItem.Qty,
                                Unit = orderItem.RequestItem.RisItem.Unit,
                                UnitCost = orderItem.UnitCost,
                                Amount = orderItem.Amount,
                                PriceRate = orderItem.PriceRate,
                                TranType = "I",
                                Remarks = oig.Remarks,
                                InsertedBy = user,
                                InsertedDt = date,
                                UpdatedBy = user,
                                UpdatedDt = date,
                                Description = oig.Description
                            };
                            db.PsCardItems.Add(psCardItem);                            
                        }
                    }
                    psCardIdList.Add(psCard.Id);
                }
            }            

            await db.SaveChangesAsync();
            
            foreach (var psCardId in psCardIdList)
            {
                var psCardItemList = db.PsCardItems.Where(w => w.PsCardId == psCardId).ToList();
                foreach (var psCardItem in psCardItemList)
                {
                    // search unit group if any
                    var orderItemUnitGroupDescriptionItem = await db.OrderItemUnitGroupDescriptionItems
                        .Include(i => i.RequestItemUnitGroupDescriptionItem.RisItemUnitGroupDescriptionItem.RisItemUnitGroupDescription.RisItemUnitGroup)
                        .Include(i => i.OrderItemUnitGroupDescription.OrderItemUnitGroup)
                        .Where(w => w.OrderItemId == psCardItem.OrderItemId).FirstOrDefaultAsync();
                    if (orderItemUnitGroupDescriptionItem != null)
                    {
                        // search in psCard unit group
                        if (!await db.PsCardItemUnitGroupDescriptionItems.Where(w => w.PsCardItemId == psCardItem.Id).AnyAsync())
                        {
                            var psCardItemUnitGroup = await db.PsCardItemUnitGroups.Include(i => i.PsCardItemUnitGroupDescriptions).Where(w => w.PoNo == psCardItem.PoNo).FirstOrDefaultAsync();
                            if (psCardItemUnitGroup == null)
                            {
                                psCardItemUnitGroup = new PsCardItemUnitGroup()
                                {
                                    Id = Guid.NewGuid(),
                                    PoNo = psCardItem.PoNo,
                                    Qty = orderItemUnitGroupDescriptionItem.RequestItemUnitGroupDescriptionItem.RisItemUnitGroupDescriptionItem.RisItemUnitGroupDescription.RisItemUnitGroup.Qty,
                                    Unit = orderItemUnitGroupDescriptionItem.RequestItemUnitGroupDescriptionItem.RisItemUnitGroupDescriptionItem.RisItemUnitGroupDescription.RisItemUnitGroup.Unit,
                                    UnitCost = orderItemUnitGroupDescriptionItem.OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost,
                                    TotalCost = orderItemUnitGroupDescriptionItem.OrderItemUnitGroupDescription.OrderItemUnitGroup.TotalCost,
                                    InsertedBy = user,
                                    InsertedDt = date,
                                    UpdatedBy = user,
                                    UpdatedDt = date
                                };
                                var psCardItemUnitGroupDescription = new PsCardItemUnitGroupDescription()
                                {
                                    Id = Guid.NewGuid(),
                                    UnitGroupId = psCardItemUnitGroup.Id,
                                    Description = orderItemUnitGroupDescriptionItem.RequestItemUnitGroupDescriptionItem.RisItemUnitGroupDescriptionItem.RisItemUnitGroupDescription.Description,
                                    InsertedBy = user,
                                    InsertedDt = date,
                                    UpdatedBy = user,
                                    UpdatedDt = date
                                };
                                var psCardItemUnitGroupDescriptionItem = new PsCardItemUnitGroupDescriptionItem()
                                {
                                    Id = Guid.NewGuid(),
                                    UnitGroupDescriptionId = psCardItemUnitGroupDescription.Id,
                                    PsCardItemId = psCardItem.Id,
                                    InsertedBy = user,
                                    InsertedDt = date,
                                    UpdatedBy = user,
                                    UpdatedDt = date
                                };
                                psCardItemUnitGroupDescription.PsCardItemUnitGroupDescriptionItems.Add(psCardItemUnitGroupDescriptionItem);
                                psCardItemUnitGroup.PsCardItemUnitGroupDescriptions.Add(psCardItemUnitGroupDescription);
                                db.PsCardItemUnitGroups.Add(psCardItemUnitGroup);                                
                            }
                            else
                            {
                                // if with psCardItemUnitGroup, check UnitGroupDescription
                                var psCardItemUnitGroupDescription = psCardItemUnitGroup.PsCardItemUnitGroupDescriptions
                                    .Where(w => w.Description == orderItemUnitGroupDescriptionItem.RequestItemUnitGroupDescriptionItem.RisItemUnitGroupDescriptionItem.RisItemUnitGroupDescription.Description)
                                    .FirstOrDefault();
                                if (psCardItemUnitGroupDescription == null)
                                {
                                    psCardItemUnitGroupDescription = new PsCardItemUnitGroupDescription()
                                    {
                                        Id = Guid.NewGuid(),
                                        UnitGroupId = psCardItemUnitGroup.Id,
                                        Description = orderItemUnitGroupDescriptionItem.RequestItemUnitGroupDescriptionItem.RisItemUnitGroupDescriptionItem.RisItemUnitGroupDescription.Description,
                                        InsertedBy = user,
                                        InsertedDt = date,
                                        UpdatedBy = user,
                                        UpdatedDt = date
                                    };
                                    var psCardItemUnitGroupDescriptionItem = new PsCardItemUnitGroupDescriptionItem()
                                    {
                                        Id = Guid.NewGuid(),
                                        UnitGroupDescriptionId = psCardItemUnitGroupDescription.Id,
                                        PsCardItemId = psCardItem.Id,
                                        InsertedBy = user,
                                        InsertedDt = date,
                                        UpdatedBy = user,
                                        UpdatedDt = date
                                    };
                                    psCardItemUnitGroupDescription.PsCardItemUnitGroupDescriptionItems.Add(psCardItemUnitGroupDescriptionItem);
                                    db.PsCardItemUnitGroupDescriptions.Add(psCardItemUnitGroupDescription);                                                                        
                                }
                                else
                                {
                                    // if with UnitGroupDescription, check UnitGroupDescriptionItem
                                    var psCardItemUnitGroupDescriptionItem = psCardItemUnitGroupDescription.PsCardItemUnitGroupDescriptionItems
                                        .Where(w => w.PsCardItemId == psCardItem.Id).FirstOrDefault();
                                    if (psCardItemUnitGroupDescriptionItem == null)
                                    {
                                        psCardItemUnitGroupDescriptionItem = new PsCardItemUnitGroupDescriptionItem()
                                        {
                                            Id = Guid.NewGuid(),
                                            UnitGroupDescriptionId = psCardItemUnitGroupDescription.Id,
                                            PsCardItemId = psCardItem.Id,
                                            InsertedBy = user,
                                            InsertedDt = date,
                                            UpdatedBy = user,
                                            UpdatedDt = date
                                        };
                                        db.PsCardItemUnitGroupDescriptionItems.Add(psCardItemUnitGroupDescriptionItem);                                        
                                    }
                                }                                
                            }
                            await db.SaveChangesAsync();
                        }
                    }
                }
            }
        });

        public ValueTask UnpostAsync(Guid airId, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            var entity = await db.AIRs.FindAsync(airId);
            if (entity == null)
            {
                throw new RecordNotFoundException(airId);
            }

            if (await db.PsCardItemIssuances.AsNoTracking().AnyAsync(a => a.PsCardItem.OrderItemId == entity.OrderId))
            {
                throw new RecordRelationshipException("Items were already issued cannot unpost!");
            }

            if (await db.IcsParItems.AsNoTracking().AnyAsync(a => a.PsCardItem.OrderItemId == entity.OrderId))
            {
                throw new RecordRelationshipException("PAR/ICS already issued cannot unpost!");
            }

            /*
                * Delete the following records onUnpost:
                * PsCardItems, Fields...
                * PsCards --> if no PsItem                
            */

            var orderItems = db.OrderItems.Include(i => i.RequestItem.RisItem.ItemCode.ItemType).Where(w => w.OrderId == entity.OrderId).ToList();

            foreach (var orderItem in orderItems)
            {                
                var psCardItems = db.PsCardItems.Where(w => w.OrderItemId == orderItem.Id);
                if (psCardItems.Any())
                {
                    var psCardItem = psCardItems.FirstOrDefault();

                    // check unit groups           
                    var unitGroupDescriptionItems = db.PsCardItemUnitGroupDescriptionItems.Where(w => w.PsCardItemId == psCardItem.Id);
                    db.PsCardItemUnitGroupDescriptionItems.RemoveRange(unitGroupDescriptionItems);
                    await db.SaveChangesAsync();

                    var unitGroupDescriptions = db.PsCardItemUnitGroupDescriptions.Where(w => w.PsCardItemUnitGroup.PoNo == psCardItem.PoNo && !w.PsCardItemUnitGroupDescriptionItems.Any());
                    db.PsCardItemUnitGroupDescriptions.RemoveRange(unitGroupDescriptions);
                    await db.SaveChangesAsync();

                    var unitGroups = db.PsCardItemUnitGroups.Where(w => w.PoNo == psCardItem.PoNo && !w.PsCardItemUnitGroupDescriptions.Any());
                    db.PsCardItemUnitGroups.RemoveRange(unitGroups);
                    await db.SaveChangesAsync();

                    var psCardId = psCardItem.PsCardId;                    

                    // log updates
                    await psCardItems.ForEachAsync(f =>
                    {
                        f.UpdatedBy = user;
                        f.UpdatedDt = date;
                    });
                    await db.SaveChangesAsync();

                    // delete all stockitems
                    db.PsCardItems.RemoveRange(psCardItems);
                    await db.SaveChangesAsync();

                    if (!db.PsCardItems.Any(a => a.PsCardId == psCardId)) // no other  order item is using this item
                    {
                        var psCard = await db.PsCards.Include(i => i.AllField).Where(w => w.Id == psCardId).FirstOrDefaultAsync();
                        psCard.UpdatedBy = user;
                        psCard.UpdatedDt = date;

                        db.PsCards.Attach(psCard);
                        db.Entry(psCard).State = EntityState.Modified;
                        await db.SaveChangesAsync();

                        // delete stock during unpost if not used by other order item
                        db.PsCards.Remove(psCard);
                        db.Entry(psCard).State = EntityState.Deleted;
                        await db.SaveChangesAsync();
                    }
                }
            }

            entity.PostedBy = null;
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.AIRs.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();            
        });

        private ValueTask UpdatePsItem(AIR entity, string user, DateTime date, bool post) => _VmExceptionService.TryCatch(async () =>
        {
            var orderItemIdList = await db.AIRItems.Where(w => w.AirId == entity.Id).GroupBy(g => g.OrderItemId)
                .Select(s => s.Key).ToListAsync();
            foreach (var orderItemId in orderItemIdList)
            {
                decimal? qtyAccepted = 0;
                if (post)
                {
                    qtyAccepted = db.AIRItems.Where(w => w.OrderItemId == orderItemId).Sum(s => s.Qty);
                }
                var psCardItem = await db.PsCardItems.Where(w => w.OrderItemId == orderItemId).FirstOrDefaultAsync();
                if (psCardItem != null)
                {
                    psCardItem.AirNo = entity.AIRNo;
                    psCardItem.AirDate = entity.AIRDate;
                    psCardItem.Qty = (int)qtyAccepted;
                    psCardItem.QtyBal = (int)qtyAccepted - psCardItem.QtyIss;
                    psCardItem.UpdatedBy = user;
                    psCardItem.UpdatedDt = date;
                    db.PsCardItems.Attach(psCardItem);
                    db.Entry(psCardItem).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                }
            }
        });

        public ValueTask<AIR_VM> SaveAsync(AIR_VM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            if (model.Id == Guid.Empty || model.Id == null)
            {
                return await CreateAsync(model, user, date);
            }
            return await UpdateAsync(model, user, date);
        });

        public async ValueTask<AIR_VM> CreateAsync(AIR_VM model, string user, DateTime date)
        {
            await ValidateOnCreate(model);

            model.Id = (model.Id == Guid.Empty || model.Id == null) ? Guid.NewGuid() : model.Id;
            if (string.IsNullOrWhiteSpace(model.AIRNo))
            {
                model.AIRNo = NextAirNo((DateTime)model.AIRDate);
            }
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = new AIR()
            {
                Id = model.Id,
                Fund = model.Fund,
                AIRNo = model.AIRNo,
                AIRDate = model.AIRDate,
                OrderId = model.OrderId,
                //InvoiceNo = model.InvoiceNo ?? "",
                //InvoiceDate = model.InvoiceDate,
                AcceptedDate = model.AcceptedDate,
                IsComplete = model.IsComplete,
                IsPartial = model.IsPartial,
                Custodian = model.Custodian ?? "",
                InspectedDate = model.InspectedDate,
                IsInspected = model.IsInspected,
                Officer = model.Officer ?? "",
                Remarks = model.Remarks ?? "",
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            // include items during add
            var orderItems = db.OrderItems.Where(w => w.OrderId == model.OrderId).ToList();
            foreach (var orderItem in orderItems)
            {

                AIRItem airItem = new AIRItem()
                {
                    Id = Guid.NewGuid(),
                    AirId = entity.Id,
                    OrderItemId = orderItem.Id,
                    Qty = orderItem.Qty,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };

                entity.AIRItems.Add(airItem);
            }

            db.AIRs.Add(entity);
            await db.SaveChangesAsync();

            return model;
        }

        public async ValueTask<AIR_VM> UpdateAsync(AIR_VM model, string user, DateTime date)
        {
            await ValidateOnUpdate(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await db.AIRs.FindAsync(model.Id);

            // if there's a change of Order item
            if (entity.OrderId != model.OrderId)
            {
                var airItems = db.AIRItems.Where(w => w.AirId == model.Id);
                await airItems.ForEachAsync(f =>
                {
                    f.UpdatedBy = model.UpdatedBy;
                    f.UpdatedDt = model.UpdatedDt;
                });
                await db.SaveChangesAsync();

                db.AIRItems.RemoveRange(airItems);
                await db.SaveChangesAsync();

                // include items during add
                var orderItems = db.OrderItems.Where(w => w.OrderId == model.OrderId).ToList();
                foreach (var orderItem in orderItems)
                {
                    AIRItem airItem = new AIRItem()
                    {
                        Id = Guid.NewGuid(),
                        AirId = entity.Id,
                        OrderItemId = orderItem.Id,
                        Qty = orderItem.Qty,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };

                    entity.AIRItems.Add(airItem);
                }
            }

            entity.Fund = model.Fund;
            entity.AIRNo = model.AIRNo;
            entity.AIRDate = model.AIRDate;
            entity.OrderId = model.OrderId;
            //entity.InvoiceNo = model.InvoiceNo ?? "";
            //entity.InvoiceDate = model.InvoiceDate;
            entity.AcceptedDate = model.AcceptedDate;
            entity.IsComplete = model.IsComplete;
            entity.IsPartial = model.IsPartial;
            entity.Custodian = model.Custodian ?? "";
            entity.InspectedDate = model.InspectedDate;
            entity.IsInspected = model.IsInspected;
            entity.Officer = model.Officer ?? "";
            entity.Remarks = model.Remarks ?? "";
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.AIRs.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return model;
        }

        public ValueTask<AIR_VM> DeleteAsync(AIR_VM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {

            await ValidateOnDelete(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await db.AIRs.FindAsync(model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.AIRs.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.AIRs.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            return model;
        });

        private string NextAirNo(DateTime date)
        {
            string yyyy = date.Year.ToString().Trim();
            string mm = date.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = db.AIRs.Where(w => w.AIRDate.Value.Year == date.Year).OrderByDescending(o => o.AIRNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(data.AIRNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }

        private async ValueTask ValidateOnCreate(AIR_VM model)
        {
            if (await db.AIRs.AnyAsync(a => a.AIRNo == model.AIRNo))
            {
                throw new RecordAlreadyExistsException(string.Format("AIR Number {0} already exists", model.AIRNo));
            }
        }

        private async ValueTask ValidateOnUpdate(AIR_VM model)
        {
            if (await db.AIRs.FindAsync(model.Id) == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (await db.AIRs.AnyAsync(a => a.AIRNo == model.AIRNo && a.Id != model.Id))
            {
                throw new RecordAlreadyExistsException(string.Format("AIR Number {0} already exists", model.AIRNo));
            }

            if (await IsPostedAsync(model.Id))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update");
            }
        }

        private async ValueTask ValidateOnDelete(AIR_VM model)
        {
            if (await db.AIRs.FindAsync(model.Id) == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (await IsPostedAsync(model.Id))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot delete!");
            }
        }
    }
}