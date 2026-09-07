using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.Requisition
{
    public interface IRisService
    {
        IQueryable<RIS_VM> GetAll();
        ValueTask<IQueryable<RIS_VM>> GetAllAsync(string userId);
        Task<RIS_VM> GetByIdAsync(Guid id);
        Task<RIS_VM> GetByRisNoAsync(string risNo);
        //Task<RIS_VM> GetByOrderIdAsync(Guid orderId);
        Task<bool> GetAnyRisNoAsync(Guid risId, string risNo);

        ValueTask<RIS_VM> CreateAsync(RIS_VM model, string user, DateTime date);
        ValueTask<RIS_VM> UpdateAsync(RIS_VM model, string user, DateTime date);
        ValueTask<RIS_VM> DeleteAsync(RIS_VM model, string user, DateTime date);

        ValueTask<RISs> PostAsync(Guid risId, string user, DateTime date);
        ValueTask<RISs> UnpostAsync(Guid risId, string user, DateTime date);

        IRisItemService RisItem { get; }
        //IRisItemUnitGroupService UnitGroup { get; }
    }

    public partial class RisService : BaseValidator, IRisService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<RIS_VM> _risVmExceptionService;
        private readonly IExceptionService<RISs> _risExceptionService;
        private readonly IUserService _userService;
        private readonly IRisValidator _validator;
        private readonly IRisSharedService _risSharedService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        private IRisItemService _risItemService;
        //private IRisItemUnitGroupService _risItemUnitGroupService;

        public RisService(AppManEntities db)
        {
            _db = db;
            _exceptions = new CreateAndLogExceptions();
            _risVmExceptionService = new ExceptionService<RIS_VM>();
            _risExceptionService = new ExceptionService<RISs>();
            _userService = new UserService(_db);
            _validator = new RisValidator(_db);
            _risSharedService = new RisSharedService(_db);
            _getDisplayName = Utility.GetDisplayName<RIS_VM>;

            _risItemService = new RisItemService(_db);
            //_risItemUnitGroupService = new RisItemUnitGroupService(_db);
        }

        //public RisService(AppManEntities db,
        //    ICreateAndLogExceptions exceptions,
        //    IExceptionService<RIS_VM> risVmExceptionService,
        //    IExceptionService<RISs> risExceptionService,
        //    IUserService userService,
        //    IRisValidator validator)
        //{
        //    _db = db;
        //    _exceptions = exceptions;
        //    _risVmExceptionService = risVmExceptionService;
        //    _risExceptionService = risExceptionService;
        //    _userService = userService;
        //    _validator = validator;
        //}

        public IRisItemService RisItem => _risItemService;
        //public IRisItemUnitGroupService UnitGroup => _risItemUnitGroupService;

        private static Expression<Func<RISs, RIS_VM>> Projection
        = s => new RIS_VM
        {
            Id = s.Id,
            OrderRequestId = s.OrderRequestId,
            CtrlNo = s.CtrlNo,
            //Division = s.Division,
            RisNo = s.RisNo,
            RisDate = s.RisDate,
            Purpose = s.Purpose,
            RequestedBy = s.RequestedBy,
            RequestedByDesignation = s.RequestedByDesignation,
            RequestedDate = s.RequestedDate,
            ApprovedBy = s.ApprovedBy,
            ApprovedByDesignation = s.ApprovedByDesignation,
            ApprovedDate = s.ApprovedDate,
            IssuedBy = s.IssuedBy,
            IssuedByDesignation = s.IssuedByDesignation,
            IssuedDate = s.IssuedDate,
            ReceivedBy = s.ReceivedBy,
            ReceivedByDesignation = s.ReceivedByDesignation,
            ReceivedDate = s.ReceivedDate,
            InsertedBy = s.InsertedBy,
            InsertedDt = s.InsertedDt,
            UpdatedBy = s.UpdatedBy,
            UpdatedDt = s.UpdatedDt,
            PostedBy = s.PostedBy,
            PostedDt = s.PostedDt,
            // Transients
            Fund = s.OrderRequest.Request.Fund,
            Office = s.OrderRequest.Request.Department,
            FPP = s.OrderRequest.Request.FPP,
            IsPosted = s.PostedDt != null,
            IssuanceSw = false,
            PoNo = s.OrderRequest.Order.PoNo,
            PoDate = s.OrderRequest.Order.PoDate,
            PrNo = s.OrderRequest.Request.PrNo,
            TotalQtyRequested = s.RisItems.Sum(i => (decimal?)i.QtyRequest) ?? 0,
            Status = s.PostedDt != null ? "Posted" : "Draft",
            CanEdit = s.PostedDt == null,
            CanDelete = s.PostedDt == null,
            CanPost = s.PostedDt == null && s.RisItems.Any(),
            CanUnpost = s.PostedDt != null
        };

        public async ValueTask<IQueryable<RIS_VM>> GetAllAsync(string userId)
        {
            IQueryable<RIS_VM> data = null;
            if (await _userService.IsAdminAsync(userId))
            {
                data = _db.RISses.AsNoTracking()
                    .Select(Projection);
            }
            else
            {
                data = _db.RISses.AsNoTracking()
                    .Where(w => w.OrderRequest.Request.Codextn.DepartmentUsers.Any(a => a.UserId == userId))
                    .Select(Projection);
            }
            return data;
        }

        public IQueryable<RIS_VM> GetAll()
        {
            var data = _db.RISses
                .Select(Projection);
            return data;
        }

        public Task<bool> GetAnyRisNoAsync(Guid risId, string risNo)
        {
            return _db.RISses.AnyAsync(a => a.Id != risId && a.RisNo == risNo);
        }

        public Task<RIS_VM> GetByIdAsync(Guid id)
        {
            return _db.RISses.Where(w => w.Id == id).Select(Projection).FirstOrDefaultAsync();
        }

        public Task<RIS_VM> GetByRisNoAsync(string risNo)
        {
            return _db.RISses.Where(w => w.RisNo == risNo).Select(Projection).FirstOrDefaultAsync();
        }

        private ValueTask<RISs> PostCoreAsync(Guid risId, string user, DateTime date) =>
        _risExceptionService.TryCatch(async () =>
        {
            _validator.ValidateOnPost(risId);

            // This entity must be tracked.  The former AsNoTracking query caused
            // the PostedBy/PostedDt changes below to be silently discarded.
            var entity = await _db.RISses
                .Include(i => i.OrderRequest.Order)
                .FirstOrDefaultAsync(f => f.Id == risId);

            if (!entity.RisDate.HasValue)
            {
                throw new InvalidValueException("RIS Date is required when posting.");
            }

            var order = await _db.Orders.FindAsync(entity.OrderRequest.OrderId);
            if (order == null)
            {
                throw new RecordRelationshipException("Purchase Order not found.");
            }
            else
            {
                if (order.PoDate > entity.RisDate)
                {
                    throw new InvalidValueException("RIS Date must be on or after the PO Date.");
                }
            }

            if (entity.RequestedDate.HasValue)
            {
                if (entity.RequestedDate.Value.Date < entity.RisDate.Value.Date)
                {
                    throw new InvalidValueException("Requested Date must be on or after the RIS Date.");
                }
            }
            else
            {
                throw new InvalidValueException("Requested Date is required.");
            }

            if (entity.ApprovedDate.HasValue)
            {
                if (entity.ApprovedDate.Value.Date < entity.RequestedDate.Value.Date)
                {
                    throw new InvalidValueException("Approved Date must be on or after the Requested Date.");
                }
            }
            else
            {
                throw new InvalidValueException("Approved Date is required.");
            }

            if (entity.IssuedDate.HasValue)
            {
                if (entity.IssuedDate.Value.Date < entity.ApprovedDate.Value.Date)
                {
                    throw new InvalidValueException("Issued Date must be on or after the Approved Date.");
                }
            }
            else
            {
                throw new InvalidValueException("Issued Date is required.");
            }

            if (entity.ReceivedDate.HasValue)
            {
                if (entity.ReceivedDate.Value.Date < entity.IssuedDate.Value.Date)
                {
                    throw new InvalidValueException("Received Date must be on or after the Issued Date.");
                }
            }
            else
            {
                throw new InvalidValueException("Received Date is required.");
            }

            if (string.IsNullOrWhiteSpace(entity.IssuedBy))
            {
                throw new InvalidValueException("Isseud by is required.");
            }

            if (string.IsNullOrWhiteSpace(entity.IssuedByDesignation))
            {
                throw new InvalidValueException("Designation of Isseud by is required.");
            }

            if (string.IsNullOrWhiteSpace(entity.ReceivedBy))
            {
                throw new InvalidValueException("Received by is required.");
            }

            if (string.IsNullOrWhiteSpace(entity.ReceivedByDesignation))
            {
                throw new InvalidValueException("Designation of Received by is required.");
            }

            ///await ValidateUploadAsync(airId, entity.AIRNo);

            if (string.IsNullOrWhiteSpace(entity.RisNo))
            {
                entity.RisNo = NextRisNo(entity.RisDate.Value);
            }

            entity.PostedBy = user;
            entity.PostedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            await _db.SaveChangesAsync();
            return entity;

            //// crate psCode foreach item (problem in unpost, sequence number will rumble)
            //var risItemList = await db.RisItems.Include(i => i.ItemCode.ItemType).Where(w => w.RisId == entity.Id).ToListAsync();
            //foreach (var risItem in risItemList)
            //{
            //    if (!db.PsCodes.Any(a => a.PsType == risItem.ItemCode.ItemType.Code && a.PsNo == risItem.PsNoDisplay && a.ItemName == risItem.ItemName))
            //    {
            //        var psCode = new PsCode()
            //        {
            //            Id = Guid.NewGuid(),
            //            ItemCodeId = risItem.ItemCodeId,
            //            PsNo = risItem.PsNoDisplay, //NextPsNo(risItem.ItemCode.ItemType.Code),
            //            PsType = risItem.ItemCode.ItemType.Code,
            //            ItemName = risItem.ItemName,
            //            InsertedBy = user,
            //            InsertedDt = date,
            //            UpdatedBy = user,
            //            UpdatedDt = date
            //        };

            //        db.PsCodes.Add(psCode);
            //        await db.SaveChangesAsync();
            //    }
            //}
        });

        private ValueTask<RISs> UnpostCoreAsync(Guid risId, string user, DateTime date) =>
        _risExceptionService.TryCatch(async () =>
        {
            _validator.ValidateOnUnpost(risId);

            var entity = await _db.RISses.FindAsync(risId);

            entity.PostedBy = null;
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            await _db.SaveChangesAsync();
            return entity;

            //// delete un-used psCode foreach item (problem in unpost, sequence number will rumble)
            //var risItemList = await db.RisItems.Include(i => i.ItemCode.ItemType).Where(w => w.RisId == entity.Id).ToListAsync();
            //foreach (var risItem in risItemList)
            //{
            //    var psCode = await db.PsCodes.Where(w => w.PsNo == risItem.PsNoDisplay && w.ItemName == risItem.ItemName && !w.PsStocks.Any()).FirstOrDefaultAsync();
            //    if (psCode != null)
            //    {
            //        db.PsCodes.Remove(psCode);
            //        db.Entry(psCode).State = EntityState.Deleted;
            //        await db.SaveChangesAsync();
            //    }
            //}
        });

        public ValueTask<RIS_VM> CreateAsync(RIS_VM model, string user, DateTime date) =>
        _risVmExceptionService.TryCatch(async () =>
        {
            if (model == null)
            {
                throw new NullException();
            }

            if (!model.RisDate.HasValue)
            {
                throw new InvalidValueException("RIS Date is required.");
            }

            model.Id = Guid.NewGuid();
            model.RisNo = null;

            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            using (var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                LockRisTransactions();
                model.CtrlNo = NextCtrlNo(date);
                _validator.ValidateOnCreate(model);

                var entity = new RISs();
                MapModelToEntityFields(entity, model, Mode.ADD);

                // Header creation is intentionally separate from allocation.
                // OrderItemRequest records are selected later in Manage Requisition.


            //foreach (var unitGroup in order.OrderItemUnitGroups)
            //{
            //    var risItemUnitGroup = new RisItemUnitGroup()
            //    {
            //        Id = Guid.NewGuid(),
            //        RisId = model.Id,
            //        OrderItemUnitGroupId = unitGroup.Id,
            //        SetLotNo = unitGroup.SetLotNo,
            //        Qty = unitGroup.Qty,
            //        Unit = unitGroup.Unit,
            //        InsertedBy = user,
            //        InsertedDt = date,
            //        UpdatedBy = user,
            //        UpdatedDt = date
            //    };

            //    foreach (var unitGroupDescription in unitGroup.OrderItemUnitGroupDescriptions)
            //    {
            //        var risItemUnitGroupDescription = new RisItemUnitGroupDescription()
            //        {
            //            Id = Guid.NewGuid(),
            //            UnitGroupId = risItemUnitGroup.Id,
            //            OrderItemUnitGroupDescriptionId = unitGroupDescription.Id,
            //            Description = unitGroupDescription.Description,
            //            InsertedBy = user,
            //            InsertedDt = date,
            //            UpdatedBy = user,
            //            UpdatedDt = date
            //        };

            //        foreach (var unitGroupDescriptionItem in unitGroupDescription.OrderItemUnitGroupDescriptionItems)
            //        {
            //            var risItemId = entity.RisItems.FirstOrDefault(p => p.OrderItemRequest.OrderItemId == unitGroupDescriptionItem.OrderItemId).Id;
            //            var risItemUnitGroupDescriptionItem = new RisItemUnitGroupDescriptionItem()
            //            {
            //                Id = Guid.NewGuid(),
            //                UnitGroupDescriptionId = risItemUnitGroupDescription.Id,
            //                OrderItemUnitGroupDescriptionItemId = unitGroupDescriptionItem.Id,
            //                RisItemId = risItemId,
            //                InsertedBy = user,
            //                InsertedDt = date,
            //                UpdatedBy = user,
            //                UpdatedDt = date
            //            };
            //            risItemUnitGroupDescription.RisItemUnitGroupDescriptionItems.Add(risItemUnitGroupDescriptionItem);
            //        }
            //        risItemUnitGroup.RisItemUnitGroupDescriptions.Add(risItemUnitGroupDescription);
            //    }
            //    entity.RisItemUnitGroups.Add(risItemUnitGroup);
            //}

                _db.RISses.Add(entity);
                await _db.SaveChangesAsync();
                transaction.Commit();
                return model;
            }
        });

        private ValueTask<RIS_VM> DeleteCoreAsync(RIS_VM model, string user, DateTime date) =>
        _risVmExceptionService.TryCatch(async () =>
        {
            //await ValidateOnDelete(model);
            _validator.ValidateOnDelete(model);
            ValidateIfPosted(model.Id);
            ValidateRelationship(model.Id);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            using (var transaction = _db.Database.BeginTransaction())
            {
                LockRisTransactions();
                var entity = await _db.RISses.FindAsync(model.Id);
                if (entity == null)
                {
                    throw new RecordNotFoundException(model.Id);
                }

                // RIS owns only these links. Source OrderItemRequest/OrderItem records remain untouched.
                await _db.Entry(entity).ReloadAsync();
                if (entity.PostedDt.HasValue || !string.IsNullOrWhiteSpace(entity.PostedBy))
                    throw new InvalidValueException("Posted RIS cannot be deleted.");
                if (!string.IsNullOrWhiteSpace(entity.RisNo) &&
                    await _db.RSMIItems.AnyAsync(x => x.RisNo == entity.RisNo))
                    throw new InvalidValueException("This RIS is referenced by an RSMI. Resolve that dependency before deleting.");
                var groupLinks = await _db.RisItemUnitGroupDescriptionItems
                    .Where(x => x.RisItem.RisId == model.Id ||
                        x.RisItemUnitGroupDescription.RisItemUnitGroup.RisId == model.Id).ToListAsync();
                _db.RisItemUnitGroupDescriptionItems.RemoveRange(groupLinks);
                var descriptions = await _db.RisItemUnitGroupDescriptions
                    .Where(x => x.RisItemUnitGroup.RisId == model.Id).ToListAsync();
                _db.RisItemUnitGroupDescriptions.RemoveRange(descriptions);
                var groups = await _db.RisItemUnitGroups.Where(x => x.RisId == model.Id).ToListAsync();
                _db.RisItemUnitGroups.RemoveRange(groups);
                var allocations = await _db.RisItems.Where(x => x.RisId == model.Id).ToListAsync();
                _db.RisItems.RemoveRange(allocations);
                _db.RISses.Remove(entity);
                await _db.SaveChangesAsync();
                transaction.Commit();
            }

            return model;
        });

        public ValueTask<RIS_VM> UpdateAsync(RIS_VM model, string user, DateTime date) =>
        _risVmExceptionService.TryCatch(async () =>
        {
            _validator.ValidateOnUpdate(model);
            ValidateIfPosted(model.Id);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            //if (string.IsNullOrWhiteSpace(model.RisNo))
            //{
            //    model.RisNo = NextRisNo((DateTime)model.RisDate);
            //}

            var entity = await _db.RISses.FindAsync(model.Id);

            MapModelToEntityFields(entity, model, Mode.EDIT);

            await _db.SaveChangesAsync();

            return model;
        });

        public void MapModelToEntityFields(RISs entity, RIS_VM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.CtrlNo = model.CtrlNo;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            entity.OrderRequestId = model.OrderRequestId;
            entity.RisNo = model.RisNo;
            entity.RisDate = model.RisDate;
            entity.Purpose = model.Purpose?.Trim() ?? "";
            entity.RequestedBy = model.RequestedBy?.Trim() ?? "";
            entity.RequestedByDesignation = model.RequestedByDesignation?.Trim() ?? "";
            entity.RequestedDate = model.RequestedDate;
            entity.ApprovedBy = model.ApprovedBy?.Trim() ?? "";
            entity.ApprovedByDesignation = model.ApprovedByDesignation?.Trim() ?? "";
            entity.ApprovedDate = model.ApprovedDate;
            entity.IssuedBy = model.IssuedBy?.Trim() ?? "";
            entity.IssuedByDesignation = model.IssuedByDesignation?.Trim() ?? "";
            entity.IssuedDate = model.IssuedDate;
            entity.ReceivedBy = model.ReceivedBy?.Trim() ?? "";
            entity.ReceivedByDesignation = model.ReceivedByDesignation?.Trim() ?? "";
            entity.ReceivedDate = model.ReceivedDate;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        private string NextRisNo(DateTime date)
        {
            string yyyy = date.Year.ToString().Trim();
            string mm = date.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = _db.RISses.Where(w => w.RisDate.Value.Year == date.Year && w.RisNo != null && w.RisNo != "").OrderByDescending(o => o.RisNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(data.RisNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }

        private string NextCtrlNo(DateTime date)
        {
            string yyyy = date.Year.ToString().Trim();
            string mm = date.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var retval = _db.RISses.Where(w => w.CtrlNo.Substring(0, 4) == yyyy).OrderByDescending(o => o.CtrlNo).FirstOrDefault();
            if (retval == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(retval.CtrlNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }

        #region VALIDATION

        //private async ValueTask ValidateOnUpdate(RIS_VM model)
        //{
        //    var rec = await _db.RISses.FindAsync(model.Id);
        //    if (rec == null)
        //    {
        //        throw new RecordNotFoundException(model.Id);
        //    }

        //    if (!model.IssuanceSw)
        //    {
        //        if (!string.IsNullOrWhiteSpace(rec.PostedBy))
        //        {
        //            throw new RecordAlreadyPostedException(string.Format("RIS No {0} already posted. Cannot update!", rec.RisNo));
        //        }

        //        if (await IsPrPostedAsync(model.Id))
        //        {
        //            throw new RecordRelationshipException("This RIS No has a posted PR, cannot update!");
        //        }
        //    }

        //    if (await GetAnyRisNoAsync(model.Id, model.RisNo))
        //    {
        //        throw new RecordAlreadyExistsException(string.Format("RIS No {0} already exists!", model.RisNo));
        //    }
        //}

        public void ValidateIfPosted(Guid risId)
        {
            if (_risSharedService.IsPosted(risId))
            {
                throw new RecordAlreadyPostedException(string.Format("RIS No. is already posted, cannot update!"));
            }
        }


        private async Task ValidateOnDelete(RIS_VM model)
        {
            if (await _risSharedService.IsPostedAsync(model.Id))
            {
                throw new RecordAlreadyPostedException(string.Format("RIS No. {0} already Posted, cannot delete!", model.RisNo));
            }

            //if (await IsPrPostedAsync(model.Id))
            //{
            //    throw new RecordRelationshipException("This RIS Number has a posted PR, cannot delete!");
            //}
        }

        private void ValidateRelationship(Guid risId)
        {
            //var pr = _db.Requests.Where(a => a.RisId == risId).AsNoTracking().FirstOrDefault();
            //if (pr != null)
            //{
            //    throw new RecordRelationshipException($"This RIS Number is in use by PR Number {pr.PrNo}, cannot delete!");
            //}
        }

        //private async Task<bool> IsWwithUploadAsync(Guid? id)
        //{
        //    var result = await _uploadService.GetAllByImageId(id).AnyAsync();
        //    return result;
        //}

        //private async Task ValidateUploadAsync(Guid? id, string airNo)
        //{
        //    if (!await IsWwithUploadAsync(id))
        //    {
        //        throw new InvalidValueException($"No attachments found for AIR No. {airNo}, cannot post!");
        //    }
        //}

        #endregion
    }
}
