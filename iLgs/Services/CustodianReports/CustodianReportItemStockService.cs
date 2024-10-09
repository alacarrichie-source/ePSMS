using ClosedXML.Excel;
using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.AllFields;
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
    public interface ICustodianReportItemStockService : ICustodianReportItemService
    {        
        new ValueTask<CustodianReportItemStockVM> GetByIdAsync(Guid id);
        IQueryable<CustodianReportItemStockVM> GetAll(Guid? reportId);
        IQueryable<CustodianReportItemStockVM> GetAllByDeptAcctGroup(Guid? deptId, int? accountGroup);
        ValueTask<CustodianReportItemStockVM> CreateAsync(CustodianReportItemStockVM model, string user, DateTime date);
        ValueTask<CustodianReportItemStockVM> UpdateAsync(CustodianReportItemStockVM model, string user, DateTime date);
        ValueTask<CustodianReportItemStockVM> DeleteAsync(CustodianReportItemStockVM model, string user, DateTime date);
    }

    public class CustodianReportItemStockService : CustodianReportItemService, ICustodianReportItemStockService
    {
        private readonly IExceptionService<CustodianReportItemStockVM> _exceptionService = new ExceptionService<CustodianReportItemStockVM>();
        private readonly ICustodianReportItemStockValidator _validator;

        public CustodianReportItemStockService(AppManEntities db) : base(db)
        {
            _validator = new CustodianReportItemStockValidator(db);
        }

        private static Expression<Func<CustodianReportItem, CustodianReportItemStockVM>> CustodianReporStockItemProjection
        = s => new CustodianReportItemStockVM
        {
            Id = s.Id,
            MainDeptId = s.CustodianReport.DeptId,
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

        public new ValueTask<CustodianReportItemStockVM> GetByIdAsync(Guid id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.CustodianReportItems
                .Where(w => w.Id == id)
                .Select(CustodianReporStockItemProjection).FirstOrDefaultAsync();
                   
            return data;
        });

        public IQueryable<CustodianReportItemStockVM> GetAll(Guid? reportId)
        {
            var data = _db.CustodianReportItems
                .AsNoTracking()
                .Where(w => w.ReportId == reportId)
                .Select(CustodianReporStockItemProjection);
                
            return data;
        }

        public IQueryable<CustodianReportItemStockVM> GetAllByDeptAcctGroup(Guid? deptId, int? accountGroup)
        {
            var data = _db.CustodianReportItems
                .AsNoTracking()
                .Where(w => w.CustodianReport.DeptId == deptId && w.CustodianReport.AccountGroup == accountGroup)
                .Select(CustodianReporStockItemProjection);

            return data;
        }
        
        public ValueTask<CustodianReportItemStockVM> CreateAsync(CustodianReportItemStockVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            if (model != null)
            {
                model.AllField = SetAllField(model);
            }

            _validator.ValidateOnCreate(model);
            await base.CreateAsync(model, user, date);
            return model;
        });

        public ValueTask<CustodianReportItemStockVM> UpdateAsync(CustodianReportItemStockVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            if (model != null)
            {
                model.AllField = SetAllField(model);
            }

            _validator.ValidateOnUpdate(model);
            await base.UpdateAsync(model, user, date);
            return model;
        });

        public ValueTask<CustodianReportItemStockVM> DeleteAsync(CustodianReportItemStockVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _validator.ValidateOnDelete(model);
            await base.DeleteAsync(model, user, date);
            return model;
        });        
    }
}