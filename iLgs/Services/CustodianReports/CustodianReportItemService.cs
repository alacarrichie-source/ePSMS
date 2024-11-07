using ClosedXML.Excel;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.AllFields;
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
        MemoryStream ProcessExcelFile(Guid id, string templateFilePath, int? accountGroup);
        MemoryStream ProcessExcelFileAnnex(Guid id, string templateFilePath, int? accountGroup, string annex);
    }

    public class CustodianReportItemService : ICustodianReportItemService
    {
        private string _annex_a_hdg = "INVENTORY COUNT FORM - ";
        private string _annex_b_hdg = "INVENTORY COUNT FORM - ";

        protected readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<CustodianReportItem> _exceptionService = new ExceptionService<CustodianReportItem>();
        protected readonly IAllFieldService _allFieldService;

        public CustodianReportItemService(AppManEntities db)
        {
            _db = db;
            _allFieldService = new AllFieldService(_db);
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

            MapModelToEntityFields(entity, model, Mode.EDIT);            

            _db.CustodianReportItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        }

        public virtual async ValueTask<CustodianReportItem> DeleteAsync(CustodianReportItem model, string user, DateTime date)         {
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
            entity.AreNo = model.AreNo;
            entity.MrNo = model.MrNo;
            entity.ParIssuedTo = model.ParIssuedTo;
            entity.AccountableOfficer = model.AccountableOfficer;            
            entity.UpcomingPar = model.UpcomingPar;
            entity.Type = model.Type;
            entity.Annex = model.Annex;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
            //entity.PostedBy = model.PostedBy;
            //entity.PostedDt = model.PostedDt;
        }

        public MemoryStream ProcessExcelFile(Guid id, string templateFilePath, int? accountGroup)
        {
            // Load the template file
            FileInfo templateFile = new FileInfo(templateFilePath);
            if (!templateFile.Exists)
            {
                throw new FileNotFoundException("The template file does not exist.", templateFilePath);
            }
            if (accountGroup == (int?)CustodianAccountGroup.STOCK)
            {
                return ProcessExcelFileStockTemplate(id, templateFilePath);
            }
            else if (accountGroup == (int?)CustodianAccountGroup.PPE)
            {
                return ProcessExcelFilePpeTemplate(id, templateFilePath);
            }
            else
            {
                return ProcessExcelFileVehicleTemplate(id, templateFilePath);
            }
        }

        private MemoryStream ProcessExcelFileStockTemplate(Guid id, string templateFilePath)
        {
            int row = 8;
            int col = 0;
            using (XLWorkbook wb = new XLWorkbook(templateFilePath))
            {
                var ws = wb.Worksheet(1);
                var reportItemList = _db.CustodianReportItems.Include(i => i.CustodianReport.Codextn).Where(w => w.ReportId == id).ToList();                
                foreach (var reportItem in reportItemList)
                {
                    ws.Row(4).Cell(2).SetValue(reportItem.CustodianReport.Codextn.Description);
                    row++;
                    col = 0;
                    ws.Row(row).InsertRowsBelow(1);
                    ws.Row(row).Cell(++col).SetValue(reportItem.CustodianItemNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SeriesNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SubLocation);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Article);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PoNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PoDate);
                    ws.Row(row).Cell(++col).SetValue(reportItem.AirNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.AirDate);
                    ws.Row(row).Cell(++col).SetValue(reportItem.UnitCost);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Unit);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SetLotNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Department);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Qty);
                    ws.Row(row).Cell(++col).SetValue(reportItem.LocationCode);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Location);
                    ws.Row(row).Cell(++col).SetValue(reportItem.TransferIn);
                    ws.Row(row).Cell(++col).SetValue(reportItem.QtyBalance);
                    ws.Row(row).Cell(++col).SetValue(reportItem.TotalCost);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OldAmount);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OldPsNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PsNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Brand);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Model_);
                    ws.Row(row).Cell(++col).SetValue($"{reportItem.Dimension}/{reportItem.Size}/{reportItem.Weight}/{reportItem.Materials}");
                    ws.Row(row).Cell(++col).SetValue(reportItem.Color);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SerialNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Description);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OtherDesc);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OtherQty);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Condition);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Remarks);
                    ws.Row(row).Cell(++col).SetValue(reportItem.GenericName);
                    ws.Row(row).Cell(++col).SetValue(reportItem.DosageStrength);
                    ws.Row(row).Cell(++col).SetValue(reportItem.DosageForm);
                    ws.Row(row).Cell(++col).SetValue(reportItem.DosageVolume);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Multipliers);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PropNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PlateNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.BodyNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.MVFileNo);
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

        private MemoryStream ProcessExcelFilePpeTemplate(Guid id, string templateFilePath)
        {
            int row = 8;
            int col = 0;
            using (XLWorkbook wb = new XLWorkbook(templateFilePath))
            {
                var ws = wb.Worksheet(1);
                var reportItemList = _db.CustodianReportItems.Include(i => i.CustodianReport.Codextn).Where(w => w.ReportId == id).ToList();
                foreach (var reportItem in reportItemList)
                {
                    ws.Row(4).Cell(2).SetValue(reportItem.CustodianReport.Codextn.Description);
                    row++;
                    col = 0;
                    ws.Row(row).InsertRowsBelow(1);
                    ws.Row(row).Cell(++col).SetValue(reportItem.CustodianItemNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Department);
                    ws.Row(row).Cell(++col).SetValue(reportItem.LocationCode);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SeriesNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SubAccount);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Article);
                    ws.Row(row).Cell(++col).SetValue("");
                    ws.Row(row).Cell(++col).SetValue(reportItem.Type);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Brand);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Model_);                                       
                    ws.Row(row).Cell(++col).SetValue($"{reportItem.Dimension}/{reportItem.Size}/{reportItem.Weight}/{reportItem.Materials}/{reportItem.Capacity}");
                    ws.Row(row).Cell(++col).SetValue(reportItem.SerialNo);
                    ws.Row(row).Cell(++col).SetValue($"{reportItem.Description}/{reportItem.OtherDesc}");
                    ws.Row(row).Cell(++col).SetValue(reportItem.OtherQty);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Color);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OldPsNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PsNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OldAmount);
                    ws.Row(row).Cell(++col).SetValue(reportItem.TotalCost);
                    ws.Row(row).Cell(++col).SetValue(reportItem.AcqDate);
                    if (reportItem.FromDonation == true)
                    {
                        ws.Row(row).Cell(++col).SetValue("From Donation");
                    }
                    else
                    {
                        ws.Row(row).Cell(++col).SetValue("Purchased");
                    }
                    ws.Row(row).Cell(++col).SetValue(reportItem.SubLocation);
                    ws.Row(row).Cell(++col).SetValue(reportItem.ParIssuedTo);
                    ws.Row(row).Cell(++col).SetValue($"{reportItem.ParNo}/{reportItem.AreNo}/{reportItem.MrNo}");
                    ws.Row(row).Cell(++col).SetValue(reportItem.AccountableOfficer);
                    ws.Row(row).Cell(++col).SetValue(reportItem.UpcomingPar);
                    ws.Row(row).Cell(++col).SetValue("");
                    ws.Row(row).Cell(++col).SetValue(reportItem.Condition);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Remarks);

                    ws.Row(row).Cell(++col).SetValue(reportItem.PoNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PoDate);
                    ws.Row(row).Cell(++col).SetValue(reportItem.AirNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.AirDate);
                    ws.Row(row).Cell(++col).SetValue(reportItem.UnitCost);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Unit);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SetLotNo);
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

        private MemoryStream ProcessExcelFileVehicleTemplate(Guid id, string templateFilePath)
        {
            int row = 8;
            int col = 0;
            using (XLWorkbook wb = new XLWorkbook(templateFilePath))
            {
                var ws = wb.Worksheet(1);
                var reportItemList = _db.CustodianReportItems.Include(i => i.CustodianReport.Codextn).Where(w => w.ReportId == id).ToList();
                foreach (var reportItem in reportItemList)
                {
                    ws.Row(4).Cell(2).SetValue(reportItem.CustodianReport.Codextn.Description);
                    row++;
                    col = 0;
                    ws.Row(row).InsertRowsBelow(1);
                    ws.Row(row).Cell(++col).SetValue(reportItem.CustodianItemNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Department);
                    ws.Row(row).Cell(++col).SetValue(reportItem.LocationCode);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SeriesNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SubAccount);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Article);
                    ws.Row(row).Cell(++col).SetValue("");
                    ws.Row(row).Cell(++col).SetValue(reportItem.Brand);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Model_);
                    ws.Row(row).Cell(++col).SetValue(reportItem.YearModel);
                    ws.Row(row).Cell(++col).SetValue($"{reportItem.PlateNo}/{reportItem.CustodianItemNo}");
                    ws.Row(row).Cell(++col).SetValue(reportItem.BodyNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.EngineNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.ChasisNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Color);
                    ws.Row(row).Cell(++col).SetValue(reportItem.CRN);
                    ws.Row(row).Cell(++col).SetValue(reportItem.CRDate);
                    ws.Row(row).Cell(++col).SetValue(reportItem.MVFileNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OrNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OrDate);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Weight);
                    ws.Row(row).Cell(++col).SetValue(reportItem.InsPolicyNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OldPropNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PropNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OldAmount);
                    ws.Row(row).Cell(++col).SetValue(reportItem.TotalCost);
                    ws.Row(row).Cell(++col).SetValue(reportItem.AcqDate);
                    if (reportItem.FromDonation == true)
                    {
                        ws.Row(row).Cell(++col).SetValue("From Donation");
                    }
                    else
                    {
                        ws.Row(row).Cell(++col).SetValue("Purchased");
                    }
                    ws.Row(row).Cell(++col).SetValue(reportItem.SubLocation);
                    ws.Row(row).Cell(++col).SetValue(reportItem.ParIssuedTo);
                    ws.Row(row).Cell(++col).SetValue($"{reportItem.ParNo}/{reportItem.AreNo}/{reportItem.MrNo}");
                    ws.Row(row).Cell(++col).SetValue(reportItem.AccountableOfficer);
                    ws.Row(row).Cell(++col).SetValue(reportItem.UpcomingPar);
                    ws.Row(row).Cell(++col).SetValue("");
                    ws.Row(row).Cell(++col).SetValue(reportItem.Condition);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Remarks);

                    ws.Row(row).Cell(++col).SetValue(reportItem.PoNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PoDate);
                    ws.Row(row).Cell(++col).SetValue(reportItem.AirNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.AirDate);
                    ws.Row(row).Cell(++col).SetValue(reportItem.UnitCost);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Unit);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SetLotNo);
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

        public MemoryStream ProcessExcelFileAnnex(Guid id, string templateFilePath, int? accountGroup, string annex)
        {
            // Load the template file
            FileInfo templateFile = new FileInfo(templateFilePath);
            if (!templateFile.Exists)
            {
                throw new FileNotFoundException("The template file does not exist.", templateFilePath);
            }
            if (accountGroup == (int?)CustodianAccountGroup.STOCK)
            {
                return ProcessExcelFileStockAnnexTemplate(id, templateFilePath, annex);
            }
            else if (accountGroup == (int?)CustodianAccountGroup.PPE)
            {
                return ProcessExcelFilePpeAnnexTemplate(id, templateFilePath, annex);
            }
            else
            {
                return ProcessExcelFileVehicleAnnexTemplate(id, templateFilePath, annex);
            }
        }

        private MemoryStream ProcessExcelFileStockAnnexTemplate(Guid id, string templateFilePath, string annex)
        {
            int row = 8;
            int col = 0;
            using (XLWorkbook wb = new XLWorkbook(templateFilePath))
            {
                var ws = wb.Worksheet(1);
                var reportItemList = _db.CustodianReportItems.Include(i => i.CustodianReport.Codextn).Where(w => w.ReportId == id).ToList();
                foreach (var reportItem in reportItemList)
                {
                    ws.Row(4).Cell(2).SetValue(reportItem.CustodianReport.Codextn.Description);
                    row++;
                    col = 0;
                    ws.Row(row).InsertRowsBelow(1);
                    ws.Row(row).Cell(++col).SetValue(reportItem.CustodianItemNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SeriesNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SubLocation);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Article);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PoNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PoDate);
                    ws.Row(row).Cell(++col).SetValue(reportItem.AirNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.AirDate);
                    ws.Row(row).Cell(++col).SetValue(reportItem.UnitCost);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Unit);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SetLotNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Department);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Qty);
                    ws.Row(row).Cell(++col).SetValue(reportItem.LocationCode);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Location);
                    ws.Row(row).Cell(++col).SetValue(reportItem.TransferIn);
                    ws.Row(row).Cell(++col).SetValue(reportItem.QtyBalance);
                    ws.Row(row).Cell(++col).SetValue(reportItem.TotalCost);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OldAmount);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OldPsNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PsNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Brand);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Model_);
                    ws.Row(row).Cell(++col).SetValue($"{reportItem.Dimension}/{reportItem.Size}/{reportItem.Weight}/{reportItem.Materials}");
                    ws.Row(row).Cell(++col).SetValue(reportItem.Color);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SerialNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Description);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OtherDesc);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OtherQty);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Condition);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Remarks);
                    ws.Row(row).Cell(++col).SetValue(reportItem.GenericName);
                    ws.Row(row).Cell(++col).SetValue(reportItem.DosageStrength);
                    ws.Row(row).Cell(++col).SetValue(reportItem.DosageForm);
                    ws.Row(row).Cell(++col).SetValue(reportItem.DosageVolume);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Multipliers);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PropNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PlateNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.BodyNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.MVFileNo);
                    //ws.Row(row).Cell(++col).SetValue(reportItem.Annex);
                }

                if (annex == "A")
                {
                    
                }

                // Create a MemoryStream to save the output
                var memoryStream = new MemoryStream();
                wb.SaveAs(memoryStream);

                // Reset the stream position to the beginning before returning
                memoryStream.Position = 0;
                return memoryStream;
            }
        }

        private MemoryStream ProcessExcelFilePpeAnnexTemplate(Guid id, string templateFilePath, string annex)
        {
            int row = 8;
            int col = 0;
            using (XLWorkbook wb = new XLWorkbook(templateFilePath))
            {
                var ws = wb.Worksheet(1);
                var reportItemList = _db.CustodianReportItems.Include(i => i.CustodianReport.Codextn).Where(w => w.ReportId == id).ToList();
                foreach (var reportItem in reportItemList)
                {
                    ws.Row(4).Cell(2).SetValue(reportItem.CustodianReport.Codextn.Description);
                    row++;
                    col = 0;
                    ws.Row(row).InsertRowsBelow(1);
                    ws.Row(row).Cell(++col).SetValue(reportItem.CustodianItemNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Department);
                    ws.Row(row).Cell(++col).SetValue(reportItem.LocationCode);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SeriesNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SubAccount);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Article);
                    ws.Row(row).Cell(++col).SetValue("");
                    ws.Row(row).Cell(++col).SetValue(reportItem.Type);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Brand);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Model_);
                    ws.Row(row).Cell(++col).SetValue($"{reportItem.Dimension}/{reportItem.Size}/{reportItem.Weight}/{reportItem.Materials}/{reportItem.Capacity}");
                    ws.Row(row).Cell(++col).SetValue(reportItem.SerialNo);
                    ws.Row(row).Cell(++col).SetValue($"{reportItem.Description}/{reportItem.OtherDesc}");
                    ws.Row(row).Cell(++col).SetValue(reportItem.OtherQty);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Color);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OldPsNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PsNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OldAmount);
                    ws.Row(row).Cell(++col).SetValue(reportItem.TotalCost);
                    ws.Row(row).Cell(++col).SetValue(reportItem.AcqDate);
                    if (reportItem.FromDonation == true)
                    {
                        ws.Row(row).Cell(++col).SetValue("From Donation");
                    }
                    else
                    {
                        ws.Row(row).Cell(++col).SetValue("Purchased");
                    }
                    ws.Row(row).Cell(++col).SetValue(reportItem.SubLocation);
                    ws.Row(row).Cell(++col).SetValue(reportItem.ParIssuedTo);
                    ws.Row(row).Cell(++col).SetValue($"{reportItem.ParNo}/{reportItem.AreNo}/{reportItem.MrNo}");
                    ws.Row(row).Cell(++col).SetValue(reportItem.AccountableOfficer);
                    ws.Row(row).Cell(++col).SetValue(reportItem.UpcomingPar);
                    ws.Row(row).Cell(++col).SetValue("");
                    ws.Row(row).Cell(++col).SetValue(reportItem.Condition);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Remarks);

                    ws.Row(row).Cell(++col).SetValue(reportItem.PoNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PoDate);
                    ws.Row(row).Cell(++col).SetValue(reportItem.AirNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.AirDate);
                    ws.Row(row).Cell(++col).SetValue(reportItem.UnitCost);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Unit);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SetLotNo);
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

        private MemoryStream ProcessExcelFileVehicleAnnexTemplate(Guid id, string templateFilePath, string annex)
        {
            int row = 8;
            int col = 0;
            using (XLWorkbook wb = new XLWorkbook(templateFilePath))
            {
                var ws = wb.Worksheet(1);
                var reportItemList = _db.CustodianReportItems.Include(i => i.CustodianReport.Codextn).Where(w => w.ReportId == id).ToList();
                foreach (var reportItem in reportItemList)
                {
                    ws.Row(4).Cell(2).SetValue(reportItem.CustodianReport.Codextn.Description);
                    row++;
                    col = 0;
                    ws.Row(row).InsertRowsBelow(1);
                    ws.Row(row).Cell(++col).SetValue(reportItem.CustodianItemNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Department);
                    ws.Row(row).Cell(++col).SetValue(reportItem.LocationCode);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SeriesNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SubAccount);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Article);
                    ws.Row(row).Cell(++col).SetValue("");
                    ws.Row(row).Cell(++col).SetValue(reportItem.Brand);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Model_);
                    ws.Row(row).Cell(++col).SetValue(reportItem.YearModel);
                    ws.Row(row).Cell(++col).SetValue($"{reportItem.PlateNo}/{reportItem.CustodianItemNo}");
                    ws.Row(row).Cell(++col).SetValue(reportItem.BodyNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.EngineNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.ChasisNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Color);
                    ws.Row(row).Cell(++col).SetValue(reportItem.CRN);
                    ws.Row(row).Cell(++col).SetValue(reportItem.CRDate);
                    ws.Row(row).Cell(++col).SetValue(reportItem.MVFileNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OrNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OrDate);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Weight);
                    ws.Row(row).Cell(++col).SetValue(reportItem.InsPolicyNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OldPropNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PropNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.OldAmount);
                    ws.Row(row).Cell(++col).SetValue(reportItem.TotalCost);
                    ws.Row(row).Cell(++col).SetValue(reportItem.AcqDate);
                    if (reportItem.FromDonation == true)
                    {
                        ws.Row(row).Cell(++col).SetValue("From Donation");
                    }
                    else
                    {
                        ws.Row(row).Cell(++col).SetValue("Purchased");
                    }
                    ws.Row(row).Cell(++col).SetValue(reportItem.SubLocation);
                    ws.Row(row).Cell(++col).SetValue(reportItem.ParIssuedTo);
                    ws.Row(row).Cell(++col).SetValue($"{reportItem.ParNo}/{reportItem.AreNo}/{reportItem.MrNo}");
                    ws.Row(row).Cell(++col).SetValue(reportItem.AccountableOfficer);
                    ws.Row(row).Cell(++col).SetValue(reportItem.UpcomingPar);
                    ws.Row(row).Cell(++col).SetValue("");
                    ws.Row(row).Cell(++col).SetValue(reportItem.Condition);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Remarks);

                    ws.Row(row).Cell(++col).SetValue(reportItem.PoNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.PoDate);
                    ws.Row(row).Cell(++col).SetValue(reportItem.AirNo);
                    ws.Row(row).Cell(++col).SetValue(reportItem.AirDate);
                    ws.Row(row).Cell(++col).SetValue(reportItem.UnitCost);
                    ws.Row(row).Cell(++col).SetValue(reportItem.Unit);
                    ws.Row(row).Cell(++col).SetValue(reportItem.SetLotNo);
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
    }
}