using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Data.Entity;
using iLgs.Exceptions;
using System.Data.SqlClient;
using System.Data.Entity.Infrastructure;

namespace iLgs.Services
{
    public class RisService : IRisService
    {
        private readonly AppManEntities db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();

        public RisService(AppManEntities db)
        {
            this.db = db;
        }

        public IQueryable<RIS_VM> GetAll()
        {
            var data = db.RISses
                .Select(s => new RIS_VM
                {
                    Id = s.Id,
                    Fund = s.Fund,
                    Division = s.Division,
                    Office = s.Office,
                    FPP = s.FPP,
                    RisNo = s.RisNo,
                    RisDate = s.RisDate,
                    Purpose = s.Purpose,
                    RequestedBy = s.RequestedBy,
                    RequestedByDesignation = s.RequestedByDesignation,
                    RequestedDate = s.RequestedDate,
                    ApprovedBy = s.ApprovedBy,
                    ApprovedByDesignation = s.ApprovedByDesignation,
                    ApprovedDate = s.ApprovedDate,
                    IssuedBy = s.IssuedBy,
                    IssuedByDesignation = s.IssuedByDesignation,
                    IssuedDate = s.IssuedDate,
                    ReceivedBy = s.ReceivedBy,
                    ReceivedByDesignation = s.ReceivedByDesignation,
                    ReceivedDate = s.ReceivedDate,
                    InsertedBy = s.InsertedBy,
                    InsertedDt = s.InsertedDt,
                    UpdatedBy = s.UpdatedBy,
                    UpdatedDt = s.UpdatedDt,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt
                });
            return data;
        }

        public async Task<bool> GetAnyRisNoAsync(Guid risId, string risNo)
        {
            return await db.RISses.AnyAsync(a => a.Id != risId && a.RisNo == risNo);
        }

        public async Task<RISs> GetByIdAsync(Guid id)
        {
            return await db.RISses.FindAsync(id);
        }

        public async Task<RISs> GetByRisNoAsync(string risNo)
        {
            return await db.RISses.Where(w => w.RisNo == risNo).FirstOrDefaultAsync();
        }

        public async Task<RISs> GetByOrderIdAsync(Guid orderId)
        {
            return await db.RISses.Where(w => w.Requests.Any(a => a.Orders.Any(b => b.Id == orderId))).FirstOrDefaultAsync();
        }

        public async Task<bool> IsPostedAsync(Guid risId)
        {
            var entity = await db.RISses.FindAsync(risId);
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }

        public async Task<bool> IsPrPostedAsync(Guid risId)
        {
            var pr = await db.Requests.Where(a => a.RisId == risId).FirstOrDefaultAsync();
            if (pr != null)
            {
                return !string.IsNullOrWhiteSpace(pr.SubmittedBy);
            }
            return false;
        }

        public async Task<bool> IsWithPrAsync(Guid risId)
        {
            return await db.Requests.AnyAsync(a => a.RisId == risId);
        }

        public Task PostAsync(Guid risId, string user, DateTime date) =>
        TryCatch(async () =>
        {
            await ValidateOnPost(risId);

            var entity = await db.RISses.FindAsync(risId);
            
            entity.PostedBy = user;
            entity.PostedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.RISses.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();            

            // crate psCode foreach item (problem in unpost, sequence number will rumble)
            var risItemList = await db.RisItems.Include(i => i.ItemCode.ItemType).Where(w => w.RisId == entity.Id).ToListAsync();
            foreach (var risItem in risItemList)
            {
                if (!db.PsCodes.Any(a => a.PsType == risItem.ItemCode.ItemType.Code && a.PsNo == risItem.PsNoDisplay))
                {
                    var psCode = new PsCode()
                    {
                        Id = Guid.NewGuid(),
                        PsNo = risItem.PsNoDisplay, //NextPsNo(risItem.ItemCode.ItemType.Code),
                        PsType = risItem.ItemCode.ItemType.Code,
                        ItemName = risItem.ItemName,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };

                    db.PsCodes.Add(psCode);
                    await db.SaveChangesAsync();
                }
            }
        });

