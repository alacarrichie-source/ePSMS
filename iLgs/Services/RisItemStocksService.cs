//using iLgs.Exceptions;
//using iLgs.Models;
//using iLgs.Services.Interfaces;
//using System;
//using System.Collections.Generic;
//using System.Data.Entity;
//using System.Data.Entity.Infrastructure;
//using System.Data.SqlClient;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Web;

//namespace iLgs.Services
//{
//    public interface IRisItemStocksService
//    {
//        IQueryable<RisItemStockVM> GetByRisId(Guid? risId);
//        ValueTask<RisItemStock> GetByIdAsync(Guid? id);
//        ValueTask<RisItemStockVM> GetVmByIdAsync(Guid? id);

//        string PsNoDisplay(string itemCode, string itemName);

//        ValueTask<RisItemStockVM> CreateAsync(RisItemStockVM model, string user, DateTime date);
//        ValueTask<RisItemStockVM> UpdateAsync(RisItemStockVM model, string user, DateTime date);
//        ValueTask<RisItemStockVM> DeleteAsync(RisItemStockVM model, string user, DateTime date);
//    }

//    public class RisItemStocksService : IRisItemStocksService
//    {
//        private readonly AppManEntities _db = new AppManEntities();
//        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
//        private readonly IExceptionService<RisItemStockVM> _vmExceptionService = new ExceptionService<RisItemStockVM>();
//        private readonly IExceptionService<RisItemStock> _exceptionService = new ExceptionService<RisItemStock>();

//        public RisItemStocksService(AppManEntities db)
//        {
//            _db = db;
//        }

//        public ValueTask<RisItemStockVM> GetVmByIdAsync(Guid? id) =>
//        _vmExceptionService.TryCatchAsync(async () =>
//        {
//            var data = await _db.RisItems.OfType<RisItemStock>().Where(w => w.Id == id)
//                .Select(s => new RisItemStockVM
//                {
//                    Id = s.Id,
//                    RisId = s.RisId,
//                    ItemCodeId = s.ItemCodeId,
//                    ItemCode = s.ItemCode.Code,
//                    ItemType = s.ItemCode.Description,
//                    PsType = s.ItemCode.ItemType.Code,
//                    PsNo = s.PsNo,
//                    PsNoDisplay = s.PsNoDisplay,
//                    Unit = s.Unit,
//                    ItemName = s.ItemName,
//                    Description = s.Description,
//                    OtherDesc = s.OtherDesc,
//                    QtyRequest = s.QtyRequest,
//                    QtyIssue = s.QtyIssue,
//                    Remarks = s.Remarks,
//                    InsertedDt = s.InsertedDt,
//                    Department = s.RISs.Office,
//                    IsPosted = s.RISs.PostedDt != null,
//                    GenericName = s.GenericName,
//                    DosageStrength = s.DosageStrength,
//                    DosageForm = s.DosageForm,
//                    Brand = s.Brand,
//                    Others = s.Others
//                }).FirstOrDefaultAsync();
//            return data;
//        });

//        public ValueTask<RisItemStock> GetByIdAsync(Guid? id) =>
//        _exceptionService.TryCatchAsync(async () =>
//        {
//            var data = await _db.RisItems.OfType<RisItemStock>().FirstOrDefaultAsync(f => f.Id == id);
//            return data;
//        });

