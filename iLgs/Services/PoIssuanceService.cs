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
    public class PoIssuanceService : IPoIssuanceService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private IUserService _userService;
        private readonly IExceptionService<RisIssuedVM> _vmExceptionService = new ExceptionService<RisIssuedVM>();

        public PoIssuanceService(AppManEntities db)
        {
            this._db = db;
            this._userService = new UserService(db);
        }

        public async ValueTask<IQueryable<PoIssuanceVM>> GetAllAsync(string userId)
        {
            var IsAdmin = await _userService.IsAdmin(userId);
            var data = _db.PsCardItems.AsNoTracking()
                .Where(w => (IsAdmin || _db.Codextns.Any(x => x.CodeMast.Code == "DEPARTMENTS" && x.Id == w.DeptId
                    && x.DepartmentUsers.Any(a => a.UserId == userId)))
                ).Select(s => new PoIssuanceVM
                {
                    Id = s.Id,
                    PoNo = s.PoNo,
                    PoDate = s.PoDate,
                    OrderItemId = s.OrderItemId,
                    AirNo = s.AirNo,
                    AirDate = s.AirDate,
                    Department = s.Codextn.Description,
                    PrNo = s.OrderItem.RequestItem.Request.PrNo,
                    RisNo = s.OrderItem.RequestItem.RisItem.RISs.RisNo,
                    Qty = s.Qty,
                    QtyIss = s.QtyIss,
                    Balance = s.QtyBal,
                    UnitCost = s.UnitCost,
                    StockNo = s.PsCard.PsNo,
                    ItemName = s.PsCard.Description
                }).AsQueryable();           
            return data;
        }

        public async ValueTask<PoIssuanceVM> GetByIdAsync(Guid? id)
        {
            var data = await _db.PsCardItems.AsNoTracking()
                .Where(w => w.Id == id)
                .Select(s => new PoIssuanceVM
                {
                    Id = s.Id,
                    PoNo = s.PoNo,
                    PoDate = s.PoDate,
                    OrderItemId = s.OrderItemId,
                    AirNo = s.AirNo,
                    AirDate = s.AirDate,
                    Department = s.Codextn.Description,
                    PrNo = s.OrderItem.RequestItem.Request.PrNo,
                    RisNo = s.OrderItem.RequestItem.RisItem.RISs.RisNo,
                    Qty = s.Qty,
                    QtyIss = s.QtyIss,
                    Balance = s.QtyBal,
                    UnitCost = s.UnitCost,
                    StockNo = s.PsCard.PsNo,
                    ItemName = s.PsCard.Description
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

        //public ValueTask<RisIssuedVM> GeneratePAR(RisIssuedVM model, string user, DateTime date) =>
        //_vmExceptionService.TryCatchAsync(async () =>
        // {

        //     var cardItem = await GetByIdAsync(model.Id);

        //     if (cardItem == null)
        //     {
        //         throw new RecordNotFoundException((Guid)model.Id);
        //     }

        //     if (cardItem.Balance == 0)
        //     {
        //         throw new InvalidValueException(string.Format("All Items weree already issued."));
        //     }

        //     if (model.Qty > cardItem.Balance)
        //     {
        //         throw new InvalidValueException(string.Format("Cannot generate more than the available balance."));
        //     }

        //     var refType = (await _db.RisIssueds.Where(w => w.OrderItemId == model.OrderItemId && w.RefType != model.RefType).FirstOrDefaultAsync())?.RefType;
        //     if (!string.IsNullOrWhiteSpace(refType))
        //     {
        //         throw new InvalidValueException(string.Format("{0} already exists, cannot add {1} as new reference type.", refType, model.RefType));
        //     }
        //     else
        //     {
        //         var nulRefType = await _db.RisIssueds.Where(w => w.OrderItemId == model.OrderItemId && (w.RefType == "" || w.RefType == null)).AnyAsync();
        //         if (nulRefType) {
        //             throw new InvalidValueException(string.Format("Issued item already exists, cannot add {0} as new reference type.", model.RefType));
        //         }
        //     }

        //     // generate par per qty
        //     for (var qty = 0; qty < model.Qty; ++qty)
        //     {                 
        //         model.RefNo = await NextRefNoAsync((DateTime)model.RefDate, model.RefType);
        //         model.PropNo = NextPropNo(cardItem.AirDate.Value.Year.ToString(), cardItem.StockNo, model.LocationCode);
        //         var entity = new RisIssued()
        //         {
        //             Id = Guid.NewGuid(),
        //             RisItemId = cardItem.RisItemId,
        //             OrderItemId = cardItem.OrderItemId,
        //             LocationId = model.LocationId,
        //             OfficerId = model.OfficerId,
        //             IssuedTo = model.IssuedTo,
        //             IssuedToPosition = model.IssuedToPosition,
        //             IssuedDate = model.IssuedDate,
        //             IssuedBy = model.IssuedBy,
        //             IssuedByDate = model.IssuedByDate,
        //             IssuedByPosition = model.IssuedByPosition,
        //             Qty = 1,
        //             Amount = cardItem.UnitCost,
        //             RefNo = model.RefNo,
        //             RefDate = model.RefDate,
        //             RefType = model.RefType,
        //             PostedBy = model.PostedBy,
        //             PostedDt = model.PostedDt,
        //             PropNo = model.PropNo,
        //             InsertedBy = user,
        //             InsertedDt = date,
        //             UpdatedBy = user,
        //             UpdatedDt = date
        //         };

        //         _db.RisIssueds.Add(entity);
        //         _db.Entry(entity).State = EntityState.Added;
        //         await _db.SaveChangesAsync();
        //     }

        //     return model;
        // });

        private string NextPropNo(string acqYear, string stockNo, string locationCode)
        {
            var propNo = _db.Database.SqlQuery<string>("Exec PoIssuance_GetNextSeqNo {0}, {1}, {2}", acqYear, stockNo, locationCode).ToList();
            return propNo.LastOrDefault();
        }

        //private async ValueTask<string> NextRefNoAsync(DateTime parDate, string refType)
        //{
        //    string yyyy = parDate.Year.ToString().Trim();
        //    string mm = parDate.Month.ToString().Trim();

        //    mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

        //    string keyName = yyyy + "-" + mm;
        //    // yyyy-mm-9999
        //    // 123456789012

        //    var data = await _db.RisIssueds.Where(w => w.RefType == refType && w.RefDate.Value.Year == parDate.Year).OrderByDescending(o => o.RefNo).FirstOrDefaultAsync();
        //    if (data == null)
        //    {
        //        return keyName + "-" + "0001";
        //    }
        //    else
        //    {
        //        var sequence = (int.Parse(data.RefNo.Split('-')[2]) + 1).ToString();
        //        return keyName + "-" + sequence.PadLeft(4, '0');
        //    }
        //}

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