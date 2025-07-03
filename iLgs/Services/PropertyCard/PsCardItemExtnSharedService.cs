using iLgs.Models;
using static iLgs.Models.Enums;

namespace iLgs.Services.PropertyCard
{
    public interface IPsCardItemExtnSharedService
    {
        void MapModelToEntityFields(PsCardItemExtn entity, PsCardItemExtnCommonVM model, Mode mode);
    }

    public class PsCardItemExtnSharedService : IPsCardItemExtnSharedService
    {
        private readonly AppManEntities _db;

        public PsCardItemExtnSharedService(AppManEntities db)
        {
            _db = db;
        }

        public void MapModelToEntityFields(PsCardItemExtn entity, PsCardItemExtnCommonVM model, Mode mode)
        {

            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }
            entity.PsCardItemId = model.PsCardItemId;
            entity.CustItemNo = model.CustItemNo;
            entity.SeriesNo = model.SeriesNo;
            entity.SetLotNo = model.SetLotNo;
            entity.SetLotQtyNo = model.SetLotQtyNo;
            entity.ContentNo = model.ContentNo;
            entity.Condition = model.Condition;
            entity.Condition = model.Condition;
            entity.SubLocation = model.SubLocation;
            entity.Annex = model.Annex;
            entity.AddCost = model.AddCost;
            entity.AcqCost = model.AcqCost;
            entity.AcqDate = model.AcqDate;
            entity.LocationId = model.LocationId;
            entity.PropNo = model.PropNo;
            entity.OldPropNo = model.OldPropNo;
            entity.OldAmount = model.OldAmount;
            entity.UpcomingOfficer = model.UpcomingOfficer;
            entity.Remarks = model.Remarks;
        }
    }
}