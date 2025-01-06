using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using iLgs.Services.PurchaseOrder;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services
{
    public interface IRpciItemService
    {
        IQueryable<RPCIItem> GetByRpciId(Guid? rpciId);
        IQueryable<RPCIItemVM> GetVmByRpciId(Guid? rpciId);
        ValueTask<RPCIItem> GetByIdAsync(Guid? id);
        ValueTask<RPCIItemVM> CreateAsync(RPCIItemVM model, string user, DateTime date);
        ValueTask<RPCIItemVM> UpdateAsync(RPCIItemVM model, string user, DateTime date);
        ValueTask<RPCIItemVM> DeleteAsync(RPCIItemVM model, string user, DateTime date);
    }

    public class RpciItemService : IRpciItemService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RPCIItem> _exceptionService = new ExceptionService<RPCIItem>();
        private readonly IExceptionService<RPCIItemVM> _vmExceptionService = new ExceptionService<RPCIItemVM>();
        private readonly IOrderService _orderService;

        public RpciItemService(AppManEntities db)
        {
            _db = db;
            _orderService = new OrderService(_db);
        }

        public ValueTask<RPCIItem> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.RPCIItems.FindAsync(id);
            return data;
        });

        public IQueryable<RPCIItem> GetByRpciId(Guid? rpciId) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.RPCIItems.Where(w => w.RpciId == rpciId);
            return data;
        });

        public IQueryable<RPCIItemVM> GetVmByRpciId(Guid? rpciId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RPCIItems.Where(w => w.RpciId == rpciId).AsNoTracking()
                .Select(s => new RPCIItemVM
                {
                    Id = s.Id,
                    RpciId = s.RpciId,
                    ItemCodeId = s.ItemCodeId,
                    Article = s.Article,
                    PoNo = s.PoNo,
                    PoDate = s.PoDate,
                    AirNo = s.AirNo,
                    AirDate = s.AirDate,
                    UnitCost = s.UnitCost,
                    Unit = s.Unit,
                    Qty = s.Qty,
                    LocationId = s.LocationId,
                    LocationCode = s.LocationCode,
                    LocationName = s.LocationName,
                    TransferIn = s.TransferIn,
                    TransferOut = s.TransferOut,
                    QtyIss = s.QtyIss,
                    QtyInBalance = s.QtyInBalance,
                    TransferInBalance = s.TransferInBalance,
                    TotalBalance = s.TotalBalance,
                    AcqCost = s.AcqCost,
                    OldStockNo = s.OldStockNo,
                    StockNo = s.StockNo,
                    Brand = s.Brand,
                    Model_ = s.Model_,
                    SerialNo = s.SerialNo,
                    Description = s.Description,
                    OtherDesc = s.OtherDesc,
                    Remarks = s.Remarks,
                    InsertedDt = s.InsertedDt,
                    Department = s.Department
                });

            return data;
        });


        private void ValidatePost(Guid? rpciId)
        {
            var data = _db.RPCIs.Find(rpciId);
            if (data == null)
            {
                throw new InvalidValueException("Record no longer exists!");
            }

            if (!string.IsNullOrWhiteSpace(data.PostedBy))
            {
                throw new RecordAlreadyPostedException($"Record Already Posted by {data.PostedBy}, cannot updaet!");
            }
        }

        public ValueTask<RPCIItemVM> CreateAsync(RPCIItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidatePost(model.RpciId);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new RPCIItem()
            {
                Id = model.Id,
                RpciId = model.RpciId,
                ItemCodeId = model.ItemCodeId,
                Article = model.Article,
                PoNo = model.PoNo,
                PoDate = model.PoDate,
                AirNo = model.AirNo,
                AirDate = model.AirDate,
                UnitCost = model.UnitCost,
                Unit = model.Unit,
                //DeptId = model.DeptId,
                //Department = model.Department,
                Qty = model.Qty,
                QtyIss = model.QtyIss,
                LocationId = model.LocationId,
                LocationCode = model.LocationCode,
                LocationName = model.LocationName,
                TransferIn = model.TransferIn,
                TransferOut = model.TransferOut,
                QtyInBalance = model.QtyInBalance,
                TransferInBalance = model.TransferInBalance,
                TotalBalance = model.TotalBalance,
                AcqCost = model.AcqCost,
                OldStockNo = model.OldStockNo,
                StockNo = model.StockNo,
                Brand = model.Brand,
                Model_ = model.Model_,
                SerialNo = model.SerialNo,
                Description = model.Description,
                OtherDesc = model.OtherDesc,
                Remarks = model.Remarks,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.RPCIItems.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RPCIItemVM> UpdateAsync(RPCIItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidatePost(model.RpciId);

            var entity = await _db.RPCIItems.FindAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.RpciId = model.RpciId;
            entity.ItemCodeId = model.ItemCodeId;
            entity.Article = model.Article;
            entity.PoNo = model.PoNo;
            entity.PoDate = model.PoDate;
            entity.AirNo = model.AirNo;
            entity.AirDate = model.AirDate;
            entity.UnitCost = model.UnitCost;
            entity.Unit = model.Unit;
            //entity.DeptId = model.DeptId;
            //entity.Department = model.Department;
            entity.Qty = model.Qty;
            entity.QtyIss = model.QtyIss;
            entity.LocationId = model.LocationId;
            entity.LocationCode = model.LocationCode;
            entity.LocationName = model.LocationName;
            entity.TransferIn = model.TransferIn;
            entity.TransferOut = model.TransferOut;
            entity.QtyInBalance = model.QtyInBalance;
            entity.TransferInBalance = model.TransferInBalance;
            entity.TotalBalance = model.TotalBalance;
            entity.AcqCost = model.AcqCost;
            entity.OldStockNo = model.OldStockNo;
            entity.StockNo = model.StockNo;
            entity.Brand = model.Brand;
            entity.Model_ = model.Model_;
            entity.SerialNo = model.SerialNo;
            entity.Description = model.Description;
            entity.OtherDesc = model.OtherDesc;
            entity.Remarks = model.Remarks;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RPCIItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return model;
        });

        public ValueTask<RPCIItemVM> DeleteAsync(RPCIItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidatePost(model.RpciId);

            var entity = await _db.RPCIItems.Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RPCIItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.RPCIItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });
    }
}