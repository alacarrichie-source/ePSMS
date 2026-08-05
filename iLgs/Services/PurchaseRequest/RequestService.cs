using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
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

        ValueTask<RequestVM> CreateAsync(RequestVM model, string user, DateTime date);
        ValueTask<RequestVM> UpdateAsync(RequestVM model, string user, DateTime date);
        ValueTask<RequestVM> DeleteAsync(RequestVM model, string user, DateTime date);
        ValueTask PostAsync(Guid requestId, string user, DateTime date);
        ValueTask UnpostAsync(Guid requestId, string user, DateTime date);
        ValueTask SubmitAsync(Guid requestId, string user, DateTime date);
        ValueTask UnsubmitAsync(Guid requestId, string user, DateTime date);

        IRequestItemService RequestItem { get; }
    }

    public class RequestService : BaseValidator, IRequestService
    {
        private decimal? _priceCap;
        private readonly AppManEntities _db;
        private readonly IRequestSharedService _requestSharedService;
        private readonly IUserService _userService;
        private readonly IExceptionService<RequestVM> _vmExceptionService;
        private readonly IPriceCapService _priceCapService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        private IRequestItemService _requestItemService;

        public RequestService(AppManEntities db)
        {
            _db = db;
            _requestSharedService = new RequestSharedService(_db);
            _userService = new UserService(_db);
            _vmExceptionService = new ExceptionService<RequestVM>();
            _getDisplayName = propertyName => Utility.GetDisplayName<RequestVM>(propertyName);
            _priceCapService = new PriceCapService(_db);

            _requestItemService = new RequestItemService(_db);
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

        private static Expression<Func<Request, RequestVM>> Projection
        = s => new RequestVM
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
            //IsWithPO = s.Orders.Any(),
            IsWithPO = s.RequestItems.Any(a => a.OrderItemRequests.Any()),
            InsertedBy = s.InsertedBy,
            InsertedDt = s.InsertedDt
        };

        public IQueryable<RequestVM> GetAll()
        {
            return _db.Requests.AsNoTracking().Select(Projection).AsQueryable();
        }

        public async ValueTask<IQueryable<RequestVM>> GetAllAsync(string userId)
        {
            var data = _db.Requests.AsQueryable();
            if (!(await _userService.IsAdminAsync(userId)))
            {
                data = data.Where(w => w.Codextn.DepartmentUsers.Any(a => a.UserId == userId));
            }

            return data.Select(Projection).AsNoTracking();
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

            return data.Select(Projection).AsNoTracking();
        }

        public Task<RequestVM> GetByIdAsync(Guid? prId)
        {
            return _db.Requests.Where(w => w.Id == prId).Select(Projection).FirstOrDefaultAsync();
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
            return _db.Requests.Where(w => w.PrNo == prNo).Select(Projection).FirstOrDefaultAsync();
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
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.Requests.Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            ValidateRecord(entity, model.Id);
            await ValidateStatusAsync(model.Id);
            await ValidateOnCreateUpdateAsync(model, Mode.EDIT);

            MapModelToEntityFields(entity, model, Mode.ADD);

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
        }

        public async ValueTask PostAsync(Guid requestId, string user, DateTime date)
        {
            var entity = await _db.Requests.FindAsync(requestId);
            if (entity != null)
            {
                if (string.IsNullOrEmpty(entity.PrNo))
                {
                    throw new InvalidValueException("PR Number is Required.");
                }

                if (!entity.PrDate.HasValue)
                {
                    throw new InvalidValueException("PR Date is Required.");
                }

                if (string.IsNullOrEmpty(entity.Availability))
                {
                    throw new InvalidValueException("Cash availability is Required.");
                }

                if (string.IsNullOrEmpty(entity.ApprovedBy))
                {
                    throw new InvalidValueException("Approved by is Required.");
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
                entity.UpdatedBy = user;
                entity.UpdatedDt = date;

                await _db.SaveChangesAsync();
            }
        }

        public async ValueTask UnpostAsync(Guid requestId, string user, DateTime date)
        {
            var entity = await _db.Requests.FindAsync(requestId);
            if (entity != null)
            {
                if (string.IsNullOrEmpty(entity.PostedBy))
                {
                    throw new InvalidValueException("This record is not yet posted, please verify.");
                }

                entity.PostedBy = null;
                entity.PostedDt = null;
                entity.UpdatedBy = user;
                entity.UpdatedDt = date;

                await _db.SaveChangesAsync();
            }
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
                entity.UpdatedBy = user;
                entity.UpdatedDt = date;

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
                entity.UpdatedBy = user;
                entity.UpdatedDt = date;

                await _db.SaveChangesAsync();
            }
        }

        private string NextPrNo(DateTime prDate)
        {
            string yyyy = prDate.Year.ToString().Trim();
            string mm = prDate.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = _db.Requests.Where(w => w.PrDate.Value.Year == prDate.Year).OrderByDescending(o => o.PrNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(data.PrNo.Split('-')[2]) + 1).ToString();
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

            var order = _db.Requests.Where(w => w.CtrlNo.Substring(0, 4) == yyyy).OrderByDescending(o => o.CtrlNo).FirstOrDefault();
            if (order == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(order.CtrlNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }

        private async Task ValidateOnCreateUpdateAsync(RequestVM model, Mode mode)
        {
            _imex = new InvalidModelException();
            if (!string.IsNullOrWhiteSpace(model.PrNo))
            {
                if (model.PrNo.Trim().Length != 12)
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.PrNo)), "Invalid value.");
                }
                else
                {
                    if (!model.PrDate.HasValue)
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.PrDate)), $"Field is required.");
                    }
                    else
                    {
                        var refNoParts = model.PrNo.Split('-');
                        var refNoYear = int.Parse(refNoParts[0]);
                        var refNoMonth = int.Parse(refNoParts[1]);
                        var refNoSeq = int.Parse(refNoParts[2]);
                        if (refNoSeq == 0)
                        {
                            _imex.UpsertDataList(_getDisplayName(nameof(model.PrNo)), "Invalid sequence number.");
                        }
                        else
                        {
                            if (refNoYear != model.PrDate.Value.Year || refNoMonth != model.PrDate.Value.Month)
                            {
                                _imex.UpsertDataList(_getDisplayName(nameof(model.PrNo)), "Series year and month must match the PR date’s year and month.");
                            }
                            //else
                            //{
                            //    //var maxNo = _db.Requests.Where(w => DbFunctions.TruncateTime(w.PrDate) < DbFunctions.TruncateTime(model.PrDate)).Max(m => m.PrNo);
                            //    var maxNo = _db.Requests.Where(w => w.PrDate.Value.Year == model.PrDate.Value.Year).Max(m => m.PrNo);
                            //    if (!string.IsNullOrWhiteSpace(maxNo))
                            //    {
                            //        var maxSeq = int.Parse(maxNo.Split('-')[2]);
                            //        if (refNoSeq <= maxSeq)
                            //        {
                            //            _imex.UpsertDataList(_getDisplayName(nameof(model.PrNo)), $"Sequnce No. must be greater than {maxSeq}");
                            //        }
                            //    }
                            //}
                        }
                    }

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
            }

            if (model.PrDate.HasValue)
            {
                if (model.PrDate > model.UpdatedDt)
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.PrDate)), $"Future date is not allowed.");
                }
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

        private async Task ValidateStatusAsync(Guid prId)
        {
            await _requestSharedService.ValidateStatusAsync(prId);
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