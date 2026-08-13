using ClosedXML.Excel;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Items;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.PPMP_
{
    public interface IPPMPService
    {
        Task<IQueryable<PPMPVM>> GetAllAsync(string userId);
        IQueryable<PPMPVM> GetAllByDepartment(int? forYear, Guid? deptId);
        ValueTask<PPMPVM> GetByIdAsync(Guid? id);

        ValueTask<PPMPVM> PostAsync(Guid id, string user, DateTime date);
        ValueTask<PPMPVM> UnPostAsync(Guid id, string user, DateTime date);
        ValueTask<PPMPVM> CreateAsync(PPMPVM model, string user, DateTime date);
        ValueTask<PPMPVM> UpdateAsync(PPMPVM model, string user, DateTime date);
        ValueTask<PPMPVM> DeleteAsync(PPMPVM model, string user, DateTime date);

        IPPMPItemService PPMPItem { get; }

        //MemoryStream ProcessExcelFileSummary(int? forYear, Guid? deptId, Guid? locationId, DateTime? asOf, DateTime? insertedAsOf, string templateFilePath);
    }

    public class PPMPService : IPPMPService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<PPMPVM> _exceptionService;
        private readonly IUserService _userService;
        private readonly IPPMPSharedService _ppmpSharedService;

        private readonly IPPMPItemService _ppmpItemService;

        public PPMPService(AppManEntities db)
        {
            _db = db;
            _exceptions = new CreateAndLogExceptions();
            _exceptionService = new ExceptionService<PPMPVM>();
            _userService = new UserService(_db);
            _ppmpSharedService = new PPMPSharedService(_db);
            _ppmpItemService = new PPMPItemService(_db);
        }

        public IPPMPItemService PPMPItem => _ppmpItemService;

        private Expression<Func<PPMP, PPMPVM>> Projection()
        {
            return s => new PPMPVM
            {
                Id = s.Id,
                ForYear = s.ForYear,
                DeptId = s.DeptId,
                PostedBy = s.PostedBy,
                PostedDt = s.PostedDt,
                InsertedBy = s.InsertedBy,
                InsertedDt = s.InsertedDt,
                UpdatedBy = s.UpdatedBy,
                UpdatedDt = s.UpdatedDt,
                // transients
                Department = s.Codextn.Description
            };
        }

        public ValueTask<PPMPVM> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.PPMPs.Where(w => w.Id == id).Select(Projection()).FirstOrDefaultAsync();
            return data;
        });
        
        public async Task<IQueryable<PPMPVM>> GetAllAsync(string userId) 
        {
            var isAdmin = await _userService.IsAdminAsync(userId);
            var filteredQuery = _db.PPMPs.AsQueryable();
            if (!isAdmin)
            {
                filteredQuery = filteredQuery.Where(w =>
                    w.Codextn.DepartmentUsers.Any(a =>
                        a.UserId == userId && (
                            //// Condition 1: Direct Location Match
                            //(w.LocationId != null && a.Codextn.Id == w.LocationId) ||
                            //// Condition 2: Fallback to Dept Match if Location is null
                            (a.Codextn.Id == w.DeptId) ||
                            // Condition 3: Hierarchy/Parent Location Logic (SubAccount matching)
                            (_db.Codextns.Any(w2 =>
                                w2.CodeMast.Code == "LOCATIONS" &&
                                w2.Code.Substring(0, 2) == a.Codextn.Code.Substring(0, 2) &&
                                w2.Code.Trim().EndsWith("00")
                            ))
                        )
                    )
                );
            }

            var data = filteredQuery.AsNoTracking().Select(Projection());
            return data;
        }
        
        public IQueryable<PPMPVM> GetAllByDepartment(int? forYear, Guid? deptId)
        {
            var data = _db.PPMPs.AsNoTracking().Where(w => w.ForYear == forYear && w.DeptId == deptId).Select(Projection()).AsQueryable();
            return data;
        }
        
        public ValueTask<PPMPVM> PostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            //_validator.ValidateOnPost(id);
            await _ppmpSharedService.ValidateStatusAsync(id);

            var entity = await _db.PPMPs.FindAsync(id);

            entity.PostedBy = user;
            entity.PostedDt = date;
            
            await _db.SaveChangesAsync();

            return await GetByIdAsync(id);            
        });

        public ValueTask<PPMPVM> UnPostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {

            if (!(await _ppmpSharedService.IsPostedAsync(id)))
            {
                throw new RecordNotYetPostedException("PPMP Number is not yet Posted. Please verify.");
            }

            var entity = await _db.PPMPs.FindAsync(id);
            
            entity.PostedBy = "";
            entity.PostedDt = null;

            await _db.SaveChangesAsync();

            return await GetByIdAsync(id);
        });

        public ValueTask<PPMPVM> CreateAsync(PPMPVM model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new PPMP();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.PPMPs.Add(entity);
            await _db.SaveChangesAsync();

            return model;            
        });

        public ValueTask<PPMPVM> UpdateAsync(PPMPVM model, string user, DateTime date) =>
       _exceptionService.TryCatch(async () =>
       {
           //_validator.ValidateOnUpdate(model);
           await _ppmpSharedService.ValidateStatusAsync(model.Id);

           var entity = await _db.PPMPs.FindAsync(model.Id);

           model.UpdatedBy = user;
           model.UpdatedDt = date;

           MapModelToEntityFields(entity, model, Mode.EDIT);

           await _db.SaveChangesAsync();

           return model;           
       });

        public ValueTask<PPMPVM> DeleteAsync(PPMPVM model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            //_validator.ValidateOnDelete(model);

            await _ppmpSharedService.ValidateStatusAsync(model.Id);

            var entity = await _db.PPMPs.Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();

            _db.PPMPs.Remove(entity);
            await _db.SaveChangesAsync();

            return model;          
        });

        public void MapModelToEntityFields(PPMP entity, PPMPVM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            entity.ForYear = model.ForYear;
            entity.DeptId = model.DeptId;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        //public MemoryStream ProcessExcelFileSummary(int? forYear, Guid? deptId, Guid? locationId, DateTime? asOf, DateTime? insertedAsOf, string templateFilePath)
        //{
        //    using (XLWorkbook wb = new XLWorkbook(templateFilePath))
        //    {
        //        int row = 3;
        //        //string groupName = "";
        //        //string account = "";
        //        decimal? tAnnexA = 0;
        //        decimal? tAnnexB = 0;
        //        decimal? tAnnexC = 0;
        //        decimal? tAnnexX = 0;
        //        decimal? gTotalCost = 0;
        //        var ws = wb.Worksheet(1);
        //        var reportItems = _db.Database.SqlQuery<PPMPVMSummaryVM>("Exec PPMPVM_GetSummary {0}, {1}, {2}, {3}", forYear, deptId, locationId, asOf, insertedAsOf).AsQueryable();
        //        var accountGroups = reportItems
        //            .GroupBy(g => new { g.OrderNo, g.GroupName })
        //            .Select(s => new { s.Key.OrderNo, s.Key.GroupName }).OrderBy(o => o.OrderNo).ToList();

        //        foreach (var accountGroup in accountGroups)
        //        {
        //            row++;
        //            ws.Row(row).Cell(1).SetValue(accountGroup.GroupName).Style.Font.Bold = true;

        //            var accounts = reportItems.Where(w => w.OrderNo == accountGroup.OrderNo)
        //                .GroupBy(g => new { ItemNoIndex = g.ItemNoIndex.Substring(0, 2), g.Account })
        //                .Select(s => new { s.Key.ItemNoIndex, s.Key.Account })
        //                .OrderBy(o => o.ItemNoIndex)
        //                .ToList();

        //            foreach (var account in accounts)
        //            {
        //                row++;
        //                ws.Row(row).Cell(2).SetValue(account.Account).Style.Font.Bold = true;

        //                var subAccounts = reportItems.Where(w => w.OrderNo == accountGroup.OrderNo && w.Account == account.Account)
        //                    .OrderBy(o => o.ItemNoIndex).ToList();

        //                foreach (var subAccount in subAccounts)
        //                {
        //                    row++;
        //                    ws.Row(row).Cell(3).SetValue(subAccount.SubAccount1);
        //                    ws.Row(row).Cell(4).SetValue(subAccount.AnnexACost);
        //                    ws.Row(row).Cell(5).SetValue(subAccount.AnnexBCost);
        //                    ws.Row(row).Cell(6).SetValue(subAccount.AnnexCCost);
        //                    ws.Row(row).Cell(7).SetValue(subAccount.AnnexXCost);
        //                    ws.Row(row).Cell(9).SetValue(subAccount.TotalCost);

        //                    ws.Range($"C{row}:I{row}").Style.Border.BottomBorder = XLBorderStyleValues.Dotted;

        //                    tAnnexA += subAccount.AnnexACost;
        //                    tAnnexB += subAccount.AnnexBCost;
        //                    tAnnexC += subAccount.AnnexCCost;
        //                    tAnnexX += subAccount.AnnexXCost;
        //                    gTotalCost += subAccount.TotalCost;
        //                }
        //            }
        //            row++;
        //        }

        //        row++;
        //        var annexACell = ws.Row(row).Cell(4);
        //        annexACell.SetValue(tAnnexA);
        //        annexACell.Style.Font.Bold = true;
        //        annexACell.Style.Border.TopBorder = XLBorderStyleValues.Hair;
        //        annexACell.Style.Border.BottomBorder = XLBorderStyleValues.Double;

        //        var annexBCell = ws.Row(row).Cell(5);
        //        annexBCell.SetValue(tAnnexB);
        //        annexBCell.Style.Font.Bold = true;
        //        annexBCell.Style.Border.TopBorder = XLBorderStyleValues.Hair;
        //        annexBCell.Style.Border.BottomBorder = XLBorderStyleValues.Double;

        //        var annexCCell = ws.Row(row).Cell(6);
        //        annexCCell.SetValue(tAnnexC);
        //        annexCCell.Style.Font.Bold = true;
        //        annexCCell.Style.Border.TopBorder = XLBorderStyleValues.Hair;
        //        annexCCell.Style.Border.BottomBorder = XLBorderStyleValues.Double;

        //        var annexXCell = ws.Row(row).Cell(7);
        //        annexXCell.SetValue(tAnnexX);
        //        annexXCell.Style.Font.Bold = true;
        //        annexXCell.Style.Border.TopBorder = XLBorderStyleValues.Hair;
        //        annexXCell.Style.Border.BottomBorder = XLBorderStyleValues.Double;

        //        var gTotalCell = ws.Row(row).Cell(9);
        //        gTotalCell.SetValue(gTotalCost);
        //        gTotalCell.Style.Font.Bold = true;
        //        gTotalCell.Style.Border.TopBorder = XLBorderStyleValues.Hair;
        //        gTotalCell.Style.Border.BottomBorder = XLBorderStyleValues.Double;

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