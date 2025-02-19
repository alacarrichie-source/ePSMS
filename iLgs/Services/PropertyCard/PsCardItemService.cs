using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Items;
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.PropertyCard
{
    public interface IPsCardItemService
    {
        IQueryable<PsCardItemVM> GetByCardId(Guid? cardId, string userName);
        IQueryable<PsCardItemVM> GetAllStocks();
        IQueryable<PsCardItemVM> GetAllProperties();
        ValueTask<PsCardItemVM> GetByIdAsync(Guid? id);
        ValueTask<string> GetCategoryAsync(Guid? psCardItemId);

        ValueTask<PsCardItemVM> CreateAsync(PsCardItemVM model, string user, DateTime date);
        ValueTask<PsCardItemVM> UpdateAsync(PsCardItemVM model, string user, DateTime date);
        ValueTask<ParIcsItemVm> UpdateIsForICSAsync(ParIcsItemVm model, string user, DateTime date);
        ValueTask<ParIcsItemVm> UpdateNoICSAsync(ParIcsItemVm model, string user, DateTime date);
        ValueTask<PsCardItemVM> DeleteAsync(PsCardItemVM model, string user, DateTime date);
        PsCardItemVM TransferItemField(PsCardItemVM sourceModel, PsCardItemVM targetModel);

        ValueTask<PsCardItem> PostAsync(Guid id, string user, DateTime date);
        ValueTask<PsCardItem> UnpostAsync(Guid id, string user, DateTime date);

        Task PostBatchAsync(string userName, string postedBy, DateTime date);
        Task UnpostBatchAsync(string userName, string postedBy, DateTime date);

        IPsCardItemExtnService PsCardItemExtn { get; }
    }

    public class PsCardItemService : IPsCardItemService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<PsCardItemVM> _vmExceptionService = new ExceptionService<PsCardItemVM>();
        private readonly IExceptionService<PsCardItem> _exceptionService = new ExceptionService<PsCardItem>();
        private readonly IExceptionService<ParIcsItemVm> _parIcsItemExceptionService = new ExceptionService<ParIcsItemVm>();
        private readonly IPsCardItemValidator _psCardItemValidator;
        private readonly IItemCodeService _itemCodeService;
        private readonly IUserService _userService;
        private IPsCardItemExtnService _psCardItemExtnService;

        public PsCardItemService(AppManEntities db)
        {
            _db = db;
            _psCardItemExtnService = new PsCardItemExtnService(_db);
            _psCardItemValidator = new PsCardItemValidator(_db);
            _itemCodeService = new ItemCodeService(_db);
            _userService = new UserService(_db);
        }

        public IPsCardItemExtnService PsCardItemExtn { get { return _psCardItemExtnService = _psCardItemExtnService ?? new PsCardItemExtnService(_db); } }

        private Expression<Func<PsCardItem, PsCardItemVM>> GetPsCardItemProjection()
        {
            return s => new PsCardItemVM
            {
                Id = s.Id,
                GroupId = s.GroupId,
                PsCardId = s.PsCardId,
                OrderItemId = s.OrderItemId,
                PoDate = s.PoDate,
                PoNo = s.PoNo,
                AirDate = s.AirDate,
                AirNo = s.AirNo,
                AirIssueDate = s.AirIssueDate,
                Qty = s.Qty,
                QtyIss = s.QtyIss,
                QtyBal = s.QtyBal,
                TransferIn = s.TransferIn,
                TransferOut = s.TransferOut,
                TranType = s.TranType,
                Days = s.Days,
                Unit = s.Unit,
                UnitCost = s.UnitCost,
                //Amount = s.Amount,
                Amount = s.UnitCost * s.Qty,
                IssueAmount = (s.PsCardItemIssuances.Sum(sum => sum.Qty) ?? 0) * s.UnitCost,
                BalanceAmount = (s.UnitCost * s.Qty) - ((s.PsCardItemIssuances.Sum(sum => sum.Qty) ?? 0) * s.UnitCost),
                PriceRate = s.PriceRate,
                AddCost = s.AddCost,
                TUnitCost = s.TUnitCost,
                GTotalCost = s.GTotalCost,
                Remarks = s.Remarks,
                DeptId = s.DeptId,
                LocationId = s.LocationId,
                DeptDisplay = s.DeptDisplay,
                Description = s.Description,
                OtherDesc = s.OtherDesc,
                IsForICS = s.IsForICS,
                IsConsumable = s.IsConsumable,
                IsIncorporated = s.IsIncorporated,
                IsOthers = s.IsOthers,
                OtherRemarks = s.OtherRemarks,
                Type = s.Type,
                InvDist = s.InvDist,
                AcqMode = s.AcqMode,
                AcqDate = s.AcqDate,
                AreaSoldDonated = s.AreaSoldDonated,
                ConstructionYear = s.ConstructionYear,
                Vendor = s.Vendor,
                OldAmount = s.OldAmount,
                PhaseNo = s.PhaseNo,
                PhaseAmount = s.PhaseAmount,
                InsertedBy = s.InsertedBy,
                InsertedDt = s.InsertedDt,
                Department = s.Codextn.Description,
                Location = s.Codextn1.Description,
                LocCode = s.Codextn1.Code,
                PrevPsNo = s.PrevPsNo,
                SetLotNo = s.SetLotNo,
                SetLotAmount = s.SetLotAmount,
                SetLotRemarks = s.SetLotRemarks,
                PostedBy = s.PostedBy,
                PostedDt = s.PostedDt,
                IsWithItemExtn = s.PsCardItemExtns.Any()
            };
        }

        public async ValueTask<PsCardItemVM> GetByIdAsync(Guid? id)
        {
            var data = await _db.PsCardItems
                .Include(i => i.Codextn) // Department
                .Include(i => i.Codextn1) // Location
                .Include(i => i.PsCardItemExtns)
                .Where(w => w.Id == id)
                .Select(GetPsCardItemProjection()).FirstOrDefaultAsync();
            return data;
        }

        public IQueryable<PsCardItemVM> GetByCardId(Guid? cardId, string userName) => _vmExceptionService.TryCatch(() =>
        {
            IQueryable<PsCardItemVM> data = null;
            if (string.IsNullOrWhiteSpace(userName))
            {
                data = _db.PsCardItems
                    .Include(i => i.Codextn) // Department
                    .Include(i => i.Codextn1) // Location
                    .Where(w => w.PsCardId == cardId).AsNoTracking()
                    .Select(GetPsCardItemProjection());
            }
            else
            {
                data = _db.PsCardItems
                    .Include(i => i.Codextn) // Department
                    .Include(i => i.Codextn1) // Location
                    .Where(w => w.PsCardId == cardId && w.InsertedBy == userName).AsNoTracking()
                    .Select(GetPsCardItemProjection());
            }
            return data;
        });

        public IQueryable<PsCardItemVM> GetAllStocks() => _vmExceptionService.TryCatch(() =>
        {
            return GetAll("S");
        });

        public IQueryable<PsCardItemVM> GetAllProperties() => _vmExceptionService.TryCatch(() =>
        {
            return GetAll("P");
        });

        private IQueryable<PsCardItemVM> GetAll(string category)
        {            
            var data = _db.Database.SqlQuery<PsCardItemVM>("Exec Card_GetQueryRecords {0}", category).AsQueryable();
            return data;
        }

        public async ValueTask<string> GetCategoryAsync(Guid? psCardItemId)
        {
            return await _db.PsCardItems.Where(w => w.Id == psCardItemId).Select(s => s.PsCard.ItemCode.ItemType.Code).FirstOrDefaultAsync();
        }

        public ValueTask<PsCardItemVM> CreateAsync(PsCardItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            _psCardItemValidator.ValidateOnCreate(model);

            model.Id = Guid.NewGuid();
            
            var entity = new PsCardItem();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.PsCardItems.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<PsCardItemVM> UpdateAsync(PsCardItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            _psCardItemValidator.ValidateOnUpdate(model);
            
            var entity = await _db.PsCardItems.FindAsync(model.Id);

            ValidateUser(entity, model);
            //ValidateRelationship(model);
            MapModelToEntityFields(entity, model, Mode.EDIT);

            _db.PsCardItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<ParIcsItemVm> UpdateIsForICSAsync(ParIcsItemVm model, string user, DateTime date) => _parIcsItemExceptionService.TryCatch(async () =>
        {
            var entity = await _db.PsCardItems.FindAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (!string.IsNullOrWhiteSpace(entity.ParPostedBy))
            {
                throw new RecordAlreadyPostedException("Item already posted, cannot update!"); ;
            }

            if (entity.IsForICS != model.IsForICS) // change in isForICS
            {
                //var icsParItem = await _db.IcsParItems.Include(i => i.IcsPar).FirstOrDefaultAsync(f => f.PsCardItemExtn.PsCardItemId == model.Id);
                var icsParItem = await _db.IcsParItems.Include(i => i.IcsPar).FirstOrDefaultAsync(f => f.PsCardItemExtn.PsCardItem.GroupId == model.GroupId);
                if (icsParItem != null)
                {
                    if (model.IsForICS == true && icsParItem.IcsPar.RefType == "P")
                    {
                        throw new InvalidValueException("Item with PAR already exists, cannot make this as For ICS.");
                    }
                    else
                    {
                        if (model.IsForICS == false && icsParItem.IcsPar.RefType == "I")
                        {
                            throw new InvalidValueException("Item with ICS already exists, cannot remove this as For ICS.");
                        }
                    }
                }
            }

            var addCost = model.AddCost ?? 0;
            var tUnitCost = addCost + model.UnitCost;
            var gTotalCost = model.Qty * tUnitCost;
            entity.AddCost = addCost;
            entity.TUnitCost = tUnitCost;
            entity.GTotalCost = gTotalCost;
            entity.IsForICS = model.IsForICS;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PsCardItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<ParIcsItemVm> UpdateNoICSAsync(ParIcsItemVm model, string user, DateTime date) => _parIcsItemExceptionService.TryCatch(async () =>
        {
            if (model.IsOthers == true && string.IsNullOrWhiteSpace(model.OtherRemarks))
            {
                throw new InvalidValueException("Remarks field is required if Others is selected!");
            }

            var entity = _db.PsCardItems.Where(w => w.GroupId == model.GroupId);
            if (entity.Count() == 0)
            {
                throw new RecordNotFoundException(model.Id);
            }

            var psCardItem = await entity.FirstOrDefaultAsync(f => f.TransferRefId == null);

            if (psCardItem.IsConsumable != model.IsConsumable
                || psCardItem.IsIncorporated != model.IsIncorporated
                || psCardItem.IsOthers != model.IsOthers) // change in decision
            {
                //var icsParItem = await _db.IcsParItems.Include(i => i.IcsPar).FirstOrDefaultAsync(f => f.PsCardItemExtn.PsCardItemId == model.Id);
                var icsParItem = await _db.IcsParItems.Include(i => i.IcsPar).FirstOrDefaultAsync(f => f.PsCardItemExtn.PsCardItem.GroupId == model.GroupId);
                if (icsParItem != null)
                {
                    if ((model.IsConsumable == true
                        || model.IsIncorporated == true
                        || model.IsOthers == true)
                        && icsParItem.IcsPar.RefType == "P")
                    {
                        throw new InvalidValueException("Item with PAR already exists, cannot make this as For ICS.");
                    }
                    else
                    {
                        if ((model.IsConsumable == false
                            || model.IsIncorporated == false
                            || model.IsOthers == false)
                            && icsParItem.IcsPar.RefType == "I")
                        {
                            throw new InvalidValueException("Item with ICS already exists, cannot remove this as For ICS.");
                        }
                    }
                }
            }

            await entity.ForEachAsync(f =>
            {
                f.IsConsumable = model.IsConsumable;
                f.IsIncorporated = model.IsIncorporated;
                f.IsOthers = model.IsOthers;
                f.OtherRemarks = model.IsOthers == true ? model.OtherRemarks : "";
                f.UpdatedBy = user;
                f.UpdatedDt = date;
            });
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<PsCardItemVM> DeleteAsync(PsCardItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            _psCardItemValidator.ValidateOnDelete(model);

            //using (var transaction = _db.Database.BeginTransaction())
            //{
            //    try
            //    {

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItems.FindAsync(model.Id);
            ValidateUser(entity, model);
            ValidateRelationship(model);

            // Get transfer record if any
            PsCardItemTransfer psCardItemTransfer = null;
            if (entity.TransferRefId != null)
            {
                psCardItemTransfer = await _db.PsCardItemTransfers.FindAsync(entity.TransferRefId);
            }

            // put back transferred items
            if (psCardItemTransfer != null)
            {
                var psCardItem = await _db.PsCardItems.FindAsync(psCardItemTransfer.PsCardItemId);
                if (psCardItem != null)
                {
                    // transfer itemextn if any
                    var psCardItemId = psCardItemTransfer.PsCardItemId;
                    var psCardItemExtn = _db.PsCardItemExtns.Where(w => w.PsCardItemId == model.Id);
                    await psCardItemExtn.ForEachAsync(f =>
                    {
                        f.PsCardItemId = psCardItemId;
                        f.UpdatedBy = user;
                        f.UpdatedDt = date;
                    });
                    await _db.SaveChangesAsync();

                    psCardItem.TransferOut -= (psCardItemTransfer.Qty ?? 0);
                    psCardItem.QtyBal = ((psCardItem.Qty ?? 0) + (psCardItem.TransferIn ?? 0)) - ((psCardItem.TransferOut ?? 0) + (psCardItem.QtyIss ?? 0));
                    psCardItem.UpdatedBy = user;
                    psCardItem.UpdatedDt = date;

                    _db.PsCardItems.Attach(psCardItem);
                    _db.Entry(psCardItem).State = EntityState.Modified;
                    await _db.SaveChangesAsync();

                    // delete transfer record
                    psCardItemTransfer.UpdatedBy = user;
                    psCardItemTransfer.UpdatedDt = date;
                    _db.PsCardItemTransfers.Attach(psCardItemTransfer);
                    _db.Entry(psCardItemTransfer).State = EntityState.Modified;
                    await _db.SaveChangesAsync();

                    _db.PsCardItemTransfers.Remove(psCardItemTransfer);
                    _db.Entry(psCardItemTransfer).State = EntityState.Deleted;
                    await _db.SaveChangesAsync();
                }
            }

            // manually remove transaction log
            var psCardItemTransacctions = _db.PsCardItemTransactions.Where(w => w.PsCardItemId == entity.Id);
            _db.PsCardItemTransactions.RemoveRange(psCardItemTransacctions);
            await _db.SaveChangesAsync();

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCardItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            //    // Commit the transaction if all operations succeed
            //    transaction.Commit();
            //}
            //catch (Exception)
            //{
            //    // Rollback the transaction if any operation fails
            //    transaction.Rollback();
            //    throw;
            //}
            //}

            return model;
        });

        public PsCardItemVM TransferItemField(PsCardItemVM sourceModel, PsCardItemVM targetModel)
        {
            targetModel.Type = sourceModel.Type;
            targetModel.AcqDate = sourceModel.AcqDate;
            targetModel.AcqMode = sourceModel.AcqMode;
            targetModel.InvDist = sourceModel.InvDist;
            targetModel.AreaSoldDonated = sourceModel.AreaSoldDonated;
            targetModel.ConstructionYear = sourceModel.ConstructionYear;
            targetModel.Vendor = sourceModel.Vendor;
            return targetModel;
        }

        public void MapModelToEntityFields(PsCardItem entity, PsCardItemVM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.GroupId = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            entity.PsCardId = model.PsCardId;
            entity.OrderItemId = model.OrderItemId;
            entity.PoDate = model.PoDate;
            entity.PoNo = model.PoNo.Trim();
            entity.AirDate = model.AirDate;
            entity.AirNo = model.AirNo;
            entity.AirIssueDate = model.AirIssueDate;
            entity.Qty = model.Qty;
            entity.QtyIss = model.QtyIss;
            entity.QtyBal = model.QtyBal;
            entity.TransferIn = model.TransferIn;
            entity.TransferOut = model.TransferOut;
            entity.Days = model.Days;
            entity.TranType = model.TranType;
            entity.Unit = model.Unit;
            entity.UnitCost = model.UnitCost;
            entity.Remarks = model.Remarks;
            entity.Amount = model.Amount;
            entity.PriceRate = model.PriceRate;
            entity.AddCost = model.AddCost;
            entity.TUnitCost = model.TUnitCost;
            entity.GTotalCost = model.Amount + model.AddCost;
            entity.DeptId = model.DeptId;
            entity.LocationId = model.LocationId;
            entity.Description = model.Description;
            entity.OtherDesc = model.OtherDesc;
            entity.DeptDisplay = model.DeptDisplay;
            entity.Type = model.Type;
            entity.InvDist = model.InvDist;
            entity.AcqDate = model.AcqDate;
            entity.AcqMode = model.AcqMode;
            entity.AreaSoldDonated = model.AreaSoldDonated;
            entity.ConstructionYear = model.ConstructionYear;
            entity.Vendor = model.Vendor;
            entity.OldAmount = model.OldAmount;
            entity.PhaseNo = model.PhaseNo;
            entity.PhaseAmount = model.PhaseAmount;
            entity.SetLotNo = model.SetLotNo;
            entity.SetLotAmount = model.SetLotAmount;
            entity.SetLotRemarks = model.SetLotRemarks;
            entity.PrevPsNo = model.PrevPsNo;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        public virtual ValueTask<PsCardItem> PostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.PsCardItems.FindAsync(id);
            ValidateRecord(entity);
            ValidateIfPosted(entity);

            entity.PostedBy = user;
            entity.PostedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PsCardItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        public virtual ValueTask<PsCardItem> UnpostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.PsCardItems.FindAsync(id);
            ValidateRecord(entity);
            ValidateIfNotPosted(entity);

            entity.PostedBy = "";
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PsCardItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        private void ValidateRecord(PsCardItem entity)
        {
            if (entity == null)
            {
                throw new NotFoundException(entity.Id);
            }
        }

        private void ValidateIfPosted(PsCardItem entity)
        {
            if (entity.PostedDt != null)
            {
                var msg = $"PO record already posted by {entity.PostedBy} on {entity.PostedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        private void ValidateIfNotPosted(PsCardItem entity)
        {
            if (entity != null && entity.PostedDt == null)
            {
                var msg = $"Record not yet posted!";
                throw new RecordNotYetPostedException(msg);
            }
        }

        public bool IsPosted(Guid psCardItemId)
        {
            var entity = _db.PsCardItems.Find(psCardItemId);
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }

        public bool IsPosted(PsCardItem psCardItem)
        {
            return IsPosted(psCardItem.Id);
        }

        //public bool IsPosted(PsCardItem psCardItem)
        //{
        //    var psCardId = (Guid)psCardItem.PsCardId;
        //    return IsPosted(psCardId);
        //}

        public bool IsPosted(PsCardItemExtn psCardItemExtn)
        {
            var psCardItemId = (Guid)psCardItemExtn.PsCardItemId;
            return IsPosted(psCardItemId);
        }

        public async Task PostBatchAsync(string userName, string postedBy, DateTime date)
        {
            var psCardItems = _db.PsCardItems.Where(w => w.InsertedBy == userName && w.PostedDt == null);
            await psCardItems.ForEachAsync(a =>
            {
                a.PostedBy = postedBy; a.PostedDt = date; a.UpdatedBy = postedBy; a.UpdatedDt = date;
            });
            await _db.SaveChangesAsync();
        }

        public async Task UnpostBatchAsync(string userName, string postedBy, DateTime date)
        {
            var psCardItems = _db.PsCardItems.Where(w => w.InsertedBy == userName && w.PostedDt != null);
            await psCardItems.ForEachAsync(a =>
            {
                a.PostedBy = ""; a.PostedDt = null; a.UpdatedBy = postedBy; a.UpdatedDt = date;
            });
            await _db.SaveChangesAsync();
        }

        private void ValidateUser(PsCardItem entity, PsCardItemVM model)
        {
            if (entity.InsertedBy != model.UpdatedBy)
            {
                var isAdmin = _userService.IsUserNameAdmin(model.UpdatedBy);
                if (!isAdmin)
                {
                    throw new RecordLockedException($"Record can only be updated by {entity.InsertedBy} or an Admin.");
                }
            }
        }

        private void ValidateRelationship(PsCardItemVM model)
        {
            if (model.OrderItemId != null)
            {
                throw new RecordRelationshipException("Record is from AIR, cannot delete here!");
            }

            if (_db.PsCardItemIssuances.Any(a => a.PsCardItemId == model.Id))
            {
                throw new RecordRelationshipException("Issuance already exists, cannot delete!");
            }

            if (_db.PsCardItemTransfers.Any(a => a.PsCardItemId == model.Id))
            {
                throw new RecordRelationshipException("Transit already exists, cannot delete!");
            }

        }
    }
}