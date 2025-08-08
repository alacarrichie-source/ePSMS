using iLgs.Models;
using iLgs.Services.Codes;
using System;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.PropertyCard
{
    public interface IPsCardItemExtnService : IPsCardItemExtnSharedService
    {        
        IQueryable<T> GetCardItemExtnForIcsPars<T>(Guid? psCardItemId) where T : PsCardItemExtn;
        IQueryable<T> GetCardItemExtnForIssuance<T>(Guid? psCardItemId) where T : PsCardItemExtn;
        IQueryable<T> GetCardItemExtnForIssuanceSelection<T>(Guid? psCardItemId) where T : PsCardItemExtn;

        ValueTask<PsCardItemExtnParVm> GetCardItemExtnParAsync(Guid? psCardItemExtnId);
        ValueTask<PsCardItemExtnLocationVm> GetCardItemExtnLocationAsync(Guid? psCardItemExtnId);

        IQueryable<PsCardItemExtn> GetCardItemExtnForIcsParsByType(Guid? psCardItemId);
        IQueryable<PsCardItemExtn> GetCardItemExtnSetForIcsParsByType(Guid? psCardItemId);
        IQueryable<PsCardItemExtnSetVM> GetCardItemExtnSetForIcsParByUnitGroupId(Guid? unitGroupId);
        IQueryable<PsCardItemExtn> GetCardItemExtnForIssuanceByType(Guid? psCardItemId);
        IQueryable<PsCardItemExtn> GetCardItemExtnForOtherIssuanceSelection(Guid? psCardItemId);
        IQueryable<PsCardItemExtn> GetCardItemExtnForVehicleIssuanceSelection(Guid? psCardItemId);
        IQueryable<PsCardItemExtn> GetCardItemExtnForLandIssuanceSelection(Guid? psCardItemId);
        IQueryable<PsCardItemExtn> GetCardItemExtnForBldgIssuanceSelection(Guid? psCardItemId);
        
        string GetEndSeries(string startSeries, Guid? itemId);
        string GetEndSeries(string startSeries, decimal qty);

        //void MapModelToEntityFields(PsCardItemExtn entity, PsCardItemExtnCommonVM model, Mode mode);

        IPsCardItemExtnVehicleService PsCardItemExtnVehicle { get; }
        IPsCardItemExtnOtherService PsCardItemExtnOther { get; }
        IPsCardItemExtnLandService PsCardItemExtnLand { get; }
        IPsCardItemExtnBldgService PsCardItemExtnBldg { get; }
        IPsCardItemExtnUpdateService PsCardItemExtnUpdate { get; }
        IPsCardItemExtnAddCostService PsCardItemExtnAddCost{ get; }       
    }


    public class PsCardItemExtnService : IPsCardItemExtnService
    {
        private readonly AppManEntities _db;
        private decimal? _SPHV;

        private readonly IPsCardSharedService _psCardSharedService;
        private readonly IPsCardItemExtnSharedService _psCardItemExtnSharedService;
        private readonly IPsCardItemExtnVehicleService _psCardItemExtnVehicleService;
        private readonly IPsCardItemExtnOtherService _psCardItemExtnOtherService;
        private readonly IPsCardItemExtnLandService _psCardItemExtnLandService;
        private readonly IPsCardItemExtnBldgService _psCardItemExtnBldgService;
        private readonly IPsCardItemExtnUpdateService _psCardItemExtnUpdateService;
        private readonly IPsCardItemExtnAddCostService _psCardItemExtnAddCostService;
        private readonly ISemiExpendableService _semiExpendableService;

        public PsCardItemExtnService(AppManEntities db,
            IPsCardSharedService psCardSharedService,
            IPsCardItemExtnSharedService psCardItemExtnSharedService,
            IPsCardItemExtnVehicleService psCardItemExtnVehicleService,
            IPsCardItemExtnOtherService psCardItemExtnOtherService,
            IPsCardItemExtnLandService psCardItemExtnLandService,
            IPsCardItemExtnBldgService psCardItemExtnBldgService,
            IPsCardItemExtnUpdateService psCardItemExtnUpdateService,
            IPsCardItemExtnAddCostService psCardItemExtnAddCostService,
            ISemiExpendableService semiExpendableService)
        {
            _db = db;
            _psCardSharedService = psCardSharedService;
            _psCardItemExtnSharedService = psCardItemExtnSharedService;
            _psCardItemExtnVehicleService = psCardItemExtnVehicleService;
            _psCardItemExtnOtherService = psCardItemExtnOtherService;
            _psCardItemExtnLandService = psCardItemExtnLandService;
            _psCardItemExtnBldgService = psCardItemExtnBldgService;
            _psCardItemExtnUpdateService = psCardItemExtnUpdateService;
            _psCardItemExtnAddCostService = psCardItemExtnAddCostService;
            _semiExpendableService = semiExpendableService;
        }

        public IPsCardItemExtnVehicleService PsCardItemExtnVehicle => _psCardItemExtnVehicleService;
        public IPsCardItemExtnLandService PsCardItemExtnLand => _psCardItemExtnLandService;
        public IPsCardItemExtnBldgService PsCardItemExtnBldg => _psCardItemExtnBldgService;
        public IPsCardItemExtnOtherService PsCardItemExtnOther => _psCardItemExtnOtherService;
        public IPsCardItemExtnUpdateService PsCardItemExtnUpdate => _psCardItemExtnUpdateService;
        public IPsCardItemExtnAddCostService PsCardItemExtnAddCost => _psCardItemExtnAddCostService;

        private decimal GetSPHV()
        {
            return _SPHV ?? (_SPHV = _semiExpendableService.GetSPHV()).Value;
        }

        public IQueryable<T> GetCardItemExtnForIcsPars<T>(Guid? psCardItemId) where T : PsCardItemExtn
        {        
            var data = _db.PsCardItemExtns.OfType<T>().AsNoTracking()
                        .Include(i => i.PsCardItem.PsCard.ItemCode)
                        .Where(w => !w.IcsParItems.Any(a => a.PsCardItemExtnId == w.Id)
                            && w.PsCardItemId == psCardItemId)
                        .AsQueryable();
         
            return data;
        }

        public IQueryable<T> GetCardItemExtnSetForIcsPars<T>(Guid? psCardItemId) where T : PsCardItemExtn
        {
            var data = _db.PsCardItemExtns.OfType<T>().AsNoTracking()
                        .Include(i => i.PsCardItem.PsCard.ItemCode)
                        .Where(w => !w.IcsParItems.Any(a => a.PsCardItemExtnId == w.Id)
                            && (w.PsCardItemId == psCardItemId
                                && _db.PsCardItemUnitGroups.Any(a => a.PoNo == w.PsCardItem.PoNo)
                            )
                        )
                        .AsQueryable();

            return data;
        }

        public IQueryable<PsCardItemExtn> GetCardItemExtnForIcsParsByType(Guid? psCardItemId)
        {
            var itemExtnName = _psCardSharedService.GetItemExtnName(psCardItemId);
            switch (itemExtnName)
            {
                case "ItemExtnLand":
                    return GetCardItemExtnForIcsPars<PsCardItemExtnLand>(psCardItemId);
                case "ItemExtnBldg":
                    return GetCardItemExtnForIcsPars<PsCardItemExtnBuilding>(psCardItemId);
                case "ItemExtnVehicle":
                    return GetCardItemExtnForIcsPars<PsCardItemExtnVehicle>(psCardItemId);
                default:
                    return GetCardItemExtnForIcsPars<PsCardItemExtnOther>(psCardItemId);
            }
        }

        public IQueryable<PsCardItemExtn> GetCardItemExtnSetForIcsParsByType(Guid? psCardItemId)
        {
            var itemExtnName = _psCardSharedService.GetItemExtnName(psCardItemId);
            switch (itemExtnName)
            {
                case "ItemExtnLand":
                    return GetCardItemExtnSetForIcsPars<PsCardItemExtnLand>(psCardItemId);
                case "ItemExtnBldg":
                    return GetCardItemExtnSetForIcsPars<PsCardItemExtnBuilding>(psCardItemId);
                case "ItemExtnVehicle":
                    return GetCardItemExtnSetForIcsPars<PsCardItemExtnVehicle>(psCardItemId);
                default:
                    return GetCardItemExtnSetForIcsPars<PsCardItemExtnOther>(psCardItemId);
            }
        }

        public IQueryable<PsCardItemExtnSetVM> GetCardItemExtnSetForIcsParByUnitGroupId(Guid? unitGroupId)
        {
            var SPHV = GetSPHV();
            var data = _db.Database.SqlQuery<PsCardItemExtnSetVM>("Exec PsCardItemExtn_GetItemExtnSetForIcsPars {0}, {1}", unitGroupId, SPHV).AsQueryable();
            return data;
        }

        public IQueryable<T> GetCardItemExtnForIssuance<T>(Guid? psCardItemId) where T : PsCardItemExtn
        {
            var data = _db.PsCardItemExtns.OfType<T>().AsNoTracking()
                        .Where(w => w.PsCardItemId == psCardItemId)
                        .AsQueryable();
            return data;
        }

        public IQueryable<PsCardItemExtn> GetCardItemExtnForIssuanceByType(Guid? psCardItemId)
        {
            var itemExtnName = _psCardSharedService.GetItemExtnName(psCardItemId);
            switch (itemExtnName)
            {
                case "ItemExtnLand":
                    return GetCardItemExtnForIssuance<PsCardItemExtnLand>(psCardItemId);
                case "ItemExtnBldg":
                    return GetCardItemExtnForIssuance<PsCardItemExtnBuilding>(psCardItemId);
                case "ItemExtnVehicle":
                    return GetCardItemExtnForIssuance<PsCardItemExtnVehicle>(psCardItemId);
                default:
                    return GetCardItemExtnForIssuance<PsCardItemExtnOther>(psCardItemId);
            }
        }

        public IQueryable<T> GetCardItemExtnForIssuanceSelection<T>(Guid? psCardItemId) where T : PsCardItemExtn
        {
            var data = _db.PsCardItemExtns.OfType<T>().AsNoTracking()
                        .Include(i => i.IcsParItems)
                        .Where(w => w.PsCardItemId == psCardItemId 
                            && !w.PsCardItemIssuanceItems.Any()
                            && !w.PsCardItemTransferItems.Any())
                        .AsQueryable();            
            return data;
        }
        public async ValueTask<PsCardItemExtnParVm> GetCardItemExtnParAsync(Guid? psCardItemExtnId)
        {
            var data = await _db.PsCardItemExtns.AsNoTracking()
                        .Where(w => w.Id == psCardItemExtnId)
                        .Select(s => new PsCardItemExtnParVm
                        {
                            Id = s.Id,
                            ParIcsNo = s.IcsParItems.FirstOrDefault() == null ? "" : s.IcsParItems.FirstOrDefault().IcsPar.RefNo,
                            ParIcsDate = s.IcsParItems.FirstOrDefault() == null ? null : s.IcsParItems.FirstOrDefault().IcsPar.RefDate
                        }).FirstOrDefaultAsync();                        
            return data;
        }

        public async ValueTask<PsCardItemExtnLocationVm> GetCardItemExtnLocationAsync(Guid? psCardItemExtnId)
        {
            var data = await _db.PsCardItemExtns.AsNoTracking()
                        .Where(w => w.Id == psCardItemExtnId)
                        .Select(s => new PsCardItemExtnLocationVm
                        {
                            Id = s.Id,
                            LocationId = s.IcsParItems.FirstOrDefault() == null ? null : s.IcsParItems.FirstOrDefault().IcsPar.LocationId,
                            Location = s.IcsParItems.FirstOrDefault() == null ? "" : s.IcsParItems.FirstOrDefault().IcsPar.Location,
                            LocationCode = s.IcsParItems.FirstOrDefault() == null ? "" : s.IcsParItems.FirstOrDefault().IcsPar.LocationCode,                            
                        }).FirstOrDefaultAsync();
            return data;
        }
        
        public IQueryable<PsCardItemExtn> GetCardItemExtnForOtherIssuanceSelection(Guid? psCardItemId)
        {
            return GetCardItemExtnForIssuanceSelection<PsCardItemExtnOther>(psCardItemId);
        }

        public IQueryable<PsCardItemExtn> GetCardItemExtnForVehicleIssuanceSelection(Guid? psCardItemId)
        {
            return GetCardItemExtnForIssuanceSelection<PsCardItemExtnOther>(psCardItemId);
        }

        public IQueryable<PsCardItemExtn> GetCardItemExtnForLandIssuanceSelection(Guid? psCardItemId)
        {
            return GetCardItemExtnForIssuanceSelection<PsCardItemExtnLand>(psCardItemId);
        }

        public IQueryable<PsCardItemExtn> GetCardItemExtnForBldgIssuanceSelection(Guid? psCardItemId)
        {
            return GetCardItemExtnForIssuanceSelection<PsCardItemExtnBuilding>(psCardItemId);
        }
        
        public string GetEndSeries(string startSeries, Guid? itemId)
        {
            var qty = _db.PsCardItems.Where(w => w.Id == itemId).AsNoTracking()
                .Select(s => new
                {
                    Qty = (s.Qty ?? 0 + s.TransferIn ?? 0) - s.PsCardItemExtns.Count()
                }).FirstOrDefault().Qty;

            return GetEndSeries(startSeries, qty);
        }

        public string GetEndSeries(string startSeries, decimal qty)
        {
            // Regular expression to capture the numeric part at the end of the string
            string pattern = @"(.*?)(\d+)$";
            Match match = Regex.Match(startSeries, pattern);

            if (match.Success)
            {
                // Extract the non-numeric part (prefix) and the numeric part (number)
                string prefix = match.Groups[1].Value;  // 'AB-01-X-'
                string numericPart = match.Groups[2].Value;  // '01'

                decimal startNumber = decimal.Parse(numericPart);  // Convert '01' to 1

                // Add the quantity to the start number
                decimal endNumber = startNumber + qty - 1;

                // Reassemble the series and maintain the same padding (based on the length of the numeric part)
                int paddingLength = numericPart.Length;  // Get the length of the original numeric part
                return $"{prefix}{endNumber.ToString($"D{paddingLength}")}";  // Dynamically format the number with the same number of digits
            }

            // If no match is found, return the start series as it is
            return startSeries;
        }

        public void MapModelToEntityFields(PsCardItemExtn entity, PsCardItemExtnCommonVM model, Mode mode)
        {
            _psCardItemExtnSharedService.MapModelToEntityFields(entity, model, mode);            
        }
    }
}