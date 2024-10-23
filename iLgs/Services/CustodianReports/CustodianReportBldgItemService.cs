using ClosedXML.Excel;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.AllFields;
using iLgs.Services.CustodianUploads;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services.CustodianReports
{
    public interface ICustodianReportBldgItemService
    {
        string GetStockNo(CustodianReportBldgItem model);
        ValueTask<CustodianReportBldgItemVM> GetByIdAsync(Guid id);
        IQueryable<CustodianReportBldgItemVM> GetAll(Guid? reportId);
        IQueryable<CustodianReportBldgItemVM> GetAllByDeptAcctGroup(Guid? deptId, int? accountGroup);
        ValueTask<CustodianReportBldgItemVM> CreateAsync(CustodianReportBldgItemVM model, string user, DateTime date);
        ValueTask<CustodianReportBldgItemVM> UpdateAsync(CustodianReportBldgItemVM model, string user, DateTime date);
        ValueTask<CustodianReportBldgItemVM> DeleteAsync(CustodianReportBldgItemVM model, string user, DateTime date);
        ValueTask<CustodianReportBldgItem> PostAsync(Guid id, string user, DateTime date);
        ValueTask <CustodianReportBldgItem> UnPostAsync(Guid id, string user, DateTime date);
        MemoryStream ProcessExcelFile(Guid id, string templateFilePath);
    }

    public class CustodianReportBldgItemService : BaseValidator, ICustodianReportBldgItemService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<CustodianReportBldgItemVM> _vmExceptionService = new ExceptionService<CustodianReportBldgItemVM>();
        private readonly IExceptionService<CustodianReportBldgItem> _exceptionService = new ExceptionService<CustodianReportBldgItem>();
        private readonly ICustodianReportItemPpeValidator _validator;
        private readonly IAllFieldService _allFieldService;        
        private readonly GetDisplayNameDelegate _getDisplayName;

        public CustodianReportBldgItemService(AppManEntities db)
        {
            _db = db;
            _validator = new CustodianReportItemPpeValidator(_db);
            _allFieldService = new AllFieldService(_db);
            _getDisplayName = propertyName => Utility.GetDisplayName<CustodianReportBldgItemVM>(propertyName);
        }

        private static Expression<Func<CustodianReportBldgItem, CustodianReportBldgItemVM>> CustodianReportBldgItemProjection
        = s => new CustodianReportBldgItemVM
        {
            Id = s.Id,
            MainDeptId = s.CustodianReport.DeptId,
            AccountGroup = s.CustodianReport.AccountGroup,
            ReportId = s.ReportId,
            Fund = s.Fund,
            CustodianItemNo = s.CustodianItemNo,
            SeriesNo = s.SeriesNo,
            FromDonation = s.FromDonation,
            Account = s.Account,
            ItemCodeId = s.ItemCodeId,
            SubAccount = s.SubAccount,
            Article = s.Article,
            BldgItem = s.BldgItem,            
            PsNo = s.PsNo,
            PoNo = s.PoNo,
            PropNo = s.PropNo,
            OldAmount = s.OldAmount,
            PoDate = s.PoDate,
            AcqCost = s.AcqCost,
            DeptId = s.DeptId,
            Department = s.Department,
            LocationId = s.LocationId,
            LocationCode = s.LocationCode,
            Location = s.Location,
            SubLocation = s.SubLocation,
            AcqMonth = s.AcqMonth,
            AcqYear = s.AcqYear,
            AcqDay = s.AcqDay,
            AcqDate = s.AcqDate,
            Address = s.Address,
            ProjectName = s.ProjectName,
            BuildingType = s.BuildingType,
            Area = s.Area,
            TotalAmount = s.TotalAmount,
            PhaseNo = s.PhaseNo,
            PhaseAmountMooe = s.PhaseAmountMooe,
            PhaseAmountCo = s.PhaseAmountCo,
            StartYear = s.StartYear,
            StartMonth = s.StartMonth,
            StartDay = s.StartDay,
            StartDate = s.StartDate,
            TargetYear = s.TargetYear,
            TargetMonth = s.TargetMonth,
            TargetDay = s.TargetDay,
            TargetDate = s.TargetDate,
            PercentComplete = s.PercentComplete,
            CompletionYear = s.CompletionYear,
            CompletionMonth = s.CompletionMonth,
            CompletionDay = s.CompletionDay,
            CompletionDate = s.CompletionDate,
            Status = s.Status,
            Condition = s.Condition,
            Remarks = s.Remarks,
            Annex = s.Annex,
            InsertedBy = s.InsertedBy,
            InsertedDt = s.InsertedDt,
            PostedBy = s.PostedBy,
            PostedDt = s.PostedDt,
            Longitude = s.Longitude,
            Latitude = s.Latitude,
            ItemType_Code = s.ItemCode.ItemType.Code,
            Item_Code = s.ItemCode.Code
        };

        public string GetStockNo(CustodianReportBldgItem model)
        {
            model.AllField = SetAllField(model);
            return _allFieldService.GetCustodianStockNo(model);
        }

        public ValueTask<CustodianReportBldgItemVM> GetByIdAsync(Guid id) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.CustodianReportBldgItems
                .Where(w => w.Id == id)
                .Select(CustodianReportBldgItemProjection).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<CustodianReportBldgItemVM> GetAll(Guid? reportId)
        {
            var data = _db.CustodianReportBldgItems
                .AsNoTracking()
                .Where(w => w.ReportId == reportId)
                .Select(CustodianReportBldgItemProjection);
            return data;
        }

        public IQueryable<CustodianReportBldgItemVM> GetAllByDeptAcctGroup(Guid? deptId, int? accountGroup)
        {
            var data = _db.CustodianReportBldgItems
                .AsNoTracking()
                .Where(w => w.CustodianReport.DeptId == deptId && w.CustodianReport.AccountGroup == accountGroup)
                .Select(CustodianReportBldgItemProjection);

            return data;
        }

        private void ValidateRequired(CustodianReportBldgItemVM model)
        {
            if (string.IsNullOrWhiteSpace(model.PhaseNo))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.PhaseNo)), "Field is required.");
            }

            if (!model.PhaseAmountCo.HasValue)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.PhaseAmountCo)), "Field is required.");
            }

            _imex.ThrowIfContainsErrors();
        }

        private void ValidateIfNull(CustodianReportBldgItemVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        public ValueTask<CustodianReportBldgItemVM> CreateAsync(CustodianReportBldgItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateRequired(model);

            model.AllField = SetAllField(model);
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var custodianReport = await _db.CustodianReports.Where(w => w.DeptId == model.MainDeptId && w.AccountGroup == model.AccountGroup).SingleOrDefaultAsync();
            if (custodianReport == null)
            {
                custodianReport = new CustodianReport();
                custodianReport.Id = Guid.NewGuid();
                custodianReport.DeptId = model.MainDeptId;
                custodianReport.AccountGroup = model.AccountGroup;
                custodianReport.InsertedBy = user;
                custodianReport.InsertedDt = date;
                custodianReport.UpdatedBy = user;
                custodianReport.UpdatedDt = date;
                _db.CustodianReports.Add(custodianReport);
                await _db.SaveChangesAsync();
            }

            model.ReportId = custodianReport.Id;

            var entity = new CustodianReportBldgItem();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.CustodianReportBldgItems.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<CustodianReportBldgItemVM> UpdateAsync(CustodianReportBldgItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateRequired(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.CustodianReportBldgItems.FindAsync(model.Id);
            ValidateRecord(entity);
            ValidateIfPosted(entity);

            MapModelToEntityFields(entity, model, Mode.EDIT);

            _db.CustodianReportBldgItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<CustodianReportBldgItemVM> DeleteAsync(CustodianReportBldgItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.CustodianReportBldgItems.FindAsync(model.Id);
            ValidateRecord(entity);
            ValidateIfPosted(entity);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.CustodianReportBldgItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.CustodianReportBldgItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<CustodianReportBldgItem> PostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.CustodianReportBldgItems.FindAsync(id);
            ValidateRecord(entity);
            ValidateIfPosted(entity);

            ICustodianBldgUploadService uploadService = new CustodianBldgUploadService(_db);
            if (!uploadService.GetAllByImageId(id).Any())
            {
                throw new NotFoundException("No uploaded images found for this record, cannot post!");
            }
            
            entity.PostedBy = user;
            entity.PostedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.CustodianReportBldgItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        public ValueTask<CustodianReportBldgItem> UnPostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.CustodianReportBldgItems.FindAsync(id);
            ValidateRecord(entity);
            ValidateIfNotPosted(entity);

            entity.PostedBy = "";
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.CustodianReportBldgItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        private AllField SetAllField(CustodianReportBldgItem custodianReportItem)
        {
            AllField allField = new AllField()
            {
                Area = custodianReportItem.Area

            };
            return allField;
        }

        private void MapFormattedAllField(CustodianReportBldgItem model)
        {
            model.AllField = SetAllField(model);
            model.Area = model.AllField.Area;
        }

        private void MapModelToEntityFields(CustodianReportBldgItem entity, CustodianReportBldgItem model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            MapFormattedAllField(model);
            entity.Id = model.Id;
            entity.ReportId = model.ReportId;
            entity.Fund = model.Fund;
            entity.CustodianItemNo = model.CustodianItemNo;
            entity.SeriesNo = model.SeriesNo;
            entity.FromDonation = model.FromDonation;
            entity.Account = model.Account;
            entity.ItemCodeId = model.ItemCodeId;
            entity.SubAccount = model.SubAccount;
            entity.Article = model.Article;
            entity.BldgItem = model.BldgItem;
            entity.PoNo = model.PoNo;
            entity.PoDate = model.PoDate;
            entity.AcqCost = model.AcqCost;
            entity.DeptId = model.DeptId;
            entity.Department = model.Department;
            entity.LocationId = model.LocationId;
            entity.LocationCode = model.LocationCode;
            entity.SubLocation = model.SubLocation;
            entity.Location = model.Location;
            entity.AcqMonth = model.AcqMonth;
            entity.AcqYear = model.AcqYear;
            entity.AcqDay = model.AcqDay;
            entity.AcqDate = model.AcqDate;
            entity.PsNo = model.PsNo;
            entity.PropNo = model.PropNo;
            entity.OldAmount = model.OldAmount;
            entity.Address = model.Address;
            entity.ProjectName = model.ProjectName;
            entity.BuildingType = model.BuildingType;
            entity.Area = model.Area;
            entity.TotalAmount = model.TotalAmount;
            entity.PhaseNo = model.PhaseNo;
            entity.PhaseAmountMooe = model.PhaseAmountMooe;
            entity.PhaseAmountCo = model.PhaseAmountCo;
            entity.StartYear = model.StartYear;
            entity.StartMonth = model.StartMonth;
            entity.StartDay = model.StartDay;
            entity.StartDate = model.StartDate;
            entity.TargetYear = model.TargetYear;
            entity.TargetMonth = model.TargetMonth;
            entity.TargetDay = model.TargetDay;
            entity.TargetDate = model.TargetDate;
            entity.PercentComplete = model.PercentComplete;
            entity.CompletionYear = model.CompletionYear;
            entity.CompletionMonth = model.CompletionMonth;
            entity.CompletionDay = model.CompletionDay;
            entity.CompletionDate = model.CompletionDate;
            entity.Status = model.Status;
            entity.Condition = model.Condition;
            entity.Remarks = model.Remarks;
            entity.Annex = model.Annex;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
            entity.PostedBy = model.PostedBy;
            entity.PostedDt = model.PostedDt;
            entity.Latitude = model.Latitude;
            entity.Longitude = model.Longitude;
        }

        public MemoryStream ProcessExcelFile(Guid id, string templateFilePath)
        {
            // Load the template file
            FileInfo templateFile = new FileInfo(templateFilePath);
            if (!templateFile.Exists)
            {
                throw new FileNotFoundException("The template file does not exist.", templateFilePath);
            }
            return ProcessExcelFileTemplate(id, templateFilePath);
        }

        private MemoryStream ProcessExcelFileTemplate(Guid id, string templateFilePath)
        {
            int row = 9;
            int col = 0;
            using (XLWorkbook wb = new XLWorkbook(templateFilePath))
            {
                var ws = wb.Worksheet(1);
                var reportItemList = _db.CustodianReportBldgItems.Where(w => w.ReportId == id).ToList();
                foreach (var reportItem in reportItemList)
                {
                    row++;
                    col = 0;
                    ws.Row(row).Cell(++col).SetValue(reportItem.CustodianItemNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.LocationCode);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SeriesNo);
                    ws.Row(row).Cell(++col).SetValue($"{reportItem.SubAccount} / {reportItem.Article}");
                    ws.Row(row).Cell(++col).SetValue(reportItem.BldgItem);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Location);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SubLocation);
                    ws.Row(row).Cell(++col).SetValue(reportItem.ProjectName);
                    ws.Row(row).Cell(++col).SetValue(reportItem.BuildingType);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Area);
                    if (reportItem.FromDonation == true)
                    {
                        ws.Row(row).Cell(++col).SetValue("From Donation");
                    }
                    else
                    {
                        ws.Row(row).Cell(++col).SetValue("Purchased");
                    }
                    ws.Row(row).Cell(++col).SetValue(reportItem.AcqDate);
                    //ws.Row(row).Cell(++col).SetValue($"{reportItem.AcqYear}/{reportItem.AcqMonth}/{reportItem.AcqDay}");
                    ws.Row(row).Cell(++col).SetValue(reportItem.PropNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.AcqCost);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OldAmount);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PhaseNo);                                        
                    ws.Row(row).Cell(++col).SetValue(reportItem.PhaseAmountCo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.TotalAmount);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PhaseAmountMooe);
                    ws.Row(row).Cell(++col).SetValue(reportItem.StartDate);
                    //ws.Row(row).Cell(++col).SetValue($"{reportItem.StartYear}/{reportItem.StartMonth}/{reportItem.StartDay}");
                    ws.Row(row).Cell(++col).SetValue($"{reportItem.TargetMonth}/{reportItem.TargetYear}");
                    ws.Row(row).Cell(++col).SetValue(reportItem.PercentComplete);
                    //ws.Row(row).Cell(++col).SetValue($"{reportItem.CompletionYear}/{reportItem.CompletionMonth}/{reportItem.CompletionDay}");                    
                    ws.Row(row).Cell(++col).SetValue(reportItem.CompletionDate);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Status);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Fund);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Condition);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Remarks);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Annex);
                }

                // Create a MemoryStream to save the output
                var memoryStream = new MemoryStream();
                wb.SaveAs(memoryStream);

                // Reset the stream position to the beginning before returning
                memoryStream.Position = 0;
                return memoryStream;
            }
        }

        private void ValidateRecord(CustodianReportBldgItem entity)
        {
            if (entity == null)
            {
                throw new NotFoundException(entity.Id);
            }
        }

        private void ValidateIfPosted(CustodianReportBldgItem entity)
        {
            if (entity.PostedDt != null)
            {
                var msg = $"Record already posted by {entity.PostedBy} on {entity.PostedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        private void ValidateIfNotPosted(CustodianReportBldgItem entity)
        {
            if (entity.PostedDt == null)
            {
                throw new RecordNotYetPostedException($"Record is not yet posted!");
            }
        }        
    }
}