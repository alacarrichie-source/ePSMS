using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.ParIcs;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.PropertyCard
{
    public interface IPsCardItemTransferItemService
    {
        IQueryable<PsCardItemExtn> GetCardItemExtnForIssuanceByType(Guid? psCardTransferId);

        IQueryable<PsCardItemExtn> GetCardItemExtnForOtherIssuanceSelection(Guid? psCardTransferId);
        IQueryable<PsCardItemExtn> GetCardItemExtnForVehicleIssuanceSelection(Guid? psCardTransferId);
        IQueryable<PsCardItemExtn> GetCardItemExtnForLandIssuanceSelection(Guid? psCardTransferId);
        IQueryable<PsCardItemExtn> GetCardItemExtnForBldgIssuanceSelection(Guid? psCardTransferId);

        /*
         * ItemExtnService:
         *    Allow Add/Edit/Delete to Not Transit Records only         
         *    Do not Delete if record is in transit
         */

        IPsCardItemTransferItemOtherService PsCardItemTransferItemOther { get; }
        IPsCardItemTransferItemVehicleService PsCardItemTransferItemVehicle { get; }
        IPsCardItemTransferItemBldgService PsCardItemTransferItemBldg { get; }
        IPsCardItemTransferItemLandService PsCardItemTransferItemLand { get; }

        void ValidateIfTransit(Guid? psCardTransferId);
        void MapModelToEntityFields(PsCardItemExtn entity, PsCardItemExtnCommonVM model, Mode mode);
        ValueTask DeleteAsync(Guid? id, string user, DateTime? date);
    }


    public class PsCardItemTransferItemService : IPsCardItemTransferItemService
    {
        private readonly AppManEntities _db;
        private IPsCardItemTransferItemOtherService _psCardItemTransferItemOtherService;
        private IPsCardItemTransferItemVehicleService _psCardItemTransferItemVehicleService;
        private IPsCardItemTransferItemBldgService _psCardItemTransferItemBldgService;
        private IPsCardItemTransferItemLandService _psCardItemTransferItemLandService;

        public PsCardItemTransferItemService(AppManEntities db)
        {
            _db = db;
            //_psCardItemTransferItemOtherService = new PsCardItemTransferItemOtherService(_db);
            //_psCardItemTransferItemVehicleService = new PsCardItemTransferItemVehicleService(_db);
            //_psCardItemTransferItemBldgService = new PsCardItemTransferItemBldgService(_db);
            //_psCardItemTransferItemLandService = new PsCardItemTransferItemLandService(_db);
        }

        public IPsCardItemTransferItemOtherService PsCardItemTransferItemOther { get { return _psCardItemTransferItemOtherService = _psCardItemTransferItemOtherService ?? new PsCardItemTransferItemOtherService(_db, this); } }
        public IPsCardItemTransferItemVehicleService PsCardItemTransferItemVehicle { get { return _psCardItemTransferItemVehicleService = _psCardItemTransferItemVehicleService ?? new PsCardItemTransferItemVehicleService(_db, this); } }
        public IPsCardItemTransferItemBldgService PsCardItemTransferItemBldg { get { return _psCardItemTransferItemBldgService = _psCardItemTransferItemBldgService ?? new PsCardItemTransferItemBldgService(_db, this); } }
        public IPsCardItemTransferItemLandService PsCardItemTransferItemLand { get { return _psCardItemTransferItemLandService = _psCardItemTransferItemLandService ?? new PsCardItemTransferItemLandService(_db, this); } }

        public IQueryable<PsCardItemExtn> GetCardItemExtnForIssuanceByType(Guid? psCardTransferId)
        {
            IPsCardService psCardService = new PsCardService(_db);
            var itemExtnName = psCardService.GetItemExtnName(psCardTransferId);
            switch (itemExtnName)
            {
                case "ItemExtnLand":
                    return GetCardItemExtnForIssuance<PsCardItemExtnLand>(psCardTransferId);
                case "ItemExtnBldg":
                    return GetCardItemExtnForIssuance<PsCardItemExtnBuilding>(psCardTransferId);
                case "ItemExtnVehicle":
                    return GetCardItemExtnForIssuance<PsCardItemExtnVehicle>(psCardTransferId);
                default:
                    return GetCardItemExtnForIssuance<PsCardItemExtnOther>(psCardTransferId);
            }
        }

        private IQueryable<T> GetCardItemExtnForIssuance<T>(Guid? psCardTransferId) where T : PsCardItemExtn
        {
            var data = _db.PsCardItemExtns.OfType<T>().AsNoTracking()
                        .Where(w => w.PsCardItemTransferItems.Any(a => a.PsCardItemTransferId == psCardTransferId))
                        .AsQueryable();
            return data;
        }

        /* 
         * Condition:
         * Not Transferred
         * Not Issueed
         */
        private IQueryable<T> GetCardItemExtnForIssuanceSelection<T>(Guid? psCardTransferId) where T : PsCardItemExtn
        {
            var data = _db.PsCardItemExtns.OfType<T>().AsNoTracking()
                        .Include(i => i.IcsParItems)
                        .Where(w => w.PsCardItemTransferItems.Any(a => a.PsCardItemTransferId == psCardTransferId && !a.PsCardItemTransferIssuanceItems.Any())
                            && !w.PsCardItemTransferItems.Any(a => a.PsCardItemTransferId != psCardTransferId))
                        .AsQueryable();
            return data;
        }

        public IQueryable<PsCardItemExtn> GetCardItemExtnForOtherIssuanceSelection(Guid? psCardTransferId)
        {
            return GetCardItemExtnForIssuanceSelection<PsCardItemExtnOther>(psCardTransferId);
        }

        public IQueryable<PsCardItemExtn> GetCardItemExtnForVehicleIssuanceSelection(Guid? psCardTransferId)
        {
            return GetCardItemExtnForIssuanceSelection<PsCardItemExtnOther>(psCardTransferId);
        }

        public IQueryable<PsCardItemExtn> GetCardItemExtnForLandIssuanceSelection(Guid? psCardTransferId)
        {
            return GetCardItemExtnForIssuanceSelection<PsCardItemExtnLand>(psCardTransferId);
        }

        public IQueryable<PsCardItemExtn> GetCardItemExtnForBldgIssuanceSelection(Guid? psCardTransferId)
        {
            return GetCardItemExtnForIssuanceSelection<PsCardItemExtnBuilding>(psCardTransferId);
        }

        public IQueryable<T> GetCardItemExtn<T>(Guid? psCardTransferId) where T : PsCardItemExtn
        {
            var data = _db.PsCardItemExtns.OfType<T>().AsNoTracking()
                        .AsQueryable();
            return data;
        }

        //public List<PsCardItemExtnOtherVM> GetCardItemExtnOthers(Guid? psCardTransferId)
        //{            
        //    var data = _db.Database.SqlQuery<PsCardItemExtnOtherVM>("Exec PsCardItemExtnTransferItem_GetItemExtnOthersByTransferId {0}", psCardTransferId).ToList();
        //    return data;
        //}

        public List<PsCardItemExtnVehicleVM> GetCardItemExtnVehicles(Guid? psCardTransferId)
        {
            var data = _db.Database.SqlQuery<PsCardItemExtnVehicleVM>("Exec PsCardItemExtnTransferItem_GetItemExtnVehiclesByTransferId {0}", psCardTransferId).ToList();
            return data;
        }

        public List<PsCardItemExtnBldgVM> GetCardItemExtnBuildings(Guid? psCardTransferId)
        {
            var data = _db.Database.SqlQuery<PsCardItemExtnBldgVM>("Exec PsCardItemExtnTransferItem_GetItemExtnBuildingsByTransferId {0}", psCardTransferId).ToList();
            return data;
        }

        public List<PsCardItemExtnLandVM> GetCardItemExtnLand(Guid? psCardTransferId)
        {
            var data = _db.Database.SqlQuery<PsCardItemExtnLandVM>("Exec PsCardItemExtnTransferItem_GetItemExtnLanByTransferId {0}", psCardTransferId).ToList();
            return data;
        }

        public void ValidateIfTransit(Guid? psCardTransferId)
        {
            if (_db.PsCardItemTransfers.Any(a => a.Id == psCardTransferId && a.ParentId != null))
            {
                throw new InvalidValueException("Action not allowed for transit record.");
            }
        }

        public async ValueTask DeleteAsync(Guid? id, string user, DateTime? date)
        {
            var transferItemEntity = await _db.PsCardItemTransferItems.FindAsync(id);
            var psCardItemExtnId = transferItemEntity.PsCardItemExtnId;

            transferItemEntity.UpdatedBy = user;
            transferItemEntity.UpdatedDt = date;

            _db.PsCardItemTransferItems.Attach(transferItemEntity);
            _db.Entry(transferItemEntity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCardItemTransferItems.Remove(transferItemEntity);
            _db.Entry(transferItemEntity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();
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
            entity.LocationId = model.LocationId;
            entity.PropNo = model.PropNo;
            entity.OldPropNo = model.OldPropNo;
            entity.OldAmount = model.OldAmount;
            entity.UpcomingOfficer = model.UpcomingOfficer;
            entity.Remarks = model.Remarks;
        }
    }
}