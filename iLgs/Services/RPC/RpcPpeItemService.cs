using iLgs.Exceptions;
using iLgs.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services.RPC
{
    public interface IRpcPpeItemService
    {
        IQueryable<RpcPpeItem> GetByRpcPpeId(Guid? rpcPpeId);
        ValueTask<RpcPpeItem> GetByIdAsync(Guid id);

        //MemoryStream ProcessExcelFile(Guid id, string templateFilePath, int? accountGroup);
        //MemoryStream ProcessExcelFileAnnex(Guid id, string templateFilePath, int? accountGroup, string annex);
    }


    public class RpcPpeItemService : IRpcPpeItemService
    {
        protected readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RpcPpeItem> _exceptionService = new ExceptionService<RpcPpeItem>();
        protected readonly IUserService _userService;

        public RpcPpeItemService(AppManEntities db)
        {
            _db = db;
            _userService = new UserService(_db);
        }

        public ValueTask<RpcPpeItem> GetByIdAsync(Guid id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.RpcPpeItems.Where(w => w.Id == id).FirstOrDefaultAsync();
            return data;
        });

        
        public IQueryable<RpcPpeItem> GetByRpcPpeId(Guid? rpcPpeId) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.RpcPpeItems.AsNoTracking().Where(w => w.RpcPpeId == rpcPpeId).AsQueryable();
            return data;
        });
        
        
        //public MemoryStream ProcessExcelFile(Guid id, string templateFilePath, int? accountGroup)
        //{
        //    // Load the template file
        //    FileInfo templateFile = new FileInfo(templateFilePath);
        //    if (!templateFile.Exists)
        //    {
        //        throw new FileNotFoundException("The template file does not exist.", templateFilePath);
        //    }
        //    if (accountGroup == (int?)CustodianAccountGroup.STOCK)
        //    {
        //        return ProcessExcelFileStockTemplate(id, templateFilePath);
        //    }
        //    else if (accountGroup == (int?)CustodianAccountGroup.PPE)
        //    {
        //        return ProcessExcelFilePpeTemplate(id, templateFilePath);
        //    }
        //    else
        //    {
        //        return ProcessExcelFileVehicleTemplate(id, templateFilePath);
        //    }
        //}

        //private string GetSubAccount(string itemCode)
        //{
        //    var data = _db.Database.SqlQuery<string>("Select dbo.fn_SubAccount({0})", itemCode).FirstOrDefault();
        //    return data;
        //}

        //private MemoryStream ProcessExcelFileStockTemplate(Guid id, string templateFilePath)
        //{
        //    int sw = 1;
        //    int row = 9;
        //    int col = 0;
        //    decimal? tAcqCost = 0;
        //    using (XLWorkbook wb = new XLWorkbook(templateFilePath))
        //    {
        //        var ws = wb.Worksheet(1);
        //        var reportItemList = _db.RpcPpeItems.Include(i => i.ItemCode).Include(i => i.CustodianReport.Codextn).Where(w => w.ReportId == id).OrderBy(o => o.CustodianItemNo).ToList();
        //        foreach (var reportItem in reportItemList)
        //        {
        //            if (sw == 1)
        //            {
        //                ws.Row(2).Cell(1).SetValue($"As of {DateTime.Now.ToShortDateString()}");
        //                ws.Row(4).Cell(2).SetValue(reportItem.CustodianReport.Department);
        //                sw = 0;
        //            }
        //            row++;
        //            col = 0;
        //            ws.Row(row).InsertRowsBelow(1);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.CustodianItemNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Department);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.LocationCode);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.SeriesNo);
        //            //var subAccount = GetSubAccount(reportItem.ItemCode.Code);
        //            if (string.IsNullOrWhiteSpace(reportItem.SubAccount))
        //            {
        //                ws.Row(row).Cell(++col).SetValue($"{reportItem.Article}");
        //            }
        //            else
        //            {
        //                ws.Row(row).Cell(++col).SetValue($"{reportItem.SubAccount} / {reportItem.Article}");
        //            }
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Type);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Brand);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Model_);
        //            ws.Row(row).Cell(++col).SetValue($"{reportItem.Dimension} / {reportItem.Size} / {reportItem.Weight} / {reportItem.Materials} / {reportItem.Capacity}");
        //            ws.Row(row).Cell(++col).SetValue(reportItem.SerialNo);
        //            ws.Row(row).Cell(++col).SetValue($"{reportItem.Description} / {reportItem.OtherDesc}");
        //            ws.Row(row).Cell(++col).SetValue(reportItem.OtherQty);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Color);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.OldPsNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.PsNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.OldAmount);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.TotalCost);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AcqDate);
        //            if (reportItem.FromDonation == true)
        //            {
        //                ws.Row(row).Cell(++col).SetValue("From Donation");
        //            }
        //            else
        //            {
        //                ws.Row(row).Cell(++col).SetValue("Purchased");
        //            }
        //            ws.Row(row).Cell(++col).SetValue(reportItem.SubLocation);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.ParIssuedTo);
        //            ws.Row(row).Cell(++col).SetValue($"{reportItem.ParNo}/{reportItem.AreNo}/{reportItem.MrNo}");
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AccountableOfficer);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.UpcomingPar);
        //            ws.Row(row).Cell(++col).SetValue("");
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Condition);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Remarks);

        //            ws.Row(row).Cell(++col).SetValue(reportItem.PoNo);
        //            ws.Row(row).Cell(++col).SetValue(Utility.ExportDate(reportItem.PoDate));
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AirNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AirDate);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.UnitCost);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Unit);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.SetLotNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Annex);
        //            tAcqCost += (reportItem.TotalCost ?? 0);
        //        }
        //        ws.Row(++row).Cell(16).SetValue("TOTAL");
        //        ws.Row(row).Cell(17).SetValue(tAcqCost);

        //        // Create a MemoryStream to save the output
        //        var memoryStream = new MemoryStream();
        //        wb.SaveAs(memoryStream);

        //        // Reset the stream position to the beginning before returning
        //        memoryStream.Position = 0;
        //        return memoryStream;
        //    }
        //    //int sw = 1;
        //    //int row = 8;
        //    //int col = 0;
        //    //decimal? tAcqCost = 0;
        //    //using (XLWorkbook wb = new XLWorkbook(templateFilePath))
        //    //{
        //    //    var ws = wb.Worksheet(1);            
        //    //    var reportItemList = _db.RpcPpeItems.Include(i => i.ItemCode).Include(i => i.CustodianReport.Codextn).Where(w => w.ReportId == id).OrderBy(o => o.CustodianItemNo).ToList();                
        //    //    foreach (var reportItem in reportItemList)
        //    //    {
        //    //        if (sw == 1)
        //    //        {
        //    //            ws.Row(2).Cell(1).SetValue($"As of {DateTime.Now.ToShortDateString()}");
        //    //            ws.Row(4).Cell(2).SetValue(reportItem.CustodianReport.Department);
        //    //            sw = 0;
        //    //        }
        //    //        row++;
        //    //        col = 0;
        //    //        ws.Row(row).InsertRowsBelow(1);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.CustodianItemNo);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.SeriesNo);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.SubLocation);
        //    //        //var subAccount = GetSubAccount(reportItem.ItemCode.Code);
        //    //        if (string.IsNullOrWhiteSpace(reportItem.SubAccount))
        //    //        {
        //    //            ws.Row(row).Cell(++col).SetValue($"{reportItem.Article}");
        //    //        }
        //    //        else
        //    //        {
        //    //            ws.Row(row).Cell(++col).SetValue($"{reportItem.SubAccount} / {reportItem.Article}");
        //    //        }
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.PoNo);
        //    //        ws.Row(row).Cell(++col).SetValue(Utility.ExportDate(reportItem.PoDate));
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.AirNo);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.AirDate);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.UnitCost);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.Unit);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.SetLotNo);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.Department);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.Qty);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.LocationCode);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.Location);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.TransferIn);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.QtyBalance);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.TotalCost);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.OldAmount);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.OldPsNo);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.PsNo);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.Brand);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.Model_);
        //    //        ws.Row(row).Cell(++col).SetValue($"{reportItem.Dimension} / {reportItem.Size} / {reportItem.Weight} / {reportItem.Materials}");
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.Color);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.ItemSerialNo);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.Description);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.OtherDesc);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.OtherQty);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.Condition);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.Remarks);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.GenericName);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.DosageStrength);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.DosageForm);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.DosageVolume);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.Multipliers);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.PropNo);
        //    //        //ws.Row(row).Cell(++col).SetValue(reportItem.PlateNo);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.BodyNo);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.MVFileNo);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.Annex);
        //    //        tAcqCost += (reportItem.TotalCost ?? 0);
        //    //    }
        //    //    ws.Row(++row).Cell(17).SetValue("TOTAL");
        //    //    ws.Row(row).Cell(18).SetValue(tAcqCost);

        //    //    // Create a MemoryStream to save the output
        //    //    var memoryStream = new MemoryStream();
        //    //    wb.SaveAs(memoryStream);

        //    //    // Reset the stream position to the beginning before returning
        //    //    memoryStream.Position = 0;
        //    //    return memoryStream;
        //    //}
        //}

        //private MemoryStream ProcessExcelFilePpeTemplate(Guid id, string templateFilePath)
        //{
        //    int sw = 1;
        //    int row = 9;
        //    int col = 0;
        //    decimal? tAcqCost = 0;
        //    using (XLWorkbook wb = new XLWorkbook(templateFilePath))
        //    {
        //        var ws = wb.Worksheet(1);
        //        var reportItemList = _db.RpcPpeItems.Include(i => i.CustodianReport.Codextn).Where(w => w.ReportId == id).OrderBy(o => o.CustodianItemNo).ToList();
        //        foreach (var reportItem in reportItemList)
        //        {
        //            if (sw == 1)
        //            {
        //                ws.Row(2).Cell(1).SetValue($"As of {DateTime.Now.ToShortDateString()}");
        //                ws.Row(4).Cell(2).SetValue(reportItem.CustodianReport.Department);
        //                sw = 0;
        //            }
        //            row++;
        //            col = 0;
        //            ws.Row(row).InsertRowsBelow(1);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.CustodianItemNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Department);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.LocationCode);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.SeriesNo);
        //            //var subAccount = GetSubAccount(reportItem.ItemCode.Code);
        //            if (string.IsNullOrWhiteSpace(reportItem.SubAccount))
        //            {
        //                ws.Row(row).Cell(++col).SetValue($"{reportItem.Article}");
        //            }
        //            else
        //            {
        //                ws.Row(row).Cell(++col).SetValue($"{reportItem.SubAccount} / {reportItem.Article}");
        //            }
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Type);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Brand);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Model_);
        //            ws.Row(row).Cell(++col).SetValue($"{reportItem.Dimension} / {reportItem.Size} / {reportItem.Weight} / {reportItem.Materials} / {reportItem.Capacity}");
        //            ws.Row(row).Cell(++col).SetValue(reportItem.SerialNo);
        //            ws.Row(row).Cell(++col).SetValue($"{reportItem.Description} / {reportItem.OtherDesc}");
        //            ws.Row(row).Cell(++col).SetValue(reportItem.OtherQty);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Color);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.OldPsNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.PsNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.OldAmount);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.TotalCost);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AcqDate);
        //            if (reportItem.FromDonation == true)
        //            {
        //                ws.Row(row).Cell(++col).SetValue("From Donation");
        //            }
        //            else
        //            {
        //                ws.Row(row).Cell(++col).SetValue("Purchased");
        //            }
        //            ws.Row(row).Cell(++col).SetValue(reportItem.SubLocation);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.ParIssuedTo);
        //            ws.Row(row).Cell(++col).SetValue($"{reportItem.ParNo}/{reportItem.AreNo}/{reportItem.MrNo}");
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AccountableOfficer);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.UpcomingPar);
        //            ws.Row(row).Cell(++col).SetValue("");
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Condition);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Remarks);

        //            ws.Row(row).Cell(++col).SetValue(reportItem.PoNo);
        //            ws.Row(row).Cell(++col).SetValue(Utility.ExportDate(reportItem.PoDate));
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AirNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AirDate);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.UnitCost);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Unit);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.SetLotNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Annex);
        //            tAcqCost += (reportItem.TotalCost ?? 0);
        //        }
        //        ws.Row(++row).Cell(16).SetValue("TOTAL");
        //        ws.Row(row).Cell(17).SetValue(tAcqCost);

        //        // Create a MemoryStream to save the output
        //        var memoryStream = new MemoryStream();
        //        wb.SaveAs(memoryStream);

        //        // Reset the stream position to the beginning before returning
        //        memoryStream.Position = 0;
        //        return memoryStream;
        //    }
        //}

        //private MemoryStream ProcessExcelFileVehicleTemplate(Guid id, string templateFilePath)
        //{
        //    int sw = 1;
        //    int row = 9;
        //    int col = 0;
        //    decimal? tAcqCost = 0;
        //    using (XLWorkbook wb = new XLWorkbook(templateFilePath))
        //    {
        //        var ws = wb.Worksheet(1);
        //        var reportItemList = _db.RpcPpeItems.Include(i => i.CustodianReport.Codextn).Where(w => w.ReportId == id).OrderBy(o => o.CustodianItemNo).ToList();
        //        foreach (var reportItem in reportItemList)
        //        {
        //            if (sw == 1)
        //            {
        //                ws.Row(2).Cell(1).SetValue($"As of {DateTime.Now.ToShortDateString()}");
        //                ws.Row(4).Cell(2).SetValue(reportItem.CustodianReport.Department);
        //                sw = 0;
        //            }
        //            row++;
        //            col = 0;
        //            ws.Row(row).InsertRowsBelow(1);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.CustodianItemNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Department);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.LocationCode);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.SeriesNo);
        //            //var subAccount = GetSubAccount(reportItem.ItemCode.Code);
        //            if (string.IsNullOrWhiteSpace(reportItem.SubAccount))
        //            {
        //                ws.Row(row).Cell(++col).SetValue($"{reportItem.Article}");
        //            }
        //            else
        //            {
        //                ws.Row(row).Cell(++col).SetValue($"{reportItem.SubAccount} / {reportItem.Article}");
        //            }
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Brand);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Model_);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.YearModel);
        //            ws.Row(row).Cell(++col).SetValue($"{reportItem.PlateNo} / {reportItem.CustodianItemNo}");
        //            ws.Row(row).Cell(++col).SetValue(reportItem.BodyNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.EngineNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.ChasisNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Color);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.CRN);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.CRDate);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.MVFileNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.OrNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.OrDate);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Weight);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.InsPolicyNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.OldPropNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.PropNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.OldAmount);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.TotalCost);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AcqDate);
        //            if (reportItem.FromDonation == true)
        //            {
        //                ws.Row(row).Cell(++col).SetValue("From Donation");
        //            }
        //            else
        //            {
        //                ws.Row(row).Cell(++col).SetValue("Purchased");
        //            }
        //            ws.Row(row).Cell(++col).SetValue(reportItem.SubLocation);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.ParIssuedTo);
        //            ws.Row(row).Cell(++col).SetValue($"{reportItem.ParNo} / {reportItem.AreNo} / {reportItem.MrNo}");
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AccountableOfficer);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.UpcomingPar);
        //            ws.Row(row).Cell(++col).SetValue("");
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Condition);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Remarks);

        //            ws.Row(row).Cell(++col).SetValue(reportItem.PoNo);
        //            ws.Row(row).Cell(++col).SetValue(Utility.ExportDate(reportItem.PoDate));
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AirNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AirDate);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.UnitCost);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Unit);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.SetLotNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Annex);
        //            tAcqCost += (reportItem.TotalCost ?? 0);
        //        }
        //        ws.Row(++row).Cell(19).SetValue("TOTAL");
        //        ws.Row(row).Cell(20).SetValue(tAcqCost);

        //        // Create a MemoryStream to save the output
        //        var memoryStream = new MemoryStream();
        //        wb.SaveAs(memoryStream);

        //        // Reset the stream position to the beginning before returning
        //        memoryStream.Position = 0;
        //        return memoryStream;
        //    }
        //}

        //public MemoryStream ProcessExcelFileAnnex(Guid id, string templateFilePath, int? accountGroup, string annex)
        //{
        //    // Load the template file
        //    FileInfo templateFile = new FileInfo(templateFilePath);
        //    if (!templateFile.Exists)
        //    {
        //        throw new FileNotFoundException("The template file does not exist.", templateFilePath);
        //    }
        //    string hdg = "";

        //    if (annex == "A")
        //    {
        //        hdg = "(INVENTORY COUNT FORM)";
        //    }
        //    else if (annex == "B")
        //    {
        //        hdg = "(LIST OF PPEs, FOUND AT STATION)";
        //    }
        //    else if (annex == "C")
        //    {
        //        hdg = "(LIST OF NON-EXISTING/MISSING PPEs)";
        //    }

        //    if (accountGroup == (int?)CustodianAccountGroup.STOCK)
        //    {
        //        return ProcessExcelFileStockAnnexTemplate(id, templateFilePath, hdg, annex);
        //    }
        //    else if (accountGroup == (int?)CustodianAccountGroup.PPE)
        //    {
        //        return ProcessExcelFilePpeAnnexTemplate(id, templateFilePath, hdg, annex);
        //    }
        //    else
        //    {
        //        return ProcessExcelFileVehicleAnnexTemplate(id, templateFilePath, hdg, annex);
        //    }
        //}

        //private MemoryStream ProcessExcelFileStockAnnexTemplate(Guid id, string templateFilePath, string hdg, string annex)
        //{
        //    int sw = 1;
        //    int row = 10;
        //    int col = 0;
        //    decimal? tAcqCost = 0;
        //    using (XLWorkbook wb = new XLWorkbook(templateFilePath))
        //    {
        //        var ws = wb.Worksheet(1);
        //        var reportItemList = _db.RpcPpeItems.Include(i => i.CustodianReport.Codextn).Where(w => w.ReportId == id && w.Annex == annex).OrderBy(o => o.CustodianItemNo).ToList();
        //        foreach (var reportItem in reportItemList)
        //        {
        //            if (sw == 1)
        //            {
        //                ws.Row(1).Cell(1).SetValue($"ANNEX {annex}");
        //                ws.Row(3).Cell(1).SetValue(hdg);
        //                ws.Row(4).Cell(1).SetValue($"As of {DateTime.Now.ToShortDateString()}");
        //                ws.Row(5).Cell(2).SetValue(reportItem.CustodianReport.Department);
        //                sw = 0;
        //            }

        //            row++;
        //            col = 0;
        //            ws.Row(row).InsertRowsBelow(1);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.CustodianItemNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Department);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.LocationCode);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.SeriesNo);
        //            //ws.Row(row).Cell(++col).SetValue($"{reportItem.SubAccount} / {reportItem.Article}");
        //            if (string.IsNullOrWhiteSpace(reportItem.SubAccount))
        //            {
        //                ws.Row(row).Cell(++col).SetValue($"{reportItem.Article}");
        //            }
        //            else
        //            {
        //                ws.Row(row).Cell(++col).SetValue($"{reportItem.SubAccount} / {reportItem.Article}");
        //            }
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Type);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Brand);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Model_);
        //            ws.Row(row).Cell(++col).SetValue($"{reportItem.Dimension} / {reportItem.Size} / {reportItem.Weight} / {reportItem.Materials} / {reportItem.Capacity}");
        //            ws.Row(row).Cell(++col).SetValue(reportItem.ItemSerialNo);
        //            ws.Row(row).Cell(++col).SetValue($"{reportItem.Description} / {reportItem.OtherDesc}");
        //            ws.Row(row).Cell(++col).SetValue(reportItem.OtherQty);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Color);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.OldPsNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.PsNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.OldAmount);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.TotalCost);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AcqDate);
        //            if (reportItem.FromDonation == true)
        //            {
        //                ws.Row(row).Cell(++col).SetValue("From Donation");
        //            }
        //            else
        //            {
        //                ws.Row(row).Cell(++col).SetValue("Purchased");
        //            }
        //            ws.Row(row).Cell(++col).SetValue(reportItem.SubLocation);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.ParIssuedTo);
        //            ws.Row(row).Cell(++col).SetValue($"{reportItem.ParNo}/{reportItem.AreNo}/{reportItem.MrNo}");
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AccountableOfficer);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.UpcomingPar);
        //            ws.Row(row).Cell(++col).SetValue("");
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Condition);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Remarks);

        //            ws.Row(row).Cell(++col).SetValue(reportItem.PoNo);
        //            ws.Row(row).Cell(++col).SetValue(Utility.ExportDate(reportItem.PoDate));
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AirNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AirDate);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.UnitCost);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Unit);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.SetLotNo);
        //            //ws.Row(row).Cell(++col).SetValue(reportItem.Annex);
        //            tAcqCost += (reportItem.TotalCost ?? 0);
        //        }
        //        ws.Row(++row).Cell(16).SetValue("TOTAL");
        //        ws.Row(row).Cell(17).SetValue(tAcqCost);
        //        // Create a MemoryStream to save the output
        //        var memoryStream = new MemoryStream();
        //        wb.SaveAs(memoryStream);

        //        // Reset the stream position to the beginning before returning
        //        memoryStream.Position = 0;
        //        return memoryStream;
        //    }
        //    //int sw = 1;
        //    //int row = 10;
        //    //int col = 0;
        //    //decimal? tAcqCost = 0;
        //    //using (XLWorkbook wb = new XLWorkbook(templateFilePath))
        //    //{
        //    //    var ws = wb.Worksheet(1);
        //    //    var reportItemList = _db.RpcPpeItems.Include(i => i.CustodianReport.Codextn).Where(w => w.ReportId == id && w.Annex == annex).OrderBy(o => o.CustodianItemNo).ToList();                
        //    //    foreach (var reportItem in reportItemList)
        //    //    {
        //    //        if (sw == 1)
        //    //        {
        //    //            ws.Row(1).Cell(1).SetValue($"ANNEX {annex}");
        //    //            ws.Row(3).Cell(1).SetValue(hdg);
        //    //            ws.Row(4).Cell(1).SetValue($"As of {DateTime.Now.ToShortDateString()}");
        //    //            ws.Row(5).Cell(2).SetValue(reportItem.CustodianReport.Department);
        //    //            sw = 0;
        //    //        }
        //    //        row++;
        //    //        col = 0;
        //    //        ws.Row(row).InsertRowsBelow(1);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.CustodianItemNo);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.SeriesNo);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.SubLocation);
        //    //        //ws.Row(row).Cell(++col).SetValue($"{reportItem.SubAccount} / {reportItem.Article}");
        //    //        if (string.IsNullOrWhiteSpace(reportItem.SubAccount))
        //    //        {
        //    //            ws.Row(row).Cell(++col).SetValue($"{reportItem.Article}");
        //    //        }
        //    //        else
        //    //        {
        //    //            ws.Row(row).Cell(++col).SetValue($"{reportItem.SubAccount} / {reportItem.Article}");
        //    //        }
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.PoNo);
        //    //        ws.Row(row).Cell(++col).SetValue(Utility.ExportDate(reportItem.PoDate));
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.AirNo);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.AirDate);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.UnitCost);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.Unit);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.SetLotNo);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.Department);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.Qty);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.LocationCode);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.Location);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.TransferIn);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.QtyBalance);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.TotalCost);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.OldAmount);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.OldPsNo);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.PsNo);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.Brand);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.Model_);
        //    //        ws.Row(row).Cell(++col).SetValue($"{reportItem.Dimension} / {reportItem.Size} / {reportItem.Weight} / {reportItem.Materials}");
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.Color);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.ItemSerialNo);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.Description);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.OtherDesc);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.OtherQty);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.Condition);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.Remarks);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.GenericName);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.DosageStrength);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.DosageForm);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.DosageVolume);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.Multipliers);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.PropNo);
        //    //        //ws.Row(row).Cell(++col).SetValue(reportItem.PlateNo);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.BodyNo);
        //    //        ws.Row(row).Cell(++col).SetValue(reportItem.MVFileNo);
        //    //        //ws.Row(row).Cell(++col).SetValue(reportItem.Annex);
        //    //        tAcqCost += (reportItem.TotalCost ?? 0);
        //    //    }
        //    //    ws.Row(++row).Cell(17).SetValue("TOTAL");
        //    //    ws.Row(row).Cell(18).SetValue(tAcqCost);

        //    //    // Create a MemoryStream to save the output
        //    //    var memoryStream = new MemoryStream();
        //    //    wb.SaveAs(memoryStream);

        //    //    // Reset the stream position to the beginning before returning
        //    //    memoryStream.Position = 0;
        //    //    return memoryStream;
        //    //}
        //}

        //private MemoryStream ProcessExcelFilePpeAnnexTemplate(Guid id, string templateFilePath, string hdg, string annex)
        //{
        //    int sw = 1;
        //    int row = 10;
        //    int col = 0;
        //    decimal? tAcqCost = 0;
        //    using (XLWorkbook wb = new XLWorkbook(templateFilePath))
        //    {
        //        var ws = wb.Worksheet(1);
        //        var reportItemList = _db.RpcPpeItems.Include(i => i.CustodianReport.Codextn).Where(w => w.ReportId == id && w.Annex == annex).OrderBy(o => o.CustodianItemNo).ToList();
        //        foreach (var reportItem in reportItemList)
        //        {
        //            if (sw == 1)
        //            {
        //                ws.Row(1).Cell(1).SetValue($"ANNEX {annex}");
        //                ws.Row(3).Cell(1).SetValue(hdg);
        //                ws.Row(4).Cell(1).SetValue($"As of {DateTime.Now.ToShortDateString()}");
        //                ws.Row(5).Cell(2).SetValue(reportItem.CustodianReport.Department);
        //                sw = 0;
        //            }

        //            row++;
        //            col = 0;
        //            ws.Row(row).InsertRowsBelow(1);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.CustodianItemNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Department);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.LocationCode);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.SeriesNo);
        //            //ws.Row(row).Cell(++col).SetValue($"{reportItem.SubAccount} / {reportItem.Article}");
        //            if (string.IsNullOrWhiteSpace(reportItem.SubAccount))
        //            {
        //                ws.Row(row).Cell(++col).SetValue($"{reportItem.Article}");
        //            }
        //            else
        //            {
        //                ws.Row(row).Cell(++col).SetValue($"{reportItem.SubAccount} / {reportItem.Article}");
        //            }
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Type);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Brand);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Model_);
        //            ws.Row(row).Cell(++col).SetValue($"{reportItem.Dimension} / {reportItem.Size} / {reportItem.Weight} / {reportItem.Materials} / {reportItem.Capacity}");
        //            ws.Row(row).Cell(++col).SetValue(reportItem.ItemSerialNo);
        //            ws.Row(row).Cell(++col).SetValue($"{reportItem.Description} / {reportItem.OtherDesc}");
        //            ws.Row(row).Cell(++col).SetValue(reportItem.OtherQty);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Color);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.OldPsNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.PsNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.OldAmount);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.TotalCost);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AcqDate);
        //            if (reportItem.FromDonation == true)
        //            {
        //                ws.Row(row).Cell(++col).SetValue("From Donation");
        //            }
        //            else
        //            {
        //                ws.Row(row).Cell(++col).SetValue("Purchased");
        //            }
        //            ws.Row(row).Cell(++col).SetValue(reportItem.SubLocation);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.ParIssuedTo);
        //            ws.Row(row).Cell(++col).SetValue($"{reportItem.ParNo}/{reportItem.AreNo}/{reportItem.MrNo}");
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AccountableOfficer);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.UpcomingPar);
        //            ws.Row(row).Cell(++col).SetValue("");
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Condition);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Remarks);

        //            ws.Row(row).Cell(++col).SetValue(reportItem.PoNo);
        //            ws.Row(row).Cell(++col).SetValue(Utility.ExportDate(reportItem.PoDate));
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AirNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AirDate);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.UnitCost);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Unit);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.SetLotNo);
        //            //ws.Row(row).Cell(++col).SetValue(reportItem.Annex);
        //            tAcqCost += (reportItem.TotalCost ?? 0);
        //        }
        //        ws.Row(++row).Cell(16).SetValue("TOTAL");
        //        ws.Row(row).Cell(17).SetValue(tAcqCost);
        //        // Create a MemoryStream to save the output
        //        var memoryStream = new MemoryStream();
        //        wb.SaveAs(memoryStream);

        //        // Reset the stream position to the beginning before returning
        //        memoryStream.Position = 0;
        //        return memoryStream;
        //    }
        //}

        //private MemoryStream ProcessExcelFileVehicleAnnexTemplate(Guid id, string templateFilePath, string hdg, string annex)
        //{
        //    int sw = 1;
        //    int row = 10;
        //    int col = 0;
        //    decimal? tAcqCost = 0;
        //    using (XLWorkbook wb = new XLWorkbook(templateFilePath))
        //    {
        //        var ws = wb.Worksheet(1);
        //        var reportItemList = _db.RpcPpeItems.Include(i => i.CustodianReport.Codextn).Where(w => w.ReportId == id && w.Annex == annex).OrderBy(o => o.CustodianItemNo).ToList();
        //        foreach (var reportItem in reportItemList)
        //        {
        //            if (sw == 1)
        //            {
        //                ws.Row(1).Cell(1).SetValue($"ANNEX {annex}");
        //                ws.Row(3).Cell(1).SetValue(hdg);
        //                ws.Row(4).Cell(1).SetValue($"As of {DateTime.Now.ToShortDateString()}");
        //                ws.Row(5).Cell(2).SetValue(reportItem.CustodianReport.Department);
        //                sw = 0;
        //            }

        //            row++;
        //            col = 0;
        //            ws.Row(row).InsertRowsBelow(1);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.CustodianItemNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Department);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.LocationCode);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.SeriesNo);
        //            //ws.Row(row).Cell(++col).SetValue($"{reportItem.SubAccount} / {reportItem.Article}");
        //            if (string.IsNullOrWhiteSpace(reportItem.SubAccount))
        //            {
        //                ws.Row(row).Cell(++col).SetValue($"{reportItem.Article}");
        //            }
        //            else
        //            {
        //                ws.Row(row).Cell(++col).SetValue($"{reportItem.SubAccount} / {reportItem.Article}");
        //            }
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Brand);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Model_);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.YearModel);
        //            ws.Row(row).Cell(++col).SetValue($"{reportItem.PlateNo} / {reportItem.CustodianItemNo}");
        //            ws.Row(row).Cell(++col).SetValue(reportItem.BodyNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.EngineNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.ChasisNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Color);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.CRN);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.CRDate);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.MVFileNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.OrNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.OrDate);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Weight);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.InsPolicyNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.OldPropNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.PropNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.OldAmount);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.TotalCost);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AcqDate);
        //            if (reportItem.FromDonation == true)
        //            {
        //                ws.Row(row).Cell(++col).SetValue("From Donation");
        //            }
        //            else
        //            {
        //                ws.Row(row).Cell(++col).SetValue("Purchased");
        //            }
        //            ws.Row(row).Cell(++col).SetValue(reportItem.SubLocation);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.ParIssuedTo);
        //            ws.Row(row).Cell(++col).SetValue($"{reportItem.ParNo} / {reportItem.AreNo} / {reportItem.MrNo}");
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AccountableOfficer);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.UpcomingPar);
        //            ws.Row(row).Cell(++col).SetValue("");
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Condition);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Remarks);

        //            ws.Row(row).Cell(++col).SetValue(reportItem.PoNo);
        //            ws.Row(row).Cell(++col).SetValue(Utility.ExportDate(reportItem.PoDate));
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AirNo);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.AirDate);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.UnitCost);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.Unit);
        //            ws.Row(row).Cell(++col).SetValue(reportItem.SetLotNo);
        //            //ws.Row(row).Cell(++col).SetValue(reportItem.Annex);
        //            tAcqCost += (reportItem.TotalCost ?? 0);
        //        }
        //        ws.Row(++row).Cell(19).SetValue("TOTAL");
        //        ws.Row(row).Cell(20).SetValue(tAcqCost);

        //        // Create a MemoryStream to save the output
        //        var memoryStream = new MemoryStream();
        //        wb.SaveAs(memoryStream);

        //        // Reset the stream position to the beginning before returning
        //        memoryStream.Position = 0;
        //        return memoryStream;
        //    }
        //}

    }
}