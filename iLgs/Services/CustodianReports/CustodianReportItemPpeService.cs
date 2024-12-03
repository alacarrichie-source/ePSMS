using iLgs.Models;
using iLgs.Services.Codes;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.CustodianReports
{
    public interface ICustodianReportItemPpeService : ICustodianReportItemService
    {
        new ValueTask<CustodianReportItemPpeVM> GetByIdAsync(Guid id);
        IQueryable<CustodianReportItemPpeVM> GetAll(Guid? reportId, string userName);
        IQueryable<CustodianReportItemPpeVM> GetAllByAcctGroup(int? accountGroup, string userName);
        IQueryable<CustodianReportItemPpeVM> GetAllByDeptAcctGroup(Guid? deptId, int? accountGroup, string userName);
        ValueTask<CustodianReportItemPpeVM> CreateAsync(CustodianReportItemPpeVM model, string user, DateTime date);
        ValueTask<CustodianReportItemPpeVM> UpdateAsync(CustodianReportItemPpeVM model, string user, DateTime date);
        ValueTask<CustodianReportItemPpeVM> DeleteAsync(CustodianReportItemPpeVM model, string user, DateTime date);
    }

    public class CustodianReportItemPpeService : CustodianReportItemService, ICustodianReportItemPpeService
    {
        private readonly IExceptionService<CustodianReportItemPpeVM> _exceptionService = new ExceptionService<CustodianReportItemPpeVM>();
        private readonly ICustodianReportItemPpeValidator _validator;
        private readonly IAnnexDService _annexDService;

        public CustodianReportItemPpeService(AppManEntities db) : base(db)
        {
            _validator = new CustodianReportItemPpeValidator(db);
            _annexDService = new AnnexDService(db);
        }

        private static Expression<Func<CustodianReportItem, CustodianReportItemPpeVM>> CustodianReporPpeItemProjection
        = s => new CustodianReportItemPpeVM
        {
            Id = s.Id,
            MainDeptId = s.CustodianReport.DeptId,
            MainDeptName = s.CustodianReport.Codextn.Description,
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
            DeptId = s.DeptId,
            Department = s.Department,
            LocationId = s.LocationId,
            LocationCode = s.LocationCode,
            Location = s.Location,
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
            UpcomingPar = s.UpcomingPar,
            Type = s.Type,
            Annex = s.Annex,
            InsertedBy = s.InsertedBy,
            InsertedDt = s.InsertedDt,
            PostedBy = s.PostedBy,
            PostedDt = s.PostedDt,
            ItemType_Code = s.ItemCode.ItemType.Code,
            Item_Code = s.ItemCode.Code
        };

        public new ValueTask<CustodianReportItemPpeVM> GetByIdAsync(Guid id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.CustodianReportItems.Include(i => i.ItemCode.ItemType)
                .Where(w => w.Id == id)
                .Select(CustodianReporPpeItemProjection).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<CustodianReportItemPpeVM> GetAll(Guid? reportId, string userName)
        {
            IQueryable<CustodianReportItemPpeVM> data = null;
            if (_userService.IsUserNameAdmin(userName) || _annexDService.IsAny(userName))
            {
                data = _db.CustodianReportItems
                    .AsNoTracking()
                    .Where(w => w.ReportId == reportId)
                    .Select(CustodianReporPpeItemProjection);
            }
            else
            {
                data = _db.CustodianReportItems
                    .AsNoTracking()
                    .Where(w => w.ReportId == reportId && w.Annex != "D")
                    .Select(CustodianReporPpeItemProjection);
            }
            return data;
        }

        public IQueryable<CustodianReportItemPpeVM> GetAllByDeptAcctGroup(Guid? deptId, int? accountGroup, string userName)
        {
            IQueryable<CustodianReportItemPpeVM> data = null;
            if (_userService.IsUserNameAdmin(userName) || _annexDService.IsAny(userName))
            {
                data = _db.CustodianReportItems
                .AsNoTracking()
                .Where(w => w.CustodianReport.DeptId == deptId && w.CustodianReport.AccountGroup == accountGroup)
                .Select(CustodianReporPpeItemProjection);
            }
            else
            {
                data = _db.CustodianReportItems
                .AsNoTracking()
                .Where(w => w.CustodianReport.DeptId == deptId && w.CustodianReport.AccountGroup == accountGroup && w.Annex != "D")
                .Select(CustodianReporPpeItemProjection);
            }
            return data;
        }

        public IQueryable<CustodianReportItemPpeVM> GetAllByAcctGroup(int? accountGroup, string userName)
        {
            IQueryable<CustodianReportItemPpeVM> data = null;
            if (_userService.IsUserNameAdmin(userName) || _annexDService.IsAny(userName))
            {
                data = _db.CustodianReportItems
                .AsNoTracking()
                .Where(w => w.CustodianReport.AccountGroup == accountGroup)
                .Select(CustodianReporPpeItemProjection);
            }
            else
            {
                data = _db.CustodianReportItems
                .AsNoTracking()
                .Where(w => w.CustodianReport.AccountGroup == accountGroup && w.Annex != "D")
                .Select(CustodianReporPpeItemProjection);
            }
            return data;
        }

        public ValueTask<CustodianReportItemPpeVM> CreateAsync(CustodianReportItemPpeVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            if (model != null)
            {
                model.AllField = SetAllField(model);
            }

            SetDefaultValues(model);
            
            _validator.ValidateOnCreate(model);
            await base.CreateAsync(model, user, date);
            return model;
        });

        public ValueTask<CustodianReportItemPpeVM> UpdateAsync(CustodianReportItemPpeVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            if (model != null)
            {
                model.AllField = SetAllField(model);
            }

            SetDefaultValues(model);

            _validator.ValidateOnUpdate(model);
            await base.UpdateAsync(model, user, date);
            return model;
        });

        public ValueTask<CustodianReportItemPpeVM> DeleteAsync(CustodianReportItemPpeVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _validator.ValidateOnDelete(model);
            await base.DeleteAsync(model, user, date);
            return model;
        });

        private void SetDefaultValues(CustodianReportItemPpeVM model)
        {
            model.InvDist = "I";
        }
    }
}