//        public IQueryable<RisItemStockVM> GetByRisId(Guid? risId) =>
//        _vmExceptionService.TryCatch(() =>
//        {
//            var data = _db.RisItems.OfType<RisItemStock>().Where(w => w.RisId == risId)
//                .Select(s => new RisItemStockVM
//                {
//                    Id = s.Id,
//                    RisId = s.RisId,
//                    ItemCodeId = s.ItemCodeId,
//                    ItemCode = s.ItemCode.Code,
//                    ItemType = s.ItemCode.Description,
//                    PsType = s.ItemCode.ItemType.Code,
//                    PsNo = s.PsNo,
//                    PsNoDisplay = s.PsNoDisplay,
//                    Unit = s.Unit,
//                    ItemName = s.ItemName,
//                    Description = s.Description,
//                    OtherDesc = s.OtherDesc,
//                    QtyRequest = s.QtyRequest,
//                    QtyIssue = s.QtyIssue,
//                    Remarks = s.Remarks,
//                    InsertedDt = s.InsertedDt,
//                    Department = s.RISs.Office,
//                    IsPosted = s.RISs.PostedDt != null,
//                    GenericName = s.GenericName,
//                    DosageStrength = s.DosageStrength,
//                    DosageForm = s.DosageForm,
//                    Brand = s.Brand,
//                    Others = s.Others
//                });
//            return data;
//        });

//        public ValueTask<RisItemStockVM> CreateAsync(RisItemStockVM model, string user, DateTime date) =>
//        _vmExceptionService.TryCatchAsync(async () =>
//        {
//            model.Id = Guid.NewGuid();
//            model.InsertedBy = user;
//            model.UpdatedBy = user;
//            model.InsertedDt = date;
//            model.UpdatedDt = date;

//            model.PsNo = PsNo(model);
//            model.PsNoDisplay = PsNoDisplay(model.ItemCode, model.ItemName);

//            var entity = new RisItemStock()
//            {
//                Id = model.Id,
//                RisId = model.RisId,
//                ItemCodeId = model.ItemCodeId,
//                PsNo = model.PsNo,
//                PsNoDisplay = model.PsNoDisplay,
//                ItemName = model.ItemName,
//                Unit = model.Unit,
//                Description = model.Description,
//                OtherDesc = model.OtherDesc,
//                QtyRequest = model.QtyRequest,
//                QtyIssue = model.QtyIssue,
//                Remarks = model.Remarks ?? "",
//                InsertedBy = model.InsertedBy,
//                InsertedDt = model.InsertedDt,
//                UpdatedBy = model.UpdatedBy,
//                UpdatedDt = model.UpdatedDt,
//                GenericName = model.GenericName,
//                DosageStrength = model.DosageStrength,
//                DosageForm = model.DosageForm,
//                Brand = model.Brand,
//                Others = model.Others
//            };

//            _db.RisItems.Add(entity);
//            await _db.SaveChangesAsync();

//            return model;
//        });

//        public ValueTask<RisItemStockVM> DeleteAsync(RisItemStockVM model, string user, DateTime date) =>
//        _vmExceptionService.TryCatchAsync(async () =>
//        {
//            model.UpdatedBy = user;
//            model.UpdatedDt = date;

//            var entity = await _db.RisItems.OfType<RisItemStock>().SingleOrDefaultAsync(s => s.Id == model.Id);

//            entity.UpdatedBy = model.UpdatedBy;
//            entity.UpdatedDt = model.UpdatedDt;

//            _db.RisItems.Attach(entity);
//            _db.Entry(entity).State = EntityState.Modified;
//            await _db.SaveChangesAsync();

//            _db.RisItems.Remove(entity);
//            _db.Entry(entity).State = EntityState.Deleted;
//            await _db.SaveChangesAsync();

//            return model;
//        });

//        public ValueTask<RisItemStockVM> UpdateAsync(RisItemStockVM model, string user, DateTime date) =>
//        _vmExceptionService.TryCatchAsync(async () =>
//        {
//            model.UpdatedBy = user;
//            model.UpdatedDt = date;

//            var entity = await _db.RisItems.OfType<RisItemStock>().SingleOrDefaultAsync(s => s.Id == model.Id);

//            model.PsNo = PsNo(model);
//            model.PsNoDisplay = PsNoDisplay(model.ItemCode, model.ItemName);

