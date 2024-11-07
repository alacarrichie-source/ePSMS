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
    public interface ICustodianReportLandItemService
    {
        string GetStockNo(CustodianReportLandItem model);
        ValueTask<CustodianReportLandItemVM> GetByIdAsync(Guid id);
        IQueryable<CustodianReportLandItemVM> GetAll(Guid? reportId);
        IQueryable<CustodianReportLandItemVM> GetAllByDeptAcctGroup(Guid? deptId, int? accountGroup);
        ValueTask<CustodianReportLandItemVM> CreateAsync(CustodianReportLandItemVM model, string user, DateTime date);
        ValueTask<CustodianReportLandItemVM> UpdateAsync(CustodianReportLandItemVM model, string user, DateTime date);
        ValueTask<CustodianReportLandItemVM> DeleteAsync(CustodianReportLandItemVM model, string user, DateTime date);
        ValueTask<CustodianReportLandItemVM> PostAsync(Guid id, string user, DateTime date);
        ValueTask<CustodianReportLandItemVM> UnPostAsync(Guid id, string user, DateTime date);
        MemoryStream ProcessExcelFile(Guid id, string templateFilePath);
        MemoryStream ProcessExcelAnnexFile(Guid id, string templateFilePath);
    }

    public class CustodianReportLandItemService : BaseValidator, ICustodianReportLandItemService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<CustodianReportLandItemVM> _exceptionService = new ExceptionService<CustodianReportLandItemVM>();
        private readonly IAllFieldService _allFieldService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        public CustodianReportLandItemService(AppManEntities db)
        {
            _db = db;
            _allFieldService = new AllFieldService(_db);
            _getDisplayName = propertyName => Utility.GetDisplayName<CustodianReportLandItemVM>(propertyName);
        }

        private static Expression<Func<CustodianReportLandItem, CustodianReportLandItemVM>> CustodianReportLandItemProjection
        = s => new CustodianReportLandItemVM
        {
            Id = s.Id,
            MainDeptId = s.CustodianReport.DeptId,
            AccountGroup = s.CustodianReport.AccountGroup,
            ReportId = s.ReportId,
            Fund = s.Fund,
            PIN = s.PIN,
            CustodianItemNo = s.CustodianItemNo,
            SeriesNo = s.SeriesNo,
            Type = s.Type,
            Condition = s.Condition,
            Description = s.Description,
            LocationId = s.LocationId,
            LocationCode = s.LocationCode,
            Location = s.Location, // Barangay
            SubLocation = s.SubLocation,
            Address = s.Address,
            LandMarks = s.LandMarks,
            Area = s.Area,
            PricePerSqm = s.PricePerSqm,
            MarketValue = s.MarketValue,
            PsNo = s.PsNo,
            PropNo = s.PropNo,
            OldPropNo = s.OldPropNo,
            OldAmount = s.OldAmount,
            AcqCost = s.AcqCost,
            AcqDate = s.AcqDate,
            FromDonation = s.FromDonation,
            Vendor = s.Vendor,
            Representative = s.Representative,
            TctNo = s.TctNo,
            OldTctNo = s.OldTctNo,
            DRPNo = s.DRPNo,
            DRPDate = s.DRPDate,
            OldDRPNo = s.OldDRPNo,
            OldDRPDate = s.OldDRPDate,
            Remarks = s.Remarks,
            CGT = s.CGT,
            CGTCompromise = s.CGTCompromise,
            CGTInterest = s.CGTInterest,
            CGTSurcharge = s.CGTInterest,
            CGTTransferTax = s.CGTTransferTax,
            DST = s.DST,
            DSTCompromise = s.DSTCompromise,
            DSTInterest = s.DSTInterest,
            DSTSurcharge = s.DSTSurcharge,
            DSTTransferTax = s.DSTTransferTax,
            TransferTax = s.TransferTax,
            Surcharge = s.Surcharge,
            Interest = s.Interest,
            ConfirmationFee = s.ConfirmationFee,
            TransferRegsFee = s.TransferRegsFee,
            RealPropertyFee = s.RealPropertyFee,
            VAT = s.VAT,
            EstateFee = s.EstateFee,
            Titling = s.Titling,
            CertificationFee = s.CertificationFee,
            Relocation = s.Relocation,
            Surveying = s.Surveying,
            IncidentalExpenses = s.IncidentalExpenses,
            Account = s.Account,
            ItemCodeId = s.ItemCodeId,
            SubAccount = s.SubAccount,
            Article = s.Article,
            Annex = s.Annex,
            InsertedBy = s.InsertedBy,
            InsertedDt = s.InsertedDt,
            PostedBy = s.PostedBy,
            PostedDt = s.PostedDt,
            ItemType_Code = s.ItemCode.ItemType.Code,
            Item_Code = s.ItemCode.Code
        };

        public string GetStockNo(CustodianReportLandItem model)
        {
            model.AllField = SetAllField(model);
            return _allFieldService.GetCustodianStockNo(model);
        }

        public ValueTask<CustodianReportLandItemVM> GetByIdAsync(Guid id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.CustodianReportLandItems
                .Where(w => w.Id == id)
                .Select(CustodianReportLandItemProjection).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<CustodianReportLandItemVM> GetAll(Guid? reportId)
        {
            var data = _db.CustodianReportLandItems
                .AsNoTracking()
                .Where(w => w.ReportId == reportId)
                .Select(CustodianReportLandItemProjection);
            return data;
        }

        public IQueryable<CustodianReportLandItemVM> GetAllByDeptAcctGroup(Guid? deptId, int? accountGroup)
        {
            var data = _db.CustodianReportLandItems
                .AsNoTracking()
                .Where(w => w.CustodianReport.DeptId == deptId && w.CustodianReport.AccountGroup == accountGroup)
                .Select(CustodianReportLandItemProjection);

            return data;
        }

        public ValueTask<CustodianReportLandItemVM> CreateAsync(CustodianReportLandItemVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
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

            var entity = new CustodianReportLandItem();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.CustodianReportLandItems.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<CustodianReportLandItemVM> UpdateAsync(CustodianReportLandItemVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateRequired(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.CustodianReportLandItems.FindAsync(model.Id);
            ValidateRecord(entity);
            ValidateIfPosted(entity);

            MapModelToEntityFields(entity, model, Mode.EDIT);

            _db.CustodianReportLandItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<CustodianReportLandItemVM> DeleteAsync(CustodianReportLandItemVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.CustodianReportLandItems.FindAsync(model.Id);
            ValidateRecord(entity);
            ValidateIfPosted(entity);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.CustodianReportLandItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.CustodianReportLandItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<CustodianReportLandItemVM> PostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.CustodianReportLandItems.FindAsync(id);
            ValidateRecord(entity);
            ValidateIfPosted(entity);

            ICustodianLandUploadService uploadService = new CustodianLandUploadService(_db);
            if (!uploadService.GetAllByImageId(id).Any())
            {
                throw new NotFoundException("No uploaded images found for this record, cannot post!");
            }

            entity.PostedBy = user;
            entity.PostedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.CustodianReportLandItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return (CustodianReportLandItemVM)entity;
        });

        public ValueTask<CustodianReportLandItemVM> UnPostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.CustodianReportLandItems.FindAsync(id);
            ValidateRecord(entity);
            ValidateIfNotPosted(entity);

            entity.PostedBy = "";
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.CustodianReportLandItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return (CustodianReportLandItemVM)entity;
        });

        private AllField SetAllField(CustodianReportLandItem custodianReportItem)
        {
            AllField allField = new AllField()
            {
                Area = custodianReportItem.Area

            };
            return allField;
        }

        private void MapFormattedAllField(CustodianReportLandItem model)
        {
            model.AllField = SetAllField(model);
            model.Area = model.AllField.Area;
        }

        private void MapModelToEntityFields(CustodianReportLandItem entity, CustodianReportLandItem model, Mode mode)
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
            entity.LocationId = model.LocationId;
            entity.LocationCode = model.LocationCode;
            entity.Location = model.Location;
            entity.Type = model.Type;
            entity.Condition = model.Condition;
            entity.Description = model.Description;
            entity.SubLocation = model.SubLocation;
            entity.PIN = model.PIN;
            entity.Address = model.Address;
            entity.LandMarks = model.LandMarks;
            entity.Area = model.Area;
            entity.PricePerSqm = model.PricePerSqm;
            entity.MarketValue = model.MarketValue;
            entity.PsNo = model.PsNo;
            entity.PropNo = model.PropNo;
            entity.OldPropNo = model.OldPropNo;
            entity.AcqCost = model.AcqCost;
            entity.OldAmount = model.OldAmount;
            entity.AcqDate = model.AcqDate;
            entity.Vendor = model.Vendor;
            entity.Representative = model.Representative;
            entity.TctNo = model.TctNo;
            entity.OldTctNo = model.OldTctNo;
            entity.DRPNo = model.DRPNo;
            entity.DRPDate = model.DRPDate;
            entity.OldDRPNo = model.OldDRPNo;
            entity.OldDRPDate = model.OldDRPDate;
            entity.Remarks = model.Remarks;
            entity.CGT = model.CGT;
            entity.CGTTransferTax = model.CGTTransferTax;
            entity.CGTSurcharge = model.CGTSurcharge;
            entity.CGTInterest = model.CGTInterest;
            entity.CGTCompromise = model.CGTCompromise;
            entity.DST = model.DST;
            entity.DSTTransferTax = model.DSTTransferTax;
            entity.DSTSurcharge = model.DSTSurcharge;
            entity.DSTInterest = model.DSTInterest;
            entity.DSTCompromise = model.DSTCompromise;
            entity.TransferTax = model.TransferTax;
            entity.Surcharge = model.Surcharge;
            entity.Interest = model.Interest;
            entity.ConfirmationFee = model.ConfirmationFee;
            entity.TransferRegsFee = model.TransferRegsFee;
            entity.RealPropertyFee = model.RealPropertyFee;
            entity.VAT = model.VAT;
            entity.EstateFee = model.EstateFee;
            entity.Titling = model.Titling;
            entity.CertificationFee = model.CertificationFee;
            entity.Relocation = model.Relocation;
            entity.Surveying = model.Surveying;
            entity.IncidentalExpenses = model.IncidentalExpenses;
            entity.CapitalOutlayOrExpense = model.CapitalOutlayOrExpense;
            entity.Annex = model.Annex;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
            entity.PostedBy = model.PostedBy;
            entity.PostedDt = model.PostedDt;
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
            int row = 8;
            int col = 0;
            using (XLWorkbook wb = new XLWorkbook(templateFilePath))
            {
                var ws = wb.Worksheet(1);
                var reportItemList = _db.CustodianReportLandItems.Include(i => i.CustodianReport.Codextn).Where(w => w.ReportId == id).ToList();
                foreach (var reportItem in reportItemList)
                {
                    ws.Row(4).Cell(2).SetValue(reportItem.CustodianReport.Codextn.Description);
                    row++;
                    col = 0;
                    ws.Row(row).InsertRowsBelow(1);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PIN);
                    ws.Row(row).Cell(++col).SetValue(reportItem.CustodianItemNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.LocationCode);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SeriesNo);
                    ws.Row(row).Cell(++col).SetValue($"{reportItem.Type} / {reportItem.Condition} / {reportItem.Description}");
                    ws.Row(row).Cell(++col).SetValue(reportItem.Location);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SubLocation);
                    ws.Row(row).Cell(++col).SetValue(reportItem.LandMarks);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Area);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PricePerSqm);
                    ws.Row(row).Cell(++col).SetValue(reportItem.MarketValue);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PropNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OldAmount);
                    ws.Row(row).Cell(++col).SetValue(reportItem.AcqCost);
                    ws.Row(row).Cell(++col).SetValue(reportItem.AcqDate);
                    if (reportItem.FromDonation == true)
                    {
                        ws.Row(row).Cell(++col).SetValue("From Donation");
                    }
                    else
                    {
                        ws.Row(row).Cell(++col).SetValue("Purchased");
                    }
                    ws.Row(row).Cell(++col).SetValue(reportItem.Vendor);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Representative);
                    ws.Row(row).Cell(++col).SetValue(reportItem.TctNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OldTctNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.DRPNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.DRPDate);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OldDRPNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OldDRPDate);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Fund);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Remarks);
                }

                // Create a MemoryStream to save the output
                var memoryStream = new MemoryStream();
                wb.SaveAs(memoryStream);

                // Reset the stream position to the beginning before returning
                memoryStream.Position = 0;
                return memoryStream;
            }
        }

        public MemoryStream ProcessExcelAnnexFile(Guid id, string templateFilePath)
        {
            // Load the template file
            FileInfo templateFile = new FileInfo(templateFilePath);
            if (!templateFile.Exists)
            {
                throw new FileNotFoundException("The template file does not exist.", templateFilePath);
            }
            return ProcessExcelFileAnnexTemplate(id, templateFilePath);
        }

        private MemoryStream ProcessExcelFileAnnexTemplate(Guid id, string templateFilePath)
        {
            int row = 10;
            int col = 0;
            using (XLWorkbook wb = new XLWorkbook(templateFilePath))
            {
                var ws = wb.Worksheet(1);
                var reportItemList = _db.CustodianReportLandItems.Where(w => w.ReportId == id).ToList();
                foreach (var reportItem in reportItemList)
                {
                    ws.Row(4).Cell(2).SetValue("");
                    row++;
                    col = 0;
                    ws.Row(row).InsertRowsBelow(1);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PIN);
                    ws.Row(row).Cell(++col).SetValue(reportItem.CustodianItemNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.LocationCode);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SeriesNo);
                    ws.Row(row).Cell(++col).SetValue($"{reportItem.Type} / {reportItem.Condition} / {reportItem.Description}");
                    ws.Row(row).Cell(++col).SetValue(reportItem.Location);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SubLocation);
                    ws.Row(row).Cell(++col).SetValue(reportItem.LandMarks);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Area);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PricePerSqm);
                    ws.Row(row).Cell(++col).SetValue(reportItem.MarketValue);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PropNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OldAmount);
                    ws.Row(row).Cell(++col).SetValue(reportItem.AcqCost);
                    ws.Row(row).Cell(++col).SetValue(reportItem.AcqDate);
                    if (reportItem.FromDonation == true)
                    {
                        ws.Row(row).Cell(++col).SetValue("From Donation");
                    }
                    else
                    {
                        ws.Row(row).Cell(++col).SetValue("Purchased");
                    }
                    ws.Row(row).Cell(++col).SetValue(reportItem.Vendor);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Representative);
                    ws.Row(row).Cell(++col).SetValue(reportItem.TctNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OldTctNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.DRPNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.DRPDate);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OldDRPNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OldDRPDate);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Fund);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Remarks);
                }

                // Create a MemoryStream to save the output
                var memoryStream = new MemoryStream();
                wb.SaveAs(memoryStream);

                // Reset the stream position to the beginning before returning
                memoryStream.Position = 0;
                return memoryStream;
            }
        }

        private void ValidateRequired(CustodianReportLandItemVM model)
        {
            //if (!model.Area.HasValue)
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.Area)), "Field is required.");
            //}

            //if (!model.PricePerSqm.HasValue)
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.PricePerSqm)), "Field is required.");
            //}

            //if (string.IsNullOrWhiteSpace(model.Vendor))
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.Vendor)), "Field is required.");
            //}

            //if (string.IsNullOrWhiteSpace(model.OldTctNo))
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.OldTctNo)), "Field is required.");
            //}

            _imex.ThrowIfContainsErrors();
        }

        private void ValidateIfNull(CustodianReportLandItemVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(CustodianReportLandItem entity)
        {
            if (entity == null)
            {
                throw new NotFoundException(entity.Id);
            }
        }

        private void ValidateIfPosted(CustodianReportLandItem entity)
        {
            if (entity.PostedDt != null)
            {
                var msg = $"Record already posted by {entity.PostedBy} on {entity.PostedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        private void ValidateIfNotPosted(CustodianReportLandItem entity)
        {
            if (entity.PostedDt == null)
            {
                throw new RecordNotYetPostedException($"Record is not yet posted!");
            }
        }
    }
}