        private string NextPsNo(string psType)
        {
            string keyName = psType;
            // yyyy-mm-9999
            // 123456789012

            var rec = db.PsCodes.Where(w => w.PsType == psType).OrderByDescending(o => o.PsNo).FirstOrDefault();
            if (rec == null)
            {
                return keyName + "0001";
            }
            else
            {
                string sequence = "";
                foreach (char c in rec.PsNo)
                {
                    if (char.IsDigit(c))
                    {
                        sequence += c;
                    }
                }
                return keyName + sequence.PadLeft(4, '0');
            }
        }

        public Task UnpostAsync(Guid risId, string user, DateTime date) =>
        TryCatch(async () =>
        {
            await ValidateOnUnpost(risId);

            var entity = await db.RISses.FindAsync(risId);

            entity.PostedBy = null;
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.RISses.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            // delete un-used psCode foreach item (problem in unpost, sequence number will rumble)
            var risItemList = await db.RisItems.Include(i => i.ItemCode.ItemType).Where(w => w.RisId == entity.Id).ToListAsync();
            foreach (var risItem in risItemList)
            {
                var psCode = await db.PsCodes.Where(w => w.PsNo == risItem.PsNoDisplay && !w.PsStocks.Any()).FirstOrDefaultAsync();
                if (psCode != null)
                {
                    db.PsCodes.Remove(psCode);
                    db.Entry(psCode).State = EntityState.Deleted;
                    await db.SaveChangesAsync();
                }
            }
            
        });

        public Task<RIS_VM> CreateAsync(RIS_VM model, string user, DateTime date) =>
        TryCatch(async () =>
            {
                await ValidateOnCreate(model);

                model.Id = Guid.NewGuid();
                if (string.IsNullOrWhiteSpace(model.RisNo))
                {
                    model.RisNo = NextRisNo((DateTime)model.RisDate);
                }
                model.InsertedBy = user;
                model.InsertedDt = date;
                model.UpdatedBy = user;
                model.UpdatedDt = date;

                var entity = new iLgs.Models.RISs()
                {
                    Id = model.Id,
                    Fund = model.Fund,
                    Division = model.Division ?? "",
                    Office = model.Office,
                    FPP = model.FPP,
                    RisNo = model.RisNo,
                    RisDate = model.RisDate,
                    Purpose = model.Purpose,
                    RequestedBy = model.RequestedBy ?? "",
                    RequestedByDesignation = model.RequestedByDesignation ?? "",
                    RequestedDate = model.RequestedDate,
                    ApprovedBy = model.ApprovedBy ?? "",
                    ApprovedByDesignation = model.ApprovedByDesignation ?? "",
                    ApprovedDate = model.ApprovedDate,
                    IssuedBy = model.IssuedBy ?? "",
                    IssuedByDesignation = model.IssuedByDesignation ?? "",
                    IssuedDate = model.IssuedDate,
                    ReceivedBy = model.ReceivedBy ?? "",
                    ReceivedByDesignation = model.ReceivedByDesignation ?? "",
                    ReceivedDate = model.ReceivedDate,
                    InsertedBy = model.InsertedBy,
                    InsertedDt = model.InsertedDt,
                    UpdatedBy = model.UpdatedBy,
                    UpdatedDt = model.UpdatedDt
                };

                db.RISses.Add(entity);
                await db.SaveChangesAsync();

                return model;
            });

