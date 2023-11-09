using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services
{
    public class RisItemService : IRisItemService
    {
        private readonly AppManEntities db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RisItemVM> _vmExceptionService = new ExceptionService<RisItemVM>();
        private readonly IExceptionService<RisItem> _exceptionService = new ExceptionService<RisItem>();

        public RisItemService(AppManEntities db)
        {
            this.db = db;
        }

        public ValueTask<RisItemVM> GetVmByIdAsync(Guid? id) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            var data = await db.RisItems.Where(w => w.Id == id)
                .Select(s => new RisItemVM
                {
                    Id = s.Id,
                    RisId = s.RisId,
                    ItemCodeId = s.ItemCodeId,
                    ItemCode = s.ItemCode.Code,
                    ItemType = s.ItemCode.Description,
                    PsType = s.ItemCode.ItemType.Code,
                    PsNo = s.PsNo,
                    PsNoDisplay = s.PsNoDisplay,
                    Unit = s.Unit,
                    ItemName = s.ItemName,
                    Description = s.Description,
                    QtyRequest = s.QtyRequest,
                    QtyIssue = s.QtyIssue,
                    Remarks = s.Remarks,
                    InsertedDt = s.InsertedDt,
                    Department = s.RISs.Office,
                    IsPosted = s.RISs.PostedDt != null
                }).FirstOrDefaultAsync();
            return data;
        });

        public ValueTask<RisItem> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatchAsync(async () =>
        {
            var data = await db.RisItems.FindAsync(id);
            return data;
        });

        public IQueryable<RisItemVM> GetByRisId(Guid? risId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = db.RisItems.Where(w => w.RisId == risId)
                .Select(s => new RisItemVM
                {
                    Id = s.Id,
                    RisId = s.RisId,
                    ItemCodeId = s.ItemCodeId,
                    ItemCode = s.ItemCode.Code,
                    ItemType = s.ItemCode.Description,
                    PsType = s.ItemCode.ItemType.Code,
                    PsNo = s.PsNo,
                    PsNoDisplay = s.PsNoDisplay,
                    Unit = s.Unit,
                    ItemName = s.ItemName,
                    Description = s.Description,
                    QtyRequest = s.QtyRequest,
                    QtyIssue = s.QtyIssue,
                    Remarks = s.Remarks,
                    InsertedDt = s.InsertedDt,
                    Department = s.RISs.Office,
                    IsPosted = s.RISs.PostedDt != null
                });
            return data;
        });

        public ValueTask<RisItemVM> CreateAsync(RisItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            model.PsNo = PsNo(model);
            model.PsNoDisplay = PsNoDisplay(model);

            RisItem entity = new RisItem()
            {
                Id = model.Id,
                RisId = model.RisId,
                ItemCodeId = model.ItemCodeId,
                PsNo = model.PsNo,
                PsNoDisplay = model.PsNoDisplay,
                ItemName = model.ItemName,
                Unit = model.Unit,
                Description = model.Description,
                QtyRequest = model.QtyRequest,
                QtyIssue = model.QtyIssue,
                Remarks = model.Remarks ?? "",
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            db.RisItems.Add(entity);
            await db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RisItemVM> DeleteAsync(RisItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RisItem entity = await db.RisItems.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.RisItems.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.RisItems.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RisItemVM> UpdateAsync(RisItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RisItem entity = await db.RisItems.FindAsync(model.Id);

            model.PsNo = PsNo(model);
            model.PsNoDisplay = PsNoDisplay(model);

            entity.RisId = model.RisId;
            entity.ItemCodeId = model.ItemCodeId;
            entity.PsNo = model.PsNo;
            entity.PsNoDisplay = model.PsNoDisplay;
            entity.ItemName = model.ItemName;
            entity.Unit = model.Unit;
            entity.Description = model.Description;
            entity.QtyRequest = model.QtyRequest;
            entity.QtyIssue = model.QtyIssue;
            entity.Remarks = model.Remarks ?? "";
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.RisItems.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            // cascade updates
            // PR, Description, Qty
            // PO, Description, Qty
            // AIR, Qty            

            return model;
        });

        private string PsNo(RisItemVM model)
        {
            var risItemExtns = (List<RisItemExtnVM>)Newtonsoft.Json.JsonConvert.DeserializeObject(model.GridRisItemExtns, typeof(List<RisItemExtnVM>));
            string psNo = model.ItemCode.Trim(); // + model.ItemName.Substring(0, 1) + model.ItemName.Substring(2, 1);
            var itemType = db.ItemTypes.Where(w => w.Code == model.PsType).FirstOrDefault();
            string itemValue = "";
            for(var x = 1; x <= itemType.FormulaNo; x++)
            {
                itemValue = risItemExtns.FirstOrDefault(f => f.ItemNo == x.ToString())?.ItemValue.Replace(" ", "").Trim();
                if (string.IsNullOrWhiteSpace(itemValue))
                {
                    psNo += "XXX";
                } else {
                    if (x == 1)
                    {
                        if (itemValue.Length >= 3)
                        {
                            psNo += itemValue.Substring(0, 1) + itemValue.Substring(2, 1);
                        }
                        else
                        {
                            psNo += itemValue.Substring(0, 1) + "X";
                        }
                    }
                    else if (x == 2)
                    {
                        psNo += itemValue;
                    }
                    else if (x == 3)
                    {
                        psNo += itemValue.PadRight(3, 'X').Substring(0, 3);                        
                    }
                }
            }

            //if (model.PsType == "M")
            //{
            //    var ds = risItemExtns.FirstOrDefault(f => f.ItemKey == "Dosage Strength");
            //    if (ds != null)
            //    {
            //        psNo += ds.ItemValue.Replace(" ", "");
            //    }

            //    var df = risItemExtns.FirstOrDefault(f => f.ItemKey == "Dosage Form");
            //    if (df != null)
            //    {
            //        psNo += df.ItemValue.Substring(0, 3);
            //    }
            //}

            return psNo;
        }

        private string PsNoDisplay(RisItemVM model)
        {
            return model.ItemCode.Trim() + model.ItemName.Substring(0, 1) + model.ItemName.Substring(2, 1);
        }

        //#region EXCEPTIONS

        //private delegate ValueTask NonReturningFunction();
        //private delegate ValueTask<RisItemVM> ReturningVMFunction();
        //private delegate ValueTask<RisItem> ReturningFunction();
        //private delegate IQueryable<RisItemVM> ReturningQueryableVMFunction();
        //private delegate IQueryable<RisItem> ReturningQueryableFunction();

        //private async ValueTask TryCatch(NonReturningFunction nonReturningFunction)
        //{
        //    try
        //    {
        //        await nonReturningFunction();
        //    }
        //    catch (RecordNotFoundException notFoundException)
        //    {
        //        throw notFoundException;
        //    }
        //    catch (RecordAlreadyExistsException recordAlreadyExistsException)
        //    {
        //        throw recordAlreadyExistsException;
        //    }
        //    catch (InvalidValueException invalidValueException)
        //    {
        //        throw invalidValueException;
        //    }
        //    catch (RecordAlreadyPostedException recordAlreadyPostedException)
        //    {
        //        throw recordAlreadyPostedException;
        //    }
        //    catch (RecordRelationshipException recordRelationshipExistsException)
        //    {
        //        throw recordRelationshipExistsException;
        //    }
        //    catch (SqlException sqlException)
        //    {
        //        throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
        //    }
        //    catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
        //    {
        //        var recordLockedException = new RecordLockedException(dbUpdateConcurrencyException);

        //        throw exceptions.CreateAndLogDependencyException(recordLockedException);
        //    }
        //    catch (DbUpdateException dbUpdateException)
        //    {
        //        throw exceptions.CreateAndLogDependencyException(dbUpdateException);
        //    }
        //    catch (Exception exception)
        //    {
        //        var failedServiceException =
        //            new FailedServiceException(exception);

        //        throw exceptions.CreateAndLogServiceException(failedServiceException);
        //    }
        //}
        //private async ValueTask<RisItemVM> TryCatch(ReturningVMFunction returningVMFunction)
        //{
        //    try
        //    {
        //        return await returningVMFunction();
        //    }
        //    catch (RecordNotFoundException notFoundException)
        //    {
        //        throw notFoundException;
        //    }
        //    catch (RecordAlreadyExistsException recordAlreadyExistsException)
        //    {
        //        throw recordAlreadyExistsException;
        //    }
        //    catch (InvalidValueException invalidValueException)
        //    {
        //        throw invalidValueException;
        //    }
        //    catch (RecordAlreadyPostedException recordAlreadyPostedException)
        //    {
        //        throw recordAlreadyPostedException;
        //    }
        //    catch (RecordRelationshipException recordRelationshipExistsException)
        //    {
        //        throw recordRelationshipExistsException;
        //    }
        //    catch (SqlException sqlException)
        //    {
        //        throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
        //    }
        //    catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
        //    {
        //        var recordLockedException = new RecordLockedException(dbUpdateConcurrencyException);

        //        throw exceptions.CreateAndLogDependencyException(recordLockedException);
        //    }
        //    catch (DbUpdateException dbUpdateException)
        //    {
        //        throw exceptions.CreateAndLogDependencyException(dbUpdateException);
        //    }
        //    catch (Exception exception)
        //    {
        //        var failedServiceException =
        //            new FailedServiceException(exception);

        //        throw exceptions.CreateAndLogServiceException(failedServiceException);
        //    }
        //}
        //private async ValueTask<RisItem> TryCatch(ReturningFunction returningFunction)
        //{
        //    try
        //    {
        //        return await returningFunction();
        //    }
        //    catch (RecordNotFoundException notFoundException)
        //    {
        //        throw notFoundException;
        //    }
        //    catch (RecordAlreadyExistsException recordAlreadyExistsException)
        //    {
        //        throw recordAlreadyExistsException;
        //    }
        //    catch (InvalidValueException invalidValueException)
        //    {
        //        throw invalidValueException;
        //    }
        //    catch (RecordAlreadyPostedException recordAlreadyPostedException)
        //    {
        //        throw recordAlreadyPostedException;
        //    }
        //    catch (RecordRelationshipException recordRelationshipExistsException)
        //    {
        //        throw recordRelationshipExistsException;
        //    }
        //    catch (SqlException sqlException)
        //    {
        //        throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
        //    }
        //    catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
        //    {
        //        var recordLockedException = new RecordLockedException(dbUpdateConcurrencyException);

        //        throw exceptions.CreateAndLogDependencyException(recordLockedException);
        //    }
        //    catch (DbUpdateException dbUpdateException)
        //    {
        //        throw exceptions.CreateAndLogDependencyException(dbUpdateException);
        //    }
        //    catch (Exception exception)
        //    {
        //        var failedServiceException =
        //            new FailedServiceException(exception);

        //        throw exceptions.CreateAndLogServiceException(failedServiceException);
        //    }
        //}
        //private IQueryable<RisItemVM> TryCatch(ReturningQueryableVMFunction returningQueryableVMFunction)
        //{
        //    try
        //    {
        //        return returningQueryableVMFunction();
        //    }
        //    catch (SqlException sqlException)
        //    {
        //        throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
        //    }
        //    catch (Exception exception)
        //    {
        //        var failedServiceException =
        //            new FailedServiceException(exception);

        //        throw exceptions.CreateAndLogServiceException(failedServiceException);
        //    }
        //}
        //private IQueryable<RisItem> TryCatch(ReturningQueryableFunction returningQueryableFunction)
        //{
        //    try
        //    {
        //        return returningQueryableFunction();
        //    }
        //    catch (SqlException sqlException)
        //    {
        //        throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
        //    }
        //    catch (Exception exception)
        //    {
        //        var failedServiceException =
        //            new FailedServiceException(exception);

        //        throw exceptions.CreateAndLogServiceException(failedServiceException);
        //    }
        //}
        //#endregion
    }
}