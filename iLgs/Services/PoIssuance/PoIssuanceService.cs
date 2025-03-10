using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
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
        ValueTask<PsCardItemVM> GetByIdAsync(Guid? id);
        IQueryable<PsCardItemVM> GetSummary();
        IQueryable<PsCardItemExtnTransitVM> GetCardItemExtnForTransit(Guid? psCardItemId, Guid? groupId);
        ValueTask PostAsync(Guid psCardItemIssuanceId, string user, DateTime date);
        ValueTask UnpostAsync(Guid psCardItemIssuanceId, string user, DateTime date);
        ValueTask<PsCardItemVM> TransferAsync(PsCardItemVM model, string user, DateTime date);
    }

    public class PoIssuanceService : IPoIssuanceService
    {
        private decimal _parPrice = 50000;
        private readonly AppManEntities _db;
        private IUserService _userService;
        private readonly IExceptionService<RisIssuedVM> _vmExceptionService = new ExceptionService<RisIssuedVM>();
        private readonly IExceptionService<PsCardItemVM> _psCardItemVMrExceptionService = new ExceptionService<PsCardItemVM>();
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;
        private readonly IPsCardService _psCardService;

        public PoIssuanceService(AppManEntities db)
        {
            _db = db;
            _userService = new UserService(_db);
            _psCardItemTransactionService = new PsCardItemTransactionService(_db);
            _psCardService = new PsCardService(_db);
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
                ParBalance = s.QtyBal - (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.Id == s.Id && w.IcsPar.RefType == "P").Sum(x => x.Qty) ?? 0),
                IcsBalance = s.QtyBal - (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.Id == s.Id && w.IcsPar.RefType == "I").Sum(x => x.Qty) ?? 0),
                RemBalance = s.QtyBal,
                Department = s.Codextn.Description,
                Location = s.Codextn1.Description,
                LocCode = s.Codextn1.Code,
                InvDistDesc = _db.Codextns.Where(w => w.CodeMast.Code == "PS-REMARKS" && w.Code == s.InvDist).FirstOrDefault() == null ? "" :
                    _db.Codextns.Where(w => w.CodeMast.Code == "PS-REMARKS" && w.Code == s.InvDist).FirstOrDefault().Description
            };
        }

        public async ValueTask<IQueryable<PsCardItemVM>> GetAllAsync(string userId)
        {
            var IsAdmin = await _userService.IsAdmin(userId);
            var data = _db.Database.SqlQuery<PsCardItemVM>("Exec PoIssuance_GetRecords {0}, {1}", IsAdmin, userId).AsQueryable();
            return data;
        }

        public async ValueTask<PsCardItemVM> GetByIdAsync(Guid? id)
        {
            var data = await _db.PsCardItems
                .Include(i => i.Codextn)
                .Include(i => i.Codextn1)
                .Where(w => w.Id == id)
                .Select(GetPsCardItemProjection(_db)).FirstOrDefaultAsync();
            return data;
        }

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

        public IQueryable<PsCardItemExtnTransitVM> GetCardItemExtnForTransit(Guid? psCardItemId, Guid? groupId)
        {
            var itemExtnName = _psCardService.GetItemExtnName(psCardItemId);
            return _db.Database.SqlQuery<PsCardItemExtnTransitVM>("Exec PsCardItemExtn_GetItemsForTransit {0}, {1}", psCardItemId, itemExtnName).AsQueryable();

            //switch (itemExtnName)
            //{
            //    case "ItemExtnLand":
                    
            //    case "ItemExtnBldg":
            //        return GetCardItemExtnSetForIcsPars<PsCardItemExtnBuilding, PsCardItemExtnTransitVM>(psCardItemId, s => new PsCardItemExtnTransitVM
            //        {
            //            Id = s.Id,
            //            PsCardItemId = s.PsCardItemId,
            //            SetLotNo = s.SetLotNo,
            //            SetLotQtyNo = s.SetLotQtyNo,
            //            ContentNo = s.ContentNo,
            //            RefNo = s.ProjectName,
            //            IcsParItemId = s.IcsParItems.FirstOrDefault().Id,
            //            IcsParId = s.IcsParItems.FirstOrDefault().IcsParId,
            //            IcsParNo = s.IcsParItems.FirstOrDefault().IcsPar.RefNo,
            //            LocationId = s.IcsParItems.FirstOrDefault().IcsPar.LocationId,
            //            LocationCode = s.IcsParItems.FirstOrDefault().IcsPar.LocationCode,
            //            Location = s.IcsParItems.FirstOrDefault().IcsPar.Location,
            //        });
            //    case "ItemExtnVehicle":
            //        return GetCardItemExtnSetForIcsPars<PsCardItemExtnVehicle, PsCardItemExtnTransitVM>(psCardItemId, s => new PsCardItemExtnTransitVM
            //        {
            //            Id = s.Id,
            //            PsCardItemId = s.PsCardItemId,
            //            SetLotNo = s.SetLotNo,
            //            SetLotQtyNo = s.SetLotQtyNo,
            //            ContentNo = s.ContentNo,
            //            RefNo = s.ConductionNo,
            //            IcsParItemId = s.IcsParItems.FirstOrDefault().Id,
            //            IcsParId = s.IcsParItems.FirstOrDefault().IcsParId,
            //            IcsParNo = s.IcsParItems.FirstOrDefault().IcsPar.RefNo,
            //            LocationId = s.IcsParItems.FirstOrDefault().IcsPar.LocationId,
            //            LocationCode = s.IcsParItems.FirstOrDefault().IcsPar.LocationCode,
            //            Location = s.IcsParItems.FirstOrDefault().IcsPar.Location,
            //        });
            //    default:                    
            //        return _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().AsNoTracking()
            //                .Include(i => i.IcsParItems)
            //                .Where(w => w.PsCardItemId == psCardItemId
            //                    //&& w.IcsParItems.Any(a => a.PsCardItemExtnId == w.GroupId
            //                    //        && a.IcsPar.PostedDt != null
            //                    //            && !_db.IcsParUpdates.Any(b => b.PrevRefNo == a.IcsPar.RefNo
            //                    //                && b.RefType == a.IcsPar.RefType))
            //                    && _db.IcsParItems.Any(a => a.PsCardItemExtnId == w.GroupId
            //                            && a.IcsPar.PostedDt != null
            //                                && !_db.IcsParUpdates.Any(b => b.PrevRefNo == a.IcsPar.RefNo
            //                                    && b.RefType == a.IcsPar.RefType))
            //                    && ((w.PsCardItem.TransferRefId == null // must not exists in transit
            //                        && !w.PsCardItemTransferItems.Any(a => a.PsCardItemExtnId == w.GroupId
            //                            && a.PsCardItemTransfer.PsCardItemId == psCardItemId)
            //                        )
            //                        ||
            //                        (w.PsCardItem.TransferRefId != null // only transit items                                                                        
            //                            //&& _db.PsCardItemTransfers.Any(a => a.Id == w.PsCardItem.TransferRefId
            //                            //    && a.PsCardItemId == psCardItemId
            //                            //    && a.PsCardItemTransferItems.Any(b => b.PsCardItemExtnId == w.Id))

            //                            //&& _db.PsCardItemTransferItems.Any(a => a.PsCardItemExtnId == w.Id
            //                            //    && a.PsCardItemTransfer.Id == w.PsCardItem.TransferRefId
            //                            //        && a.PsCardItemTransfer.PsCardItemId == psCardItemId)

            //                            //&& w.PsCardItemTransferItems.Any(a => a.PsCardItemExtnId == w.Id
            //                            //    && a.PsCardItemTransfer.PsCardItemId == psCardItemId)
            //                            ))
                            
            //                )
            //                .Select(s => new PsCardItemExtnTransitVM
            //                {
            //                    Id = s.Id,
            //                    PsCardItemId = s.PsCardItemId,
            //                    SetLotNo = s.SetLotNo,
            //                    SetLotQtyNo = s.SetLotQtyNo,
            //                    ContentNo = s.ContentNo,
            //                    RefNo = s.SerialNo,
            //                    IcsParItemId = s.IcsParItems.FirstOrDefault().Id,
            //                    IcsParId = s.IcsParItems.FirstOrDefault().IcsParId,
            //                    IcsParNo = s.IcsParItems.FirstOrDefault().IcsPar.RefNo,
            //                    LocationId = s.IcsParItems.FirstOrDefault().IcsPar.LocationId,
            //                    LocationCode = s.IcsParItems.FirstOrDefault().IcsPar.LocationCode,
            //                    Location = s.IcsParItems.FirstOrDefault().IcsPar.Location
            //                })
            //                .AsQueryable();
            //}
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

        public ValueTask<PsCardItemVM> TransferAsync(PsCardItemVM model, string user, DateTime date) =>
        _psCardItemVMrExceptionService.TryCatch(async () =>
        {
            List<PsCardItemExtnTransitVM> selectedItems = null;
            var psCardItem = await _db.PsCardItems.FindAsync(model.Id);

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

                // Transit Items
                // Group by IcsParId = same location
                var itemExtnName = _psCardService.GetItemExtnName(psCardItem.Id);

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

                if (psCardItem.LocationId != null)
                {
                    if (selectedItemIcsParIds.Any(a => a.LocationId == psCardItem.LocationId))
                    {
                        throw new InvalidValueException("Cannot transit items within the same PO Location");
                    }
                }
                else
                {
                    if (selectedItemIcsParIds.Any(a => a.LocationId == psCardItem.DeptId))
                    {
                        throw new InvalidValueException("Cannot transit items within the same PO Location");
                    }
                }

                foreach(var s in selectedItemIcsParIds)
                {                    
                    var psCardItemTransfer = CreatePsCardItemTransfer(psCardItem, model.TransDate, s.Count, s.LocationId, user, date);
                    var newPsCardItem = CreatePsCardItem(psCardItem, psCardItemTransfer.Id, s.LocationId, s.Count, user, date);

                    // Save Transit Items
                    var transitItems = selectedItems.Where(w => w.IcsParId == s.IcsParId && w.LocationId == s.LocationId);
                    foreach(var transitItem in transitItems)
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

                        PsCardItemExtn psCardItemExtn = null;

                        if (itemExtnName == "itemExtnLand")
                        {
                            psCardItemExtn = _db.PsCardItemExtns.OfType<PsCardItemExtnLand>()
                                .AsNoTracking()
                                .FirstOrDefault(f => f.Id == transitItem.Id);
                        }
                        else if (itemExtnName == "ItemExtnBldg")
                        {
                            psCardItemExtn = _db.PsCardItemExtns.OfType<PsCardItemExtnBuilding>()
                                .AsNoTracking()
                                .FirstOrDefault(f => f.Id == transitItem.Id);
                        }
                        else if (itemExtnName == "ItemExtnVehicle")
                        {
                            psCardItemExtn = _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>()
                                .AsNoTracking()
                                .FirstOrDefault(f => f.Id == transitItem.Id);
                        }
                        else
                        {
                            psCardItemExtn = _db.PsCardItemExtns.OfType<PsCardItemExtnOther>()
                                .AsNoTracking()
                                .FirstOrDefault(f => f.Id == transitItem.Id);
                        }

                        if (psCardItemExtn != null)
                        {
                            psCardItemExtn.Id = Guid.NewGuid();
                            psCardItemExtn.PsCardItemId = newPsCardItem.Id;
                            psCardItemExtn.PsCardItemTransferItemId = psCardItemTransferItem.Id;
                            psCardItemExtn.InsertedBy = user;
                            psCardItemExtn.InsertedDt = date;
                            psCardItemExtn.UpdatedBy = user;
                            psCardItemExtn.UpdatedDt = date;

                            _db.PsCardItemExtns.Add(psCardItemExtn);
                            _db.Entry(psCardItemExtn).State = EntityState.Added;
                            await _db.SaveChangesAsync();
                        }
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

                if (model.TransferOut > psCardItem.QtyBal)
                {
                    throw new InvalidValueException("Transit out must not be greater than the balance!");
                }

                var psCardItemTransfer = CreatePsCardItemTransfer(psCardItem, model.TransDate, model.TransferOut, model.LocationId, user, date);
                CreatePsCardItem(psCardItem, psCardItemTransfer.Id, model.LocationId, model.TransferOut, user, date);
            }                                              

            return model;
        });

        private PsCardItemTransfer CreatePsCardItemTransfer(PsCardItem sourcePsCardItem, DateTime? transDate, int? transOut, Guid? locationId, string user, DateTime date)
        {
            var psCardItemTransfer = new PsCardItemTransfer()
            {
                Id = Guid.NewGuid(),
                PsCardItemId = sourcePsCardItem.Id,
                Qty = transOut,
                TransDate = transDate,
                InsertedBy = user,
                InsertedDt = date,
                UpdatedBy = user,
                UpdatedDt = date
            };

            _db.PsCardItemTransfers.Add(psCardItemTransfer);
            _db.SaveChanges();                                   

            return psCardItemTransfer;
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