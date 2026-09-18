using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.Items;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
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
        IQueryable<PsCardItemVM> GetTransitByCardId(Guid? cardId, string userName);
        IQueryable<PsCardItemVM> GetAllStocks();
        IQueryable<PsCardItemVM> GetAllProperties();
        ValueTask<List<PsCardItemVM>> GetAllWithExtnsAsync();
        ValueTask<PsCardItemVM> GetByIdAsync(Guid? id);
        ValueTask<PsCardItemVM> GetByTransferIdAsync(Guid? id);
        ValueTask<PsCardItemVM> GetByGroupIdAsync(Guid? groupId);
        ValueTask<string> GetCategoryAsync(Guid? psCardItemId);

        ValueTask<PsCardItem> GetByItemExtnIdAsync(Guid? psCardItemExtnId);

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
        IPsCardItemTransferService PsCardItemTransfer { get; }
    }

    internal class PsCardItemService : IPsCardItemService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<PsCardItemVM> _vmExceptionService;
        private readonly IExceptionService<PsCardItem> _exceptionService;
        private readonly IExceptionService<ParIcsItemVm> _parIcsItemExceptionService;
        private readonly IPsCardItemValidator _psCardItemValidator;
        private readonly IItemCodeService _itemCodeService;
        private readonly IUserService _userService;
        private readonly IPsCardItemExtnService _psCardItemExtnService;
        private readonly IPsCardItemTransferService _psCardItemTransferService;
        private readonly ICodextnService _codextnService;

        public PsCardItemService(AppManEntities db)
        {
            _db = db;
            _vmExceptionService = new ExceptionService<PsCardItemVM>();
            _exceptionService = new ExceptionService<PsCardItem>();
            _parIcsItemExceptionService = new ExceptionService<ParIcsItemVm>();
            _psCardItemValidator = new PsCardItemValidator(_db);
            _itemCodeService = new ItemCodeService(_db);
            _userService = new UserService(_db);
            _psCardItemExtnService = new PsCardItemExtnService(_db);
            _psCardItemTransferService = new PsCardItemTransferService(_db);
            _codextnService = new CodextnService(_db);
        }


        //public PsCardItemService(AppManEntities db,
        //    IAppManEntitiesFactory appManEntitiesFactory,
        //    IExceptionService<PsCardItemVM> vmExceptionService,
        //    IExceptionService<PsCardItem> exceptionService,
        //    IExceptionService<ParIcsItemVm> parIcsItemExceptionService,
        //    IPsCardItemValidator psCardItemValidator,
        //    IItemCodeService itemCodeService,
        //    IUserService userService,
        //    IPsCardItemExtnService psCardItemExtnService,
        //    IPsCardItemTransferService psCardItemTransferService)
        //{
        //    _db = db;
        //    _contextFactory = appManEntitiesFactory;
        //    _vmExceptionService = vmExceptionService;
        //    _exceptionService = exceptionService;
        //    _parIcsItemExceptionService = parIcsItemExceptionService;
        //    _psCardItemValidator = psCardItemValidator;
        //    _itemCodeService = itemCodeService;
        //    _userService = userService;
        //    _psCardItemExtnService = psCardItemExtnService;
        //    _psCardItemTransferService = psCardItemTransferService;                                    
        //}

        public IPsCardItemExtnService PsCardItemExtn => _psCardItemExtnService;
        public IPsCardItemTransferService PsCardItemTransfer => _psCardItemTransferService;

        private Expression<Func<PsCardItem, PsCardItemVM>> GetPsCardItemProjection()
        {
            return s => new PsCardItemVM
            {
                Id = s.Id,
                Fund = s.PsCard.Fund,
                GroupId = s.GroupId,
                PsCardId = s.PsCardId,
                OrderItemRequestId = s.OrderItemRequestId,
                TransferRefId = s.TransferRefId,
                PoDate = s.PoDate,
                PoNo = s.PoNo,
                AirDate = s.AirDate,
                AirNo = s.AirNo,
                AirIssueDate = s.AirIssueDate,
                TransferId = s.PsCardItemTransfers.FirstOrDefault().Id,
                ParentId = s.PsCardItemTransfers.FirstOrDefault().ParentId,
                Qty = s.PsCardItemTransfers.FirstOrDefault().Qty,
                QtyIss = s.PsCardItemTransfers.FirstOrDefault().QtyIss,
                QtyBal = s.PsCardItemTransfers.FirstOrDefault().QtyBal,
                TransferIn = s.PsCardItemTransfers.FirstOrDefault().TransferIn,
                TransferOut = s.PsCardItemTransfers.FirstOrDefault().TransferOut,
                TranType = s.PsCardItemTransfers.FirstOrDefault().TranType,
                Days = s.Days,
                Unit = s.Unit,
                UnitCost = s.UnitCost,
                Amount = s.UnitCost * s.PsCardItemTransfers.FirstOrDefault().Qty,
                IssueAmount = (s.PsCardItemTransfers.FirstOrDefault().PsCardItemTransferIssuances.Sum(t => t.Amount) ?? 0),
                BalanceAmount = (s.PsCardItemTransfers.Sum(t => t.Amount) ?? 0),
                PriceRate = s.PriceRate,
                AddCost = s.AddCost,
                TUnitCost = s.TUnitCost,
                GTotalCost = s.GTotalCost,
                Remarks = s.Remarks,
                DeptId = s.DeptId,
                LocationId = s.PsCardItemTransfers.FirstOrDefault().LocationId,
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
                Location = s.PsCardItemTransfers.FirstOrDefault().Codextn.Description,
                LocCode = s.PsCardItemTransfers.FirstOrDefault().Codextn.Code,
                PrevPsNo = s.PrevPsNo,
                SetLotNo = s.SetLotNo,
                SetLotAmount = s.SetLotAmount,
                SetLotRemarks = s.SetLotRemarks,
                PostedBy = s.PostedBy,
                PostedDt = s.PostedDt,
                IsWithItemExtn = s.PsCardItemExtns.Any(),
                PpmpItemId = s.PpmpItemId,
                PpmpCode = s.PpmpCode
            };
        }
        
        public async ValueTask<PsCardItemVM> GetByIdAsync(Guid? id)
        {
            var data = await _db.PsCardItems
                .Include(i => i.Codextn) // Department
                .Include(i => i.Codextn1) // Location
                .Include(i => i.PsCardItemExtns)
                .Include(i => i.PsCardItemTransfers)
                .Where(w => w.Id == id)
                .Select(GetPsCardItemProjection()).FirstOrDefaultAsync();
            return data;
        }

        public async ValueTask<PsCardItemVM> GetByTransferIdAsync(Guid? id)
        {
            var data = await _db.PsCardItems
                .Include(i => i.Codextn) // Department
                .Include(i => i.Codextn1) // Location
                .Include(i => i.PsCardItemExtns)
                .Include(i => i.PsCardItemTransfers)
                .Where(w => w.PsCardItemTransfers.Any(a => a.Id == id))
                .Select(GetPsCardItemProjection()).FirstOrDefaultAsync();
            return data;
        }

        public async ValueTask<PsCardItemVM> GetByGroupIdAsync(Guid? groupId)
        {
            var data = await _db.PsCardItems
                .Include(i => i.Codextn) // Department
                .Include(i => i.Codextn1) // Location
                .Include(i => i.PsCardItemExtns)
                .Include(i => i.PsCardItemTransfers)
                .Where(w => w.GroupId == groupId)
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
                    .Include(i => i.PsCardItemTransfers)
                    .Where(w => w.PsCardId == cardId).AsNoTracking()
                    .Select(GetPsCardItemProjection());
            }
            else
            {
                data = _db.PsCardItems
                    .Include(i => i.Codextn) // Department
                    .Include(i => i.Codextn1) // Location
                    .Include(i => i.PsCardItemTransfers)
                    .Where(w => w.PsCardId == cardId && w.InsertedBy == userName).AsNoTracking()
                    .Select(GetPsCardItemProjection());
            }
            return data;
        });

        private Expression<Func<PsCardItemTransfer, PsCardItemVM>> GetPsCardItemTransferProjection()
        {
            return s => new PsCardItemVM
            {
                Id = s.PsCardItem.Id,
                GroupId = s.PsCardItem.GroupId,
                PsCardId = s.PsCardItem.PsCardId,
                OrderItemRequestId = s.PsCardItem.OrderItemRequestId,
                TransferRefId = s.PsCardItem.TransferRefId, // retained, but not used anymore.
                PoDate = s.PsCardItem.PoDate,
                PoNo = s.PsCardItem.PoNo,
                AirDate = s.PsCardItem.AirDate,
                AirNo = s.PsCardItem.AirNo,
                AirIssueDate = s.PsCardItem.AirIssueDate,
                TransferId = s.Id == null ? Guid.NewGuid() : s.Id,
                ParentId = s.ParentId,
                Qty = s.Qty,
                QtyIss = s.QtyIss,
                QtyBal = s.QtyBal,
                TransferIn = s.TransferIn,
                TransferOut = s.TransferOut,
                TranType = s.TranType,
                TransDate = s.ParentId == null ? null: s.TransDate,
                Days = s.PsCardItem.Days,
                Unit = s.PsCardItem.Unit,
                UnitCost = s.PsCardItem.UnitCost,
                Amount = s.PsCardItem.UnitCost * s.QtyBal,
                IssueAmount = (s.PsCardItemTransferIssuances.Sum(sum => sum.Qty) ?? 0) * s.PsCardItem.UnitCost,
                BalanceAmount = (s.PsCardItem.UnitCost * s.Qty) - ((s.PsCardItemTransferIssuances.Sum(sum => sum.Qty) ?? 0) * s.PsCardItem.UnitCost),
                PriceRate = s.PsCardItem.PriceRate,
                AddCost = s.PsCardItem.AddCost,
                TUnitCost = s.PsCardItem.TUnitCost,
                GTotalCost = s.PsCardItem.GTotalCost,
                Remarks = s.PsCardItem.Remarks,
                DeptId = s.PsCardItem.DeptId,
                LocationId = s.LocationId,
                DeptCode = s.PsCardItem.Codextn.Code,
                DeptDisplay = s.PsCardItem.DeptDisplay,
                Description = s.PsCardItem.Description,
                OtherDesc = s.PsCardItem.OtherDesc,
                IsForICS = s.PsCardItem.IsForICS,
                IsConsumable = s.PsCardItem.IsConsumable,
                IsIncorporated = s.PsCardItem.IsIncorporated,
                IsOthers = s.PsCardItem.IsOthers,
                OtherRemarks = s.PsCardItem.OtherRemarks,
                Type = s.PsCardItem.Type,
                InvDist = s.PsCardItem.InvDist,
                AcqMode = s.PsCardItem.AcqMode,
                AcqDate = s.PsCardItem.AcqDate,
                AreaSoldDonated = s.PsCardItem.AreaSoldDonated,
                ConstructionYear = s.PsCardItem.ConstructionYear,
                Vendor = s.PsCardItem.Vendor,
                OldAmount = s.PsCardItem.OldAmount,
                PhaseNo = s.PsCardItem.PhaseNo,
                PhaseAmount = s.PsCardItem.PhaseAmount,
                InsertedBy = s.PsCardItem.InsertedBy,
                InsertedDt = s.PsCardItem.InsertedDt,
                Department = s.PsCardItem.Codextn.Description,
                Location = s.Codextn.Description,
                LocCode = s.Codextn.Code,
                PrevPsNo = s.PsCardItem.PrevPsNo,
                SetLotNo = s.PsCardItem.SetLotNo,
                SetLotAmount = s.PsCardItem.SetLotAmount,
                SetLotRemarks = s.PsCardItem.SetLotRemarks,
                PostedBy = s.PsCardItem.PostedBy,
                PostedDt = s.PsCardItem.PostedDt,
                IsWithItemExtn = s.PsCardItem.PsCardItemExtns.Any(),
                Consumable = s.PsCardItem.IsConsumable.HasValue ? (s.PsCardItem.IsConsumable == true ? "Y" : "N") : "",
                PpmpCode = s.PsCardItem.PpmpCode,
                PpmpItemId = s.PsCardItem.PpmpItemId
            };
        }

        public IQueryable<PsCardItemVM> GetTransitByCardId(Guid? cardId, string userName) => _vmExceptionService.TryCatch(() =>
        {
            IQueryable<PsCardItemVM> data = null;
            if (string.IsNullOrWhiteSpace(userName))
            {
                data = _db.PsCardItemTransfers
                    .Include(i => i.Codextn) // Location
                    .Include(i => i.PsCardItem.Codextn) // Department
                    .Include(i => i.PsCardItemTransferIssuances)
                    .Where(w => w.PsCardItem.PsCardId == cardId).AsNoTracking()
                    .Select(GetPsCardItemTransferProjection());
            }
            else
            {
                data = _db.PsCardItemTransfers
                    .Include(i => i.Codextn) // Location
                    .Include(i => i.PsCardItem.Codextn) // Department
                    .Include(i => i.PsCardItemTransferIssuances)
                    .Where(w => w.PsCardItem.PsCardId == cardId && w.PsCardItem.InsertedBy == userName).AsNoTracking()
                    .Select(GetPsCardItemTransferProjection());
            }
            return data;
        });


        private Expression<Func<PsCardItem, PsCardItemVM>> GetAllProjectionX()
        {
            return s => new PsCardItemVM
            {
                Id = s.Id,
                TransferId = s.PsCardItemTransfers.FirstOrDefault().Id,
                Fund = s.PsCard.Fund,
                ItemCodeId = s.PsCard.ItemCodeId,
                Account = s.PsCard.ItemCode.ItemType.Description,
                StockNo = s.PsCard.PsNo,
                SubAccount = _db.SubAccountViews.Where(f => f.Id == s.PsCard.ItemCodeId).Select(sel => sel.SubAccount).FirstOrDefault(),
                Article = s.PsCard.ItemCode.Description,
                PoDate = s.PoDate,
                PoNo = s.PoNo,
                AirDate = s.AirDate,
                AirNo = s.AirNo,
                AirIssueDate = s.AirIssueDate,
                Qty = s.PsCardItemTransfers.FirstOrDefault().Qty,
                QtyIss = s.PsCardItemTransfers.FirstOrDefault().QtyIss,
                QtyBal = s.PsCardItemTransfers.FirstOrDefault().QtyBal,                
                TransferIn = s.PsCardItemTransfers.FirstOrDefault().TransferIn,
                TransferOut = s.PsCardItemTransfers.FirstOrDefault().TransferOut,
                TranType = s.PsCardItemTransfers.FirstOrDefault().TranType,
                Days = s.Days,
                Unit = s.Unit,
                UnitCost = s.UnitCost,
                Amount = s.UnitCost * s.PsCardItemTransfers.FirstOrDefault().QtyBal,
                IssueAmount = s.UnitCost * s.PsCardItemTransfers.FirstOrDefault().QtyIss,
                BalanceAmount = s.UnitCost * s.PsCardItemTransfers.FirstOrDefault().QtyBal,
                PriceRate = s.PriceRate,
                AddCost = s.AddCost,
                GTotalCost = s.GTotalCost,
                Remarks = s.Remarks,
                DeptId = s.DeptId,
                LocationId = s.PsCardItemTransfers.FirstOrDefault().LocationId,
                DeptDisplay = s.DeptDisplay,
                Description = s.Description,
                InsertedBy = s.InsertedBy,
                InsertedDt = s.InsertedDt,
                UpdatedBy = s.UpdatedBy,
                UpdatedDt = s.UpdatedDt,
                Department = s.Codextn.Description,
                Location = s.Codextn1.Description,
                LocCode = s.Codextn1.Code,
                PpmpCode = s.PpmpCode,
                PpmpItemId = s.PpmpItemId
            };
        }

        private Expression<Func<PsCardItemTransfer, PsCardItemVM>> GetAllProjection()
        {
            return s => new PsCardItemVM
            {
                Id = s.PsCardItem.Id,
                TransferId = s.Id,
                Fund = s.PsCardItem.PsCard.Fund,
                ItemCodeId = s.PsCardItem.PsCard.ItemCodeId,
                Account = s.PsCardItem.PsCard.ItemCode.ItemType.Description,
                StockNo = s.PsCardItem.PsCard.PsNo,
                SubAccount = _db.SubAccountViews.Where(f => f.Id == s.PsCardItem.PsCard.ItemCodeId).Select(sel => sel.SubAccount).FirstOrDefault(),
                Article = s.PsCardItem.PsCard.ItemCode.Description,
                PoDate = s.PsCardItem.PoDate,
                PoNo = s.PsCardItem.PoNo,
                AirDate = s.PsCardItem.AirDate,
                AirNo = s.PsCardItem.AirNo,
                AirIssueDate = s.PsCardItem.AirIssueDate,
                Qty = s.Qty,
                QtyIss = s.QtyIss,
                QtyBal = s.QtyBal,
                TransferIn = s.TransferIn,
                TransferOut = s.TransferOut,
                TranType = s.TranType,
                Days = s.PsCardItem.Days,
                Unit = s.PsCardItem.Unit,
                UnitCost = s.PsCardItem.UnitCost,
                Amount = s.PsCardItem.UnitCost * s.QtyBal,
                IssueAmount = s.PsCardItem.UnitCost * s.QtyIss,
                BalanceAmount = s.PsCardItem.UnitCost * s.QtyBal,
                PriceRate = s.PsCardItem.PriceRate,
                AddCost = s.PsCardItem.AddCost,
                GTotalCost = s.PsCardItem.GTotalCost,
                Remarks = s.PsCardItem.Remarks,
                DeptId = s.PsCardItem.DeptId,
                LocationId = s.LocationId,
                DeptDisplay = s.PsCardItem.DeptDisplay,
                Description = s.PsCardItem.Description,
                InsertedBy = s.PsCardItem.InsertedBy,
                InsertedDt = s.PsCardItem.InsertedDt,
                UpdatedBy = s.PsCardItem.UpdatedBy,
                UpdatedDt = s.PsCardItem.UpdatedDt,
                Department = s.PsCardItem.Codextn.Description,
                DeptCode = s.PsCardItem.Codextn.Code,
                Location = s.Codextn.Description,
                LocCode = s.Codextn.Code,
                InvDistDesc = _db.Codextns.Where(w => w.CodeMast.Code == "PS-REMARKS" && w.Code == s.PsCardItem.InvDist).Select(sel => sel.Description).FirstOrDefault(),
                SemiExpendable = (s.PsCardItem.PsCard.ItemCode.ItemType.Code == "S" || s.PsCardItem.PsCard.ItemCode.ItemType.Code == "J") ? "Y" :
                    s.PsCardItem.PsCard.ItemCode.IsConsumable == "Y" ? "N" : s.PsCardItem.PsCard.ItemCode.IsConsumable == "N" ? "Y" : "",
                EncodedSemiExpendable = s.PsCardItem.IsConsumable.HasValue ? (s.PsCardItem.IsConsumable == true ? "N" : "Y") : "",
                Consumable = s.PsCardItem.IsConsumable.HasValue ? (s.PsCardItem.IsConsumable == true ? "Y" : "N") : "",
                PpmpCode = s.PsCardItem.PpmpCode,
                PpmpItemId = s.PsCardItem.PpmpItemId
            };
        }

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
            //var data = _db.PsCardItems.Include(i => i.PsCardItemTransfer).Where(w => w.PsCard.CardCategory == category).Select(GetAllProjection());
            var data = _db.PsCardItemTransfers.Where(w => w.PsCardItem.PsCard.CardCategory == category).Select(GetAllProjection());
            return data;
        }

        private IQueryable<PsCardItemVM> GetAllOld(string category)
        {
            var data = _db.Database.SqlQuery<PsCardItemVM>("Exec Card_GetQueryRecords {0}", category).AsQueryable();
            return data;
        }

        public async ValueTask<List<PsCardItemVM>> GetAllWithExtnsAsync()
        {
            var data = await _db.Database.SqlQuery<PsCardItemVM>("Exec Card_GetAllWithExtns").ToListAsync();
            return data;
        }

        public async ValueTask<string> GetCategoryAsync(Guid? psCardItemId)
        {
            return await _db.PsCardItems.Where(w => w.Id == psCardItemId).Select(s => s.PsCard.ItemCode.ItemType.Code).FirstOrDefaultAsync();
        }

        public async ValueTask<PsCardItem> GetByItemExtnIdAsync(Guid? psCardItemExtnId)
        {
            return await _db.PsCardItems.FirstOrDefaultAsync(f => f.PsCardItemExtns.Any(a => a.Id == psCardItemExtnId));
        }

        public ValueTask<PsCardItemVM> CreateAsync(PsCardItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            _psCardItemValidator.ValidateOnCreate(model);
            ValidatePoDate(model.PoDate);

            model.Id = Guid.NewGuid();
            model.TransferId = Guid.NewGuid();

            var entity = new PsCardItem();
            var isAdmin = await _userService.IsAdminAsync(user);

            MapModelToEntityFields(entity, model, Mode.ADD, isAdmin);

            if (!string.IsNullOrWhiteSpace(model.PoNo))
            {
                if (!(await _db.PsCardRefNos.AnyAsync(a => a.RefType == "PO" && a.RefNo == model.PoNo && a.RefDate == model.PoDate && a.DepId == model.DeptId)))
                {
                    var psCardRefNo = new PsCardRefNo
                    {
                        Id = Guid.NewGuid(),
                        RefType = "PO",
                        RefNo = model.PoNo,
                        RefDate = model.PoDate,
                        DepId = model.DeptId,
                        InsertedBy = user,
                        InsertedDt = date
                    };

                    _db.PsCardRefNos.Add(psCardRefNo);
                }
            }

            _db.PsCardItems.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<PsCardItemVM> UpdateAsync(PsCardItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            _psCardItemValidator.ValidateOnUpdate(model);            

            var psCardItemEntity = await _db.PsCardItems.FindAsync(model.Id);

            ValidateUser(psCardItemEntity, model);

            var isAdmin = await _userService.IsUserNameAdminAsync(user);
            var entity = await _db.PsCardItemTransfers.Include(i => i.PsCardItem)
                .FirstOrDefaultAsync(f => f.Id == model.TransferId);
            if (entity == null)
            {
                //ValidateIssuance(model.PoDate, date);
                model.TransferId = Guid.NewGuid();
                var psCardItemTransfer = new PsCardItemTransfer()
                {
                    Id = (Guid)model.TransferId,
                    PsCardItemId = model.Id,
                    Qty = model.Qty,
                    TransDate = model.TransDate,
                    QtyIss = model.QtyIss,
                    QtyBal = model.QtyBal,
                    TransferIn = model.TransferIn,
                    TransferOut = model.TransferOut,
                    Amount = model.TUnitCost * model.QtyBal,
                    LocationId = model.LocationId,
                    TranType = model.TranType,
                    InsertedBy = model.InsertedBy,
                    InsertedDt = model.InsertedDt,
                    UpdatedBy = model.UpdatedBy,
                    UpdatedDt = model.UpdatedDt
                };

                psCardItemEntity.PoDate = model.PoDate;
                psCardItemEntity.PoNo = model.PoNo.Trim();
                psCardItemEntity.AirDate = model.AirDate;
                psCardItemEntity.AirNo = model.AirNo;
                psCardItemEntity.AirIssueDate = model.AirIssueDate;
                psCardItemEntity.Days = model.Days;
                psCardItemEntity.Unit = model.Unit;
                psCardItemEntity.Remarks = model.Remarks;
                psCardItemEntity.DeptId = model.DeptId;
                psCardItemEntity.Description = model.Description;
                psCardItemEntity.OtherDesc = model.OtherDesc;
                psCardItemEntity.DeptDisplay = model.DeptDisplay;
                psCardItemEntity.Type = model.Type;
                psCardItemEntity.InvDist = model.InvDist;
                psCardItemEntity.AcqDate = model.AcqDate;
                psCardItemEntity.AcqMode = model.AcqMode;
                psCardItemEntity.AreaSoldDonated = model.AreaSoldDonated;
                psCardItemEntity.ConstructionYear = model.ConstructionYear;
                psCardItemEntity.Vendor = model.Vendor;
                psCardItemEntity.OldAmount = model.OldAmount;
                psCardItemEntity.PhaseNo = model.PhaseNo;
                psCardItemEntity.PhaseAmount = model.PhaseAmount;
                psCardItemEntity.SetLotNo = model.SetLotNo;
                psCardItemEntity.SetLotAmount = model.SetLotAmount;
                psCardItemEntity.SetLotRemarks = model.SetLotRemarks;
                psCardItemEntity.PrevPsNo = model.PrevPsNo;
                psCardItemEntity.UpdatedBy = model.UpdatedBy;
                psCardItemEntity.UpdatedDt = model.UpdatedDt;
                psCardItemEntity.ProRatedCost = model.ProRatedCost;
                psCardItemEntity.OtherQty = model.OtherQty;
                psCardItemEntity.UnitCost = model.UnitCost;
                psCardItemEntity.AddCost = model.AddCost;
                psCardItemEntity.TUnitCost = model.TUnitCost;
                psCardItemEntity.PriceRate = model.PriceRate;
                psCardItemEntity.FPP = model.FPP;

                psCardItemEntity.Qty = model.Qty;
                psCardItemEntity.QtyIss = model.QtyIss;
                psCardItemEntity.QtyBal = model.QtyBal;
                psCardItemEntity.TransferIn = model.TransferIn;
                psCardItemEntity.TransferOut = model.TransferOut;
                psCardItemEntity.Amount = model.UnitCost * model.QtyBal;
                psCardItemEntity.GTotalCost = model.TUnitCost * model.QtyBal;

                psCardItemEntity.PpmpCode = model.PpmpCode;
                psCardItemEntity.PpmpItemId = model.PpmpItemId;

                if (isAdmin)
                {
                    if (string.IsNullOrWhiteSpace(model.Consumable))
                    {
                        psCardItemEntity.IsConsumable = null;
                    }
                    else
                    {
                        model.Consumable = model.Consumable.ToUpper();
                        psCardItemEntity.IsConsumable = model.Consumable == "Y" ? true : false;
                    }
                }
                
                psCardItemEntity.PsCardItemTransfers.Add(psCardItemTransfer);
                //_db.PsCardItems.Attach(psCardItemEntity);
                //_db.Entry(psCardItemEntity).State = EntityState.Modified;                    
            }
            else
            {
                ValidatePoDate(entity.TransDate);
                ValidatePoDate(model.PoDate);
                
                entity.PsCardItem.PoDate = model.PoDate;
                entity.PsCardItem.PoNo = model.PoNo.Trim();
                entity.PsCardItem.AirDate = model.AirDate;
                entity.PsCardItem.AirNo = model.AirNo;
                entity.PsCardItem.AirIssueDate = model.AirIssueDate;
                entity.PsCardItem.Days = model.Days;
                entity.PsCardItem.Unit = model.Unit;
                entity.PsCardItem.Remarks = model.Remarks;
                entity.PsCardItem.DeptId = model.DeptId;
                entity.PsCardItem.Description = model.Description;
                entity.PsCardItem.OtherDesc = model.OtherDesc;
                entity.PsCardItem.DeptDisplay = model.DeptDisplay;
                entity.PsCardItem.Type = model.Type;
                entity.PsCardItem.InvDist = model.InvDist;
                entity.PsCardItem.AcqDate = model.AcqDate;
                entity.PsCardItem.AcqMode = model.AcqMode;
                entity.PsCardItem.AreaSoldDonated = model.AreaSoldDonated;
                entity.PsCardItem.ConstructionYear = model.ConstructionYear;
                entity.PsCardItem.Vendor = model.Vendor;
                entity.PsCardItem.OldAmount = model.OldAmount;
                entity.PsCardItem.PhaseNo = model.PhaseNo;
                entity.PsCardItem.PhaseAmount = model.PhaseAmount;
                entity.PsCardItem.SetLotNo = model.SetLotNo;
                entity.PsCardItem.SetLotAmount = model.SetLotAmount;
                entity.PsCardItem.SetLotRemarks = model.SetLotRemarks;
                entity.PsCardItem.PrevPsNo = model.PrevPsNo;
                entity.PsCardItem.UpdatedBy = model.UpdatedBy;
                entity.PsCardItem.UpdatedDt = model.UpdatedDt;
                entity.PsCardItem.ProRatedCost = model.ProRatedCost;
                entity.PsCardItem.OtherQty = model.OtherQty;
                entity.PsCardItem.UnitCost = model.UnitCost;
                entity.PsCardItem.AddCost = model.AddCost;
                entity.PsCardItem.TUnitCost = model.TUnitCost;
                entity.PsCardItem.PriceRate = model.PriceRate;
                entity.PsCardItem.FPP = model.FPP;

                entity.PsCardItem.PpmpCode = model.PpmpCode;
                entity.PsCardItem.PpmpItemId = model.PpmpItemId;

                if (model.ParentId == null) // original record
                {
                    entity.PsCardItem.Qty = model.Qty;
                    entity.PsCardItem.QtyIss = model.QtyIss;
                    entity.PsCardItem.QtyBal = model.QtyBal;
                    entity.PsCardItem.TransferIn = model.TransferIn;
                    entity.PsCardItem.TransferOut = model.TransferOut;
                    entity.PsCardItem.Amount = model.UnitCost * model.QtyBal;
                    entity.PsCardItem.GTotalCost = model.TUnitCost * model.QtyBal;
                    entity.TransDate = model.PoDate;
                }
                else
                {
                    entity.PsCardItem.Qty = 0;
                    entity.TransDate = model.TransDate;
                }

                if (isAdmin)
                {
                    //entity.PsCardItem.IsConsumable = model.IsConsumable;
                    if (string.IsNullOrWhiteSpace(model.Consumable))
                    {
                        entity.PsCardItem.IsConsumable = null;
                    }
                    else
                    {
                        model.Consumable = model.Consumable.ToUpper();
                        entity.PsCardItem.IsConsumable = model.Consumable == "Y" ? true : false;
                    }
                }
                // edit not allowed for users 
                //else
                //{
                //    var psCard = await _db.PsCards.Include(i => i.ItemCode).FirstOrDefaultAsync(f => f.Id == model.PsCardId);
                //    psCardItemEntity.IsConsumable = _itemCodeService.GetIsConsumable(psCard.ItemCode.IsConsumable);                    
                //}
                
                entity.Qty = model.Qty;
                entity.QtyIss = model.QtyIss;
                entity.QtyBal = model.QtyBal;
                entity.TransferIn = model.TransferIn;
                entity.TransferOut = model.TransferOut;
                entity.Amount = model.TUnitCost * model.QtyBal;
                entity.LocationId = model.LocationId;
                entity.TranType = model.TranType;
                entity.UpdatedBy = model.UpdatedBy;
                entity.UpdatedDt = model.UpdatedDt;

                if (!string.IsNullOrWhiteSpace(model.PoNo))
                {
                    if (!(await _db.PsCardRefNos.AnyAsync(a => a.RefType == "PO" && a.RefNo == model.PoNo && a.RefDate == model.PoDate && a.DepId == model.DeptId)))
                    {
                        var psCardRefNo = new PsCardRefNo
                        {
                            Id = Guid.NewGuid(),
                            RefType = "PO",
                            RefNo = model.PoNo,
                            RefDate = model.PoDate,
                            DepId = model.DeptId,
                            InsertedBy = user,
                            InsertedDt = date
                        };

                        _db.PsCardRefNos.Add(psCardRefNo);
                    }
                }

                await _db.SaveChangesAsync();
                await _psCardItemTransferService.UpdatePsCardItemTransfer(model.TransferId, user, date);
            }

            return model;
        });

        public ValueTask<ParIcsItemVm> UpdateIsForICSAsync(ParIcsItemVm model, string user, DateTime date) => _parIcsItemExceptionService.TryCatch(async () =>
        {
            var entity = await _db.PsCardItems.Include(i => i.PsCardItemTransfers).FirstOrDefaultAsync(f => f.Id == model.PsCardItemId);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.PsCardItemId);
            }

            if (!string.IsNullOrWhiteSpace(entity.ParPostedBy))
            {
                throw new RecordAlreadyPostedException("Item already posted, cannot update!"); ;
            }

            if (entity.IsForICS != model.IsForICS) // change in isForICS
            {
                var icsParItem = await _db.IcsParItems.Include(i => i.IcsPar).FirstOrDefaultAsync(f => f.PsCardItemExtn.PsCardItem.GroupId == model.GroupId);
                if (icsParItem != null)
                {
                    if (model.IsForICS == true && icsParItem.IcsPar.RefType == "P")
                    {
                        throw new InvalidValueException("Item with PAR already exists, cannot make this as For ICS.");
                    }
                    else
                    {
                        if (model.IsForICS != true && icsParItem.IcsPar.RefType == "I")
                        {
                            throw new InvalidValueException("Item with ICS already exists, cannot remove this as For ICS.");
                        }
                    }
                }
            }

            var addCost = model.AddCost ?? 0;
            var tUnitCost = addCost + model.UnitCost;
            //var gTotalCost = model.Qty * tUnitCost;
            entity.AddCost = addCost;
            entity.TUnitCost = tUnitCost;
            //entity.GTotalCost = gTotalCost;
            entity.IsForICS = model.IsForICS;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            foreach (var psCardItemTransfer in entity.PsCardItemTransfers)
            {
                psCardItemTransfer.Amount = psCardItemTransfer.QtyBal * tUnitCost;
            }

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
                throw new RecordNotFoundException(model.GroupId);
            }

            var psCardItem = entity.FirstOrDefault(f => f.TransferRefId == null);

            if (psCardItem.IsConsumable != model.IsConsumable
                || psCardItem.IsIncorporated != model.IsIncorporated
                || psCardItem.IsOthers != model.IsOthers) // change in decision
            {
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
                        if ((model.IsConsumable != true
                            || model.IsIncorporated != true
                            || model.IsOthers != true)
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
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItems.FindAsync(model.Id);
            ValidateUser(entity, model);
            ValidateRelationship(model);

            var transferItem = await _db.PsCardItemTransfers.Include(i => i.PsCardItem)
                .FirstOrDefaultAsync(f => f.Id == model.TransferId);

            ValidatePoDate(transferItem.TransDate);

            await _psCardItemTransferService.DeleteAsync(model.TransferId, user, date);

            // manually remove transaction log
            //var psCardItemTransactions = _db.PsCardItemTransactions.Where(w => w.PsCardItemId == entity.Id);
            //_db.PsCardItemTransactions.RemoveRange(psCardItemTransactions);
            //await _db.SaveChangesAsync();

            if (model.ParentId == null && model.OrderItemRequestId == null) // manual records
            {
                entity.UpdatedBy = model.UpdatedBy;
                entity.UpdatedDt = model.UpdatedDt;

                await _db.SaveChangesAsync();

                _db.PsCardItems.Remove(entity);
                await _db.SaveChangesAsync();
            }

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

        public void MapModelToEntityFields(PsCardItem entity, PsCardItemVM model, Mode mode, bool isAdmin)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.GroupId = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            entity.PsCardId = model.PsCardId;
            entity.OrderItemRequestId = model.OrderItemRequestId;
            entity.PoDate = model.PoDate;
            entity.PoNo = model.PoNo.Trim();
            entity.AirDate = model.AirDate;
            entity.AirNo = model.AirNo;
            entity.AirIssueDate = model.AirIssueDate;
            entity.Days = model.Days;
            entity.TranType = model.TranType;
            entity.Unit = model.Unit;
            entity.UnitCost = model.UnitCost;
            entity.Remarks = model.Remarks;
            entity.DeptId = model.DeptId;
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
            entity.ProRatedCost = model.ProRatedCost;
            entity.OtherQty = model.OtherQty;

            entity.Qty = model.Qty;
            entity.QtyIss = model.QtyIss;
            entity.QtyBal = model.QtyBal;
            entity.TransferIn = model.TransferIn;
            entity.TransferOut = model.TransferOut;
            entity.Amount = model.Amount;
            entity.PriceRate = model.PriceRate;
            entity.AddCost = model.AddCost;
            entity.TUnitCost = model.TUnitCost;
            entity.GTotalCost = model.TUnitCost * model.QtyBal;

            entity.PpmpCode = model.PpmpCode;
            entity.PpmpItemId = model.PpmpItemId;

            if (isAdmin)
            {                
                if (string.IsNullOrWhiteSpace(model.Consumable))
                {
                    entity.IsConsumable = null;
                }
                else
                {
                    model.Consumable = model.Consumable.ToUpper();
                    entity.IsConsumable = model.Consumable == "Y" ? true : false;
                }
            }
            else
            {
                if (mode == Mode.ADD)
                {
                    var psCard = _db.PsCards.Include(i => i.ItemCode).FirstOrDefault(f => f.Id == model.PsCardId);
                    entity.IsConsumable = _itemCodeService.GetIsConsumable(psCard.ItemCode.IsConsumable);
                    model.Consumable = entity.IsConsumable.HasValue ? (entity.IsConsumable == true ? "Y" : "N") : "";
                }
            }

            var psCardItemTransfer = new PsCardItemTransfer()
            {
                Id = (Guid)model.TransferId,
                PsCardItemId = model.Id,
                Qty = model.Qty,
                QtyIss = model.QtyIss,
                QtyBal = model.QtyBal,
                TransDate = model.PoDate, // original entry, use PO Date (No transit yet)
                TransferIn = model.TransferIn,
                TransferOut = model.TransferOut,
                Amount = model.TUnitCost * model.QtyBal,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            entity.PsCardItemTransfers.Add(psCardItemTransfer);
        }

        public virtual ValueTask<PsCardItem> PostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.PsCardItems.FindAsync(id);
            ValidateRecord(entity);
            ValidateIfPosted(entity);

            if ((entity.Qty ?? 0) <= 0)
            {
                throw new InvalidOperationException("Cannot post an acquisition with zero or negative quantity.");
            }
            if ((entity.QtyBal ?? 0) < 0)
            {
                throw new InvalidOperationException("Cannot post an acquisition with a negative balance.");
            }
            if ((entity.QtyIss ?? 0) > (entity.Qty ?? 0))
            {
                throw new InvalidOperationException("Issued quantity cannot exceed received quantity.");
            }

            entity.PostedBy = user;
            entity.PostedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            await _db.SaveChangesAsync();

            return entity;
        });

        public virtual ValueTask<PsCardItem> UnpostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.PsCardItems.FindAsync(id);
            ValidateRecord(entity);
            ValidateIfNotPosted(entity);

            // AIR-generated acquisition protection: AIR owns the source lifecycle.
            if (entity.AIRItemId != null || !string.IsNullOrWhiteSpace(entity.AirNo) || (entity.OrderItemRequestId != null && entity.TranType == "A"))
            {
                throw new InvalidOperationException("This acquisition was generated from an AIR inspection and cannot be independently unposted from Property Card. Source reversal must be performed through the AIR module.");
            }

            // Check if any physical units belonging to this acquisition have ICS/PAR accountability records
            var icsPars = await _db.IcsParItems.AsNoTracking()
                .Where(x => x.PsCardItemExtn.PsCardItemId == id)
                .Select(x => (x.IcsPar.RefType ?? "PAR/ICS") + " No. " + x.IcsPar.RefNo)
                .Distinct()
                .ToListAsync();
            if (icsPars.Any())
            {
                throw new InvalidOperationException($"This acquisition cannot be unposted because one or more units already have accountability records ({string.Join(", ", icsPars)}).");
            }

            // Check if any components belonging to this acquisition have ICS/PAR accountability records
            var compIcsPars = await _db.IcsParItemComponents.AsNoTracking()
                .Where(x => (x.PsCardSubItem.PsCardItemId == id || x.PsCardItemExtn.PsCardItemId == id))
                .Select(x => (x.IcsParItem.IcsPar.RefType ?? "PAR/ICS") + " No. " + x.IcsParItem.IcsPar.RefNo)
                .Distinct()
                .ToListAsync();
            if (compIcsPars.Any())
            {
                throw new InvalidOperationException($"This acquisition cannot be unposted because components are assigned to accountability records ({string.Join(", ", compIcsPars)}).");
            }

            // Check if any transfers or issuances exist
            if (await _db.PsCardItemTransfers.AnyAsync(a => a.PsCardItemId == id && a.ParentId != null))
            {
                throw new InvalidOperationException("This acquisition has been transferred to another department/location and cannot be unposted.");
            }
            if (await _db.PsCardItemTransferIssuances.AnyAsync(a => a.PsCardItemTransfer.PsCardItemId == id))
            {
                throw new InvalidOperationException("This acquisition has issued items and cannot be unposted.");
            }
            if ((entity.QtyIss ?? 0) > 0)
            {
                throw new InvalidOperationException("This acquisition has issued quantity and cannot be unposted.");
            }

            entity.PostedBy = "";
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            await _db.SaveChangesAsync();

            return entity;
        });

        private void ValidatePoDate(DateTime? poItemTransDate)
        {
            //var issuanceYears = _codextnService.GetIssuanceYears().Where(w => (w.Desc3 != "Y" && w.Desc3 != "y"));
            //if (issuanceYears.Any())
            //{
            //    var minYear = int.Parse(issuanceYears.Min(m => m.Description));
            //    var maxYear = int.Parse(issuanceYears.Max(m => m.Description));

            //    if (poItemTransDate.Value.Year < minYear)
            //    {
            //        if (minYear == maxYear)
            //        {
            //            throw new InvalidValueException($"Update for this year is not allowed.");
            //        }
            //        else
            //        {
            //            throw new InvalidValueException($"Update for this year is not allowed.");
            //        }
            //    }

            //    var year = poItemTransDate.Value.Year.ToString().Trim();
            //    var issuanceYear = issuanceYears.FirstOrDefault(f => f.Description == year);
            //    if (issuanceYear == null)
            //    {
            //        throw new InvalidValueException($"Update for this year is not allowed.");
            //    }
            //    else
            //    {
            //        if (!string.IsNullOrWhiteSpace(issuanceYear.Desc2))
            //        {
            //            var cutOffDate = DateTime.Parse(issuanceYear.Desc2);
            //            if (updatedDt > cutOffDate)
            //            {
            //                throw new InvalidValueException($"Update for this year is only valid until {cutOffDate.ToShortDateString()}.");
            //            }
            //        }
            //    }
            //}
            //else
            //{
            //    throw new InvalidValueException($"Issuance year setup is not a available.");
            //}

            var poYears = _codextnService.GetPoYears();
            var poYear = poItemTransDate.Value.Year.ToString().Trim();
            poYears = poYears.Where(w => w.Description == poYear && w.Desc2 != "Y");
            if (!poYears.Any())
            {
                throw new InvalidValueException($"Update for this year is not allowed.");
            }
        }

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
            var entity = _db.PsCardItems.Find(model.Id);
            if (entity != null && entity.PostedDt != null)
            {
                throw new InvalidOperationException("Cannot delete a posted acquisition. Unpost it first.");
            }

            if (entity != null && (entity.AIRItemId != null || !string.IsNullOrWhiteSpace(entity.AirNo) || (entity.OrderItemRequestId != null && entity.TranType == "A")))
            {
                throw new RecordRelationshipException("This acquisition was generated from an AIR inspection and cannot be deleted from Property Card. Source reversal must be performed through the AIR module.");
            }

            if (_db.PsCardItemExtns.Any(x => x.PsCardItemId == model.Id && x.PsCardSubItemId == null))
            {
                throw new RecordRelationshipException("Cannot delete this acquisition because it has main physical units. Please remove the physical units first.");
            }

            if (_db.PsCardSubItems.Any(x => x.PsCardItemId == model.Id))
            {
                throw new RecordRelationshipException("Cannot delete this acquisition because it has components defined. Please remove the components first.");
            }

            if (_db.IcsParItems.Any(x => x.PsCardItemExtn.PsCardItemId == model.Id))
            {
                throw new RecordRelationshipException("Cannot delete this acquisition because one or more units have accountability records.");
            }

            if (_db.IcsParItemComponents.Any(x => x.PsCardSubItem.PsCardItemId == model.Id || x.PsCardItemExtn.PsCardItemId == model.Id))
            {
                throw new RecordRelationshipException("Cannot delete this acquisition because components are assigned to accountability records.");
            }

            if (_db.PsCardItemTransferIssuances.Any(a => a.PsCardItemTransferId == model.TransferId))
            {
                throw new RecordRelationshipException("Issuance already exists, cannot delete!");
            }

            if (_db.PsCardItemTransfers.Any(a => a.ParentId == model.TransferId))
            {
                throw new RecordRelationshipException("Transit already exists, cannot delete!");
            }
        }
    }
}