//            entity.RisId = model.RisId;
//            entity.ItemCodeId = model.ItemCodeId;
//            entity.PsNo = model.PsNo;
//            entity.PsNoDisplay = model.PsNoDisplay;
//            entity.ItemName = model.ItemName;
//            entity.Unit = model.Unit;
//            entity.Description = model.Description;
//            entity.OtherDesc = model.OtherDesc;
//            entity.QtyRequest = model.QtyRequest;
//            entity.QtyIssue = model.QtyIssue;
//            entity.Remarks = model.Remarks ?? "";
//            entity.UpdatedBy = model.UpdatedBy;
//            entity.UpdatedDt = model.UpdatedDt;
//            entity.GenericName = model.GenericName;
//            entity.DosageStrength = model.DosageStrength;
//            entity.DosageForm = model.DosageForm;
//            entity.Brand = model.Brand;
//            entity.Others = model.Others;

//            _db.RisItems.Attach(entity);
//            _db.Entry(entity).State = EntityState.Modified;
//            await _db.SaveChangesAsync();

//            // cascade updates
//            // PR, Description, Qty
//            // PO, Description, Qty
//            // AIR, Qty            

//            return model;
//        });

//        private string PsNo(RisItemStockVM model)
//        {
//            var risItemExtns = (List<RisItemExtnVM>)Newtonsoft.Json.JsonConvert.DeserializeObject(model.GridRisItemExtns, typeof(List<RisItemExtnVM>));
//            string psNo = model.ItemCode.Trim();
//            var itemType = _db.ItemTypes.Where(w => w.Code == model.PsType).FirstOrDefault();
//            string itemValue = "";
//            for (var x = 1; x <= itemType.FormulaNo; x++)
//            {
//                itemValue = risItemExtns.FirstOrDefault(f => f.ItemNo == x.ToString())?.ItemValue.Replace(" ", "").Trim();
//                if (string.IsNullOrWhiteSpace(itemValue))
//                {
//                    psNo += "XXX";
//                }
//                else
//                {
//                    int itemCount = 0;
//                    if (model.PsType == "M")
//                    {
//                        var raItems = itemValue.Split('/');
//                        foreach (var raItem in raItems)
//                        {
//                            if (++itemCount > 1)
//                            {
//                                psNo += "/";
//                            }

//                            if (x == 1)
//                            {
//                                if (raItem.Length >= 3)
//                                {
//                                    psNo += raItem.Substring(0, 1) + raItem.Substring(2, 1);
//                                }
//                                else
//                                {
//                                    psNo += raItem.Substring(0, 1) + "X";
//                                }
//                            }
//                            else if (x == 2)
//                            {
//                                psNo += raItem;
//                            }
//                            else if (x == 3)
//                            {
//                                psNo += raItem.PadRight(3, 'X').Substring(0, 3);
//                            }
//                        }
//                    }
//                    else if (model.PsType == "L")
//                    {
//                        if (x == 1)
//                        {
//                            psNo += itemValue;
//                        }
//                        else if (x == 2)
//                        {
//                            psNo += itemValue.Substring(0, 1).ToUpper();
//                        }
//                        else if (x == 3)
//                        {
//                            psNo += itemValue.Substring(0, 1).ToUpper();
//                        }
//                        else if (x == 4)
//                        {
//                            psNo += itemValue.Substring(2, 2);
//                        }
//                        else if (x == 5)
//                        {
//                            psNo += itemValue.Replace(",", "");
//                        }
//                        else if (x == 6)
//                        {
//                            psNo += itemValue.Substring(0, 1).ToUpper();
//                        }
//                        else if (x == 7)
//                        {
//                            psNo += itemValue.Substring(itemValue.Length - 3);
//                        }
//                        else if (x == 8)
//                        {
//                            psNo += itemValue.Substring(0, 1).ToUpper();
//                        }
//                        else if (x == 9)
//                        {
//                            psNo += itemValue.Substring(2, 2);
//                        }
//                    }
//                }
//            }

