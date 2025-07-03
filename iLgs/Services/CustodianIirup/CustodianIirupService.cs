using ClosedXML.Excel;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.CustodianUploads;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.CustodianIirup
{
    public interface ICustodianIirupService
    {
        IQueryable<CustodianIIRUP> GetAll();
        ValueTask<CustodianIIRUP> GetByIdAsync(Guid? id);
        ValueTask<CustodianIIRUP> PostAsync(Guid id, string user, DateTime date);
        ValueTask<CustodianIIRUP> UnPostAsync(Guid id, string user, DateTime date);
        ValueTask<CustodianIIRUP> CreateAsync(CustodianIIRUP model, string user, DateTime date);
        ValueTask<CustodianIIRUP> UpdateAsync(CustodianIIRUP model, string user, DateTime date);
        ValueTask<CustodianIIRUP> DeleteAsync(CustodianIIRUP model, string user, DateTime date);
        MemoryStream ProcessExcelFile(Guid iirupId, string templateFilePath);
    }

    public class CustodianIirupService : BaseValidator, ICustodianIirupService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<CustodianIIRUP> _exceptionService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        public CustodianIirupService(AppManEntities db, ICreateAndLogExceptions createAndLogExceptions, 
            IExceptionService<CustodianIIRUP> exceptionService)
        {
            _db = db;
            _exceptions = createAndLogExceptions;
            _exceptionService = exceptionService;
            _getDisplayName = Utility.GetDisplayName<CustodianIIRUP>;
        }

        public IQueryable<CustodianIIRUP> GetAll() =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.CustodianIIRUPs.AsNoTracking().AsQueryable();
            return data;
        });

        public ValueTask<CustodianIIRUP> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.CustodianIIRUPs.FindAsync(id);
            return data;
        });

        
        public ValueTask<CustodianIIRUP> PostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.CustodianIIRUPs.FindAsync(id);

            ValidateRecord(entity);
            ValidateIfPosted(entity);
            ValidateFields(entity, Mode.POST);

            ICustodianIirupUploadService uploadService = new CustodianIirupUploadService(_db);
            var irrupItems = await _db.CustodianIirupItems.Include(i => i.CustodianDisposal).Where(w => w.CustodianIirupId == entity.Id).ToListAsync();
            foreach(var item in irrupItems)
            {
                if (!uploadService.GetAllByImageId(item.Id).Any())
                {
                    throw new NotFoundException($"No uploaded images found for Transmittal No. [{item.CustodianDisposal.TransmittalNo}], cannot post!");
                }
            }
            
            entity.PostedBy = user;
            entity.PostedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.CustodianIIRUPs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        public ValueTask<CustodianIIRUP> UnPostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.CustodianIIRUPs.FindAsync(id);
            ValidateRecord(entity);
            ValidateIfNotPosted(entity);
            
            entity.PostedBy = "";
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.CustodianIIRUPs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        public ValueTask<CustodianIIRUP> CreateAsync(CustodianIIRUP model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateFields(model, Mode.ADD);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new CustodianIIRUP();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.CustodianIIRUPs.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<CustodianIIRUP> UpdateAsync(CustodianIIRUP model, string user, DateTime date) =>
       _exceptionService.TryCatch(async () =>
       {
           ValidateIfNull(model);

           var entity = await _db.CustodianIIRUPs.FindAsync(model.Id);
           ValidateRecord(entity);
           ValidateIfPosted(entity);
           ValidateFields(model, Mode.EDIT);

           model.UpdatedBy = user;
           model.UpdatedDt = date;

           MapModelToEntityFields(entity, model, Mode.EDIT);

           _db.CustodianIIRUPs.Attach(entity);
           _db.Entry(entity).State = EntityState.Modified;
           await _db.SaveChangesAsync();
           return model;
       });

        public ValueTask<CustodianIIRUP> DeleteAsync(CustodianIIRUP model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);

            var entity = await _db.CustodianIIRUPs.Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            ValidateRecord(entity);
            ValidateIfPosted(entity);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.CustodianIIRUPs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.CustodianIIRUPs.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public void MapModelToEntityFields(CustodianIIRUP entity, CustodianIIRUP model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            entity.AsOf = model.AsOf;
            entity.Chairman = model.Chairman;
            entity.ChairmanTitle = model.ChairmanTitle;
            entity.ViceChairman = model.ViceChairman;
            entity.ViceChairmanTitle = model.ViceChairmanTitle;
            entity.Member = model.Member;
            entity.MemberTitle = model.Member;
            entity.Officer = model.Officer;
            entity.OfficerTitle = model.Officer;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        public MemoryStream ProcessExcelFile(Guid iirupId, string templateFilePath)
        {
            int row = 12;
            int col = 0;
            using (XLWorkbook wb = new XLWorkbook(templateFilePath))
            {
                var ws = wb.Worksheet(1);
                var reportItemList = _db.Database.SqlQuery<CustodianIirupExportVM>("Exec REPORTS_CustodianIirup {0}", iirupId)
                    .OrderBy(o => o.Code).ThenBy(t => t.Article)
                    .ToList();

                if (reportItemList.Any())
                {
                    int itemNo = 0;
                    string code = "";
                    string article = "";
                    var sw = 1;
                    foreach (var reportItem in reportItemList)
                    {
                        if (sw == 1)
                        {
                            ws.Row(6).Cell(1).SetValue(reportItem.AsOf);
                            sw = 0;
                        }

                        if (code != reportItem.Code) // Account
                        {
                            code = reportItem.Code;
                            row++;
                            ws.Row(row).Cell(4).SetValue(reportItem.Account);

                            article = reportItem.Article;
                            row++;
                            ws.Row(row).Cell(4).SetValue(reportItem.Article);                            
                        }
                        else
                        {
                            if (article != reportItem.Article)
                            {
                                article = reportItem.Article;
                                row += 2;
                                ws.Row(row).Cell(4).SetValue(reportItem.Article);                                
                            }
                        }
                        
                        row++;
                        col = 1;
                        ws.Row(row).Cell(++col).SetValue(++itemNo);
                        ws.Row(row).Cell(++col).SetValue("");
                        ws.Row(row).Cell(++col).SetValue($"{reportItem.Brand} / {reportItem.Model_} / {reportItem.Dimension} / " +
                            $"{reportItem.Size} / {reportItem.Weight} / {reportItem.Materials} / {reportItem.Color} / " +
                            $"{reportItem.Description} / {reportItem.OtherDesc} / {reportItem.SerialNo} / {reportItem.EngineNo} / " +
                            $"{reportItem.BodyNo} / {reportItem.MVFileNo}");
                        ++col;
                        ++col;
                        ws.Row(row).Cell(++col).SetValue(reportItem.Qty);
                        ws.Row(row).Cell(++col).SetValue(reportItem.EstimatedKg);
                        ws.Row(row).Cell(++col).SetValue(reportItem.ReplacementCost);
                        ws.Row(row).Cell(++col).SetValue(reportItem.UnitCost);
                        ++col;
                        ++col;
                        ws.Row(row).Cell(++col).SetValue(reportItem.AcqDate?.Month);
                        ws.Row(row).Cell(++col).SetValue(reportItem.AcqDate?.Day);
                        ws.Row(row).Cell(++col).SetValue(reportItem.AcqDate?.Year);
                        ++col;
                        ++col;
                        ws.Row(row).Cell(++col).SetValue($"{reportItem.Department.Trim()} {reportItem.RequestedDt}");
                    }
                }

                // Create a MemoryStream to save the output
                var memoryStream = new MemoryStream();
                wb.SaveAs(memoryStream);

                // Reset the stream position to the beginning before returning
                memoryStream.Position = 0;
                return memoryStream;
            }
        }

        private void ValidateFields(CustodianIIRUP model, Mode mode)
        {
            if (!model.AsOf.HasValue)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.AsOf)), "Field is required.");
            }

            if (mode == Mode.POST)
            {
                if (string.IsNullOrWhiteSpace(model.Chairman))
                {
                    var chairman = _getDisplayName(nameof(model.Chairman));
                    _imex.UpsertDataList(chairman, $"{chairman} Field is required.");
                }

                if (string.IsNullOrWhiteSpace(model.ChairmanTitle))
                {
                    var chairmanTitle = _getDisplayName(nameof(model.ChairmanTitle));
                    _imex.UpsertDataList(chairmanTitle, $"{chairmanTitle} Field is required.");
                }

                if (string.IsNullOrWhiteSpace(model.ViceChairman))
                {
                    var vice = _getDisplayName(nameof(model.ViceChairman));
                    _imex.UpsertDataList(vice, $"{vice} Field is required.");
                }

                if (string.IsNullOrWhiteSpace(model.ViceChairmanTitle))
                {
                    var viceTitle = _getDisplayName(nameof(model.ViceChairmanTitle));
                    _imex.UpsertDataList(viceTitle, $"{viceTitle} Field is required.");
                }

                if (string.IsNullOrWhiteSpace(model.Member))
                {
                    var member = _getDisplayName(nameof(model.Member));
                    _imex.UpsertDataList(member, $"{member} Field is required.");
                }

                if (string.IsNullOrWhiteSpace(model.MemberTitle))
                {
                    var memberTitle = _getDisplayName(nameof(model.MemberTitle));
                    _imex.UpsertDataList(memberTitle, $"{memberTitle} Field is required.");
                }

                if (string.IsNullOrWhiteSpace(model.Officer))
                {
                    var officer = _getDisplayName(nameof(model.Officer));
                    _imex.UpsertDataList(officer, $"{officer} Field is required.");
                }

                if (string.IsNullOrWhiteSpace(model.OfficerTitle))
                {
                    var officerTitle = _getDisplayName(nameof(model.OfficerTitle));
                    _imex.UpsertDataList(officerTitle, $"{officerTitle} Field is required.");
                }
            }

            _imex.ThrowIfContainsErrors();
        }

        private void ValidateIfNull(CustodianIIRUP model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(CustodianIIRUP entity)
        {
            if (entity == null)
            {
                throw new NotFoundException(entity.Id);
            }
        }

        private void ValidateIfPosted(CustodianIIRUP entity)
        {
            if (entity.PostedDt != null)
            {
                var msg = $"Record already posted by {entity.PostedBy} on {entity.PostedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        private void ValidateIfNotPosted(CustodianIIRUP entity)
        {
            if (entity.PostedDt == null)
            {
                throw new RecordNotYetPostedException($"Record is not yet posted!");
            }
        }
    }
}