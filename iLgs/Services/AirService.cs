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
            //await db.SaveChangesAsync();

            //await UpdatePsItem(entity, user, date, true);

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
                    var psCard = await db.PsCards.Include(i => i.PsCardItems).AsNoTracking()
                        .Where(w => w.PsNo == oig.StockNo && w.Fund == oig.Fund 
                            && w.FromDonation != true
                            //&& w.Unit == oig.Unit
                        ).FirstOrDefaultAsync();
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

                        FieldsAccountableForm fieldsAccountableForm = null;
                        FieldsAgricultural fieldsAgricultural = null;
                        FieldsAnimal fieldsAnimal = null;
                        FieldsFurniture fieldsFurniture = null;
                        FieldsLand fieldsLand = null;
                        FieldsMachinery fieldsMachinery = null;
                        FieldsMedical fieldsMedical = null;
                        FieldsMedicine fieldsMedicine = null;
                        FieldsMilitarySuuply fieldsMilitarySuuply = null;
                        FieldsNonAccountableForm fieldsNonAccountableForm = null;
                        FieldsOfficeSupply fieldsOfficeSupply = null;
                        FieldsOther fieldsOther = null;
                        FieldsOtherSupplyMaterial fieldsOtherSupplyMaterial = null;
                        FieldsRepair fieldsRepair = null;
                        FieldsTransportation fieldsTransportation = null;
                        FieldsVehicle fieldsVehicle = null;
                        FieldsConstruction fieldsConstruction = null;


                        if (Enum.TryParse(oig.ItemTypeCode, out Category category))
                        {
                            if (category == Category.A)
                            {
                                var risFieldsAccountableForm = await db.FieldsAccountableForms.AsNoTracking().Where(w => w.Id == orderItem.RequestItem.RisItemId).FirstOrDefaultAsync();
                                if (risFieldsAccountableForm != null)
                                {
                                    fieldsAccountableForm = new FieldsAccountableForm()
                                    {
                                        Id = psCardId,
                                        Brand = risFieldsAccountableForm.Brand,
                                        Model_ = risFieldsAccountableForm.Model_
                                    };
                                    psCard.FieldsAccountableForm = fieldsAccountableForm;
                                }
                            }
                            else if (category == Category.C)
                            {
                                var risFieldsConstruction = await db.FieldsConstructions.AsNoTracking().Where(w => w.Id == orderItem.RequestItem.RisItemId).FirstOrDefaultAsync();
                                if (risFieldsConstruction != null)
                                {
                                    fieldsConstruction = new FieldsConstruction()
                                    {
                                        Id = psCardId,
                                        Brand = risFieldsConstruction.Brand,
                                        Model_ = risFieldsConstruction.Model_
                                    };
                                    psCard.FieldsConstruction = fieldsConstruction;
                                }
                            }
                            else if (category == Category.D)
                            {
                                var risFieldsMedicine = await db.FieldsMedicines.AsNoTracking().Where(w => w.Id == orderItem.RequestItem.RisItemId).FirstOrDefaultAsync();
                                if (risFieldsMedicine != null)
                                {
                                    fieldsMedicine = new FieldsMedicine()
                                    {
                                        Id = psCardId,
                                        GenericName = risFieldsMedicine.GenericName,
                                        DosageForm = risFieldsMedicine.DosageForm,
                                        DosageStrength = risFieldsMedicine.DosageStrength,
                                        DosageVolume = risFieldsMedicine.DosageVolume,
                                        Brand = orderItem.Brand
                                    };

                                    psCard.FieldsMedicine = fieldsMedicine;
                                }
                            }
                            else if (category == Category.E)
                            {
                                var risFieldsMachinery = await db.FieldsMachineries.AsNoTracking().Where(w => w.Id == orderItem.RequestItem.RisItemId).FirstOrDefaultAsync();
                                if (risFieldsMachinery != null)
                                {
                                    fieldsMachinery = new FieldsMachinery()
                                    {
                                        Id = psCardId,
                                        Brand = risFieldsMachinery.Brand,
                                        Model_ = risFieldsMachinery.Model_
                                    };
                                    psCard.FieldsMachinery = fieldsMachinery;
                                }
                            }
                            else if (category == Category.G)
                            {
                                var risFieldsAgricultural = await db.FieldsAgriculturals.AsNoTracking().Where(w => w.Id == orderItem.RequestItem.RisItemId).FirstOrDefaultAsync();
                                if (risFieldsAgricultural != null)
                                {
                                    fieldsAgricultural = new FieldsAgricultural()
                                    {
                                        Id = psCardId,
                                        Brand = risFieldsAgricultural.Brand,
                                        Model_ = risFieldsAgricultural.Model_
                                    };
                                    psCard.FieldsAgricultural = fieldsAgricultural;
                                }
                            }
                            else if (category == Category.L)
                            {
                                var risFieldsLand = await db.FieldsLands.AsNoTracking().Where(w => w.Id == orderItem.RequestItem.RisItemId).FirstOrDefaultAsync();
                                if (risFieldsLand != null)
                                {
                                    fieldsLand = new FieldsLand()
                                    {
                                        Id = psCardId,
                                        Area = risFieldsLand.Area
                                    };
                                    psCard.FieldsLand = fieldsLand;
                                }
                            }
                            else if (category == Category.M)
                            {
                                var risFieldsMedical = await db.FieldsMedicals.AsNoTracking().Where(w => w.Id == orderItem.RequestItem.RisItemId).FirstOrDefaultAsync();
                                if (risFieldsMedical != null)
                                {
                                    fieldsMedical = new FieldsMedical()
                                    {
                                        Id = psCardId,
                                        Brand = risFieldsMedical.Brand,
                                        Model_ = risFieldsMedical.Model_
                                    };
                                    psCard.FieldsMedical = fieldsMedical;
                                }
                            }
                            else if (category == Category.N)
                            {
                                var risFieldsNonAccountableForm = await db.FieldsNonAccountableForms.AsNoTracking().Where(w => w.Id == orderItem.RequestItem.RisItemId).FirstOrDefaultAsync();
                                if (risFieldsNonAccountableForm != null)
                                {
                                    fieldsNonAccountableForm = new FieldsNonAccountableForm()
                                    {
                                        Id = psCardId,
                                        Brand = risFieldsNonAccountableForm.Brand,
                                        Model_ = risFieldsNonAccountableForm.Model_
                                    };
                                    psCard.FieldsNonAccountableForm = fieldsNonAccountableForm;
                                }
                            }
                            else if (category == Category.O)
                            {
                                var risFieldsOfficeSupply = await db.FieldsOfficeSupplies.AsNoTracking().Where(w => w.Id == orderItem.RequestItem.RisItemId).FirstOrDefaultAsync();
                                if (risFieldsOfficeSupply != null)
                                {
                                    fieldsOfficeSupply = new FieldsOfficeSupply()
                                    {
                                        Id = psCardId,
                                        Brand = risFieldsOfficeSupply.Brand,
                                        Model_ = risFieldsOfficeSupply.Model_
                                    };
                                    psCard.FieldsOfficeSupply = fieldsOfficeSupply;
                                }
                            }
                            else if (category == Category.P)
                            {
                                var risFieldsMilitarySuuply = await db.FieldsMilitarySuuplies.AsNoTracking().Where(w => w.Id == orderItem.RequestItem.RisItemId).FirstOrDefaultAsync();
                                if (risFieldsMilitarySuuply != null)
                                {
                                    fieldsMilitarySuuply = new FieldsMilitarySuuply()
                                    {
                                        Id = psCardId,
                                        Brand = risFieldsMilitarySuuply.Brand,
                                        Model_ = risFieldsMilitarySuuply.Model_
                                    };
                                    psCard.FieldsMilitarySuuply = fieldsMilitarySuuply;
                                }
                            }
                            else if (category == Category.R)
                            {
                                var risFieldsRepair = await db.FieldsRepairs.AsNoTracking().Where(w => w.Id == orderItem.RequestItem.RisItemId).FirstOrDefaultAsync();
                                if (risFieldsRepair != null)
                                {
                                    fieldsRepair = new FieldsRepair()
                                    {
                                        Id = psCardId,
                                        SerialNo = risFieldsRepair.SerialNo,
                                        PropertyNo = risFieldsRepair.PropertyNo,
                                        PlateNo = risFieldsRepair.PlateNo,
                                        BodyNo = risFieldsRepair.BodyNo,
                                        MVFileNo = risFieldsRepair.MVFileNo,
                                        Brand = risFieldsRepair.Brand,
                                        Model_ = risFieldsRepair.Model_
                                    };
                                    psCard.FieldsRepair = fieldsRepair;
                                }
                            }
                            else if (category == Category.T)
                            {
                                var risFieldsTransportation = await db.FieldsTransportations.AsNoTracking().Where(w => w.Id == orderItem.RequestItem.RisItemId).FirstOrDefaultAsync();
                                if (risFieldsTransportation != null)
                                {
                                    fieldsTransportation = new FieldsTransportation()
                                    {
                                        Id = psCardId,
                                        Brand = risFieldsTransportation.Brand,
                                        Model_ = risFieldsTransportation.Model_
                                    };
                                    psCard.FieldsTransportation = fieldsTransportation;
                                }
                            }
                            else if (category == Category.U)
                            {
                                var risFieldsFurniture = await db.FieldsFurnitures.AsNoTracking().Where(w => w.Id == orderItem.RequestItem.RisItemId).FirstOrDefaultAsync();
                                if (risFieldsFurniture != null)
                                {
                                    fieldsFurniture = new FieldsFurniture()
                                    {
                                        Id = psCardId,
                                        Brand = risFieldsFurniture.Brand,
                                        Model_ = risFieldsFurniture.Model_,
                                        Dimension = risFieldsFurniture.Dimension,
                                        Size = risFieldsFurniture.Brand,
                                        Weight = risFieldsFurniture.Weight,
                                        Capacity = risFieldsFurniture.Capacity,
                                        Color = risFieldsFurniture.Color
                                    };
                                    psCard.FieldsFurniture = fieldsFurniture;
                                }
                            }
                            else if (category == Category.V)
                            {
                                var risFieldsAnimal = await db.FieldsAnimals.AsNoTracking().Where(w => w.Id == orderItem.RequestItem.RisItemId).FirstOrDefaultAsync();
                                if (risFieldsAnimal != null)
                                {
                                    fieldsAnimal = new FieldsAnimal()
                                    {
                                        Id = psCardId,
                                        Brand = risFieldsAnimal.Brand,
                                        Model_ = risFieldsAnimal.Model_
                                    };
                                    psCard.FieldsAnimal = fieldsAnimal;
                                }
                            }
                            else if (category == Category.X)
                            {
                                var risFieldsOtherSupplyMaterial = await db.FieldsOtherSupplyMaterials.AsNoTracking().Where(w => w.Id == orderItem.RequestItem.RisItemId).FirstOrDefaultAsync();
                                if (risFieldsOtherSupplyMaterial != null)
                                {
                                    fieldsOtherSupplyMaterial = new FieldsOtherSupplyMaterial()
                                    {
                                        Id = psCardId,
                                        Brand = risFieldsOtherSupplyMaterial.Brand,
                                        Model_ = risFieldsOtherSupplyMaterial.Model_
                                    };
                                    psCard.FieldsOtherSupplyMaterial = fieldsOtherSupplyMaterial;
                                }
                            }
                            else if (category == Category.Z)
                            {
                                var risFieldsOther = await db.FieldsOthers.AsNoTracking().Where(w => w.Id == orderItem.RequestItem.RisItemId).FirstOrDefaultAsync();
                                if (risFieldsOther != null)
                                {
                                    fieldsOther = new FieldsOther()
                                    {
                                        Id = psCardId,
                                        Brand = risFieldsOther.Brand,
                                        Model_ = risFieldsOther.Model_
                                    };
                                    psCard.FieldsOther = fieldsOther;
                                }
                            }
                        }
                        var office = orderItem.RequestItem.RisItem.RISs.Office;
                        var deptId = db.Codextns.Where(w => w.CodeMast.Code == "DEPARTMENTS" && w.Description.Trim() == office).FirstOrDefault()?.Id;

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
                        //await db.SaveChangesAsync();
                    }
                }
            }

            await db.SaveChangesAsync();
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

            /*
                * Delete the following records onUnpost:
                * PsCardItems, Fields...
                * PsCards --> if no PsItem                
            */

            var orderItems = db.OrderItems.Include(i => i.RequestItem.RisItem.ItemCode.ItemType).Where(w => w.OrderId == entity.OrderId).ToList();

            foreach (var orderItem in orderItems)
            {
                if (Enum.TryParse(orderItem.RequestItem.RisItem.ItemCode.ItemType.Code, out Category category))
                {
                    if (category == Category.D)
                    {
                        var psCardItems = db.PsCardItems.Where(w => w.OrderItemId == orderItem.Id);
                        if (psCardItems.Any())
                        {
                            var psCardId = psCardItems.FirstOrDefault().PsCardId;
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
                                var psCard = await db.PsCards.Where(w => w.Id == psCardId).FirstOrDefaultAsync();
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
        }

        private async ValueTask ValidateOnDelete(AIR_VM model)
        {
            if (await db.AIRs.FindAsync(model.Id) == null)
            {
                throw new RecordNotFoundException(model.Id);
            }            
        }        
    }
}