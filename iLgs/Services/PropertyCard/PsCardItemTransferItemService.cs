using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.PropertyCard
{
    public interface IPsCardItemTransferItemService : IPsCardItemTransferItemSharedService
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

        //void ValidateIfTransit(Guid? psCardTransferId);
        //void MapModelToEntityFields(PsCardItemExtn entity, PsCardItemExtnCommonVM model, Mode mode);
        ValueTask DeleteAsync(Guid? id, string user, DateTime? date);
    }


    public class PsCardItemTransferItemService : IPsCardItemTransferItemService
    {
        private readonly AppManEntities _db;
        private readonly IPsCardItemTransferItemSharedService _psCardItemTransferItemSharedService;
        private readonly IPsCardItemTransferItemOtherService _psCardItemTransferItemOtherService;
        private readonly IPsCardItemTransferItemVehicleService _psCardItemTransferItemVehicleService;
        private readonly IPsCardItemTransferItemBldgService _psCardItemTransferItemBldgService;
        private readonly IPsCardItemTransferItemLandService _psCardItemTransferItemLandService;
        private readonly IPsCardSharedService _psCardSharedService;

        public PsCardItemTransferItemService(AppManEntities db)
        {
            _db = db;
            _psCardItemTransferItemSharedService = new PsCardItemTransferItemSharedService(_db);
            _psCardItemTransferItemOtherService = new PsCardItemTransferItemOtherService(_db);
            _psCardItemTransferItemVehicleService = new PsCardItemTransferItemVehicleService(_db);
            _psCardItemTransferItemBldgService = new PsCardItemTransferItemBldgService(_db);
            _psCardItemTransferItemLandService = new PsCardItemTransferItemLandService(_db);
            _psCardSharedService = new PsCardSharedService(_db);
        }

        //public PsCardItemTransferItemService(AppManEntities db,
        //    IAppManEntitiesFactory appManEntitiesFactory,
        //    IPsCardItemTransferItemSharedService psCardItemTransferItemSharedService,
        //    IPsCardItemTransferItemOtherService psCardItemTransferItemOtherService,
        //    IPsCardItemTransferItemVehicleService psCardItemTransferItemVehicleService,
        //    IPsCardItemTransferItemBldgService psCardItemTransferItemBldgService,
        //    IPsCardItemTransferItemLandService psCardItemTransferItemLandService,
        //    IPsCardSharedService psCardSharedService)
        //{
        //    _db = db;
        //    _contextFactory = appManEntitiesFactory;
        //    _psCardItemTransferItemSharedService = psCardItemTransferItemSharedService;
        //    _psCardItemTransferItemOtherService = psCardItemTransferItemOtherService;
        //    _psCardItemTransferItemVehicleService = psCardItemTransferItemVehicleService;
        //    _psCardItemTransferItemBldgService = psCardItemTransferItemBldgService;
        //    _psCardItemTransferItemLandService = psCardItemTransferItemLandService;
        //    _psCardSharedService = psCardSharedService;
        //}

        public IPsCardItemTransferItemOtherService PsCardItemTransferItemOther => _psCardItemTransferItemOtherService;
        public IPsCardItemTransferItemVehicleService PsCardItemTransferItemVehicle => _psCardItemTransferItemVehicleService;
        public IPsCardItemTransferItemBldgService PsCardItemTransferItemBldg => _psCardItemTransferItemBldgService;
        public IPsCardItemTransferItemLandService PsCardItemTransferItemLand => _psCardItemTransferItemLandService;

        public IQueryable<PsCardItemExtn> GetCardItemExtnForIssuanceByType(Guid? psCardTransferId)
        {
            var itemExtnName = _psCardSharedService.GetItemExtnName(psCardTransferId);
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
            _psCardItemTransferItemSharedService.ValidateIfTransit(psCardTransferId);
        }

        public async ValueTask DeleteAsync(Guid? id, string user, DateTime? date)
        {
            var transferItemEntity = await _db.PsCardItemTransferItems.FindAsync(id);
            var psCardItemExtnId = transferItemEntity.PsCardItemExtnId;

            transferItemEntity.UpdatedBy = user;
            transferItemEntity.UpdatedDt = date;

            await _db.SaveChangesAsync();

            _db.PsCardItemTransferItems.Remove(transferItemEntity);
            await _db.SaveChangesAsync();
        }

        public void MapModelToEntityFields(PsCardItemExtn entity, PsCardItemExtnCommonVM model, Mode mode)
        {
            _psCardItemTransferItemSharedService.MapModelToEntityFields(entity, model, mode);
        }
    }
}