using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.PropertyCard;
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace iLgs.Services.Interfaces
{
    public interface IPoIssuanceService
    {
        ValueTask<IQueryable<PsCardItemVM>> GetAllAsync(string userId);
        ValueTask<PsCardItemVM> GetByIdAsync(Guid? id);
        IQueryable<PsCardItemVM> GetSummary();
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
        public PoIssuanceService(AppManEntities db)
        {
            _db = db;
            _userService = new UserService(_db);
            _psCardItemTransactionService = new PsCardItemTransactionService(_db);
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
            //var data = _db.PsCardItems.AsNoTracking()
            //    .Include(i => i.Codextn)
            //    .Include(i => i.Codextn1)
            //    .Where(w => (IsAdmin || _db.Codextns.Any(x => x.CodeMast.Code == "LOCATIONS" && x.Id == w.DeptId
            //        && x.DepartmentUsers.Any(a => a.UserId == userId)))
            //    ).Select(GetPsCardItemProjection(_db)).AsQueryable();

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

        public ValueTask<PsCardItemVM> TransferAsync(PsCardItemVM model, string user, DateTime date) =>
        _psCardItemVMrExceptionService.TryCatch(async () =>
        {
            string[] selectedIds = null;
            var psCardItem = await _db.PsCardItems.FindAsync(model.Id);
            if (model.IsWithItemExtn == true)
            {
                if (model.SelectedIds == null)
                {
                    throw new InvalidValueException(string.Format("No selected items, cannot transit!"));
                }

                selectedIds = model.SelectedIds.Split(',');
                if (selectedIds.Count() == 0)
                {
                    throw new InvalidValueException(string.Format("No selected items, cannot transit!"));
                }

                model.TransferOut = selectedIds.Count();
            }
            else
            {
                if (!model.TransferOut.HasValue)
                {
                    throw new InvalidValueException("Transit out is required!");
                }
            }

            if (model.LocationId == null)
            {
                throw new InvalidValueException("Location is required!");
            }
            if (model.TransferOut > psCardItem.QtyBal)
            {
                throw new InvalidValueException("Transit out must not be greater than the balance!");
            }
            if (!model.TransDate.HasValue)
            {
                throw new InvalidValueException("Transit date is required!");
            }

            //using (var transaction = _db.Database.BeginTransaction())
            //{
            //    try
            //    {
                    var psCardItemTransfer = new PsCardItemTransfer()
                    {
                        Id = Guid.NewGuid(),
                        PsCardItemId = model.Id,
                        Qty = model.TransferOut,
                        TransDate = model.TransDate,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };

                    _db.PsCardItemTransfers.Add(psCardItemTransfer);
                    await _db.SaveChangesAsync();

                    var entity = new PsCardItem()
                    {
                        Id = Guid.NewGuid(),
                        //GroupId = model.Id,
                        GroupId = model.GroupId,
                        PsCardId = model.PsCardId,
                        OrderItemId = model.OrderItemId,
                        TransferRefId = psCardItemTransfer.Id,
                        PoNo = model.PoNo,
                        PoDate = model.PoDate,
                        AirNo = model.AirNo,
                        AirDate = model.AirDate,
                        AirIssueDate = model.AirIssueDate,
                        Qty = 0,
                        QtyBal = model.TransferOut,
                        QtyIss = 0,
                        TransferIn = model.TransferOut,
                        TransferOut = 0,
                        TranType = model.TranType,
                        Days = model.Days,
                        Unit = model.Unit,
                        UnitCost = model.UnitCost,
                        Amount = model.UnitCost * model.TransferOut,
                        Remarks = model.Remarks,
                        DeptId = model.DeptId,
                        LocationId = model.LocationId,
                        DeptDisplay = model.DeptDisplay,
                        Description = model.Description,
                        OtherDesc = model.OtherDesc,
                        IsForICS = model.IsForICS,
                        IsConsumable = model.IsConsumable,
                        IsIncorporated = model.IsIncorporated,
                        IsOthers = model.IsOthers,
                        OtherRemarks = model.OtherRemarks,
                        Type = model.Type,
                        InvDist = model.InvDist,
                        Vendor = model.Vendor,
                        PrevPsNo = model.PrevPsNo,
                        FPP = model.FPP,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };

                    _db.PsCardItems.Add(entity);
                    await _db.SaveChangesAsync();

                    var transferOuts = (_db.PsCardItemTransfers.Where(w => w.PsCardItemId == model.Id).Sum(s => s.Qty)) ?? 0;

                    var qtyBal = ((model.Qty ?? 0) + (model.TransferIn ?? 0)) - ((model.QtyIss ?? 0) + transferOuts);
                    psCardItem.TransferOut = transferOuts;
                    psCardItem.QtyBal = qtyBal;
                    psCardItem.Amount = qtyBal * model.UnitCost;
                    psCardItem.UpdatedBy = user;
                    psCardItem.UpdatedDt = date;
                    _db.PsCardItems.Attach(psCardItem);
                    _db.Entry(psCardItem).State = EntityState.Modified;
                    await _db.SaveChangesAsync();

                    if (selectedIds != null)
                    {
                        foreach (var selectedId in selectedIds)
                        {
                            var psCardItemExtn = await _db.PsCardItemExtns.FindAsync(Guid.Parse(selectedId));
                            psCardItemExtn.PsCardItemId = entity.Id;
                            psCardItemExtn.UpdatedBy = user;
                            psCardItemExtn.UpdatedDt = date;
                            await _db.SaveChangesAsync();

                            //await _psCardItemTransactionService.LogUpdates(psCardItemExtn.Id, psCardItemTransfer.Id, "TRANSIT", user, date);
                            await _psCardItemTransactionService.LogUpdates(psCardItemExtn.Id, entity.Id, "TRANSIT", user, date);
                        }                        
                    }
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

    }
}