//            //if (model.PsType == "M")
//            //{
//            //    var ds = risItemExtns.FirstOrDefault(f => f.ItemKey == "Dosage Strength");
//            //    if (ds != null)
//            //    {
//            //        psNo += ds.ItemValue.Replace(" ", "");
//            //    }

//            //    var df = risItemExtns.FirstOrDefault(f => f.ItemKey == "Dosage Form");
//            //    if (df != null)
//            //    {
//            //        psNo += df.ItemValue.Substring(0, 3);
//            //    }
//            //}

//            return psNo;
//        }

//        public string PsNoDisplay(string itemCode, string itemName)
//        {
//            var raItems = itemName.Replace(" ", "").Split('/');
//            int itemCount = 0;
//            string psNoDisplay = "";
//            foreach (var raItem in raItems)
//            {
//                if (++itemCount > 1)
//                {
//                    psNoDisplay += "/";
//                }
//                if (raItem.Length >= 3)
//                {
//                    psNoDisplay += raItem.Substring(0, 1) + raItem.Substring(2, 1);
//                }
//                else
//                {
//                    psNoDisplay += raItem.Substring(0, 1) + "X";
//                }
//            }
//            return itemCode.Trim() + psNoDisplay; // model.ItemName.Substring(0, 1) + model.ItemName.Substring(2, 1);
//        }

//        //#region EXCEPTIONS

//        //private delegate ValueTask NonReturningFunction();
//        //private delegate ValueTask<RisItemStockVM> ReturningVMFunction();
//        //private delegate ValueTask<RisItem> ReturningFunction();
//        //private delegate IQueryable<RisItemStockVM> ReturningQueryableVMFunction();
//        //private delegate IQueryable<RisItem> ReturningQueryableFunction();

//        //private async ValueTask TryCatch(NonReturningFunction nonReturningFunction)
//        //{
//        //    try
//        //    {
//        //        await nonReturningFunction();
//        //    }
//        //    catch (RecordNotFoundException notFoundException)
//        //    {
//        //        throw notFoundException;
//        //    }
//        //    catch (RecordAlreadyExistsException recordAlreadyExistsException)
//        //    {
//        //        throw recordAlreadyExistsException;
//        //    }
//        //    catch (InvalidValueException invalidValueException)
//        //    {
//        //        throw invalidValueException;
//        //    }
//        //    catch (RecordAlreadyPostedException recordAlreadyPostedException)
//        //    {
//        //        throw recordAlreadyPostedException;
//        //    }
//        //    catch (RecordRelationshipException recordRelationshipExistsException)
//        //    {
//        //        throw recordRelationshipExistsException;
//        //    }
//        //    catch (SqlException sqlException)
//        //    {
//        //        throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
//        //    }
//        //    catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
//        //    {
//        //        var recordLockedException = new RecordLockedException(dbUpdateConcurrencyException);

//        //        throw exceptions.CreateAndLogDependencyException(recordLockedException);
//        //    }
//        //    catch (DbUpdateException dbUpdateException)
//        //    {
//        //        throw exceptions.CreateAndLogDependencyException(dbUpdateException);
//        //    }
//        //    catch (Exception exception)
//        //    {
//        //        var failedServiceException =
//        //            new FailedServiceException(exception);

//        //        throw exceptions.CreateAndLogServiceException(failedServiceException);
//        //    }
//        //}
//        //private async ValueTask<RisItemStockVM> TryCatch(ReturningVMFunction returningVMFunction)
//        //{
//        //    try
//        //    {
//        //        return await returningVMFunction();
//        //    }
//        //    catch (RecordNotFoundException notFoundException)
//        //    {
//        //        throw notFoundException;
//        //    }
//        //    catch (RecordAlreadyExistsException recordAlreadyExistsException)
//        //    {
//        //        throw recordAlreadyExistsException;
//        //    }
//        //    catch (InvalidValueException invalidValueException)
//        //    {
//        //        throw invalidValueException;
//        //    }
//        //    catch (RecordAlreadyPostedException recordAlreadyPostedException)
//        //    {
//        //        throw recordAlreadyPostedException;
//        //    }
//        //    catch (RecordRelationshipException recordRelationshipExistsException)
//        //    {
//        //        throw recordRelationshipExistsException;
//        //    }
//        //    catch (SqlException sqlException)
//        //    {
//        //        throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
//        //    }
//        //    catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
//        //    {
//        //        var recordLockedException = new RecordLockedException(dbUpdateConcurrencyException);

