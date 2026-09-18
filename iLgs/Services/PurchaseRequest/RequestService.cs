using iLgs.Ai.Services;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.PurchaseRequest
{
    public interface IRequestService
    {
        IQueryable<RequestVM> GetAll();
        ValueTask<IQueryable<RequestVM>> GetAllAsync(string userId);
        ValueTask<IQueryable<RequestVM>> GetAllAsync(string userId, bool? isSubmitted);
        Task<RequestVM> GetByIdAsync(Guid? prId);
        Task<RequestVM> GetByPrNoAsync(string prNo);
        Task<bool> IsAnyPrNoAsync(Guid id, string prNo);
        //Task<bool> IsAnyRisNoAsync(Guid id, string risNo);
        //bool IsPosted(Guid requestId);
        //bool IsPosted(Request request);
        //bool IsPosted(RequestItem requestItem);
        //bool IsPosted(RequestItemUnitGroup requestItemUnitGroup);
        //bool IsPosted(RequestItemUnitGroupDescription requestItemunitGroupDescription);
        //bool IsPosted(RequestItemUnitGroupDescriptionItem requestItemunitGroupDescriptionItem);
        //Task<bool> IsPostedAsync(Guid? requestId);
        //Task<bool> IsWithPOAsync(Guid? requestId);
        //Task<bool> IsPoPostedAsync(Guid? requestId);
        Task<bool> IsWithInvalidUnitCostAsync(Guid? requestId);
        IDictionary ValidateOnPost(string prNo, DateTime? prDate);
        IDictionary ValidatePrNoAndDate(string prNo, DateTime? prDate);
        string NextPrNo(DateTime prDate);

        ValueTask<RequestVM> CreateAsync(RequestVM model, string user, DateTime date);
        ValueTask<RequestVM> UpdateAsync(RequestVM model, string user, DateTime date);
        ValueTask<RequestVM> UpdateAsync(RequestVM model, string user, DateTime date, bool allowAdminEdit);
        ValueTask<RequestVM> DeleteAsync(RequestVM model, string user, DateTime date);
        ValueTask PostAsync(Guid requestId, string user, DateTime date);
        ValueTask UnpostAsync(Guid requestId, string user, DateTime date);
        ValueTask SubmitAsync(Guid requestId, string user, DateTime date);
        ValueTask UnsubmitAsync(Guid requestId, string user, DateTime date);

        ValueTask<PurchaseRequestViewModel> SaveCheckoutAsync(PurchaseRequestViewModel model, string user, DateTime date);
        ValueTask<PurchaseRequestViewModel> SaveRevisionCheckoutAsync(PurchaseRequestViewModel model, string user, DateTime date);
        ValueTask<PurchaseRequestViewModel> SaveAdminEditCheckoutAsync(PurchaseRequestViewModel model, string user, DateTime date);

        IRequestItemService RequestItem { get; }
    }

    public class RequestService : BaseValidator, IRequestService
    {
        private decimal? _priceCap;
        private readonly AppManEntities _db;
        private readonly IRequestSharedService _requestSharedService;
        private readonly IUserService _userService;
        private readonly IExceptionService<RequestVM> _vmExceptionService = new ExceptionService<RequestVM>();
        private readonly IExceptionService<PurchaseRequestViewModel> _prCheckoutExceptionService = new ExceptionService<PurchaseRequestViewModel>();
        private readonly IPriceCapService _priceCapService;
        private readonly IDocumentHistoryService _documentHistoryService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        private IRequestItemService _requestItemService;

        public RequestService(AppManEntities db)
        {
            _db = db;
            _requestSharedService = new RequestSharedService(_db);
            _userService = new UserService(_db);
            _getDisplayName = propertyName => Utility.GetDisplayName<RequestVM>(propertyName);
            _priceCapService = new PriceCapService(_db);
            _requestItemService = new RequestItemService(_db);
            _documentHistoryService = new DocumentHistoryService(_db);
        }

        //public RequestService(AppManEntities db,
        //    IUserService userService,
        //    IExceptionService<RequestVM> vmExceptionService,
        //    IPriceCapService priceCapService)
        //{
        //    _db = db;
        //    _userService = userService;
        //    _vmExceptionService = vmExceptionService;
        //    _getDisplayName = propertyName => Utility.GetDisplayName<RequestVM>(propertyName);
        //    _priceCapService = priceCapService;            
        //}

        public IRequestItemService RequestItem => _requestItemService;

        private decimal GetPriceCap()
        {
            return _priceCap ?? (_priceCap = _priceCapService.GetPriceCap()).Value;
        }

        private static Expression<Func<Request, RequestVM>> Projection(AppManEntities db) {
            return s => new RequestVM
            {
                Id = s.Id,
                CtrlNo = s.CtrlNo,
                Fund = s.Fund,
                FundSpecific = s.FundSpecific,
                DeptId = s.DeptId,
                Department = s.Department,
                Section = s.Section,
                PrNo = s.PrNo,
                PrDate = s.PrDate,
                FPP = s.FPP,
                Purpose = s.Purpose,
                RequestedBy = s.RequestedBy,
                RequestedDesig = s.RequestedDesig,
                Availability = s.Availability,
                AvaialbilityDesig = s.AvaialbilityDesig,
                ApprovedBy = s.ApprovedBy,
                ApprovedDesig = s.ApprovedDesig,
                SubmittedBy = s.SubmittedBy,
                SubmittedDt = s.SubmittedDt,
                PostedBy = s.PostedBy,
                PostedDt = s.PostedDt,
                ItemCount = s.RequestItems.Count(),
                TotalAmount = s.RequestItems.Sum(i => i.TotalCost) ?? 0,
                //Status = !(s.PostedBy == null || s.PostedBy == "")
                //    ? "Posted"
                //    : (!(s.SubmittedBy == null || s.SubmittedBy == "")
                //        ? "Submitted"
                //        : "Draft"),
                Status = db.DocumentStatusHistories.Where(h => h.DocumentType == DocumentTypes.PurchaseRequest && h.DocumentId == s.Id)
                    .OrderByDescending(h => h.ChangedDt)
                    .Select(h => h.ToStatus)
                    .FirstOrDefault(),
                StatusUser = db.DocumentStatusHistories.Where(h => h.DocumentType == DocumentTypes.PurchaseRequest && h.DocumentId == s.Id)
                    .OrderByDescending(h => h.ChangedDt)
                    .Select(h => h.ChangedBy)
                    .FirstOrDefault(),
                StatusDate = db.DocumentStatusHistories.Where(h => h.DocumentType == DocumentTypes.PurchaseRequest && h.DocumentId == s.Id)
                    .OrderByDescending(h => h.ChangedDt)
                    .Select(h => h.ChangedDt)
                    .FirstOrDefault(),
                //IsWithPO = s.Orders.Any(),
                IsWithPO = s.RequestItems.Any(a => a.OrderItemRequests.Any()),
                InsertedBy = s.InsertedBy,
                InsertedDt = s.InsertedDt
            };
        }

        public IQueryable<RequestVM> GetAll()
        {
            return _db.Requests.AsNoTracking().Select(Projection(_db)).AsQueryable();
        }

        public async ValueTask<IQueryable<RequestVM>> GetAllAsync(string userId)
        {
            var data = _db.Requests.AsQueryable();
            if (!(await _userService.IsAdminAsync(userId)))
            {
                data = data.Where(w => w.Codextn.DepartmentUsers.Any(a => a.UserId == userId));
            }

            return data.Select(Projection(_db)).AsNoTracking();
        }

        public async ValueTask<IQueryable<RequestVM>> GetAllAsync(string userId, bool? isSubmitted)
        {
            var data = _db.Requests.AsQueryable();
            if (!(await _userService.IsAdminAsync(userId)))
            {
                data = data.Where(w => w.Codextn.DepartmentUsers.Any(a => a.UserId == userId));
            }

            if (isSubmitted.Value == true)
            {
                data = data.Where(w => !(w.SubmittedBy == null || w.SubmittedBy == ""));
            }

            return data.Select(Projection(_db)).AsNoTracking();
        }

        public Task<RequestVM> GetByIdAsync(Guid? prId)
        {
            return _db.Requests.Where(w => w.Id == prId).Select(Projection(_db)).FirstOrDefaultAsync();
        }

        public async Task<bool> IsAnyPrNoAsync(Guid id, string prNo)
        {
            return await _db.Requests.AnyAsync(a => a.Id != id && a.PrNo == prNo);
        }

        //public async Task<bool> IsAnyRisNoAsync(Guid id, string risNo)
        //{
        //    return await _db.Requests.AnyAsync(a => a.Id != id && a.RISs.RisNo == risNo);
        //}

        public Task<RequestVM> GetByPrNoAsync(string prNo)
        {
            return _db.Requests.Where(w => w.PrNo == prNo).Select(Projection(_db)).FirstOrDefaultAsync();
        }

        //public bool IsPosted(Guid requestId)
        //{
        //    var entity = _db.Requests.Find(requestId);
        //    return !string.IsNullOrWhiteSpace(entity.SubmittedBy);
        //}

        //public bool IsPosted(Request request)
        //{
        //    return IsPosted(request.Id);
        //}

        //public bool IsPosted(RequestItem requestItem)
        //{
        //    var requestId = (Guid)requestItem.PrId;
        //    return IsPosted(requestId);
        //}

        //public bool IsPosted(RequestItemUnitGroup requestItemUnitGroup)
        //{
        //    var requestId = (Guid)requestItemUnitGroup.PrId;
        //    return IsPosted(requestId);
        //}

        //public bool IsPosted(RequestItemUnitGroupDescription requestItemUnitGroupDescription)
        //{
        //    var requestId = (Guid)_db.RequestItemUnitGroupDescriptions
        //        .Include(i => i.RequestItemUnitGroup)
        //        .Where(w => w.RequestItemUnitGroupId == requestItemUnitGroupDescription.RequestItemUnitGroupId)
        //        .AsNoTracking()
        //        .FirstOrDefault()?.RequestItemUnitGroup.PrId;
        //    return IsPosted(requestId);
        //}

        //public bool IsPosted(RequestItemUnitGroupDescriptionItem requestItemUnitGroupDescriptionItem)
        //{
        //    var requestId = (Guid)_db.RequestItemUnitGroupDescriptionItems
        //        .Include(i => i.RequestItemUnitGroupDescription.RequestItemUnitGroup)
        //        .Where(w => w.RequestItemUnitGroupDescriptionId == requestItemUnitGroupDescriptionItem.RequestItemUnitGroupDescriptionId)
        //        .AsNoTracking()
        //        .FirstOrDefault()?.RequestItemUnitGroupDescription.RequestItemUnitGroup.PrId;
        //    return IsPosted(requestId);
        //}

        public async Task<bool> IsWithInvalidUnitCostAsync(Guid? requestId)
        {
            return await _db.RequestItems.AnyAsync(a => a.PrId == requestId && (a.UnitCost == null || a.UnitCost == 0));
        }

        public ValueTask<PurchaseRequestViewModel> SaveCheckoutAsync(PurchaseRequestViewModel model, string user, DateTime date) =>
        _prCheckoutExceptionService.TryCatch(async () =>
        {
            using (var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    var itemIds = model.Items.Select(x => x.Id).Distinct().ToList();
                    var currentItems = await _db.PPMPItems
                        .Include(x => x.PPMP)
                        .Include(x => x.PPMPItemUsages)
                        .Where(x => itemIds.Contains(x.Id))
                        .ToListAsync();

                    if (currentItems.Count != itemIds.Count)
                    {
                        throw new InvalidOperationException("One or more procurement items no longer exist.");
                    }

                    foreach (var item in model.Items)
                    {
                        var currentItem = currentItems.Single(x => x.Id == item.Id);
                        var usedQuantity = currentItem.PPMPItemUsages.Sum(x => x.Qty).GetValueOrDefault();
                        var availableQuantity = currentItem.Qty.GetValueOrDefault() - usedQuantity;

                        if (currentItem.PPMP == null ||
                            currentItem.PPMP.DeptId != model.DeptId ||
                            currentItem.PPMP.ForYear != model.ProcurementFiscalYear ||
                            item.Quantity <= 0 ||
                            item.Quantity > availableQuantity)
                        {
                            throw new InvalidOperationException(
                                item.Code + " is no longer available for the requested quantity.");
                        }

                        if (!currentItem.UnitCost.HasValue)
                        {
                            throw new InvalidOperationException(item.Code + " has no unit cost.");
                        }

                        if (String.IsNullOrWhiteSpace(item.Description))
                        {
                            throw new InvalidOperationException(item.Code + " requires a description.");
                        }

                        item.Code = currentItem.Code;
                        item.Description = item.Description.Trim();
                        item.Unit = currentItem.Unit;
                        item.UnitCost = currentItem.UnitCost;
                    }

                    var request = new RequestVM()
                    {
                        Fund = model.Fund,
                        FundSpecific = model.FundSpecific,
                        DeptId = model.DeptId,
                        Department = model.Department,
                        FPP = model.FPP,
                        Purpose = model.Purpose,
                        RequestedBy = model.RequestedBy,
                        RequestedDesig = model.RequestedDesig,
                        Availability = model.Availability,
                        AvaialbilityDesig = model.AvaialbilityDesig,
                        ApprovedBy = model.ApprovedBy,
                        ApprovedDesig = model.ApprovedDesig,
                        SubmittedBy = user,
                        SubmittedDt = date
                    };
                    var requestVM = await CreateAsync(request, user, date);
                    model.CtrlNo = requestVM.CtrlNo;

                    foreach (var item in model.Items)
                    {
                        var requestItem = new RequestItemVM()
                        {
                            PrId = requestVM.Id,
                            ItemNo = item.ItemNo,
                            ItemNoIndex = Utility.GetItemNoIndex(item.ItemNo),
                            Description = item.Description,
                            Qty = item.Quantity,
                            Unit = item.Unit,
                            UnitCost = item.UnitCost,
                            TotalCost = item.EstimatedAmount,
                            PpmpItemId = item.Id,
                            PpmpCode = item.Code
                        };
                        await RequestItem.CreateAsync(requestItem, user, date);

                        foreach (var subItem in item.SubItems ?? Enumerable.Empty<CartSubItemViewModel>())
                        {
                            if (String.IsNullOrWhiteSpace(subItem.Description) ||
                                String.IsNullOrWhiteSpace(subItem.Unit) ||
                                subItem.Quantity <= 0 ||
                                subItem.UnitCost < 0)
                            {
                                throw new InvalidOperationException(
                                    "A sub-item under " + item.Code + " contains invalid values.");
                            }

                            _db.RequestSubItems.Add(new RequestSubItem
                            {
                                Id = Guid.NewGuid(),
                                RequestItemId = requestItem.Id,
                                ItemNo = subItem.ItemNo,
                                ItemNoIndex = Utility.GetItemNoIndex(subItem.ItemNo),
                                Description = subItem.Description.Trim(),
                                Unit = subItem.Unit.Trim(),
                                Qty = subItem.Quantity,
                                UnitCost = subItem.UnitCost,
                                Total = subItem.Total,
                                InsertedBy = user,
                                InsertedDt = date,
                                UpdatedBy = user,
                                UpdatedDt = date
                            });
                        }                        
                    }

                    var oldStatus = "DRAFT";
                    var history = (await _documentHistoryService.GetLatestHistoryAsync(DocumentTypes.PurchaseRequest, request.Id));
                    if (history != null)
                    {
                        oldStatus = history.ToStatus;
                    }
                    _documentHistoryService.AddStatusHistory(
                        DocumentTypes.PurchaseRequest,
                        requestVM.Id != Guid.Empty ? requestVM.Id : request.Id,
                        request.PrNo ?? requestVM.CtrlNo ?? request.CtrlNo ?? "PR",
                        oldStatus,
                        "SUBMITTED",
                        "Submit",
                        null,
                        user);

                    if (model.CartId.HasValue)
                    {
                        var cartEntity = await _db.ProcurementCarts.FirstOrDefaultAsync(c => c.Id == model.CartId.Value);
                        if (cartEntity != null)
                        {
                            cartEntity.Status = "COMPLETED";
                            cartEntity.UpdatedBy = user;
                            cartEntity.UpdatedDt = date;
                        }
                    }
                    await _db.SaveChangesAsync();

                    transaction.Commit();
                    return model;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        });

        public ValueTask<PurchaseRequestViewModel> SaveRevisionCheckoutAsync(PurchaseRequestViewModel model, string user, DateTime date) =>
        _prCheckoutExceptionService.TryCatch(async () =>
        {
            if (!model.RequestId.HasValue || model.RequestId.Value == Guid.Empty)
                throw new InvalidOperationException("The revision Purchase Request is missing.");

            using (var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    var requestId = model.RequestId.Value;
                    var entity = await _db.Requests
                        .Include(x => x.RequestItems.Select(i => i.RequestSubItems))
                        .FirstOrDefaultAsync(x => x.Id == requestId);
                    if (entity == null)
                        throw new InvalidOperationException("The Purchase Request no longer exists.");

                    var latestStatus = await _db.DocumentStatusHistories
                        .Where(x => x.DocumentType == DocumentTypes.PurchaseRequest && x.DocumentId == requestId)
                        .OrderByDescending(x => x.ChangedDt).ThenByDescending(x => x.Id)
                        .Select(x => x.ToStatus).FirstOrDefaultAsync();
                    if (!String.Equals(latestStatus, PrStatuses.Revising, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Only a Purchase Request currently being revised can be resubmitted.");

                    if (model.Items == null || !model.Items.Any())
                        throw new InvalidOperationException("The Purchase Request must contain at least one item.");

                    var ppmpIds = model.Items.Select(x => x.Id).Distinct().ToList();
                    var ppmpItems = await _db.PPMPItems.Include(x => x.PPMP).Include(x => x.PPMPItemUsages)
                        .Where(x => ppmpIds.Contains(x.Id)).ToListAsync();
                    if (ppmpItems.Count != ppmpIds.Count)
                        throw new InvalidOperationException("One or more procurement items no longer exist.");

                    foreach (var item in model.Items)
                    {
                        var ppmpItem = ppmpItems.Single(x => x.Id == item.Id);
                        var usedByOthers = ppmpItem.PPMPItemUsages.Where(x => x.PrId != requestId).Sum(x => x.Qty).GetValueOrDefault();
                        var available = ppmpItem.Qty.GetValueOrDefault() - usedByOthers;
                        if (ppmpItem.PPMP == null || ppmpItem.PPMP.DeptId != entity.DeptId ||
                            ppmpItem.PPMP.ForYear != model.ProcurementFiscalYear || item.Quantity <= 0 || item.Quantity > available)
                            throw new InvalidOperationException(item.Code + " is no longer available for the requested quantity.");
                        if (!ppmpItem.UnitCost.HasValue || String.IsNullOrWhiteSpace(item.Description))
                            throw new InvalidOperationException(item.Code + " has incomplete item details.");
                    }

                    entity.Fund = model.Fund == null ? null : model.Fund.Trim().ToUpper();
                    entity.FundSpecific = model.FundSpecific;
                    entity.FPP = model.FPP;
                    entity.Purpose = model.Purpose == null ? "" : model.Purpose.Trim();
                    entity.RequestedBy = model.RequestedBy == null ? "" : model.RequestedBy.Trim();
                    entity.RequestedDesig = model.RequestedDesig == null ? "" : model.RequestedDesig.Trim();
                    entity.Availability = model.Availability == null ? "" : model.Availability.Trim();
                    entity.AvaialbilityDesig = model.AvaialbilityDesig == null ? "" : model.AvaialbilityDesig.Trim();
                    entity.ApprovedBy = model.ApprovedBy == null ? "" : model.ApprovedBy.Trim();
                    entity.ApprovedDesig = model.ApprovedDesig == null ? "" : model.ApprovedDesig.Trim();
                    entity.SubmittedBy = user;
                    entity.SubmittedDt = date;
                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;

                    var retainedItemIds = new HashSet<Guid>();
                    foreach (var item in model.Items)
                    {
                        var ppmpItem = ppmpItems.Single(x => x.Id == item.Id);
                        var requestItem = item.RequestItemId.HasValue
                            ? entity.RequestItems.SingleOrDefault(x => x.Id == item.RequestItemId.Value)
                            : null;
                        if (item.RequestItemId.HasValue && requestItem == null)
                            throw new InvalidOperationException("A Purchase Request item changed in another session. Reload the revision.");
                        if (requestItem == null)
                        {
                            requestItem = new RequestItem { Id = Guid.NewGuid(), PrId = entity.Id, InsertedBy = user, InsertedDt = date };
                            entity.RequestItems.Add(requestItem);
                            item.RequestItemId = requestItem.Id;
                        }

                        retainedItemIds.Add(requestItem.Id);
                        requestItem.ItemNo = item.ItemNo;
                        requestItem.ItemNoIndex = Utility.GetItemNoIndex(item.ItemNo);
                        requestItem.Description = item.Description.Trim();
                        requestItem.OtherDesc = item.TechnicalSpecifications == null ? "" : item.TechnicalSpecifications.Trim();
                        requestItem.Qty = item.Quantity;
                        requestItem.Unit = ppmpItem.Unit;
                        requestItem.UnitCost = ppmpItem.UnitCost;
                        requestItem.TotalCost = item.Quantity * ppmpItem.UnitCost.GetValueOrDefault();
                        requestItem.PpmpItemId = ppmpItem.Id;
                        requestItem.PpmpCode = ppmpItem.Code;
                        requestItem.UpdatedBy = user;
                        requestItem.UpdatedDt = date;

                        var retainedSubItemIds = new HashSet<Guid>();
                        foreach (var subItem in item.SubItems ?? Enumerable.Empty<CartSubItemViewModel>())
                        {
                            if (String.IsNullOrWhiteSpace(subItem.Description) || String.IsNullOrWhiteSpace(subItem.Unit) || subItem.Quantity <= 0 || subItem.UnitCost < 0)
                                throw new InvalidOperationException("A sub-item under " + item.Code + " contains invalid values.");
                            var entitySubItem = requestItem.RequestSubItems.SingleOrDefault(x => x.Id == subItem.Id);
                            if (entitySubItem == null)
                            {
                                entitySubItem = new RequestSubItem { Id = subItem.Id == Guid.Empty ? Guid.NewGuid() : subItem.Id, RequestItemId = requestItem.Id, InsertedBy = user, InsertedDt = date };
                                requestItem.RequestSubItems.Add(entitySubItem);
                                subItem.Id = entitySubItem.Id;
                            }
                            retainedSubItemIds.Add(entitySubItem.Id);
                            entitySubItem.ItemNo = subItem.ItemNo;
                            entitySubItem.ItemNoIndex = Utility.GetItemNoIndex(subItem.ItemNo);
                            entitySubItem.Description = subItem.Description.Trim();
                            entitySubItem.Unit = subItem.Unit.Trim();
                            entitySubItem.Qty = subItem.Quantity;
                            entitySubItem.UnitCost = subItem.UnitCost;
                            entitySubItem.Total = subItem.Total;
                            entitySubItem.UpdatedBy = user;
                            entitySubItem.UpdatedDt = date;
                        }
                        _db.RequestSubItems.RemoveRange(requestItem.RequestSubItems.Where(x => !retainedSubItemIds.Contains(x.Id)).ToList());
                    }

                    var removedItems = entity.RequestItems.Where(x => !retainedItemIds.Contains(x.Id)).ToList();
                    _db.RequestSubItems.RemoveRange(removedItems.SelectMany(x => x.RequestSubItems).ToList());
                    _db.RequestItems.RemoveRange(removedItems);

                    var usages = await _db.PPMPItemUsages.Where(x => x.PrId == requestId).ToListAsync();
                    foreach (var item in model.Items)
                    {
                        var usage = usages.FirstOrDefault(x => x.PpmpItemId == item.Id);
                        if (usage == null)
                        {
                            usage = new PPMPItemUsage { Id = Guid.NewGuid(), PpmpItemId = item.Id, PrId = requestId, InsertedBy = user, InsertedDt = date };
                            _db.PPMPItemUsages.Add(usage);
                        }
                        usage.Type = String.IsNullOrWhiteSpace(entity.PrNo) ? "PR-CN" : "PR";
                        usage.Reference = entity.PrNo ?? entity.CtrlNo;
                        usage.Qty = item.Quantity;
                        usage.UpdatedBy = user;
                        usage.UpdatedDt = date;
                    }
                    _db.PPMPItemUsages.RemoveRange(usages.Where(x => !x.PpmpItemId.HasValue || !ppmpIds.Contains(x.PpmpItemId.Value)).ToList());

                    var revisionNo = await _db.DocumentStatusHistories.CountAsync(x =>
                        x.DocumentType == DocumentTypes.PurchaseRequest && x.DocumentId == requestId && x.Action.StartsWith("Resubmitted")) + 1;
                    _documentHistoryService.AddStatusHistory(DocumentTypes.PurchaseRequest, requestId,
                        entity.PrNo ?? entity.CtrlNo, PrStatuses.Revising, PrStatuses.Submitted,
                        "Resubmitted - Revision " + revisionNo,
                        "Purchase Request resubmitted after revision " + revisionNo + ".", user);

                    if (model.CartId.HasValue)
                    {
                        var cartEntity = await _db.ProcurementCarts.FirstOrDefaultAsync(c => c.Id == model.CartId.Value);
                        if (cartEntity != null)
                        {
                            cartEntity.Status = "COMPLETED";
                            cartEntity.UpdatedBy = user;
                            cartEntity.UpdatedDt = date;
                        }
                    }
                    else
                    {
                        var cartEntity = await _db.ProcurementCarts.FirstOrDefaultAsync(c => c.RequestId == requestId && c.Status == "ACTIVE");
                        if (cartEntity != null)
                        {
                            cartEntity.Status = "COMPLETED";
                            cartEntity.UpdatedBy = user;
                            cartEntity.UpdatedDt = date;
                        }
                    }
                    await _db.SaveChangesAsync();
                    transaction.Commit();
                    model.CtrlNo = entity.CtrlNo;
                    model.RevisionNo = revisionNo;
                    return model;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        });

        public ValueTask<PurchaseRequestViewModel> SaveAdminEditCheckoutAsync(PurchaseRequestViewModel model, string user, DateTime date) =>
        _prCheckoutExceptionService.TryCatch(async () =>
        {
            Guid prId = Guid.Empty;
            if (model.SourceRequestId.HasValue && model.SourceRequestId.Value != Guid.Empty)
            {
                prId = model.SourceRequestId.Value;
            }
            else if (model.RequestId.HasValue && model.RequestId.Value != Guid.Empty)
            {
                prId = model.RequestId.Value;
            }
            else if (model.Id.HasValue && model.Id.Value != Guid.Empty)
            {
                prId = model.Id.Value;
            }

            if (prId == Guid.Empty)
            {
                throw new InvalidOperationException("The Purchase Request reference is missing.");
            }

            using (var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    var entity = await _db.Requests
                        .Include(x => x.RequestItems.Select(i => i.RequestSubItems))
                        .FirstOrDefaultAsync(x => x.Id == prId);

                    if (entity == null)
                    {
                        throw new InvalidOperationException("The Purchase Request no longer exists.");
                    }

                    var latestStatus = await _db.DocumentStatusHistories
                        .Where(x => x.DocumentType == DocumentTypes.PurchaseRequest && x.DocumentId == prId)
                        .OrderByDescending(x => x.ChangedDt).ThenByDescending(x => x.Id)
                        .Select(x => x.ToStatus).FirstOrDefaultAsync();

                    bool isSubmitted = String.Equals(latestStatus, PrStatuses.Submitted, StringComparison.OrdinalIgnoreCase) ||
                                       String.Equals(latestStatus, "Submitted", StringComparison.OrdinalIgnoreCase);
                    bool isUnposted = String.Equals(latestStatus, PrStatuses.Unposted, StringComparison.OrdinalIgnoreCase) ||
                                      String.Equals(latestStatus, "Unposted", StringComparison.OrdinalIgnoreCase);

                    if (!isSubmitted && !isUnposted)
                    {
                        throw new InvalidOperationException(string.Format(
                            "Purchase Request is no longer available for administrative editing. Current status is '{0}'.",
                            latestStatus ?? "Unknown"));
                    }

                    if (!string.IsNullOrWhiteSpace(entity.PostedBy) && !isUnposted)
                    {
                        throw new InvalidOperationException("This Purchase Request is already Posted. Cannot update.");
                    }

                    if (await _requestSharedService.GetAnyOrderAsync(prId))
                    {
                        throw new InvalidOperationException("PR Number already has an associated Purchase Order. Cannot update.");
                    }

                    if (model.Items == null || !model.Items.Any())
                    {
                        throw new InvalidOperationException("The Purchase Request must contain at least one item.");
                    }

                    var existingItemMap = entity.RequestItems.ToDictionary(x => x.Id);

                    // Strictly reject any cart item that does not correspond to an existing RequestItem
                    foreach (var item in model.Items)
                    {
                        if (!item.RequestItemId.HasValue || !existingItemMap.ContainsKey(item.RequestItemId.Value))
                        {
                            throw new InvalidOperationException(string.Format(
                                "Cart item '{0}' does not correspond to an existing Purchase Request item. New procurement items cannot be added during Admin Edit.",
                                item.Description ?? item.Code));
                        }
                    }

                    var ppmpIds = entity.RequestItems
                        .Where(x => x.PpmpItemId.HasValue)
                        .Select(x => x.PpmpItemId.Value)
                        .Distinct().ToList();

                    var ppmpItems = await _db.PPMPItems
                        .Include(x => x.PPMP)
                        .Include(x => x.PPMPItemUsages)
                        .Where(x => ppmpIds.Contains(x.Id)).ToListAsync();

                    var validUnitCodes = new HashSet<string>(
                        await _db.Codextns
                            .Where(c => c.CodeMast.Code == "UNIT")
                            .Select(c => c.Code.Trim())
                            .ToListAsync(),
                        StringComparer.OrdinalIgnoreCase);

                    // Reconcile and validate each item
                    foreach (var item in model.Items)
                    {
                        var requestItem = existingItemMap[item.RequestItemId.Value];
                        var origQty = requestItem.Qty.GetValueOrDefault();
                        var newQty = item.Quantity;

                        if (newQty <= 0)
                        {
                            throw new InvalidOperationException((item.Code ?? requestItem.PpmpCode ?? "Item") + " quantity must be greater than zero.");
                        }

                        if (string.IsNullOrWhiteSpace(item.Description))
                        {
                            throw new InvalidOperationException((item.Code ?? requestItem.PpmpCode ?? "Item") + " requires a description.");
                        }

                        if (string.IsNullOrWhiteSpace(item.Unit))
                        {
                            throw new InvalidOperationException((item.Code ?? requestItem.PpmpCode ?? "Item") + " requires a unit.");
                        }

                        if (!validUnitCodes.Contains(item.Unit.Trim()))
                        {
                            throw new InvalidOperationException(string.Format("Unit '{0}' for item '{1}' is not recognized.", item.Unit, item.Code ?? requestItem.PpmpCode));
                        }

                        if (!item.UnitCost.HasValue || item.UnitCost.Value <= 0)
                        {
                            throw new InvalidOperationException((item.Code ?? requestItem.PpmpCode ?? "Item") + " must have a unit cost greater than zero.");
                        }

                        if (requestItem.PpmpItemId.HasValue && requestItem.PpmpItemId.Value != item.Id)
                        {
                            throw new InvalidOperationException(string.Format("PPMP Item identity for item '{0}' has been modified.", item.Code ?? requestItem.PpmpCode));
                        }

                        if (requestItem.PpmpItemId.HasValue)
                        {
                            var ppmpItem = ppmpItems.FirstOrDefault(x => x.Id == requestItem.PpmpItemId.Value);
                            if (ppmpItem != null)
                            {
                                var usedByOthers = ppmpItem.PPMPItemUsages
                                    .Where(x => x.PrId != prId)
                                    .Sum(x => x.Qty).GetValueOrDefault();
                                var availableForPr = ppmpItem.Qty.GetValueOrDefault() - usedByOthers;
                                if (newQty > availableForPr)
                                {
                                    throw new InvalidOperationException(string.Format(
                                        "Insufficient PPMP allocation for item '{0}'. Requested: {1}, Available balance: {2}.",
                                        item.Code ?? ppmpItem.Code, newQty, (availableForPr - origQty)));
                                }
                            }
                        }
                    }

                    // Permitted header corrections
                    entity.Purpose = model.Purpose == null ? "" : model.Purpose.Trim();
                    entity.FPP = model.FPP;
                    entity.Fund = model.Fund == null ? null : model.Fund.Trim().ToUpper();
                    entity.FundSpecific = model.FundSpecific;
                    entity.RequestedBy = model.RequestedBy == null ? "" : model.RequestedBy.Trim();
                    entity.RequestedDesig = model.RequestedDesig == null ? "" : model.RequestedDesig.Trim();
                    entity.Availability = model.Availability == null ? "" : model.Availability.Trim();
                    entity.AvaialbilityDesig = model.AvaialbilityDesig == null ? "" : model.AvaialbilityDesig.Trim();
                    entity.ApprovedBy = model.ApprovedBy == null ? "" : model.ApprovedBy.Trim();
                    entity.ApprovedDesig = model.ApprovedDesig == null ? "" : model.ApprovedDesig.Trim();
                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;

                    var retainedItemIds = new HashSet<Guid>();
                    foreach (var item in model.Items)
                    {
                        var requestItem = existingItemMap[item.RequestItemId.Value];
                        retainedItemIds.Add(requestItem.Id);

                        requestItem.ItemNo = item.ItemNo;
                        requestItem.ItemNoIndex = Utility.GetItemNoIndex(item.ItemNo);
                        requestItem.Description = item.Description.Trim();
                        requestItem.OtherDesc = item.TechnicalSpecifications == null ? "" : item.TechnicalSpecifications.Trim();
                        requestItem.Qty = item.Quantity;
                        if (!string.IsNullOrWhiteSpace(item.Unit))
                        {
                            requestItem.Unit = item.Unit.Trim();
                        }
                        if (item.UnitCost.HasValue)
                        {
                            requestItem.UnitCost = item.UnitCost.Value;
                        }
                        requestItem.TotalCost = item.Quantity * requestItem.UnitCost.GetValueOrDefault();
                        requestItem.UpdatedBy = user;
                        requestItem.UpdatedDt = date;

                        var retainedSubItemIds = new HashSet<Guid>();
                        foreach (var subItem in item.SubItems ?? Enumerable.Empty<CartSubItemViewModel>())
                        {
                            if (string.IsNullOrWhiteSpace(subItem.Description) ||
                                string.IsNullOrWhiteSpace(subItem.Unit) ||
                                subItem.Quantity <= 0 ||
                                subItem.UnitCost < 0)
                            {
                                throw new InvalidOperationException("A sub-item under " + item.Code + " contains invalid values.");
                            }

                            var entitySubItem = subItem.Id != Guid.Empty
                                ? requestItem.RequestSubItems.FirstOrDefault(x => x.Id == subItem.Id)
                                : null;

                            if (entitySubItem == null)
                            {
                                entitySubItem = new RequestSubItem
                                {
                                    Id = subItem.Id == Guid.Empty ? Guid.NewGuid() : subItem.Id,
                                    RequestItemId = requestItem.Id,
                                    InsertedBy = user,
                                    InsertedDt = date
                                };
                                requestItem.RequestSubItems.Add(entitySubItem);
                                subItem.Id = entitySubItem.Id;
                            }

                            retainedSubItemIds.Add(entitySubItem.Id);
                            entitySubItem.ItemNo = subItem.ItemNo;
                            entitySubItem.ItemNoIndex = Utility.GetItemNoIndex(subItem.ItemNo);
                            entitySubItem.Description = subItem.Description.Trim();
                            entitySubItem.Unit = subItem.Unit.Trim();
                            entitySubItem.Qty = subItem.Quantity;
                            entitySubItem.UnitCost = subItem.UnitCost;
                            entitySubItem.Total = subItem.Quantity * subItem.UnitCost;
                            entitySubItem.UpdatedBy = user;
                            entitySubItem.UpdatedDt = date;
                        }

                        var removedSubItems = requestItem.RequestSubItems
                            .Where(x => !retainedSubItemIds.Contains(x.Id)).ToList();
                        _db.RequestSubItems.RemoveRange(removedSubItems);
                    }

                    // Handle removed RequestItems
                    var removedItems = entity.RequestItems.Where(x => !retainedItemIds.Contains(x.Id)).ToList();
                    _db.RequestSubItems.RemoveRange(removedItems.SelectMany(x => x.RequestSubItems).ToList());
                    _db.RequestItems.RemoveRange(removedItems);

                    // Reconcile PPMPItemUsages (Delta adjustment)
                    var usages = await _db.PPMPItemUsages.Where(x => x.PrId == prId).ToListAsync();
                    foreach (var item in model.Items)
                    {
                        var requestItem = existingItemMap[item.RequestItemId.Value];
                        if (!requestItem.PpmpItemId.HasValue) continue;

                        var usage = usages.FirstOrDefault(x => x.PpmpItemId == requestItem.PpmpItemId);
                        if (usage == null)
                        {
                            usage = new PPMPItemUsage
                            {
                                Id = Guid.NewGuid(),
                                PpmpItemId = requestItem.PpmpItemId,
                                PrId = prId,
                                InsertedBy = user,
                                InsertedDt = date
                            };
                            _db.PPMPItemUsages.Add(usage);
                        }
                        usage.Type = string.IsNullOrWhiteSpace(entity.PrNo) ? "PR-CN" : "PR";
                        usage.Reference = entity.PrNo ?? entity.CtrlNo;
                        usage.Qty = item.Quantity;
                        usage.UpdatedBy = user;
                        usage.UpdatedDt = date;
                    }

                    var retainedPpmpIds = model.Items
                        .Select(x => existingItemMap[x.RequestItemId.Value].PpmpItemId)
                        .Where(x => x.HasValue)
                        .Select(x => x.Value)
                        .Distinct().ToList();
                    var removedUsages = usages
                        .Where(x => !x.PpmpItemId.HasValue || !retainedPpmpIds.Contains(x.PpmpItemId.Value))
                        .ToList();
                    _db.PPMPItemUsages.RemoveRange(removedUsages);

                    var form = System.Web.HttpContext.Current != null ? System.Web.HttpContext.Current.Request.Form : null;
                    var postAfterAdminEdit = form != null && (string.Equals(form["PostAfterAdminEdit"], "true", StringComparison.OrdinalIgnoreCase) || string.Equals(form["PostAfterAdminEdit"], "on", StringComparison.OrdinalIgnoreCase));
                    var prNoInput = form != null ? form["AdminEditPrNoInput"] : null;
                    var prDateInputStr = form != null ? form["AdminEditPrDateInput"] : null;
                    DateTime? postDateInput = null;
                    if (!string.IsNullOrWhiteSpace(prDateInputStr))
                    {
                        DateTime parsedDate;
                        if (DateTime.TryParse(prDateInputStr, out parsedDate))
                        {
                            postDateInput = parsedDate;
                        }
                    }

                    if (postAfterAdminEdit)
                    {
                        var postDate = postDateInput.HasValue ? postDateInput.Value : date;
                        if (postDate == default(DateTime))
                        {
                            throw new InvalidOperationException("PR Date is required for posting.");
                        }
                        if (postDate.Date > DateTime.Today)
                        {
                            throw new InvalidOperationException("Future PR Date is not allowed.");
                        }

                        var isManualPrNumber = !string.IsNullOrWhiteSpace(prNoInput);
                        var manualPrNumber = isManualPrNumber ? prNoInput.Trim() : null;

                        if (isManualPrNumber)
                        {
                            var validation = ValidatePrNoAndDate(manualPrNumber, postDate);
                            if (validation != null && validation.Count > 0)
                            {
                                var errorList = new List<string>();
                                foreach (DictionaryEntry entry in validation)
                                {
                                    var msgs = entry.Value as IEnumerable<string>;
                                    if (msgs != null) errorList.AddRange(msgs);
                                    else if (entry.Value != null) errorList.Add(entry.Value.ToString());
                                }
                                if (errorList.Any())
                                {
                                    throw new InvalidOperationException(string.Join(" ", errorList));
                                }
                            }

                            if (await _db.Requests.AnyAsync(x => x.Id != prId && x.PrNo == manualPrNumber))
                            {
                                throw new InvalidOperationException("PR Number already exists.");
                            }
                            entity.PrNo = manualPrNumber;
                            entity.PrDate = postDate.Date;
                        }
                        else if (!string.IsNullOrWhiteSpace(entity.PrNo))
                        {
                            var existingPrNo = entity.PrNo.Trim();
                            if (await _db.Requests.AnyAsync(x => x.Id != prId && x.PrNo == existingPrNo))
                            {
                                throw new InvalidOperationException("The existing PR Number is already used by another record.");
                            }
                            entity.PrDate = postDate.Date;
                        }
                        else
                        {
                            var generatedPrNo = NextPrNo(postDate);
                            if (string.IsNullOrWhiteSpace(generatedPrNo))
                            {
                                throw new InvalidOperationException("Failed to generate a PR Number. Please try again.");
                            }
                            if (await _db.Requests.AnyAsync(x => x.Id != prId && x.PrNo == generatedPrNo))
                            {
                                throw new InvalidOperationException("PR Number already exists. Please try posting again.");
                            }
                            entity.PrNo = generatedPrNo;
                            entity.PrDate = postDate.Date;
                        }

                        entity.PostedBy = user;
                        entity.PostedDt = date;

                        foreach (var usage in usages)
                        {
                            usage.Type = "PR";
                            usage.Reference = entity.PrNo;
                        }

                        _documentHistoryService.AddStatusHistory(
                            DocumentTypes.PurchaseRequest,
                            prId,
                            entity.PrNo,
                            latestStatus,
                            PrStatuses.Posted,
                            "Post",
                            "Purchase Request updated via Admin Edit and posted successfully.",
                            user);
                    }
                    else
                    {
                        // Save only: preserve current workflow status (Submitted remains Submitted, Unposted remains Unposted)
                        _documentHistoryService.AddStatusHistory(
                            DocumentTypes.PurchaseRequest,
                            prId,
                            entity.PrNo ?? entity.CtrlNo,
                            latestStatus,
                            latestStatus,
                            "Admin Edit",
                            "Purchase Request updated via Admin Edit Checkout.",
                            user);
                    }

                    // Complete the Admin Edit cart
                    if (model.CartId.HasValue)
                    {
                        var cartEntity = await _db.ProcurementCarts.FirstOrDefaultAsync(c => c.Id == model.CartId.Value);
                        if (cartEntity != null)
                        {
                            cartEntity.Status = "COMPLETED";
                            cartEntity.UpdatedBy = user;
                            cartEntity.UpdatedDt = date;
                        }
                    }
                    else
                    {
                        var cartEntity = await _db.ProcurementCarts.FirstOrDefaultAsync(c => c.RequestId == prId && c.Status == "ACTIVE" && ((c.RevisionUser != null && c.RevisionUser.StartsWith("ADMIN_EDIT:")) || (c.ReviewComment != null && c.ReviewComment.StartsWith("[CART_MODE:ADMIN_EDIT]"))));
                        if (cartEntity != null)
                        {
                            cartEntity.Status = "COMPLETED";
                            cartEntity.UpdatedBy = user;
                            cartEntity.UpdatedDt = date;
                        }
                    }

                    await _db.SaveChangesAsync();
                    transaction.Commit();

                    model.CtrlNo = entity.CtrlNo;
                    model.AdminEditPrNo = entity.PrNo;
                    model.Id = entity.Id;
                    model.RequestId = entity.Id;
                    return model;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        });

        public ValueTask<RequestVM> CreateAsync(RequestVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            model.Id = Guid.NewGuid();

            //if (string.IsNullOrWhiteSpace(model.PrNo))
            //{
            //    model.PrNo = NextPrNo((DateTime)model.PrDate);
            //}

            model.CtrlNo = NextCtrlNo(date);
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            await ValidateOnCreateUpdateAsync(model, Mode.ADD);

            var entity = new Request();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.Requests.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RequestVM> UpdateAsync(RequestVM model, string user, DateTime date) =>
            UpdateAsync(model, user, date, false);

        public ValueTask<RequestVM> UpdateAsync(RequestVM model, string user, DateTime date, bool allowAdminEdit) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.Requests.Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            ValidateRecord(entity, model.Id);
            await ValidateStatusAsync(model.Id, allowAdminEdit);

            if (allowAdminEdit)
            {
                model.PrNo = entity.PrNo;
                model.PrDate = entity.PrDate;
            }

            await ValidateOnCreateUpdateAsync(model, Mode.EDIT);

            MapModelToEntityFields(entity, model, Mode.EDIT);

            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RequestVM> DeleteAsync(RequestVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.Requests.FindAsync(model.Id);
            ValidateRecord(entity, model.Id);
            await ValidateStatusAsync(model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            await _db.SaveChangesAsync();

            _db.Requests.Remove(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        private void MapModelToEntityFields(Request entity, RequestVM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.CtrlNo = model.CtrlNo;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;               
            }

            entity.Fund = model.Fund?.Trim().ToUpper();
            entity.FundSpecific = model.FundSpecific;
            entity.DeptId = model.DeptId;
            entity.Department = model.Department?.Trim();
            entity.Section = model.Section?.Trim() ?? "";
            entity.PrNo = model.PrNo;
            entity.PrDate = model.PrDate;
            entity.FPP = model.FPP;
            entity.Purpose = model.Purpose?.Trim() ?? "";
            entity.RequestedBy = model.RequestedBy?.Trim() ?? "";
            entity.RequestedDesig = model.RequestedDesig?.Trim() ?? "";
            entity.Availability = model.Availability?.Trim() ?? "";
            entity.AvaialbilityDesig = model.AvaialbilityDesig?.Trim() ?? "";
            entity.ApprovedBy = model.ApprovedBy?.Trim() ?? "";
            entity.ApprovedDesig = model.ApprovedDesig?.Trim() ?? "";
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            if (!string.IsNullOrWhiteSpace(model.SubmittedBy))
            {
                entity.SubmittedBy = model.SubmittedBy;
                entity.SubmittedDt = model.SubmittedDt;
            }
        }

        public async ValueTask PostAsync(Guid requestId, string user, DateTime date)
        {
            var entity = await _db.Requests.FindAsync(requestId);
            if (entity != null)
            {
                if (!entity.PrDate.HasValue)
                {
                    entity.PrDate = date.Date;
                }

                if (string.IsNullOrEmpty(entity.PrNo))
                {
                    entity.PrNo = NextPrNo(entity.PrDate.Value);
                }

                var validation = ValidateOnPost(entity.PrNo, entity.PrDate);
                if (validation != null && validation.Count > 0)
                {
                    var imex = new InvalidModelException();
                    imex.AddData(validation);
                    imex.ThrowIfContainsErrors();
                }

                if (string.IsNullOrEmpty(entity.Availability))
                {
                    throw new InvalidValueException("Cash availability is Required.");
                }

                if (string.IsNullOrEmpty(entity.ApprovedBy))
                {
                    throw new InvalidValueException("Approved by is Required.");
                }

                var ppmpItemUsages = await _db.PPMPItemUsages.Where(w => w.PrId == requestId && w.Type != "PR").ToListAsync();
                foreach (var ppmpItemUsage in ppmpItemUsages)
                {
                    ppmpItemUsage.Type = "PR";
                    ppmpItemUsage.Reference = entity.PrNo;
                }

                //var unitGroupItems = _db.RequestItemUnitGroupDescriptionItems
                //    .AsNoTracking()
                //    .Include(i => i.RequestItem)
                //    .Include(i => i.RequestItemUnitGroupDescription.RequestItemUnitGroup)
                //    .Where(w => w.RequestItemUnitGroupDescription.RequestItemUnitGroup.PrId == entity.Id).ToList();
                //if (unitGroupItems.Any())
                //{
                //    var rate = unitGroupItems.Sum(s => s.RequestItem.PriceRate) ?? 0;
                //    if (rate != 100)
                //    {
                //        throw new InvalidValueException("Price rate must be 100%");
                //    }
                //}

                //if (_db.RequestItems.Any(a => a.PrId == requestId && (!a.UnitCost.HasValue || a.UnitCost == 0)))
                //{
                //    throw new InvalidValueException("All items must have a unit cost.");
                //}

                //// validate item price
                //var priceCap = GetPriceCap();
                //var requestItems = _db.RequestItems.Include(i => i.RisItem.ItemCode.ItemType).AsNoTracking().Where(w => w.PrId == requestId).ToList();
                //foreach(var requestItem in requestItems)
                //{                    
                //    var category = requestItem.RisItem.ItemCode.ItemType.Category;
                //    decimal? unitCost = 0;
                //    var unitGroup = _db.RequestItemUnitGroups.Where(w => w.RequestItemUnitGroupDescriptions.Any(a => a.RequestItemUnitGroupDescriptionItems.Any(b => b.RequestItemId == requestItem.Id))).FirstOrDefault();
                //    if (unitGroup != null)
                //    {
                //        unitCost = unitGroup.UnitCost;
                //        if (category != "S")
                //        {
                //            if (unitCost < priceCap)
                //            {
                //                throw new InvalidValueException($"Please use supplies code for items with a group unit cost below {priceCap:n0}.");
                //            }
                //        }
                //        else
                //        {
                //            if (unitCost >= priceCap)
                //            {
                //                throw new InvalidValueException($"Please use property code for items with a group unit cost of {priceCap:n0} and above.");
                //            }
                //        }
                //    }
                //    else
                //    {
                //        unitCost = requestItem.UnitCost;
                //        if (category != "S")
                //        {
                //            if (unitCost < priceCap)
                //            {
                //                throw new InvalidValueException($"Please use supplies code for items with a unit cost below {priceCap:n0}.");
                //            }
                //        }
                //        else
                //        {
                //            if (unitCost >= priceCap)
                //            {
                //                throw new InvalidValueException($"Please use property code for items with a unit cost of {priceCap:n0} and above.");
                //            }
                //        }
                //    }
                //}

                entity.PostedBy = user;
                entity.PostedDt = date;
                //entity.UpdatedBy = user;
                //entity.UpdatedDt = date;

                await _db.SaveChangesAsync();
            }
        }

        public async ValueTask UnpostAsync(Guid requestId, string user, DateTime date)
        {
            await new PurchaseRequestLifecycleService(_db).UnpostAsync(requestId, user, date);
        }

        public async ValueTask SubmitAsync(Guid requestId, string user, DateTime date)
        {
            var entity = await _db.Requests.Include(i => i.RequestItems).FirstOrDefaultAsync(f => f.Id == requestId);
            if (entity != null)
            {
                if (string.IsNullOrEmpty(entity.Availability))
                {
                    throw new InvalidValueException("Cash availability is Required.");
                }

                if (string.IsNullOrEmpty(entity.ApprovedBy))
                {
                    throw new InvalidValueException("Approved by is Required.");
                }

                if (!entity.RequestItems.Any())
                {
                    throw new InvalidValueException("No request items were found.");
                }

                entity.SubmittedBy = user;
                entity.SubmittedDt = date;
                //entity.UpdatedBy = user;
                //entity.UpdatedDt = date;

                await _db.SaveChangesAsync();
            }
        }

        public async ValueTask UnsubmitAsync(Guid requestId, string user, DateTime date)
        {
            var entity = await _db.Requests.FindAsync(requestId);
            if (entity != null)
            {
                if (string.IsNullOrEmpty(entity.SubmittedBy))
                {
                    throw new InvalidValueException("This record is not yet posted, please verify.");
                }

                entity.SubmittedBy = null;
                entity.SubmittedDt = null;
                //entity.UpdatedBy = user;
                //entity.UpdatedDt = date;

                await _db.SaveChangesAsync();
            }
        }

        public string NextPrNo(DateTime prDate)
        {
            string yyyy = prDate.Year.ToString().Trim();
            string mm = prDate.Month.ToString().PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = _db.Requests
                .Where(w => w.PrNo != null && w.PrNo.StartsWith(yyyy + "-") && w.PrNo.Length == 12)
                .OrderByDescending(o => o.PrNo)
                .FirstOrDefault();

            int nextSeq = 1;
            if (data != null && !string.IsNullOrWhiteSpace(data.PrNo))
            {
                var parts = data.PrNo.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastSeq))
                {
                    nextSeq = lastSeq + 1;
                }
            }

            string candidate = keyName + "-" + nextSeq.ToString().PadLeft(4, '0');
            while (_db.Requests.Any(a => a.PrNo == candidate))
            {
                nextSeq++;
                candidate = keyName + "-" + nextSeq.ToString().PadLeft(4, '0');
            }

            return candidate;
        }

        private string NextCtrlNo(DateTime date)
        {
            string yyyy = date.Year.ToString().Trim();
            string mm = date.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + mm;
            // yyyy-mm-9999
            // 123456789012

            var order = _db.Requests.Where(w => w.CtrlNo.Substring(0, 4) == yyyy).OrderByDescending(o => o.CtrlNo).FirstOrDefault();
            if (order == null)
            {
                return keyName + "0001";
            }
            else
            {                
                var sequence = (int.Parse(order.CtrlNo.Substring(order.CtrlNo.Length -4)) + 1).ToString();
                return keyName + sequence.PadLeft(4, '0');
            }
        }

        public IDictionary ValidateOnPost(string prNo, DateTime? prDate)
        {
            return ValidatePrNoAndDate(prNo, prDate);
        }

        public IDictionary ValidatePrNoAndDate(string prNo, DateTime? prDate)
        {
            InvalidModelException _imex = new InvalidModelException();
            if (!string.IsNullOrWhiteSpace(prNo))
            {
                prNo = prNo.Trim();
                if (prNo.Length != 12)
                {
                    _imex.UpsertDataList("PR Number", "Invalid value.");
                }
                else
                {
                    if (!prDate.HasValue)
                    {
                        _imex.UpsertDataList("PR Date", "Field is required.");
                    }
                    else
                    {
                        var refNoParts = prNo.Split('-');
                        if (refNoParts.Length != 3 ||
                            !int.TryParse(refNoParts[0], out int refNoYear) ||
                            !int.TryParse(refNoParts[1], out int refNoMonth) ||
                            !int.TryParse(refNoParts[2], out int refNoSeq))
                        {
                            _imex.UpsertDataList("PR Number", "Invalid format. Expected format: YYYY-MM-####.");
                        }
                        else if (refNoSeq == 0)
                        {
                            _imex.UpsertDataList("PR Number", "Invalid sequence number.");
                        }
                        else
                        {
                            if (refNoYear != prDate.Value.Year || refNoMonth != prDate.Value.Month)
                            {
                                _imex.UpsertDataList("PR Number", "Series year and month must match the PR date's year and month.");
                            }
                        }
                    }
                }
            }

            if (prDate.HasValue)
            {
                if (prDate.Value.Date > DateTime.Today)
                {
                    _imex.UpsertDataList("PR Date", "Future date is not allowed.");
                }
            }
            return _imex.Data;
        }

        private async Task ValidateOnCreateUpdateAsync(RequestVM model, Mode mode)
        {
            _imex = new InvalidModelException();
            var validationResult = ValidatePrNoAndDate(model.PrNo, model.PrDate);
            if (validationResult.Count == 0 && !string.IsNullOrEmpty(model.PrNo))
            {
                if (mode == Mode.ADD)
                {
                    if (await _db.Requests.AnyAsync(a => a.PrNo == model.PrNo))
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.PrNo)), $"Already exists.");
                    }
                }
                else
                {
                    if (await _db.Requests.AnyAsync(a => a.PrNo == model.PrNo && a.Id != model.Id))
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.PrNo)), $"Already exists.");
                    }
                }
            }                        
            else
            {
                _imex.AddData(validationResult);
            }

            _imex.ThrowIfContainsErrors();
        }

        //private async ValueTask ValidateOnDestroy(RequestVM model)
        //{
        //    var order = await _db.Orders.FindAsync(model.Id);
        //    if (order == null)
        //    {
        //        throw new RecordNotFoundException(model.Id);
        //    }

        //    if (await IsPostedAsync(model.Id))
        //    {
        //        throw new RecordAlreadyPostedException(string.Format("PO Number {0} already Posted, cannot delete!", model.PoNo));
        //    }

        //    if (await GetAnyAirsAsync(model.Id))
        //    {
        //        throw new RecordRelationshipException("PO Number already with AIR, cannot delete!");
        //    }

        //    if (await GetAnyParsAsync(model.Id))
        //    {
        //        throw new RecordRelationshipException("PO Number already with PAR, cannot delete!");
        //    }
        //}

        private async Task ValidateStatusAsync(Guid prId, bool allowAdminEdit = false)
        {
            await _requestSharedService.ValidateStatusAsync(prId, allowAdminEdit);
        }

        private void ValidateRecord(Request entity, Guid id)
        {
            if (entity is null)
            {
                throw new NotFoundException(id);
            }
        }

        private void ValidateIfNull(RequestVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }
    }
}
