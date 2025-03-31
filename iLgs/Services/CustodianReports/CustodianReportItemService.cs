using ClosedXML.Excel;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.AllFields;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services.CustodianReports
{
    public interface ICustodianReportItemService
    {
        IQueryable<CustodianReportItem> GetByReportId(Guid? reportId);
        IQueryable<CustodianReportItem> GetAvailableItemsForDisposal(Guid? deptId);
        ValueTask<CustodianReportItem> GetByIdAsync(Guid id);
        string GetStockNo(CustodianReportItem model);
        ValueTask<CustodianReportItem> CreateAsync(CustodianReportItem model, string user, DateTime date);
        ValueTask<CustodianReportItem> UpdateAsync(CustodianReportItem model, string user, DateTime date);
        ValueTask<CustodianReportItem> DeleteAsync(CustodianReportItem model, string user, DateTime date);
        ValueTask<CustodianReportItem> PostAsync(Guid id, string user, DateTime date);
        ValueTask<CustodianReportItem> UnPostAsync(Guid id, string user, DateTime date);
        MemoryStream ProcessExcelFile(Guid? id, Guid? deptId, string templateFilePath, int? accountGroup);
        MemoryStream ProcessExcelFile(Guid? id, Guid? deptId, string templateFilePath, int? accountGroup, string mainAccount, DateTime? asOf);
        MemoryStream ProcessExcelFileAnnex(Guid? id, Guid? deptId, string templateFilePath, int? accountGroup, string annex, string mainAccount, DateTime? asOf);
    }

    public class CustodianReportItemService : ICustodianReportItemService
    {
        protected readonly AppManEntities _db;
        protected readonly IAllFieldService _allFieldService;
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<CustodianReportItem> _exceptionService = new ExceptionService<CustodianReportItem>();
        protected readonly IUserService _userService;

        public CustodianReportItemService(AppManEntities db)
        {
            _db = db;
            _allFieldService = new AllFieldService(_db);
            _userService = new UserService(_db);
        }

        public ValueTask<CustodianReportItem> GetByIdAsync(Guid id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.CustodianReportItems.Where(w => w.Id == id).FirstOrDefaultAsync();
            return data;
        });

        public string GetStockNo(CustodianReportItem model)
        {
            model.AllField = SetAllField(model);
            return _allFieldService.GetCustodianStockNo(model);
        }

        public IQueryable<CustodianReportItem> GetByReportId(Guid? reportId) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.CustodianReportItems.AsNoTracking().Where(w => w.ReportId == reportId).AsQueryable();
            return data;
        });

        public IQueryable<CustodianReportItem> GetAvailableItemsForDisposal(Guid? deptId) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.CustodianReportItems.AsNoTracking()
                .Where(w => w.DeptId == deptId
                    && (w.Annex == "A" || w.Annex == "B")
                    && !w.CustodianDisposalItems.Any())
                .AsQueryable();

            return data;
        });

        public virtual async ValueTask<CustodianReportItem> CreateAsync(CustodianReportItem model, string user, DateTime date)
        {
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

            var entity = new CustodianReportItem();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.CustodianReportItems.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        }

        public virtual async ValueTask<CustodianReportItem> UpdateAsync(CustodianReportItem model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.CustodianReportItems.FindAsync(model.Id);
            ValidateRecord(entity);
            ValidateIfPosted(entity);
            ValidateUser(entity, model);

            MapModelToEntityFields(entity, model, Mode.EDIT);

            _db.CustodianReportItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        }

        public virtual async ValueTask<CustodianReportItem> DeleteAsync(CustodianReportItem model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.CustodianReportItems.FindAsync(model.Id);
            ValidateRecord(entity);
            ValidateIfPosted(entity);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.CustodianReportItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.CustodianReportItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();
            return model;
        }

        public virtual ValueTask<CustodianReportItem> PostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.CustodianReportItems.FindAsync(id);
            ValidateRecord(entity);
            ValidateIfPosted(entity);

            entity.PostedBy = user;
            entity.PostedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.CustodianReportItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        public virtual ValueTask<CustodianReportItem> UnPostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.CustodianReportItems.FindAsync(id);
            ValidateRecord(entity);
            ValidateIfNotPosted(entity);

            entity.PostedBy = "";
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.CustodianReportItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        protected AllField SetAllField(CustodianReportItem custodianReportItem)
        {
            AllField allField = new AllField()
            {
                GenericName = custodianReportItem.GenericName,
                DosageStrength = custodianReportItem.DosageStrength,
                DosageForm = custodianReportItem.DosageForm,
                DosageVolume = custodianReportItem.DosageVolume,
                Brand = custodianReportItem.Brand,
                Multipliers = custodianReportItem.Multipliers,
                Model_ = custodianReportItem.Model_,
                Dimension = custodianReportItem.Dimension,
                Size = custodianReportItem.Size,
                Weight = custodianReportItem.Weight,
                Materials = custodianReportItem.Materials,
                Capacity = custodianReportItem.Capacity,
                Color = custodianReportItem.Color,
                SerialNo = custodianReportItem.SerialNo,
                PropNo = custodianReportItem.PropNo,
                PlateNo = custodianReportItem.PlateNo,
                BodyNo = custodianReportItem.BodyNo,
                MVFileNo = custodianReportItem.MVFileNo,
            };
            allField = _allFieldService.ChangeAllFieldCase(allField);
            return allField;
        }

        protected void MapFormattedAllField(CustodianReportItem model)
        {
            model.AllField = SetAllField(model);
            // transfer formarteted allfield to base model
            model.GenericName = model.AllField.GenericName;
            model.DosageForm = model.AllField.DosageForm;
            model.DosageStrength = model.AllField.DosageStrength;
            model.DosageVolume = model.AllField.DosageVolume;
            model.Multipliers = model.AllField.Multipliers;
            model.DosageForm = model.AllField.SerialNo;
            model.PropNo = model.AllField.PropNo;

            model.SerialNo = model.AllField.SerialNo;
            model.PropNo = model.AllField.PropNo;

            model.Materials = model.AllField.Materials;
            model.Size = model.AllField.Size;
            model.Capacity = model.AllField.Capacity;
            model.Color = model.AllField.Color;
            model.Weight = model.AllField.Weight;

            model.PlateNo = model.AllField.PlateNo;
            ///
        }

        protected void MapModelToEntityFields(CustodianReportItem entity, CustodianReportItem model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            MapFormattedAllField(model);

            entity.ReportId = model.ReportId;
            entity.Fund = model.Fund;
            entity.CustodianItemNo = model.CustodianItemNo;
            entity.SeriesNo = model.SeriesNo;
            entity.FromDonation = model.FromDonation;
            entity.InvDist = model.InvDist;
            entity.Account = model.Account;
            entity.ItemCodeId = model.ItemCodeId;
            entity.SubAccount = model.SubAccount;
            entity.Article = model.Article;
            entity.PoNo = model.PoNo;
            entity.PoDate = model.PoDate;
            entity.AirNo = model.AirNo;
            entity.AirDate = model.AirDate;
            entity.AcqDate = model.AcqDate;
            entity.UnitCost = model.UnitCost;
            entity.Unit = model.Unit;
            entity.SetLotNo = model.SetLotNo;
            entity.DeptId = model.DeptId;
            entity.Department = model.Department;
            entity.LocationId = model.LocationId;
            entity.LocationCode = model.LocationCode;
            entity.Location = model.Location;
            entity.SubLocation = model.SubLocation;
            entity.Qty = model.Qty;
            entity.TransferIn = model.TransferIn;
            entity.QtyBalance = model.QtyBalance;
            entity.TotalCost = model.TotalCost;
            entity.OldAmount = model.OldAmount;
            entity.OldPsNo = model.OldPsNo;
            entity.PsNo = model.PsNo;
            entity.Description = model.Description;
            entity.Brand = model.Brand;
            entity.Model_ = model.Model_;
            entity.Dimension = model.Dimension;
            entity.Size = model.Size;
            entity.Weight = model.Weight;
            entity.Materials = model.Materials;
            entity.Capacity = model.Capacity;
            entity.Color = model.Color;
            entity.GenericName = model.GenericName;
            entity.DosageStrength = model.DosageStrength;
            entity.DosageForm = model.DosageForm;
            entity.DosageVolume = model.DosageVolume;
            entity.Multipliers = model.Multipliers;
            entity.SerialNo = model.SerialNo;
            entity.OldPropNo = model.OldPropNo;
            entity.PropNo = model.PropNo;
            entity.YearModel = model.YearModel;
            entity.PlateNo = model.PlateNo;
            entity.BodyNo = model.BodyNo;
            entity.MVFileNo = model.MVFileNo;
            entity.EngineNo = model.EngineNo;
            entity.ChasisNo = model.ChasisNo;
            entity.CRN = model.CRN;
            entity.CRDate = model.CRDate;
            entity.OrNo = model.OrNo;
            entity.OrDate = model.OrDate;
            entity.InsPolicyNo = model.InsPolicyNo;
            entity.ConductionNo = model.ConductionNo;
            entity.ItemSerialNo = model.ItemSerialNo;
            entity.OtherDesc = model.OtherDesc;
            entity.OtherQty = model.OtherQty;
            entity.Condition = model.Condition;
            entity.Remarks = model.Remarks;
            entity.ParNo = model.ParNo;
            entity.IcsNo = model.IcsNo;
            entity.AreNo = model.AreNo;
            entity.MrNo = model.MrNo;
            entity.ParIssuedTo = model.ParIssuedTo;
            entity.IcsIssuedTo = model.IcsIssuedTo;
            entity.AreIssuedTo = model.AreIssuedTo;
            entity.MrIssuedTo = model.MrIssuedTo;
            entity.AccountableOfficer = model.AccountableOfficer;
            entity.IcsOfficer = model.IcsOfficer;
            entity.AreOfficer = model.AreOfficer;
            entity.MrOfficer = model.MrOfficer;
            entity.UpcomingPar = model.UpcomingPar;
            entity.UpcomingIcs = model.UpcomingIcs;
            entity.Type = model.Type;
            entity.Annex = model.Annex;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
            entity.SetLotAmount = model.SetLotAmount;
            entity.SetLotRemarks = model.SetLotRemarks;
            entity.PriceRate = model.PriceRate;
            entity.ProRatedCost = model.ProRatedCost;
            entity.AddCost = model.AddCost;
            entity.TUnitCost = model.TUnitCost;
            entity.GTotalCost = model.GTotalCost;

            //entity.PostedBy = model.PostedBy;
            //entity.PostedDt = model.PostedDt;
        }

        public MemoryStream ProcessExcelFile(Guid? id, Guid? deptId, string templateFilePath, int? accountGroup)
        {
            return ProcessExcelFile(id, deptId, templateFilePath, accountGroup, "", null);
        }

        public MemoryStream ProcessExcelFile(Guid? id, Guid? deptId, string templateFilePath, int? accountGroup, string mainAccount, DateTime? asOf)
        {
            // Load the template file
            FileInfo templateFile = new FileInfo(templateFilePath);
            if (!templateFile.Exists)
            {
                throw new FileNotFoundException("The template file does not exist.", templateFilePath);
            }
            if (accountGroup == (int?)CustodianAccountGroup.STOCK)
            {
                return ProcessExcelFileStockTemplate(id, deptId, accountGroup, templateFilePath, mainAccount, asOf);
            }
            else if (accountGroup == (int?)CustodianAccountGroup.PPE)
            {
                return ProcessExcelFilePpeTemplate(id, deptId, accountGroup, templateFilePath, mainAccount, asOf);
            }
            else
            {
                return ProcessExcelFileVehicleTemplate(id, deptId, accountGroup, templateFilePath, mainAccount, asOf);
            }
        }

        private string GetSubAccount(string itemCode)
        {
            var data = _db.Database.SqlQuery<string>("Select dbo.fn_SubAccount({0})", itemCode).FirstOrDefault();
            return data;
        }

        private void SetStockRowColValue(IXLWorksheet ws, CustodianReportItem reportItem, int row, bool isAnnex)
        {
            int col = 1;
            //ws.Row(row).InsertRowsBelow(1);
            ws.Row(row).Cell(++col).SetValue(reportItem.CustodianItemNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.Codextn?.Code);
            ws.Row(row).Cell(++col).SetValue(reportItem.LocationCode);
            ws.Row(row).Cell(++col).SetValue(reportItem.SeriesNo);
            if (string.IsNullOrWhiteSpace(reportItem.SubAccount))
            {
                ws.Row(row).Cell(++col).SetValue($"{reportItem.Article}");
            }
            else
            {
                ws.Row(row).Cell(++col).SetValue($"{reportItem.SubAccount} / {reportItem.Article}");
            }
            ws.Row(row).Cell(++col).SetValue(reportItem.Multipliers);
            ws.Row(row).Cell(++col).SetValue(reportItem.Brand);
            ws.Row(row).Cell(++col).SetValue(reportItem.Model_);
            ws.Row(row).Cell(++col).SetValue($"{reportItem.Dimension ?? ""} / {reportItem.Size ?? ""} / {reportItem.Weight ?? ""} / {reportItem.Materials ?? ""} / {reportItem.Capacity ?? ""}");
            ws.Row(row).Cell(++col).SetValue(reportItem.SerialNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.OtherDesc);
            ws.Row(row).Cell(++col).SetValue(reportItem.OtherQty);
            ws.Row(row).Cell(++col).SetValue(reportItem.Color);
            ws.Row(row).Cell(++col).SetValue(reportItem.OldPropNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.PropNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.OldAmount);
            if (reportItem.FromDonation == true)
            {
                ws.Row(row).Cell(++col).SetValue("From Donation");
            }
            else
            {
                ws.Row(row).Cell(++col).SetValue("Purchased");
            }
            ws.Row(row).Cell(++col).SetValue(reportItem.PoNo);
            ws.Row(row).Cell(++col).SetValue(Utility.ExportDate(reportItem.PoDate));
            ws.Row(row).Cell(++col).SetValue(reportItem.Unit);
            ws.Row(row).Cell(++col).SetValue(reportItem.UnitCost);
            ws.Row(row).Cell(++col).SetValue(reportItem.AddCost);
            ws.Row(row).Cell(++col).SetValue(reportItem.TotalCost);
            ws.Row(row).Cell(++col).SetValue(reportItem.AcqDate).Style.DateFormat.Format = "MM/dd/yyyy";
            ws.Row(row).Cell(++col).SetValue(reportItem.PriceRate);
            ws.Row(row).Cell(++col).SetValue(reportItem.SetLotNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.SetLotAmount);
            ws.Row(row).Cell(++col).SetValue(reportItem.SetLotRemarks);
            ws.Row(row).Cell(++col).SetValue(reportItem.AirNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.AirDate).Style.DateFormat.Format = "MM/dd/yyyy";
            ws.Row(row).Cell(++col).SetValue(reportItem.SubLocation);
            //ws.Row(row).Cell(++col).SetValue($"{reportItem.ParNo ?? ""} / {reportItem.AreNo ?? ""} / {reportItem.MrNo ?? ""}");
            //ws.Row(row).Cell(++col).SetValue($"{reportItem.AccountableOfficer ?? ""} / {reportItem.AreOfficer ?? ""} / {reportItem.MrOfficer ?? ""}");
            //ws.Row(row).Cell(++col).SetValue($"{reportItem.ParIssuedTo ?? ""} / {reportItem.AreIssuedTo ?? ""} / {reportItem.MrIssuedTo ?? ""}");
            var pars = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            var ares = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            var mrs = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            ws.Row(row).Cell(++col).SetValue($"{(reportItem.ParNo ?? "") + (string.IsNullOrWhiteSpace(pars) ? "" : "\r\n" + pars)} " +
                $"/ {(reportItem.AreNo ?? "") + (string.IsNullOrWhiteSpace(ares) ? "" : "\r\n" + ares)} " +
                $"/ {(reportItem.MrNo ?? "") + (string.IsNullOrWhiteSpace(mrs) ? "" : "\r\n" + mrs)}");

            var parOfficers = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            var areOfficers = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            var mrOfficers = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            ws.Row(row).Cell(++col).SetValue($"{(reportItem.AccountableOfficer ?? "") + (string.IsNullOrWhiteSpace(parOfficers) ? "" : "\r\n" + parOfficers)} " +
                $"/ {(reportItem.AreOfficer ?? "") + (string.IsNullOrWhiteSpace(areOfficers) ? "" : "\r\n" + areOfficers)} " +
                $"/ {(reportItem.MrOfficer ?? "") + (string.IsNullOrWhiteSpace(mrOfficers) ? "" : "\r\n" + mrOfficers)}");

            var parIssuedTo = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            var areIssuedTo = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            var mrIssuedTo = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            ws.Row(row).Cell(++col).SetValue($"{(reportItem.ParIssuedTo ?? "") + (string.IsNullOrWhiteSpace(parIssuedTo) ? "" : "\r\n" + parIssuedTo)} " +
                $"/ {(reportItem.AreIssuedTo ?? "") + (string.IsNullOrWhiteSpace(areIssuedTo) ? "" : "\r\n" + areIssuedTo)} " +
                $"/ {(reportItem.MrIssuedTo ?? "") + (string.IsNullOrWhiteSpace(mrIssuedTo) ? "" : "\r\n" + mrIssuedTo)}");

            ws.Row(row).Cell(++col).SetValue(reportItem.UpcomingPar);
            ws.Row(row).Cell(++col).SetValue(reportItem.Fund);
            ws.Row(row).Cell(++col).SetValue(reportItem.Condition);
            ws.Row(row).Cell(++col).SetValue(reportItem.Remarks);
            if (!isAnnex)
            {
                ws.Row(row).Cell(++col).SetValue(reportItem.Annex);
            }
        }

        private void SetPpeRowColValue(IXLWorksheet ws, CustodianReportItem reportItem, int row, bool isAnnex)
        {
            int col = 1;
            //ws.Row(row).InsertRowsBelow(1);
            ws.Row(row).Cell(++col).SetValue(reportItem.CustodianItemNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.Codextn?.Code);
            ws.Row(row).Cell(++col).SetValue(reportItem.LocationCode);
            ws.Row(row).Cell(++col).SetValue(reportItem.SeriesNo);
            if (string.IsNullOrWhiteSpace(reportItem.SubAccount))
            {
                ws.Row(row).Cell(++col).SetValue($"{reportItem.Article}");
            }
            else
            {
                ws.Row(row).Cell(++col).SetValue($"{reportItem.SubAccount} / {reportItem.Article}");
            }
            ws.Row(row).Cell(++col).SetValue(reportItem.Multipliers);
            ws.Row(row).Cell(++col).SetValue(reportItem.Brand);
            ws.Row(row).Cell(++col).SetValue(reportItem.Model_);
            ws.Row(row).Cell(++col).SetValue($"{reportItem.Dimension ?? ""} / {reportItem.Size ?? ""} / {reportItem.Weight ?? ""} / {reportItem.Materials ?? ""} / {reportItem.Capacity ?? ""}");
            ws.Row(row).Cell(++col).SetValue(reportItem.SerialNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.OtherDesc);
            ws.Row(row).Cell(++col).SetValue(reportItem.OtherQty);
            ws.Row(row).Cell(++col).SetValue(reportItem.Color);
            ws.Row(row).Cell(++col).SetValue(reportItem.OldPropNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.PropNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.OldAmount);
            if (reportItem.FromDonation == true)
            {
                ws.Row(row).Cell(++col).SetValue("From Donation");
            }
            else
            {
                ws.Row(row).Cell(++col).SetValue("Purchased");
            }
            ws.Row(row).Cell(++col).SetValue(reportItem.PoNo);
            ws.Row(row).Cell(++col).SetValue(Utility.ExportDate(reportItem.PoDate));
            ws.Row(row).Cell(++col).SetValue(reportItem.Unit);
            ws.Row(row).Cell(++col).SetValue(reportItem.UnitCost);
            ws.Row(row).Cell(++col).SetValue(reportItem.AddCost);
            ws.Row(row).Cell(++col).SetValue(reportItem.TotalCost);
            ws.Row(row).Cell(++col).SetValue(reportItem.AcqDate).Style.DateFormat.Format = "MM/dd/yyyy";
            ws.Row(row).Cell(++col).SetValue(reportItem.PriceRate);
            ws.Row(row).Cell(++col).SetValue(reportItem.SetLotNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.SetLotAmount);
            ws.Row(row).Cell(++col).SetValue(reportItem.SetLotRemarks);
            ws.Row(row).Cell(++col).SetValue(reportItem.AirNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.AirDate).Style.DateFormat.Format = "MM/dd/yyyy";
            ws.Row(row).Cell(++col).SetValue(reportItem.SubLocation);

            var pars = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            var ares = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            var mrs = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            //ws.Cell(2, 1).Style.Alignment.WrapText = true; // Enable text wrapping
            ws.Row(row).Cell(++col).SetValue($"{(reportItem.ParNo ?? "") + (string.IsNullOrWhiteSpace(pars) ? "" : "\r\n" + pars)} " +
                $"/ {(reportItem.AreNo ?? "") + (string.IsNullOrWhiteSpace(ares) ? "" : "\r\n" + ares)} " +
                $"/ {(reportItem.MrNo ?? "") + (string.IsNullOrWhiteSpace(mrs) ? "" : "\r\n" + mrs)}");

            var parOfficers = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            var areOfficers = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            var mrOfficers = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            ws.Row(row).Cell(++col).SetValue($"{(reportItem.AccountableOfficer ?? "") + (string.IsNullOrWhiteSpace(parOfficers) ? "" : "\r\n" + parOfficers)} " +
                $"/ {(reportItem.AreOfficer ?? "") + (string.IsNullOrWhiteSpace(areOfficers) ? "" : "\r\n" + areOfficers)} " +
                $"/ {(reportItem.MrOfficer ?? "") + (string.IsNullOrWhiteSpace(mrOfficers) ? "" : "\r\n" + mrOfficers)}");

            var parIssuedTo = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            var areIssuedTo = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            var mrIssuedTo = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            ws.Row(row).Cell(++col).SetValue($"{(reportItem.ParIssuedTo ?? "") + (string.IsNullOrWhiteSpace(parIssuedTo) ? "" : "\r\n" + parIssuedTo)} " +
                $"/ {(reportItem.AreIssuedTo ?? "") + (string.IsNullOrWhiteSpace(areIssuedTo) ? "" : "\r\n" + areIssuedTo)} " +
                $"/ {(reportItem.MrIssuedTo ?? "") + (string.IsNullOrWhiteSpace(mrIssuedTo) ? "" : "\r\n" + mrIssuedTo)}");

            ws.Row(row).Cell(++col).SetValue(reportItem.UpcomingPar);
            ws.Row(row).Cell(++col).SetValue(reportItem.Fund);
            ws.Row(row).Cell(++col).SetValue(reportItem.Condition);
            ws.Row(row).Cell(++col).SetValue(reportItem.Remarks);
            if (!isAnnex)
            {
                ws.Row(row).Cell(++col).SetValue(reportItem.Annex);
            }
        }

        private void SetVehicleRowColValue(IXLWorksheet ws, CustodianReportItem reportItem, int row, bool isAnnex)
        {
            int col = 1;
            //ws.Row(row).InsertRowsBelow(1);
            ws.Row(row).Cell(++col).SetValue(reportItem.CustodianItemNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.Codextn?.Code);
            ws.Row(row).Cell(++col).SetValue(reportItem.LocationCode);
            ws.Row(row).Cell(++col).SetValue(reportItem.SeriesNo);
            if (string.IsNullOrWhiteSpace(reportItem.SubAccount))
            {
                ws.Row(row).Cell(++col).SetValue($"{reportItem.Article}");
            }
            else
            {
                ws.Row(row).Cell(++col).SetValue($"{reportItem.SubAccount} / {reportItem.Article}");

            }
            ws.Row(row).Cell(++col).SetValue(reportItem.Brand);
            ws.Row(row).Cell(++col).SetValue(reportItem.Model_);
            ws.Row(row).Cell(++col).SetValue(reportItem.YearModel);
            ws.Row(row).Cell(++col).SetValue(reportItem.PlateNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.ConductionNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.BodyNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.EngineNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.ChasisNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.Color);
            ws.Row(row).Cell(++col).SetValue(reportItem.CRN);
            ws.Row(row).Cell(++col).SetValue(reportItem.CRDate).Style.DateFormat.Format = "MM/dd/yyyy";
            ws.Row(row).Cell(++col).SetValue(reportItem.MVFileNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.Weight);
            ws.Row(row).Cell(++col).SetValue(reportItem.OrNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.OrDate).Style.DateFormat.Format = "MM/dd/yyyy";
            ws.Row(row).Cell(++col).SetValue(reportItem.InsPolicyNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.OldPropNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.PropNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.OldAmount);
            if (reportItem.FromDonation == true)
            {
                ws.Row(row).Cell(++col).SetValue("From Donation");
            }
            else
            {
                ws.Row(row).Cell(++col).SetValue("Purchased");
            }
            ws.Row(row).Cell(++col).SetValue(reportItem.PoNo);
            ws.Row(row).Cell(++col).SetValue(Utility.ExportDate(reportItem.PoDate));
            ws.Row(row).Cell(++col).SetValue(reportItem.Unit);
            ws.Row(row).Cell(++col).SetValue(reportItem.UnitCost);
            ws.Row(row).Cell(++col).SetValue(reportItem.AddCost);
            ws.Row(row).Cell(++col).SetValue(reportItem.TotalCost);
            ws.Row(row).Cell(++col).SetValue(reportItem.AcqDate).Style.DateFormat.Format = "MM/dd/yyyy";
            ws.Row(row).Cell(++col).SetValue(reportItem.PriceRate);
            ws.Row(row).Cell(++col).SetValue(reportItem.SetLotNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.SetLotAmount);
            ws.Row(row).Cell(++col).SetValue(reportItem.SetLotRemarks);
            ws.Row(row).Cell(++col).SetValue(reportItem.AirNo);
            ws.Row(row).Cell(++col).SetValue(reportItem.AirDate).Style.DateFormat.Format = "MM/dd/yyyy";

            ws.Row(row).Cell(++col).SetValue(reportItem.SubLocation);

            var pars = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            var ares = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            var mrs = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            ws.Row(row).Cell(++col).SetValue($"{(reportItem.ParNo ?? "") + (string.IsNullOrWhiteSpace(pars) ? "" : "\r\n" + pars)} " +
                $"/ {(reportItem.AreNo ?? "") + (string.IsNullOrWhiteSpace(ares) ? "" : "\r\n" + ares)} " +
                $"/ {(reportItem.MrNo ?? "") + (string.IsNullOrWhiteSpace(mrs) ? "" : "\r\n" + mrs)}");

            var parOfficers = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            var areOfficers = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            var mrOfficers = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            ws.Row(row).Cell(++col).SetValue($"{(reportItem.AccountableOfficer ?? "") + (string.IsNullOrWhiteSpace(parOfficers) ? "" : "\r\n" + parOfficers)} " +
                $"/ {(reportItem.AreOfficer ?? "") + (string.IsNullOrWhiteSpace(areOfficers) ? "" : "\r\n" + areOfficers)} " +
                $"/ {(reportItem.MrOfficer ?? "") + (string.IsNullOrWhiteSpace(mrOfficers) ? "" : "\r\n" + mrOfficers)}");

            var parIssuedTo = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            var areIssuedTo = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            var mrIssuedTo = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            ws.Row(row).Cell(++col).SetValue($"{(reportItem.ParIssuedTo ?? "") + (string.IsNullOrWhiteSpace(parIssuedTo) ? "" : "\r\n" + parIssuedTo)} " +
                $"/ {(reportItem.AreIssuedTo ?? "") + (string.IsNullOrWhiteSpace(areIssuedTo) ? "" : "\r\n" + areIssuedTo)} " +
                $"/ {(reportItem.MrIssuedTo ?? "") + (string.IsNullOrWhiteSpace(mrIssuedTo) ? "" : "\r\n" + mrIssuedTo)}");

            ws.Row(row).Cell(++col).SetValue(reportItem.UpcomingPar);
            ws.Row(row).Cell(++col).SetValue(reportItem.Fund);
            ws.Row(row).Cell(++col).SetValue(reportItem.Condition);
            ws.Row(row).Cell(++col).SetValue(reportItem.Remarks);
            if (!isAnnex)
            {
                ws.Row(row).Cell(++col).SetValue(reportItem.Annex);
            }
        }

        private MemoryStream ProcessExcelFileStockTemplate(Guid? id, Guid? deptId, int? accountGroup, string templateFilePath, string mainAccount, DateTime? asOf)
        {
            return ProcessExcelFileStockTemplate(id, deptId, accountGroup, templateFilePath, "", "", mainAccount, asOf);
        }

        private MemoryStream ProcessExcelFileStockTemplate(Guid? id, Guid? deptId, int? accountGroup, string templateFilePath, string hdg, string annex, string mainAccount, DateTime? asOf)
        {
            using (XLWorkbook wb = new XLWorkbook(templateFilePath))
            {
                int sw = 1;
                int row = 14;
                string setLotNo = "";
                string account = "";
                string department = "";
                decimal? tAcqCost = 0;
                var ws = wb.Worksheet(1);
                IQueryable<CustodianReportItem> reportItems;
                reportItems = _db.CustodianReportItems
                    .Include(i => i.ItemCode.ItemType)
                    .Include(i => i.CustodianReport.Codextn)
                    .Include(i => i.Codextn) // deptId
                    .Include(i => i.Codextn1) // LocationId
                    .Include(i => i.CustodianReportItemIssuances)
                    .Where(w => w.CustodianReport.AccountGroup == accountGroup)
                    .AsNoTracking();

                if (!string.IsNullOrWhiteSpace(mainAccount))
                {
                    reportItems = reportItems.Where(w => w.ItemCode.ItemType.Description == mainAccount);
                }

                if (asOf.HasValue)
                {
                    reportItems = reportItems.Where(w => w.AcqDate <= asOf);
                }

                if (!string.IsNullOrWhiteSpace(annex))
                {
                    reportItems = reportItems.Where(w => w.Annex == annex);
                }

                if (id != null)
                {
                    reportItems = reportItems.Where(w => w.ReportId == id).OrderBy(t => t.ItemCode.ItemType.Description).ThenBy(o => o.CustodianItemNo).ThenBy(t => t.SetLotNo).ThenBy(t => t.SetLotRemarks).ThenBy(t => t.ItemCode.ItemNoIndex);
                }
                else
                {
                    if (deptId != null)
                    {
                        reportItems = reportItems.Where(w => w.CustodianReport.DeptId == deptId).OrderBy(t => t.ItemCode.ItemType.Description).ThenBy(o => o.CustodianReport.Department).ThenBy(o => o.CustodianItemNo).ThenBy(t => t.SetLotNo).ThenBy(t => t.SetLotRemarks).ThenBy(t => t.ItemCode.ItemNoIndex);
                    }
                    else
                    {
                        reportItems = reportItems.OrderBy(t => t.ItemCode.ItemType.Description).ThenBy(o => o.CustodianReport.Department).ThenBy(o => o.CustodianItemNo).ThenBy(t => t.SetLotNo).ThenBy(t => t.SetLotRemarks).ThenBy(t => t.ItemCode.ItemNoIndex);
                    }
                }

                foreach (var reportItem in reportItems)
                {
                    if (sw == 1)
                    {
                        account = reportItem.ItemCode.ItemType.Description;
                        department = reportItem.CustodianReport.Department;
                        if (!string.IsNullOrWhiteSpace(annex))
                        {
                            ws.Row(2).Cell(2).SetValue($"Annex {annex}");
                            ws.Row(4).Cell(2).SetValue(hdg);
                        }
                        if (asOf.HasValue)
                        {
                            ws.Row(5).Cell(2).SetValue($"As of {asOf.Value.ToShortDateString()}");
                        }
                        else
                        {
                            ws.Row(5).Cell(2).SetValue($"As of {DateTime.Now.ToShortDateString()}");
                        }
                        ws.Row(6).Cell(2).SetValue(account).Style.Font.Bold = true;

                        if (deptId == null || deptId == Guid.Empty)
                        {
                            ws.Row(8).Cell(3).SetValue("ALL").Style.Font.Bold = true;
                        }

                        ws.Row(10).Cell(2).SetValue($"{reportItem.CustodianReport.Codextn.Code} {reportItem.CustodianReport.Department}").Style.Font.Bold = true;
                        sw = 0;
                    }

                    if (account != reportItem.ItemCode.ItemType.Description || department != reportItem.CustodianReport.Department)
                    {
                        account = reportItem.ItemCode.ItemType.Description;
                        department = reportItem.CustodianReport.Department;
                        row += 3;
                        ws.Row(row).Cell(2).SetValue(account).Style.Font.Bold = true;
                        row += 2;
                        ws.Row(row).Cell(2).SetValue("Department:");
                        if (deptId == null || deptId == Guid.Empty)
                        {
                            ws.Row(row).Cell(3).SetValue("ALL").Style.Font.Bold = true;
                        }
                        row += 2;
                        ws.Row(row).Cell(2).SetValue($"{reportItem.CustodianReport.Codextn.Code} {reportItem.CustodianReport.Department}").Style.Font.Bold = true;
                        row++;
                        ws.Row(12).CopyTo(ws.Row(++row));
                        ws.Row(13).CopyTo(ws.Row(++row));
                        ws.Row(14).CopyTo(ws.Row(++row));
                        ws.Range($"B{row - 2}:B{row}").Merge();
                        ws.Range($"C{row - 1}:C{row}").Merge();
                        ws.Range($"D{row - 1}:D{row}").Merge();
                        ws.Range($"E{row - 2}:E{row}").Merge();
                        ws.Range($"F{row - 2}:F{row}").Merge();
                        ws.Range($"G{row - 1}:G{row}").Merge();
                        ws.Range($"H{row - 1}:H{row}").Merge();
                        ws.Range($"I{row - 1}:I{row}").Merge();
                        ws.Range($"J{row - 1}:J{row}").Merge();
                        ws.Range($"K{row - 1}:K{row}").Merge();
                        ws.Range($"O{row - 2}:O{row}").Merge();
                        ws.Range($"P{row - 2}:P{row}").Merge();
                        ws.Range($"Q{row - 2}:Q{row}").Merge();
                        ws.Range($"R{row - 2}:R{row}").Merge();
                        ws.Range($"S{row - 2}:S{row}").Merge();
                        ws.Range($"T{row - 2}:T{row}").Merge();
                        ws.Range($"U{row - 2}:U{row}").Merge();
                        ws.Range($"V{row - 2}:V{row}").Merge();
                        ws.Range($"W{row - 2}:W{row}").Merge();
                        ws.Range($"X{row - 2}:X{row}").Merge();
                        ws.Range($"Y{row - 2}:Y{row}").Merge();
                        ws.Range($"Z{row - 2}:Z{row}").Merge();
                        ws.Range($"AA{row - 2}:AA{row}").Merge();
                        ws.Range($"AB{row - 2}:AB{row}").Merge();
                        ws.Range($"AC{row - 2}:AC{row}").Merge();
                        ws.Range($"AD{row - 2}:AD{row}").Merge();
                        ws.Range($"AE{row - 2}:AE{row}").Merge();
                        ws.Range($"AF{row - 2}:AF{row}").Merge();
                        ws.Range($"AG{row - 1}:AG{row}").Merge();
                        ws.Range($"AH{row - 1}:AH{row}").Merge();
                        ws.Range($"AI{row - 1}:AI{row}").Merge();
                        ws.Range($"AJ{row - 2}:AJ{row}").Merge();
                        ws.Range($"AK{row - 2}:AK{row}").Merge();
                        ws.Range($"AL{row - 2}:AL{row}").Merge();
                        ws.Range($"AM{row - 2}:AM{row}").Merge();
                        ws.Range($"AN{row - 2}:AN{row}").Merge();
                    }

                    row++;
                    if (!string.IsNullOrWhiteSpace(reportItem.SetLotNo))
                    {
                        if (setLotNo != reportItem.SetLotNo)
                        {
                            setLotNo = reportItem.SetLotNo;
                            reportItem.TotalCost = reportItem.SetLotAmount;
                        }
                        else
                        {
                            reportItem.TotalCost = 0;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(annex))
                    {
                        SetStockRowColValue(ws, reportItem, row, false);
                        ws.Range($"B{row}:AN{row}").Style.Border.BottomBorder = XLBorderStyleValues.Dotted;
                    }
                    else
                    {
                        SetStockRowColValue(ws, reportItem, row, true);
                        ws.Range($"B{row}:AM{row}").Style.Border.BottomBorder = XLBorderStyleValues.Dotted;
                    }

                    tAcqCost += (reportItem.TotalCost ?? 0);
                }
                ws.Row(++row).Cell(23).SetValue("TOTAL");
                ws.Row(row).Cell(24).SetValue(tAcqCost);

                row += 4;
                ws.Row(row).Cell(3).SetValue("Certified Correct by:").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Row(row).Cell(10).SetValue("Verified by:").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                row += 3;
                ws.Row(row).Cell(4).SetValue("Signature over Printed Name").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Row(row).Cell(11).SetValue("Signature over Printed Name").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Range($"D{row}:G{row}").Merge().Style.Border.TopBorder = XLBorderStyleValues.Medium;
                ws.Range($"K{row}:L{row}").Merge().Style.Border.TopBorder = XLBorderStyleValues.Medium;
                row++;
                ws.Row(row).Cell(4).SetValue("Custodian").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Row(row).Cell(11).SetValue("Assistant to the Custodian").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Range($"D{row}:G{row}").Merge();
                ws.Range($"K{row}:L{row}").Merge();

                // Create a MemoryStream to save the output
                var memoryStream = new MemoryStream();
                wb.SaveAs(memoryStream);

                // Reset the stream position to the beginning before returning
                memoryStream.Position = 0;
                return memoryStream;
            }
        }

        private MemoryStream ProcessExcelFilePpeTemplate(Guid? id, Guid? deptId, int? accountGroup, string templateFilePath, string mainAccount, DateTime? asOf)
        {
            return ProcessExcelFilePpeTemplate(id, deptId, accountGroup, templateFilePath, "", "", mainAccount, asOf);
        }

        private MemoryStream ProcessExcelFilePpeTemplate(Guid? id, Guid? deptId, int? accountGroup, string templateFilePath, string hdg, string annex, string mainAccount, DateTime? asOf)
        {
            using (XLWorkbook wb = new XLWorkbook(templateFilePath))
            {
                int sw = 1;
                int row = 14;
                string setLotNo = "";
                string account = "";
                string department = "";
                decimal? tAcqCost = 0;
                var ws = wb.Worksheet(1);
                IQueryable<CustodianReportItem> reportItems;
                reportItems = _db.CustodianReportItems
                    .Include(i => i.ItemCode.ItemType)
                    .Include(i => i.CustodianReport.Codextn)
                    .Include(i => i.Codextn) // deptId
                    .Include(i => i.Codextn1) // LocationId
                    .Include(i => i.CustodianReportItemIssuances)
                    .Where(w => w.CustodianReport.AccountGroup == accountGroup)
                    .AsNoTracking();

                if (!string.IsNullOrWhiteSpace(mainAccount))
                {
                    reportItems = reportItems.Where(w => w.ItemCode.ItemType.Description == mainAccount);
                }

                if (asOf.HasValue)
                {
                    reportItems = reportItems.Where(w => w.AcqDate <= asOf);
                }

                if (!string.IsNullOrWhiteSpace(annex))
                {
                    reportItems = reportItems.Where(w => w.Annex == annex);
                }

                if (id != null)
                {
                    reportItems = reportItems.Where(w => w.ReportId == id).OrderBy(t => t.ItemCode.ItemType.Description).ThenBy(o => o.CustodianItemNo).ThenBy(t => t.SetLotNo).ThenBy(t => t.SetLotRemarks).ThenBy(t => t.ItemCode.ItemNoIndex);
                }
                else
                {
                    if (deptId != null)
                    {
                        reportItems = reportItems.Where(w => w.CustodianReport.DeptId == deptId).OrderBy(t => t.ItemCode.ItemType.Description).ThenBy(o => o.CustodianReport.Department).ThenBy(o => o.CustodianItemNo).ThenBy(t => t.SetLotNo).ThenBy(t => t.SetLotRemarks).ThenBy(t => t.ItemCode.ItemNoIndex);
                    }
                    else
                    {
                        reportItems = reportItems.OrderBy(t => t.ItemCode.ItemType.Description).ThenBy(o => o.CustodianReport.Department).ThenBy(o => o.CustodianItemNo).ThenBy(t => t.SetLotNo).ThenBy(t => t.SetLotRemarks).ThenBy(t => t.ItemCode.ItemNoIndex);
                    }
                }

                foreach (var reportItem in reportItems)
                {
                    if (sw == 1)
                    {
                        account = reportItem.ItemCode.ItemType.Description;
                        department = reportItem.CustodianReport.Department;
                        if (!string.IsNullOrWhiteSpace(annex))
                        {
                            ws.Row(2).Cell(2).SetValue($"Annex {annex}");
                            ws.Row(4).Cell(2).SetValue(hdg);
                        }

                        if (asOf.HasValue)
                        {
                            ws.Row(5).Cell(2).SetValue($"As of {asOf.Value.ToShortDateString()}");
                        }
                        else
                        {
                            ws.Row(5).Cell(2).SetValue($"As of {DateTime.Now.ToShortDateString()}");
                        }

                        ws.Row(6).Cell(2).SetValue(account).Style.Font.Bold = true;

                        if (deptId == null || deptId == Guid.Empty)
                        {
                            ws.Row(8).Cell(3).SetValue("ALL").Style.Font.Bold = true;
                        }

                        ws.Row(10).Cell(2).SetValue($"{reportItem.CustodianReport.Codextn.Code} {reportItem.CustodianReport.Department}").Style.Font.Bold = true;
                        sw = 0;
                    }

                    if (account != reportItem.ItemCode.ItemType.Description || department != reportItem.CustodianReport.Department)
                    {
                        account = reportItem.ItemCode.ItemType.Description;
                        department = reportItem.CustodianReport.Department;
                        row += 3;
                        ws.Row(row).Cell(2).SetValue(account).Style.Font.Bold = true;
                        row += 2;
                        ws.Row(row).Cell(2).SetValue("Department:");
                        if (deptId == null || deptId == Guid.Empty)
                        {
                            ws.Row(row).Cell(3).SetValue("ALL").Style.Font.Bold = true;
                        }
                        row += 2;
                        ws.Row(row).Cell(2).SetValue($"{reportItem.CustodianReport.Codextn.Code} {reportItem.CustodianReport.Department}").Style.Font.Bold = true;
                        row++;
                        ws.Row(12).CopyTo(ws.Row(++row));
                        ws.Row(13).CopyTo(ws.Row(++row));
                        ws.Row(14).CopyTo(ws.Row(++row));
                        ws.Range($"B{row - 2}:B{row}").Merge();
                        ws.Range($"C{row - 1}:C{row}").Merge();
                        ws.Range($"D{row - 1}:D{row}").Merge();
                        ws.Range($"E{row - 2}:E{row}").Merge();
                        ws.Range($"F{row - 2}:F{row}").Merge();
                        ws.Range($"G{row - 1}:G{row}").Merge();
                        ws.Range($"H{row - 1}:H{row}").Merge();
                        ws.Range($"I{row - 1}:I{row}").Merge();
                        ws.Range($"J{row - 1}:J{row}").Merge();
                        ws.Range($"K{row - 1}:K{row}").Merge();
                        ws.Range($"O{row - 2}:O{row}").Merge();
                        ws.Range($"P{row - 2}:P{row}").Merge();
                        ws.Range($"Q{row - 2}:Q{row}").Merge();
                        ws.Range($"R{row - 2}:R{row}").Merge();
                        ws.Range($"S{row - 2}:S{row}").Merge();
                        ws.Range($"T{row - 2}:T{row}").Merge();
                        ws.Range($"U{row - 2}:U{row}").Merge();
                        ws.Range($"V{row - 2}:V{row}").Merge();
                        ws.Range($"W{row - 2}:W{row}").Merge();
                        ws.Range($"X{row - 2}:X{row}").Merge();
                        ws.Range($"Y{row - 2}:Y{row}").Merge();
                        ws.Range($"Z{row - 2}:Z{row}").Merge();
                        ws.Range($"AA{row - 2}:AA{row}").Merge();
                        ws.Range($"AB{row - 2}:AB{row}").Merge();
                        ws.Range($"AC{row - 2}:AC{row}").Merge();
                        ws.Range($"AD{row - 2}:AD{row}").Merge();
                        ws.Range($"AE{row - 2}:AE{row}").Merge();
                        ws.Range($"AF{row - 2}:AF{row}").Merge();
                        ws.Range($"AG{row - 1}:AG{row}").Merge();
                        ws.Range($"AH{row - 1}:AH{row}").Merge();
                        ws.Range($"AI{row - 1}:AI{row}").Merge();
                        ws.Range($"AJ{row - 2}:AJ{row}").Merge();
                        ws.Range($"AK{row - 2}:AK{row}").Merge();
                        ws.Range($"AL{row - 2}:AL{row}").Merge();
                        ws.Range($"AM{row - 2}:AM{row}").Merge();
                        ws.Range($"AN{row - 2}:AN{row}").Merge();
                    }

                    row++;
                    if (!string.IsNullOrWhiteSpace(reportItem.SetLotNo))
                    {
                        if (setLotNo != reportItem.SetLotNo)
                        {
                            setLotNo = reportItem.SetLotNo;
                            reportItem.TotalCost = reportItem.SetLotAmount;
                        }
                        else
                        {
                            reportItem.TotalCost = 0;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(annex))
                    {
                        SetPpeRowColValue(ws, reportItem, row, false);
                        ws.Range($"B{row}:AN{row}").Style.Border.BottomBorder = XLBorderStyleValues.Dotted;
                    }
                    else
                    {
                        SetPpeRowColValue(ws, reportItem, row, true);
                        ws.Range($"B{row}:AM{row}").Style.Border.BottomBorder = XLBorderStyleValues.Dotted;
                    }
                    tAcqCost += (reportItem.TotalCost ?? 0);
                }
                ws.Row(++row).Cell(23).SetValue("TOTAL");
                ws.Row(row).Cell(24).SetValue(tAcqCost);

                row += 4;
                ws.Row(row).Cell(3).SetValue("Certified Correct by:").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Row(row).Cell(10).SetValue("Verified by:").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                row += 3;
                ws.Row(row).Cell(4).SetValue("Signature over Printed Name").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Row(row).Cell(11).SetValue("Signature over Printed Name").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Range($"D{row}:G{row}").Merge().Style.Border.TopBorder = XLBorderStyleValues.Medium;
                ws.Range($"K{row}:L{row}").Merge().Style.Border.TopBorder = XLBorderStyleValues.Medium;
                row++;
                ws.Row(row).Cell(4).SetValue("Custodian").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Row(row).Cell(11).SetValue("Assistant to the Custodian").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Range($"D{row}:G{row}").Merge();
                ws.Range($"K{row}:L{row}").Merge();

                // Create a MemoryStream to save the output
                var memoryStream = new MemoryStream();
                wb.SaveAs(memoryStream);

                // Reset the stream position to the beginning before returning
                memoryStream.Position = 0;
                return memoryStream;
            }
        }

        private MemoryStream ProcessExcelFileVehicleTemplate(Guid? id, Guid? deptId, int? accountGroup, string templateFilePath, string mainAccount, DateTime? asOf)
        {
            return ProcessExcelFileVehicleTemplate(id, deptId, accountGroup, templateFilePath, "", "", mainAccount, asOf);
        }

        private MemoryStream ProcessExcelFileVehicleTemplate(Guid? id, Guid? deptId, int? accountGroup, string templateFilePath, string hdg, string annex, string mainAccount, DateTime? asOf)
        {
            using (XLWorkbook wb = new XLWorkbook(templateFilePath))
            {
                int sw = 1;
                int row = 14;
                string setLotNo = "";
                string account = "";
                string department = "";
                decimal? tAcqCost = 0;
                var ws = wb.Worksheet(1);
                IQueryable<CustodianReportItem> reportItems;
                reportItems = _db.CustodianReportItems
                    .Include(i => i.ItemCode.ItemType)
                    .Include(i => i.CustodianReport.Codextn)
                    .Include(i => i.Codextn) // deptId
                    .Include(i => i.Codextn1) // LocationId
                    .Include(i => i.CustodianReportItemIssuances)
                    .Where(w => w.CustodianReport.AccountGroup == accountGroup)
                    .AsNoTracking();

                if (!string.IsNullOrWhiteSpace(mainAccount))
                {
                    reportItems = reportItems.Where(w => w.ItemCode.ItemType.Description == mainAccount);
                }

                if (asOf.HasValue)
                {
                    reportItems = reportItems.Where(w => w.AcqDate <= asOf);
                }

                if (!string.IsNullOrWhiteSpace(annex))
                {
                    reportItems = reportItems.Where(w => w.Annex == annex);
                }

                if (id != null)
                {
                    reportItems = reportItems.Where(w => w.ReportId == id).OrderBy(t => t.ItemCode.ItemType.Description).ThenBy(o => o.CustodianItemNo).ThenBy(t => t.SetLotNo).ThenBy(t => t.SetLotRemarks).ThenBy(t => t.ItemCode.ItemNoIndex);
                }
                else
                {
                    if (deptId != null)
                    {
                        reportItems = reportItems.Where(w => w.CustodianReport.DeptId == deptId).OrderBy(t => t.ItemCode.ItemType.Description).ThenBy(o => o.CustodianReport.Department).ThenBy(o => o.CustodianItemNo).ThenBy(t => t.SetLotNo).ThenBy(t => t.SetLotRemarks).ThenBy(t => t.ItemCode.ItemNoIndex);
                    }
                    else
                    {
                        reportItems = reportItems.OrderBy(t => t.ItemCode.ItemType.Description).ThenBy(o => o.CustodianReport.Department).ThenBy(o => o.CustodianItemNo).ThenBy(t => t.SetLotNo).ThenBy(t => t.SetLotRemarks).ThenBy(t => t.ItemCode.ItemNoIndex);
                    }
                }

                foreach (var reportItem in reportItems)
                {
                    if (sw == 1)
                    {
                        account = reportItem.ItemCode.ItemType.Description;
                        department = reportItem.CustodianReport.Department;
                        if (!string.IsNullOrWhiteSpace(annex))
                        {
                            ws.Row(2).Cell(2).SetValue($"Annex {annex}");
                            ws.Row(4).Cell(2).SetValue(hdg);
                        }
                        if (asOf.HasValue)
                        {
                            ws.Row(5).Cell(2).SetValue($"As of {asOf.Value.ToShortDateString()}");
                        }
                        else
                        {
                            ws.Row(5).Cell(2).SetValue($"As of {DateTime.Now.ToShortDateString()}");
                        }
                        ws.Row(6).Cell(2).SetValue(account).Style.Font.Bold = true;

                        if (deptId == null || deptId == Guid.Empty)
                        {
                            ws.Row(8).Cell(3).SetValue("ALL").Style.Font.Bold = true;
                        }

                        ws.Row(10).Cell(2).SetValue($"{reportItem.CustodianReport.Codextn.Code} {reportItem.CustodianReport.Department}").Style.Font.Bold = true;
                        sw = 0;
                    }

                    if (account != reportItem.ItemCode.ItemType.Description || department != reportItem.CustodianReport.Department)
                    {
                        account = reportItem.ItemCode.ItemType.Description;
                        department = reportItem.CustodianReport.Department;
                        row += 3;
                        ws.Row(row).Cell(2).SetValue(account).Style.Font.Bold = true;
                        row += 2;
                        ws.Row(row).Cell(2).SetValue("Department:");
                        if (deptId == null || deptId == Guid.Empty)
                        {
                            ws.Row(row).Cell(3).SetValue("ALL").Style.Font.Bold = true;
                        }
                        row += 2;
                        ws.Row(row).Cell(2).SetValue($"{reportItem.CustodianReport.Codextn.Code} {reportItem.CustodianReport.Department}").Style.Font.Bold = true;
                        row++;
                        ws.Row(12).CopyTo(ws.Row(++row));
                        ws.Row(13).CopyTo(ws.Row(++row));
                        ws.Row(14).CopyTo(ws.Row(++row));
                        ws.Range($"B{row - 2}:B{row}").Merge();
                        ws.Range($"C{row - 1}:C{row}").Merge();
                        ws.Range($"D{row - 1}:D{row}").Merge();
                        ws.Range($"E{row - 2}:E{row}").Merge();
                        ws.Range($"F{row - 2}:F{row}").Merge();
                        ws.Range($"G{row - 1}:G{row}").Merge();
                        ws.Range($"H{row - 1}:H{row}").Merge();
                        ws.Range($"I{row - 1}:I{row}").Merge();
                        ws.Range($"J{row - 1}:J{row}").Merge();
                        ws.Range($"K{row - 1}:K{row}").Merge();
                        ws.Range($"L{row - 1}:L{row}").Merge();
                        ws.Range($"M{row - 1}:M{row}").Merge();
                        ws.Range($"N{row - 1}:N{row}").Merge();
                        ws.Range($"O{row - 1}:O{row}").Merge();
                        ws.Range($"P{row - 1}:P{row}").Merge();
                        ws.Range($"Q{row - 1}:Q{row}").Merge();
                        ws.Range($"R{row - 1}:R{row}").Merge();
                        ws.Range($"S{row - 1}:S{row}").Merge();
                        ws.Range($"W{row - 2}:W{row}").Merge();
                        ws.Range($"X{row - 2}:X{row}").Merge();
                        ws.Range($"Y{row - 2}:Y{row}").Merge();
                        ws.Range($"Z{row - 2}:Z{row}").Merge();
                        ws.Range($"AA{row - 2}:AA{row}").Merge();
                        ws.Range($"AB{row - 2}:AB{row}").Merge();
                        ws.Range($"AC{row - 2}:AC{row}").Merge();
                        ws.Range($"AD{row - 2}:AD{row}").Merge();
                        ws.Range($"AE{row - 2}:AE{row}").Merge();
                        ws.Range($"AF{row - 2}:AF{row}").Merge();
                        ws.Range($"AG{row - 2}:AG{row}").Merge();
                        ws.Range($"AH{row - 2}:AH{row}").Merge();
                        ws.Range($"AI{row - 2}:AI{row}").Merge();
                        ws.Range($"AJ{row - 2}:AJ{row}").Merge();
                        ws.Range($"AK{row - 2}:AK{row}").Merge();
                        ws.Range($"AL{row - 2}:AL{row}").Merge();
                        ws.Range($"AM{row - 2}:AM{row}").Merge();
                        ws.Range($"AN{row - 2}:AN{row}").Merge();
                        ws.Range($"AO{row - 1}:AO{row}").Merge();
                        ws.Range($"AP{row - 1}:AP{row}").Merge();
                        ws.Range($"AQ{row - 1}:AQ{row}").Merge();
                        ws.Range($"AR{row - 2}:AR{row}").Merge();
                        ws.Range($"AS{row - 2}:AS{row}").Merge();
                        ws.Range($"AT{row - 2}:AT{row}").Merge();
                        ws.Range($"AU{row - 2}:AU{row}").Merge();
                        ws.Range($"AV{row - 2}:AV{row}").Merge();
                    }

                    row++;
                    if (!string.IsNullOrWhiteSpace(reportItem.SetLotNo))
                    {
                        if (setLotNo != reportItem.SetLotNo)
                        {
                            setLotNo = reportItem.SetLotNo;
                            reportItem.TotalCost = reportItem.SetLotAmount;
                        }
                        else
                        {
                            reportItem.TotalCost = 0;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(annex))
                    {
                        SetVehicleRowColValue(ws, reportItem, row, false);
                        ws.Range($"B{row}:AV{row}").Style.Border.BottomBorder = XLBorderStyleValues.Dotted;
                    }
                    else
                    {
                        SetVehicleRowColValue(ws, reportItem, row, true);
                        ws.Range($"B{row}:AU{row}").Style.Border.BottomBorder = XLBorderStyleValues.Dotted;
                    }

                    tAcqCost += (reportItem.TotalCost ?? 0);
                }
                ws.Row(++row).Cell(31).SetValue("TOTAL");
                ws.Row(row).Cell(32).SetValue(tAcqCost);

                row += 4;
                ws.Row(row).Cell(3).SetValue("Certified Correct by:").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Row(row).Cell(10).SetValue("Verified by:").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                row += 3;
                ws.Row(row).Cell(4).SetValue("Signature over Printed Name").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Row(row).Cell(11).SetValue("Signature over Printed Name").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Range($"D{row}:G{row}").Merge().Style.Border.TopBorder = XLBorderStyleValues.Medium;
                ws.Range($"K{row}:L{row}").Merge().Style.Border.TopBorder = XLBorderStyleValues.Medium;
                row++;
                ws.Row(row).Cell(4).SetValue("Custodian").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Row(row).Cell(11).SetValue("Assistant to the Custodian").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Range($"D{row}:G{row}").Merge();
                ws.Range($"K{row}:L{row}").Merge();

                // Create a MemoryStream to save the output
                var memoryStream = new MemoryStream();
                wb.SaveAs(memoryStream);

                // Reset the stream position to the beginning before returning
                memoryStream.Position = 0;
                return memoryStream;
            }
        }

        public MemoryStream ProcessExcelFileAnnex(Guid? id, Guid? deptId, string templateFilePath, int? accountGroup, string annex, string mainAccount, DateTime? asOf)
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

            if (accountGroup == (int?)CustodianAccountGroup.STOCK)
            {
                return ProcessExcelFileStockTemplate(id, deptId, accountGroup, templateFilePath, hdg, annex, mainAccount, asOf);
            }
            else if (accountGroup == (int?)CustodianAccountGroup.PPE)
            {
                return ProcessExcelFilePpeTemplate(id, deptId, accountGroup, templateFilePath, hdg, annex, mainAccount, asOf);
            }
            else
            {
                return ProcessExcelFileVehicleTemplate(id, deptId, accountGroup, templateFilePath, hdg, annex, mainAccount, asOf);
            }
        }

        private void ValidateIfNull(CustodianReportItem model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(CustodianReportItem entity)
        {
            if (entity == null)
            {
                throw new NotFoundException(entity.Id);
            }
        }

        private void ValidateIfPosted(CustodianReportItem entity)
        {
            if (entity.PostedDt != null)
            {
                var msg = $"Record already posted by {entity.PostedBy} on {entity.PostedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        private void ValidateIfNotPosted(CustodianReportItem entity)
        {
            if (entity.PostedDt == null)
            {
                throw new RecordNotYetPostedException($"Record is not yet posted!");
            }
        }

        private void ValidateUser(CustodianReportItem entity, CustodianReportItem model)
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