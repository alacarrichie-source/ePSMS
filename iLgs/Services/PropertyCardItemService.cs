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
//    public interface IPropertyCardItemService
//    {
//        IQueryable<PropertyCardItemVM> GetByCardId(Guid? cardId);
//        ValueTask<PropertyCardItemVM> GetByIdAsync(Guid? id);

//        ValueTask<PropertyCardItemVM> CreateAsync(PropertyCardItemVM model, string user, DateTime date);
//        ValueTask<PropertyCardItemVM> UpdateAsync(PropertyCardItemVM model, string user, DateTime date);
//        ValueTask<PropertyCardItemVM> DeleteAsync(PropertyCardItemVM model, string user, DateTime date);
//    }

//    public class PropertyCardItemService : IPropertyCardItemService
//    {
//        private readonly AppManEntities _db = new AppManEntities();
//        private readonly IExceptionService<PropertyCardItemVM> _VmExceptionService = new ExceptionService<PropertyCardItemVM>();

//        public PropertyCardItemService(AppManEntities db)
//        {
//            _db = db;
//        }

//        public ValueTask<PropertyCardItemVM> GetByIdAsync(Guid? id) => _VmExceptionService.TryCatch(async () =>
//        {
//            var data = await _db.PropertyCardItems.Where(w => w.Id == id)
//                .Select(s => new PropertyCardItemVM
//                {
//                    Id = s.Id,
//                    CardId = s.CardId,
//                    Location = s.Location,
//                    IssuedTo = s.IssuedTo,
//                    Officer = s.Officer,
//                    RefNo = s.RefNo,
//                    RefDate = s.RefDate,
//                    RefType = s.RefType,
//                    PrevRefNo = s.PrevRefNo,
//                    PrevRefType = s.PrevRefType,
//                    PrevOfficer = s.PrevOfficer,
//                    QtyRec = s.QtyRec,
//                    Qty = s.Qty,
//                    QtyBal = s.QtyBal,
//                    TransType = s.TransType,
//                    Remarks = s.Remarks,
//                    Amount = s.Amount,
//                    InsertedDt = s.InsertedDt
//                }).FirstOrDefaultAsync();
//            return data;
//        });

//        public IQueryable<PropertyCardItemVM> GetByCardId(Guid? cardId) => _VmExceptionService.TryCatch(() =>
//        {
//            var data = _db.PropertyCardItems.Where(w => w.CardId == cardId)
//                .Select(s => new PropertyCardItemVM
//                {
//                    Id = s.Id,
//                    CardId = s.CardId,
//                    Location = s.Location,
//                    IssuedTo = s.IssuedTo,
//                    Officer = s.Officer,
//                    RefNo = s.RefNo,
//                    RefDate = s.RefDate,
//                    RefType = s.RefType,
//                    PrevRefNo = s.PrevRefNo,
//                    PrevRefType = s.PrevRefType,
//                    PrevOfficer = s.PrevOfficer,
//                    QtyRec = s.QtyRec,
//                    Qty = s.Qty,
//                    QtyBal = s.QtyBal,
//                    TransType = s.TransType,
//                    Remarks = s.Remarks,
//                    Amount = s.Amount,
//                    InsertedDt = s.InsertedDt
//                });
//            return data;
//        });


//        public ValueTask<PropertyCardItemVM> CreateAsync(PropertyCardItemVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
//        {
//            model.Id = Guid.NewGuid();
//            model.InsertedBy = user;
//            model.UpdatedBy = user;
//            model.InsertedDt = date;
//            model.UpdatedDt = date;

//            var entity = new PropertyCardItem
//            {
//                Id = model.Id,
//                CardId = model.CardId,
//                Location = model.Location,
//                IssuedTo = model.IssuedTo,
//                Officer = model.Officer,
//                RefNo = model.RefNo,
//                RefDate = model.RefDate,
//                RefType = model.RefType,
//                PrevRefNo = model.PrevRefNo,
//                PrevRefType = model.PrevRefType,
//                PrevOfficer = model.PrevOfficer,
//                QtyRec = model.QtyRec,
//                Qty = model.Qty,
//                QtyBal = model.QtyBal,
//                TransType = model.TransType,
//                Remarks = model.Remarks,
//                Amount = model.Amount,
//                InsertedBy = model.InsertedBy,
//                InsertedDt = model.InsertedDt,
//                UpdatedBy = model.UpdatedBy,
//                UpdatedDt = model.UpdatedDt
//            };

//            _db.PropertyCardItems.Add(entity);
//            await _db.SaveChangesAsync();

//            return model;
//        });

//        public ValueTask<PropertyCardItemVM> DeleteAsync(PropertyCardItemVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
//        {
//            model.UpdatedBy = user;
//            model.UpdatedDt = date;

//            var entity = await _db.PropertyCardItems.FindAsync(model.Id);

//            entity.UpdatedBy = model.UpdatedBy;
//            entity.UpdatedDt = model.UpdatedDt;

//            _db.PropertyCardItems.Attach(entity);
//            _db.Entry(entity).State = EntityState.Modified;
//            await _db.SaveChangesAsync();

//            _db.PropertyCardItems.Remove(entity);
//            _db.Entry(entity).State = EntityState.Deleted;
//            await _db.SaveChangesAsync();

//            return model;
//        });

//        public ValueTask<PropertyCardItemVM> UpdateAsync(PropertyCardItemVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
//        {
//            model.UpdatedBy = user;
//            model.UpdatedDt = date;

//            var entity = await _db.PropertyCardItems.FindAsync(model.Id);
//            if (entity == null)
//            {
//                throw new RecordNotFoundException(model.Id);
//            }

//            entity.CardId = model.CardId;
//            entity.Location = model.Location;
//            entity.IssuedTo = model.IssuedTo;
//            entity.Officer = model.Officer;
//            entity.RefNo = model.RefNo;
//            entity.RefDate = model.RefDate;
//            entity.RefType = model.RefType;
//            entity.PrevRefNo = model.PrevRefNo;
//            entity.PrevRefType = model.PrevRefType;
//            entity.PrevOfficer = model.PrevOfficer;
//            entity.QtyRec = model.QtyRec;
//            entity.Qty = model.Qty;
//            entity.QtyBal = model.QtyBal;
//            entity.TransType = model.TransType;
//            entity.Remarks = model.Remarks;
//            entity.Amount = model.Amount;
//            entity.UpdatedBy = model.UpdatedBy;
//            entity.UpdatedDt = model.UpdatedDt;

//            _db.PropertyCardItems.Attach(entity);
//            _db.Entry(entity).State = EntityState.Modified;
//            await _db.SaveChangesAsync();

//            return model;
//        });

//    }
//}