using ClosedXML.Excel;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.AllFields;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.CustodianReports
{
    public interface ICustodianReportItemService
    {
        IQueryable<CustodianReportItem> GetByReportId(Guid? reportId);
        IQueryable<CustodianReportItem> GetAvailableItemsForDisposal(int? forYear, Guid? deptId);
        ValueTask<CustodianReportItem> GetByIdAsync(Guid id);
        string GetStockNo(CustodianReportItem model);
        ValueTask<CustodianReportItem> CreateAsync(CustodianReportItem model, string user, DateTime date);
        ValueTask<CustodianReportItem> UpdateAsync(CustodianReportItem model, string user, DateTime date);
        ValueTask<CustodianReportItem> DeleteAsync(CustodianReportItem model, string user, DateTime date);
        ValueTask<CustodianReportItem> PostAsync(Guid id, string user, DateTime date);
        ValueTask<CustodianReportItem> UnPostAsync(Guid id, string user, DateTime date);

        Task<int> GetSetLotNoCountAsync(int? forYear, Guid? deptId, Guid? locationId, Guid? custodianReportItemId, string setLotNo);
        Task<decimal?> GetSetLotNoAmountAsync(int? forYear, Guid? deptId, Guid? locationId, Guid? custodianReportItemId, string setLotNo);

        Task UpdateAllSetLotRemarksAsync(int forYear);

        MemoryStream ProcessExcelFile(int? forYear, Guid? deptId, Guid? sectionId, string templateFilePath, int? accountGroup, string userName);
        MemoryStream ProcessExcelFile(int? forYear, Guid? deptId, Guid? sectionId, string templateFilePath, int? accountGroup, string mainAccount, DateTime? asOf
            , string subAccount1, string subAccount2, string subAccount3, string subAccount4, string userName);
        MemoryStream ProcessExcelFileAnnex(int? forYear, Guid? deptId, Guid? sectionId, string templateFilePath, int? accountGroup, string annex, string mainAccount, DateTime? asOf
            , string subAccount1, string subAccount2, string subAccount3, string subAccount4, string userName);
    }

    public class CustodianReportItemService : BaseValidator, ICustodianReportItemService
    {
        protected readonly AppManEntities _db;
        protected readonly IAppManEntitiesFactory _contextFactory;
        protected readonly IAllFieldService _allFieldService;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<CustodianReportItem> _exceptionService;
        protected readonly IUserService _userService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        public CustodianReportItemService(AppManEntities db,
            IAppManEntitiesFactory appManEntitiesFactory,
            IAllFieldService allFieldService,
            ICreateAndLogExceptions exceptions,
            IExceptionService<CustodianReportItem> exceptionService,
            IUserService userService)
        {
            _db = db;
            _contextFactory = appManEntitiesFactory;
            _allFieldService = allFieldService;
            _exceptions = exceptions;
            _exceptionService = exceptionService;
            _userService = userService;
            _getDisplayName = Utility.GetDisplayName<CustodianReportItemPpeVM>;
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

        public IQueryable<CustodianReportItem> GetAvailableItemsForDisposal(int? forYear, Guid? deptId) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.CustodianReportItems.AsNoTracking()
                .Where(w => w.CustodianReport.AsOf.Value.Year == forYear && w.DeptId == deptId
                    && (w.Annex == "A" || w.Annex == "B")
                    && !w.CustodianDisposalItems.Any())
                .AsQueryable();

            return data;
        });

        public virtual async ValueTask<CustodianReportItem> CreateAsync(CustodianReportItem model, string user, DateTime date)
        {
            if (model.ForYear == 0 || model.ForYear == null)
            {
                throw new InvalidValueException("For Year is Required.");
            }
            var asOfDate = Utility.GetAsOfDate((int)model.ForYear);
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
                    custodianReport.AsOf = asOfDate;
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
                ValidateIfSubmitted(model);
                if (!string.IsNullOrWhiteSpace(model.SetLotNo))
                {
                    model.SetLotNo = model.SetLotNo.Trim();
                }

                ValidateFieldsOnCreateUpdate(model, Mode.ADD);
                ValidateSerials(model, Mode.EDIT);

                var entity = new CustodianReportItem();
                MapModelToEntityFields(entity, model, Mode.ADD);

                ctx.CustodianReportItems.Add(entity);

                await ctx.SaveChangesAsync();
                await UpdateSetLotRemarksRawAsync(asOfDate, model.MainDeptId, model.LocationId, model.SetLotNo);

                return model;
            }
        }

        public virtual async ValueTask<CustodianReportItem> UpdateAsync(CustodianReportItem model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = await ctx.CustodianReportItems.Include(i => i.CustodianReport).FirstOrDefaultAsync(f => f.Id == model.Id);
                var prevSetLotNo = entity.SetLotNo;
                var prevLocationId = entity.LocationId;

                ValidateRecord(entity, model.Id);
                ValidateIfPosted(entity);
                ValidateIfSubmitted(model);
                //ValidateUser(entity, model);

                if (!string.IsNullOrWhiteSpace(model.SetLotNo))
                {
                    model.SetLotNo = model.SetLotNo.Trim();
                }

                ValidateFieldsOnCreateUpdate(model, Mode.EDIT);
                ValidateSerials(model, Mode.EDIT);

                MapModelToEntityFields(entity, model, Mode.EDIT);

                //ctx.CustodianReportItems.Attach(entity);
                //ctx.Entry(entity).State = EntityState.Modified;
                await ctx.SaveChangesAsync();

                var asOfDate = Utility.GetAsOfDate((int)model.ForYear);
                if (prevSetLotNo != model.SetLotNo)
                {
                    await UpdateSetLotRemarksRawAsync(asOfDate, model.MainDeptId, prevLocationId, prevSetLotNo);
                }
                await UpdateSetLotRemarksRawAsync(asOfDate, model.MainDeptId, model.LocationId, model.SetLotNo);

                return model;
            }
        }

        public async Task UpdateAllSetLotRemarksAsync(int forYear)
        {
            var asOfDate = Utility.GetAsOfDate(forYear);                        
            var custudianReportItemGroups = await _db.CustodianReportItems
               .Where(w => w.CustodianReport.AsOf == asOfDate && (w.SetLotNo.StartsWith("S") || w.SetLotNo.StartsWith("L")))
               .GroupBy(g => new { g.CustodianReport.DeptId, g.LocationId, g.SetLotNo })
               .Select(g => new { g.Key.DeptId, g.Key.LocationId, g.Key.SetLotNo })
               .ToListAsync();

            foreach (var custodianReportItemGroup in custudianReportItemGroups)
            {
                await UpdateSetLotRemarksRawAsync(asOfDate, 
                    custodianReportItemGroup.DeptId, 
                    custodianReportItemGroup.LocationId,
                    custodianReportItemGroup.SetLotNo);
            }

        }

        private async Task UpdateSetLotRemarksRawAsync(DateTime asOfDate, Guid? deptId, Guid? locationId, string setLotNo)
        {
            if (string.IsNullOrWhiteSpace(setLotNo))
            {
                return;
            }

            var items = await _db.CustodianReportItems
                .Where(w => w.CustodianReport.AsOf == asOfDate
                    && w.CustodianReport.DeptId == deptId
                    && w.LocationId == locationId
                    && w.SetLotNo == setLotNo)
                .OrderBy(o => o.Annex)
                .ThenBy(t => t.ItemCode.ItemType.GroupCode)
                .ThenBy(t => t.ItemCode.ItemType.Code)
                .ThenBy(t => t.ItemCode.ItemNoIndex)
                .Select(s => new { s.Id })
                .ToListAsync();

            if (items.Any())
            {
                var total = items.Count;
                var sb = new StringBuilder();

                int index = 1;
                foreach (var item in items)
                {
                    var remarks = $"{index++} of {total}";
                    sb.AppendLine($"UPDATE CustodianReportItems SET SetLotRemarks = '{remarks}' WHERE Id = '{item.Id}';");
                }

                var sql = sb.ToString();
                await _db.Database.ExecuteSqlCommandAsync(TransactionalBehavior.EnsureTransaction, sql);
            }
        }
        
        private void ValidateSerials(CustodianReportItem model, Mode mode)
        {
            _imex = new InvalidModelException();
            var accountGroup = model.AccountGroup;
            if ((int?)CustodianAccountGroup.PPE == accountGroup || (int?)CustodianAccountGroup.STOCK == accountGroup)
            {
                if (!string.IsNullOrWhiteSpace(model.SerialNo))
                {
                    if (mode == Mode.ADD)
                    {
                        var psCardItemExtn = _db.PsCardItemExtns.OfType<PsCardItemExtnOther>()
                            .AsNoTracking()
                            .FirstOrDefault(f => f.PsCardItem.PsCard.PsNo == model.PsNo && f.SerialNo == model.SerialNo);
                        if (psCardItemExtn != null)
                        {
                            _imex.UpsertDataList(_getDisplayName(nameof(model.SerialNo)), "Already Exists.");
                        }
                    }
                    else
                    {
                        var custodianReportUploads = _db.CustodianReportUploads.FirstOrDefault(f => f.Id == model.Id);
                        if (custodianReportUploads != null)
                        {
                            var psCardItemExtn = _db.PsCardItemExtns.OfType<PsCardItemExtnOther>()
                                .AsNoTracking()
                                .FirstOrDefault(f => f.PsCardItem.PsCard.PsNo == model.PsNo && f.SerialNo == model.SerialNo
                                && f.Id != custodianReportUploads.PsCardItemExtnId);
                            if (psCardItemExtn != null)
                            {
                                _imex.UpsertDataList(_getDisplayName(nameof(model.SerialNo)), "Already Exists.");
                            }
                        }
                    }
                }
            }
            else
            {
                var psCardItemExtns = _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>()
                    .AsNoTracking()
                    .Include(i => i.PsCardItem.PsCard)
                    .Where(w => w.PsCardItem.PsCard.PsNo == model.PsNo).ToList();
                if (psCardItemExtns.Any())
                {
                    var custodianReportUploads = _db.CustodianReportUploads.FirstOrDefault(f => f.Id == model.Id);
                    if (mode == Mode.ADD || custodianReportUploads == null)
                    {
                        if (!string.IsNullOrWhiteSpace(model.ConductionNo) && psCardItemExtns.Any(a => a.ConductionNo == model.ConductionNo))
                        {
                            _imex.UpsertDataList(_getDisplayName(nameof(model.ConductionNo)), "Already Exists.");                        
                        }
                        if (!string.IsNullOrWhiteSpace(model.PlateNo) && psCardItemExtns.Any(a => a.PlateNo == model.PlateNo))
                        {
                            _imex.UpsertDataList(_getDisplayName(nameof(model.PlateNo)), "Already Exists.");
                        }
                        if (!string.IsNullOrWhiteSpace(model.BodyNo) && psCardItemExtns.Any(a => a.BodyNo == model.BodyNo))
                        {
                            _imex.UpsertDataList(_getDisplayName(nameof(model.BodyNo)), "Already Exists.");
                        }
                        if (!string.IsNullOrWhiteSpace(model.EngineNo) && psCardItemExtns.Any(a => a.EngineNo == model.EngineNo))
                        {
                            _imex.UpsertDataList(_getDisplayName(nameof(model.EngineNo)), "Already Exists.");
                        }
                    }
                    else
                    {                        
                        if (!string.IsNullOrWhiteSpace(model.ConductionNo) 
                            && psCardItemExtns.Any(a => a.ConductionNo == model.ConductionNo && a.Id != custodianReportUploads.PsCardItemExtnId))
                        {
                            _imex.UpsertDataList(_getDisplayName(nameof(model.ConductionNo)), "Already Exists.");
                        }
                        if (!string.IsNullOrWhiteSpace(model.PlateNo) 
                            && psCardItemExtns.Any(a => a.PlateNo == model.PlateNo && a.Id != custodianReportUploads.PsCardItemExtnId))
                        {
                            _imex.UpsertDataList(_getDisplayName(nameof(model.PlateNo)), "Already Exists.");
                        }
                        if (!string.IsNullOrWhiteSpace(model.BodyNo) 
                            && psCardItemExtns.Any(a => a.BodyNo == model.BodyNo && a.Id != custodianReportUploads.PsCardItemExtnId))
                        {
                            _imex.UpsertDataList(_getDisplayName(nameof(model.BodyNo)), "Already Exists.");
                        }
                        if (!string.IsNullOrWhiteSpace(model.EngineNo) 
                            && psCardItemExtns.Any(a => a.EngineNo == model.EngineNo && a.Id != custodianReportUploads.PsCardItemExtnId))
                        {
                            _imex.UpsertDataList(_getDisplayName(nameof(model.EngineNo)), "Already Exists.");
                        }
                    }
                }
            }

            _imex.ThrowIfContainsErrors();
        }

        public void ValidateFieldsOnCreateUpdate(CustodianReportItem model, Mode mode)
        {
            _imex = new InvalidModelException();

            if (!string.IsNullOrWhiteSpace(model.SetLotNo) && model.SetLotNo.Contains(" "))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.SetLotNo)), "Space is not allowed in Set/Lot No.");
            }

            if (!string.IsNullOrWhiteSpace(model.SetLotNo) && model.SetLotNo.Length == 1)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.SetLotNo)), "Invalid Value.");
            }

            if (!string.IsNullOrWhiteSpace(model.SetLotNo) && (!model.SetLotAmount.HasValue || model.SetLotAmount == 0))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.SetLotAmount)), "Set/Lot Amount is Required if with Set/Lot No.");
            }

            if ((model.SetLotAmount.HasValue && model.SetLotAmount > 0) && string.IsNullOrWhiteSpace(model.SetLotNo))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.SetLotNo)), "Set/Lot No. is Required if with Set/Lot Amount");
            }

            // validate SetLotNo and SetLotAmount, SetLotNo must only have 1 SetLotAmount
            //if (mode == Mode.ADD)
            //{
            //    //if ((model.SetLotAmount.HasValue && model.SetLotAmount > 0) && !string.IsNullOrWhiteSpace(model.SetLotNo))
            //    if (!string.IsNullOrWhiteSpace(model.SetLotNo))
            //    {
            //        if (ctx.CustodianReportItems.Any(a => a.ReportId == model.ReportId 
            //            && a.LocationCode == model.LocationCode
            //            && a.SetLotNo == model.SetLotNo && a.SetLotAmount != model.SetLotAmount))
            //        {
            //            _imex.UpsertDataList(_getDisplayName(nameof(model.SetLotNo)), "Set/Lot Amount with different value already exist for the same Set/Lot No.");
            //        }
            //    }
            //}

            if (!string.IsNullOrWhiteSpace(model.SetLotNo))
            {
                if (mode == Mode.ADD)
                {
                    var item = _db.CustodianReportItems.Where(a => a.ReportId == model.ReportId
                        && a.LocationCode == model.LocationCode
                        && a.SetLotNo == model.SetLotNo && a.SetLotAmount != model.SetLotAmount && a.SetLotAmount > 0);
                    if (item.Any())
                    {
                        var amount = item.FirstOrDefault().SetLotAmount;
                        _imex.UpsertDataList(_getDisplayName(nameof(model.SetLotNo)), $"Set/Lot Amount with a value of {amount.Value:N2} already exist for the same Set/Lot No.");
                    }
                }
                else
                {
                    var item = _db.CustodianReportItems.Where(a => a.Id != model.Id
                        && a.ReportId == model.ReportId
                        && a.LocationCode == model.LocationCode
                        && a.SetLotNo == model.SetLotNo && a.SetLotAmount != model.SetLotAmount && a.SetLotAmount > 0);
                    if (item.Any())
                    {
                        var amount = item.FirstOrDefault().SetLotAmount;
                        _imex.UpsertDataList(_getDisplayName(nameof(model.SetLotNo)), $"Set/Lot Amount with a value of {amount.Value:N2} already exist for the same Set/Lot No.");
                    }
                }
            
                if (!model.SetLotNo.ToUpper().StartsWith("S") && !model.SetLotNo.ToUpper().StartsWith("L"))
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.SetLotNo)), "Set/Lot No. must start with S or L.");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.Annex) && model.Annex == "A")
            {
                if ((model.SetLotAmount == 0 && model.TotalCost == 0))
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.TotalCost)), "Please input the acquisition cost from existing records");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.Annex) && model.Annex == "C")
            {
                if (string.IsNullOrWhiteSpace(model.Remarks))
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.Remarks)), "Field is Required for Annex C.");
                }
            }

            _imex.ThrowIfContainsErrors();
        }

        public virtual async ValueTask<CustodianReportItem> DeleteAsync(CustodianReportItem model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = await ctx.CustodianReportItems.FirstOrDefaultAsync(f => f.Id == model.Id);
                ValidateRecord(entity, model.Id);
                ValidateIfPosted(entity);
                ValidateIfSubmitted(model);

                // manually remove, cascade is not working due to multiple relationship.
                var custodianReportUpload = await ctx.CustodianReportUploads.FindAsync(entity.Id);
                if (custodianReportUpload != null)
                {
                    ctx.CustodianReportUploads.Remove(entity.CustodianReportUpload);
                    //ctx.Entry(entity.CustodianReportUpload).State = EntityState.Deleted;
                    await ctx.SaveChangesAsync();
                }

                //entity.UpdatedBy = model.UpdatedBy;
                //entity.UpdatedDt = model.UpdatedDt;

                //ctx.CustodianReportItems.Attach(entity);
                //ctx.Entry(entity).State = EntityState.Modified;
                //await ctx.SaveChangesAsync();

                var reportId = entity.ReportId;
                var locationId = entity.LocationId;
                var setLotNo = entity.SetLotNo;

                ctx.CustodianReportItems.Remove(entity);
                //ctx.Entry(entity).State = EntityState.Deleted;

                await ctx.SaveChangesAsync();

                var custodianReport = await ctx.CustodianReports.Where(w => w.Id == reportId).FirstOrDefaultAsync();
                if (custodianReport != null)
                {
                    var asOfDate = (DateTime)custodianReport.AsOf;
                    var deptId = custodianReport.DeptId;
                    await UpdateSetLotRemarksRawAsync(asOfDate, deptId, locationId, setLotNo);
                }

                return model;
            }
        }

        public virtual ValueTask<CustodianReportItem> PostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = await ctx.CustodianReportItems.FindAsync(id);
                ValidateRecord(entity, id);
                ValidateIfPosted(entity);

                entity.PostedBy = user;
                entity.PostedDt = date;
                entity.UpdatedBy = user;
                entity.UpdatedDt = date;

                //ctx.CustodianReportItems.Attach(entity);
                //ctx.Entry(entity).State = EntityState.Modified;
                await ctx.SaveChangesAsync();
                return entity;
            }
        });

        public virtual ValueTask<CustodianReportItem> UnPostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = await ctx.CustodianReportItems.FindAsync(id);
                ValidateRecord(entity, id);
                ValidateIfNotPosted(entity);

                entity.PostedBy = "";
                entity.PostedDt = null;
                entity.UpdatedBy = user;
                entity.UpdatedDt = date;

                //ctx.CustodianReportItems.Attach(entity);
                //ctx.Entry(entity).State = EntityState.Modified;
                await ctx.SaveChangesAsync();
                return entity;
            }
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
            // model.SerialNo = model.AllField.SerialNo;
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
            entity.SetLotNo = string.IsNullOrWhiteSpace(model.SetLotNo) ? model.SetLotNo : model.SetLotNo.ToUpper();
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
            entity.RpcPpeNo = model.RpcPpeNo;
            entity.ParIssuedTo = model.ParIssuedTo;
            entity.IcsIssuedTo = model.IcsIssuedTo;
            entity.AreIssuedTo = model.AreIssuedTo;
            entity.MrIssuedTo = model.MrIssuedTo;
            entity.RpcPpeIssuedTo = model.RpcPpeIssuedTo;
            entity.AccountableOfficer = model.AccountableOfficer;
            entity.IcsOfficer = model.IcsOfficer;
            entity.AreOfficer = model.AreOfficer;
            entity.MrOfficer = model.MrOfficer;
            entity.RpcPpeOfficer = model.RpcPpeOfficer;
            entity.UpcomingPar = model.UpcomingPar;
            entity.UpcomingIcs = model.UpcomingIcs;
            entity.Type = model.Type;
            entity.Annex = model.Annex?.ToUpper();
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
            if (!string.IsNullOrWhiteSpace(model.SetLotNo) && (model.SetLotNo.ToUpper().StartsWith("S") || model.SetLotNo.ToUpper().StartsWith("L")))
            {
                entity.SetLotAmount = model.SetLotAmount;
                entity.TotalCost = 0;
            }
            else
            {
                entity.TotalCost = model.TotalCost;
                entity.SetLotAmount = 0;
            }
            entity.SetLotRemarks = model.SetLotRemarks;
            entity.PriceRate = model.PriceRate;
            entity.ProRatedCost = model.ProRatedCost;
            entity.AddCost = model.AddCost;
            entity.TUnitCost = model.TUnitCost;
            entity.GTotalCost = model.GTotalCost;

            //entity.PostedBy = model.PostedBy;
            //entity.PostedDt = model.PostedDt;
        }

        public async Task<int> GetSetLotNoCountAsync(int? forYear, Guid? deptId, Guid? locationId, Guid? custodianReportItemId, string setLotNo)
        {
            if (string.IsNullOrWhiteSpace(setLotNo))
            {
                return 0;
            }

            return await _db.CustodianReportItems.Where(w => w.CustodianReport.DeptId == deptId
                && w.CustodianReport.AsOf.Value.Year == forYear
                && w.LocationId == locationId && w.SetLotNo == setLotNo && w.Id != custodianReportItemId).CountAsync();
        }

        public async Task<decimal?> GetSetLotNoAmountAsync(int? forYear, Guid? deptId, Guid? locationId, Guid? custodianReportItemId, string setLotNo)
        {
            if (string.IsNullOrWhiteSpace(setLotNo))
            {
                return 0;
            }

            return (await _db.CustodianReportItems.Where(w => w.CustodianReport.DeptId == deptId
                && w.CustodianReport.AsOf.Value.Year == forYear
                && w.LocationId == locationId && w.SetLotNo == setLotNo && w.SetLotAmount > 0 
                && w.Id != custodianReportItemId).Select(s => s.SetLotAmount).FirstOrDefaultAsync());
        }

        public MemoryStream ProcessExcelFile(int? forYear, Guid? deptId, Guid? sectionId, string templateFilePath, int? accountGroup, string userName)
        {
            return ProcessExcelFile(forYear, deptId, sectionId, templateFilePath, accountGroup, "", null, "", "", "", "", userName);
        }

        public MemoryStream ProcessExcelFile(int? forYear, Guid? deptId, Guid? sectionId, string templateFilePath, int? accountGroup, string mainAccount, DateTime? asOf
            , string subAccount1, string subAccount2, string subAccount3, string subAccount4, string userName)
        {
            // Load the template file
            FileInfo templateFile = new FileInfo(templateFilePath);
            if (!templateFile.Exists)
            {
                throw new FileNotFoundException("The template file does not exist.", templateFilePath);
            }
            if (accountGroup == (int?)CustodianAccountGroup.STOCK)
            {
                return ProcessExcelFileStockTemplate(forYear, deptId, sectionId, accountGroup, templateFilePath, mainAccount, asOf
                    , subAccount1, subAccount2, subAccount3, subAccount4, userName);
            }
            else if (accountGroup == (int?)CustodianAccountGroup.PPE)
            {
                return ProcessExcelFilePpeTemplate(forYear, deptId, sectionId, accountGroup, templateFilePath, mainAccount, asOf
                    , subAccount1, subAccount2, subAccount3, subAccount4, userName);
            }
            else
            {
                return ProcessExcelFileVehicleTemplate(forYear, deptId, sectionId, accountGroup, templateFilePath, mainAccount, asOf
                    , subAccount1, subAccount2, subAccount3, subAccount4, userName);
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
            ws.Row(row).Cell(++col).SetValue(reportItem.DeptCode);
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
            ws.Row(row).Cell(++col).SetValue(reportItem.ItemSerialNo);
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

            //var pars = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            //var ics = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "ICS").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            //var ares = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            //var mrs = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.RefNo));

            //ws.Row(row).Cell(++col).SetValue($"{(reportItem.ParNo ?? "") + (string.IsNullOrWhiteSpace(pars) ? "" : "\r\n" + pars)} " +
            //    $"/ {(reportItem.IcsNo ?? "") + (string.IsNullOrWhiteSpace(ics) ? "" : "\r\n" + ics)} " +
            //    $"/ {(reportItem.AreNo ?? "") + (string.IsNullOrWhiteSpace(ares) ? "" : "\r\n" + ares)} " +
            //    $"/ {(reportItem.MrNo ?? "") + (string.IsNullOrWhiteSpace(mrs) ? "" : "\r\n" + mrs)}");

            //var parOfficers = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            //var icsOfficers = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "ICS").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            //var areOfficers = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            //var mrOfficers = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));


            //ws.Row(row).Cell(++col).SetValue($"{(reportItem.AccountableOfficer ?? "") + (string.IsNullOrWhiteSpace(parOfficers) ? "" : "\r\n" + parOfficers)} " +
            //    $"/ {(reportItem.IcsOfficer ?? "") + (string.IsNullOrWhiteSpace(icsOfficers) ? "" : "\r\n" + icsOfficers)} " +
            //    $"/ {(reportItem.AreOfficer ?? "") + (string.IsNullOrWhiteSpace(areOfficers) ? "" : "\r\n" + areOfficers)} " +
            //    $"/ {(reportItem.MrOfficer ?? "") + (string.IsNullOrWhiteSpace(mrOfficers) ? "" : "\r\n" + mrOfficers)}");

            //var parIssuedTo = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            //var icsIssuedTo = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "ICS").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            //var areIssuedTo = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            //var mrIssuedTo = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            //ws.Row(row).Cell(++col).SetValue($"{(reportItem.ParIssuedTo ?? "") + (string.IsNullOrWhiteSpace(parIssuedTo) ? "" : "\r\n" + parIssuedTo)} " +
            //    $"/ {(reportItem.IcsIssuedTo ?? "") + (string.IsNullOrWhiteSpace(icsIssuedTo) ? "" : "\r\n" + icsIssuedTo)} " +
            //    $"/ {(reportItem.AreIssuedTo ?? "") + (string.IsNullOrWhiteSpace(areIssuedTo) ? "" : "\r\n" + areIssuedTo)} " +
            //    $"/ {(reportItem.MrIssuedTo ?? "") + (string.IsNullOrWhiteSpace(mrIssuedTo) ? "" : "\r\n" + mrIssuedTo)}");

            //var pars = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            //var ics = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "ICS").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            //var ares = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            //var mrs = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.RefNo));

            ws.Row(row).Cell(++col).SetValue($"{(reportItem.ParNo ?? "") + (string.IsNullOrWhiteSpace(reportItem.Par) ? "" : "\r\n" + reportItem.Par)} " +
                $"/ {(reportItem.IcsNo ?? "") + (string.IsNullOrWhiteSpace(reportItem.Ics) ? "" : "\r\n" + reportItem.Ics)} " +
                $"/ {(reportItem.AreNo ?? "") + (string.IsNullOrWhiteSpace(reportItem.Are) ? "" : "\r\n" + reportItem.Are)} " +
                $"/ {(reportItem.MrNo ?? "") + (string.IsNullOrWhiteSpace(reportItem.Mr) ? "" : "\r\n" + reportItem.Mr)}" +
                $"/ {(reportItem.RpcPpeNo ?? "") + (string.IsNullOrWhiteSpace(reportItem.RpcPpe) ? "" : "\r\n" + reportItem.RpcPpe)}");

            ws.Row(row).Cell(++col).SetValue($"{(reportItem.AccountableOfficer ?? "") + (string.IsNullOrWhiteSpace(reportItem.ParOfficers) ? "" : "\r\n" + reportItem.ParOfficers)} " +
                $"/ {(reportItem.IcsOfficer ?? "") + (string.IsNullOrWhiteSpace(reportItem.IcsOfficer) ? "" : "\r\n" + reportItem.IcsOfficers)} " +
                $"/ {(reportItem.AreOfficer ?? "") + (string.IsNullOrWhiteSpace(reportItem.AreOfficers) ? "" : "\r\n" + reportItem.AreOfficers)} " +
                $"/ {(reportItem.MrOfficer ?? "") + (string.IsNullOrWhiteSpace(reportItem.MrOfficers) ? "" : "\r\n" + reportItem.MrOfficers)}" +
                $"/ {(reportItem.RpcPpeOfficer ?? "") + (string.IsNullOrWhiteSpace(reportItem.RpcPpeOfficers) ? "" : "\r\n" + reportItem.RpcPpeOfficers)}");

            ws.Row(row).Cell(++col).SetValue($"{(reportItem.ParIssuedTo ?? "") + (string.IsNullOrWhiteSpace(reportItem.ParIssuedsTo) ? "" : "\r\n" + reportItem.ParIssuedsTo)} " +
                $"/ {(reportItem.IcsIssuedTo ?? "") + (string.IsNullOrWhiteSpace(reportItem.IcsIssuedsTo) ? "" : "\r\n" + reportItem.IcsIssuedsTo)} " +
                $"/ {(reportItem.AreIssuedTo ?? "") + (string.IsNullOrWhiteSpace(reportItem.AreIssuedsTo) ? "" : "\r\n" + reportItem.AreIssuedsTo)} " +
                $"/ {(reportItem.MrIssuedTo ?? "") + (string.IsNullOrWhiteSpace(reportItem.MrIssuedsTo) ? "" : "\r\n" + reportItem.MrIssuedsTo)}" +
                $"/ {(reportItem.RpcPpeIssuedTo ?? "") + (string.IsNullOrWhiteSpace(reportItem.RpcPpeIssuedsTo) ? "" : "\r\n" + reportItem.RpcPpeIssuedTo)}");

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
            ws.Row(row).Cell(++col).SetValue(reportItem.DeptCode);
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
            ws.Row(row).Cell(++col).SetValue(reportItem.ItemSerialNo);
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

            //var pars = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            //var ics = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "ICS").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            //var ares = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            //var mrs = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.RefNo));

            ws.Row(row).Cell(++col).SetValue($"{(reportItem.ParNo ?? "") + (string.IsNullOrWhiteSpace(reportItem.Par) ? "" : "\r\n" + reportItem.Par)} " +
                $"/ {(reportItem.IcsNo ?? "") + (string.IsNullOrWhiteSpace(reportItem.Ics) ? "" : "\r\n" + reportItem.Ics)} " +
                $"/ {(reportItem.AreNo ?? "") + (string.IsNullOrWhiteSpace(reportItem.Are) ? "" : "\r\n" + reportItem.Are)} " +
                $"/ {(reportItem.MrNo ?? "") + (string.IsNullOrWhiteSpace(reportItem.Mr) ? "" : "\r\n" + reportItem.Mr)}" +
                $"/ {(reportItem.RpcPpeNo ?? "") + (string.IsNullOrWhiteSpace(reportItem.RpcPpe) ? "" : "\r\n" + reportItem.RpcPpe)}");

            //var parOfficers = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            //var icsOfficers = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "ICS").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            //var areOfficers = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            //var mrOfficers = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));

            ws.Row(row).Cell(++col).SetValue($"{(reportItem.AccountableOfficer ?? "") + (string.IsNullOrWhiteSpace(reportItem.ParOfficers) ? "" : "\r\n" + reportItem.ParOfficers)} " +
                $"/ {(reportItem.IcsOfficer ?? "") + (string.IsNullOrWhiteSpace(reportItem.IcsOfficer) ? "" : "\r\n" + reportItem.IcsOfficers)} " +
                $"/ {(reportItem.AreOfficer ?? "") + (string.IsNullOrWhiteSpace(reportItem.AreOfficers) ? "" : "\r\n" + reportItem.AreOfficers)} " +
                $"/ {(reportItem.MrOfficer ?? "") + (string.IsNullOrWhiteSpace(reportItem.MrOfficers) ? "" : "\r\n" + reportItem.MrOfficers)}" +
                $"/ {(reportItem.RpcPpeOfficer ?? "") + (string.IsNullOrWhiteSpace(reportItem.RpcPpeOfficers) ? "" : "\r\n" + reportItem.RpcPpeOfficers)}");

            //var parIssuedTo = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            //var icsIssuedTo = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "ICS").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            //var areIssuedTo = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            //var mrIssuedTo = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));

            ws.Row(row).Cell(++col).SetValue($"{(reportItem.ParIssuedTo ?? "") + (string.IsNullOrWhiteSpace(reportItem.ParIssuedsTo) ? "" : "\r\n" + reportItem.ParIssuedsTo)} " +
                $"/ {(reportItem.IcsIssuedTo ?? "") + (string.IsNullOrWhiteSpace(reportItem.IcsIssuedsTo) ? "" : "\r\n" + reportItem.IcsIssuedsTo)} " +
                $"/ {(reportItem.AreIssuedTo ?? "") + (string.IsNullOrWhiteSpace(reportItem.AreIssuedsTo) ? "" : "\r\n" + reportItem.AreIssuedsTo)} " +
                $"/ {(reportItem.MrIssuedTo ?? "") + (string.IsNullOrWhiteSpace(reportItem.MrIssuedsTo) ? "" : "\r\n" + reportItem.MrIssuedsTo)}" +
                $"/ {(reportItem.RpcPpeIssuedTo ?? "") + (string.IsNullOrWhiteSpace(reportItem.RpcPpeIssuedsTo) ? "" : "\r\n" + reportItem.RpcPpeIssuedTo)}");

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
            ws.Row(row).Cell(++col).SetValue(reportItem.DeptCode);
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

            //var pars = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            //var ics = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "ICS").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            //var ares = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            //var mrs = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.RefNo));

            //ws.Row(row).Cell(++col).SetValue($"{(reportItem.ParNo ?? "") + (string.IsNullOrWhiteSpace(pars) ? "" : "\r\n" + pars)} " +
            //    $"/ {(reportItem.IcsNo ?? "") + (string.IsNullOrWhiteSpace(ics) ? "" : "\r\n" + ics)} " +
            //    $"/ {(reportItem.AreNo ?? "") + (string.IsNullOrWhiteSpace(ares) ? "" : "\r\n" + ares)} " +
            //    $"/ {(reportItem.MrNo ?? "") + (string.IsNullOrWhiteSpace(mrs) ? "" : "\r\n" + mrs)}");

            //var parOfficers = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            //var icsOfficers = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "ICS").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            //var areOfficers = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            //var mrOfficers = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));


            //ws.Row(row).Cell(++col).SetValue($"{(reportItem.AccountableOfficer ?? "") + (string.IsNullOrWhiteSpace(parOfficers) ? "" : "\r\n" + parOfficers)} " +
            //    $"/ {(reportItem.IcsOfficer ?? "") + (string.IsNullOrWhiteSpace(icsOfficers) ? "" : "\r\n" + icsOfficers)} " +
            //    $"/ {(reportItem.AreOfficer ?? "") + (string.IsNullOrWhiteSpace(areOfficers) ? "" : "\r\n" + areOfficers)} " +
            //    $"/ {(reportItem.MrOfficer ?? "") + (string.IsNullOrWhiteSpace(mrOfficers) ? "" : "\r\n" + mrOfficers)}");

            //var parIssuedTo = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            //var icsIssuedTo = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "ICS").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            //var areIssuedTo = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            //var mrIssuedTo = string.Join("\r\n", ctx.CustodianReportItemIssuances.Where(w => w.ReportItemId == reportItem.ReportId && w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            //ws.Row(row).Cell(++col).SetValue($"{(reportItem.ParIssuedTo ?? "") + (string.IsNullOrWhiteSpace(parIssuedTo) ? "" : "\r\n" + parIssuedTo)} " +
            //    $"/ {(reportItem.IcsIssuedTo ?? "") + (string.IsNullOrWhiteSpace(icsIssuedTo) ? "" : "\r\n" + icsIssuedTo)} " +
            //    $"/ {(reportItem.AreIssuedTo ?? "") + (string.IsNullOrWhiteSpace(areIssuedTo) ? "" : "\r\n" + areIssuedTo)} " +
            //    $"/ {(reportItem.MrIssuedTo ?? "") + (string.IsNullOrWhiteSpace(mrIssuedTo) ? "" : "\r\n" + mrIssuedTo)}");


            ws.Row(row).Cell(++col).SetValue($"{(reportItem.ParNo ?? "") + (string.IsNullOrWhiteSpace(reportItem.Par) ? "" : "\r\n" + reportItem.Par)} " +
                $"/ {(reportItem.IcsNo ?? "") + (string.IsNullOrWhiteSpace(reportItem.Ics) ? "" : "\r\n" + reportItem.Ics)} " +
                $"/ {(reportItem.AreNo ?? "") + (string.IsNullOrWhiteSpace(reportItem.Are) ? "" : "\r\n" + reportItem.Are)} " +
                $"/ {(reportItem.MrNo ?? "") + (string.IsNullOrWhiteSpace(reportItem.Mr) ? "" : "\r\n" + reportItem.Mr)}" +
                $"/ {(reportItem.RpcPpeNo ?? "") + (string.IsNullOrWhiteSpace(reportItem.RpcPpe) ? "" : "\r\n" + reportItem.RpcPpe)}");

            ws.Row(row).Cell(++col).SetValue($"{(reportItem.AccountableOfficer ?? "") + (string.IsNullOrWhiteSpace(reportItem.ParOfficers) ? "" : "\r\n" + reportItem.ParOfficers)} " +
                $"/ {(reportItem.IcsOfficer ?? "") + (string.IsNullOrWhiteSpace(reportItem.IcsOfficer) ? "" : "\r\n" + reportItem.IcsOfficers)} " +
                $"/ {(reportItem.AreOfficer ?? "") + (string.IsNullOrWhiteSpace(reportItem.AreOfficers) ? "" : "\r\n" + reportItem.AreOfficers)} " +
                $"/ {(reportItem.MrOfficer ?? "") + (string.IsNullOrWhiteSpace(reportItem.MrOfficers) ? "" : "\r\n" + reportItem.MrOfficers)}" +
                $"/ {(reportItem.RpcPpeOfficer ?? "") + (string.IsNullOrWhiteSpace(reportItem.RpcPpeOfficers) ? "" : "\r\n" + reportItem.RpcPpeOfficers)}");

            ws.Row(row).Cell(++col).SetValue($"{(reportItem.ParIssuedTo ?? "") + (string.IsNullOrWhiteSpace(reportItem.ParIssuedsTo) ? "" : "\r\n" + reportItem.ParIssuedsTo)} " +
                $"/ {(reportItem.IcsIssuedTo ?? "") + (string.IsNullOrWhiteSpace(reportItem.IcsIssuedsTo) ? "" : "\r\n" + reportItem.IcsIssuedsTo)} " +
                $"/ {(reportItem.AreIssuedTo ?? "") + (string.IsNullOrWhiteSpace(reportItem.AreIssuedsTo) ? "" : "\r\n" + reportItem.AreIssuedsTo)} " +
                $"/ {(reportItem.MrIssuedTo ?? "") + (string.IsNullOrWhiteSpace(reportItem.MrIssuedsTo) ? "" : "\r\n" + reportItem.MrIssuedsTo)}" +
                $"/ {(reportItem.RpcPpeIssuedTo ?? "") + (string.IsNullOrWhiteSpace(reportItem.RpcPpeIssuedsTo) ? "" : "\r\n" + reportItem.RpcPpeIssuedTo)}");

            ws.Row(row).Cell(++col).SetValue(reportItem.UpcomingPar);
            ws.Row(row).Cell(++col).SetValue(reportItem.Fund);
            ws.Row(row).Cell(++col).SetValue(reportItem.Condition);
            ws.Row(row).Cell(++col).SetValue(reportItem.Remarks);
            if (!isAnnex)
            {
                ws.Row(row).Cell(++col).SetValue(reportItem.Annex);
            }
        }

        private void SetStockRowColValueOld(IXLWorksheet ws, CustodianReportItem reportItem, int row, bool isAnnex)
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
            ws.Row(row).Cell(++col).SetValue(reportItem.ItemSerialNo);
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
            var ics = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ICS").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            var ares = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            var mrs = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            ws.Row(row).Cell(++col).SetValue($"{(reportItem.ParNo ?? "") + (string.IsNullOrWhiteSpace(pars) ? "" : "\r\n" + pars)} " +
                $"/ {(reportItem.IcsNo ?? "") + (string.IsNullOrWhiteSpace(ics) ? "" : "\r\n" + ics)} " +
                $"/ {(reportItem.AreNo ?? "") + (string.IsNullOrWhiteSpace(ares) ? "" : "\r\n" + ares)} " +
                $"/ {(reportItem.MrNo ?? "") + (string.IsNullOrWhiteSpace(mrs) ? "" : "\r\n" + mrs)}");

            var parOfficers = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            var icsOfficers = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ICS").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            var areOfficers = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            var mrOfficers = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            ws.Row(row).Cell(++col).SetValue($"{(reportItem.AccountableOfficer ?? "") + (string.IsNullOrWhiteSpace(parOfficers) ? "" : "\r\n" + parOfficers)} " +
                $"/ {(reportItem.IcsOfficer ?? "") + (string.IsNullOrWhiteSpace(icsOfficers) ? "" : "\r\n" + icsOfficers)} " +
                $"/ {(reportItem.AreOfficer ?? "") + (string.IsNullOrWhiteSpace(areOfficers) ? "" : "\r\n" + areOfficers)} " +
                $"/ {(reportItem.MrOfficer ?? "") + (string.IsNullOrWhiteSpace(mrOfficers) ? "" : "\r\n" + mrOfficers)}");

            var parIssuedTo = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            var icsIssuedTo = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ICS").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            var areIssuedTo = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            var mrIssuedTo = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            ws.Row(row).Cell(++col).SetValue($"{(reportItem.ParIssuedTo ?? "") + (string.IsNullOrWhiteSpace(parIssuedTo) ? "" : "\r\n" + parIssuedTo)} " +
                $"/ {(reportItem.IcsIssuedTo ?? "") + (string.IsNullOrWhiteSpace(icsIssuedTo) ? "" : "\r\n" + icsIssuedTo)} " +
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

        private void SetPpeRowColValueOld(IXLWorksheet ws, CustodianReportItem reportItem, int row, bool isAnnex)
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
            ws.Row(row).Cell(++col).SetValue(reportItem.ItemSerialNo);
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
            var ics = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ICS").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            var ares = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            var mrs = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            //ws.Cell(2, 1).Style.Alignment.WrapText = true; // Enable text wrapping
            ws.Row(row).Cell(++col).SetValue($"{(reportItem.ParNo ?? "") + (string.IsNullOrWhiteSpace(pars) ? "" : "\r\n" + pars)} " +
                $"/ {(reportItem.IcsNo ?? "") + (string.IsNullOrWhiteSpace(ics) ? "" : "\r\n" + ics)} " +
                $"/ {(reportItem.AreNo ?? "") + (string.IsNullOrWhiteSpace(ares) ? "" : "\r\n" + ares)} " +
                $"/ {(reportItem.MrNo ?? "") + (string.IsNullOrWhiteSpace(mrs) ? "" : "\r\n" + mrs)}");

            var parOfficers = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            var icsOfficers = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ICS").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            var areOfficers = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            var mrOfficers = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            ws.Row(row).Cell(++col).SetValue($"{(reportItem.AccountableOfficer ?? "") + (string.IsNullOrWhiteSpace(parOfficers) ? "" : "\r\n" + parOfficers)} " +
                $"/ {(reportItem.IcsOfficer ?? "") + (string.IsNullOrWhiteSpace(icsOfficers) ? "" : "\r\n" + icsOfficers)} " +
                $"/ {(reportItem.AreOfficer ?? "") + (string.IsNullOrWhiteSpace(areOfficers) ? "" : "\r\n" + areOfficers)} " +
                $"/ {(reportItem.MrOfficer ?? "") + (string.IsNullOrWhiteSpace(mrOfficers) ? "" : "\r\n" + mrOfficers)}");

            var parIssuedTo = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            var icsIssuedTo = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ICS").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            var areIssuedTo = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            var mrIssuedTo = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            ws.Row(row).Cell(++col).SetValue($"{(reportItem.ParIssuedTo ?? "") + (string.IsNullOrWhiteSpace(parIssuedTo) ? "" : "\r\n" + parIssuedTo)} " +
                $"/ {(reportItem.IcsIssuedTo ?? "") + (string.IsNullOrWhiteSpace(icsIssuedTo) ? "" : "\r\n" + icsIssuedTo)} " +
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

        private void SetVehicleRowColValueOld(IXLWorksheet ws, CustodianReportItem reportItem, int row, bool isAnnex)
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
            var ics = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ICS").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            var ares = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            var mrs = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.RefNo));
            ws.Row(row).Cell(++col).SetValue($"{(reportItem.ParNo ?? "") + (string.IsNullOrWhiteSpace(pars) ? "" : "\r\n" + pars)} " +
                $"/ {(reportItem.IcsNo ?? "") + (string.IsNullOrWhiteSpace(ics) ? "" : "\r\n" + ics)} " +
                $"/ {(reportItem.AreNo ?? "") + (string.IsNullOrWhiteSpace(ares) ? "" : "\r\n" + ares)} " +
                $"/ {(reportItem.MrNo ?? "") + (string.IsNullOrWhiteSpace(mrs) ? "" : "\r\n" + mrs)}");

            var parOfficers = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            var icsOfficers = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ICS").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            var areOfficers = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            var mrOfficers = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.AccountableOfficer));
            ws.Row(row).Cell(++col).SetValue($"{(reportItem.AccountableOfficer ?? "") + (string.IsNullOrWhiteSpace(parOfficers) ? "" : "\r\n" + parOfficers)} " +
                $"/ {(reportItem.IcsOfficer ?? "") + (string.IsNullOrWhiteSpace(icsOfficers) ? "" : "\r\n" + icsOfficers)} " +
                $"/ {(reportItem.AreOfficer ?? "") + (string.IsNullOrWhiteSpace(areOfficers) ? "" : "\r\n" + areOfficers)} " +
                $"/ {(reportItem.MrOfficer ?? "") + (string.IsNullOrWhiteSpace(mrOfficers) ? "" : "\r\n" + mrOfficers)}");

            var parIssuedTo = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "PAR").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            var icsIssuedTo = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ICS").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            var areIssuedTo = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "ARE").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            var mrIssuedTo = string.Join("\r\n", reportItem.CustodianReportItemIssuances.Where(w => w.RefType == "MR").OrderBy(o => o.RefNo).Select(s => s.IssuedTo));
            ws.Row(row).Cell(++col).SetValue($"{(reportItem.ParIssuedTo ?? "") + (string.IsNullOrWhiteSpace(parIssuedTo) ? "" : "\r\n" + parIssuedTo)} " +
                $"/ {(reportItem.IcsIssuedTo ?? "") + (string.IsNullOrWhiteSpace(icsIssuedTo) ? "" : "\r\n" + icsIssuedTo)} " +
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


        private MemoryStream ProcessExcelFileStockTemplate(int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup
            , string templateFilePath, string hdg, string annex, string mainAccount, DateTime? asOf
            , string subAccount1, string subAccount2, string subAccount3, string subAccount4, string userName)
        {
            using (XLWorkbook wb = new XLWorkbook(templateFilePath))
            {
                int sw = 1;
                int row = 14;
                string account = "";
                string itemTypeIndex = "";
                string itemCodeIndex = "";
                string department = "";
                decimal? tAcqCost = 0;
                var ws = wb.Worksheet(1);
                var subAccount = new[] { subAccount4, subAccount3, subAccount2, subAccount1 }.FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? string.Empty;
                var userId = _userService.GetByUserName(userName).Id;
                var userIsAdmin = _userService.IsUserNameAdmin(userName);                
                var reportItems = _db.Database.SqlQuery<CustodianReportItemStockVM>("Exec CustodianReport_GetItems {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}",
                    forYear, deptId, sectionId, accountGroup, mainAccount, asOf, annex, userIsAdmin, subAccount1, userId).AsQueryable();
                
                if (reportItems.Any() && string.IsNullOrWhiteSpace(annex))
                {
                    reportItems = reportItems.Where(w => w.Annex != "D");
                }

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

                    if (reportItems.Any())
                    {
                        var reportId = reportItems.First().ReportId;
                        var report = _db.CustodianReports.Include(i => i.Codextn).FirstOrDefault(f => f.Id == reportId);
                        ws.Row(10).Cell(2).SetValue($"{report.Codextn.Code} {report.Department}").Style.Font.Bold = true;                        
                    }
                }
                else
                {
                    var codextn = _db.Codextns.Find(deptId);
                    ws.Row(10).Cell(2).SetValue($"{codextn.Code} {codextn.Description}").Style.Font.Bold = true;
                }

                if (!reportItems.Any())
                {
                    var accountCell = ws.Row(6).Cell(2);
                    accountCell.SetValue(mainAccount);
                    accountCell.Style.Font.Bold = true;
                    accountCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    accountCell.Style.Alignment.WrapText = false;
                }

                foreach (var reportItem in reportItems)
                {
                    if (sw == 1)
                    {
                        itemTypeIndex = reportItem.ItemTypeIndex;
                        itemCodeIndex = reportItem.ItemCodeIndex;
                        account = reportItem.Account;
                        department = reportItem.Department;                        
                        sw = 0;

                        var accountCell = ws.Row(6).Cell(2);
                        accountCell.SetValue(account);
                        accountCell.Style.Font.Bold = true;
                        accountCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        accountCell.Style.Alignment.WrapText = false;
                    }

                    if (itemTypeIndex != reportItem.ItemTypeIndex || department != reportItem.Department)
                    {
                        itemTypeIndex = reportItem.ItemTypeIndex;
                        itemCodeIndex = reportItem.ItemCodeIndex;
                        account = reportItem.Account;
                        department = reportItem.Department;
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
                        //ws.Row(row).Cell(2).SetValue(account).Style.Font.Bold = true;
                        //row += 2;
                        //ws.Row(row).Cell(2).SetValue("Department:");
                        //if (id == null)
                        //{
                        //    if (deptId == null || deptId == Guid.Empty)
                        //    {
                        //        ws.Row(row).Cell(3).SetValue("ALL").Style.Font.Bold = true;
                        //    }
                        //}
                        row += 2;
                        var deptCell = ws.Row(row).Cell(2);
                        deptCell.SetValue($"{reportItem.DeptCode} {reportItem.Department}");
                        deptCell.Style.Font.Bold = true;
                        deptCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        deptCell.Style.Alignment.WrapText = false;
                        if (deptCell.IsMerged())
                        {
                            deptCell.MergedRange().Unmerge();
                        }
                        //ws.Row(row).Cell(2).SetValue($"{reportItem.DeptCode} {reportItem.Department}").Style.Font.Bold = true;
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

        private MemoryStream ProcessExcelFilePpeTemplate(int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup
            , string templateFilePath, string hdg, string annex, string mainAccount, DateTime? asOf
            , string subAccount1, string subAccount2, string subAccount3, string subAccount4, string userName)
        {
            using (XLWorkbook wb = new XLWorkbook(templateFilePath))
            {
                int sw = 1;
                int row = 14;
                string itemTypeIndex = "";
                string itemCodeIndex = "";
                string account = "";
                string department = "";
                decimal? tAcqCost = 0;
                var ws = wb.Worksheet(1);
                var subAccount = new[] { subAccount4, subAccount3, subAccount2, subAccount1 }.FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? string.Empty;
                var userId = _userService.GetByUserName(userName).Id;
                var userIsAdmin = _userService.IsUserNameAdmin(userName);
                var reportItems = _db.Database.SqlQuery<CustodianReportItemPpeVM>("Exec CustodianReport_GetItems {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}",
                    forYear, deptId, sectionId, accountGroup, mainAccount, asOf, annex, userIsAdmin, subAccount, userId).AsQueryable();                

                if (reportItems.Any() && string.IsNullOrWhiteSpace(annex))
                {
                    reportItems = reportItems.Where(w => w.Annex != "D");
                }

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

                    if (reportItems.Any())
                    {
                        var reportId = reportItems.First().ReportId;
                        var report = _db.CustodianReports.Include(i => i.Codextn).FirstOrDefault(f => f.Id == reportId);
                        ws.Row(10).Cell(2).SetValue($"{report.Codextn.Code} {report.Department}").Style.Font.Bold = true;                        
                    }
                }
                else
                {
                    var codextn = _db.Codextns.Find(deptId);
                    ws.Row(10).Cell(2).SetValue($"{codextn.Code} {codextn.Description}").Style.Font.Bold = true;
                }

                if (!reportItems.Any())
                {
                    var accountCell = ws.Row(6).Cell(2);
                    accountCell.SetValue(mainAccount);
                    accountCell.Style.Font.Bold = true;
                    accountCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    accountCell.Style.Alignment.WrapText = false;
                }

                foreach (var reportItem in reportItems)
                {
                    if (sw == 1)
                    {
                        itemTypeIndex = reportItem.ItemTypeIndex;
                        itemCodeIndex = reportItem.ItemCodeIndex;
                        account = reportItem.Account;
                        department = reportItem.Department;                        
                        sw = 0;

                        var accountCell = ws.Row(6).Cell(2);
                        accountCell.SetValue(account);
                        accountCell.Style.Font.Bold = true;
                        accountCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        accountCell.Style.Alignment.WrapText = false;
                    }

                    if (itemTypeIndex != reportItem.ItemTypeIndex || department != reportItem.Department)
                    {
                        itemTypeIndex = reportItem.ItemTypeIndex;
                        itemCodeIndex = reportItem.ItemCodeIndex;
                        account = reportItem.Account;
                        department = reportItem.Department;
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
                        //ws.Row(row).Cell(2).SetValue(account).Style.Font.Bold = true;
                        //row += 2;
                        //ws.Row(row).Cell(2).SetValue("Department:");

                        //if (id == null)
                        //{
                        //    if (deptId == null || deptId == Guid.Empty)
                        //    {
                        //        ws.Row(row).Cell(3).SetValue("ALL").Style.Font.Bold = true;
                        //    }
                        //}

                        row += 2;
                        var deptCell = ws.Row(row).Cell(2);
                        deptCell.SetValue($"{reportItem.DeptCode} {reportItem.Department}");
                        deptCell.Style.Font.Bold = true;
                        deptCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        deptCell.Style.Alignment.WrapText = false;
                        if (deptCell.IsMerged())
                        {
                            deptCell.MergedRange().Unmerge();
                        }
                        //ws.Row(row).Cell(2).SetValue($"{reportItem.DeptCode} {reportItem.Department}").Style.Font.Bold = true;
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

        private MemoryStream ProcessExcelFileVehicleTemplate(int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup
            , string templateFilePath, string hdg, string annex, string mainAccount, DateTime? asOf
            , string subAccount1, string subAccount2, string subAccount3, string subAccount4, string userName)
        {
            using (XLWorkbook wb = new XLWorkbook(templateFilePath))
            {
                int sw = 1;
                int row = 14;
                string itemTypeIndex = "";
                string itemCodeIndex = "";
                string account = "";
                string department = "";
                decimal? tAcqCost = 0;
                var ws = wb.Worksheet(1);
                var subAccount = new[] { subAccount4, subAccount3, subAccount2, subAccount1 }.FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? string.Empty;
                var userId = _userService.GetByUserName(userName).Id;
                var userIsAdmin = _userService.IsUserNameAdmin(userName);
                var reportItems = _db.Database.SqlQuery<CustodianReportItemVehicleVM>("Exec CustodianReport_GetItems {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}",
                    forYear, deptId, sectionId, accountGroup, mainAccount, asOf, annex, userIsAdmin, subAccount, userId).AsQueryable();                

                if (reportItems.Any() && string.IsNullOrWhiteSpace(annex))
                {
                    reportItems = reportItems.Where(w => w.Annex != "D");
                }

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

                    if (reportItems.Any())
                    {
                        var reportId = reportItems.First().ReportId;
                        var report = _db.CustodianReports.Include(i => i.Codextn).FirstOrDefault(f => f.Id == reportId);
                        ws.Row(10).Cell(2).SetValue($"{report.Codextn.Code} {report.Department}").Style.Font.Bold = true;
                    }
                }
                else
                {
                    var codextn = _db.Codextns.Find(deptId);
                    ws.Row(10).Cell(2).SetValue($"{codextn.Code} {codextn.Description}").Style.Font.Bold = true;
                }

                if (!reportItems.Any())
                {
                    var accountCell = ws.Row(6).Cell(2);
                    accountCell.SetValue(mainAccount);
                    accountCell.Style.Font.Bold = true;
                    accountCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    accountCell.Style.Alignment.WrapText = false;
                }

                foreach (var reportItem in reportItems)
                {
                    if (sw == 1)
                    {
                        itemTypeIndex = reportItem.ItemTypeIndex;
                        itemCodeIndex = reportItem.ItemCodeIndex;
                        account = reportItem.Account;
                        department = reportItem.Department;
                        sw = 0;

                        var accountCell = ws.Row(6).Cell(2);
                        accountCell.SetValue(account);
                        accountCell.Style.Font.Bold = true;
                        accountCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        accountCell.Style.Alignment.WrapText = false;
                    }

                    if (itemTypeIndex != reportItem.ItemTypeIndex || department != reportItem.Department)
                    {
                        itemTypeIndex = reportItem.ItemTypeIndex;
                        itemCodeIndex = reportItem.ItemCodeIndex;
                        account = reportItem.Account;
                        department = reportItem.Department;
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
                        //ws.Row(row).Cell(2).SetValue(account).Style.Font.Bold = true;
                        //row += 2;
                        //ws.Row(row).Cell(2).SetValue("Department:");
                        //if (id == null)
                        //{
                        //    if (deptId == null || deptId == Guid.Empty)
                        //    {
                        //        ws.Row(row).Cell(3).SetValue("ALL").Style.Font.Bold = true;
                        //    }
                        //}
                        row += 2;
                        var deptCell = ws.Row(row).Cell(2);
                        deptCell.SetValue($"{reportItem.DeptCode} {reportItem.Department}");
                        deptCell.Style.Font.Bold = true;
                        deptCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        deptCell.Style.Alignment.WrapText = false;
                        if (deptCell.IsMerged())
                        {
                            deptCell.MergedRange().Unmerge();
                        }
                        //ws.Row(row).Cell(2).SetValue($"{reportItem.DeptCode} {reportItem.Department}").Style.Font.Bold = true;
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

        private MemoryStream ProcessExcelFileStockTemplate(int? forYear, Guid? deptId,  Guid? sectionId, int? accountGroup, string templateFilePath, string mainAccount, DateTime? asOf
            , string subAccount1, string subAccount2, string subAccount3, string subAccount4, string userName)
        {
            return ProcessExcelFileStockTemplate(forYear, deptId, sectionId, accountGroup, templateFilePath, "", "", mainAccount, asOf, subAccount1, subAccount2, subAccount3, subAccount4, userName);
        }

        private MemoryStream ProcessExcelFilePpeTemplate(int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, string templateFilePath, string mainAccount, DateTime? asOf
            , string subAccount1, string subAccount2, string subAccount3, string subAccount4, string userName)
        {
            return ProcessExcelFilePpeTemplate(forYear, deptId, sectionId, accountGroup, templateFilePath, "", "", mainAccount, asOf, subAccount1, subAccount2, subAccount3, subAccount4, userName);
        }

        private MemoryStream ProcessExcelFileVehicleTemplate(int? forYear, Guid? deptId, Guid? sectiondI, int? accountGroup, string templateFilePath, string mainAccount, DateTime? asOf
            , string subAccount1, string subAccount2, string subAccount3, string subAccount4, string userName)
        {
            return ProcessExcelFileVehicleTemplate(forYear, deptId, sectiondI, accountGroup, templateFilePath, "", "", mainAccount, asOf, subAccount1, subAccount2, subAccount3, subAccount4, userName);
        }

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

            if (accountGroup == (int?)CustodianAccountGroup.STOCK)
            {
                return ProcessExcelFileStockTemplate(forYear, deptId, sectionId, accountGroup, templateFilePath, hdg, annex, mainAccount, asOf
                    , subAccount1, subAccount2, subAccount3, subAccount4, userName);
            }
            else if (accountGroup == (int?)CustodianAccountGroup.PPE)
            {
                return ProcessExcelFilePpeTemplate(forYear, deptId, sectionId, accountGroup, templateFilePath, hdg, annex, mainAccount, asOf
                    , subAccount1, subAccount2, subAccount3, subAccount4, userName);
            }
            else
            {
                return ProcessExcelFileVehicleTemplate(forYear, deptId, sectionId, accountGroup, templateFilePath, hdg, annex, mainAccount, asOf
                    , subAccount1, subAccount2, subAccount3, subAccount4, userName);
            }
        }

        private void ValidateIfNull(CustodianReportItem model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(CustodianReportItem entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
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

        private void ValidateIfSubmitted(CustodianReportItem model)
        {
            var submitForCount = _db.CustodianReportSubmitForCounts.FirstOrDefault(f => f.ReportId == model.ReportId && f.LocationId == model.LocationId && f.Status == "Submit");
            if (submitForCount != null)
            {
                var msg = $"Record already submitted for count by {submitForCount.UpdatedBy} on {submitForCount.UpdatedDt}, cannot update!";
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