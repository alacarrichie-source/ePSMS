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
    public class AirInvoiceService : IAirInvoiceService
    {
        private readonly AppManEntities db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<AIRInvoiceVM> _vmExceptionService = new ExceptionService<AIRInvoiceVM>();
        private readonly IExceptionService<AIRInvoice> _exceptionService = new ExceptionService<AIRInvoice>();

        public AirInvoiceService(AppManEntities db)
        {
            this.db = db;
        }

        public IQueryable<AIRInvoiceVM> GetVmByAirId(Guid? airId) =>
       _vmExceptionService.TryCatch(() =>
       {
           var data = db.AIRInvoices.Where(w => w.AirId == airId)
               .Select(s => new AIRInvoiceVM
               {
                   Id = s.Id,
                   AirId = s.AirId,
                   InvoiceNo = s.InvoiceNo,
                   Amount = s.Amount,
                   InvoiceDate = s.InvoiceDate,
                   InsertedDt = s.InsertedDt
               });
           return data;
       });

        public ValueTask<AIRInvoiceVM> GetVmByIdAsync(Guid? id) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            var data = await db.AIRInvoices.Where(w => w.Id == id)
               .Select(s => new AIRInvoiceVM
               {
                   Id = s.Id,
                   AirId = s.AirId,
                   InvoiceNo = s.InvoiceNo,
                   InvoiceDate = s.InvoiceDate,
                   Amount = s.Amount,
                   InsertedDt = s.InsertedDt
               }).FirstOrDefaultAsync();
            return data;
        });

        
        public ValueTask<AIRInvoiceVM> CreateAsync(AIRInvoiceVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            ValidateRequired(model);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;
            
            await ValidateOnCreate(model);
            
            AIRInvoice entity = new AIRInvoice()
            {
                Id = model.Id,
                AirId = model.AirId,
                InvoiceNo = model.InvoiceNo,
                InvoiceDate = model.InvoiceDate,
                Amount = model.Amount,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            db.AIRInvoices.Add(entity);
            await db.SaveChangesAsync();

            await UpdateAIR(model.AirId, user, date);            

            return model;
        });        

        private async ValueTask UpdateAIR(Guid? airId, string user, DateTime date)
        {
            var invoices = await db.AIRInvoices.Where(w => w.AirId == airId).OrderBy(o => o.InvoiceNo).ToListAsync();
            string invoiceNo = "";
            DateTime? invoiceDate = null;
            if (invoices.Any())
            {
                invoiceNo = string.Join(",", invoices.Select(s => s.InvoiceNo));
                if (invoiceNo.Length > 150)
                {
                    invoiceNo = invoiceNo.Substring(0, 150);
                }

                invoiceDate = invoices.GroupBy(g => g.InvoiceDate).Min(m => m.Key);
            }            

            var air = await db.AIRs.FindAsync(airId);
            air.UpdatedBy = user;
            air.UpdatedDt = date;
            air.InvoiceNo = invoiceNo;
            air.InvoiceDate = invoiceDate;            

            db.AIRs.Attach(air);
            db.Entry(air).State = EntityState.Modified;
            await db.SaveChangesAsync();
        }

        public ValueTask<AIRInvoiceVM> UpdateAsync(AIRInvoiceVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            ValidateRequired(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            await ValidateOnUpdate(model);

            AIRInvoice entity = await db.AIRInvoices.FindAsync(model.Id);

            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }            

            entity.InvoiceNo = model.InvoiceNo;
            entity.InvoiceDate = model.InvoiceDate;
            entity.Amount = model.Amount;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.AIRInvoices.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            await UpdateAIR(model.AirId, user, date);

            return model;
        });

        public ValueTask<AIRInvoiceVM> DeleteAsync(AIRInvoiceVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            AIRInvoice entity = await db.AIRInvoices.FindAsync(model.Id);

            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.AIRInvoices.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.AIRInvoices.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            await UpdateAIR(model.AirId, user, date);

            return model;
        });

        private void ValidateRequired(AIRInvoiceVM model)
        {
            if (string.IsNullOrWhiteSpace(model.InvoiceNo))
            {
                throw new RequiredFieldException(nameof(model.InvoiceNo));
            }

            if (model.InvoiceDate == null)
            {
                throw new RequiredFieldException(nameof(model.InvoiceDate));
            }
        }
        private async Task ValidateOnCreate(AIRInvoiceVM model)
        {            

            if (await db.AIRInvoices.AnyAsync(a => a.InvoiceNo == model.InvoiceNo && a.AirId == model.AirId))
            {
                throw new RecordAlreadyExistsException(string.Format("Invoice Number {0} already exists", model.InvoiceNo));
            }                                    
        }

        private async Task ValidateOnUpdate(AIRInvoiceVM model)
        {            
            if (await db.AIRInvoices.AnyAsync(a => a.InvoiceNo == model.InvoiceNo && a.Id != model.Id))
            {
                throw new RecordAlreadyExistsException(string.Format("Invoice Number {0} already exists", model.InvoiceNo));
            }
        }

        //private static void Validate(params (dynamic Rule, string Parameter)[] validations)
        //{
        //    var invalidRecordException = new InvalidRecordException();

        //    foreach ((dynamic rule, string parameter) in validations)
        //    {
        //        if (rule.Condition)
        //        {
        //            invalidRecordException.UpsertDataList(KeyNotFoundException: parameter, ValueTask: rule.Message);
        //        }
        //    }
        //    invalidRecordException.ThrowIfContainsErrors();
        //}
    }
}