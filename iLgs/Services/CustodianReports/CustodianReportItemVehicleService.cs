using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.AllFields;
using iLgs.Services.Codes;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace iLgs.Services.CustodianReports
{
    public interface ICustodianReportItemVehicleService : ICustodianReportItemService
    {
        new ValueTask<CustodianReportItemVehicleVM> GetByIdAsync(Guid id);
        IQueryable<CustodianReportItemVehicleVM> GetAll(Guid? reportId, string userName);
        IQueryable<CustodianReportItemVehicleVM> GetAllByDeptAcctGroup(int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, string userName, bool? isDemand, bool? isView);
        IQueryable<CustodianReportItemVehicleVM> GetAllByDeptAcctGroupItemCodeId(int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, string userName, bool? isDemand, Guid? itemCodeId, bool? isView);
        IQueryable<CustodianReportItemVehicleVM> GetAllByAcctGroup(int? forYear, int? accountGroup, string userName);
        ValueTask<CustodianReportItemVehicleVM> CreateAsync(CustodianReportItemVehicleVM model, string user, DateTime date);
        ValueTask<CustodianReportItemVehicleVM> UpdateAsync(CustodianReportItemVehicleVM model, string user, DateTime date);
        ValueTask<CustodianReportItemVehicleVM> DeleteAsync(CustodianReportItemVehicleVM model, string user, DateTime date);
    }

    public class CustodianReportItemVehicleService : CustodianReportItemService, ICustodianReportItemVehicleService
    {
        private readonly IExceptionService<CustodianReportItemVehicleVM> _xtraExceptionService;
        private readonly ICustodianReportItemVehicleValidator _validator;
        private readonly IAnnexDService _annexDService;

        public CustodianReportItemVehicleService(AppManEntities db) : base(db)
        {
            _xtraExceptionService = new ExceptionService<CustodianReportItemVehicleVM>();
            _validator = new CustodianReportItemVehicleValidator(_db);
            _annexDService = new AnnexDService(_db);
        }

        //public CustodianReportItemVehicleService(AppManEntities db,
        //    IAppManEntitiesFactory appManEntitiesFactory,
        //    IAllFieldService allFieldService,
        //    ICodextnService codextnService,
        //    ICreateAndLogExceptions exceptions,
        //    IExceptionService<CustodianReportItem> exceptionService,
        //    IUserService userService,
        //    IExceptionService<CustodianReportItemVehicleVM> xtraExceptionService,
        //    ICustodianReportItemVehicleValidator validator,
        //    IAnnexDService annexDService) : base(db, appManEntitiesFactory, allFieldService, codextnService, exceptions, exceptionService, userService)
        //{
        //    _xtraExceptionService = xtraExceptionService;
        //    _validator = validator;
        //    _annexDService = annexDService;
        //}

        private static Expression<Func<CustodianReportItem, CustodianReportItemVehicleVM>> CustodianReporVehicleItemProjection
        = s => new CustodianReportItemVehicleVM
        {
            Id = s.Id,
            MainDeptId = s.CustodianReport.DeptId,
            MainDeptName = s.CustodianReport.Department,
            AccountGroup = s.CustodianReport.AccountGroup,
            ReportId = s.ReportId,
            Fund = s.Fund,
            CustodianItemNo = s.CustodianItemNo,
            SeriesNo = s.SeriesNo,
            FromDonation = s.FromDonation,
            InvDist = s.InvDist,
            Account = s.Account,
            ItemCodeId = s.ItemCodeId,
            SubAccount = s.SubAccount,
            Article = s.Article,
            PoNo = s.PoNo,
            PoDate = s.PoDate,
            AirNo = s.AirNo,
            AirDate = s.AirDate,
            AcqDate = s.AcqDate,
            UnitCost = s.UnitCost,
            Unit = s.Unit,
            SetLotNo = s.SetLotNo,
            SetLotAmount = s.SetLotAmount,
            SetLotRemarks = s.SetLotRemarks,
            PriceRate = s.PriceRate,
            DeptId = s.DeptId,
            Department = s.Department,
            LocationId = s.LocationId,
            LocationCode = s.Codextn1.Code,
            Location = s.Department.Trim() + (s.Department == s.Codextn1.Description ? "" : "/" + s.Codextn1.Description.Trim()),            
            SubLocation = s.SubLocation,
            Qty = s.Qty,
            TransferIn = s.TransferIn,
            QtyBalance = s.QtyBalance,
            TotalCost = s.TotalCost,
            OldAmount = s.OldAmount,
            OldPsNo = s.OldPsNo,
            PsNo = s.PsNo,
            Description = s.Description,
            Brand = s.Brand,
            Model_ = s.Model_,
            Dimension = s.Dimension,
            Size = s.Size,
            Weight = s.Weight,
            Materials = s.Materials,
            Capacity = s.Capacity,
            Color = s.Color,
            GenericName = s.GenericName,
            DosageStrength = s.DosageStrength,
            DosageForm = s.DosageForm,
            DosageVolume = s.DosageVolume,
            Multipliers = s.Multipliers,
            SerialNo = s.SerialNo,
            OldPropNo = s.OldPropNo,
            PropNo = s.PropNo,
            PlateNo = s.PlateNo,
            BodyNo = s.BodyNo,
            MVFileNo = s.MVFileNo,
            EngineNo = s.EngineNo,
            ChasisNo = s.ChasisNo,
            CRN = s.CRN,
            CRDate = s.CRDate,
            OrNo = s.OrNo,
            OrDate = s.OrDate,
            InsPolicyNo = s.InsPolicyNo,
            YearModel = s.YearModel,
            ConductionNo = s.ConductionNo,
            ItemSerialNo = s.ItemSerialNo,
            OtherDesc = s.OtherDesc,
            OtherQty = s.OtherQty,
            Condition = s.Condition,
            Remarks = s.Remarks,
            ParNo = s.ParNo,
            ParIssuedTo = s.ParIssuedTo,
            AccountableOfficer = s.AccountableOfficer,
            IcsNo = s.IcsNo,
            IcsIssuedTo = s.IcsIssuedTo,
            IcsOfficer = s.IcsOfficer,
            AreNo = s.AreNo,
            AreIssuedTo = s.AreIssuedTo,
            AreOfficer = s.AreOfficer,
            MrNo = s.MrNo,
            MrIssuedTo = s.MrIssuedTo,
            MrOfficer = s.MrOfficer,
            RpcPpeNo = s.RpcPpeNo,
            RpcPpeIssuedTo = s.RpcPpeIssuedTo,
            RpcPpeOfficer = s.RpcPpeOfficer,
            UpcomingPar = s.UpcomingPar,
            UpcomingIcs = s.UpcomingIcs,
            Type = s.Type,
            Annex = s.Annex,
            InsertedBy = s.InsertedBy,
            InsertedDt = s.InsertedDt,
            PostedBy = s.PostedBy,
            PostedDt = s.PostedDt,
            ProRatedCost = s.ProRatedCost,
            ItemType_Code = s.ItemCode.ItemType.Code,
            Item_Code = s.ItemCode.Code,
            Category = s.ItemCode.ItemType.Category
        };

        public new ValueTask<CustodianReportItemVehicleVM> GetByIdAsync(Guid id) =>
        _xtraExceptionService.TryCatch(async () =>
        {
            var data = await _db.CustodianReportItems
                .Where(w => w.Id == id)
                .Select(CustodianReporVehicleItemProjection).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<CustodianReportItemVehicleVM> GetAll(Guid? reportId, string userName)
        {
            IQueryable<CustodianReportItemVehicleVM> data = null;
            if (_userService.IsUserNameAdmin(userName) || _annexDService.IsAny(userName))
            {
                data = _db.CustodianReportItems.AsNoTracking()
                .Where(w => w.ReportId == reportId)
                .Select(CustodianReporVehicleItemProjection);
                //data = _db.Database.SqlQuery<CustodianReportItemVehicleVM>("Exec CustodianReport_Vehicle {0}, {1}, {2}", reportId, null, null).AsQueryable();
            }
            else
            {
                data = _db.CustodianReportItems.AsNoTracking()
                .Where(w => w.ReportId == reportId && w.Annex != "D")
                .Select(CustodianReporVehicleItemProjection);
                //data = _db.Database.SqlQuery<CustodianReportItemVehicleVM>("Exec CustodianReport_Vehicle {0}, {1}, {2}", reportId, null, null).AsQueryable();
                //if (data.Any())
                //{
                //    data = data.Where(w => w.Annex != "D");
                //}
            }
            return data;
        }

        public IQueryable<CustodianReportItemVehicleVM> GetAllByDeptAcctGroup(int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, string userName, bool? isDemand, bool? isView)
        {
            IQueryable<CustodianReportItemVehicleVM> data = null;
            if (deptId != null)
            {
                var userId = _userService.GetByUserName(userName).Id;
                var userIsAdmin = _userService.IsUserNameAdmin(userName);
                if (isDemand == true || isView == true)
                {
                    userIsAdmin = true;
                }
                data = _db.Database.SqlQuery<CustodianReportItemVehicleVM>("Exec CustodianReport_GetItems {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}", forYear, deptId, sectionId, accountGroup, "", null, "", userIsAdmin, "", userId).AsQueryable();
                if (data.Any() && isDemand == true)
                {
                    data = data.Where(w => w.Annex == "C");
                }
                if (data.Any())
                {
                    data = data.AsNoTracking();
                }
            }

            return data ?? Enumerable.Empty<CustodianReportItemVehicleVM>().AsQueryable();
        }

        public IQueryable<CustodianReportItemVehicleVM> GetAllByDeptAcctGroupItemCodeId(int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, string userName, bool? isDemand, Guid? itemCodeId, bool? isView)
        {
            IQueryable<CustodianReportItemVehicleVM> data = null;
            if (deptId != null)
            {
                var userId = _userService.GetByUserName(userName).Id;
                var userIsAdmin = _userService.IsUserNameAdmin(userName);
                if (isDemand == true || isView == true)
                {
                    userIsAdmin = true;
                }
                data = _db.Database.SqlQuery<CustodianReportItemVehicleVM>("Exec CustodianReport_GetItems {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}", forYear, deptId, sectionId, accountGroup, "", null, "", userIsAdmin, "", userId).AsQueryable();
                if (data.Any() && isDemand == true)
                {
                    data = data.Where(w => w.Annex == "C");
                }
                if (data.Any())
                {
                    data = data.Where(w => w.ItemCodeId == itemCodeId).AsNoTracking();
                }
            }
            return data ?? Enumerable.Empty<CustodianReportItemVehicleVM>().AsQueryable();
        }

        public IQueryable<CustodianReportItemVehicleVM> GetAllByAcctGroup(int? forYear, int? accountGroup, string userName)
        {
            IQueryable<CustodianReportItemVehicleVM> data = null;
            var userId = _userService.GetByUserName(userName).Id;
            var userIsAdmin = _userService.IsUserNameAdmin(userName);
            data = _db.Database.SqlQuery<CustodianReportItemVehicleVM>("Exec CustodianReport_GetItems {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}", forYear, null, null, accountGroup, "", null, "", userIsAdmin, "", userId).AsQueryable();
            
            return data.AsNoTracking();
        }

        public ValueTask<CustodianReportItemVehicleVM> CreateAsync(CustodianReportItemVehicleVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            if (model != null)
            {
                model.AllField = SetAllField(model);
            }

            SetDefaultValues(model);

            await _validator.ValidateOnCreateAsync(model);
            await base.CreateAsync(model, user, date);
            return model;
        });

        public ValueTask<CustodianReportItemVehicleVM> UpdateAsync(CustodianReportItemVehicleVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            if (model != null)
            {
                model.AllField = SetAllField(model);
            }

            SetDefaultValues(model);

            await _validator.ValidateOnUpdateAsync(model);
            await base.UpdateAsync(model, user, date);
            return model;
        });

        public ValueTask<CustodianReportItemVehicleVM> DeleteAsync(CustodianReportItemVehicleVM model, string user, DateTime date) => _xtraExceptionService.TryCatch(async () =>
        {
            _validator.ValidateOnDelete(model);
            await base.DeleteAsync(model, user, date);
            return model;
        });

        private void SetDefaultValues(CustodianReportItemVehicleVM model)
        {
            model.InvDist = "I";
        }
    }
}