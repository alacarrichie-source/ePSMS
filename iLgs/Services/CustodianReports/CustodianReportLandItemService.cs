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
        ValueTask<CustodianReportLandItem> PostAsync(Guid id, string user, DateTime date);
        ValueTask<CustodianReportLandItem> UnPostAsync(Guid id, string user, DateTime date);
        MemoryStream ProcessExcelFile(Guid? id, string templateFilePath, int? accountGroup);
        MemoryStream ProcessExcelAnnexFile(Guid? id, string templateFilePath, int? accountGroup, string annex);
    }

    public class CustodianReportLandItemService : BaseValidator, ICustodianReportLandItemService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<CustodianReportLandItemVM> _vmExceptionService = new ExceptionService<CustodianReportLandItemVM>();
        private readonly IExceptionService<CustodianReportLandItem> _exceptionService = new ExceptionService<CustodianReportLandItem>();
        private readonly IAllFieldService _allFieldService;
        private readonly IUserService _userService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        public CustodianReportLandItemService(AppManEntities db)
        {
            _db = db;
            _allFieldService = new AllFieldService(_db);
            _userService = new UserService(_db);
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
            Unit = s.Unit,
            AreaXPrice = s.AreaXPrice,
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
        _vmExceptionService.TryCatch(async () =>
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

        public ValueTask<CustodianReportLandItemVM> CreateAsync(CustodianReportLandItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
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
                custodianReport.Department = model.MainDeptName;
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

        public ValueTask<CustodianReportLandItemVM> UpdateAsync(CustodianReportLandItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateRequired(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.CustodianReportLandItems.FindAsync(model.Id);
            ValidateRecord(entity);
            ValidateIfPosted(entity);
            //ValidateUser(entity, model);

            MapModelToEntityFields(entity, model, Mode.EDIT);

            _db.CustodianReportLandItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<CustodianReportLandItemVM> DeleteAsync(CustodianReportLandItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
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

        public ValueTask<CustodianReportLandItem> PostAsync(Guid id, string user, DateTime date) =>
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
            return entity;
        });

        public ValueTask<CustodianReportLandItem> UnPostAsync(Guid id, string user, DateTime date) =>
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
            return entity;
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
            entity.Unit = model.Unit;
            entity.AreaXPrice = model.AreaXPrice;
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

        public MemoryStream ProcessExcelFile(Guid? id, string templateFilePath, int? accountGroup)
        {
            // Load the template file
            FileInfo templateFile = new FileInfo(templateFilePath);
            if (!templateFile.Exists)
            {
                throw new FileNotFoundException("The template file does not exist.", templateFilePath);
            }
            return ProcessExcelFileTemplate(id, accountGroup, templateFilePath);
        }
        private void SetRowColValue(IXLWorksheet ws, CustodianReportLandItem reportItem, int row, bool isAnnex)
        {
            int col = 1;
            ws.Row(row).Cell(++col).SetValue(reportItem.PIN);
            ws.Row(row).Cell(++col).SetValue(reportItem.PropNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.CustodianItemNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.SeriesNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.LocationCode);
            ws.Row(row).Cell(++col).SetValue(reportItem.Location);
            ws.Row(row).Cell(++col).SetValue(reportItem.SubLocation);
            ws.Row(row).Cell(++col).SetValue(reportItem.LandMarks);
            ws.Row(row).Cell(++col).SetValue(reportItem.Description);
            if (reportItem.FromDonation == true)
            {
                ws.Row(row).Cell(++col).SetValue("From Donation");
            }
            else
            {
                ws.Row(row).Cell(++col).SetValue("Purchased");
            }
            ws.Row(row).Cell(++col).SetValue(Utility.ExportDate(reportItem.AcqDate));
            ws.Row(row).Cell(++col).SetValue(reportItem.Unit);
            ws.Row(row).Cell(++col).SetValue(reportItem.Area);
            ws.Row(row).Cell(++col).SetValue(reportItem.PricePerSqm);
            ws.Row(row).Cell(++col).SetValue(reportItem.AreaXPrice);
            ws.Row(row).Cell(++col).SetValue(reportItem.MarketValue);
            ws.Row(row).Cell(++col).SetValue(reportItem.OldAmount);
            ws.Row(row).Cell(++col).SetValue(reportItem.AcqCost);
            ws.Row(row).Cell(++col).SetValue(reportItem.Vendor);
            ws.Row(row).Cell(++col).SetValue(reportItem.Representative);
            ws.Row(row).Cell(++col).SetValue(reportItem.TctNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.OldTctNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.DRPNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.DRPDate).Style.DateFormat.Format = "MM/dd/yyyy";
            ws.Row(row).Cell(++col).SetValue(reportItem.OldDRPNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.OldDRPDate).Style.DateFormat.Format = "MM/dd/yyyy";
            ws.Row(row).Cell(++col).SetValue(reportItem.Fund);
            ws.Row(row).Cell(++col).SetValue(reportItem.Remarks);
            if (isAnnex)
            {
                ws.Row(row).Cell(++col).SetValue("");
            }
            else
            {
                ws.Row(row).Cell(++col).SetValue(reportItem.Annex);
            }
            col++;
            ws.Row(row).Cell(++col).SetValue(reportItem.CGTTransferTax);
            ws.Row(row).Cell(++col).SetValue(reportItem.CGTSurcharge);
            ws.Row(row).Cell(++col).SetValue(reportItem.CGTInterest);
            ws.Row(row).Cell(++col).SetValue(reportItem.CGTCompromise);
            ws.Row(row).Cell(++col).SetValue(reportItem.DSTTransferTax);
            ws.Row(row).Cell(++col).SetValue(reportItem.DSTSurcharge);
            ws.Row(row).Cell(++col).SetValue(reportItem.DSTInterest);
            ws.Row(row).Cell(++col).SetValue(reportItem.DSTCompromise);
            ws.Row(row).Cell(++col).SetValue(reportItem.Surcharge);
            ws.Row(row).Cell(++col).SetValue(reportItem.Interest);
            ws.Row(row).Cell(++col).SetValue(reportItem.ConfirmationFee);
            ws.Row(row).Cell(++col).SetValue(reportItem.TransferRegsFee);
            ws.Row(row).Cell(++col).SetValue(reportItem.TransferTax);
            ws.Row(row).Cell(++col).SetValue(reportItem.RealPropertyFee);
            ws.Row(row).Cell(++col).SetValue(reportItem.VAT);
            ws.Row(row).Cell(++col).SetValue(reportItem.EstateFee);
            ws.Row(row).Cell(++col).SetValue(reportItem.Titling);
            ws.Row(row).Cell(++col).SetValue(reportItem.CertificationFee);
            ws.Row(row).Cell(++col).SetValue(reportItem.Relocation);
            ws.Row(row).Cell(++col).SetValue(reportItem.Surveying);
            ws.Row(row).Cell(++col).SetValue(reportItem.IncidentalExpenses);
            ws.Row(row).Cell(++col).SetValue(reportItem.CGTTransferTax ?? 0 +
                reportItem.CGTSurcharge ?? 0 +
                reportItem.CGTInterest ?? 0 +
                reportItem.CGTCompromise ?? 0 +
                reportItem.DSTTransferTax ?? 0 +
                reportItem.DSTSurcharge ?? 0 +
                reportItem.DSTInterest ?? 0 +
                reportItem.DSTCompromise ?? 0 +
                reportItem.Surcharge ?? 0 +
                reportItem.Interest ?? 0 +
                reportItem.ConfirmationFee ?? 0 +
                reportItem.TransferRegsFee ?? 0 +
                reportItem.TransferTax ?? 0 +
                reportItem.RealPropertyFee ?? 0 +
                reportItem.VAT ?? 0 +
                reportItem.EstateFee ?? 0 +
                reportItem.Titling ?? 0 +
                reportItem.CertificationFee ?? 0 +
                reportItem.Relocation ?? 0 +
                reportItem.Surveying ?? 0 +
                reportItem.IncidentalExpenses ?? 0);
        }

        private MemoryStream ProcessExcelFileTemplate(Guid? id, int? accountGroup, string templateFilePath)
        {
            return ProcessExcelFileTemplate(id, accountGroup, templateFilePath, "", "");
        }

        private MemoryStream ProcessExcelFileTemplate(Guid? id, int? accountGroup, string templateFilePath, string hdg, string annex)
        {
            
            using (XLWorkbook wb = new XLWorkbook(templateFilePath))
            {
                int sw = 1;
                int row = 10;
                //string account = "";
                string department = "";
                decimal? tAcqCost = 0;
                var ws = wb.Worksheet(1);
                var reportItems = _db.CustodianReportLandItems
                    .Include(i => i.ItemCode.ItemType)
                    .Include(i => i.CustodianReport.Codextn)
                    .Include(i => i.Codextn) // deptId                    
                    .Where(w => w.CustodianReport.AccountGroup == accountGroup)
                    .AsNoTracking();

                if (!string.IsNullOrWhiteSpace(annex))
                {
                    reportItems = reportItems.Where(w => w.Annex == annex);
                }

                if (id != null)
                {
                    reportItems = reportItems.Where(w => w.ReportId == id).OrderBy(t => t.ItemCode.ItemType.Description).ThenBy(o => o.CustodianItemNo).ThenBy(t => t.ItemCode.ItemNoIndex);
                }
                else
                {
                    reportItems = reportItems.OrderBy(t => t.ItemCode.ItemType.Description).ThenBy(o => o.CustodianReport.Department).ThenBy(o => o.CustodianItemNo).ThenBy(t => t.ItemCode.ItemNoIndex);
                }

                foreach (var reportItem in reportItems)
                {
                    if (sw == 1)
                    {
                        //account = reportItem.ItemCode == null ? "" : reportItem.ItemCode.ItemType.Description;
                        department = reportItem.CustodianReport.Department;
                        if (!string.IsNullOrWhiteSpace(annex))
                        {
                            ws.Row(2).Cell(2).SetValue($"Annex {annex}");
                            ws.Row(4).Cell(2).SetValue(hdg);
                        }
                        ws.Row(5).Cell(2).SetValue($"As of {DateTime.Now.ToShortDateString()}");
                        //ws.Row(6).Cell(2).SetValue(account).Style.Font.Bold = true;

                        if (id == null)
                        {
                            ws.Row(6).Cell(3).SetValue($"ALL : {reportItem.CustodianReport.Codextn.Code} {reportItem.CustodianReport.Department}").Style.Font.Bold = true;
                        }
                        else
                        {
                            ws.Row(6).Cell(3).SetValue($"{reportItem.CustodianReport.Codextn.Code} {reportItem.CustodianReport.Department}").Style.Font.Bold = true;
                        }
                        sw = 0;
                    }

                    if (department != reportItem.CustodianReport.Department)
                    {
                        //account = reportItem.ItemCode.ItemType.Description;
                        department = reportItem.CustodianReport.Department;
                        row += 3;
                        ws.Row(row).Cell(2).SetValue("Custodian:");
                        if (id == null)
                        {
                            ws.Row(row).Cell(3).SetValue($"ALL : {reportItem.CustodianReport.Codextn.Code} {reportItem.CustodianReport.Department}").Style.Font.Bold = true;
                        }
                        else
                        {
                            ws.Row(row).Cell(3).SetValue($"{reportItem.CustodianReport.Codextn.Code} {reportItem.CustodianReport.Department}").Style.Font.Bold = true;
                        }
                        row += 2;
                        ws.Row(8).CopyTo(ws.Row(++row));
                        ws.Row(9).CopyTo(ws.Row(++row));
                        ws.Row(10).CopyTo(ws.Row(++row));
                        ws.Range($"B{row - 2}:B{row}").Merge();
                        ws.Range($"C{row - 2}:C{row}").Merge();
                        ws.Range($"D{row - 2}:D{row}").Merge();
                        ws.Range($"E{row - 2}:E{row}").Merge();
                        ws.Range($"F{row - 2}:F{row}").Merge();
                        ws.Range($"G{row - 2}:G{row}").Merge();
                        ws.Range($"H{row - 2}:H{row}").Merge();
                        ws.Range($"I{row - 2}:I{row}").Merge();
                        ws.Range($"J{row - 2}:J{row}").Merge();
                        ws.Range($"K{row - 2}:K{row}").Merge();
                        ws.Range($"O{row - 2}:O{row}").Merge();
                        ws.Range($"P{row - 2}:P{row}").Merge();
                        ws.Range($"Q{row - 2}:Q{row}").Merge();
                        ws.Range($"R{row - 2}:R{row}").Merge();
                        ws.Range($"S{row - 2}:S{row}").Merge();
                        ws.Range($"T{row - 1}:T{row}").Merge();
                        ws.Range($"U{row - 1}:U{row}").Merge();
                        ws.Range($"V{row - 1}:V{row}").Merge();
                        ws.Range($"W{row - 1}:W{row}").Merge();
                        ws.Range($"AB{row - 2}:AB{row}").Merge();
                        ws.Range($"AC{row - 2}:AC{row}").Merge();
                        ws.Range($"AD{row - 2}:AD{row}").Merge();
                        ws.Range($"AN{row - 1}:AN{row}").Merge();
                        ws.Range($"AO{row - 1}:AO{row}").Merge();
                        ws.Range($"AP{row - 1}:AP{row}").Merge();
                        ws.Range($"AQ{row - 1}:AQ{row}").Merge();
                        ws.Range($"AR{row - 1}:AR{row}").Merge();
                        ws.Range($"AS{row - 1}:AS{row}").Merge();
                        ws.Range($"AT{row - 1}:AT{row}").Merge();
                        ws.Range($"AU{row - 1}:AU{row}").Merge();
                        ws.Range($"AV{row - 1}:AV{row}").Merge();
                        ws.Range($"AW{row - 1}:AW{row}").Merge();
                        ws.Range($"AX{row - 1}:AX{row}").Merge();
                        ws.Range($"AY{row - 1}:AY{row}").Merge();
                        ws.Range($"AZ{row - 1}:AZ{row}").Merge();
                        ws.Range($"BA{row - 1}:BA{row}").Merge();
                    }
                    row++;
                    if (string.IsNullOrWhiteSpace(annex))
                    {
                        SetRowColValue(ws, reportItem, row, false);
                        ws.Range($"B{row}:AD{row}").Style.Border.BottomBorder = XLBorderStyleValues.Dotted;                        
                    }
                    else
                    {
                        SetRowColValue(ws, reportItem, row, true);
                        ws.Range($"B{row}:AC{row}").Style.Border.BottomBorder = XLBorderStyleValues.Dotted;
                    }
                    ws.Range($"AF{row}:BA{row}").Style.Border.BottomBorder = XLBorderStyleValues.Dotted;
                    tAcqCost += (reportItem.AcqCost ?? 0);
                }
                //ws.Row(++row).Cell(13).SetValue("TOTAL");
                //ws.Row(row).Cell(14).SetValue(tAcqCost);

                row += 4;
                ws.Row(row).Cell(3).SetValue("Certified Correct by:").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Row(row).Cell(10).SetValue("Verified by:").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                row += 3;
                ws.Row(row).Cell(4).SetValue("Signature over Printed Name").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Row(row).Cell(11).SetValue("Signature over Printed Name").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Range($"D{row}:G{row}").Merge().Style.Border.TopBorder = XLBorderStyleValues.Medium;
                ws.Range($"K{row}:M{row}").Merge().Style.Border.TopBorder = XLBorderStyleValues.Medium;
                row++;
                ws.Row(row).Cell(4).SetValue("Custodian").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Row(row).Cell(11).SetValue("Assistant to the Custodian").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Range($"D{row}:G{row}").Merge();
                ws.Range($"K{row}:M{row}").Merge();

                // Create a MemoryStream to save the output
                var memoryStream = new MemoryStream();
                wb.SaveAs(memoryStream);

                // Reset the stream position to the beginning before returning
                memoryStream.Position = 0;
                return memoryStream;
            }
        }

        public MemoryStream ProcessExcelAnnexFile(Guid? id, string templateFilePath, int? accountGroup, string annex)
        {
            // Load the template file
            FileInfo templateFile = new FileInfo(templateFilePath);
            if (!templateFile.Exists)
            {
                throw new FileNotFoundException("The template file does not exist.", templateFilePath);
            }
            string hdg = "";

            if (annex == "A")
            {
                hdg = "(INVENTORY COUNT FORM)";
            }
            else if (annex == "B")
            {
                hdg = "(LIST OF PPEs, FOUND AT STATION)";
            }
            else if (annex == "C")
            {
                hdg = "(LIST OF NON-EXISTING/MISSING PPEs)";
            }
            return ProcessExcelFileTemplate(id, accountGroup, templateFilePath, hdg, annex);
        }
        

        private void ValidateRequired(CustodianReportLandItemVM model)
        {
            if (model.MainDeptId == null || model.MainDeptId == Guid.Empty)
            {
                _imex.UpsertDataList("Department", "Please select department before creating an entry.");
            }
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

        private void ValidateUser(CustodianReportLandItem entity, CustodianReportLandItem model)
        {
            if (entity.InsertedBy != model.UpdatedBy)
            {
                var isAdmin = _userService.IsUserNameAdmin(model.UpdatedBy);
                if (!isAdmin)
                {
                    throw new RecordLockedException($"Record can only be updated by {entity.InsertedBy} or an Admin.");
                }
            }
        }
    }
}