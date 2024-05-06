//using iLgs.Exceptions;
//using iLgs.Models;
//using iLgs.Services.Interfaces;
//using System;
//using System.Collections.Generic;
//using System.Data.Entity;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Web;

//namespace iLgs.Services
//{
//    public interface IPsItemIssueanceService
//    {
//        IQueryable<PsItemIssuanceVM> GetByPsItemId(Guid? psItemId);
//        ValueTask<PsItemIssuance> GetByIdAsync(Guid? id);
//        ValueTask<PsItemIssuanceVM> GetVmByIdAsync(Guid? id);
//        ValueTask<PsItemIssuanceVM> CreateAsync(PsItemIssuanceVM model, string user, DateTime date);
//        ValueTask<PsItemIssuanceVM> UpdateAsync(PsItemIssuanceVM model, string user, DateTime date);
//        ValueTask<PsItemIssuanceVM> DeleteAsync(PsItemIssuanceVM model, string user, DateTime date);
//    }


//    public class PsItemIssueanceService : IPsItemIssueanceService
//    {
//        private readonly AppManEntities _db = new AppManEntities();
//        private readonly ICreateAndLogExceptions _exceptions = new CreateAndLogExceptions();
//        private readonly IExceptionService<PsItemIssuanceVM> _vmExceptionService = new ExceptionService<PsItemIssuanceVM>();
//        private readonly IExceptionService<PsItemIssuance> _exceptionService = new ExceptionService<PsItemIssuance>();

//        public PsItemIssueanceService(AppManEntities db)
//        {
//            _db = db;
//        }

//        public ValueTask<PsItemIssuanceVM> GetVmByIdAsync(Guid? id) =>
//        _vmExceptionService.TryCatchAsync(async () =>
//        {
//            var data = await _db.PsItemIssuances.Where(w => w.Id == id)
//                .Select(s => new PsItemIssuanceVM
//                {
//                    Id = s.Id,
//                    TranCode = s.TranCode,
//                    PsItemId = s.PsItemId,
//                    RisIssuedId = s.RisIssuedId,
//                    IssuedDate = s.IssuedDate,
//                    IssuedTo = s.IssuedTo,
//                    Qty = s.Qty,
//                    UnitCost = s.UnitCost,
//                    SourceId = s.SourceId,
//                    InsertedDt = s.InsertedDt,
//                }).FirstOrDefaultAsync();
//            return data;
//        });

//        public ValueTask<PsItemIssuance> GetByIdAsync(Guid? id) =>
//        _exceptionService.TryCatchAsync(async () =>
//        {
//            var data = await _db.PsItemIssuances.FindAsync(id);
//            return data;
//        });

//        public IQueryable<PsItemIssuanceVM> GetByPsItemId(Guid? psItemId) =>
//        _vmExceptionService.TryCatch(() =>
//        {
//            var data = _db.PsItemIssuances.Where(w => w.PsItemId == psItemId)
//                .Select(s => new PsItemIssuanceVM
//                {
//                    Id = s.Id,
//                    TranCode = s.TranCode,
//                    PsItemId = s.PsItemId,
//                    RisIssuedId = s.RisIssuedId,
//                    IssuedDate = s.IssuedDate,
//                    IssuedTo = s.IssuedTo,
//                    Qty = s.Qty,
//                    UnitCost = s.UnitCost,
//                    SourceId = s.SourceId,
//                    InsertedDt = s.InsertedDt,
//                });
//            return data;
//        });

//        public ValueTask<PsItemIssuanceVM> CreateAsync(PsItemIssuanceVM model, string user, DateTime date) =>
//        _vmExceptionService.TryCatchAsync(async () =>
//        {
//            model.Id = Guid.NewGuid();
//            model.InsertedBy = user;
//            model.UpdatedBy = user;
//            model.InsertedDt = date;
//            model.UpdatedDt = date;

//            var entity = new PsItemIssuance()
//            {
//                Id = model.Id,
//                TranCode = model.TranCode,
//                PsItemId = model.PsItemId,
//                RisIssuedId = model.RisIssuedId,
//                IssuedDate = model.IssuedDate,
//                IssuedTo = model.IssuedTo,
//                Qty = model.Qty,
//                UnitCost = model.UnitCost,
//                SourceId = model.SourceId,
//                InsertedBy = model.InsertedBy,
//                InsertedDt = model.InsertedDt,
//                UpdatedBy = model.UpdatedBy,
//                UpdatedDt = model.UpdatedDt
//            };

//            _db.PsItemIssuances.Add(entity);
//            await _db.SaveChangesAsync();
//            await UpdatePsItems(model.PsItemId);
//            return model;
//        });

//        public ValueTask<PsItemIssuanceVM> DeleteAsync(PsItemIssuanceVM model, string user, DateTime date) =>
//        _vmExceptionService.TryCatchAsync(async () =>
//        {
//            if (model.RisIssuedId != null)
//            {
//                throw new RecordRelationshipException("Please use the issuance mmodule to delete this record..");
//            }

//            model.UpdatedBy = user;
//            model.UpdatedDt = date;

//            var entity = await _db.PsItemIssuances.FindAsync(model.Id);

//            entity.UpdatedBy = model.UpdatedBy;
//            entity.UpdatedDt = model.UpdatedDt;

//            _db.PsItemIssuances.Attach(entity);
//            _db.Entry(entity).State = EntityState.Modified;
//            await _db.SaveChangesAsync();

//            _db.PsItemIssuances.Remove(entity);
//            _db.Entry(entity).State = EntityState.Deleted;
//            await _db.SaveChangesAsync();
//            await UpdatePsItems(model.PsItemId);
//            return model;
//        });

//        public ValueTask<PsItemIssuanceVM> UpdateAsync(PsItemIssuanceVM model, string user, DateTime date) =>
//        _vmExceptionService.TryCatchAsync(async () =>
//        {
//            if (model.RisIssuedId != null)
//            {
//                throw new RecordRelationshipException("Please use the issuance mmodule to update this record..");
//            }

//            model.UpdatedBy = user;
//            model.UpdatedDt = date;

//            var entity = await _db.PsItemIssuances.FindAsync(model.Id);

//            entity.TranCode = model.TranCode;
//            entity.PsItemId = model.PsItemId;
//            entity.RisIssuedId = model.RisIssuedId;
//            entity.IssuedDate = model.IssuedDate;
//            entity.IssuedTo = model.IssuedTo;
//            entity.Qty = model.Qty;
//            entity.UnitCost = model.UnitCost;
//            entity.SourceId = model.SourceId;
//            entity.UpdatedBy = model.UpdatedBy;
//            entity.UpdatedDt = model.UpdatedDt;

//            _db.PsItemIssuances.Attach(entity);
//            _db.Entry(entity).State = EntityState.Modified;
//            await _db.SaveChangesAsync();
//            await UpdatePsItems(model.PsItemId);
//            return model;
//        });

//        private async ValueTask UpdatePsItems(Guid? psItemId)
//        {
//            var qtyIssued = await _db.PsItemIssuances.Where(w => w.PsItemId == psItemId).SumAsync(s => s.Qty) ?? 0;            
//            var entity = await _db.PsItems.FindAsync(psItemId);
//            entity.QtyIss = qtyIssued;
//            entity.QtyBal = entity.QtyPo - qtyIssued;
//            _db.PsItems.Attach(entity);
//            _db.Entry(entity).State = EntityState.Modified;

//            await _db.SaveChangesAsync();
//        }
//    }
//}