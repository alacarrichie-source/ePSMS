using iLgs.Models;
using iLgs.Services.ParIcs;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace iLgs.Services.PropertyCard
{
    public interface IPsCardItemTransferItemService
    {
        IQueryable<PsCardItemExtn> GetCardItemExtnForIssuanceByType(Guid? psCardTransferId);
       
        IQueryable<PsCardItemExtn> GetCardItemExtnForOtherIssuanceSelection(Guid? psCardTransferId);
        IQueryable<PsCardItemExtn> GetCardItemExtnForVehicleIssuanceSelection(Guid? psCardTransferId);
        IQueryable<PsCardItemExtn> GetCardItemExtnForLandIssuanceSelection(Guid? psCardTransferId);
        IQueryable<PsCardItemExtn> GetCardItemExtnForBldgIssuanceSelection(Guid? psCardTransferId);

        //List<PsCardItemExtnOtherVM> GetCardItemExtnOthers(Guid? psCardTransferId);
        //List<PsCardItemExtnVehicleVM> GetCardItemExtnVehicles(Guid? psCardTransferId);
        //List<PsCardItemExtnBldgVM> GetCardItemExtnBuildings(Guid? psCardTransferId);
        //List<PsCardItemExtnLandVM> GetCardItemExtnLand(Guid? psCardTransferId);

        IPsCardItemTransferItemOtherService PsCardItemTransferItemOther { get; }
        IPsCardItemTransferItemVehicleService PsCardItemTransferItemVehicle { get; }
        IPsCardItemTransferItemBldgService PsCardItemTransferItemBldg { get; }
        IPsCardItemTransferItemLandService PsCardItemTransferItemLand { get; }

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
            _psCardItemTransferItemOtherService = new PsCardItemTransferItemOtherService(_db);
            _psCardItemTransferItemVehicleService = new PsCardItemTransferItemVehicleService(_db);
            _psCardItemTransferItemBldgService = new PsCardItemTransferItemBldgService(_db);
            _psCardItemTransferItemLandService = new PsCardItemTransferItemLandService(_db);
        }

        public IPsCardItemTransferItemOtherService PsCardItemTransferItemOther { get { return _psCardItemTransferItemOtherService = _psCardItemTransferItemOtherService ?? new PsCardItemTransferItemOtherService(_db); } }
        public IPsCardItemTransferItemVehicleService PsCardItemTransferItemVehicle { get { return _psCardItemTransferItemVehicleService = _psCardItemTransferItemVehicleService ?? new PsCardItemTransferItemVehicleService(_db); } }
        public IPsCardItemTransferItemBldgService PsCardItemTransferItemBldg { get { return _psCardItemTransferItemBldgService = _psCardItemTransferItemBldgService ?? new PsCardItemTransferItemBldgService(_db); } }
        public IPsCardItemTransferItemLandService PsCardItemTransferItemLand { get { return _psCardItemTransferItemLandService = _psCardItemTransferItemLandService ?? new PsCardItemTransferItemLandService(_db); } }

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
    }
}