        public Task<RIS_VM> DeleteAsync(RIS_VM model, string user, DateTime date) =>
        TryCatch(async () =>
        {
            await ValidateOnDelete(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await db.RISses.FindAsync(model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.RISses.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.RISses.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            return model;
        });

        public Task<RIS_VM> UpdateAsync(RIS_VM model, string user, DateTime date) =>
        TryCatch(async () =>
        {
            await ValidateOnUpdate(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await db.RISses.FindAsync(model.Id);

            entity.Fund = model.Fund;
            entity.Division = model.Division ?? "";
            entity.Office = model.Office;
            entity.FPP = model.FPP;
            entity.RisNo = model.RisNo;
            entity.RisDate = model.RisDate;
            entity.Purpose = model.Purpose;
            entity.RequestedBy = model.RequestedBy ?? "";
            entity.RequestedByDesignation = model.RequestedByDesignation ?? "";
            entity.RequestedDate = model.RequestedDate;
            entity.ApprovedBy = model.ApprovedBy ?? "";
            entity.ApprovedByDesignation = model.ApprovedByDesignation ?? "";
            entity.ApprovedDate = model.ApprovedDate;
            entity.IssuedBy = model.IssuedBy ?? "";
            entity.IssuedByDesignation = model.IssuedByDesignation ?? "";
            entity.IssuedDate = model.IssuedDate;
            entity.ReceivedBy = model.ReceivedBy ?? "";
            entity.ReceivedByDesignation = model.ReceivedByDesignation ?? "";
            entity.ReceivedDate = model.ReceivedDate;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.RISses.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return model;
        });

        private string NextRisNo(DateTime date)
        {
            string yyyy = date.Year.ToString().Trim();
            string mm = date.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = db.RISses.Where(w => w.RisDate.Value.Year == date.Year).OrderByDescending(o => o.RisNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(data.RisNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }

        #region VALIDATION

        private async Task ValidateOnCreate(RIS_VM model)
        {
            if (await db.RISses.AnyAsync(a => a.RisNo == model.RisNo))
            {
                throw new RecordAlreadyExistsException(string.Format("RIS Number {0} already exists", model.RisNo));
            }            
        }

        private async Task ValidateOnUpdate(RIS_VM model)
        {
            var rec = await db.RISses.FindAsync(model.Id);
            if (rec == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (!string.IsNullOrWhiteSpace(rec.PostedBy))
            {
                throw new RecordAlreadyPostedException(string.Format("RIS No {0} already posted. Cannot update!", rec.RisNo));
            }


            if (await IsPrPostedAsync(model.Id))
            {
                throw new RecordRelationshipException("This RIS No has a posted PR, cannot update!");
            }
            else if (await GetAnyRisNoAsync(model.Id, model.RisNo))
            {
                throw new RecordAlreadyExistsException(string.Format("RIS No {0} already exists!", model.RisNo));
            }
        }

        private async Task ValidateOnDelete(RIS_VM model)
        {
            if (await IsPostedAsync(model.Id))
            {
                throw new RecordAlreadyPostedException(string.Format("RIS No. {0} already Posted, cannot delete!", model.RisNo));
            }

            if (await IsPrPostedAsync(model.Id))
            {
                throw new RecordRelationshipException("This RIS Number has a posted PR, cannot delete!");
            }
        }

        private async Task ValidateOnPost(Guid id)
        {
            var rec = await db.RISses.FindAsync(id);
            if (rec == null)
            {
                throw new RecordNotFoundException(id);
            }

            if (!string.IsNullOrWhiteSpace(rec.PostedBy))
            {
                throw new RecordAlreadyPostedException(string.Format("RIS No {0} already posted. Please verify!", rec.RisNo));
            }            
        }
        private async Task ValidateOnUnpost(Guid id)
        {
            var rec = await db.RISses.FindAsync(id);
            if (rec == null)
            {
                throw new RecordNotFoundException(id);
            }

            if (string.IsNullOrWhiteSpace(rec.PostedBy))
            {
                throw new RecordAlreadyPostedException(string.Format("RIS No {0} is not yet posted. Please verify!", rec.RisNo));
            }

            if (await IsPrPostedAsync(id))
            {
                throw new RecordRelationshipException("This RIS No has a posted PR, cannot unpost!");
            }
        }        

        //private async Task ValidateOnPost(Order entity)
        //{
        //    if (!string.IsNullOrWhiteSpace(entity.PostedBy))
        //    {
        //        throw new PoNumberAlreadyPostedException(entity.PoNo);
        //    }

        //    var idList = await db.OrderItems.Where(w => w.OrderId == entity.Id).GroupBy(g => g.RequestItem.Request.Id)
        //        .Select(s => s.Key).ToListAsync();

        //    foreach (var id in idList)
        //    {
        //        var request = await db.Requests.FindAsync(id);
        //        if (request == null)
        //        {
        //            throw new RecordNotFoundException(id);
        //        }
        //        else
        //        {
        //            if (string.IsNullOrWhiteSpace(request.SubmittedBy))
        //            {
        //                throw new PurchaseRequestNotYetPostedException(request.PrNo);
        //            }
        //        }
        //    }

        //    var orderItems = await db.OrderItems.Where(w => w.OrderId == entity.Id).ToListAsync();
        //    foreach (var orderItem in orderItems)
        //    {
        //        if (string.IsNullOrWhiteSpace(orderItem.Brand))
        //        {
        //            throw new RequiredFieldException(nameof(orderItem.Brand));
        //        }
        //    }
        //}
        #endregion

        #region EXCEPTIONS
        private delegate Task NonReturningFunction();
        private delegate Task<RIS_VM> ReturningFunction();
        private delegate IQueryable<RIS_VM> ReturningQueryableFunction();

        private async Task TryCatch(NonReturningFunction nonReturningFunction)
        {
            try
            {
                await nonReturningFunction();
            }
            catch (RecordNotFoundException notFoundException)
            {
                throw notFoundException;
            }
            catch (RecordAlreadyExistsException recordAlreadyExistsException)
            {
                throw recordAlreadyExistsException;
            }
            catch (InvalidValueException invalidValueException)
            {
                throw invalidValueException;
            }
            catch (RecordAlreadyPostedException recordAlreadyPostedException)
            {
                throw recordAlreadyPostedException;
            }
            catch (RecordRelationshipException recordRelationshipExistsException)
            {
                throw recordRelationshipExistsException;
            }
            catch (SqlException sqlException)
            {
                throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var recordLockedException = new RecordLockedException(dbUpdateConcurrencyException);

                throw exceptions.CreateAndLogDependencyException(recordLockedException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                throw exceptions.CreateAndLogDependencyException(dbUpdateException);
            }
            catch (Exception exception)
            {
                var failedServiceException =
                    new FailedServiceException(exception);

                throw exceptions.CreateAndLogServiceException(failedServiceException);
            }
        }
        private async Task<RIS_VM> TryCatch(ReturningFunction returningFunction)
        {
            try
            {
                return await returningFunction();
            }
            catch (RecordNotFoundException notFoundException)
            {
                throw notFoundException;
            }
            catch (RecordAlreadyExistsException recordAlreadyExistsException)
            {
                throw recordAlreadyExistsException;
            }
            catch (InvalidValueException invalidValueException)
            {
                throw invalidValueException;
            }
            catch (RecordAlreadyPostedException recordAlreadyPostedException)
            {
                throw recordAlreadyPostedException;
            }
            catch (RecordRelationshipException recordRelationshipExistsException)
            {
                throw recordRelationshipExistsException;
            }
            catch (SqlException sqlException)
            {
                throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var recordLockedException = new RecordLockedException(dbUpdateConcurrencyException);

                throw exceptions.CreateAndLogDependencyException(recordLockedException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                throw exceptions.CreateAndLogDependencyException(dbUpdateException);
            }
            catch (Exception exception)
            {
                var failedServiceException =
                    new FailedServiceException(exception);

                throw exceptions.CreateAndLogServiceException(failedServiceException);
            }
        }

        #endregion
    }
}