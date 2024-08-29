using iLgs.Controllers;
using iLgs.Exceptions;
using iLgs.Exceptions.PARs;
using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.Interfaces
{
    public interface IPoIssuanceService
    {
        ValueTask<IQueryable<PsCardItemVM>> GetAllAsync(string userId);
        ValueTask<PsCardItemVM> GetByIdAsync(Guid? id);
        
        ValueTask PostAsync(Guid psCardItemIssuanceId, string user, DateTime date);
        ValueTask UnpostAsync(Guid psCardItemIssuanceId, string user, DateTime date);
        ValueTask<PsCardItemVM> TransferAsync(PsCardItemVM model, string user, DateTime date);
    }

    public class PoIssuanceService : IPoIssuanceService
    {
        private decimal _parPrice = 50000;
        private readonly AppManEntities _db = new AppManEntities();
        private IUserService _userService;
        private readonly IExceptionService<RisIssuedVM> _vmExceptionService = new ExceptionService<RisIssuedVM>();        
        private readonly IExceptionService<PsCardItemVM> _psCardItemVMrExceptionService = new ExceptionService<PsCardItemVM>();

        public PoIssuanceService(AppManEntities db)
        {
            this._db = db;
            this._userService = new UserService(db);
        }

        public async ValueTask<IQueryable<PsCardItemVM>> GetAllAsync(string userId)
        {
            var IsAdmin = await _userService.IsAdmin(userId);
            var data = _db.PsCardItems.AsNoTracking()
                .Include(i => i.Codextn)
                .Include(i => i.Codextn1)
                .Where(w => (IsAdmin || _db.Codextns.Any(x => x.CodeMast.Code == "DEPARTMENTS" && x.Id == w.DeptId
                    && x.DepartmentUsers.Any(a => a.UserId == userId)))
                ).Select(s => new PsCardItemVM
                {
                    Id = s.Id,
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
                    //Department = s.Codextn.Description,
                    Article = s.PsCard.ItemCode.Description,
                    Description = s.Description,
                    DeptDisplay = s.DeptDisplay,
                    //LocCode = s.Codextn1.Code,
                    //Location = s.Codextn1.Description,
                    StockNo = s.PsCard.PsNo,
                    ParBalance = s.QtyBal - (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.Id == s.Id && w.IcsPar.RefType == "P").Sum(x => x.Qty) ?? 0),
                    //(s.IcsParItems.Where(w => w.IcsPar.RefType == "P").Sum(x => x.Qty) ?? 0),
                    IcsBalance = s.QtyBal - (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.Id == s.Id && w.IcsPar.RefType == "I").Sum(x => x.Qty) ?? 0),
                    //(s.IcsParItems.Where(w => w.IcsPar.RefType == "I").Sum(x => x.Qty) ?? 0),
                    RemBalance = s.QtyBal,
                    Department = s.Codextn.Description,
                    Location = s.Codextn1.Description,
                    LocCode = s.Codextn1.Code
                    //_Deparment = s.Codextn,
                    //_Location = s.Codextn1
                }).AsQueryable();           
            return data;
        }

        public async ValueTask<PsCardItemVM> GetByIdAsync(Guid? id)
        {
            var data = await _db.PsCardItems
                .Include(i => i.Codextn)
                .Include(i => i.Codextn1)
                .Where(w => w.Id == id)
                .Select(s => new PsCardItemVM
                {
                    Id = s.Id,
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
                    //Department = s.Codextn.Description,
                    Article = s.PsCard.ItemCode.Description,
                    Description = s.Description,
                    DeptDisplay = s.DeptDisplay,
                    //LocCode = s.Codextn1.Code,
                    //Location = s.Codextn1.Description,
                    StockNo = s.PsCard.PsNo,
                    ParBalance = s.QtyBal - (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.Id == s.Id && w.IcsPar.RefType == "P").Sum(x => x.Qty) ?? 0),
                    //(s.IcsParItems.Where(w => w.IcsPar.RefType == "P").Sum(x => x.Qty) ?? 0),
                    IcsBalance = s.QtyBal - (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.Id == s.Id && w.IcsPar.RefType == "I").Sum(x => x.Qty) ?? 0),
                    //(s.IcsParItems.Where(w => w.IcsPar.RefType == "I").Sum(x => x.Qty) ?? 0),
                    RemBalance = s.QtyBal,
                    Department = s.Codextn.Description,
                    Location = s.Codextn1.Description,
                    LocCode = s.Codextn1.Code
                }).FirstOrDefaultAsync();
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

        //public async ValueTask<IQueryable<PoIssuanceVM>> GetAllPostedAirAsync(string userId)
        //{
        //    var IsAdmin = await _userService.IsAdmin(userId);
        //    var data = _db.AIRItems.AsNoTracking()
        //            .Where(w => w.AIR.PostedDt != null
        //                && (IsAdmin ||
        //                    _db.Codextns.Any(x => x.CodeMast.Code == "DEPARTMENTS"
        //                    && x.Description == w.AIR.Order.Request.RISs.Office
        //                    && x.DepartmentUsers.Any(a => a.UserId == userId))
        //                )
        //            )
        //            .Select(s => new PoIssuanceVM
        //            {
        //                Id = s.Id,
        //                AirNo = s.AIR.AIRNo,
        //                AirDate = s.AIR.AIRDate,
        //                OrderItemId = s.OrderItemId,
        //                RisItemId = s.OrderItem.RequestItem.RisItemId,
        //                Department = s.AIR.Order.Request.RISs.Office,
        //                PoNo = s.AIR.Order.PoNo,
        //                PoDate = s.AIR.Order.PoDate,
        //                PrNo = s.AIR.Order.Request.PrNo,
        //                RisNo = s.AIR.Order.Request.RISs.RisNo,
        //                Qty = (int?)s.Qty, // Qty of AIR not PO
        //                QtyIss = s.OrderItem.RequestItem.RisItem.RisIssueds.Sum(x => x.Qty) ?? 0,
        //                Balance = (int?)s.Qty - (s.OrderItem.RequestItem.RisItem.RisIssueds.Sum(x => x.Qty) ?? 0),
        //                StockNo = s.OrderItem.StockNo,
        //                StockName = s.OrderItem.StockName,
        //                UnitCost = s.OrderItem.UnitCost,
        //                ItemName = s.OrderItem.RequestItem.RisItem.ItemName,
        //                Description = s.OrderItem.Description,
        //                IsProperty = s.OrderItem.UnitCost >= 50000
        //                //Brand = s.OrderItem.Brand
        //            })
        //            .AsQueryable();
        //    return data;
        //}
        //public async ValueTask<PoIssuanceVM> GetOrderItemByAirItemIdAsync(Guid? airItemId)
        //{
        //    var data = await _db.AIRItems.Where(w => w.Id == airItemId).AsNoTracking()
        //            .Select(s => new PoIssuanceVM
        //            {
        //                Id = s.Id,
        //                AirNo = s.AIR.AIRNo,
        //                AirDate = s.AIR.AIRDate,
        //                OrderItemId = s.OrderItemId,
        //                RisItemId = s.OrderItem.RequestItem.RisItemId,
        //                Department = s.AIR.Order.Request.RISs.Office,
        //                PoNo = s.AIR.Order.PoNo,
        //                PoDate = s.AIR.Order.PoDate,
        //                PrNo = s.AIR.Order.Request.PrNo,
        //                RisNo = s.AIR.Order.Request.RISs.RisNo,
        //                Qty = (int?)s.Qty, // Qty of AIR not PO
        //                QtyIss = s.OrderItem.RequestItem.RisItem.RisIssueds.Sum(x => x.Qty) ?? 0,
        //                Balance = (int?)s.Qty - (s.OrderItem.RequestItem.RisItem.RisIssueds.Sum(x => x.Qty) ?? 0),
        //                StockNo = s.OrderItem.StockNo,
        //                StockName = s.OrderItem.StockName,
        //                UnitCost = s.OrderItem.UnitCost,
        //                ItemName = s.OrderItem.RequestItem.RisItem.ItemName,
        //                Description = s.OrderItem.Description,
        //                IsProperty = s.OrderItem.UnitCost >= 50000
        //                //Brand = s.Brand
        //            }).FirstOrDefaultAsync();
        //    return data;
        //}

        private string RefTypeDesc(string refType)
        {
            return refType == "P" ? "PAR" : "ICS";
        }        

        public ValueTask<PsCardItemVM> TransferAsync(PsCardItemVM model, string user, DateTime date) =>
        _psCardItemVMrExceptionService.TryCatchAsync(async () =>
        {
            var psCardItem = await _db.PsCardItems.FindAsync(model.Id);
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
            if (!model.TransDate.HasValue)
            {
                throw new InvalidValueException("Transit date is required!");
            }
            
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
                GroupId = model.Id,
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

            return model;
        });

        //public async ValueTask PostAsync(Guid risIssuedId, string user, DateTime date)
        //{
        //    var entity = await _db.RisIssueds.Include(i => i.OrderItem.RequestItem.RisItem.RISs).Where(w => w.Id == risIssuedId).FirstOrDefaultAsync();
        //    if (entity == null)
        //    {
        //        throw new RecordNotFoundException(risIssuedId);
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

        //    if (!string.IsNullOrWhiteSpace(entity.RefType))
        //    {
        //        if (entity.OfficerId == null)
        //        {
        //            throw new InvalidValueException("Accountable Officer is Required!");
        //        }

        //        if (entity.RefDate == null)
        //        {
        //            throw new InvalidValueException("Reference Date is Required!");
        //        }

        //        if (string.IsNullOrWhiteSpace(entity.RefNo))
        //        {
        //            throw new InvalidValueException("Reference Number is Required!");
        //        }

        //        if (string.IsNullOrWhiteSpace(entity.PropNo))
        //        {
        //            throw new InvalidValueException("Property Number is Required!");
        //        }
        //    }            

        //    //var office = entity.OrderItem.RequestItem.RisItem.RISs.Office;
        //    //var deptId = _db.Codextns.Where(w => w.CodeMast.Code == "DEPARTMENTS" && w.Description.Trim() == office).FirstOrDefault()?.Id;

        //    var psCardItem = await _db.PsCardItems.Include(i => i.PsCardItemIssuances).Where(w => w.OrderItemId == entity.OrderItemId).FirstOrDefaultAsync();
        //    if (psCardItem == null)
        //    {
        //        throw new RecordRelationshipException("Property/Stock Card not found. Please repost AIR before proceeding!");
        //    }

        //    var IsNew = false;
        //    var psCardItemIssuance = await _db.PsCardItemIssuances.Where(w => w.PsCardItemId == psCardItem.Id && w.RefIssuedId == entity.Id).FirstOrDefaultAsync();
        //    if (psCardItemIssuance == null)
        //    {
        //        psCardItemIssuance = new PsCardItemIssuance()
        //        {
        //            Id = Guid.NewGuid(),                    
        //            InsertedBy = user,
        //            InsertedDt = date                    
        //        };
        //        IsNew = true;
        //    }

        //    psCardItemIssuance.PsCardItemId = psCardItem.Id;
        //    psCardItemIssuance.RefIssuedId = entity.Id;
        //    psCardItemIssuance.OfficerId = entity.OfficerId;
        //    psCardItemIssuance.LocationId = entity.LocationId;
        //    psCardItemIssuance.IssuedTo = entity.IssuedTo;
        //    psCardItemIssuance.IssuedDate = entity.IssuedDate;
        //    psCardItemIssuance.Qty = entity.Qty;
        //    psCardItemIssuance.Amount = entity.Amount;
        //    psCardItemIssuance.RefNo = entity.RefNo;
        //    psCardItemIssuance.RefDate = entity.RefDate;
        //    psCardItemIssuance.RefType = entity.RefType;
        //    psCardItemIssuance.PropNo = entity.PropNo;
        //    psCardItemIssuance.UpdatedBy = user;
        //    psCardItemIssuance.UpdatedDt = date;

        //    if (IsNew)
        //    {
        //        _db.PsCardItemIssuances.Add(psCardItemIssuance);
        //        _db.Entry(psCardItemIssuance).State = EntityState.Added;
        //    }
        //    else
        //    {
        //        _db.PsCardItemIssuances.Attach(psCardItemIssuance);
        //        _db.Entry(psCardItemIssuance).State = EntityState.Modified;
        //    }

        //    entity.PostedBy = user;
        //    entity.PostedDt = date;

        //    _db.RisIssueds.Attach(entity);
        //    _db.Entry(entity).State = EntityState.Modified;
        //    await _db.SaveChangesAsync();

        //    await UpdatePsCardItemAsync(psCardItem, user, date);
        //}

        //public async ValueTask UnpostAsync(Guid risIssuedId, string user, DateTime date)
        //{
        //    var entity = await _db.RisIssueds.FindAsync(risIssuedId);

        //    if (entity == null)
        //    {
        //        throw new RecordNotFoundException(risIssuedId);
        //    }

        //    if (string.IsNullOrWhiteSpace(entity.PostedBy))
        //    {
        //        throw new RecordNotYetPostedException(string.Format("Record is not yet posted.."));
        //    }

        //    var psCardItem = await _db.PsCardItems.Include(i => i.PsCardItemIssuances).Where(w => w.OrderItemId == entity.OrderItemId).FirstOrDefaultAsync();
        //    if (psCardItem == null)
        //    {
        //        throw new RecordRelationshipException("Property/Stock Card not found. Please repost AIR before proceeding!");
        //    }

        //    var psCardItemIssuance = await _db.PsCardItemIssuances.Where(w => w.PsCardItemId == psCardItem.Id && w.RefIssuedId == entity.Id).FirstOrDefaultAsync();

        //    _db.PsCardItemIssuances.Remove(psCardItemIssuance);
        //    _db.Entry(psCardItemIssuance).State = EntityState.Deleted;

        //    entity.PostedBy = null;
        //    entity.PostedDt = null;
        //    entity.UpdatedBy = user;
        //    entity.UpdatedDt = date;

        //    _db.RisIssueds.Attach(entity);
        //    _db.Entry(entity).State = EntityState.Modified;
        //    await _db.SaveChangesAsync();

        //    await UpdatePsCardItemAsync(psCardItem, user, date);
        //}

        //private async ValueTask UpdatePsCardItemAsync(PsCardItem psCardItem, string user, DateTime date)
        //{
        //    psCardItem.QtyIss = psCardItem.PsCardItemIssuances.Sum(s => s.Qty) ?? 0;
        //    psCardItem.QtyBal = psCardItem.Qty - psCardItem.QtyIss;
        //    psCardItem.UpdatedBy = user;
        //    psCardItem.UpdatedDt = date;

        //    _db.PsCardItems.Attach(psCardItem);
        //    _db.Entry(psCardItem).State = EntityState.Modified;
        //    await _db.SaveChangesAsync();
        //}
    }
}