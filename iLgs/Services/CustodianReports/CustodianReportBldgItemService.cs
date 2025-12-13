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
using System.Data.Entity.SqlServer;
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
        Task<string> GetStockNoAsync(CustodianReportBldgItem model);
        ValueTask<CustodianReportBldgItemVM> GetByIdAsync(Guid id);
        IQueryable<CustodianReportBldgItemVM> GetAll(Guid? reportId);
        IQueryable<CustodianReportBldgItemVM> GetAllByDeptAcctGroup(int? forYear, Guid? deptId, int? accountGroup, bool? isDemand);
        IQueryable<CustodianReportBldgItemVM> GetAllByDeptAcctGroupItemCodeId(int? forYear, Guid? deptId, int? accountGroup, string userName, bool? isDemand, Guid? itemCodeId);
        IQueryable<CustodianReportBldgItemVM> GetAllByAcctGroup(int? forYear, int? accountGroup, string userName);
        ValueTask UpdateItemCodeAsync(int? reportingYearEnd, string selectedIds, Guid? newItemId, string user, DateTime date);
        ValueTask<CustodianReportBldgItemVM> CreateAsync(CustodianReportBldgItemVM model, string user, DateTime date);
        ValueTask<CustodianReportBldgItemVM> UpdateAsync(CustodianReportBldgItemVM model, string user, DateTime date);
        ValueTask<CustodianReportBldgItemVM> DeleteAsync(CustodianReportBldgItemVM model, string user, DateTime date);
        ValueTask<CustodianReportBldgItem> PostAsync(Guid id, string user, DateTime date);
        ValueTask<CustodianReportBldgItem> UnPostAsync(Guid id, string user, DateTime date);
        //MemoryStream ProcessExcelFile(int? forYear, Guid? deptId, string templateFilePath, int? accountGroup);
        //MemoryStream ProcessExcelFileAnnex(int? forYear, Guid? deptId, string templateFilePath, int? accontGroup, string annex);
        MemoryStream ProcessExcelFile(int? forYear, Guid? deptId, Guid? sectionId, string templateFilePath, int? accountGroup, string userName);
        MemoryStream ProcessExcelFile(int? forYear, Guid? deptId, Guid? sectionId, string templateFilePath, int? accountGroup, string mainAccount, DateTime? asOf
            , string subAccount1, string subAccount2, string subAccount3, string subAccount4, string userName);
        MemoryStream ProcessExcelFileAnnex(int? forYear, Guid? deptId, Guid? sectionId, string templateFilePath, int? accountGroup, string annex, string mainAccount, DateTime? asOf
            , string subAccount1, string subAccount2, string subAccount3, string subAccount4, string userName);

        ICustodianReportBldgItemPhaseService CustodianReportBldgItemPhase { get; }
    }

    public class CustodianReportBldgItemService : BaseValidator, ICustodianReportBldgItemService
    {
        private readonly AppManEntities _db;
        private readonly IAppManEntitiesFactory _contextFactory;
        private readonly IExceptionService<CustodianReportBldgItemVM> _vmExceptionService;
        private readonly IExceptionService<CustodianReportBldgItem> _exceptionService;
        private readonly ICustodianReportItemPpeValidator _validator;
        private readonly IAllFieldService _allFieldService;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly IUserService _userService;
        private readonly ICustodianReportBldgItemPhaseService _custodianReportBldgItemPhase;
        private readonly ICustodianBldgUploadService _custodianBldgUploadService;

        public CustodianReportBldgItemService(AppManEntities db,
            IAppManEntitiesFactory appManEntitiesFactory,
            IExceptionService<CustodianReportBldgItemVM> vmExceptionService,
            IExceptionService<CustodianReportBldgItem> exceptionService,
            IAllFieldService allFieldService,
            IUserService userService,
            ICustodianReportBldgItemPhaseService custodianReportBldgItemPhase,
            ICustodianReportItemPpeValidator validator,
            ICustodianBldgUploadService custodianBldgUploadService)
        {
            _db = db;
            _contextFactory = appManEntitiesFactory;
            _vmExceptionService = vmExceptionService;
            _exceptionService = exceptionService;
            _validator = validator;
            _allFieldService = allFieldService;
            _userService = userService;
            _custodianReportBldgItemPhase = custodianReportBldgItemPhase;
            _getDisplayName = Utility.GetDisplayName<CustodianReportBldgItemVM>;
            _custodianBldgUploadService = custodianBldgUploadService;
        }

        public ICustodianReportBldgItemPhaseService CustodianReportBldgItemPhase => _custodianReportBldgItemPhase;

        private static Expression<Func<CustodianReportBldgItem, CustodianReportBldgItemVM>> CustodianReportBldgItemProjection(AppManEntities db)
        {
            return s => new CustodianReportBldgItemVM
            {
                Id = s.Id,
                MainDeptId = s.CustodianReport.DeptId,
                AccountGroup = s.CustodianReport.AccountGroup,
                ReportId = s.ReportId,
                //Fund = s.Fund, moved to phaseItems
                CustodianItemNo = s.CustodianItemNo,
                SeriesNo = s.SeriesNo,
                FromDonation = s.FromDonation,
                Account = s.Account,
                ItemCodeId = s.ItemCodeId,
                SubAccount = s.SubAccount,
                Article = s.Article,
                //BldgItem = s.BldgItem, moved to phaseItems
                PsNo = s.PsNo,
                PropNo = s.PropNo,
                //PoNo = s.PoNo,
                //OldAmount = s.OldAmount,
                //PoDate = s.PoDate,
                //AcqCost = s.AcqCost,
                DeptId = s.DeptId,
                Department = s.Department,
                LocationId = s.LocationId,
                LocationCode = s.LocationCode,
                Location = s.Location,
                SubLocation = s.SubLocation,
                //AcqMonth = s.AcqMonth,
                //AcqYear = s.AcqYear,
                //AcqDay = s.AcqDay,
                //AcqDate = s.AcqDate,
                //Address = s.Address,
                //ProjectName = s.ProjectName,
                //BuildingType = s.BuildingType,
                Area = s.Area,
                AppraiseValue = s.AppraiseValue,
                //TotalAmount = s.TotalAmount,
                //PhaseNo = s.PhaseNo,
                //PhaseAmountMooe = s.PhaseAmountMooe,
                //PhaseAmountCo = s.PhaseAmountCo,
                //StartYear = s.StartYear,
                //StartMonth = s.StartMonth,
                //StartDay = s.StartDay,
                //StartDate = s.StartDate,
                //TargetYear = s.TargetYear,
                //TargetMonth = s.TargetMonth,
                //TargetDay = s.TargetDay,
                //TargetDate = s.TargetDate,
                //PercentComplete = s.PercentComplete,
                //CompletionYear = s.CompletionYear,
                //CompletionMonth = s.CompletionMonth,
                //CompletionDay = s.CompletionDay,
                //CompletionDate = s.CompletionDate,
                //Status = s.Status,
                Condition = s.Condition,
                //Remarks = s.Remarks,
                Annex = s.Annex, // not required, updated ItemPhase annex value
                InsertedBy = s.InsertedBy,
                InsertedDt = s.InsertedDt,
                UpdatedBy = s.UpdatedBy,
                UpdatedDt = s.UpdatedDt,
                PostedBy = s.PostedBy,
                PostedDt = s.PostedDt,
                Longitude = s.Longitude,
                Latitude = s.Latitude,
                ItemType_Code = s.ItemCode.ItemType.Code,
                Item_Code = s.ItemCode.Code,
                IsSubmitted = db.CustodianReportSubmitForCounts.Any(a => a.ReportId == s.ReportId && a.LocationId == s.LocationId && a.Status == "Submit"),
                //IsSubmitted = s.CustodianReport.CustodianReportSubmitForCounts.Any(a => a.Status == "Submit" && a.LocationId == s.LocationId)
                // Transients
                AcqCost = s.CustodianReportBldgItemPhases.Sum(x => x.AcqCost),
                PhaseAmountCo = s.CustodianReportBldgItemPhases.Sum(x => x.CapitalOutlay),
                PhaseAmountMooe = s.CustodianReportBldgItemPhases.Sum(x => x.MOOE),
                PhaseCount = s.CustodianReportBldgItemPhases.Count()
            };
        }

        public async Task<string> GetStockNoAsync(CustodianReportBldgItem model)
        {
            model.AllField = SetAllField(model);
            return await _allFieldService.GetCustodianStockNoAsync(model);
        }

        public ValueTask<CustodianReportBldgItemVM> GetByIdAsync(Guid id) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.CustodianReportBldgItems
                .Where(w => w.Id == id)
                .Select(CustodianReportBldgItemProjection(_db)).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<CustodianReportBldgItemVM> GetAll(Guid? reportId)
        {
            var data = _db.CustodianReportBldgItems
            .AsNoTracking()
            .Where(w => w.ReportId == reportId)
            .Select(CustodianReportBldgItemProjection(_db));
            return data;
        }

        public IQueryable<CustodianReportBldgItemVM> GetAllByDeptAcctGroup(int? forYear, Guid? deptId, int? accountGroup, bool? isDemand)
        {
            var data = _db.CustodianReportBldgItems
            .AsNoTracking()
            .Where(w => w.CustodianReport.AsOf.Value.Year == forYear && w.CustodianReport.DeptId == deptId && w.CustodianReport.AccountGroup == accountGroup)
            .Select(CustodianReportBldgItemProjection(_db));

            if (data.Any() && isDemand == true)
            {
                data = data.Where(w => w.Annex == "C");
            }

            return data;
        }

        public IQueryable<CustodianReportBldgItemVM> GetAllByDeptAcctGroupItemCodeId(int? forYear, Guid? deptId, int? accountGroup, string userName, bool? isDemand, Guid? itemCodeId)
        {
            var data = _db.CustodianReportBldgItems
                .AsNoTracking()
                .Where(w => w.CustodianReport.AsOf.Value.Year == forYear 
                && (deptId == Guid.Empty || w.CustodianReport.DeptId == deptId) 
                && w.CustodianReport.AccountGroup == accountGroup && w.ItemCodeId == itemCodeId)
                .Select(CustodianReportBldgItemProjection(_db));

            if (data.Any() && isDemand == true)
            {
                data = data.Where(w => w.Annex == "C");
            }

            if (data.Any())
            {
                data = data.Where(w => w.ItemCodeId == itemCodeId);
            }

            return data;
        }

        public IQueryable<CustodianReportBldgItemVM> GetAllByAcctGroup(int? forYear, int? accountGroup, string userName)
        {
            var data = _db.CustodianReportBldgItems
            .AsNoTracking()
            .Where(w => w.CustodianReport.AsOf.Value.Year == forYear && w.CustodianReport.AccountGroup == accountGroup)
            .Select(CustodianReportBldgItemProjection(_db));

            return data;
        }

        private void ValidateRequired(CustodianReportBldgItemVM model)
        {
            _imex = new InvalidModelException();

            if (model.MainDeptId == null || model.MainDeptId == Guid.Empty)
            {
                _imex.UpsertDataList("Department", "Please select department before creating an entry.");
            }

            //if (string.IsNullOrWhiteSpace(model.PhaseNo))
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.PhaseNo)), "Field is required.");
            //}

            //if (!model.PhaseAmountCo.HasValue)
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.PhaseAmountCo)), "Field is required.");
            //}

            //if (!string.IsNullOrWhiteSpace(model.Annex) && model.Annex == "C")
            //{
            //    if (string.IsNullOrWhiteSpace(model.Remarks))
            //    {
            //        _imex.UpsertDataList(_getDisplayName(nameof(model.Remarks)), "Field is Required for Annex C.");
            //    }
            //}

            _imex.ThrowIfContainsErrors();
        }

        private void ValidateIfNull(CustodianReportBldgItemVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        public void ValidateReportingYearEnd(int? year)
        {
            if (year == null || year == 0)
            {
                throw new InvalidValueException("For Year is Required.");
            }

            var data = _db.Codextns.OrderByDescending(o => o.Description).FirstOrDefault(f => f.CodeMast.Code == "REPORT-YEAR-END" && f.Description == year.ToString());
            if (data == null)
            {
                throw new NotFoundException("Invalid reporting year end.");
            }
            else if (!string.IsNullOrWhiteSpace(data.Desc2) && data.Desc2.ToUpper() == "Y")
            {
                throw new RecordLockedException($"Reporting year-end {year} is already locked.");
            }
        }

        public async ValueTask UpdateItemCodeAsync(int? reportingYearEnd, string selectedIds, Guid? newItemId, string user, DateTime date)
        {
            ValidateReportingYearEnd(reportingYearEnd);

            if (newItemId == null)
            {
                throw new InvalidValueException("New Item Code is Required.");
            }

            var selectedIdList = selectedIds.Split(',').ToList();
            if (selectedIdList.Count() == 0)
            {
                throw new RecordNotFoundException("No Items to process");
            }

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                foreach (var selectedId in selectedIdList)
                {
                    var id = Guid.Parse(selectedId);
                    var entity = await ctx.CustodianReportBldgItems.FirstOrDefaultAsync(f => f.Id == id);
                    if (entity != null)
                    {
                        var newItemCode = await ctx.ItemCodes.FindAsync(newItemId);
                        if (newItemCode != null)
                        {
                            entity.ItemCodeId = newItemId;
                            entity.Item_Code = newItemCode.Code;
                            var psNo = await GetStockNoAsync(entity);
                            entity.PsNo = psNo;
                            entity.UpdatedBy = user;
                            entity.UpdatedDt = date;
                        }                        
                    }
                }

                await ctx.SaveChangesAsync();
            }
        }

        public ValueTask<CustodianReportBldgItemVM> CreateAsync(CustodianReportBldgItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);

            if (model.ForYear == 0 || model.ForYear == null)
            {
                throw new InvalidValueException("For Year is Required.");
            }

            ValidateRequired(model);

            model.AllField = SetAllField(model);
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var custodianReport = await ctx.CustodianReports.Where(w => w.AsOf.Value.Year == model.ForYear && w.DeptId == model.MainDeptId && w.AccountGroup == model.AccountGroup).SingleOrDefaultAsync();
                if (custodianReport == null)
                {
                    custodianReport = new CustodianReport();
                    custodianReport.Id = Guid.NewGuid();
                    custodianReport.AsOf = Utility.GetAsOfDate((int)model.ForYear);
                    custodianReport.DeptId = model.MainDeptId;
                    custodianReport.Department = model.MainDeptName;
                    custodianReport.AccountGroup = model.AccountGroup;
                    custodianReport.InsertedBy = user;
                    custodianReport.InsertedDt = date;
                    custodianReport.UpdatedBy = user;
                    custodianReport.UpdatedDt = date;
                    ctx.CustodianReports.Add(custodianReport);
                    await ctx.SaveChangesAsync();
                }

                model.ReportId = custodianReport.Id;
                ValidateIfSubmitted(model, ctx);
                var entity = new CustodianReportBldgItem();
                MapModelToEntityFields(entity, model, Mode.ADD);

                ctx.CustodianReportBldgItems.Add(entity);
                await ctx.SaveChangesAsync();

                return model;
            }
        });

        public ValueTask<CustodianReportBldgItemVM> UpdateAsync(CustodianReportBldgItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateRequired(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = await ctx.CustodianReportBldgItems.FindAsync(model.Id);
                ValidateRecord(entity);
                ValidateIfPosted(entity);
                ValidateIfSubmitted(model, ctx);

                MapModelToEntityFields(entity, model, Mode.EDIT);

                //ctx.CustodianReportBldgItems.Attach(entity);
                //ctx.Entry(entity).State = EntityState.Modified;
                await ctx.SaveChangesAsync();

                return model;
            }
        });

        public ValueTask<CustodianReportBldgItemVM> DeleteAsync(CustodianReportBldgItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = await ctx.CustodianReportBldgItems.FindAsync(model.Id);
                ValidateRecord(entity);
                ValidateIfPosted(entity);
                ValidateIfSubmitted(model, ctx);

                entity.UpdatedBy = model.UpdatedBy;
                entity.UpdatedDt = model.UpdatedDt;

                //ctx.CustodianReportBldgItems.Attach(entity);
                //ctx.Entry(entity).State = EntityState.Modified;
                await ctx.SaveChangesAsync();

                ctx.CustodianReportBldgItems.Remove(entity);
                //ctx.Entry(entity).State = EntityState.Deleted;
                await ctx.SaveChangesAsync();

                return model;
            }
        });

        public ValueTask<CustodianReportBldgItem> PostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = await ctx.CustodianReportBldgItems.FindAsync(id);
                ValidateRecord(entity);
                ValidateIfPosted(entity);

                if (!_custodianBldgUploadService.GetAllByImageId(id).Any())
                {
                    throw new NotFoundException("No uploaded images found for this record, cannot post!");
                }

                entity.PostedBy = user;
                entity.PostedDt = date;
                entity.UpdatedBy = user;
                entity.UpdatedDt = date;

                //ctx.CustodianReportBldgItems.Attach(entity);
                //ctx.Entry(entity).State = EntityState.Modified;
                await ctx.SaveChangesAsync();
                return entity;
            }
        });

        public ValueTask<CustodianReportBldgItem> UnPostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = await ctx.CustodianReportBldgItems.FindAsync(id);
                ValidateRecord(entity);
                ValidateIfNotPosted(entity);

                entity.PostedBy = "";
                entity.PostedDt = null;
                entity.UpdatedBy = user;
                entity.UpdatedDt = date;

                //ctx.CustodianReportBldgItems.Attach(entity);
                //ctx.Entry(entity).State = EntityState.Modified;
                await ctx.SaveChangesAsync();
                return entity;
            }
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
            //entity.Fund = model.Fund;
            entity.CustodianItemNo = model.CustodianItemNo;
            entity.SeriesNo = model.SeriesNo;
            entity.FromDonation = model.FromDonation;
            entity.Account = model.Account;
            entity.ItemCodeId = model.ItemCodeId;
            entity.SubAccount = model.SubAccount;
            entity.Article = model.Article;
            //entity.BldgItem = model.BldgItem;
            //entity.PoNo = model.PoNo;
            //entity.PoDate = model.PoDate;
            //entity.AcqCost = model.AcqCost;
            entity.DeptId = model.DeptId;
            entity.Department = model.Department;
            entity.LocationId = model.LocationId;
            entity.LocationCode = model.LocationCode;
            entity.SubLocation = model.SubLocation;
            entity.Location = model.Location;
            //entity.AcqMonth = model.AcqMonth;
            //entity.AcqYear = model.AcqYear;
            //entity.AcqDay = model.AcqDay;
            //entity.AcqDate = model.AcqDate;
            //entity.AcqDate = new DateTime((int)model.AcqYear, (int)((model.AcqMonth == null || model.AcqMonth == 0) ? 1 : model.AcqMonth), (int)((model.AcqDay == null || model.AcqDay == 0) ? 1 : model.AcqDay));
            entity.PsNo = model.PsNo;
            entity.PropNo = model.PropNo;
            //entity.OldAmount = model.OldAmount;
            //entity.Address = model.Address;
            //entity.ProjectName = model.ProjectName;
            //entity.BuildingType = model.BuildingType;
            entity.Area = model.Area;
            entity.AppraiseValue = model.AppraiseValue;
            //entity.TotalAmount = model.TotalAmount;
            //entity.PhaseNo = model.PhaseNo;
            //entity.PhaseAmountMooe = model.PhaseAmountMooe;
            //entity.PhaseAmountCo = model.PhaseAmountCo;
            //entity.StartYear = model.StartYear;
            //entity.StartMonth = model.StartMonth;
            //entity.StartDay = model.StartDay;
            //entity.StartDate = model.StartDate;
            //entity.StartDate = new DateTime((int)model.StartYear, (int)((model.StartMonth == null || model.StartMonth == 0) ? 1 : model.StartMonth), (int)((model.StartDay == null || model.StartDay == 0) ? 1 : model.StartDay));
            //entity.TargetYear = model.TargetYear;
            //entity.TargetMonth = model.TargetMonth;
            //entity.TargetDay = model.TargetDay;
            //entity.TargetDate = model.TargetDate;
            //entity.TargetDate = new DateTime((int)model.TargetYear, (int)((model.TargetMonth == null || model.TargetMonth == 0) ? 1 : model.TargetMonth), (int)((model.TargetDay == null || model.TargetDay == 0) ? 1 : model.TargetDay));
            //entity.PercentComplete = model.PercentComplete;
            //entity.CompletionYear = model.CompletionYear;
            //entity.CompletionMonth = model.CompletionMonth;
            //entity.CompletionDay = model.CompletionDay;
            //entity.CompletionDate = model.CompletionDate;
            //entity.Status = model.Status;
            entity.Condition = model.Condition;
            //entity.Remarks = model.Remarks;
            entity.Annex = model.Annex;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
            entity.PostedBy = model.PostedBy;
            entity.PostedDt = model.PostedDt;
            entity.Latitude = model.Latitude;
            entity.Longitude = model.Longitude;
        }

        public MemoryStream ProcessExcelFile(int? forYear, Guid? deptId, Guid? sectionId, string templateFilePath, int? accountGroup, string userName)
        {
            return ProcessExcelFile(forYear, deptId, sectionId, templateFilePath, accountGroup, "", null, "", "", "", "", userName);
        }

        //public MemoryStream ProcessExcelFile(int? forYear, Guid? deptId, string templateFilePath, int? accountGroup)
        public MemoryStream ProcessExcelFile(int? forYear, Guid? deptId, Guid? sectionId, string templateFilePath, int? accountGroup, string mainAccount, DateTime? asOf
            , string subAccount1, string subAccount2, string subAccount3, string subAccount4, string userName)
        {
            // Load the template file
            FileInfo templateFile = new FileInfo(templateFilePath);
            if (!templateFile.Exists)
            {
                throw new FileNotFoundException("The template file does not exist.", templateFilePath);
            }
            //return ProcessExcelFileTemplate(forYear, deptId, accountGroup, templateFilePath);
            return ProcessExcelFileTemplate(forYear, deptId, sectionId, accountGroup, templateFilePath, mainAccount, asOf
                    , subAccount1, subAccount2, subAccount3, subAccount4, userName);
        }

        private string GetSubAccount(string itemCode)
        {
            var data = _db.Database.SqlQuery<string>("Select dbo.fn_SubAccount({0})", itemCode).FirstOrDefault();
            return data;
        }

        private void SetRowColValue(IXLWorksheet ws, CustodianReportBldgItem reportItem, int row, string annex)
        {
            ws.Row(row).Cell(2).SetValue(reportItem.CustodianItemNo);
            ws.Row(row).Cell(3).SetValue(reportItem.SeriesNo);
            ws.Row(row).Cell(4).SetValue(reportItem.PropNo);
            ws.Row(row).Cell(5).SetValue(reportItem.LocationCode);
            ws.Row(row).Cell(6).SetValue(reportItem.Location);
            ws.Row(row).Cell(7).SetValue(reportItem.SubLocation);
            ws.Row(row).Cell(8).SetValue(reportItem.Latitude);
            ws.Row(row).Cell(9).SetValue(reportItem.Longitude);
            //ws.Row(row).Cell(10).SetValue(reportItem.BldgItem);            

            if (reportItem.FromDonation == true)
            {
                ws.Row(row).Cell(13).SetValue("From Donation");
            }
            else
            {
                ws.Row(row).Cell(13).SetValue("Purchased");
            }
            ws.Row(row).Cell(16).SetValue(reportItem.Area);
            ws.Row(row).Cell(17).SetValue(reportItem.AppraiseValue);
            ws.Row(row).Cell(22).SetValue(reportItem.CustodianReportBldgItemPhases.Sum(s => s.CapitalOutlay));
            ws.Row(row).Cell(30).SetValue(reportItem.Condition);

            // display outside
            //if (reportItem.CustodianReportBldgItemPhases.Any())
            //{
            //    var engAmt = reportItem.CustodianReportBldgItemPhases.OrderBy(o => o.InsertedDt).FirstOrDefault();
            //    ws.Row(row).Cell(10).SetValue(engAmt.BldgItem);
            //    ws.Row(row).Cell(11).SetValue(engAmt.ProjectName);
            //    ws.Row(row).Cell(12).SetValue(engAmt.BuildingType);
            //    ws.Row(row).Cell(15).SetValue(engAmt.AcqDate.HasValue ? engAmt.AcqDate.Value.Year.ToString() : "");
            //    ws.Row(row).Cell(18).SetValue(engAmt.OldAmount);
            //    ws.Row(row).Cell(19).SetValue(engAmt.AcqCost);
            //    ws.Row(row).Cell(20).SetValue(engAmt.PhaseNo);
            //    ws.Row(row).Cell(21).SetValue(engAmt.CapitalOutlay);
            //ws.Row(row).Cell(22).SetValue(reportItem.CustodianReportBldgItemPhases.Sum(s => s.CapitalOutlay));                
            //    ws.Row(row).Cell(23).SetValue(engAmt.MOOE);
            //    ws.Row(row).Cell(24).SetValue(engAmt.StartDate).Style.DateFormat.Format = "MM/dd/yyyy";
            //    ws.Row(row).Cell(25).SetValue(engAmt.TargetDate.HasValue ? $"{engAmt.TargetDate.Value.Month}/{engAmt.TargetDate.Value.Year}" : "");
            //    ws.Row(row).Cell(26).SetValue(engAmt.PercentComplete);
            //    ws.Row(row).Cell(27).SetValue(engAmt.CompletionDate).Style.DateFormat.Format = "MM/dd/yyyy";
            //    ws.Row(row).Cell(28).SetValue(engAmt.Status);
            //    ws.Row(row).Cell(29).SetValue(engAmt.Fund);
            //    ws.Row(row).Cell(31).SetValue(engAmt.Remarks);
            //    if (!string.IsNullOrWhiteSpace(annex))
            //    {
            //        ws.Row(row).Cell(32).SetValue(engAmt.Annex);
            //    }
            //}

            //ws.Row(row).Cell(29).SetValue(reportItem.Fund);
            //ws.Row(row).Cell(30).SetValue(reportItem.Condition);
            //if (!isAnnex)
            //{
            //    ws.Row(row).Cell(32).SetValue(reportItem.Annex);
            //}
        }

        //private MemoryStream ProcessExcelFileTemplate(int? forYear, Guid? deptId, int? accountGroup, string templateFilePath)
        //{
        //    return ProcessExcelFileTemplate(forYear, deptId, accountGroup, templateFilePath, "", "");
        //}

        private MemoryStream ProcessExcelFileTemplate(int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, string templateFilePath, string mainAccount, DateTime? asOf
            , string subAccount1, string subAccount2, string subAccount3, string subAccount4, string userName)
        {
            return ProcessExcelFileTemplate(forYear, deptId, sectionId, accountGroup, templateFilePath, "", "", mainAccount, asOf, subAccount1, subAccount2, subAccount3, subAccount4, userName);
        }

        //private MemoryStream ProcessExcelFileTemplate(int? forYear, Guid? deptId, int? accountGroup, string templateFilePath, string hdg, string annex)
        private MemoryStream ProcessExcelFileTemplate(int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup
            , string templateFilePath, string hdg, string annex, string mainAccount, DateTime? asOf
            , string subAccount1, string subAccount2, string subAccount3, string subAccount4, string userName)
        {

            using (XLWorkbook wb = new XLWorkbook(templateFilePath))
            {
                int sw = 1;
                int row = 12;
                string itemTypeIndex = "";
                string itemCode = "";
                string account = "";
                string department = "";
                decimal? tAcqCost = 0;
                var ws = wb.Worksheet(1);
                var subAccount = new[] { subAccount4, subAccount3, subAccount2, subAccount1 }.FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? string.Empty;
                var reportItems = _db.CustodianReportBldgItems
                    .Include(i => i.ItemCode.ItemType)
                    .Include(i => i.CustodianReport.Codextn)
                    .Include(i => i.Codextn) // deptId
                    .Include(i => i.CustodianReportBldgItemPhases)
                    .Where(w => w.CustodianReport.AccountGroup == accountGroup && w.CustodianReport.AsOf.Value.Year == forYear);

                if (!string.IsNullOrWhiteSpace(subAccount))
                {
                    reportItems = reportItems.Where(w => w.Item_Code.StartsWith(subAccount));
                }

                if (!string.IsNullOrWhiteSpace(annex))
                {
                    //reportItems = reportItems.Where(w => w.Annex == annex);
                    reportItems = reportItems.Where(w => w.CustodianReportBldgItemPhases.Any(a => a.Annex == annex));
                }

                if (deptId != null)
                {
                    reportItems = reportItems.Where(w => w.CustodianReport.DeptId == deptId);

                }

                reportItems = reportItems.OrderBy(t => t.ItemCode.ItemType.Code)
                            .ThenBy(t => t.ItemCode.ItemType.GroupCode)
                            .ThenBy(t => t.ItemCode.ItemNoIndex)
                            .ThenBy(o => o.CustodianReport.Department)
                            .ThenBy(o => o.LocationCode)
                            .ThenBy(o => o.CustodianItemNo).AsNoTracking();

                if (!string.IsNullOrWhiteSpace(annex))
                {
                    ws.Row(2).Cell(2).SetValue($"Annex {annex}");
                    ws.Row(4).Cell(2).SetValue(hdg);
                }
                ws.Row(5).Cell(2).SetValue($"As of {DateTime.Now.ToShortDateString()}");
                ws.Row(6).Cell(2).SetValue(account).Style.Font.Bold = true;

                //if (deptId == null)
                //{                    
                //    if (reportItems.Any())
                //    {                        
                //        var report = ctx.CustodianReports.Include(i => i.Codextn).FirstOrDefault(f => f.Id == reportItems.First().ReportId);
                //        ws.Row(8).Cell(3).SetValue($"ALL : {report.Codextn.Code} {report.Department}").Style.Font.Bold = true;
                //    }
                //}
                //else
                //{
                //    var codextn = ctx.Codextns.Find(deptId);
                //    ws.Row(8).Cell(3).SetValue($"{codextn.Code} {codextn.Description}").Style.Font.Bold = true;
                //}

                if (deptId == null || deptId == Guid.Empty)
                {
                    if (reportItems.Any())
                    {
                        var reportId = reportItems.First().ReportId;
                        var report = _db.CustodianReports.Include(i => i.Codextn).FirstOrDefault(f => f.Id == reportId);
                        ws.Row(8).Cell(3).SetValue($"ALL : {report.Codextn.Code} {report.Department}").Style.Font.Bold = true;
                    }
                    else
                    {
                        ws.Row(8).Cell(3).SetValue("ALL").Style.Font.Bold = true;
                    }
                }
                else
                {
                    var codextn = _db.Codextns.Find(deptId);
                    ws.Row(8).Cell(3).SetValue($"{codextn.Code} {codextn.Description}").Style.Font.Bold = true;
                }

                foreach (var reportItem in reportItems)
                {
                    if (sw == 1)
                    {
                        itemTypeIndex = reportItem.ItemCode == null ? "" : reportItem.ItemCode.ItemType.Code + reportItem.ItemCode.ItemType.GroupCode;
                        itemCode = reportItem.ItemCode == null ? "" : reportItem.ItemCode.ItemNoIndex;
                        account = reportItem.ItemCode == null ? "" : reportItem.ItemCode.ItemType.Description;
                        department = reportItem.CustodianReport.Department;                        
                        sw = 0;
                    }

                    if (reportItem.ItemCode != null && (itemTypeIndex != reportItem.ItemCode.ItemType.Code + reportItem.ItemCode.ItemType.GroupCode || department != reportItem.CustodianReport.Department))
                    {
                        itemTypeIndex = reportItem.ItemCode.ItemType.Code + reportItem.ItemCode.ItemType.GroupCode;
                        itemCode = reportItem.ItemCode.ItemNoIndex;
                        account = reportItem.ItemCode.ItemType.Description;
                        department = reportItem.CustodianReport.Department;
                        row += 3;
                        var accountCell = ws.Row(row).Cell(2);
                        accountCell.SetValue(account);
                        accountCell.Style.Font.Bold = true;
                        accountCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        accountCell.Style.Alignment.WrapText = false;
                        if (accountCell.IsMerged())
                        {
                            accountCell.MergedRange().Unmerge();
                        }

                        row += 2;
                        var custCell = ws.Row(row).Cell(2);
                        custCell.SetValue("Custodian:").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        custCell.Style.Font.Bold = false;
                        custCell.Style.Alignment.WrapText = false;
                        if (custCell.IsMerged())
                        {
                            custCell.MergedRange().Unmerge();
                        }

                        var custCellVal = ws.Row(row).Cell(3);
                        custCellVal.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        custCellVal.Style.Font.Bold = true;
                        custCellVal.Style.Alignment.WrapText = false;
                        if (deptId == null)
                        {
                            custCellVal.SetValue($"ALL : {reportItem.CustodianReport.Codextn.Code} {reportItem.CustodianReport.Department}");
                        }
                        else
                        {
                            custCellVal.SetValue($"{reportItem.CustodianReport.Codextn.Code} {reportItem.CustodianReport.Department}");
                        }
                        if (custCellVal.IsMerged())
                        {
                            custCellVal.MergedRange().Unmerge();
                        }

                        row += 2;
                        ws.Row(10).CopyTo(ws.Row(++row));
                        ws.Row(11).CopyTo(ws.Row(++row));
                        ws.Row(12).CopyTo(ws.Row(++row));
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
                        ws.Range($"L{row - 2}:L{row}").Merge();
                        ws.Range($"M{row - 2}:M{row}").Merge();
                        ws.Range($"N{row - 2}:N{row}").Merge();
                        ws.Range($"O{row - 2}:O{row}").Merge();
                        ws.Range($"P{row - 2}:P{row}").Merge();
                        ws.Range($"Q{row - 2}:Q{row}").Merge();
                        ws.Range($"R{row - 2}:R{row}").Merge();
                        ws.Range($"S{row - 2}:S{row}").Merge();
                        ws.Range($"T{row - 1}:T{row}").Merge();
                        ws.Range($"U{row - 1}:U{row}").Merge();
                        ws.Range($"V{row - 1}:V{row}").Merge();
                        ws.Range($"W{row - 1}:W{row}").Merge();
                        ws.Range($"X{row - 1}:X{row}").Merge();
                        ws.Range($"AC{row - 2}:AC{row}").Merge();
                        ws.Range($"AD{row - 2}:AD{row}").Merge();
                        ws.Range($"AE{row - 2}:AE{row}").Merge();
                        ws.Range($"AF{row - 2}:AF{row}").Merge();
                    }

                    row++;

                    //if (string.IsNullOrWhiteSpace(annex))
                    //{
                    //    SetRowColValue(ws, reportItem, row, false);
                    //    ws.Range($"B{row}:AF{row}").Style.Border.BottomBorder = XLBorderStyleValues.Dotted;
                    //}
                    //else
                    //{
                    //    SetRowColValue(ws, reportItem, row, true);
                    //    ws.Range($"B{row}:AE{row}").Style.Border.BottomBorder = XLBorderStyleValues.Dotted;
                    //}

                    SetRowColValue(ws, reportItem, row, annex);
                    ws.Range($"B{row}:AE{row}").Style.Border.BottomBorder = XLBorderStyleValues.Dotted;

                    if (reportItem.CustodianReportBldgItemPhases.Any())
                    {
                        var otherEngAmounts = reportItem.CustodianReportBldgItemPhases
                        .OrderBy(o => o.InsertedDt)
                        //.Skip(1)
                        .ToList();
                        if (otherEngAmounts.Any())
                        {
                            foreach (var engAmt in otherEngAmounts)
                            {
                                row++;
                                ws.Row(row).Cell(10).SetValue(engAmt.BldgItem); // new
                                ws.Row(row).Cell(11).SetValue(engAmt.ProjectName);
                                ws.Row(row).Cell(14).SetValue(engAmt.StartDate.HasValue ? engAmt.StartDate.Value.Year.ToString() : "");
                                ws.Row(row).Cell(15).SetValue(engAmt.AcqDate.HasValue ? engAmt.AcqDate.Value.Year.ToString() : "");
                                ws.Row(row).Cell(18).SetValue(engAmt.OldAmount);
                                ws.Row(row).Cell(19).SetValue(engAmt.AcqCost);
                                ws.Row(row).Cell(20).SetValue(engAmt.PhaseNo);
                                ws.Row(row).Cell(21).SetValue(engAmt.CapitalOutlay);
                                ws.Row(row).Cell(23).SetValue(engAmt.MOOE);
                                ws.Row(row).Cell(24).SetValue(engAmt.StartDate).Style.DateFormat.Format = "MM/dd/yyyy";
                                ws.Row(row).Cell(25).SetValue(engAmt.TargetDate.HasValue ? $"{engAmt.TargetDate.Value.Month}/{engAmt.TargetDate.Value.Year}" : "");
                                ws.Row(row).Cell(26).SetValue(engAmt.PercentComplete);
                                ws.Row(row).Cell(27).SetValue(engAmt.CompletionDate).Style.DateFormat.Format = "MM/dd/yyyy";
                                ws.Row(row).Cell(28).SetValue(engAmt.Status);
                                ws.Row(row).Cell(29).SetValue(engAmt.Fund); // new
                                ws.Row(row).Cell(31).SetValue(engAmt.Remarks);
                                
                                if (string.IsNullOrWhiteSpace(annex))
                                {
                                    ws.Range($"B{row}:AF{row}").Style.Border.BottomBorder = XLBorderStyleValues.Dotted;
                                }
                                else
                                {
                                    ws.Row(row).Cell(32).SetValue(engAmt.Annex); // new
                                    ws.Range($"B{row}:AE{row}").Style.Border.BottomBorder = XLBorderStyleValues.Dotted;
                                }
                            }
                        }
                    }

                    tAcqCost += (reportItem.TotalAmount ?? 0);
                }
                //ws.Row(++row).Cell(14).SetValue("TOTAL");
                //ws.Row(row).Cell(15).SetValue(tAcqCost);

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

        //public MemoryStream ProcessExcelFileAnnex(int? forYear, Guid? deptId, string templateFilePath, int? accountGroup, string annex)
        public MemoryStream ProcessExcelFileAnnex(int? forYear, Guid? deptId, Guid? sectionId, string templateFilePath, int? accountGroup, string annex, string mainAccount, DateTime? asOf
            , string subAccount1, string subAccount2, string subAccount3, string subAccount4, string userName)
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

            //return ProcessExcelFileTemplate(forYear, deptId, accountGroup, templateFilePath, hdg, annex);
            return ProcessExcelFileTemplate(forYear, deptId, sectionId, accountGroup, templateFilePath, hdg, annex, mainAccount, asOf
                    , subAccount1, subAccount2, subAccount3, subAccount4, userName);
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

        private void ValidateIfSubmitted(CustodianReportBldgItem model, AppManEntities ctx)
        {
            var submitForCount = ctx.CustodianReportSubmitForCounts.FirstOrDefault(f => f.ReportId == model.ReportId && f.LocationId == model.LocationId && f.Status == "Submit");
            if (submitForCount != null)
            {
                var msg = $"Record already submitted for count by {submitForCount.UpdatedBy} on {submitForCount.UpdatedDt}, cannot update!";
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

        private void ValidateUser(CustodianReportBldgItem entity, CustodianReportBldgItem model)
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