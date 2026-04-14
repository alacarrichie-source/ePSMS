using ClosedXML.Excel;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Items;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.CustodianReports
{
    public interface ICustodianReportService
    {
        IQueryable<CustodianReport> GetAll(int? forYear);
        IQueryable<CustodianReport> GetAllByAccountGroup(int? forYear, int? accountGroup);
        IQueryable<CustodianReport> GetAllByDepartmentAccountGroup(int? forYear, Guid? deptId, int? accountGroup);
        IQueryable<CustodianReportSummaryVM> GetSummary(int? forYear, Guid? deptId, Guid? sectionId, DateTime? asOf, DateTime? insertedAsOf);
        ValueTask<CustodianReport> GetByIdAsync(Guid? id);
        string GetAccountGroupMenuId(CustodianAccountGroup accountGroup);
        string GetAccountGroupMenuId(int? accountGroup);
        int GetReportingYearEnd();
        Task<Codextn> GetReportingYearEndAsync(int year);

        ValueTask<CustodianReport> PostAsync(Guid id, string user, DateTime date);
        ValueTask<CustodianReport> UnPostAsync(Guid id, string user, DateTime date);
        ValueTask<CustodianReport> CreateAsync(CustodianReport model, string user, DateTime date);
        ValueTask<CustodianReport> UpdateAsync(CustodianReport model, string user, DateTime date);
        ValueTask<CustodianReport> DeleteAsync(CustodianReport model, string user, DateTime date);

        ValueTask<CustodianReport> Download(int? forYear, Guid? deptId, int? accountGroup, string user, DateTime date);
        ValueTask<CustodianReport> DownloadBldg(int? forYear, Guid? deptId, int? accountGroup, string user, DateTime date);
        ValueTask<CustodianReport> DownloadLand(int? forYear, Guid? deptId, int? accountGroup, string user, DateTime date);
        ValueTask<CustodianReport> Upload(int? forYear, Guid? deptId, int? accountGroup, string user, DateTime date);
        ValueTask<CustodianReport> UploadBldg(int? forYear, Guid? deptId, int? accountGroup, string user, DateTime date);
        ValueTask<CustodianReport> UploadLand(int? forYear, Guid? deptId, int? accountGroup, string user, DateTime date);

        MemoryStream ProcessExcelFileSummary(int? forYear, Guid? deptId, Guid? locationId, DateTime? asOf, DateTime? insertedAsOf, string templateFilePath);
    }

    public class CustodianReportService : ICustodianReportService
    {
        private readonly AppManEntities _db;
        //private readonly IAppManEntitiesFactory _contextFactory;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<CustodianReport> _exceptionService;
        private readonly ICustodianReportValidator _validator;
        private readonly IUserService _userService;
        private readonly IItemCodeService _itemCodeService;
        private readonly IUploadService _uploadService;

        public CustodianReportService(AppManEntities db)
        {
            _db = db;
            _exceptions = new CreateAndLogExceptions();
            _exceptionService = new ExceptionService<CustodianReport>();
            _validator = new CustodianReportValidator(_db);
            _userService = new UserService(_db);
            _itemCodeService = new ItemCodeService(_db);
            _uploadService = new UploadService(_db);
        }

        //public CustodianReportService(AppManEntities db,
        //    IAppManEntitiesFactory appManEntitiesFactory,
        //    ICreateAndLogExceptions exceptions,
        //    IExceptionService<CustodianReport> exceptionService,
        //    IUserService userService,
        //    IItemCodeService itemCodeService,
        //    IUploadService uploadService,
        //    ICustodianReportValidator validator)
        //{
        //    _db = db;
        //    _contextFactory = appManEntitiesFactory;
        //    _exceptions = exceptions;
        //    _exceptionService = exceptionService;
        //    _validator = validator;
        //    _userService = userService;
        //    _itemCodeService = itemCodeService;
        //    _uploadService = uploadService;
        //}

        public ValueTask<CustodianReport> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.CustodianReports.FindAsync(id);
            return data;
        });

        public ValueTask<CustodianReport> GetByAsOfAsync(DateTime? AsOf) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.CustodianReports.Where(w => w.AsOf == AsOf).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<CustodianReport> GetAll(int? forYear) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.CustodianReports.AsNoTracking().Where(w => w.AsOf.Value.Year == forYear).AsQueryable();
            return data;
        });

        public IQueryable<CustodianReport> GetAllByAccountGroup(int? forYear, int? accountGroup) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.CustodianReports.AsNoTracking().Where(w => w.AsOf.Value.Year == forYear && w.AccountGroup == accountGroup).AsQueryable();
            return data;
        });

        public IQueryable<CustodianReport> GetAllByDepartmentAccountGroup(int? forYear, Guid? deptId, int? accountGroup)
        {
            var data = _db.CustodianReports.AsNoTracking().Where(w => w.AsOf.Value.Year == forYear && w.DeptId == deptId && w.AccountGroup == accountGroup).AsQueryable();
            return data;
        }

        public IQueryable<CustodianReportSummaryVM> GetSummary(int? forYear, Guid? deptId, Guid? sectionId, DateTime? asOf, DateTime? insertedAsOf)
        {
            var data = _db.Database.SqlQuery<CustodianReportSummaryVM>("Exec CustodianReport_GetSummary {0}, {1}, {2}, {3}", forYear, deptId, sectionId, asOf, insertedAsOf).AsQueryable();
            return data;
        }

        public string GetAccountGroupMenuId(CustodianAccountGroup accountGroup)
        {
            return GetAccountGroupMenuId((int?)accountGroup);
        }

        public string GetAccountGroupMenuId(int? accountGroup)
        {
            string menuId = "";
            if (accountGroup == (int?)CustodianAccountGroup.PPE)
            {
                menuId = "custodian_report_ppe";
            }
            else if (accountGroup == (int?)CustodianAccountGroup.STOCK)
            {
                menuId = "custodian_report_stock";
            }
            else if (accountGroup == (int?)CustodianAccountGroup.VEHICLE)
            {
                menuId = "custodian_report_vehicle";
            }
            else if (accountGroup == (int?)CustodianAccountGroup.LAND)
            {
                menuId = "custodian_report_land";
            }
            else if (accountGroup == (int?)CustodianAccountGroup.BUILDING)
            {
                menuId = "custodian_report_bldg";
            }
            return menuId;
        }

        public int GetReportingYearEnd()
        {
            var data = _db.Codextns.OrderByDescending(o => o.Description).FirstOrDefault(f => f.CodeMast.Code == "REPORT-YEAR-END");
            if (data != null)
            {
                return int.Parse(data.Description);
            }
            return DateTime.Now.Year;
        }

        public Task<Codextn> GetReportingYearEndAsync(int year)
        {
            return _db.Codextns.OrderByDescending(o => o.Description).FirstOrDefaultAsync(f => f.CodeMast.Code == "REPORT-YEAR-END" && f.Description == year.ToString());
        }

        public ValueTask<CustodianReport> PostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            _validator.ValidateOnPost(id);

            //using (var ctx = await _contextFactory.CreateContextAsync())
            //{
            var entity = await _db.CustodianReports.FindAsync(id);

            entity.PostedBy = user;
            entity.PostedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            await _db.SaveChangesAsync();
            return entity;
            //}
        });

        public ValueTask<CustodianReport> UnPostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            _validator.ValidateOnUnpost(id);

            //using (var ctx = await _contextFactory.CreateContextAsync())
            //{
            var entity = await _db.CustodianReports.FindAsync(id);

            //// check if in disposal
            //if(_db.CustodianReportItems.Where(w => w.Report`Id == id && w.CustodianDisposalItems.Any()).Any())
            //{
            //    throw new RecordAlreadyExistsException("Record already in disposal entry, cannot unpost!");
            //}

            entity.PostedBy = "";
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            await _db.SaveChangesAsync();
            return entity;
            //}
        });

        public ValueTask<CustodianReport> CreateAsync(CustodianReport model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {

            _validator.ValidateOnCreate(model);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            //using (var ctx = await _contextFactory.CreateContextAsync())
            //{
            var entity = new CustodianReport();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.CustodianReports.Add(entity);
            await _db.SaveChangesAsync();

            return model;
            //}
        });

        public ValueTask<CustodianReport> UpdateAsync(CustodianReport model, string user, DateTime date) =>
       _exceptionService.TryCatch(async () =>
       {
           _validator.ValidateOnUpdate(model);

           //using (var ctx = await _contextFactory.CreateContextAsync())
           //{
           var entity = await _db.CustodianReports.FindAsync(model.Id);

           model.UpdatedBy = user;
           model.UpdatedDt = date;

           MapModelToEntityFields(entity, model, Mode.EDIT);

           await _db.SaveChangesAsync();
           return model;
           //}
       });

        public ValueTask<CustodianReport> DeleteAsync(CustodianReport model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            _validator.ValidateOnDelete(model);

            //using (var ctx = await _contextFactory.CreateContextAsync())
            //{
            var entity = await _db.CustodianReports.Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();

            _db.CustodianReports.Remove(entity);
            await _db.SaveChangesAsync();

            return model;
            //}
        });

        public void MapModelToEntityFields(CustodianReport entity, CustodianReport model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            entity.AsOf = model.AsOf;
            entity.Fund = model.Fund;
            entity.AccountGroup = model.AccountGroup;
            entity.DeptId = model.DeptId;
            entity.Department = model.Department;
            entity.CertifiedCorrectBy = model.CertifiedCorrectBy;
            entity.ApprovedBy = model.ApprovedBy;
            entity.VerifiedBy = model.VerifiedBy;
            entity.PostedBy = model.PostedBy;
            entity.PostedDt = model.PostedDt;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        private async ValueTask UploadPhotosAsync(CustodianReport custodianReport, string user, DateTime date)
        {
            // include photos
            var directoryPath = $"{_uploadService.GetDirectoryPath()}CARD/";
            foreach (var item in custodianReport.CustodianReportItems)
            {
                var imageId = item?.CustodianReportUpload?.PsCardItemExtnId;
                if (imageId != null)
                {
                    await _uploadService.CopyAsync(imageId, directoryPath, user, date);
                }
            }
        }

        private async ValueTask CreateIcsParsAsync(CustodianReportItem cri, Guid? psCardItemExtnId, string refType, string refNo, string user, DateTime date)
        {
            refNo = refNo.Trim();
            if (refNo.Length > 50)
            {
                return;
            }

            //using (var ctx = await _contextFactory.CreateContextAsync())
            //{
            var icsPar = await _db.IcsPars.FirstOrDefaultAsync(f => f.RefType == refType && f.RefNo == refNo);
            if (icsPar == null)
            {
                icsPar = new IcsPar()
                {
                    Id = Guid.NewGuid(),
                    UpdateCode = "M", // Migration
                    RefNo = refNo,
                    RefType = refType,
                    RefDate = null,
                    LocationId = cri.LocationId,
                    LocationCode = cri.LocationCode,
                    Location = cri.Location,
                    ReceivedBy = cri.IcsOfficer,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };

                var icsParItem = new IcsParItem()
                {
                    Id = Guid.NewGuid(),
                    IcsParId = icsPar.Id,
                    PsCardItemExtnId = psCardItemExtnId,
                    Qty = 1,
                    Amount = cri.TotalCost,
                    IssuedTo = cri.IcsIssuedsTo,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };

                icsPar.IcsParItems.Add(icsParItem);

                await _db.SaveChangesAsync();
            }
            //}
        }

        private async ValueTask<CustodianReportUpload> CreateCustodianReportAsync(Guid reportItemId, Guid? psCardId, string user, DateTime date)
        {
            //using (var ctx = await _contextFactory.CreateContextAsync())
            //{
            // create CustodianReportUpload record based on uploaded records
            var custodianReportUpload = new CustodianReportUpload()
            {
                Id = reportItemId,
                PsCardId = psCardId,
                UploadedBy = user,
                UploadedDt = date
            };

            _db.CustodianReportUploads.Add(custodianReportUpload);
            await _db.SaveChangesAsync();

            return custodianReportUpload;
            //}
        }

        public ValueTask<CustodianReport> Upload(int? forYear, Guid? deptId, int? accountGroup, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            //using (var ctx = await _contextFactory.CreateContextAsync())
            //{
            var custodianReport = await _db.CustodianReports.Include(i => i.CustodianReportItems).FirstOrDefaultAsync(f => f.AsOf.Value.Year == forYear && f.DeptId == deptId && f.AccountGroup == accountGroup);
            if (custodianReport == null)
            {
                throw new InvalidValueException("No selected records to upload.");
            }

            var reportId = custodianReport.Id;
            var custodianReportItems = await _db.CustodianReportItems.AsNoTracking()
                .Include(i => i.ItemCode.ItemType)
                .Include(i => i.CustodianReportUpload)
                .Include(i => i.CustodianReportItemIssuances)
                .Where(w => w.ReportId == reportId
                    && w.PostedBy != "" && w.PostedBy != null // must be posted
                    && w.Fund != null && w.Fund != "" && w.PsNo != null && w.PsNo != "") // must be with valid fields
                .ToListAsync();

            foreach (var cri in custodianReportItems)
            {
                CustodianReportUpload custodianReportUpload = cri.CustodianReportUpload;
                PsCard psCard = null;
                PsCardItem psCardItem = null;
                PsCardItemTransfer psCardItemTransfer = null;
                Guid psCardId = Guid.NewGuid();
                Guid psCardItemId = Guid.NewGuid();
                if (custodianReportUpload == null)
                {
                    psCard = await _db.PsCards
                        .Include(i => i.AllField).FirstOrDefaultAsync(f => f.PsNo == cri.PsNo && f.Fund == cri.Fund);
                }
                else
                {
                    psCard = await _db.PsCards
                        .Include(i => i.AllField)
                        .FirstOrDefaultAsync(f => f.Id == cri.CustodianReportUpload.PsCardId);
                }

                if (psCard == null)
                {
                    var subAccountCode = await _db.Database.SqlQuery<string>("Select Code From dbo.fn_SubAccountTab({0})", cri.ItemCode.Code).FirstOrDefaultAsync();
                    psCard = new PsCard()
                    {
                        Id = psCardId,
                        ItemCodeId = cri.ItemCodeId,
                        SubAccountCode = subAccountCode,
                        Fund = cri.Fund,
                        Description = "Please see attachment.",
                        PsNo = cri.PsNo,
                        FromDonation = cri.FromDonation,
                        CardCategory = cri.ItemCode.ItemType.Category == "S" ? "S" : "P",
                        InsertedBy = cri.InsertedBy,
                        InsertedDt = cri.InsertedDt,
                        UpdatedBy = cri.UpdatedBy,
                        UpdatedDt = cri.UpdatedDt
                    };

                    var allField = new AllField()
                    {
                        Id = psCard.Id,
                        GenericName = cri.GenericName,
                        DosageStrength = cri.DosageStrength,
                        DosageForm = cri.DosageForm,
                        DosageVolume = cri.DosageVolume,
                        Brand = cri.Brand,
                        Multipliers = cri.Multipliers,
                        Model_ = cri.Model_,
                        Dimension = cri.Dimension,
                        Size = cri.Size,
                        Capacity = cri.Capacity,
                        Weight = cri.Weight,
                        Materials = cri.Materials,
                        Color = cri.Color,
                        SerialNo = cri.SerialNo,
                        PropNo = cri.PropNo,
                        PlateNo = cri.PlateNo,
                        BodyNo = cri.BodyNo,
                        MVFileNo = cri.MVFileNo,
                        InsertedBy = cri.InsertedBy,
                        InsertedDt = cri.InsertedDt,
                        UpdatedBy = cri.UpdatedBy,
                        UpdatedDt = cri.UpdatedDt
                    };

                    psCard.AllField = allField;

                    _db.PsCards.Add(psCard);
                    await _db.SaveChangesAsync(); // need to save here so that this record will be included in the next findAsync
                }
                else
                {
                    psCardId = psCard.Id;
                }

                if (custodianReportUpload == null)
                {
                    // create CustodianReportUpload record based on uploaded records
                    custodianReportUpload = await CreateCustodianReportAsync(cri.Id, psCardId, user, date);

                    psCardItem = await _db.PsCardItems
                        .Include(i => i.PsCardItemTransfer.PsCardItemTransferItems)
                        .Include(i => i.PsCardItemExtns)
                        .FirstOrDefaultAsync(f => f.PsCardId == psCard.Id && f.PoNo == cri.PoNo && f.PoDate == cri.PoDate && f.Description == cri.Description);
                }
                else
                {
                    psCardItem = await _db.PsCardItems
                        .Include(i => i.PsCardItemTransfer.PsCardItemTransferItems)
                        .Include(i => i.PsCardItemExtns)
                        .FirstOrDefaultAsync(f => f.Id == cri.CustodianReportUpload.PsCardItemId);
                }

                if (psCardItem == null)
                {
                    psCardItem = new PsCardItem()
                    {
                        Id = psCardItemId,
                        GroupId = psCardItemId,
                        PsCardId = psCard.Id,
                        PoDate = cri.PoDate,
                        PoNo = cri.PoNo,
                        AirDate = cri.AirDate,
                        AirNo = cri.AirNo,
                        Qty = 1,
                        QtyBal = 1,
                        Unit = cri.Unit,
                        UnitCost = cri.UnitCost,
                        Amount = cri.TotalCost,
                        TUnitCost = cri.TUnitCost,
                        GTotalCost = cri.GTotalCost,
                        Remarks = cri.Remarks,
                        DeptId = cri.DeptId,
                        DeptDisplay = cri.Department,
                        LocationId = cri.LocationId,
                        Description = cri.Description,
                        OtherDesc = cri.OtherDesc,
                        InvDist = cri.InvDist,
                        PrevPsNo = cri.PropNo,
                        InsertedBy = cri.InsertedBy,
                        InsertedDt = cri.InsertedDt,
                        UpdatedBy = cri.UpdatedBy,
                        UpdatedDt = cri.UpdatedDt,
                        PostedBy = cri.PostedBy,
                        PostedDt = cri.PostedDt
                    };

                    _db.PsCardItems.Add(psCardItem);
                    //_db.Entry(psCardItem).State = EntityState.Added;
                }
                else
                {
                    psCardItemId = psCardItem.Id;
                    // Update PsCardItem Fields                                  
                    psCardItem.UpdatedBy = cri.UpdatedBy;
                    psCardItem.UpdatedDt = cri.UpdatedDt;

                    _db.PsCardItems.Attach(psCardItem);
                    _db.Entry(psCardItem).State = EntityState.Modified;
                }

                if (custodianReportUpload.PsCardItemId == null)
                {
                    custodianReportUpload.PsCardItemId = psCardItemId;
                    _db.CustodianReportUploads.Attach(custodianReportUpload);
                    _db.Entry(custodianReportUpload).State = EntityState.Modified;
                }

                await _db.SaveChangesAsync();

                // Update PsCardItemExnts
                Guid psCardItemExtnId = Guid.NewGuid();
                if ((int?)CustodianAccountGroup.PPE == accountGroup || (int?)CustodianAccountGroup.STOCK == accountGroup)
                {
                    PsCardItemExtnOther psCardItemExtn = null;
                    if (cri.CustodianReportUpload == null)
                    {
                        psCardItemExtn = await _db.PsCardItemExtns.OfType<PsCardItemExtnOther>()
                            .FirstOrDefaultAsync(f => f.PsCardItem.PsCard.PsNo == cri.PsNo
                                && f.PsCardItem.PoNo == cri.PoNo
                                && f.SerialNo == cri.SerialNo);
                    }
                    else
                    {
                        psCardItemExtn = await _db.PsCardItemExtns.OfType<PsCardItemExtnOther>()
                            .FirstOrDefaultAsync(f => f.Id == cri.CustodianReportUpload.PsCardItemExtnId);
                    }

                    if (psCardItemExtn == null)
                    {
                        psCardItemExtn = new PsCardItemExtnOther()
                        {
                            Id = psCardItemExtnId,
                            GroupId = psCardItemExtnId,
                            PsCardItemId = psCardItem.Id,
                            //SetLotNo = 1,
                            //SetLotQtyNo =,
                            //ContentNo = cri.SetLotNo,
                            CustItemNo = cri.CustodianItemNo,
                            LocationId = cri.LocationId,
                            PropNo = cri.PropNo,
                            SeriesNo = cri.SeriesNo,
                            Remarks = cri.Remarks,
                            Annex = cri.Annex,
                            OldPropNo = cri.OldPropNo,
                            UpcomingOfficer = cri.UpcomingPar,
                            InsertedBy = cri.InsertedBy,
                            InsertedDt = cri.InsertedDt,
                            UpdatedBy = cri.UpdatedBy,
                            UpdatedDt = cri.UpdatedDt,
                            SubLocation = cri.SubLocation,
                            Condition = cri.Condition,
                            AddCost = cri.AddCost,
                            AcqCost = cri.TotalCost,
                            OldAmount = cri.OldAmount,
                            AcqDate = cri.AcqDate,
                            SerialNo = cri.SerialNo
                        };

                        _db.PsCardItemExtns.Add(psCardItemExtn);
                        _db.Entry(psCardItemExtn).State = EntityState.Added;
                    }
                    else
                    {
                        psCardItemExtnId = psCardItemExtn.Id;
                        psCardItemExtn.CustItemNo = cri.CustodianItemNo;
                        psCardItemExtn.SeriesNo = cri.SeriesNo;
                        psCardItemExtn.LocationId = cri.LocationId;
                        psCardItemExtn.Annex = cri.Annex;
                        psCardItemExtn.Remarks = cri.Remarks;
                        psCardItemExtn.SerialNo = cri.SerialNo;
                        psCardItemExtn.UpdatedBy = cri.UpdatedBy;
                        psCardItemExtn.UpdatedDt = cri.UpdatedDt;

                        _db.PsCardItemExtns.Attach(psCardItemExtn);
                        _db.Entry(psCardItemExtn).State = EntityState.Modified;
                    }
                }
                else
                {
                    PsCardItemExtnVehicle psCardItemExtn = null;
                    if (custodianReportUpload.PsCardItemExtnId == null)
                    {
                        psCardItemExtn = await _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>()
                            .FirstOrDefaultAsync(f => f.PsCardItem.PsCard.PsNo == cri.PsNo
                                && f.PsCardItem.PoNo == cri.PoNo
                                && f.PlateNo == cri.PlateNo && f.ConductionNo == cri.CustodianItemNo
                                && f.BodyNo == cri.BodyNo && f.EngineNo == cri.EngineNo);
                    }
                    else
                    {
                        psCardItemExtn = await _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>()
                            .FirstOrDefaultAsync(f => f.Id == cri.CustodianReportUpload.PsCardItemExtnId);
                    }

                    int.TryParse(cri.Weight, out int netWeight);
                    if (psCardItemExtn == null)
                    {
                        psCardItemExtn = new PsCardItemExtnVehicle()
                        {
                            Id = psCardItemExtnId,
                            GroupId = psCardItemExtnId,
                            PsCardItemId = psCardItem.Id,
                            //SetLotNo = 1,
                            //SetLotQtyNo =,
                            //ContentNo = cri.SetLotNo,
                            CustItemNo = cri.CustodianItemNo,
                            LocationId = cri.LocationId,
                            PropNo = cri.PropNo,
                            SeriesNo = cri.SeriesNo,
                            Remarks = cri.Remarks,
                            Annex = cri.Annex,
                            OldPropNo = cri.OldPropNo,
                            UpcomingOfficer = cri.UpcomingPar,
                            InsertedBy = cri.InsertedBy,
                            InsertedDt = cri.InsertedDt,
                            UpdatedBy = cri.UpdatedBy,
                            UpdatedDt = cri.UpdatedDt,
                            SubLocation = cri.SubLocation,
                            Condition = cri.Condition,
                            AddCost = cri.AddCost,
                            AcqCost = cri.TotalCost,
                            OldAmount = cri.OldAmount,
                            AcqDate = cri.AcqDate,
                            YearModel = cri.YearModel,
                            PlateNo = cri.PlateNo,
                            EngineNo = cri.EngineNo,
                            BodyNo = cri.BodyNo,
                            ChasisNo = cri.ChasisNo,
                            Color = cri.Color,
                            CRN = cri.CRN,
                            CRDate = cri.CRDate,
                            MVFileNo = cri.MVFileNo,
                            OrNo = cri.OrNo,
                            OrDate = cri.OrDate,
                            NetWeight = netWeight,
                            InsPolicyNo = cri.InsPolicyNo,
                            ConductionNo = cri.ConductionNo
                        };

                        _db.PsCardItemExtns.Add(psCardItemExtn);
                        _db.Entry(psCardItemExtn).State = EntityState.Added;
                    }
                    else
                    {
                        psCardItemExtnId = psCardItemExtn.Id;
                        psCardItemExtn.CustItemNo = cri.CustodianItemNo;
                        psCardItemExtn.SeriesNo = cri.SeriesNo;
                        psCardItemExtn.LocationId = cri.LocationId;
                        psCardItemExtn.Annex = cri.Annex;
                        psCardItemExtn.Remarks = cri.Remarks;
                        psCardItemExtn.YearModel = cri.YearModel;
                        psCardItemExtn.PlateNo = cri.PlateNo;
                        psCardItemExtn.ConductionNo = cri.ConductionNo;
                        psCardItemExtn.BodyNo = cri.BodyNo;
                        psCardItemExtn.EngineNo = cri.EngineNo;
                        psCardItemExtn.InsPolicyNo = cri.InsPolicyNo;
                        psCardItemExtn.OrNo = cri.OrNo;
                        psCardItemExtn.OrDate = cri.OrDate;
                        psCardItemExtn.NetWeight = netWeight;
                        psCardItemExtn.Color = cri.Color;
                        psCardItemExtn.CRN = cri.CRN;
                        psCardItemExtn.CRDate = cri.CRDate;
                        psCardItemExtn.MVFileNo = cri.MVFileNo;
                        psCardItemExtn.UpdatedBy = cri.UpdatedBy;
                        psCardItemExtn.UpdatedDt = cri.UpdatedDt;

                        _db.PsCardItemExtns.Attach(psCardItemExtn);
                        _db.Entry(psCardItemExtn).State = EntityState.Modified;
                    }
                }

                if (custodianReportUpload.PsCardItemExtnId == null)
                {
                    custodianReportUpload.PsCardItemExtnId = psCardItemExtnId;
                    _db.CustodianReportUploads.Attach(custodianReportUpload);
                    _db.Entry(custodianReportUpload).State = EntityState.Modified;
                }

                await _db.SaveChangesAsync();

                // Update PsCarditemTransfers
                Guid psCardItemTransferId = Guid.NewGuid();
                if (custodianReportUpload.PsCardTransferItemId == null)
                {
                    psCardItemTransfer = await _db.PsCardItemTransfers
                        .FirstOrDefaultAsync(f => f.PsCardItemId == psCardItem.Id && f.LocationId == cri.LocationId);
                }
                else
                {
                    psCardItemTransfer = await _db.PsCardItemTransfers.FirstOrDefaultAsync(f => f.Id == cri.CustodianReportUpload.PsCardTransferId);
                }

                if (psCardItemTransfer == null)
                {
                    psCardItemTransfer = new PsCardItemTransfer()
                    {
                        Id = psCardItemTransferId,
                        PsCardItemId = psCardItem.Id,
                        Qty = cri.Qty,
                        TransDate = cri.PoDate,
                        LocationId = cri.LocationId,
                        Amount = cri.UnitCost * cri.Qty,
                        InsertedBy = cri.InsertedBy,
                        InsertedDt = cri.InsertedDt,
                        UpdatedBy = cri.UpdatedBy,
                        UpdatedDt = cri.UpdatedDt
                    };

                    _db.PsCardItemTransfers.Add(psCardItemTransfer);
                    _db.Entry(psCardItemTransfer).State = EntityState.Added;
                }
                else
                {
                    psCardItemTransferId = psCardItemTransfer.Id;
                    _db.PsCardItemTransfers.Attach(psCardItemTransfer);
                    _db.Entry(psCardItemTransfer).State = EntityState.Modified;
                }

                if (custodianReportUpload.PsCardTransferId == null)
                {
                    custodianReportUpload.PsCardTransferId = psCardItemTransferId;
                    _db.CustodianReportUploads.Attach(custodianReportUpload);
                    _db.Entry(custodianReportUpload).State = EntityState.Modified;
                }

                await _db.SaveChangesAsync();

                if (custodianReportUpload.PsCardTransferItemId == null)
                {
                    var psCardItemTransferItem = await _db.PsCardItemTransferItems
                        .FirstOrDefaultAsync(f => f.PsCardItemTransferId == psCardItemTransfer.Id && f.PsCardItemExtnId == psCardItemExtnId);
                    if (psCardItemTransferItem == null)
                    {
                        psCardItemTransferItem = new PsCardItemTransferItem()
                        {
                            Id = Guid.NewGuid(),
                            PsCardItemTransferId = psCardItemTransfer.Id,
                            PsCardItemExtnId = psCardItemExtnId,
                            InsertedBy = cri.InsertedBy,
                            InsertedDt = cri.InsertedDt,
                            UpdatedBy = cri.UpdatedBy,
                            UpdatedDt = cri.UpdatedDt
                        };
                        _db.PsCardItemTransferItems.Add(psCardItemTransferItem);
                        _db.Entry(psCardItemTransferItem).State = EntityState.Added;
                        await _db.SaveChangesAsync();
                    }

                    // update newly created records only
                    var psCardItemExtns = _db.PsCardItemExtns.Where(w => w.PsCardItemId == psCardItem.Id);
                    var qty = psCardItemExtns.Count();
                    psCardItem.Qty = qty;
                    psCardItem.QtyBal = qty;
                    psCardItem.Amount = psCardItem.UnitCost * qty;
                    _db.PsCardItems.Attach(psCardItem);
                    _db.Entry(psCardItem).State = EntityState.Modified;

                    custodianReportUpload.PsCardTransferItemId = psCardItemTransferItem.Id;
                    _db.CustodianReportUploads.Attach(custodianReportUpload);
                    _db.Entry(custodianReportUpload).State = EntityState.Modified;

                    await _db.SaveChangesAsync();
                }

                /*
                 *  Unit Group (Set Items)
                 *  Set Items must have:
                 *      PO Number - (Group Key)                      
                 *      Unit
                 *      Unit Cost
                 *      Total Cost
                 *      <Group Description>
                 */

                if (custodianReportUpload.PsCardUnitGroupId == null)
                {

                }

                /*
                 *  PAR/ICS
                 */
                if (custodianReportUpload.IcsParId == null)
                {
                    if (!string.IsNullOrWhiteSpace(cri.IcsNo))
                    {
                        await CreateIcsParsAsync(cri, psCardItemExtnId, "I", cri.IcsNo, user, date);
                    }

                    if (!string.IsNullOrWhiteSpace(cri.ParNo))
                    {
                        await CreateIcsParsAsync(cri, psCardItemExtnId, "P", cri.ParNo, user, date);
                    }

                    if (!string.IsNullOrWhiteSpace(cri.MrNo))
                    {
                        await CreateIcsParsAsync(cri, psCardItemExtnId, "M", cri.MrNo, user, date);
                    }

                    if (!string.IsNullOrWhiteSpace(cri.AreNo))
                    {
                        await CreateIcsParsAsync(cri, psCardItemExtnId, "A", cri.AreNo, user, date);
                    }

                    if (!string.IsNullOrWhiteSpace(cri.RpcPpeNo))
                    {
                        await CreateIcsParsAsync(cri, psCardItemExtnId, "R", cri.RpcPpeNo, user, date);
                    }
                }

                foreach (var criIssuance in cri.CustodianReportItemIssuances)
                {
                    await CreateIcsParsAsync(cri, psCardItemExtnId, criIssuance.RefType, criIssuance.RefNo, user, date);
                }

                await UploadPhotosAsync(custodianReport, user, date);
            }

            return new CustodianReport();
            //}
        });

        public ValueTask<CustodianReport> UploadBldg(int? forYear, Guid? deptId, int? accountGroup, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            //using (var ctx = _contextFactory.CreateContext())
            //{
            var custodianReport = await _db.CustodianReports.Include(i => i.CustodianReportItems).FirstOrDefaultAsync(f => f.AsOf.Value.Year == forYear && f.DeptId == deptId && f.AccountGroup == accountGroup);
            if (custodianReport == null)
            {
                throw new InvalidValueException("No selected records to upload.");
            }

            var reportId = custodianReport.Id;
            var custodianReportItems = await _db.CustodianReportBldgItems.AsNoTracking()
                .Include(i => i.ItemCode.ItemType)
                .Include(i => i.CustodianReportUpload)
                .Where(w => w.ReportId == reportId
                    && w.PostedBy != "" && w.PostedBy != null // must be posted
                    && w.Fund != null && w.Fund != "" && w.PsNo != null && w.PsNo != "") // must be with valid fields
                .ToListAsync();

            foreach (var cri in custodianReportItems)
            {
                CustodianReportUpload custodianReportUpload = cri.CustodianReportUpload;
                PsCard psCard = null;
                PsCardItem psCardItem = null;
                PsCardItemTransfer psCardItemTransfer = null;
                Guid psCardId = Guid.NewGuid();
                Guid psCardItemId = Guid.NewGuid();
                if (custodianReportUpload == null)
                {
                    psCard = await _db.PsCards.AsNoTracking()
                        .Include(i => i.AllField).FirstOrDefaultAsync(f => f.PsNo == cri.PsNo && f.Fund == cri.Fund);
                }
                else
                {
                    psCard = await _db.PsCards.AsNoTracking()
                        .Include(i => i.AllField)
                        .FirstOrDefaultAsync(f => f.Id == cri.CustodianReportUpload.PsCardId);
                }

                if (psCard == null)
                {
                    var subAccountCode = await _db.Database.SqlQuery<string>("Select Code From dbo.fn_SubAccountTab({0})", cri.ItemCode.Code).FirstOrDefaultAsync();
                    psCard = new PsCard()
                    {
                        Id = psCardId,
                        ItemCodeId = cri.ItemCodeId,
                        SubAccountCode = subAccountCode,
                        Fund = cri.Fund,
                        Description = "Please see attachment.",
                        PsNo = cri.PsNo,
                        FromDonation = cri.FromDonation,
                        CardCategory = cri.ItemCode.ItemType.Category == "S" ? "S" : "P",
                        InsertedBy = cri.InsertedBy,
                        InsertedDt = cri.InsertedDt,
                        UpdatedBy = cri.UpdatedBy,
                        UpdatedDt = cri.UpdatedDt
                    };

                    var allField = new AllField()
                    {
                        Id = psCard.Id,
                        InsertedBy = cri.InsertedBy,
                        InsertedDt = cri.InsertedDt,
                        UpdatedBy = cri.UpdatedBy,
                        UpdatedDt = cri.UpdatedDt
                    };

                    psCard.AllField = allField;
                    _db.PsCards.Add(psCard);
                    _db.Entry(psCard).State = EntityState.Added;
                    await _db.SaveChangesAsync(); // need to save here so that this record will be included in the next findAsync
                }
                else
                {
                    psCardId = psCard.Id;
                }

                if (custodianReportUpload == null) // not from download
                {
                    // create CustodianReportUpload record based on uploaded records
                    custodianReportUpload = await CreateCustodianReportAsync(cri.Id, psCardId, user, date);

                    psCardItem = await _db.PsCardItems
                        .Include(i => i.PsCardItemTransfer.PsCardItemTransferItems)
                        .Include(i => i.PsCardItemExtns)
                        .FirstOrDefaultAsync(f => f.PsCardId == psCard.Id && f.PoNo == cri.PoNo && f.PoDate == cri.PoDate
                            && f.Description == cri.Description);
                }
                else
                {
                    psCardItem = await _db.PsCardItems
                        .Include(i => i.PsCardItemTransfer.PsCardItemTransferItems)
                        .Include(i => i.PsCardItemExtns)
                        .FirstOrDefaultAsync(f => f.Id == cri.CustodianReportUpload.PsCardItemId);
                }

                if (psCardItem == null)
                {
                    psCardItem = new PsCardItem()
                    {
                        Id = psCardItemId,
                        GroupId = psCardItemId,
                        PsCardId = psCard.Id,
                        PoDate = cri.PoDate,
                        PoNo = cri.PoNo,
                        Qty = 1,
                        QtyBal = 1,
                        //Unit = cri.Unit,
                        UnitCost = cri.AcqCost,
                        Amount = cri.AcqCost,
                        TUnitCost = cri.AcqCost,
                        GTotalCost = cri.AcqCost,
                        Remarks = cri.Remarks,
                        DeptId = cri.DeptId,
                        DeptDisplay = cri.Department,
                        LocationId = cri.LocationId,
                        Description = cri.Description,
                        //OtherDesc = cri.OtherDesc,
                        //InvDist = cri.InvDist,
                        PrevPsNo = cri.PropNo,
                        InsertedBy = cri.InsertedBy,
                        InsertedDt = cri.InsertedDt,
                        UpdatedBy = cri.UpdatedBy,
                        UpdatedDt = cri.UpdatedDt,
                        PostedBy = cri.PostedBy,
                        PostedDt = cri.PostedDt
                    };

                    _db.PsCardItems.Add(psCardItem);
                    _db.Entry(psCardItem).State = EntityState.Added;
                }
                else
                {
                    psCardItemId = psCardItem.Id;
                    // Update PsCardItem Fields                                  
                    psCardItem.UpdatedBy = cri.UpdatedBy;
                    psCardItem.UpdatedDt = cri.UpdatedDt;

                    _db.PsCardItems.Attach(psCardItem);
                    _db.Entry(psCardItem).State = EntityState.Modified;
                }

                if (custodianReportUpload.PsCardItemId == null)
                {
                    custodianReportUpload.PsCardItemId = psCardItemId;
                    _db.CustodianReportUploads.Attach(custodianReportUpload);
                    _db.Entry(custodianReportUpload).State = EntityState.Modified;
                }

                await _db.SaveChangesAsync();

                // Update PsCardItemExnts
                Guid psCardItemExtnId = Guid.NewGuid();
                PsCardItemExtnBuilding psCardItemExtn = null;
                if (cri.CustodianReportUpload == null)
                {
                    psCardItemExtn = await _db.PsCardItemExtns.OfType<PsCardItemExtnBuilding>()
                        .FirstOrDefaultAsync(f => f.PsCardItem.PsCard.PsNo == cri.PsNo
                            && f.PsCardItem.PoNo == cri.PoNo
                            && f.PhaseNo == cri.PhaseNo && f.ProjectName == cri.ProjectName
                            && f.BuildingItem == cri.BldgItem);
                }
                else
                {
                    psCardItemExtn = await _db.PsCardItemExtns.OfType<PsCardItemExtnBuilding>()
                        .FirstOrDefaultAsync(f => f.Id == cri.CustodianReportUpload.PsCardItemExtnId);
                }

                if (psCardItemExtn == null)
                {
                    psCardItemExtn = new PsCardItemExtnBuilding()
                    {
                        Id = psCardItemExtnId,
                        GroupId = psCardItemExtnId,
                        PsCardItemId = psCardItem.Id,
                        CustItemNo = cri.CustodianItemNo,
                        LocationId = cri.LocationId,
                        PropNo = cri.PropNo,
                        SeriesNo = cri.SeriesNo,
                        Remarks = cri.Remarks,
                        Annex = cri.Annex,
                        //OldPropNo = cri.ol,
                        //UpcomingOfficer = cri.,
                        SubLocation = cri.SubLocation,
                        Condition = cri.Condition,
                        //AddCost = cri.,
                        AcqCost = cri.AcqCost,
                        OldAmount = cri.OldAmount,
                        AcqDate = cri.AcqDate,
                        Address = cri.Address,
                        BuildingItem = cri.BldgItem,
                        ProjectName = cri.ProjectName,
                        BuildingType = cri.BuildingType,
                        Area = cri.Area,
                        AppraisedValue = cri.AppraiseValue,
                        TotalAmount = cri.TotalAmount,
                        PhaseNo = cri.PhaseNo,
                        PhaseAmountMooe = cri.PhaseAmountMooe,
                        PhaseAmountCo = cri.PhaseAmountCo,
                        StartDate = cri.StartDate,
                        TargetDate = cri.TargetDate,
                        PercentComplete = cri.PercentComplete,
                        CompletionDate = cri.CompletionDate,
                        Status = cri.Status,
                        Longitude = cri.Longitude,
                        Latitude = cri.Latitude
                    };

                    _db.PsCardItemExtns.Add(psCardItemExtn);
                    _db.Entry(psCardItemExtn).State = EntityState.Added;
                }
                else
                {
                    psCardItemExtnId = psCardItemExtn.Id;
                    psCardItemExtn.CustItemNo = cri.CustodianItemNo;
                    psCardItemExtn.SeriesNo = cri.SeriesNo;
                    psCardItemExtn.LocationId = cri.LocationId;
                    psCardItemExtn.Annex = cri.Annex;
                    psCardItemExtn.Remarks = cri.Remarks;
                    psCardItemExtn.UpdatedBy = cri.UpdatedBy;
                    psCardItemExtn.UpdatedDt = cri.UpdatedDt;

                    _db.PsCardItemExtns.Attach(psCardItemExtn);
                    _db.Entry(psCardItemExtn).State = EntityState.Modified;
                }

                if (custodianReportUpload.PsCardItemExtnId == null)
                {
                    custodianReportUpload.PsCardItemExtnId = psCardItemExtnId;
                    _db.CustodianReportUploads.Attach(custodianReportUpload);
                    _db.Entry(custodianReportUpload).State = EntityState.Modified;
                }


                await _db.SaveChangesAsync();

                // Update PsCarditemTransfers
                Guid psCardItemTransferId = Guid.NewGuid();
                if (cri.CustodianReportUpload == null)
                {
                    psCardItemTransfer = await _db.PsCardItemTransfers
                        .FirstOrDefaultAsync(f => f.PsCardItemId == psCardItem.Id && f.LocationId == cri.LocationId);
                }
                else
                {
                    psCardItemTransfer = await _db.PsCardItemTransfers.FirstOrDefaultAsync(f => f.Id == cri.CustodianReportUpload.PsCardTransferId);
                }

                if (psCardItemTransfer == null)
                {
                    psCardItemTransfer = new PsCardItemTransfer()
                    {
                        Id = Guid.NewGuid(),
                        PsCardItemId = psCardItem.Id,
                        Qty = 1,
                        QtyBal = 1,
                        TransDate = cri.PoDate,
                        LocationId = cri.LocationId,
                        Amount = cri.TotalAmount,
                        InsertedBy = cri.InsertedBy,
                        InsertedDt = cri.InsertedDt,
                        UpdatedBy = cri.UpdatedBy,
                        UpdatedDt = cri.UpdatedDt
                    };

                    _db.PsCardItemTransfers.Add(psCardItemTransfer);
                    _db.Entry(psCardItemTransfer).State = EntityState.Added;
                }
                else
                {
                    psCardItemTransferId = psCardItemTransfer.Id;
                    _db.PsCardItemTransfers.Attach(psCardItemTransfer);
                    _db.Entry(psCardItemTransfer).State = EntityState.Modified;
                }

                if (custodianReportUpload.PsCardTransferId == null)
                {
                    custodianReportUpload.PsCardTransferId = psCardItemTransferId;
                    _db.CustodianReportUploads.Attach(custodianReportUpload);
                    _db.Entry(custodianReportUpload).State = EntityState.Modified;
                }

                await _db.SaveChangesAsync();

                if (custodianReportUpload.PsCardTransferItemId == null)
                {
                    var psCardItemTransferItem = await _db.PsCardItemTransferItems
                        .FirstOrDefaultAsync(f => f.PsCardItemTransferId == psCardItemTransfer.Id && f.PsCardItemExtnId == psCardItemExtnId);
                    if (psCardItemTransferItem == null)
                    {
                        psCardItemTransferItem = new PsCardItemTransferItem()
                        {
                            Id = Guid.NewGuid(),
                            PsCardItemTransferId = psCardItemTransfer.Id,
                            PsCardItemExtnId = psCardItemExtnId,
                            InsertedBy = cri.InsertedBy,
                            InsertedDt = cri.InsertedDt,
                            UpdatedBy = cri.UpdatedBy,
                            UpdatedDt = cri.UpdatedDt
                        };
                        _db.PsCardItemTransferItems.Add(psCardItemTransferItem);
                        _db.Entry(psCardItemTransferItem).State = EntityState.Added;
                        await _db.SaveChangesAsync();
                    }
                }

                // do this if applicable for bldg
                //if (cri.CustodianReportUpload == null) // update newly created records only
                //{
                //    var psCardItemExtns = _db.PsCardItemExtns.Where(w => w.PsCardItemId == psCardItem.Id);
                //    var qty = psCardItemExtns.Count();
                //    psCardItem.Qty = qty;
                //    psCardItem.QtyBal = qty;
                //    psCardItem.Amount = psCardItem.UnitCost * qty;
                //    _db.PsCardItems.Attach(psCardItem);
                //    _db.Entry(psCardItem).State = EntityState.Modified;
                //    await _db.SaveChangesAsync();
                //}

                await UploadPhotosAsync(custodianReport, user, date);
            }

            return new CustodianReport();
            //}
        });

        public ValueTask<CustodianReport> UploadLand(int? forYear, Guid? deptId, int? accountGroup, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            //using (var ctx = _contextFactory.CreateContext())
            //{
            var custodianReport = await _db.CustodianReports.Include(i => i.CustodianReportItems).FirstOrDefaultAsync(f => f.AsOf.Value.Year == forYear && f.DeptId == deptId && f.AccountGroup == accountGroup);
            if (custodianReport == null)
            {
                throw new InvalidValueException("No selected records to upload.");
            }

            var reportId = custodianReport.Id;
            var custodianReportItems = await _db.CustodianReportLandItems.AsNoTracking()
                .Include(i => i.CustodianReport)
                .Include(i => i.ItemCode.ItemType)
                .Include(i => i.CustodianReportUpload)
                .Where(w => w.ReportId == reportId
                    && w.PostedBy != "" && w.PostedBy != null // must be posted
                    && w.Fund != null && w.Fund != "" && w.PsNo != null && w.PsNo != "") // must be with valid fields
                .ToListAsync();

            foreach (var cri in custodianReportItems)
            {
                CustodianReportUpload custodianReportUpload = cri.CustodianReportUpload;
                PsCard psCard = null;
                PsCardItem psCardItem = null;
                PsCardItemTransfer psCardItemTransfer = null;
                Guid psCardId = Guid.NewGuid();
                Guid psCardItemId = Guid.NewGuid();
                if (custodianReportUpload == null)
                {
                    psCard = await _db.PsCards
                        .Include(i => i.AllField).FirstOrDefaultAsync(f => f.PsNo == cri.PsNo && f.Fund == cri.Fund);
                }
                else
                {
                    psCard = await _db.PsCards
                        .Include(i => i.AllField)
                        .FirstOrDefaultAsync(f => f.Id == cri.CustodianReportUpload.PsCardId);
                }

                if (psCard == null)
                {
                    var subAccountCode = await _db.Database.SqlQuery<string>("Select Code From dbo.fn_SubAccountTab({0})", cri.ItemCode.Code).FirstOrDefaultAsync();
                    psCard = new PsCard()
                    {
                        Id = psCardId,
                        ItemCodeId = cri.ItemCodeId,
                        SubAccountCode = subAccountCode,
                        Fund = cri.Fund,
                        Description = "Please see attachment.",
                        PsNo = cri.PsNo,
                        FromDonation = cri.FromDonation,
                        CardCategory = cri.ItemCode.ItemType.Category == "S" ? "S" : "P",
                        InsertedBy = cri.InsertedBy,
                        InsertedDt = cri.InsertedDt,
                        UpdatedBy = cri.UpdatedBy,
                        UpdatedDt = cri.UpdatedDt
                    };

                    var allField = new AllField()
                    {
                        Id = psCard.Id,
                        InsertedBy = cri.InsertedBy,
                        InsertedDt = cri.InsertedDt,
                        UpdatedBy = cri.UpdatedBy,
                        UpdatedDt = cri.UpdatedDt
                    };

                    psCard.AllField = allField;
                    _db.PsCards.Add(psCard);
                    _db.Entry(psCard).State = EntityState.Added;
                    await _db.SaveChangesAsync(); // need to save here so that this record will be included in the next findAsync
                }
                else
                {
                    psCardId = psCard.Id;
                }

                if (custodianReportUpload == null) // not from download
                {
                    // create CustodianReportUpload record based on uploaded records
                    custodianReportUpload = await CreateCustodianReportAsync(cri.Id, psCardId, user, date);

                    psCardItem = await _db.PsCardItems
                        .Include(i => i.PsCardItemTransfer.PsCardItemTransferItems)
                        .Include(i => i.PsCardItemExtns)
                        .FirstOrDefaultAsync(f => f.PsCardId == psCard.Id && f.PsCardItemExtns.Any(a => a.PropNo == cri.PropNo)
                            && f.Description == cri.Description);
                }
                else
                {
                    psCardItem = await _db.PsCardItems
                        .Include(i => i.PsCardItemTransfer.PsCardItemTransferItems)
                        .Include(i => i.PsCardItemExtns)
                        .FirstOrDefaultAsync(f => f.Id == cri.CustodianReportUpload.PsCardItemId);
                }

                if (psCardItem == null)
                {
                    psCardItem = new PsCardItem()
                    {
                        Id = psCardItemId,
                        GroupId = psCardItemId,
                        PsCardId = psCard.Id,
                        PoDate = cri.AcqDate,
                        //PoNo = cri.PoNo,
                        Qty = cri.Area,
                        QtyBal = cri.Area,
                        //Unit = cri.Unit,
                        UnitCost = cri.PricePerSqm,
                        Amount = cri.AreaXPrice,
                        TUnitCost = cri.PricePerSqm,
                        GTotalCost = cri.AreaXPrice,
                        Remarks = cri.Remarks,
                        DeptId = cri.CustodianReport.DeptId,
                        DeptDisplay = cri.CustodianReport.Department,
                        LocationId = cri.LocationId,
                        Description = cri.Description,
                        //OtherDesc = cri.OtherDesc,
                        //InvDist = cri.InvDist,
                        PrevPsNo = cri.PropNo,
                        InsertedBy = cri.InsertedBy,
                        InsertedDt = cri.InsertedDt,
                        UpdatedBy = cri.UpdatedBy,
                        UpdatedDt = cri.UpdatedDt,
                        PostedBy = cri.PostedBy,
                        PostedDt = cri.PostedDt
                    };

                    _db.PsCardItems.Add(psCardItem);
                    _db.Entry(psCardItem).State = EntityState.Added;
                }
                else
                {
                    psCardItemId = psCardItem.Id;
                    // Update PsCardItem Fields                                  
                    psCardItem.UpdatedBy = cri.UpdatedBy;
                    psCardItem.UpdatedDt = cri.UpdatedDt;

                    _db.PsCardItems.Attach(psCardItem);
                    _db.Entry(psCardItem).State = EntityState.Modified;
                }

                if (custodianReportUpload.PsCardItemId == null)
                {
                    custodianReportUpload.PsCardItemId = psCardItemId;
                    _db.CustodianReportUploads.Attach(custodianReportUpload);
                    _db.Entry(custodianReportUpload).State = EntityState.Modified;
                }

                await _db.SaveChangesAsync();

                // Update PsCardItemExnts
                Guid psCardItemExtnId = Guid.NewGuid();
                PsCardItemExtnLand psCardItemExtn = null;
                if (cri.CustodianReportUpload == null)
                {
                    psCardItemExtn = await _db.PsCardItemExtns.OfType<PsCardItemExtnLand>()
                        .FirstOrDefaultAsync(f => f.PsCardItem.PsCard.PsNo == cri.PsNo
                            && f.PIN == cri.PIN);
                }
                else
                {
                    psCardItemExtn = await _db.PsCardItemExtns.OfType<PsCardItemExtnLand>()
                        .FirstOrDefaultAsync(f => f.Id == cri.CustodianReportUpload.PsCardItemExtnId);
                }

                Mode psCardItemExtnMode = Mode.EDIT;
                if (psCardItemExtn == null)
                {
                    psCardItemExtn = new PsCardItemExtnLand()
                    {
                        Id = psCardItemExtnId,
                        GroupId = psCardItemExtnId,
                        PsCardItemId = psCardItem.Id,
                        InsertedBy = user,
                        InsertedDt = date
                    };
                    psCardItemExtnMode = Mode.ADD;
                }

                psCardItemExtn.CustItemNo = cri.CustodianItemNo;
                psCardItemExtn.LocationId = cri.LocationId;
                psCardItemExtn.PropNo = cri.PropNo;
                psCardItemExtn.SeriesNo = cri.SeriesNo;
                psCardItemExtn.Remarks = cri.Remarks;
                psCardItemExtn.Annex = cri.Annex;
                psCardItemExtn.SubLocation = cri.SubLocation;
                psCardItemExtn.Condition = cri.Condition;
                psCardItemExtn.AcqCost = cri.AcqCost;
                psCardItemExtn.OldAmount = cri.OldAmount;
                psCardItemExtn.AcqDate = cri.AcqDate;
                psCardItemExtn.PIN = cri.PIN;
                psCardItemExtn.Address = cri.Address;
                psCardItemExtn.LandMarks = cri.LandMarks;
                psCardItemExtn.MarketValue = cri.MarketValue;
                psCardItemExtn.PricePerSqm = cri.PricePerSqm;
                psCardItemExtn.AreaXPrice = cri.AreaXPrice;
                psCardItemExtn.Vendor = cri.Vendor;
                psCardItemExtn.Representative = cri.Representative;
                psCardItemExtn.TctNo = cri.TctNo;
                psCardItemExtn.OldTctNo = cri.OldTctNo;
                psCardItemExtn.DRPNo = cri.DRPNo;
                psCardItemExtn.DRPDate = cri.DRPDate;
                psCardItemExtn.OldDRPNo = cri.OldDRPNo;
                psCardItemExtn.OldDRPDate = cri.OldDRPDate;
                psCardItemExtn.CGT = cri.CGT;
                psCardItemExtn.CGTCompromise = cri.CGTCompromise;
                psCardItemExtn.CGTCompromiseCap = cri.CGTCompromiseCap;
                psCardItemExtn.CGTInteest = cri.CGTInterest;
                psCardItemExtn.CGTInterestCap = cri.CGTInterestCap;
                psCardItemExtn.CGTSurcharge = cri.CGTSurcharge;
                psCardItemExtn.CGTSurchargeCap = cri.CGTSurchargeCap;
                psCardItemExtn.CGTTransferTax = cri.CGTTransferTax;
                psCardItemExtn.CGTTransferTaxCap = cri.CGTTransferTaxCap;
                psCardItemExtn.DST = cri.DST;
                psCardItemExtn.DSTCompromise = cri.DSTCompromise;
                psCardItemExtn.DSTCompromiseCap = cri.DSTCompromiseCap;
                psCardItemExtn.DSTInterest = cri.DSTInterest;
                psCardItemExtn.DSTInterestCap = cri.DSTInterestCap;
                psCardItemExtn.DSTSurcharge = cri.DSTSurcharge;
                psCardItemExtn.DSTSurchargeCap = cri.DSTSurchargeCap;
                psCardItemExtn.DSTTransferTax = cri.DSTTransferTax;
                psCardItemExtn.DSTTransferTaxCap = cri.DSTTransferTaxCap;
                psCardItemExtn.TransferTax = cri.TransferTax;
                psCardItemExtn.Surcharge = cri.Surcharge;
                psCardItemExtn.Interest = cri.Interest;
                psCardItemExtn.TransferTaxCap = cri.TransferTaxCap;
                psCardItemExtn.SurchargeCap = cri.SurchargeCap;
                psCardItemExtn.InterestCap = cri.InterestCap;
                psCardItemExtn.ConfirmationFee = cri.ConfirmationFee;
                psCardItemExtn.TransferRegsFee = cri.TransferRegsFee;
                psCardItemExtn.RealPropertyTax = cri.RealPropertyFee;
                psCardItemExtn.ConfirmationFeeCap = cri.ConfirmationFeeCap;
                psCardItemExtn.TransferRegsFeeCap = cri.TransferRegsFeeCap;
                psCardItemExtn.RealPropertyTaxCap = cri.RealPropertyFeeCap;
                psCardItemExtn.VAT = cri.VAT;
                psCardItemExtn.EstateTax = cri.EstateFee;
                psCardItemExtn.Titling = cri.Titling;
                psCardItemExtn.CerttificationFee = cri.CertificationFee;
                psCardItemExtn.Relocation = cri.Relocation;
                psCardItemExtn.Surveying = cri.Surveying;
                psCardItemExtn.IncidentalExpenses = cri.IncidentalExpenses;
                psCardItemExtn.VATCap = cri.VATCap;
                psCardItemExtn.EstateTaxCap = cri.EstateFeeCap;
                psCardItemExtn.TitlingCap = cri.TitlingCap;
                psCardItemExtn.CertificationFeeCap = cri.CertificationFeeCap;
                psCardItemExtn.RelocationCap = cri.RelocationCap;
                psCardItemExtn.SurveyingCap = cri.SurveyingCap;
                psCardItemExtn.IncidentalExpensesCap = cri.IncidentalExpensesCap;
                psCardItemExtn.CapitalOutlayOrExpense = cri.CapitalOutlayOrExpense;
                psCardItemExtn.UpdatedBy = cri.UpdatedBy;
                psCardItemExtn.UpdatedDt = cri.UpdatedDt;

                if (psCardItemExtnMode == Mode.ADD)
                {
                    _db.PsCardItemExtns.Add(psCardItemExtn);
                    _db.Entry(psCardItemExtn).State = EntityState.Added;
                }
                else
                {
                    _db.PsCardItemExtns.Attach(psCardItemExtn);
                    _db.Entry(psCardItemExtn).State = EntityState.Modified;
                }

                if (custodianReportUpload.PsCardItemExtnId == null)
                {
                    custodianReportUpload.PsCardItemExtnId = psCardItemExtnId;
                    _db.CustodianReportUploads.Attach(custodianReportUpload);
                    _db.Entry(custodianReportUpload).State = EntityState.Modified;
                }

                await _db.SaveChangesAsync();

                // Update PsCarditemTransfers
                Guid psCardItemTransferId = Guid.NewGuid();
                if (custodianReportUpload.PsCardTransferItemId == null)
                {
                    psCardItemTransfer = await _db.PsCardItemTransfers
                        .FirstOrDefaultAsync(f => f.PsCardItemId == psCardItem.Id && f.LocationId == cri.LocationId);
                }
                else
                {
                    psCardItemTransfer = await _db.PsCardItemTransfers.FirstOrDefaultAsync(f => f.Id == cri.CustodianReportUpload.PsCardTransferId);
                }

                if (psCardItemTransfer == null)
                {
                    psCardItemTransfer = new PsCardItemTransfer()
                    {
                        Id = psCardItemTransferId,
                        PsCardItemId = psCardItem.Id,
                        Qty = cri.Area,
                        QtyBal = cri.Area,
                        TransDate = cri.AcqDate,
                        LocationId = cri.LocationId,
                        Amount = cri.AcqCost,
                        InsertedBy = cri.InsertedBy,
                        InsertedDt = cri.InsertedDt,
                        UpdatedBy = cri.UpdatedBy,
                        UpdatedDt = cri.UpdatedDt
                    };

                    _db.PsCardItemTransfers.Add(psCardItemTransfer);
                    _db.Entry(psCardItemTransfer).State = EntityState.Added;
                }
                else
                {
                    _db.PsCardItemTransfers.Attach(psCardItemTransfer);
                    _db.Entry(psCardItemTransfer).State = EntityState.Modified;
                }

                if (custodianReportUpload.PsCardTransferId == null)
                {
                    custodianReportUpload.PsCardTransferId = psCardItemTransferId;
                    _db.CustodianReportUploads.Attach(custodianReportUpload);
                    _db.Entry(custodianReportUpload).State = EntityState.Modified;
                }

                await _db.SaveChangesAsync();

                if (custodianReportUpload.PsCardTransferItemId == null)
                {
                    var psCardItemTransferItem = await _db.PsCardItemTransferItems
                        .FirstOrDefaultAsync(f => f.PsCardItemTransferId == psCardItemTransfer.Id && f.PsCardItemExtnId == psCardItemExtnId);
                    if (psCardItemTransferItem == null)
                    {
                        psCardItemTransferItem = new PsCardItemTransferItem()
                        {
                            Id = Guid.NewGuid(),
                            PsCardItemTransferId = psCardItemTransfer.Id,
                            PsCardItemExtnId = psCardItemExtnId,
                            InsertedBy = cri.InsertedBy,
                            InsertedDt = cri.InsertedDt,
                            UpdatedBy = cri.UpdatedBy,
                            UpdatedDt = cri.UpdatedDt
                        };
                        _db.PsCardItemTransferItems.Add(psCardItemTransferItem);
                        _db.Entry(psCardItemTransferItem).State = EntityState.Added;
                        await _db.SaveChangesAsync();
                    }
                }

                // do this if applicable for bldg
                //if (cri.CustodianReportUpload == null) // update newly created records only
                //{
                //    var psCardItemExtns = _db.PsCardItemExtns.Where(w => w.PsCardItemId == psCardItem.Id);
                //    var qty = psCardItemExtns.Count();
                //    psCardItem.Qty = qty;
                //    psCardItem.QtyBal = qty;
                //    psCardItem.Amount = psCardItem.UnitCost * qty;
                //    _db.PsCardItems.Attach(psCardItem);
                //    _db.Entry(psCardItem).State = EntityState.Modified;
                //    await _db.SaveChangesAsync();
                //}

                await UploadPhotosAsync(custodianReport, user, date);
            }

            return new CustodianReport();
            //}
        });

        /*
         * Download record based on itemcode's account Group
         * ICs
         * Records to be downloaded:
         * > Records not yet downloaded (Not in CustodianReportUploads > Which contains both Uploads/Download records)
         */
        public ValueTask<CustodianReport> Download(int? forYear, Guid? deptId, int? accountGroup, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            Mode mode = Mode.ADD;
            //using (var ctx = _contextFactory.CreateContext())
            //{
            var custodianReport = await _db.CustodianReports.Include(i => i.CustodianReportItems).FirstOrDefaultAsync(f => f.AsOf.Value.Year == forYear && f.DeptId == deptId && f.AccountGroup == accountGroup);
            if (custodianReport != null)
            {
                if (custodianReport.PostedBy != "" && custodianReport.PostedBy != null)
                {
                    throw new RecordLockedException($"The record for year {forYear} is already locked!");
                }
                mode = Mode.EDIT;
            }

            var psCardItemTransfers = await _db.PsCardItemTransfers.AsNoTracking()
                .Include(i => i.PsCardItem.PsCard)
                .Include(i => i.PsCardItemTransferItems)
                .Where(w => w.TransDate.Value.Year <= forYear
                    && ((w.LocationId == null && w.PsCardItem.DeptId == deptId) || w.LocationId == deptId)
                    && w.PsCardItem.PostedBy != "" && w.PsCardItem.PostedBy != null && w.QtyBal > 0
                    && w.PsCardItemTransferItems.Any()
                ).ToListAsync();
            if (!psCardItemTransfers.Any())
            {
                throw new NotFoundException($"No records where found for year {forYear}.");
            }

            IQueryable<ItemCodeVM> itemCodes = null;
            if (accountGroup == (int?)AccountGroup.PPE)
            {
                itemCodes = _itemCodeService.GetCustodianItemPpe(string.Empty);
            }
            else if (accountGroup == (int?)AccountGroup.SUPPLIES)
            {
                itemCodes = _itemCodeService.GetCustodianItemStocks(string.Empty);
            }
            else if (accountGroup == (int?)AccountGroup.VEHICLE)
            {
                itemCodes = _itemCodeService.GetCustodianItemVehicle(string.Empty);
            }

            psCardItemTransfers = psCardItemTransfers.Where(w => itemCodes.Any(a => w.PsCardItem.PsCard.ItemCodeId == a.Id)).ToList();
            if (!psCardItemTransfers.Any())
            {
                throw new InvalidValueException($"No records where found for year {forYear}.");
            }

            List<CustodianReportItem> custodianReportItems = null;
            if (custodianReport == null)
            {
                var departmemnt = (await _db.Codextns.FirstOrDefaultAsync(f => f.Id == deptId)).Description;
                custodianReport = new CustodianReport()
                {
                    Id = Guid.NewGuid(),
                    AsOf = new DateTime((int)forYear, 12, 31),
                    DeptId = deptId,
                    Department = departmemnt,
                    AccountGroup = accountGroup,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };
            }
            else
            {
                custodianReportItems = await _db.CustodianReportItems.AsNoTracking()
                    .Include(i => i.CustodianReportUpload)
                    .Where(w => w.ReportId == custodianReport.Id).ToListAsync();
            }

            // Ensure it's not null            
            custodianReportItems = custodianReportItems ?? new List<CustodianReportItem>();

            // do not include records that were already downloaded
            psCardItemTransfers = psCardItemTransfers
                .Where(w => !w.PsCardItemTransferItems.Any(a =>
                    custodianReportItems.Any(b =>
                        b.CustodianReportUpload != null &&
                        b.CustodianReportUpload.PsCardItemExtnId == a.PsCardItemExtnId))).ToList();

            foreach (var psCardItemTransfer in psCardItemTransfers)
            {
                CustodianReportItem custodianReportItem = null;
                // get items not yet transfered
                var transferItems = psCardItemTransfer.PsCardItemTransferItems.Where(w =>
                    !_db.PsCardItemTransferItems.Any(a => a.PsCardItemTransfer.ParentId == w.PsCardItemTransferId
                    && a.PsCardItemExtnId == w.PsCardItemExtnId));

                foreach (var transferItem in transferItems)
                {
                    var psCardItemExtn = await _db.PsCardItemExtns.AsNoTracking()
                            .Include(i => i.PsCardItem.PsCard.ItemCode.ItemType)
                            .FirstOrDefaultAsync(f => f.Id == transferItem.PsCardItemExtnId);
                    var psNo = psCardItemExtn.PsCardItem.PsCard.PsNo;
                    var description = psCardItemExtn.PsCardItem.Description;

                    if (accountGroup == (int?)AccountGroup.PPE || accountGroup == (int?)AccountGroup.SUPPLIES)
                    {
                        var psCardItemExtnOther = await _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().AsNoTracking()
                            .Include(i => i.PsCardItem.PsCard.ItemCode.ItemType)
                            .FirstOrDefaultAsync(f => f.Id == transferItem.PsCardItemExtnId);
                        if (psCardItemExtnOther != null)
                        {
                            if (custodianReportItems.Any(a => a.PsNo == psNo && a.Description == description && a.SerialNo == psCardItemExtnOther.SerialNo))
                            {
                                continue;
                            }
                            custodianReportItem = new CustodianReportItem();
                            custodianReportItem.ItemSerialNo = psCardItemExtnOther.SerialNo;
                        }
                    }
                    else if (accountGroup == (int?)AccountGroup.VEHICLE)
                    {
                        var psCardItemExtnVehicle = await _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>().AsNoTracking()
                            .Include(i => i.PsCardItem.PsCard.ItemCode.ItemType)
                            .FirstOrDefaultAsync(f => f.Id == transferItem.PsCardItemExtnId);
                        if (psCardItemExtnVehicle != null)
                        {
                            if (custodianReportItems.Any(a => a.PsNo == psNo && a.Description == description
                                && a.YearModel == psCardItemExtnVehicle.YearModel
                                && a.PlateNo == psCardItemExtnVehicle.PlateNo
                                && a.ConductionNo == psCardItemExtnVehicle.ConductionNo
                                && a.EngineNo == psCardItemExtnVehicle.EngineNo
                                && a.ChasisNo == psCardItemExtnVehicle.ChasisNo))
                            {
                                continue;
                            }
                            custodianReportItem = new CustodianReportItem();
                            custodianReportItem.YearModel = psCardItemExtnVehicle.YearModel;
                            custodianReportItem.PlateNo = psCardItemExtnVehicle.PlateNo;
                            custodianReportItem.BodyNo = psCardItemExtnVehicle.BodyNo;
                            custodianReportItem.MVFileNo = psCardItemExtnVehicle.MVFileNo;
                            custodianReportItem.EngineNo = psCardItemExtnVehicle.EngineNo;
                            custodianReportItem.ChasisNo = psCardItemExtnVehicle.ChasisNo;
                            custodianReportItem.CRN = psCardItemExtnVehicle.CRN;
                            custodianReportItem.CRDate = psCardItemExtnVehicle.CRDate;
                            custodianReportItem.OrNo = psCardItemExtnVehicle.OrNo;
                            custodianReportItem.OrDate = psCardItemExtnVehicle.OrDate;
                            custodianReportItem.InsPolicyNo = psCardItemExtnVehicle.InsPolicyNo;
                            custodianReportItem.ConductionNo = psCardItemExtnVehicle.ConductionNo;
                        }
                    }

                    Codextn location = null;
                    if (psCardItemExtn.PsCardItem.LocationId != null)
                    {
                        location = await _db.Codextns.FirstOrDefaultAsync(f => f.Id == psCardItemExtn.PsCardItem.LocationId);
                    }
                    var account = itemCodes.FirstOrDefault(f => f.Id == psCardItemExtn.PsCardItem.PsCard.ItemCodeId);
                    var allField = await _db.AllFields.FirstOrDefaultAsync(f => f.Id == psCardItemExtn.PsCardItem.PsCard.Id);
                    var icsParItems = await _db.IcsParItems.AsNoTracking()
                        .Include(i => i.IcsPar.IcsParUnitGroups)
                        .Where(w => w.PsCardItemExtnId == transferItem.PsCardItemExtnId && w.IcsPar.RefDate.Value.Year <= forYear)
                        .OrderByDescending(o => o.IcsPar.RefDate).ToListAsync();
                    var pars = icsParItems.Where(w => w.IcsPar.RefType == "P");
                    var icss = icsParItems.Where(w => w.IcsPar.RefType == "I");
                    var mrs = icsParItems.Where(w => w.IcsPar.RefType == "M");
                    var ares = icsParItems.Where(w => w.IcsPar.RefType == "A");
                    var rpcs = icsParItems.Where(w => w.IcsPar.RefType == "R");
                    var par = pars.FirstOrDefault();
                    var ics = icss.FirstOrDefault();
                    var mr = mrs.FirstOrDefault();
                    var are = ares.FirstOrDefault();
                    var rpc = rpcs.FirstOrDefault();
                    var reportItemId = Guid.NewGuid();

                    custodianReportItem.Id = reportItemId;
                    custodianReportItem.ReportId = custodianReport.Id;
                    custodianReportItem.Fund = psCardItemExtn.PsCardItem.PsCard.Fund;
                    custodianReportItem.CustodianItemNo = psCardItemExtn.CustItemNo;
                    custodianReportItem.SeriesNo = psCardItemExtn.SeriesNo;
                    custodianReportItem.FromDonation = psCardItemExtn.PsCardItem.PsCard.FromDonation;
                    custodianReportItem.InvDist = psCardItemExtn.PsCardItem.InvDist;
                    custodianReportItem.Account = account.Account;
                    custodianReportItem.ItemCodeId = psCardItemExtn.PsCardItem.PsCard.ItemCodeId;
                    custodianReportItem.SubAccount = account.MainDesc;
                    custodianReportItem.Article = account.Article;
                    custodianReportItem.PoNo = psCardItemExtn.PsCardItem.PoNo;
                    custodianReportItem.PoDate = psCardItemExtn.PsCardItem.PoDate;
                    custodianReportItem.AirNo = psCardItemExtn.PsCardItem.AirNo;
                    custodianReportItem.AirDate = psCardItemExtn.PsCardItem.AirDate;
                    custodianReportItem.AcqDate = psCardItemExtn.AcqDate;
                    custodianReportItem.UnitCost = psCardItemExtn.PsCardItem.UnitCost;
                    custodianReportItem.Unit = psCardItemExtn.PsCardItem.Unit;
                    custodianReportItem.SetLotNo = psCardItemExtn.PsCardItem.SetLotNo;
                    custodianReportItem.DeptId = psCardItemExtn.PsCardItem.DeptId;
                    custodianReportItem.Department = psCardItemExtn.PsCardItem.DeptDisplay;
                    custodianReportItem.LocationId = psCardItemExtn.PsCardItem.LocationId;
                    custodianReportItem.LocationCode = location?.Code;
                    custodianReportItem.Location = location?.Description;
                    custodianReportItem.SubLocation = psCardItemExtn.SubLocation;
                    custodianReportItem.Qty = 1;
                    //TransferIn, --> not used
                    //QtyBalance,  --> not used
                    custodianReportItem.TotalCost = psCardItemExtn.AcqCost;
                    custodianReportItem.OldAmount = psCardItemExtn.OldAmount;
                    //OldPsNo = psCardItemExtn.OldPropNo, --> not used
                    custodianReportItem.PsNo = psNo;
                    custodianReportItem.Description = description;
                    custodianReportItem.Brand = allField.Brand;
                    custodianReportItem.Model_ = allField.Model_;
                    custodianReportItem.Dimension = allField.Dimension;
                    custodianReportItem.Size = allField.Size;
                    custodianReportItem.Weight = allField.Weight;
                    custodianReportItem.Materials = allField.Materials;
                    custodianReportItem.Capacity = allField.Capacity;
                    custodianReportItem.Color = allField.Color;
                    custodianReportItem.GenericName = allField.GenericName;
                    custodianReportItem.DosageStrength = allField.DosageStrength;
                    custodianReportItem.DosageForm = allField.DosageForm;
                    custodianReportItem.DosageVolume = allField.DosageVolume;
                    custodianReportItem.Multipliers = allField.Multipliers;
                    //SerialNo, --> Not used
                    custodianReportItem.OldPropNo = psCardItemExtn.OldPropNo;
                    custodianReportItem.PropNo = psCardItemExtn.PropNo;
                    //---- Vehicles ------------
                    //YearModel,
                    //PlateNo,
                    //BodyNo,
                    //MVFileNo,
                    //EngineNo,
                    //ChasisNo,
                    //CRN,
                    //CRDate,
                    //OrNo,
                    //OrDate,
                    //InsPolicyNo,
                    //ConductionNo,
                    //ItemSerialNo, --> Serial No.
                    custodianReportItem.OtherDesc = psCardItemExtn.PsCardItem.OtherDesc;
                    custodianReportItem.OtherQty = psCardItemExtn.PsCardItem.OtherQty;
                    custodianReportItem.Condition = psCardItemExtn.Condition;
                    custodianReportItem.Remarks = psCardItemExtn.Remarks;
                    custodianReportItem.ParNo = par?.IcsPar.RefNo;
                    custodianReportItem.ParIssuedTo = par?.IssuedTo;
                    custodianReportItem.AccountableOfficer = par?.IcsPar.ReceivedBy;
                    custodianReportItem.AreNo = are?.IcsPar.RefNo;
                    custodianReportItem.AreIssuedTo = are?.IssuedTo;
                    custodianReportItem.AreOfficer = are?.IcsPar.ReceivedBy;
                    custodianReportItem.MrNo = mr?.IcsPar.RefNo;
                    custodianReportItem.MrIssuedTo = mr?.IssuedTo;
                    custodianReportItem.MrOfficer = mr?.IcsPar.ReceivedBy;
                    custodianReportItem.IcsNo = ics?.IcsPar.RefNo;
                    custodianReportItem.IcsIssuedTo = ics?.IssuedTo;
                    custodianReportItem.IcsOfficer = ics?.IcsPar.ReceivedBy;
                    custodianReportItem.RpcPpeNo = rpc?.IcsPar.RefNo;
                    custodianReportItem.RpcPpeIssuedTo = rpc?.IssuedTo;
                    custodianReportItem.RpcPpeOfficer = rpc?.IcsPar.ReceivedBy;
                    custodianReportItem.UpcomingPar = par != null ? psCardItemExtn.UpcomingOfficer : null;
                    custodianReportItem.UpcomingIcs = par == null ? psCardItemExtn.UpcomingOfficer : null;
                    //Type,
                    custodianReportItem.Annex = string.IsNullOrWhiteSpace(psCardItemExtn.Annex) ? "A" : psCardItemExtn.Annex;
                    custodianReportItem.InsertedBy = psCardItemExtn.InsertedBy;
                    custodianReportItem.InsertedDt = psCardItemExtn.InsertedDt;
                    custodianReportItem.UpdatedBy = psCardItemExtn.UpdatedBy;
                    custodianReportItem.UpdatedDt = psCardItemExtn.UpdatedDt;
                    //custodianReportItem.PostedBy = psCardItemExtn.PsCardItem.PostedBy;
                    //custodianReportItem.PostedDt = psCardItemExtn.PsCardItem.PostedDt;
                    custodianReportItem.SetLotAmount = psCardItemExtn.PsCardItem.SetLotAmount;
                    custodianReportItem.SetLotRemarks = psCardItemExtn.PsCardItem.SetLotRemarks;
                    custodianReportItem.PriceRate = psCardItemExtn.PsCardItem.PriceRate;
                    custodianReportItem.ProRatedCost = psCardItemExtn.PsCardItem.ProRatedCost;
                    custodianReportItem.AddCost = psCardItemExtn.AddCost;
                    //TUnitCost,
                    //GTotalCost,
                    //UploadedBy,
                    //UploadedDt,

                    var icsParItem = icsParItems.FirstOrDefault();
                    IcsParUnitGroupDescriptionItem icsParItmUnitGroupDescriptionItem = null;
                    if (icsParItem != null)
                    {
                        icsParItmUnitGroupDescriptionItem = await _db.IcsParUnitGroupDescriptionItems.AsNoTracking()
                            .Include(i => i.IcsPartUnitGroupDescription.IcsParUnitGroup)
                            .FirstOrDefaultAsync(f => f.IcsParItemId == icsParItem.Id);
                    }
                    var psCardItemUnitGroupDescriptionItem = await _db.PsCardItemUnitGroupDescriptionItems.AsNoTracking()
                        .Include(i => i.PsCardItemUnitGroupDescription.PsCardItemUnitGroup)
                        .FirstOrDefaultAsync(f => f.PsCardItemId == psCardItemExtn.PsCardItemId);

                    var custodianReportUpload = new CustodianReportUpload()
                    {
                        Id = reportItemId,
                        PsCardId = psCardItemExtn.PsCardItem.PsCardId,
                        PsCardItemId = psCardItemExtn.PsCardItemId,
                        PsCardTransferId = transferItem.PsCardItemTransferId,
                        PsCardTransferItemId = transferItem.Id,
                        PsCardUnitGroupId = psCardItemUnitGroupDescriptionItem?.PsCardItemUnitGroupDescription.UnitGroupId,
                        PsCardUnitGroupDescriptionId = psCardItemUnitGroupDescriptionItem?.UnitGroupDescriptionId,
                        PsCardUnitGroupDescriptionItemId = psCardItemUnitGroupDescriptionItem?.Id,
                        PsCardItemExtnId = psCardItemExtn.Id,
                        IcsParId = icsParItem?.IcsParId,
                        IcsParItemId = icsParItem?.Id,
                        IcsParUnitGroupId = icsParItmUnitGroupDescriptionItem?.IcsPartUnitGroupDescription.UnitGroupId,
                        IcsParUnitGroupDescriptionId = icsParItmUnitGroupDescriptionItem?.UnitGroupDescriptionId,
                        IcsParUnitGroupDescriptionItemId = icsParItmUnitGroupDescriptionItem?.Id,
                        DownloadedBy = user,
                        DownloadedDt = date
                    };

                    custodianReportItem.CustodianReportUpload = custodianReportUpload;

                    var icsParIssuances = icsParItems.Skip(1).ToList();
                    foreach (var icsParIssuance in icsParIssuances)
                    {
                        var custodianReportItemIssuance = new CustodianReportItemIssuance()
                        {
                            Id = Guid.NewGuid(),
                            ReportItemId = reportItemId,
                            RefType = icsParIssuance.IcsPar.RefType,
                            RefNo = icsParIssuance.IcsPar.RefNo,
                            IssuedTo = icsParIssuance.IssuedTo,
                            AccountableOfficer = icsParIssuance.IcsPar.ReceivedBy,
                            InsertedBy = icsParIssuance.InsertedBy,
                            InsertedDt = icsParIssuance.InsertedDt,
                            UpdatedBy = icsParIssuance.UpdatedBy,
                            UpdatedDt = icsParIssuance.UpdatedDt
                        };
                        custodianReportItem.CustodianReportItemIssuances.Add(custodianReportItemIssuance);
                    }

                    custodianReport.CustodianReportItems.Add(custodianReportItem);
                }

                if (mode == Mode.ADD)
                {
                    _db.CustodianReports.Add(custodianReport);
                }
                else
                {
                    _db.CustodianReports.Attach(custodianReport);
                }

                await _db.SaveChangesAsync();
                await DownloadPhotosAsync(custodianReport, user, date);
            }

            return new CustodianReport();
            //}
        });

        private async ValueTask DownloadPhotosAsync(CustodianReport custodianReport, string user, DateTime date)
        {
            // include photos
            var directoryPath = $"{_uploadService.GetDirectoryPath()}CUSTODIAN/";
            foreach (var item in custodianReport.CustodianReportItems)
            {
                var imageId = item?.CustodianReportUpload?.PsCardItemExtnId;
                if (imageId != null)
                {
                    await _uploadService.CopyAsync(imageId, directoryPath, user, date);
                }
            }
        }

        public ValueTask<CustodianReport> DownloadBldg(int? forYear, Guid? deptId, int? accountGroup, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            Mode mode = Mode.ADD;
            //using (var ctx = _contextFactory.CreateContext())
            //{
            var custodianReport = await _db.CustodianReports.Include(i => i.CustodianReportItems).FirstOrDefaultAsync(f => f.AsOf.Value.Year == forYear && f.DeptId == deptId && f.AccountGroup == accountGroup);
            if (custodianReport != null)
            {
                if (custodianReport.PostedBy != "" && custodianReport.PostedBy != null)
                {
                    throw new RecordLockedException($"The record for year {forYear} is already locked!");
                }
                mode = Mode.EDIT;
            }

            var psCardItemTransfers = await _db.PsCardItemTransfers.AsNoTracking()
                .Include(i => i.PsCardItem.PsCard)
                .Include(i => i.PsCardItemTransferItems).AsNoTracking()
                .Where(w => w.TransDate.Value.Year <= forYear
                    && ((w.LocationId == null && w.PsCardItem.DeptId == deptId) || w.LocationId == deptId)
                    && w.PsCardItem.PostedBy != "" && w.PsCardItem.PostedBy != null && w.QtyBal > 0
                    && w.PsCardItemTransferItems.Any()
                ).ToListAsync();
            if (!psCardItemTransfers.Any())
            {
                throw new RecordNotFoundException($"No records where found for year {forYear}.");
            }

            IQueryable<ItemCodeVM> itemCodes = _itemCodeService.GetCustodianItemBldg(string.Empty);
            psCardItemTransfers = psCardItemTransfers.Where(w => itemCodes.Any(a => w.PsCardItem.PsCard.ItemCodeId == a.Id)).ToList();
            if (!psCardItemTransfers.Any())
            {
                throw new InvalidValueException($"No records where found for year {forYear}.");
            }

            List<CustodianReportBldgItem> custodianReportItems = null;
            if (custodianReport == null)
            {
                var departmemnt = (await _db.Codextns.FirstOrDefaultAsync(f => f.Id == deptId)).Description;
                custodianReport = new CustodianReport()
                {
                    Id = Guid.NewGuid(),
                    AsOf = new DateTime((int)forYear, 12, 31),
                    DeptId = deptId,
                    Department = departmemnt,
                    AccountGroup = accountGroup,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };
            }
            else
            {
                custodianReportItems = await _db.CustodianReportBldgItems.AsNoTracking()
                    .Include(i => i.CustodianReportUpload)
                    .Where(w => w.ReportId == custodianReport.Id).ToListAsync();
            }

            // Ensure it's not null            
            custodianReportItems = custodianReportItems ?? new List<CustodianReportBldgItem>();

            // do not include records that were already downloaded
            psCardItemTransfers = psCardItemTransfers
                .Where(w => !w.PsCardItemTransferItems.Any(a =>
                    custodianReportItems.Any(b =>
                        b.CustodianReportUpload != null &&
                        b.CustodianReportUpload.PsCardItemExtnId == a.PsCardItemExtnId))).ToList();

            foreach (var psCardItemTransfer in psCardItemTransfers)
            {
                CustodianReportBldgItem custodianReportItem = null;
                // get items not yet transfered
                var transferItems = psCardItemTransfer.PsCardItemTransferItems.Where(w =>
                    !_db.PsCardItemTransferItems.Any(a => a.PsCardItemTransfer.ParentId == w.PsCardItemTransferId
                    && a.PsCardItemExtnId == w.PsCardItemExtnId));

                foreach (var transferItem in transferItems)
                {
                    var psCardItemExtn = await _db.PsCardItemExtns.OfType<PsCardItemExtnBuilding>().AsNoTracking()
                        .Include(i => i.PsCardItem.PsCard.ItemCode.ItemType)
                        .FirstOrDefaultAsync(f => f.Id == transferItem.PsCardItemExtnId);
                    if (psCardItemExtn == null)
                    {
                        continue;
                    }

                    var psNo = psCardItemExtn.PsCardItem.PsCard.PsNo;
                    var description = psCardItemExtn.PsCardItem.Description;
                    if (custodianReportItems.Any(a => a.PsNo == psNo && a.Description == description && a.CustodianItemNo == psCardItemExtn.CustItemNo))
                    {
                        continue;
                    }

                    Codextn location = null;
                    if (psCardItemExtn.PsCardItem.LocationId != null)
                    {
                        location = await _db.Codextns.FirstOrDefaultAsync(f => f.Id == psCardItemExtn.PsCardItem.LocationId);
                    }
                    var account = itemCodes.FirstOrDefault(f => f.Id == psCardItemExtn.PsCardItem.PsCard.ItemCodeId);
                    var allField = await _db.AllFields.FirstOrDefaultAsync(f => f.Id == psCardItemExtn.PsCardItem.PsCard.Id);
                    var reportItemId = Guid.NewGuid();

                    custodianReportItem = new CustodianReportBldgItem();
                    custodianReportItem.Id = reportItemId;
                    custodianReportItem.ReportId = custodianReport.Id;
                    custodianReportItem.Fund = psCardItemExtn.PsCardItem.PsCard.Fund;
                    custodianReportItem.CustodianItemNo = psCardItemExtn.CustItemNo;
                    custodianReportItem.SeriesNo = psCardItemExtn.SeriesNo;
                    custodianReportItem.FromDonation = psCardItemExtn.PsCardItem.PsCard.FromDonation;
                    custodianReportItem.Account = account.Account;
                    custodianReportItem.ItemCodeId = psCardItemExtn.PsCardItem.PsCard.ItemCodeId;
                    custodianReportItem.SubAccount = account.MainDesc;
                    custodianReportItem.Article = account.Article;
                    custodianReportItem.Description = psCardItemExtn.PsCardItem.Description;
                    custodianReportItem.BldgItem = psCardItemExtn.BuildingItem;
                    custodianReportItem.PoNo = psCardItemExtn.PsCardItem.PoNo;
                    custodianReportItem.PoDate = psCardItemExtn.PsCardItem.PoDate;
                    custodianReportItem.AcqCost = psCardItemExtn.AcqCost;
                    custodianReportItem.AcqDate = psCardItemExtn.AcqDate;
                    custodianReportItem.DeptId = psCardItemExtn.PsCardItem.DeptId;
                    custodianReportItem.Department = psCardItemExtn.PsCardItem.DeptDisplay;
                    custodianReportItem.LocationId = psCardItemExtn.PsCardItem.LocationId;
                    custodianReportItem.LocationCode = location?.Code;
                    custodianReportItem.Location = location?.Description;
                    custodianReportItem.SubLocation = psCardItemExtn.SubLocation;
                    custodianReportItem.AcqDate = psCardItemExtn.AcqDate;
                    custodianReportItem.Address = psCardItemExtn.Address;
                    custodianReportItem.ProjectName = psCardItemExtn.ProjectName;
                    custodianReportItem.BuildingType = psCardItemExtn.BuildingType;
                    custodianReportItem.Area = psCardItemExtn.Area;
                    custodianReportItem.AppraiseValue = psCardItemExtn.AppraisedValue;
                    custodianReportItem.TotalAmount = psCardItemExtn.TotalAmount;
                    custodianReportItem.OldAmount = psCardItemExtn.OldAmount;
                    custodianReportItem.PsNo = psNo;
                    custodianReportItem.PropNo = psCardItemExtn.PropNo;
                    custodianReportItem.PhaseNo = psCardItemExtn.PhaseNo;
                    custodianReportItem.PhaseAmountMooe = psCardItemExtn.PhaseAmountMooe;
                    custodianReportItem.PhaseAmountCo = psCardItemExtn.PhaseAmountCo;
                    custodianReportItem.StartDate = psCardItemExtn.StartDate;
                    custodianReportItem.TargetDate = psCardItemExtn.TargetDate;
                    custodianReportItem.PercentComplete = psCardItemExtn.PercentComplete;
                    custodianReportItem.CompletionDate = psCardItemExtn.CompletionDate;
                    custodianReportItem.Status = psCardItemExtn.Status;
                    custodianReportItem.Condition = psCardItemExtn.Condition;
                    custodianReportItem.Remarks = psCardItemExtn.Remarks;
                    custodianReportItem.Longitude = psCardItemExtn.Longitude;
                    custodianReportItem.Latitude = psCardItemExtn.Latitude;
                    custodianReportItem.Annex = string.IsNullOrWhiteSpace(psCardItemExtn.Annex) ? "A" : psCardItemExtn.Annex;
                    custodianReportItem.InsertedBy = psCardItemExtn.InsertedBy;
                    custodianReportItem.InsertedDt = psCardItemExtn.InsertedDt;
                    custodianReportItem.UpdatedBy = psCardItemExtn.UpdatedBy;
                    custodianReportItem.UpdatedDt = psCardItemExtn.UpdatedDt;

                    var custodianReportUpload = new CustodianReportUpload()
                    {
                        Id = reportItemId,
                        PsCardId = psCardItemExtn.PsCardItem.PsCardId,
                        PsCardItemId = psCardItemExtn.PsCardItemId,
                        PsCardTransferId = transferItem.PsCardItemTransferId,
                        PsCardTransferItemId = transferItem.Id,
                        //PsCardUnitGroupId = psCardItemUnitGroupDescriptionItem?.PsCardItemUnitGroupDescription.UnitGroupId,
                        //PsCardUnitGroupDescriptionId = psCardItemUnitGroupDescriptionItem?.UnitGroupDescriptionId,
                        //PsCardUnitGroupDescriptionItemId = psCardItemUnitGroupDescriptionItem?.Id,
                        PsCardItemExtnId = psCardItemExtn.Id,
                        //IcsParId = icsParItem?.IcsParId,
                        //IcsParItemId = icsParItem?.Id,
                        //IcsParUnitGroupId = icsParItmUnitGroupDescriptionItem?.IcsPartUnitGroupDescription.UnitGroupId,
                        //IcsParUnitGroupDescriptionId = icsParItmUnitGroupDescriptionItem?.UnitGroupDescriptionId,
                        //IcsParUnitGroupDescriptionItemId = icsParItmUnitGroupDescriptionItem?.Id,
                        DownloadedBy = user,
                        DownloadedDt = date
                    };

                    custodianReportItem.CustodianReportUpload = custodianReportUpload;

                    //var icsParIssuances = icsParItems.Skip(1).ToList();
                    //foreach (var icsParIssuance in icsParIssuances)
                    //{
                    //    var custodianReportItemIssuance = new CustodianReportItemIssuance()
                    //    {
                    //        Id = Guid.NewGuid(),
                    //        ReportItemId = reportItemId,
                    //        RefType = icsParIssuance.IcsPar.RefType,
                    //        RefNo = icsParIssuance.IcsPar.RefNo,
                    //        IssuedTo = icsParIssuance.IssuedTo,
                    //        AccountableOfficer = icsParIssuance.IcsPar.ReceivedBy,
                    //        InsertedBy = icsParIssuance.InsertedBy,
                    //        InsertedDt = icsParIssuance.InsertedDt,
                    //        UpdatedBy = icsParIssuance.UpdatedBy,
                    //        UpdatedDt = icsParIssuance.UpdatedDt
                    //    };
                    //    custodianReportItem.CustodianReportItemIssuances.Add(custodianReportItemIssuance);
                    //}

                    custodianReport.CustodianReportBldgItems.Add(custodianReportItem);
                }

                if (mode == Mode.ADD)
                {
                    _db.CustodianReports.Add(custodianReport);
                }
                else
                {
                    _db.CustodianReports.Attach(custodianReport);
                }

                await _db.SaveChangesAsync();
                await DownloadPhotosAsync(custodianReport, user, date);
            }

            return new CustodianReport();
            //}
        });

        public ValueTask<CustodianReport> DownloadLand(int? forYear, Guid? deptId, int? accountGroup, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            Mode mode = Mode.ADD;
            //using (var ctx = _contextFactory.CreateContext())
            //{
            var custodianReport = await _db.CustodianReports.Include(i => i.CustodianReportItems).FirstOrDefaultAsync(f => f.AsOf.Value.Year == forYear && f.DeptId == deptId && f.AccountGroup == accountGroup);
            if (custodianReport != null)
            {
                if (custodianReport.PostedBy != "" && custodianReport.PostedBy != null)
                {
                    throw new RecordLockedException($"The record for year {forYear} is already locked!");
                }
                mode = Mode.EDIT;
            }

            var psCardItemTransfers = await _db.PsCardItemTransfers.AsNoTracking()
                .Include(i => i.PsCardItem.PsCard)
                .Include(i => i.PsCardItemTransferItems).AsNoTracking()
                .Where(w => w.TransDate.Value.Year <= forYear
                    && ((w.LocationId == null && w.PsCardItem.DeptId == deptId) || w.LocationId == deptId)
                    && w.PsCardItem.PostedBy != "" && w.PsCardItem.PostedBy != null && w.QtyBal > 0
                    && w.PsCardItemTransferItems.Any()
                ).ToListAsync();
            if (!psCardItemTransfers.Any())
            {
                throw new RecordNotFoundException($"No records where found for year {forYear}.");
            }

            IQueryable<ItemCodeVM> itemCodes = _itemCodeService.GetCustodianItemLand(string.Empty);
            psCardItemTransfers = psCardItemTransfers.Where(w => itemCodes.Any(a => w.PsCardItem.PsCard.ItemCodeId == a.Id)).ToList();
            if (!psCardItemTransfers.Any())
            {
                throw new InvalidValueException($"No records where found for year {forYear}.");
            }

            List<CustodianReportLandItem> custodianReportItems = null;
            if (custodianReport == null)
            {
                var departmemnt = (await _db.Codextns.FirstOrDefaultAsync(f => f.Id == deptId)).Description;
                custodianReport = new CustodianReport()
                {
                    Id = Guid.NewGuid(),
                    AsOf = new DateTime((int)forYear, 12, 31),
                    DeptId = deptId,
                    Department = departmemnt,
                    AccountGroup = accountGroup,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };
            }
            else
            {
                custodianReportItems = await _db.CustodianReportLandItems.AsNoTracking()
                    .Include(i => i.CustodianReportUpload)
                    .Where(w => w.ReportId == custodianReport.Id).ToListAsync();
            }

            // Ensure it's not null            
            custodianReportItems = custodianReportItems ?? new List<CustodianReportLandItem>();

            // do not include records that were already downloaded
            psCardItemTransfers = psCardItemTransfers
                .Where(w => !w.PsCardItemTransferItems.Any(a =>
                    custodianReportItems.Any(b =>
                        b.CustodianReportUpload != null &&
                        b.CustodianReportUpload.PsCardItemExtnId == a.PsCardItemExtnId))).ToList();

            foreach (var psCardItemTransfer in psCardItemTransfers)
            {
                CustodianReportLandItem custodianReportItem = null;
                // get items not yet transfered
                var transferItems = psCardItemTransfer.PsCardItemTransferItems.Where(w =>
                    !_db.PsCardItemTransferItems.Any(a => a.PsCardItemTransfer.ParentId == w.PsCardItemTransferId
                    && a.PsCardItemExtnId == w.PsCardItemExtnId));

                foreach (var transferItem in transferItems)
                {
                    var psCardItemExtn = await _db.PsCardItemExtns.OfType<PsCardItemExtnLand>().AsNoTracking()
                        .Include(i => i.PsCardItem.PsCard.ItemCode.ItemType)
                        .FirstOrDefaultAsync(f => f.Id == transferItem.PsCardItemExtnId);
                    var psNo = psCardItemExtn.PsCardItem.PsCard.PsNo;
                    var description = psCardItemExtn.PsCardItem.Description;

                    if (psCardItemExtn != null)
                    {
                        if (custodianReportItems.Any(a => a.PsNo == psNo && a.Description == description
                            && a.PIN == psCardItemExtn.PIN && a.TctNo == psCardItemExtn.TctNo))
                        {
                            continue;
                        }
                        custodianReportItem = new CustodianReportLandItem();
                    }

                    Codextn location = null;
                    if (psCardItemExtn.PsCardItem.LocationId != null)
                    {
                        location = await _db.Codextns.FirstOrDefaultAsync(f => f.Id == psCardItemExtn.PsCardItem.LocationId);
                    }
                    var account = itemCodes.FirstOrDefault(f => f.Id == psCardItemExtn.PsCardItem.PsCard.ItemCodeId);
                    var allField = await _db.AllFields.FirstOrDefaultAsync(f => f.Id == psCardItemExtn.PsCardItem.PsCard.Id);
                    var reportItemId = Guid.NewGuid();

                    custodianReportItem.Id = reportItemId;
                    custodianReportItem.ReportId = custodianReport.Id;
                    custodianReportItem.Fund = psCardItemExtn.PsCardItem.PsCard.Fund;
                    custodianReportItem.CustodianItemNo = psCardItemExtn.CustItemNo;
                    custodianReportItem.SeriesNo = psCardItemExtn.SeriesNo;
                    custodianReportItem.FromDonation = psCardItemExtn.PsCardItem.PsCard.FromDonation;
                    custodianReportItem.Account = account.Account;
                    custodianReportItem.ItemCodeId = psCardItemExtn.PsCardItem.PsCard.ItemCodeId;
                    custodianReportItem.SubAccount = account.MainDesc;
                    custodianReportItem.Article = account.Article;
                    custodianReportItem.LocationId = psCardItemExtn.PsCardItem.LocationId;
                    custodianReportItem.LocationCode = location?.Code;
                    custodianReportItem.Location = location?.Description;
                    custodianReportItem.Type = psCardItemExtn.PsCardItem.Type;
                    custodianReportItem.Condition = psCardItemExtn.Condition;
                    custodianReportItem.Description = psCardItemExtn.PsCardItem.Description;
                    custodianReportItem.SubLocation = psCardItemExtn.SubLocation;
                    custodianReportItem.PIN = psCardItemExtn.PIN;
                    custodianReportItem.Address = psCardItemExtn.Address;
                    custodianReportItem.LandMarks = psCardItemExtn.LandMarks;
                    custodianReportItem.Area = psCardItemExtn.PsCardItem.Qty;
                    custodianReportItem.Unit = psCardItemExtn.PsCardItem.Unit;
                    custodianReportItem.PricePerSqm = psCardItemExtn.PsCardItem.UnitCost;
                    custodianReportItem.AreaXPrice = psCardItemExtn.AreaXPrice;
                    custodianReportItem.MarketValue = psCardItemExtn.PsCardItem.Amount;
                    custodianReportItem.PsNo = psNo;
                    custodianReportItem.PropNo = psCardItemExtn.PropNo;
                    custodianReportItem.OldPropNo = psCardItemExtn.OldPropNo;
                    custodianReportItem.OldAmount = psCardItemExtn.OldAmount;
                    custodianReportItem.AcqCost = psCardItemExtn.AddCost;
                    custodianReportItem.AcqDate = psCardItemExtn.AcqDate;
                    custodianReportItem.Vendor = psCardItemExtn.Vendor;
                    custodianReportItem.Representative = psCardItemExtn.Representative;
                    custodianReportItem.TctNo = psCardItemExtn.TctNo;
                    custodianReportItem.DRPNo = psCardItemExtn.DRPNo;
                    custodianReportItem.DRPDate = psCardItemExtn.DRPDate;
                    custodianReportItem.OldDRPNo = psCardItemExtn.OldDRPNo;
                    custodianReportItem.OldDRPDate = psCardItemExtn.OldDRPDate;
                    custodianReportItem.Remarks = psCardItemExtn.Remarks;
                    custodianReportItem.CGT = psCardItemExtn.CGT;
                    custodianReportItem.CGTCompromise = psCardItemExtn.CGTCompromise;
                    custodianReportItem.CGTCompromiseCap = psCardItemExtn.CGTCompromiseCap;
                    custodianReportItem.CGTInterest = psCardItemExtn.CGTInteest;
                    custodianReportItem.CGTInterestCap = psCardItemExtn.CGTInterestCap;
                    custodianReportItem.CGTSurcharge = psCardItemExtn.CGTSurcharge;
                    custodianReportItem.CGTSurchargeCap = psCardItemExtn.CGTSurchargeCap;
                    custodianReportItem.CGTTransferTax = psCardItemExtn.CGTTransferTax;
                    custodianReportItem.CGTTransferTaxCap = psCardItemExtn.CGTTransferTaxCap;
                    custodianReportItem.DST = psCardItemExtn.DST;
                    custodianReportItem.DSTCompromise = psCardItemExtn.DSTCompromise;
                    custodianReportItem.DSTCompromiseCap = psCardItemExtn.DSTCompromiseCap;
                    custodianReportItem.DSTInterest = psCardItemExtn.DSTInterest;
                    custodianReportItem.DSTInterestCap = psCardItemExtn.DSTInterestCap;
                    custodianReportItem.DSTSurcharge = psCardItemExtn.DSTSurcharge;
                    custodianReportItem.DSTSurchargeCap = psCardItemExtn.DSTSurchargeCap;
                    custodianReportItem.DSTTransferTax = psCardItemExtn.DSTTransferTax;
                    custodianReportItem.DSTTransferTaxCap = psCardItemExtn.DSTTransferTaxCap;
                    custodianReportItem.TransferTax = psCardItemExtn.TransferTax;
                    custodianReportItem.Surcharge = psCardItemExtn.Surcharge;
                    custodianReportItem.Interest = psCardItemExtn.Interest;
                    custodianReportItem.TransferTaxCap = psCardItemExtn.TransferTaxCap;
                    custodianReportItem.SurchargeCap = psCardItemExtn.SurchargeCap;
                    custodianReportItem.InterestCap = psCardItemExtn.InterestCap;
                    custodianReportItem.ConfirmationFee = psCardItemExtn.ConfirmationFee;
                    custodianReportItem.TransferRegsFee = psCardItemExtn.TransferRegsFee;
                    custodianReportItem.RealPropertyFee = psCardItemExtn.RealPropertyTax;
                    custodianReportItem.ConfirmationFeeCap = psCardItemExtn.ConfirmationFeeCap;
                    custodianReportItem.TransferRegsFeeCap = psCardItemExtn.TransferRegsFeeCap;
                    custodianReportItem.RealPropertyFeeCap = psCardItemExtn.RealPropertyTaxCap;
                    custodianReportItem.VAT = psCardItemExtn.VAT;
                    custodianReportItem.EstateFee = psCardItemExtn.EstateTax;
                    custodianReportItem.Titling = psCardItemExtn.Titling;
                    custodianReportItem.CertificationFee = psCardItemExtn.CerttificationFee;
                    custodianReportItem.Relocation = psCardItemExtn.Relocation;
                    custodianReportItem.Surveying = psCardItemExtn.Surveying;
                    custodianReportItem.IncidentalExpenses = psCardItemExtn.IncidentalExpenses;
                    custodianReportItem.VATCap = psCardItemExtn.VATCap;
                    custodianReportItem.EstateFeeCap = psCardItemExtn.EstateTaxCap;
                    custodianReportItem.CertificationFeeCap = psCardItemExtn.CertificationFeeCap;
                    custodianReportItem.RelocationCap = psCardItemExtn.RelocationCap;
                    custodianReportItem.SurveyingCap = psCardItemExtn.SurveyingCap;
                    custodianReportItem.IncidentalExpensesCap = psCardItemExtn.IncidentalExpensesCap;
                    custodianReportItem.CapitalOutlayOrExpense = psCardItemExtn.CapitalOutlayOrExpense;
                    custodianReportItem.Annex = string.IsNullOrWhiteSpace(psCardItemExtn.Annex) ? "A" : psCardItemExtn.Annex;
                    custodianReportItem.InsertedBy = psCardItemExtn.InsertedBy;
                    custodianReportItem.InsertedDt = psCardItemExtn.InsertedDt;
                    custodianReportItem.UpdatedBy = psCardItemExtn.UpdatedBy;
                    custodianReportItem.UpdatedDt = psCardItemExtn.UpdatedDt;

                    var custodianReportUpload = new CustodianReportUpload()
                    {
                        Id = reportItemId,
                        PsCardId = psCardItemExtn.PsCardItem.PsCardId,
                        PsCardItemId = psCardItemExtn.PsCardItemId,
                        PsCardTransferId = transferItem.PsCardItemTransferId,
                        PsCardTransferItemId = transferItem.Id,
                        //PsCardUnitGroupId = psCardItemUnitGroupDescriptionItem?.PsCardItemUnitGroupDescription.UnitGroupId,
                        //PsCardUnitGroupDescriptionId = psCardItemUnitGroupDescriptionItem?.UnitGroupDescriptionId,
                        //PsCardUnitGroupDescriptionItemId = psCardItemUnitGroupDescriptionItem?.Id,
                        PsCardItemExtnId = psCardItemExtn.Id,
                        //IcsParId = icsParItem?.IcsParId,
                        //IcsParItemId = icsParItem?.Id,
                        //IcsParUnitGroupId = icsParItmUnitGroupDescriptionItem?.IcsPartUnitGroupDescription.UnitGroupId,
                        //IcsParUnitGroupDescriptionId = icsParItmUnitGroupDescriptionItem?.UnitGroupDescriptionId,
                        //IcsParUnitGroupDescriptionItemId = icsParItmUnitGroupDescriptionItem?.Id,
                        DownloadedBy = user,
                        DownloadedDt = date
                    };

                    custodianReportItem.CustodianReportUpload = custodianReportUpload;
                    custodianReport.CustodianReportLandItems.Add(custodianReportItem);
                }

                if (mode == Mode.ADD)
                {
                    _db.CustodianReports.Add(custodianReport);
                }
                else
                {
                    _db.CustodianReports.Attach(custodianReport);
                }

                await _db.SaveChangesAsync();
                await DownloadPhotosAsync(custodianReport, user, date);
            }

            return new CustodianReport();
            //}
        });

        public MemoryStream ProcessExcelFileSummary(int? forYear, Guid? deptId, Guid? locationId, DateTime? asOf, DateTime? insertedAsOf, string templateFilePath)
        {
            using (XLWorkbook wb = new XLWorkbook(templateFilePath))
            {
                int row = 3;
                //string groupName = "";
                //string account = "";
                decimal? tAnnexA = 0;
                decimal? tAnnexB = 0;
                decimal? tAnnexC = 0;
                decimal? tAnnexX = 0;
                decimal? gTotalCost = 0;
                var ws = wb.Worksheet(1);
                var reportItems = _db.Database.SqlQuery<CustodianReportSummaryVM>("Exec CustodianReport_GetSummary {0}, {1}, {2}, {3}", forYear, deptId, locationId, asOf, insertedAsOf).AsQueryable();
                var accountGroups = reportItems
                    .GroupBy(g => new { g.OrderNo, g.GroupName })
                    .Select(s => new { s.Key.OrderNo, s.Key.GroupName }).OrderBy(o => o.OrderNo).ToList();

                foreach (var accountGroup in accountGroups)
                {
                    row++;
                    ws.Row(row).Cell(1).SetValue(accountGroup.GroupName).Style.Font.Bold = true;

                    var accounts = reportItems.Where(w => w.OrderNo == accountGroup.OrderNo)
                        .GroupBy(g => new { ItemNoIndex = g.ItemNoIndex.Substring(0, 2), g.Account })
                        .Select(s => new { s.Key.ItemNoIndex, s.Key.Account})
                        .OrderBy(o => o.ItemNoIndex)
                        .ToList();

                    foreach(var account in accounts)
                    {
                        row++;
                        ws.Row(row).Cell(2).SetValue(account.Account).Style.Font.Bold = true;

                        var subAccounts = reportItems.Where(w => w.OrderNo == accountGroup.OrderNo && w.Account == account.Account)
                            .OrderBy(o => o.ItemNoIndex).ToList();

                        foreach(var subAccount in subAccounts)
                        {
                            row++;
                            ws.Row(row).Cell(3).SetValue(subAccount.SubAccount1);
                            ws.Row(row).Cell(4).SetValue(subAccount.AnnexACost);
                            ws.Row(row).Cell(5).SetValue(subAccount.AnnexBCost);
                            ws.Row(row).Cell(6).SetValue(subAccount.AnnexCCost);
                            ws.Row(row).Cell(7).SetValue(subAccount.AnnexXCost);
                            ws.Row(row).Cell(9).SetValue(subAccount.TotalCost);

                            ws.Range($"C{row}:I{row}").Style.Border.BottomBorder = XLBorderStyleValues.Dotted;

                            tAnnexA += subAccount.AnnexACost;
                            tAnnexB += subAccount.AnnexBCost;
                            tAnnexC += subAccount.AnnexCCost;
                            tAnnexX += subAccount.AnnexXCost;
                            gTotalCost += subAccount.TotalCost;
                        }
                    }
                    row++;
                }

                row++;
                var annexACell = ws.Row(row).Cell(4);
                annexACell.SetValue(tAnnexA);
                annexACell.Style.Font.Bold = true;
                annexACell.Style.Border.TopBorder = XLBorderStyleValues.Hair;
                annexACell.Style.Border.BottomBorder = XLBorderStyleValues.Double;

                var annexBCell = ws.Row(row).Cell(5);
                annexBCell.SetValue(tAnnexB);
                annexBCell.Style.Font.Bold = true;
                annexBCell.Style.Border.TopBorder = XLBorderStyleValues.Hair;
                annexBCell.Style.Border.BottomBorder = XLBorderStyleValues.Double;

                var annexCCell = ws.Row(row).Cell(6);
                annexCCell.SetValue(tAnnexC);
                annexCCell.Style.Font.Bold = true;
                annexCCell.Style.Border.TopBorder = XLBorderStyleValues.Hair;
                annexCCell.Style.Border.BottomBorder = XLBorderStyleValues.Double;

                var annexXCell = ws.Row(row).Cell(7);
                annexXCell.SetValue(tAnnexX);
                annexXCell.Style.Font.Bold = true;
                annexXCell.Style.Border.TopBorder = XLBorderStyleValues.Hair;
                annexXCell.Style.Border.BottomBorder = XLBorderStyleValues.Double;

                var gTotalCell = ws.Row(row).Cell(9);
                gTotalCell.SetValue(gTotalCost);
                gTotalCell.Style.Font.Bold = true;
                gTotalCell.Style.Border.TopBorder = XLBorderStyleValues.Hair;
                gTotalCell.Style.Border.BottomBorder = XLBorderStyleValues.Double;

                // Create a MemoryStream to save the output
                var memoryStream = new MemoryStream();
                wb.SaveAs(memoryStream);

                // Reset the stream position to the beginning before returning
                memoryStream.Position = 0;
                return memoryStream;
            }
        }
    }
}