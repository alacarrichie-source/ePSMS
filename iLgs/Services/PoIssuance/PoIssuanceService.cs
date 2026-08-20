using Dapper;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.PropertyCard;
using iLgs.Utilities;
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
        Task<IList<PsCardItemVM>> GetAllListAsync(string userId, bool? isViewOnly);
        IQueryable<PsCardItemVM> GetById(Guid? id);
        IQueryable<PsCardItemVM> GetSummary();
        IQueryable<PsCardItemVM> GetSummary(int? forYear);
        IList<PoIssuancePoSumVM> GetSummaryByPo(int? forYear, Guid? deptId);
        IQueryable<PsCardItemExtnTransitVM> GetCardItemExtnForTransit(Guid? psCardItemId, Guid? transferId);
        //ValueTask PostAsync(Guid psCardItemIssuanceId, string user, DateTime date);
        //ValueTask UnpostAsync(Guid psCardItemIssuanceId, string user, DateTime date);
        ValueTask<PsCardItemTransferVM> TransferAsync(PsCardItemTransferVM model, string user, DateTime date);
    }

    public class PoIssuanceService : IPoIssuanceService
    {
        private decimal? _priceCap;
        private readonly AppManEntities _db;
        private readonly IUserService _userService;
        //private readonly IExceptionService<RisIssuedVM> _vmExceptionService;
        private readonly IExceptionService<PsCardItemVM> _psCardItemVMExceptionService;
        private readonly IExceptionService<PsCardItemTransferVM> _psCardItemTransferVMExceptionService;
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;
        private readonly IPsCardService _psCardService;
        private readonly IPriceCapService _priceCapService;
        private readonly ICodextnService _codextnService;
        private int _issuanceYear;

        public PoIssuanceService(AppManEntities db)
        {
            _db = db;
            _userService = new UserService(_db);
            //_vmExceptionService = new ExceptionService<RisIssuedVM>();
            _psCardItemVMExceptionService = new ExceptionService<PsCardItemVM>();
            _psCardItemTransferVMExceptionService = new ExceptionService<PsCardItemTransferVM>();
            _psCardItemTransactionService = new PsCardItemTransactionService(_db);
            _psCardService = new PsCardService(_db);
            _priceCapService = new PriceCapService(_db);
            _codextnService = new CodextnService(_db);
            _issuanceYear = int.Parse(_codextnService.GetIssuanceYears().OrderByDescending(o => o.Description).FirstOrDefault().Description);
        }

        //public PoIssuanceService(AppManEntities db,
        //    IAppManEntitiesFactory appManEntitiesFactory,
        //    IUserService userService,
        //    IExceptionService<RisIssuedVM> vmExceptionService,
        //    IExceptionService<PsCardItemVM> psCardItemVMExceptionService,
        //    IExceptionService<PsCardItemTransferVM> psCardItemTransferVMExceptionService,
        //    IPsCardItemTransactionService psCardItemTransactionService,
        //    IPsCardService psCardService,
        //    IPriceCapService priceCapService)
        //{
        //    _db = db;
        //    _contextFactory = appManEntitiesFactory;
        //    _userService = userService;
        //    _vmExceptionService = vmExceptionService;
        //    _psCardItemVMExceptionService = psCardItemVMExceptionService;
        //    _psCardItemTransferVMExceptionService = psCardItemTransferVMExceptionService;
        //    _psCardItemTransactionService = psCardItemTransactionService;
        //    _psCardService = psCardService;
        //    _priceCapService = priceCapService;            
        //}

        private decimal GetPriceCap()
        {
            return _priceCap ?? (_priceCap = _priceCapService.GetPriceCap()).Value;
        }

        //private int? GetIssuanceYear()
        //{
        //    return _issuanceYear ?? (_issuanceYear = int.Parse(_codextnService.GetIssuanceYears().FirstOrDefault().Description)).Value;
        //}

        private Expression<Func<PsCardItemTransfer, PsCardItemVM>> GetPsCardItemProjection()
        {
            return s => new PsCardItemVM
            {
                Id = s.PsCardItem.Id,
                GroupId = s.PsCardItem.GroupId,
                PsCardId = s.PsCardItem.PsCardId,
                OrderItemRequestId = s.PsCardItem.OrderItemRequestId,
                TransferRefId = s.PsCardItem.TransferRefId,
                TransferId = s.Id,
                ParentId = s.ParentId,
                Fund = s.PsCardItem.PsCard.Fund,
                PoNo = s.PsCardItem.PoNo,
                PoDate = s.PsCardItem.PoDate,
                AirDate = s.PsCardItem.AirDate,
                AirNo = s.PsCardItem.AirNo,
                AirIssueDate = s.PsCardItem.AirIssueDate,
                Qty = s.Qty,
                QtyIss = s.QtyIss,
                QtyBal = s.QtyBal,
                TransferIn = s.TransferIn,
                TransferOut = s.TransferOut,
                TranType = s.TranType,
                TransDate = s.TransDate,
                Unit = s.PsCardItem.Unit,
                UnitCost = s.PsCardItem.UnitCost,
                Amount = s.Amount,
                Days = s.PsCardItem.Days,
                Remarks = s.PsCardItem.Remarks,
                InsertedDt = s.PsCardItem.InsertedDt,
                DeptId = s.PsCardItem.DeptId,
                LocationId = s.LocationId,
                Article = _db.SubAccountViews.Where(f => f.Id == s.PsCardItem.PsCard.ItemCodeId).Select(sel => sel.SubAccount + "/" + sel.Description).FirstOrDefault(),
                Account = s.PsCardItem.PsCard.ItemCode.ItemType.Description,
                Description = s.PsCardItem.Description,
                DeptDisplay = s.PsCardItem.DeptDisplay,
                StockNo = s.PsCardItem.PsCard.PsNo,
                //ParBalance = (int?)s.QtyBal - (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.Id == s.PsCardItem.Id && w.IcsPar.RefType == "P").Sum(x => x.Qty) ?? 0),
                //IcsBalance = (int?)s.QtyBal - (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.Id == s.PsCardItem.Id && w.IcsPar.RefType == "I").Sum(x => x.Qty) ?? 0),
                RemBalance = (int?)s.QtyBal,
                BalanceAmount = (decimal?)(s.QtyBal * s.PsCardItem.UnitCost),
                Department = s.PsCardItem.Codextn.Description,
                Location = s.Codextn.Description,
                LocCode = s.Codextn.Code,
                InvDistDesc = _db.Codextns.Where(w => w.CodeMast.Code == "PS-REMARKS" && w.Code == s.PsCardItem.InvDist).Select(x => x.Description).FirstOrDefault(),
                IssuedStartDate = s.PsCardItemTransferIssuances.Min(m => m.IssuedDate),
                IssuedLastDate = s.PsCardItemTransferIssuances.Max(m => m.IssuedDate),
                UpdateStartDate = s.PsCardItemTransferIssuances.Min(m => m.InsertedDt),
                UpdateLastDate = s.PsCardItemTransferIssuances.Max(m => m.UpdatedDt),
                QtyIssPrior = (int?)s.PsCardItemTransferIssuances.Where(w => w.IssuedDate.Value.Year < _issuanceYear).Sum(x => (int?)x.Qty),
                QtyIssCurrent = (int?)s.PsCardItemTransferIssuances.Where(w => w.IssuedDate.Value.Year == _issuanceYear).Sum(x => x.Qty),
                QtyIssTotal = (int?)s.PsCardItemTransferIssuances.Where(w => w.IssuedDate.Value.Year <= _issuanceYear).Sum(x => x.Qty) ?? 0,
                AmountIssPrior = s.PsCardItem.UnitCost * s.PsCardItemTransferIssuances.Where(w => w.IssuedDate.Value.Year < _issuanceYear).Sum(x => x.Qty),
                AmountIssCurrent = s.PsCardItem.UnitCost * s.PsCardItemTransferIssuances.Where(w => w.IssuedDate.Value.Year == _issuanceYear).Sum(x => x.Qty),
                AmountIssTotal = s.PsCardItem.UnitCost * (s.PsCardItemTransferIssuances.Where(w => w.IssuedDate.Value.Year <= _issuanceYear).Sum(x => x.Qty) ?? 0),
                QtyBalPrior = (int?)(s.Qty - (s.PsCardItemTransferIssuances.Where(w => w.IssuedDate.Value.Year < _issuanceYear).Sum(x => x.Qty) ?? 0)),
                QtyBalCurrent = (int?)(
                    (s.Qty - (s.PsCardItemTransferIssuances.Where(w => w.IssuedDate.Value.Year < _issuanceYear).Sum(x => x.Qty) ?? 0))
                    - ((s.PsCardItemTransferIssuances.Where(w => w.IssuedDate.Value.Year == _issuanceYear).Sum(x => x.Qty)) ?? 0)
                ),
                AmountBalPrior = s.PsCardItem.UnitCost * (s.Qty - (s.PsCardItemTransferIssuances.Where(w => w.IssuedDate.Value.Year < _issuanceYear).Sum(x => x.Qty) ?? 0)),
                AmountBalCurrent = s.PsCardItem.UnitCost * (
                    (s.Qty - (s.PsCardItemTransferIssuances.Where(w => w.IssuedDate.Value.Year < _issuanceYear).Sum(x => x.Qty) ?? 0))
                    - ((s.PsCardItemTransferIssuances.Where(w => w.IssuedDate.Value.Year == _issuanceYear).Sum(x => x.Qty) ?? 0))
                )
            };
        }

        public async ValueTask<IQueryable<PsCardItemVM>> GetAllAsync(string userId)
        {
            var isAdmin = await _userService.IsAdminAsync(userId);
            var issuanceYear = _issuanceYear; // Local variable for closure

            var filteredQuery = _db.PsCardItemTransfers.AsQueryable();
            if (!isAdmin)
            {
                filteredQuery = _db.PsCardItemTransfers.Where(w =>
                    w.PsCardItem.Codextn.DepartmentUsers.Any(a =>
                        a.UserId == userId && (
                            // Condition 1: Direct Location Match
                            (w.LocationId != null && a.Codextn.Id == w.LocationId) ||
                            // Condition 2: Fallback to Dept Match if Location is null
                            (w.LocationId == null && a.Codextn.Id == w.PsCardItem.DeptId) ||
                            // Condition 3: Hierarchy/Parent Location Logic (SubAccount matching)
                            (_db.Codextns.Any(w2 =>
                                w2.CodeMast.Code == "LOCATIONS" &&
                                w2.Code.Substring(0, 2) == a.Codextn.Code.Substring(0, 2) &&
                                w2.Code.Trim().EndsWith("00")
                            ))
                        )
                    )
                );
            }

            var data = filteredQuery.Select(a => new
            {
                // 1. Capture the main entities
                Source = a,
                Item = a.PsCardItem,
                Card = a.PsCardItem.PsCard,

                // 2. Pre-calculate the sums ONCE
                // We use (int?) to handle empty collections safely in EF6
                SumPrior = a.PsCardItemTransferIssuances
                            .Where(w => w.IssuedDate.Value.Year < issuanceYear)
                            .Sum(x => (int?)x.Qty) ?? 0,

                SumCurrent = a.PsCardItemTransferIssuances
                            .Where(w => w.IssuedDate.Value.Year == issuanceYear)
                            .Sum(x => (int?)x.Qty) ?? 0
            })
            .Select(s => new PsCardItemVM
            {
                // Standard Mappings
                Id = s.Item.Id,
                GroupId = s.Item.GroupId,
                PsCardId = s.Item.PsCardId,
                OrderItemRequestId = s.Item.OrderItemRequestId,
                TransferRefId = s.Item.TransferRefId,
                TransferId = s.Source.Id,
                ParentId = s.Source.ParentId,
                Fund = s.Card.Fund,
                PoNo = s.Item.PoNo,
                PoDate = s.Item.PoDate,
                AirDate = s.Item.AirDate,
                AirNo = s.Item.AirNo,
                AirIssueDate = s.Item.AirIssueDate,
                Qty = s.Source.Qty,
                QtyIss = s.Source.QtyIss,
                QtyBal = s.Source.QtyBal,
                TransferIn = s.Source.TransferIn,
                TransferOut = s.Source.TransferOut,
                TranType = s.Source.TranType,
                TransDate = s.Source.TransDate,
                Unit = s.Item.Unit,
                UnitCost = s.Item.UnitCost,
                Amount = s.Source.Amount,
                Days = s.Item.Days,
                Remarks = s.Item.Remarks,
                InsertedDt = s.Item.InsertedDt,
                DeptId = s.Item.DeptId,
                LocationId = s.Source.LocationId,
                Article = _db.SubAccountViews.Where(f => f.Id == s.Card.ItemCodeId).Select(sel => sel.SubAccount + "/" + sel.Description).FirstOrDefault(),
                Account = s.Card.ItemCode.ItemType.Description,
                Description = s.Item.Description,
                DeptDisplay = s.Item.DeptDisplay,
                StockNo = s.Card.PsNo,

                RemBalance = (int?)s.Source.QtyBal,
                BalanceAmount = (decimal?)(s.Source.QtyBal * s.Item.UnitCost),
                Department = s.Item.Codextn.Description,
                Location = s.Source.Codextn.Description,
                LocCode = s.Source.Codextn.Code,
                InvDistDesc = _db.Codextns.Where(w => w.CodeMast.Code == "PS-REMARKS" && w.Code == s.Item.InvDist).Select(x => x.Description).FirstOrDefault(),

                // Aggregates (Dates)
                IssuedStartDate = s.Source.PsCardItemTransferIssuances.Min(m => m.IssuedDate),
                IssuedLastDate = s.Source.PsCardItemTransferIssuances.Max(m => m.IssuedDate),
                UpdateStartDate = s.Source.PsCardItemTransferIssuances.Min(m => m.InsertedDt),
                UpdateLastDate = s.Source.PsCardItemTransferIssuances.Max(m => m.UpdatedDt),

                // Use the pre-calculated sums to do math
                // This is much faster as it doesn't create 10+ subqueries
                QtyIssPrior = s.SumPrior,
                QtyIssCurrent = s.SumCurrent,
                QtyIssTotal = s.SumPrior + s.SumCurrent,

                AmountIssPrior = s.Item.UnitCost * s.SumPrior,
                AmountIssCurrent = s.Item.UnitCost * s.SumCurrent,
                AmountIssTotal = s.Item.UnitCost * (s.SumPrior + s.SumCurrent),

                // Balance calculations
                //QtyBalPrior = (int?)((s.Source.Qty ?? 0 + s.Source.TransferIn ?? 0 - s.Source.TransferOut ?? 0) - s.SumPrior),
                //QtyBalCurrent = (int?)((s.Source.Qty ?? 0 + s.Source.TransferIn ?? 0 - s.Source.TransferOut ?? 0) - (s.SumPrior + s.SumCurrent)),
                //AmountBalPrior = s.Item.UnitCost * ((s.Source.Qty ?? 0 + s.Source.TransferIn ?? 0 - s.Source.TransferOut ?? 0) - s.SumPrior),
                //AmountBalCurrent = s.Item.UnitCost * ((s.Source.Qty ?? 0 + s.Source.TransferIn ?? 0 - s.Source.TransferOut ?? 0) - (s.SumPrior + s.SumCurrent))
                QtyBalPrior = (int?)((s.Source.Qty) - s.SumPrior),
                QtyBalCurrent = (int?)((s.Source.Qty) - (s.SumPrior + s.SumCurrent)),
                AmountBalPrior = s.Item.UnitCost * ((s.Source.Qty) - s.SumPrior),
                AmountBalCurrent = s.Item.UnitCost * ((s.Source.Qty) - (s.SumPrior + s.SumCurrent))
            });

            return data;
        }

        public async ValueTask<IQueryable<PsCardItemVM>> GetAllAsyncOld(string userId)
        {
            var isAdmin = await _userService.IsAdminAsync(userId);
            IQueryable<PsCardItemVM> data;
            if (isAdmin)
            {
                data = _db.PsCardItemTransfers.Select(GetPsCardItemProjection()).AsQueryable();
            }
            else
            {
                data = _db.PsCardItemTransfers.Where(w => w.PsCardItem.Codextn.DepartmentUsers
                    .Any(a => a.UserId == userId &&
                            //(
                            //    (w.Codextn.Id == w.LocationId && w.LocationId != null)
                            //    ||
                            //    (w.Codextn.Id == w.PsCardItem.DeptId && w.LocationId == null)
                            //    ||
                            //    //(w.Codextn.Id == w.location)                                
                            //)
                            (
                                (a.Codextn.Id == w.LocationId && w.LocationId != null)
                                ||
                                (a.Codextn.Id == w.PsCardItem.DeptId && w.LocationId == null)
                                ||
                                (_db.Codextns.Where(w2 => w2.CodeMast.Code == "LOCATIONS"
                                    && w2.Code.Substring(0, 2) == a.Codextn.Code.Substring(0, 2)
                                    && w2.Code.TrimEnd().EndsWith("00")).Any()
                                )
                            )
                        )
                    ).Select(GetPsCardItemProjection()).AsQueryable();
            }
            return data;
        }

        public async Task<IList<PsCardItemVM>> GetAllListAsync(string userId, bool? isViewOnly)
        {
            if (isViewOnly.HasValue && isViewOnly.Value == true)
            {
                var data = _db.Database.Connection.Query<PsCardItemVM>("Exec PoIssuance_GetRecords @p0, @p1", new { p0 = true, p1 = userId }).ToList();
                return data;
            }
            else
            {
                var IsAdmin = await _userService.IsAdminAsync(userId);
                var data = _db.Database.Connection.Query<PsCardItemVM>("Exec PoIssuance_GetRecords @p0, @p1", new { p0 = IsAdmin, p1 = userId }).ToList();
                return data;
            }
        }

        public IQueryable<PsCardItemVM> GetById(Guid? id)
        {
            var data = _db.Database.SqlQuery<PsCardItemVM>("Exec PoIssuance_GetByPsCardItemId {0}", id).AsQueryable();
            return data;
        }

        public IQueryable<PsCardItemVM> GetSummary()
        {
            var data = _db.Database.SqlQuery<PsCardItemVM>("Exec PoIssuance_Summary").AsQueryable();
            return data;
        }

        public IQueryable<PsCardItemVM> GetSummary(int? forYear)
        {
            var data = _db.Database.SqlQuery<PsCardItemVM>("Exec PoIssuance_Summary {0}", forYear).AsQueryable();
            return data;
        }

        public IList<PoIssuancePoSumVM> GetSummaryByPo(int? forYear, Guid? deptId)
        {
            string poNo = null;
            deptId = deptId == Guid.Empty ? null : deptId;
            var data = _db.Database.Connection.Query<PoIssuancePoSumVM>("Exec PoIssuance_SummaryByPo @p0, @p1, @p2", new { p0 = forYear, @p1 = poNo, @p2 = deptId }).ToList();
            return data;
        }

        //public async ValueTask PostAsync(Guid psCardItemIssuanceId, string user, DateTime date)
        //{
        //    var entity = await _db.PsCardItemIssuances.FindAsync(psCardItemIssuanceId);
        //    if (entity == null)
        //    {
        //        throw new RecordNotFoundException(psCardItemIssuanceId);
        //    }

        //    if (!string.IsNullOrWhiteSpace(entity.PostedBy))
        //    {
        //        throw new RecordAlreadyPostedException(string.Format("Record is currently posted.."));
        //    }

        //    if (entity.IssuedDate == null)
        //    {
        //        throw new InvalidValueException("Issued Date is Required!");
        //    }

        //    if (entity.Qty == null || entity.Qty <= 0)
        //    {
        //        throw new InvalidValueException("Quantity is Required!");
        //    }


        //    entity.PostedBy = user;
        //    entity.PostedDt = date;

        //    await _db.SaveChangesAsync();
        //}

        //public async ValueTask UnpostAsync(Guid psCardItemIssuanceId, string user, DateTime date)
        //{
        //    var entity = await _db.PsCardItemIssuances.FindAsync(psCardItemIssuanceId);

        //    if (entity == null)
        //    {
        //        throw new RecordNotFoundException(psCardItemIssuanceId);
        //    }

        //    if (string.IsNullOrWhiteSpace(entity.PostedBy))
        //    {
        //        throw new RecordNotYetPostedException(string.Format("Record is not yet posted.."));
        //    }

        //    entity.PostedBy = null;
        //    entity.PostedDt = null;
        //    entity.UpdatedBy = user;
        //    entity.UpdatedDt = date;

        //    await _db.SaveChangesAsync();
        //}

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

            //if (!model.TransDate.HasValue)
            //{
            //    throw new InvalidValueException("Transit date is required!");
            //}

            if (model.TransDate.HasValue)
            {
                if (psCardItemTransferSource.TransDate > model.TransDate)
                {
                    throw new InvalidValueException($"Transit Date must be on or after the date for this item.");
                }

                var issuanceYears = _codextnService.GetIssuanceYears().Where(w => (w.Desc3 != "Y" && w.Desc3 != "y"));
                if (issuanceYears.Any())
                {
                    var minYear = int.Parse(issuanceYears.Min(m => m.Description));
                    var maxYear = int.Parse(issuanceYears.Max(m => m.Description));

                    if (model.TransDate.Value.Year < minYear)
                    {
                        if (minYear == maxYear)
                        {
                            throw new InvalidValueException($"Year of date issued must be for year {minYear}.");
                        }
                        else
                        {
                            throw new InvalidValueException($"Year of date issued must be from {minYear} to {maxYear}.");
                        }
                    }

                    var year = model.TransDate.Value.Year.ToString().Trim();
                    var issuanceYear = issuanceYears.FirstOrDefault(f => f.Description == year);
                    if (issuanceYear == null)
                    {
                        throw new InvalidValueException($"Transit for this year is not allowed.");
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(issuanceYear.Desc2))
                        {
                            var cutOffDate = DateTime.Parse(issuanceYear.Desc2);
                            if (date.Date > cutOffDate.Date)
                            {
                                throw new InvalidValueException($"Transit for this year is only valid until {cutOffDate.ToShortDateString()}.");
                            }                            
                        }

                        if (issuanceYear.Desc4?.ToUpper() == "Y")
                        {
                            if (!(await _userService.IsUserNameAdminAsync(user)))
                            {
                                throw new InvalidValueException($"Transit for this year is only allowed for Admins.");
                            }
                        }
                    }
                }
                else
                {
                    throw new InvalidValueException($"Issuance year setup is not a available.");
                }
            }
            else
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
                    //_db.Entry(psCardItemTransferItem).State = EntityState.Added;
                    await _db.SaveChangesAsync();
                }
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
                        //_db.Entry(psCardItemTransferItem).State = EntityState.Added;
                        await _db.SaveChangesAsync();
                    }
                }
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
            await _db.SaveChangesAsync();

            // update parent record
            await _psCardService.PsCardItem.PsCardItemTransfer.UpdatePsCardItemTransfer(psCardItemTransferSource.Id, user, date);

            return psCardItemTransfer;
        }

        private void UpdatePsCardItem()
        {

        }

        private async Task<PsCardItem> CreatePsCardItemAsync(PsCardItem sourcePsCardItem, Guid? transferRefId, Guid? locationId, int? transOut, string user, DateTime date)
        {
            var targetPsCardItem = await _db.PsCardItems.AsNoTracking().FirstOrDefaultAsync(f => f.Id == sourcePsCardItem.Id); // load source value, then use this as target            

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
            await _db.SaveChangesAsync();

            var totalTransferOut = _db.PsCardItemTransfers.Where(w => w.PsCardItemId == sourcePsCardItem.Id).Sum(s => s.Qty) ?? 0;
            var qtyBal = ((sourcePsCardItem.Qty ?? 0) + (sourcePsCardItem.TransferIn ?? 0)) - ((sourcePsCardItem.QtyIss ?? 0) + totalTransferOut);

            sourcePsCardItem.TransferOut = totalTransferOut;
            sourcePsCardItem.QtyBal = qtyBal;
            sourcePsCardItem.Amount = qtyBal * sourcePsCardItem.UnitCost;
            sourcePsCardItem.GTotalCost = qtyBal * sourcePsCardItem.TUnitCost;
            sourcePsCardItem.UpdatedBy = user;
            sourcePsCardItem.UpdatedDt = date;

            //_db.PsCardItems.Attach(sourcePsCardItem);
            //_db.Entry(sourcePsCardItem).State = EntityState.Modified;
            await _db.SaveChangesAsync();

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