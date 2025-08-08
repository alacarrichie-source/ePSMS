using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.PropertyCard;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace iLgs.Services.PoIssuance
{
    public interface IPoIssuanceService
    {
        ValueTask<IQueryable<PsCardItemVM>> GetAllAsync(string userId);
        IQueryable<PsCardItemVM> GetById(Guid? id);
        IQueryable<PsCardItemVM> GetSummary();
        IQueryable<PsCardItemExtnTransitVM> GetCardItemExtnForTransit(Guid? psCardItemId, Guid? transferId);
        ValueTask PostAsync(Guid psCardItemIssuanceId, string user, DateTime date);
        ValueTask UnpostAsync(Guid psCardItemIssuanceId, string user, DateTime date);
        ValueTask<PsCardItemTransferVM> TransferAsync(PsCardItemTransferVM model, string user, DateTime date);
    }

    public class PoIssuanceService : IPoIssuanceService
    {
        private decimal? _priceCap;
        private readonly AppManEntities _db;
        private readonly IUserService _userService;
        private readonly IExceptionService<RisIssuedVM> _vmExceptionService;
        private readonly IExceptionService<PsCardItemVM> _psCardItemVMExceptionService;
        private readonly IExceptionService<PsCardItemTransferVM> _psCardItemTransferVMExceptionService;
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;
        private readonly IPsCardService _psCardService;
        private readonly IPriceCapService _priceCapService;

        public PoIssuanceService(AppManEntities db,
            IUserService userService,
            IExceptionService<RisIssuedVM> vmExceptionService,
            IExceptionService<PsCardItemVM> psCardItemVMExceptionService,
            IExceptionService<PsCardItemTransferVM> psCardItemTransferVMExceptionService,
            IPsCardItemTransactionService psCardItemTransactionService,
            IPsCardService psCardService,
            IPriceCapService priceCapService)
        {
            _db = db;
            _userService = userService;
            _vmExceptionService = vmExceptionService;
            _psCardItemVMExceptionService = psCardItemVMExceptionService;
            _psCardItemTransferVMExceptionService = psCardItemTransferVMExceptionService;
            _psCardItemTransactionService = psCardItemTransactionService;
            _psCardService = psCardService;
            _priceCapService = priceCapService;            
        }

        private decimal GetPriceCap()
        {
            return _priceCap ?? (_priceCap = _priceCapService.GetPriceCap()).Value;
        }
        
        private Expression<Func<PsCardItem, PsCardItemVM>> GetPsCardItemProjection(AppManEntities _db)
        {
            return s => new PsCardItemVM
            {
                Id = s.Id,
                GroupId = s.GroupId,
                PsCardId = s.PsCardId,
                OrderItemId = s.OrderItemId,
                TransferRefId = s.TransferRefId,
                PoNo = s.PoNo,
                PoDate = s.PoDate,
                AirDate = s.AirDate,
                AirNo = s.AirNo,
                AirIssueDate = s.AirIssueDate,
                Qty = s.Qty,
                QtyIss = s.QtyIss,
                QtyBal = s.QtyBal,
                TransferIn = s.TransferIn,
                TransferOut = s.TransferOut,
                TranType = s.TranType,
                Unit = s.Unit,
                UnitCost = s.UnitCost,
                Amount = s.Amount,
                Days = s.Days,
                Remarks = s.Remarks,
                InsertedDt = s.InsertedDt,
                DeptId = s.DeptId,
                LocationId = s.LocationId,
                Article = _db.ItemCodes.FirstOrDefault(f => f.Code == s.PsCard.SubAccountCode).Description + "/" +
                s.PsCard.ItemCode.Description,
                Description = s.Description,
                DeptDisplay = s.DeptDisplay,
                StockNo = s.PsCard.PsNo,
                ParBalance = (int?)s.QtyBal - (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.Id == s.Id && w.IcsPar.RefType == "P").Sum(x => x.Qty) ?? 0),
                IcsBalance = (int?)s.QtyBal - (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.Id == s.Id && w.IcsPar.RefType == "I").Sum(x => x.Qty) ?? 0),
                RemBalance = (int?)s.QtyBal,
                Department = s.Codextn.Description,
                Location = s.Codextn1.Description,
                LocCode = s.Codextn1.Code,
                InvDistDesc = _db.Codextns.Where(w => w.CodeMast.Code == "PS-REMARKS" && w.Code == s.InvDist).FirstOrDefault() == null ? "" :
                    _db.Codextns.Where(w => w.CodeMast.Code == "PS-REMARKS" && w.Code == s.InvDist).FirstOrDefault().Description
            };
        }

        public async ValueTask<IQueryable<PsCardItemVM>> GetAllAsync(string userId)
        {
            var IsAdmin = await _userService.IsAdminAsync(userId);
            var data = _db.Database.SqlQuery<PsCardItemVM>("Exec PoIssuance_GetRecords {0}, {1}", IsAdmin, userId).AsQueryable();
            return data;
        }

        public IQueryable<PsCardItemVM> GetById(Guid? id)
        {
            var data = _db.Database.SqlQuery<PsCardItemVM>("Exec PoIssuance_GetByPsCardItemId {0}", id).AsQueryable();
            return data;
        }

        //public async ValueTask<PsCardItemVM> GetByIdAsync(Guid? id)
        //{
        //    var data = await _db.PsCardItems
        //        .Include(i => i.Codextn)
        //        .Include(i => i.Codextn1)
        //        .Where(w => w.Id == id)
        //        .Select(GetPsCardItemProjection(_db)).FirstOrDefaultAsync();
        //    return data;
        //}

        public IQueryable<PsCardItemVM> GetSummary()
        {
            var data = _db.Database.SqlQuery<PsCardItemVM>("Exec PoIssuance_Summary").AsQueryable();
            return data;
        }

        public async ValueTask PostAsync(Guid psCardItemIssuanceId, string user, DateTime date)
        {
            var entity = await _db.PsCardItemIssuances.FindAsync(psCardItemIssuanceId);
            if (entity == null)
            {
                throw new RecordNotFoundException(psCardItemIssuanceId);
            }

            if (!string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordAlreadyPostedException(string.Format("Record is currently posted.."));
            }

            if (entity.IssuedDate == null)
            {
                throw new InvalidValueException("Issued Date is Required!");
            }

            if (entity.Qty == null || entity.Qty <= 0)
            {
                throw new InvalidValueException("Quantity is Required!");
            }


            entity.PostedBy = user;
            entity.PostedDt = date;

            _db.PsCardItemIssuances.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }

        public async ValueTask UnpostAsync(Guid psCardItemIssuanceId, string user, DateTime date)
        {
            var entity = await _db.PsCardItemIssuances.FindAsync(psCardItemIssuanceId);

            if (entity == null)
            {
                throw new RecordNotFoundException(psCardItemIssuanceId);
            }

            if (string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordNotYetPostedException(string.Format("Record is not yet posted.."));
            }

            entity.PostedBy = null;
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PsCardItemIssuances.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }

        private string RefTypeDesc(string refType)
        {
            return refType == "P" ? "PAR" : "ICS";
        }

        //public IQueryable<PsCardItemExtnTransitVM> GetCardItemExtnForTransit(Guid? psCardItemId, Guid? groupId)
        //{
        //    var itemExtnName = _psCardService.GetItemExtnName(psCardItemId);
        //    return _db.Database.SqlQuery<PsCardItemExtnTransitVM>("Exec PsCardItemExtn_GetItemsForTransit {0}, {1}", psCardItemId, itemExtnName).AsQueryable();            
        //}

        public IQueryable<PsCardItemExtnTransitVM> GetCardItemExtnForTransit(Guid? psCardItemId, Guid? transferId)
        {
            var itemExtnName = _psCardService.GetItemExtnName(psCardItemId);
            return _db.Database.SqlQuery<PsCardItemExtnTransitVM>("Exec PsCardItemTransferItems_GetItemsForTransit {0}, {1}", transferId, itemExtnName).AsQueryable();
        }

        public IQueryable<TResult> GetCardItemExtnSetForIcsPars<T, TResult>(Guid? psCardItemId, Expression<Func<T, TResult>> selector) where T : PsCardItemExtn
        {
            var data = _db.PsCardItemExtns.OfType<T>().AsNoTracking()
                        .Include(i => i.PsCardItem.PsCard.ItemCode)
                        .Where(w => w.PsCardItemId == psCardItemId
                            && w.IcsParItems.Any(a => a.PsCardItemExtnId == w.Id && !a.PsCardItemTransferItems.Any())
                        )
                        .Select(selector)
                        .AsQueryable();

            return data;
        }

        public ValueTask<PsCardItemTransferVM> TransferAsync(PsCardItemTransferVM model, string user, DateTime date) =>
        _psCardItemTransferVMExceptionService.TryCatch(async () =>
        {
            List<PsCardItemExtnTransitVM> selectedItems = null;
            var psCardItemTransferSource = await _psCardService.PsCardItem.PsCardItemTransfer.GetByIdAsync(model.Id);

            if (!model.TransDate.HasValue)
            {
                throw new InvalidValueException("Transit date is required!");
            }

            if (model.IsWithItemExtn == true)
            {
                if (model.SelectedIds == null)
                {
                    throw new InvalidValueException(string.Format("No selected items, cannot continue."));
                }

                selectedItems = JsonConvert.DeserializeObject<List<PsCardItemExtnTransitVM>>(model.SelectedIds).OrderBy(o => o.SetLotNo).ThenBy(o => o.SetLotQtyNo).ThenBy(o => o.ContentNo).ToList();
                model.TransferOut = selectedItems.Count();

                if(model.TransferOut == 0)
                {
                    throw new InvalidValueException("No items to transit, cannot continue.");
                }

                // Transit to single Location (According to entered Location)
                var psCardItemTransfer = await CreatePsCardItemTransfer(psCardItemTransferSource, model.TransDate, model.TransferOut, model.LocationId, user, date);
                
                foreach (var transitItem in selectedItems)
                {
                    var psCardItemTransferItem = new PsCardItemTransferItem()
                    {
                        Id = Guid.NewGuid(),
                        PsCardItemTransferId = psCardItemTransfer.Id,
                        PsCardItemExtnId = transitItem.Id,
                        IcsParItemId = transitItem.IcsParItemId,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };

                    _db.PsCardItemTransferItems.Add(psCardItemTransferItem);
                    _db.Entry(psCardItemTransferItem).State = EntityState.Added;
                    await _db.SaveChangesAsync();
                }

                //// Transit Items
                //// Group by IcsParId = same location
                //var itemExtnName = _psCardService.GetItemExtnName(psCardItemTransferSource.PsCardItemId);

                //if (string.IsNullOrEmpty(itemExtnName))
                //{
                //    throw new InvalidValueException("Item ExtnName is emmpty.");
                //}

                //var selectedItemLocations = selectedItems.GroupBy(g => new { g.LocationId })
                //.Select(s => new
                //{
                //    LocationId = s.Key.LocationId,
                //    Count = s.Count()
                //}).ToList();

                //if (psCardItemTransferSource.LocationId != null)
                //{
                //    if (selectedItemLocations.Any(a => a.LocationId == psCardItemTransferSource.LocationId))
                //    {
                //        throw new InvalidValueException("Cannot transit items within the same PO Location");
                //    }
                //}
                //else
                //{
                //    if (selectedItemLocations.Any(a => a.LocationId == psCardItemTransferSource.DeptId))
                //    {
                //        throw new InvalidValueException("Cannot transit items within the same PO Location");
                //    }
                //}

                //foreach(var s in selectedItemLocations)
                //{                    
                //    var psCardItemTransfer = await CreatePsCardItemTransfer(psCardItemTransferSource, model.TransDate, s.Count, s.LocationId, user, date);
                    
                //    // Save Transit Items
                //    var transitItems = selectedItems.Where(w => w.LocationId == s.LocationId);
                //    foreach(var transitItem in transitItems)
                //    {
                //        var psCardItemTransferItem = new PsCardItemTransferItem()
                //        {
                //            Id = Guid.NewGuid(),
                //            PsCardItemTransferId = psCardItemTransfer.Id,
                //            PsCardItemExtnId = transitItem.Id,
                //            IcsParItemId = transitItem.IcsParItemId,
                //            InsertedBy = user,
                //            InsertedDt = date,
                //            UpdatedBy = user,
                //            UpdatedDt = date
                //        };

                //        _db.PsCardItemTransferItems.Add(psCardItemTransferItem);
                //        _db.Entry(psCardItemTransferItem).State = EntityState.Added;
                //        await _db.SaveChangesAsync();                        
                //    }
                //}


                /*
                 * TO DO: TRANSIT BY SET
                 */

                // non set items
                //var nonSetItems = selectedItems.Where(w => w.SetLotNo == "" || w.SetLotNo == null);
                //if (nonSetItems.Any())
                //{
                //    CreateTransitRecord(model.Id, model.TransDate, nonSetItems.Count(), nonSetItems.First().LocationId, user, date);
                //}
                //else
                //{
                //    // set items, may come from different par, but most likely from only same set
                //    var selectedItemIcsParIds = selectedItems.Where(w => w.SetLotNo == "" || w.SetLotNo == null).GroupBy(g => new { g.IcsParId, g.IcsParItemId })
                //    .Select(s => new
                //    {
                //        IcsParId = s.Key.IcsParId,
                //        IcsParItemId = s.Key.IcsParItemId
                //    }).ToList();

                //    // if ics/par is a set/lot, get all items included in the set.
                //    foreach (var selectedItemIcsParId in selectedItemIcsParIds)
                //    {
                //        var icsParUnitGroup = _db.IcsParUnitGroups
                //            .AsNoTracking()
                //            .Include(i => i.IcsPartUnitGroupDescriptions)
                //            .Where(w => w.IcsParId == selectedItemIcsParId.IcsParId 
                //                && w.IcsPartUnitGroupDescriptions.Any(a => a.IcsParUnitGroupDescriptionItems
                //                    .Any(b => b.IcsParItemId == selectedItemIcsParId.IcsParItemId))).FirstOrDefault();                        

                //        // transit the PsCardItem of the items in the set
                //        foreach(var unitGroupDescription in icsParUnitGroup.IcsPartUnitGroupDescriptions)
                //        {
                //            foreach(var unitGroupDescriptionItem in unitGroupDescription.IcsParUnitGroupDescriptionItems)
                //            {
                //                var psCardItemId = GetPsCardItemId(unitGroupDescriptionItem.IcsParItem.PsCardItemExtn.PsCardItemId, unitGroupDescriptionItem.IcsParItemId);
                //                CreateTransitRecord(psCardItemId, model.TransDate, model.TransferOut, model.LocationId, user, date);
                //            }
                //        }

                //        var icsParItems = _db.IcsParItems
                //            .AsNoTracking()
                //            .Include(i => i.IcsPar)
                //            .Where(w => w.Id == selectedItemIcsParId.IcsParItemId).ToList();
                //        foreach (var icsParItem in icsParItems)
                //        {

                //        }
                //    }
                //}                
            }
            else
            {
                if (!model.TransferOut.HasValue)
                {
                    throw new InvalidValueException("Transit out is required!");
                }

                if (model.LocationId == null)
                {
                    throw new InvalidValueException("Location is required!");
                }

                if (model.TransferOut > psCardItemTransferSource.QtyBal)
                {
                    throw new InvalidValueException("Transit out must not be greater than the balance!");
                }

                var psCardItemTransfer = await CreatePsCardItemTransfer(psCardItemTransferSource, model.TransDate, model.TransferOut, model.LocationId, user, date);
                //CreatePsCardItem(psCardItem, psCardItemTransfer.Id, model.LocationId, model.TransferOut, user, date);
                
            }                                              

            return model;
        });

        public ValueTask<PsCardItemTransferVM> TransferAsyncOld(PsCardItemTransferVM model, string user, DateTime date) =>
        _psCardItemTransferVMExceptionService.TryCatch(async () =>
        {
            List<PsCardItemExtnTransitVM> selectedItems = null;
            var psCardItemTransferSource = await _psCardService.PsCardItem.PsCardItemTransfer.GetByIdAsync(model.Id);

            if (!model.TransDate.HasValue)
            {
                throw new InvalidValueException("Transit date is required!");
            }

            if (model.IsWithItemExtn == true)
            {
                if (model.SelectedIds == null)
                {
                    throw new InvalidValueException(string.Format("No selected items, cannot continue."));
                }

                selectedItems = JsonConvert.DeserializeObject<List<PsCardItemExtnTransitVM>>(model.SelectedIds).OrderBy(o => o.SetLotNo).ThenBy(o => o.SetLotQtyNo).ThenBy(o => o.ContentNo).ToList();
                model.TransferOut = selectedItems.Count();

                if (model.TransferOut == 0)
                {
                    throw new InvalidValueException("No items to transit, cannot continue.");
                }

                // Transit Items
                // Group by IcsParId = same location
                var itemExtnName = _psCardService.GetItemExtnName(psCardItemTransferSource.PsCardItemId);

                if (string.IsNullOrEmpty(itemExtnName))
                {
                    throw new InvalidValueException("Item ExtnName is emmpty.");
                }

                var selectedItemIcsParIds = selectedItems.GroupBy(g => new { g.IcsParId, g.LocationId })
                .Select(s => new
                {
                    IcsParId = s.Key.IcsParId,
                    LocationId = s.Key.LocationId,
                    Count = s.Count()
                }).ToList();

                if (psCardItemTransferSource.LocationId != null)
                {
                    if (selectedItemIcsParIds.Any(a => a.LocationId == psCardItemTransferSource.LocationId))
                    {
                        throw new InvalidValueException("Cannot transit items within the same PO Location");
                    }
                }
                else
                {
                    if (selectedItemIcsParIds.Any(a => a.LocationId == psCardItemTransferSource.DeptId))
                    {
                        throw new InvalidValueException("Cannot transit items within the same PO Location");
                    }
                }

                foreach (var s in selectedItemIcsParIds)
                {
                    var psCardItemTransfer = await CreatePsCardItemTransfer(psCardItemTransferSource, model.TransDate, s.Count, s.LocationId, user, date);
                    //var newPsCardItem = CreatePsCardItem(psCardItem, psCardItemTransfer.Id, s.LocationId, s.Count, user, date);

                    // Save Transit Items
                    var transitItems = selectedItems.Where(w => w.IcsParId == s.IcsParId && w.LocationId == s.LocationId);
                    foreach (var transitItem in transitItems)
                    {
                        var psCardItemTransferItem = new PsCardItemTransferItem()
                        {
                            Id = Guid.NewGuid(),
                            PsCardItemTransferId = psCardItemTransfer.Id,
                            PsCardItemExtnId = transitItem.Id,
                            IcsParItemId = transitItem.IcsParItemId,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };

                        _db.PsCardItemTransferItems.Add(psCardItemTransferItem);
                        _db.Entry(psCardItemTransferItem).State = EntityState.Added;
                        await _db.SaveChangesAsync();
                    }
                }


                /*
                 * TO DO: TRANSIT BY SET
                 */

                // non set items
                //var nonSetItems = selectedItems.Where(w => w.SetLotNo == "" || w.SetLotNo == null);
                //if (nonSetItems.Any())
                //{
                //    CreateTransitRecord(model.Id, model.TransDate, nonSetItems.Count(), nonSetItems.First().LocationId, user, date);
                //}
                //else
                //{
                //    // set items, may come from different par, but most likely from only same set
                //    var selectedItemIcsParIds = selectedItems.Where(w => w.SetLotNo == "" || w.SetLotNo == null).GroupBy(g => new { g.IcsParId, g.IcsParItemId })
                //    .Select(s => new
                //    {
                //        IcsParId = s.Key.IcsParId,
                //        IcsParItemId = s.Key.IcsParItemId
                //    }).ToList();

                //    // if ics/par is a set/lot, get all items included in the set.
                //    foreach (var selectedItemIcsParId in selectedItemIcsParIds)
                //    {
                //        var icsParUnitGroup = _db.IcsParUnitGroups
                //            .AsNoTracking()
                //            .Include(i => i.IcsPartUnitGroupDescriptions)
                //            .Where(w => w.IcsParId == selectedItemIcsParId.IcsParId 
                //                && w.IcsPartUnitGroupDescriptions.Any(a => a.IcsParUnitGroupDescriptionItems
                //                    .Any(b => b.IcsParItemId == selectedItemIcsParId.IcsParItemId))).FirstOrDefault();                        

                //        // transit the PsCardItem of the items in the set
                //        foreach(var unitGroupDescription in icsParUnitGroup.IcsPartUnitGroupDescriptions)
                //        {
                //            foreach(var unitGroupDescriptionItem in unitGroupDescription.IcsParUnitGroupDescriptionItems)
                //            {
                //                var psCardItemId = GetPsCardItemId(unitGroupDescriptionItem.IcsParItem.PsCardItemExtn.PsCardItemId, unitGroupDescriptionItem.IcsParItemId);
                //                CreateTransitRecord(psCardItemId, model.TransDate, model.TransferOut, model.LocationId, user, date);
                //            }
                //        }

                //        var icsParItems = _db.IcsParItems
                //            .AsNoTracking()
                //            .Include(i => i.IcsPar)
                //            .Where(w => w.Id == selectedItemIcsParId.IcsParItemId).ToList();
                //        foreach (var icsParItem in icsParItems)
                //        {

                //        }
                //    }
                //}                
            }
            else
            {
                if (!model.TransferOut.HasValue)
                {
                    throw new InvalidValueException("Transit out is required!");
                }

                if (model.LocationId == null)
                {
                    throw new InvalidValueException("Location is required!");
                }

                if (model.TransferOut > psCardItemTransferSource.QtyBal)
                {
                    throw new InvalidValueException("Transit out must not be greater than the balance!");
                }

                var psCardItemTransfer = await CreatePsCardItemTransfer(psCardItemTransferSource, model.TransDate, model.TransferOut, model.LocationId, user, date);
                //CreatePsCardItem(psCardItem, psCardItemTransfer.Id, model.LocationId, model.TransferOut, user, date);

            }

            return model;
        });

        private async ValueTask<PsCardItemTransfer> CreatePsCardItemTransfer(PsCardItemTransferVM psCardItemTransferSource, DateTime? transDate, decimal? transOut, Guid? locationId, string user, DateTime date)
        {
            var psCardItemTransfer = new PsCardItemTransfer()
            {
                Id = Guid.NewGuid(),
                PsCardItemId = psCardItemTransferSource.PsCardItemId,
                ParentId = psCardItemTransferSource.Id,                
                TransDate = transDate,
                TransferIn = transOut,
                QtyBal = transOut,
                LocationId = locationId,
                TranType = "T",
                InsertedBy = user,
                InsertedDt = date,
                UpdatedBy = user,
                UpdatedDt = date
            };

            _db.PsCardItemTransfers.Add(psCardItemTransfer);
            _db.SaveChanges();

            // update parent record
            await _psCardService.PsCardItem.PsCardItemTransfer.UpdatePsCardItemTransfer(psCardItemTransferSource.Id, user, date);

            return psCardItemTransfer;
        }

        private void UpdatePsCardItem()
        {
            
        }
       
        private PsCardItem CreatePsCardItem(PsCardItem sourcePsCardItem, Guid? transferRefId, Guid? locationId, int? transOut, string user, DateTime date)
        {
            var targetPsCardItem = _db.PsCardItems.AsNoTracking().FirstOrDefault(f => f.Id == sourcePsCardItem.Id); // load source value, then use this as target            

            targetPsCardItem.Id = Guid.NewGuid();
            targetPsCardItem.TransferRefId = transferRefId;
            targetPsCardItem.TranType = "T";
            targetPsCardItem.LocationId = locationId;
            targetPsCardItem.Qty = null;
            targetPsCardItem.TransferIn = transOut;
            targetPsCardItem.TransferOut = null;
            targetPsCardItem.QtyIss = null;
            targetPsCardItem.QtyBal = transOut;
            targetPsCardItem.Amount = transOut * targetPsCardItem.UnitCost;
            targetPsCardItem.GTotalCost = transOut * targetPsCardItem.TUnitCost;
            targetPsCardItem.InsertedBy = user;
            targetPsCardItem.InsertedDt = date;
            targetPsCardItem.UpdatedBy = user;
            targetPsCardItem.UpdatedDt = date;

            _db.PsCardItems.Add(targetPsCardItem);
            _db.SaveChanges();

            var totalTransferOut = _db.PsCardItemTransfers.Where(w => w.PsCardItemId == sourcePsCardItem.Id).Sum(s => s.Qty) ?? 0;
            var qtyBal = ((sourcePsCardItem.Qty ?? 0) + (sourcePsCardItem.TransferIn ?? 0)) - ((sourcePsCardItem.QtyIss ?? 0) + totalTransferOut);

            sourcePsCardItem.TransferOut = totalTransferOut;
            sourcePsCardItem.QtyBal = qtyBal;
            sourcePsCardItem.Amount = qtyBal * sourcePsCardItem.UnitCost;
            sourcePsCardItem.GTotalCost = qtyBal * sourcePsCardItem.TUnitCost;
            sourcePsCardItem.UpdatedBy = user;
            sourcePsCardItem.UpdatedDt = date;

            _db.PsCardItems.Attach(sourcePsCardItem);
            _db.Entry(sourcePsCardItem).State = EntityState.Modified;
            _db.SaveChanges();

            return targetPsCardItem;
        }

        /*
         * returns PsCardItemId that w/o transit of the PsCardItemExtn.Id
         * if It has a transit record, move to the transitted PsCardItem and repeat the same verification process.
         */
        private Guid? GetPsCardItemIdWithoutTransit(Guid? psCardItemId, Guid? icsParItemId)
        {
            // if icsParItem has transit record
            if (_db.PsCardItemTransferItems.Any(a => a.IcsParItemId == icsParItemId && a.PsCardItemTransfer.PsCardItemId == psCardItemId))
            {
                // move to the transit record
                var psCardItemTransfer = _db.PsCardItemTransfers
                    .AsNoTracking()
                    .Where(w => w.PsCardItemId == psCardItemId
                        && w.PsCardItemTransferItems.Any(a => a.IcsParItemId == icsParItemId)).FirstOrDefault();
                if (psCardItemTransfer == null)
                {
                    throw new NotFoundException("Transit Item record is missing.");                    
                }
                
                var psCardItem = _db.PsCardItems
                    .AsNoTracking()
                    .Where(w => w.TransferRefId == psCardItemTransfer.Id).FirstOrDefault();
                if (psCardItem == null)
                {
                    throw new NotFoundException("Transit Card Item record is missing.");
                }

                return psCardItem.Id;
            }

            return psCardItemId;
        }
    }
}