using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services
{
    public interface IPsCardItemIssuanceService
    {
        IQueryable<PsCardItemIssuanceVM> GetByCardItemId(Guid? cardItemId);
        ValueTask<PsCardItemIssuanceVM> GetByIdAsync(Guid? id);        

        ValueTask<PsCardItemIssuanceVM> CreateAsync(PsCardItemIssuanceVM model, string user, DateTime date);
        ValueTask<PsCardItemIssuanceVM> UpdateAsync(PsCardItemIssuanceVM model, string user, DateTime date);
        ValueTask<PsCardItemIssuanceVM> DeleteAsync(PsCardItemIssuanceVM model, string user, DateTime date);
        ValueTask PostAsync(Guid psCardItemIssuanceId, string user, DateTime date);
        ValueTask UnpostAsync(Guid psCardItemIssuanceId, string user, DateTime date);
    }

    public class PsCardItemIssuanceService : IPsCardItemIssuanceService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly IExceptionService<PsCardItemIssuanceVM> _VmExceptionService = new ExceptionService<PsCardItemIssuanceVM>();

        public PsCardItemIssuanceService(AppManEntities db)
        {
            _db = db;
        }        

        public ValueTask<PsCardItemIssuanceVM> GetByIdAsync(Guid? id) => _VmExceptionService.TryCatch(async () =>
        {
            var data = await _db.PsCardItemIssuances.Where(w => w.Id == id).AsNoTracking()
                .Select(s => new PsCardItemIssuanceVM
                {
                    Id = s.Id,
                    LocationId = s.LocationId,
                    PsCardItemId = s.PsCardItemId,
                    IssuedTo = s.IssuedTo,
                    IssuedDate = s.IssuedDate,
                    IssuedToCode = s.IssuedToCode,
                    IssuedToDescription = s.IssuedToDescription,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    InsertedDt = s.InsertedDt,
                    DeptId = s.DeptId,
                    Department = s.Codextn.Description,
                    UnitCost = s.PsCardItem.UnitCost,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt
                    //IssuedToDesc = s.IssuedTo.Contains("Department") ? s.Codextn.Description : (s.IssuedTo == "Location" || s.IssuedTo == "Disposal") ? s.Codextn1.Description : ""
                }).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<PsCardItemIssuanceVM> GetByCardItemId(Guid? PsCardItemId) => _VmExceptionService.TryCatch(() =>
        {
            var data = _db.PsCardItemIssuances.Where(w => w.PsCardItemId == PsCardItemId).AsNoTracking()
                .Select(s => new PsCardItemIssuanceVM
                {
                    Id = s.Id,
                    LocationId = s.LocationId,
                    PsCardItemId = s.PsCardItemId,
                    IssuedTo = s.IssuedTo,
                    IssuedDate = s.IssuedDate,
                    IssuedToCode = s.IssuedToCode,
                    IssuedToDescription = s.IssuedToDescription,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    InsertedDt = s.InsertedDt,
                    DeptId = s.DeptId,
                    Department = s.Codextn.Description,
                    UnitCost = s.PsCardItem.UnitCost,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt          
                    //IssuedToDesc = s.IssuedTo.Contains("Department") ? s.Codextn.Description : (s.IssuedTo == "Location" || s.IssuedTo == "Disposal") ? s.Codextn1.Description : ""
                });
            return data;
        });

        private async ValueTask ValidateFieldsAsync(PsCardItemIssuanceVM model)
        {
            if (model.IssuedTo.Contains("Department"))
            {
                if (model.DeptId == null)
                {
                    throw new InvalidValueException("Department is required!");
                }                
            }

            if (model.IssuedTo == "Location" || model.IssuedTo == "Disposal")
            {
                if (model.LocationId == null)
                {
                    throw new InvalidValueException("Location is required!");
                }
            }

            if (model.IssuedDate == null)
            {
                throw new InvalidValueException("Issued Date is required!");
            }

            if (model.Qty == 0)
            {
                throw new InvalidValueException("Quantity is required!");
            }

            var rsmiDate = await _db.RSMIs.MaxAsync(m => m.Date);
            if (rsmiDate != null && rsmiDate > model.IssuedDate)
            {
                throw new InvalidValueException(string.Format("Date issued must be after the last RSMI date on {0}", rsmiDate.Value.ToShortDateString()));
            }
        }
        
        public ValueTask<PsCardItemIssuanceVM> CreateAsync(PsCardItemIssuanceVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            await ValidateFieldsAsync(model);

            //var totalQtyIssued = _db.PsCardItems.Find(model.PsCardItemId)?.Qty ?? 0;
            //var qtyIssued = _db.PsCardItemIssuances.Where(w => w.PsCardItemId == model.PsCardItemId).Sum(s => s.Qty) ?? 0;            
            //var qtyBalance = totalQtyIssued - qtyIssued;
            var qtyBalance = _db.PsCardItems.Find(model.PsCardItemId)?.QtyBal ?? 0;
            if (model.Qty > qtyBalance)
            {
                throw new InvalidValueException(string.Format("Quantity must not exceed the remaing balance of {0}", qtyBalance));
            }

            if (model.IssuedTo.Contains("Department") || model.IssuedTo == "Location" || model.IssuedTo == "Disposal")
            { 
                if (model.IssuedTo.Contains("Department"))
                {
                    model.LocationId = null;
                    model.IssuedToDescription = model.IssuedTo;
                }
                else if (model.IssuedTo == "Location" || model.IssuedTo == "Disposal")
                {
                    model.DeptId = null;
                }
            }
            else
            {
                model.LocationId = null;
                model.IssuedToCode = "";                
            }

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;            

            var cardItem = await _db.PsCardItems.FindAsync(model.PsCardItemId);

            var entity = new PsCardItemIssuance
            {
                Id = model.Id,
                LocationId = model.LocationId,
                PsCardItemId = model.PsCardItemId,
                IssuedTo = model.IssuedTo,
                IssuedDate = model.IssuedDate,
                Qty = model.Qty,
                Amount = cardItem.UnitCost * model.Qty,
                IssuedToCode = model.IssuedToCode,
                IssuedToDescription = model.IssuedToDescription,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt,
                DeptId = model.DeptId
            };

            _db.PsCardItemIssuances.Add(entity);
            await _db.SaveChangesAsync();

            await UpdatePsItems(model.PsCardItemId, user, date);

            return model;
        });

        public ValueTask<PsCardItemIssuanceVM> UpdateAsync(PsCardItemIssuanceVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            await ValidateFieldsAsync(model);

            var entity = await _db.PsCardItemIssuances.Include(i => i.PsCardItem).Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (entity.PostedDt != null)
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            }

            //var totalQtyIssued = _db.PsCardItems.Find(model.PsCardItemId)?.Qty ?? 0;
            //var qtyIssued = _db.PsCardItemIssuances.Where(w => w.PsCardItemId == model.PsCardItemId && w.Id != model.Id).Sum(s => s.Qty) ?? 0;
            //var qtyBalance = totalQtyIssued - qtyIssued;
            var qtyBalance = _db.PsCardItems.Find(model.PsCardItemId)?.QtyBal ?? 0;
            if (model.Qty > qtyBalance)
            {
                throw new InvalidValueException(string.Format("Quantity must not exceed the remaing balance of {0}", qtyBalance));
            }

            if (model.IssuedTo.Contains("Department") || model.IssuedTo == "Location" || model.IssuedTo == "Disposal")
            {
                if (model.IssuedTo.Contains("Department"))
                {
                    model.LocationId = null;
                }
                else if (model.IssuedTo == "Location" || model.IssuedTo == "Disposal")
                {
                    model.DeptId = null;
                }
            }
            else
            {
                model.LocationId = null;
                model.IssuedToCode = "";
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.LocationId = model.LocationId;
            entity.PsCardItemId = model.PsCardItemId;
            entity.DeptId = model.DeptId;
            entity.IssuedTo = model.IssuedTo;
            entity.IssuedDate = model.IssuedDate;
            entity.IssuedToCode = model.IssuedToCode;
            entity.IssuedToDescription = model.IssuedToDescription;
            entity.Qty = model.Qty;
            entity.Amount = entity.PsCardItem.UnitCost * model.Qty;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItemIssuances.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            await UpdatePsItems(model.PsCardItemId, user, date);

            return model;
        });

        public ValueTask<PsCardItemIssuanceVM> DeleteAsync(PsCardItemIssuanceVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            var entity = await _db.PsCardItemIssuances.FindAsync(model.Id);

            if (entity.PostedDt != null)
            {
                throw new RecordAlreadyPostedException("Record Already Posted, cannot delete!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;
            
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItemIssuances.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCardItemIssuances.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            await UpdatePsItems(model.PsCardItemId, user, date);

            return model;
        });

        private async Task UpdatePsItems(Guid? cardItemId, string user, DateTime date)
        {
            var entity = await _db.PsCardItems.Include(i => i.PsCardItemIssuances).Where(w => w.Id == cardItemId).FirstOrDefaultAsync();
            var qtyIss = entity.PsCardItemIssuances.Sum(s => s.Qty) ?? 0;
            var qty = (entity.Qty ?? 0) + (entity.TransferIn ?? 0);
            var transferOut = (entity.TransferOut ?? 0);

            entity.QtyIss = qtyIss;
            entity.QtyBal = qty - (qtyIss + transferOut);
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PsCardItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }

        public ValueTask PostAsync(Guid psCardItemIssuanceId, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            var entity = await _db.PsCardItemIssuances.FindAsync(psCardItemIssuanceId);
            if (entity == null)
            {
                throw new RecordNotFoundException(psCardItemIssuanceId);
            }

            entity.PostedBy = user;
            entity.PostedDt = date;

            _db.PsCardItemIssuances.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        });

        public ValueTask UnpostAsync(Guid psCardItemIssuanceId, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            var entity = await _db.PsCardItemIssuances.FindAsync(psCardItemIssuanceId);
            if (entity == null)
            {
                throw new RecordNotFoundException(psCardItemIssuanceId);
            }

            entity.PostedBy = null;
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PsCardItemIssuances.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        });
    }
}