//        //        throw exceptions.CreateAndLogDependencyException(recordLockedException);
//        //    }
//        //    catch (DbUpdateException dbUpdateException)
//        //    {
//        //        throw exceptions.CreateAndLogDependencyException(dbUpdateException);
//        //    }
//        //    catch (Exception exception)
//        //    {
//        //        var failedServiceException =
//        //            new FailedServiceException(exception);

//        //        throw exceptions.CreateAndLogServiceException(failedServiceException);
//        //    }
//        //}
//        //private async ValueTask<RisItem> TryCatch(ReturningFunction returningFunction)
//        //{
//        //    try
//        //    {
//        //        return await returningFunction();
//        //    }
//        //    catch (RecordNotFoundException notFoundException)
//        //    {
//        //        throw notFoundException;
//        //    }
//        //    catch (RecordAlreadyExistsException recordAlreadyExistsException)
//        //    {
//        //        throw recordAlreadyExistsException;
//        //    }
//        //    catch (InvalidValueException invalidValueException)
//        //    {
//        //        throw invalidValueException;
//        //    }
//        //    catch (RecordAlreadyPostedException recordAlreadyPostedException)
//        //    {
//        //        throw recordAlreadyPostedException;
//        //    }
//        //    catch (RecordRelationshipException recordRelationshipExistsException)
//        //    {
//        //        throw recordRelationshipExistsException;
//        //    }
//        //    catch (SqlException sqlException)
//        //    {
//        //        throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
//        //    }
//        //    catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
//        //    {
//        //        var recordLockedException = new RecordLockedException(dbUpdateConcurrencyException);

//        //        throw exceptions.CreateAndLogDependencyException(recordLockedException);
//        //    }
//        //    catch (DbUpdateException dbUpdateException)
//        //    {
//        //        throw exceptions.CreateAndLogDependencyException(dbUpdateException);
//        //    }
//        //    catch (Exception exception)
//        //    {
//        //        var failedServiceException =
//        //            new FailedServiceException(exception);

//        //        throw exceptions.CreateAndLogServiceException(failedServiceException);
//        //    }
//        //}
//        //private IQueryable<RisItemStockVM> TryCatch(ReturningQueryableVMFunction returningQueryableVMFunction)
//        //{
//        //    try
//        //    {
//        //        return returningQueryableVMFunction();
//        //    }
//        //    catch (SqlException sqlException)
//        //    {
//        //        throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
//        //    }
//        //    catch (Exception exception)
//        //    {
//        //        var failedServiceException =
//        //            new FailedServiceException(exception);

//        //        throw exceptions.CreateAndLogServiceException(failedServiceException);
//        //    }
//        //}
//        //private IQueryable<RisItem> TryCatch(ReturningQueryableFunction returningQueryableFunction)
//        //{
//        //    try
//        //    {
//        //        return returningQueryableFunction();
//        //    }
//        //    catch (SqlException sqlException)
//        //    {
//        //        throw exceptions.CreateAndLogCriticalDependencyException(sqlException);
//        //    }
//        //    catch (Exception exception)
//        //    {
//        //        var failedServiceException =
//        //            new FailedServiceException(exception);

//        //        throw exceptions.CreateAndLogServiceException(failedServiceException);
//        //    }
//        //}
//        //#endregion
//    }
//}