using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services
{
    public interface IAirService
    {
        IQueryable<AIR_VM> GetAll();
        ValueTask<AIR> GetByIdAsync(Guid id);
        ValueTask<AIR_VM> GetVmByIdAsync(Guid id);
        ValueTask<AIR> GetByAirNoAsync(string airNo);
        ValueTask<bool> GetAnyAirNoAsync(Guid airId, string airNo);
        ValueTask<bool> IsPostedAsync(Guid airId);

        ValueTask<AIR_VM> CreateAsync(AIR_VM model, string user, DateTime date);
        ValueTask<AIR_VM> UpdateAsync(AIR_VM model, string user, DateTime date);
        ValueTask<AIR_VM> DeleteAsync(AIR_VM model, string user, DateTime date);
        ValueTask<AIR_VM> SaveAsync(AIR_VM model, string user, DateTime date);

        ValueTask<AIR> PostAsync(Guid airId, string user, DateTime date);
        ValueTask<AIR> UnpostAsync(Guid airId, string user, DateTime date);
    }

    public class AirService : IAirService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<AIR_VM> _VmExceptionService = new ExceptionService<AIR_VM>();
        private readonly IExceptionService<AIR> _ExceptionService = new ExceptionService<AIR>();

        private IAirItemService _airItemService;

        public AirService(AppManEntities db)
        {
            _db = db;
            _airItemService = new AirItemService(_db);
        }

        public IQueryable<AIR_VM> GetAll() => _VmExceptionService.TryCatch(() =>
        {
            var data = _db.AIRs
                .Include(i => i.AIRInvoices)
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
                    PostedDt = s.PostedDt,
                    AIRInvoices = s.AIRInvoices
                });
            return data;
        });

        public async ValueTask<bool> GetAnyAirNoAsync(Guid airId, string airNo)
        {
            return await _db.AIRs.AnyAsync(a => a.Id != airId && a.AIRNo == airNo);
        }

        public ValueTask<AIR> GetByIdAsync(Guid id) => _ExceptionService.TryCatch(async () =>
        {
            return await _db.AIRs.FindAsync(id);
        });

        public ValueTask<AIR_VM> GetVmByIdAsync(Guid id) => _VmExceptionService.TryCatch(async () =>
        {
            var data = await _db.AIRs
                .Include(i => i.AIRInvoices)
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
                    PostedDt = s.PostedDt,
                    AIRInvoices = s.AIRInvoices
                }).FirstOrDefaultAsync();
            return data;
        });

        public ValueTask<AIR> GetByAirNoAsync(string airNo) => _ExceptionService.TryCatch(async () =>
        {
            return await _db.AIRs.Where(w => w.AIRNo == airNo).FirstOrDefaultAsync();
        });

        public async ValueTask<bool> IsPostedAsync(Guid airId)
        {
            var entity = await _db.AIRs.FindAsync(airId);
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

        public ValueTask<AIR> PostAsync(Guid airId, string user, DateTime date) => _ExceptionService.TryCatch(async () =>
        {
            var entity = await _db.AIRs.Include(i => i.AIRItems).FirstOrDefaultAsync(f => f.Id == airId);
            if (entity == null)
            {
                throw new RecordNotFoundException(airId);
            }

            if (await IsPostedAsync(airId))
            {
                throw new RecordAlreadyPostedException();
            }

            _airItemService.ValidAirItems(airId);

            entity.PostedBy = user;
            entity.PostedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.AIRs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;

            List<Guid> psCardIdList = new List<Guid>();
            var orderItemGroups = await _db.Database.SqlQuery<OrderItemGroupVM>("Exec OrderService_GetOrderItemGroup {0}", entity.OrderId).ToListAsync();
            // create stock for each group
            foreach (var oig in orderItemGroups)
            {
                // Post the OrderItems under the stocks having the same PsCodeId
                var orderItemList = await _db.OrderItems
                    .Include(i => i.Order)
                    .Include(i => i.RequestItem.RisItem.RISs)
                    .Where(w => w.OrderId == entity.OrderId
                        && w.StockNo == oig.StockNo
                        && w.StockName == oig.StockName
                        && w.Description == oig.Description
                        && w.RequestItem.RisItem.RISs.Fund == oig.Fund).ToListAsync();

                foreach (var orderItem in orderItemList)
                {
                    var isNew = false;
                    var psCard = await _db.PsCards.Include(i => i.PsCardItems)
                        .Include(i => i.AllField)
                        .Where(w => w.PsNo == oig.StockNo && w.Fund == oig.Fund
                            && w.FromDonation != true
                        //&& w.Unit == oig.Unit
                        ).FirstOrDefaultAsync();

                    var risAllField = await _db.AllFields.AsNoTracking().Where(w => w.Id == orderItem.RequestItem.RisItemId).FirstOrDefaultAsync();
                    var office = orderItem.RequestItem.RisItem.RISs.Office;
                    var deptId = _db.Codextns.Where(w => w.CodeMast.Code == "DEPARTMENTS" && w.Description.Trim() == office).FirstOrDefault()?.Id;

                    if (psCard == null)
                    {
                        isNew = true;
                        var psCardId = Guid.NewGuid();
                        psCard = new PsCard()
                        {
                            Id = psCardId,
                            ItemCodeId = oig.ItemCodeId,
                            Fund = oig.Fund,
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
                    }

                    var psCardItem = await _db.PsCardItems.Where(w => w.OrderItemId == orderItem.Id).FirstOrDefaultAsync();
                    if (psCardItem == null)
                    {
                        var psCardItemId = Guid.NewGuid();
                        psCardItem = new PsCardItem()
                        {
                            Id = psCardItemId,
                            GroupId = psCardItemId,
                            PsCardId = psCard.Id,
                            OrderItemId = orderItem.Id,
                            PoDate = orderItem.Order.PoDate,
                            PoNo = orderItem.Order.PoNo,
                            AirDate = entity.AIRDate,
                            AirNo = entity.AIRNo,
                            Qty = (int)orderItem.Qty,
                            QtyIss = 0,
                            QtyBal = (int)orderItem.Qty,
                            TranType = "I",
                            Unit = orderItem.RequestItem.RisItem.Unit,
                            UnitCost = orderItem.UnitCost,
                            Amount = orderItem.Amount,
                            PriceRate = orderItem.PriceRate,
                            DeptId = deptId,
                            Description = oig.Description,
                            OtherDesc = orderItem.RequestItem.RisItem.OtherDesc,
                            Type = risAllField.Type,
                            InvDist = oig.InvDist,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };

                        // include ItemExtns
                        var airItemExtnOthers = _airItemService.AirItemExtn.GetAirItemExtnByOrderItemId<AIRItemExtnOther>(orderItem.Id);
                        foreach (var airItemExtnOther in airItemExtnOthers)
                        {
                            var psCardItemExtnOther = await _db.PsCardItemExtns.OfType<PsCardItemExtnOther>()
                                .FirstOrDefaultAsync(f => f.AIRItemExtnId == airItemExtnOther.Id);
                            if (psCardItemExtnOther == null)
                            {
                                psCardItemExtnOther = new PsCardItemExtnOther()
                                {
                                    Id = Guid.NewGuid(),
                                    PsCardItemId = psCardItem.Id,
                                    AIRItemExtnId = airItemExtnOther.Id,
                                    SerialNo = airItemExtnOther.SerialNo,
                                    InsertedBy = user,
                                    InsertedDt = date,
                                    UpdatedBy = user,
                                    UpdatedDt = date
                                };
                                psCardItem.PsCardItemExtns.Add(psCardItemExtnOther);
                            }
                        }

                        var airItemExtnVehicles = _airItemService.AirItemExtn.GetAirItemExtnByOrderItemId<AIRItemExtnVehicle>(orderItem.Id);
                        foreach (var airItemExtnVehicle in airItemExtnVehicles)
                        {
                            var psCardItemExtnVehicle = await _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>()
                                    .FirstOrDefaultAsync(f => f.AIRItemExtnId == airItemExtnVehicle.Id);
                            if (psCardItemExtnVehicle == null)
                            {
                                psCardItemExtnVehicle = new PsCardItemExtnVehicle()
                                {
                                    Id = Guid.NewGuid(),
                                    PsCardItemId = psCardItem.Id,
                                    AIRItemExtnId = airItemExtnVehicle.Id,
                                    YearModel = airItemExtnVehicle.YearModel,
                                    PlateNo = airItemExtnVehicle.PlateNo,
                                    BodyNo = airItemExtnVehicle.BodyNo,
                                    EngineNo = airItemExtnVehicle.EngineNo,
                                    ChasisNo = airItemExtnVehicle.ChasisNo,
                                    Color = airItemExtnVehicle.Color,
                                    CRN = airItemExtnVehicle.CRN,
                                    CRDate = airItemExtnVehicle.CRDate,
                                    MVFileNo = airItemExtnVehicle.MVFileNo,
                                    OrNo = airItemExtnVehicle.OrNo,
                                    OrDate = airItemExtnVehicle.OrDate,
                                    NetWeight = airItemExtnVehicle.NetWeight,
                                    InsPolicyNo = airItemExtnVehicle.InsPolicyNo,
                                    //ParReissuance = airItemExtnVehicle.ParReissuance,
                                    //Condition = airItemExtnVehicle.Condition,
                                    SubLocation = airItemExtnVehicle.SubLocation,
                                    InsertedBy = user,
                                    InsertedDt = date,
                                    UpdatedBy = user,
                                    UpdatedDt = date
                                };
                                psCardItem.PsCardItemExtns.Add(psCardItemExtnVehicle);
                            }
                        }

                        psCard.PsCardItems.Add(psCardItem);
                    }

                    if (isNew == true)
                    {
                        _db.PsCards.Add(psCard);
                    }

                    psCardIdList.Add(psCard.Id);
                }
            }

            await _db.SaveChangesAsync();

            foreach (var psCardId in psCardIdList)
            {
                var psCardItemList = _db.PsCardItems.Where(w => w.PsCardId == psCardId).ToList();
                foreach (var psCardItem in psCardItemList)
                {
                    // search unit group if any
                    var orderItemUnitGroupDescriptionItem = await _db.OrderItemUnitGroupDescriptionItems
                        .Include(i => i.RequestItemUnitGroupDescriptionItem.RisItemUnitGroupDescriptionItem.RisItemUnitGroupDescription.RisItemUnitGroup)
                        .Include(i => i.OrderItemUnitGroupDescription.OrderItemUnitGroup)
                        .Where(w => w.OrderItemId == psCardItem.OrderItemId).FirstOrDefaultAsync();
                    if (orderItemUnitGroupDescriptionItem != null)
                    {
                        // search in psCard unit group
                        if (!await _db.PsCardItemUnitGroupDescriptionItems.Where(w => w.PsCardItemId == psCardItem.Id).AnyAsync())
                        {
                            var psCardItemUnitGroup = await _db.PsCardItemUnitGroups.Include(i => i.PsCardItemUnitGroupDescriptions).Where(w => w.PoNo == psCardItem.PoNo).FirstOrDefaultAsync();
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
                                _db.PsCardItemUnitGroups.Add(psCardItemUnitGroup);
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
                                    _db.PsCardItemUnitGroupDescriptions.Add(psCardItemUnitGroupDescription);
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
                                        _db.PsCardItemUnitGroupDescriptionItems.Add(psCardItemUnitGroupDescriptionItem);
                                    }
                                }
                            }
                            await _db.SaveChangesAsync();
                        }
                    }
                }
            }
            return entity;
        });

        public ValueTask<AIR> UnpostAsync(Guid airId, string user, DateTime date) => _ExceptionService.TryCatch(async () =>
        {
            var entity = await _db.AIRs.FindAsync(airId);
            if (entity == null)
            {
                throw new RecordNotFoundException(airId);
            }

            if (!await IsPostedAsync(airId))
            {
                throw new RecordNotYetPostedException();
            }

            if (await _db.PsCardItemIssuances.AsNoTracking().AnyAsync(a => a.PsCardItem.OrderItemId == entity.OrderId))
            {
                throw new RecordRelationshipException("Items were already issued cannot unpost!");
            }

            if (await _db.IcsParItems.AsNoTracking().AnyAsync(a => a.PsCardItemExtn.PsCardItem.OrderItemId == entity.OrderId))
            {
                throw new RecordRelationshipException("PAR/ICS already issued cannot unpost!");
            }

            /*
                * Delete the following records onUnpost:
                * PsCardItems, Fields..., PsCardItemExtns
                * PsCards --> if no PsItem                
            */

            var orderItems = _db.OrderItems.Include(i => i.RequestItem.RisItem.ItemCode.ItemType).Where(w => w.OrderId == entity.OrderId).ToList();

            foreach (var orderItem in orderItems)
            {
                var psCardItems = _db.PsCardItems.Include(i => i.PsCardItemExtns).Where(w => w.OrderItemId == orderItem.Id).ToList();
                Guid? psCardId = psCardItems?.FirstOrDefault()?.PsCardId;

                foreach (var psCardItem in psCardItems)
                {

                    // check unit groups           
                    var unitGroupDescriptionItems = _db.PsCardItemUnitGroupDescriptionItems.Where(w => w.PsCardItemId == psCardItem.Id);
                    if (unitGroupDescriptionItems.Any())
                    {
                        _db.PsCardItemUnitGroupDescriptionItems.RemoveRange(unitGroupDescriptionItems);
                        await _db.SaveChangesAsync();
                    }

                    var unitGroupDescriptions = _db.PsCardItemUnitGroupDescriptions.Where(w => w.PsCardItemUnitGroup.PoNo == psCardItem.PoNo && !w.PsCardItemUnitGroupDescriptionItems.Any());
                    if (unitGroupDescriptions.Any())
                    {
                        _db.PsCardItemUnitGroupDescriptions.RemoveRange(unitGroupDescriptions);
                        await _db.SaveChangesAsync();
                    }

                    var unitGroups = _db.PsCardItemUnitGroups.Where(w => w.PoNo == psCardItem.PoNo && !w.PsCardItemUnitGroupDescriptions.Any());
                    if (unitGroups.Any())
                    {
                        _db.PsCardItemUnitGroups.RemoveRange(unitGroups);
                        await _db.SaveChangesAsync();
                    }

                    if (psCardItem.PsCardItemExtns.Any())
                    {
                        _db.PsCardItemExtns.RemoveRange(psCardItem.PsCardItemExtns);
                        await _db.SaveChangesAsync();
                    }


                    var item = await _db.PsCardItems.FirstOrDefaultAsync(f => f.Id == psCardItem.Id);
                    if (item != null)
                    {
                        item.UpdatedBy = user;
                        item.UpdatedDt = date;

                        _db.PsCardItems.Attach(item);
                        _db.Entry(item).State = EntityState.Modified;
                        await _db.SaveChangesAsync();

                        _db.PsCardItems.Remove(item);
                        _db.Entry(item).State = EntityState.Deleted;
                        await _db.SaveChangesAsync();

                    }                    
                }
                
                if (psCardId != null)
                {
                    if (!_db.PsCardItems.Any(a => a.PsCardId == psCardId)) // no other  order item is using this item
                    {
                        var psCard = await _db.PsCards.Include(i => i.AllField).Where(w => w.Id == psCardId).FirstOrDefaultAsync();
                        psCard.UpdatedBy = user;
                        psCard.UpdatedDt = date;

                        _db.PsCards.Attach(psCard);
                        _db.Entry(psCard).State = EntityState.Modified;
                        await _db.SaveChangesAsync();

                        // delete stock during unpost if not used by other order item
                        _db.PsCards.Remove(psCard);
                        _db.Entry(psCard).State = EntityState.Deleted;
                        await _db.SaveChangesAsync();
                    }
                }
            }

            entity.PostedBy = null;
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.AIRs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        private ValueTask<AIR> UpdatePsItem(AIR entity, string user, DateTime date, bool post) => _ExceptionService.TryCatch(async () =>
        {
            var orderItemIdList = await _db.AIRItems.Where(w => w.AirId == entity.Id).GroupBy(g => g.OrderItemId)
                .Select(s => s.Key).ToListAsync();
            foreach (var orderItemId in orderItemIdList)
            {
                decimal? qtyAccepted = 0;
                if (post)
                {
                    qtyAccepted = _db.AIRItems.Where(w => w.OrderItemId == orderItemId).Sum(s => s.Qty);
                }
                var psCardItem = await _db.PsCardItems.Include(i => i.OrderItem.RequestItem.RisItem).Where(w => w.OrderItemId == orderItemId).FirstOrDefaultAsync();
                if (psCardItem != null)
                {
                    psCardItem.AirNo = entity.AIRNo;
                    psCardItem.AirDate = entity.AIRDate;
                    psCardItem.Qty = (int)qtyAccepted;
                    psCardItem.QtyBal = (int)qtyAccepted - psCardItem.QtyIss;
                    psCardItem.UpdatedBy = user;
                    psCardItem.UpdatedDt = date;
                    psCardItem.Description = psCardItem.OrderItem.RequestItem.RisItem.Description;
                    psCardItem.OtherDesc = psCardItem.OrderItem.RequestItem.RisItem.OtherDesc;
                    _db.PsCardItems.Attach(psCardItem);
                    _db.Entry(psCardItem).State = EntityState.Modified;
                    await _db.SaveChangesAsync();
                }
            }
            return entity;
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
            var orderItems = _db.OrderItems.Where(w => w.OrderId == model.OrderId).ToList();
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

            _db.AIRs.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        }

        public async ValueTask<AIR_VM> UpdateAsync(AIR_VM model, string user, DateTime date)
        {
            await ValidateOnUpdate(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.AIRs.FindAsync(model.Id);

            // if there's a change of Order item
            if (entity.OrderId != model.OrderId)
            {
                var airItems = _db.AIRItems.Where(w => w.AirId == model.Id);
                await airItems.ForEachAsync(f =>
                {
                    f.UpdatedBy = model.UpdatedBy;
                    f.UpdatedDt = model.UpdatedDt;
                });
                await _db.SaveChangesAsync();

                _db.AIRItems.RemoveRange(airItems);
                await _db.SaveChangesAsync();

                // include items during add
                var orderItems = _db.OrderItems.Where(w => w.OrderId == model.OrderId).ToList();
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

            _db.AIRs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        }

        public ValueTask<AIR_VM> DeleteAsync(AIR_VM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {

            await ValidateOnDelete(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.AIRs.FindAsync(model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.AIRs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.AIRs.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

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

            var data = _db.AIRs.Where(w => w.AIRDate.Value.Year == date.Year).OrderByDescending(o => o.AIRNo).FirstOrDefault();
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
            if (await _db.AIRs.AnyAsync(a => a.AIRNo == model.AIRNo))
            {
                throw new RecordAlreadyExistsException(string.Format("AIR Number {0} already exists", model.AIRNo));
            }
        }

        private async ValueTask ValidateOnUpdate(AIR_VM model)
        {
            if (await _db.AIRs.FindAsync(model.Id) == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (await _db.AIRs.AnyAsync(a => a.AIRNo == model.AIRNo && a.Id != model.Id))
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
            if (await _db.AIRs.FindAsync(model.Id) == null)
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