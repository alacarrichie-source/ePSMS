using iLgs.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.PropertyCard
{
    public interface IPsCardItemExtnUpdateService
    {
        IPsCardItemExtnVehicleService PsCardItemExtnVehicle { get; }
        IPsCardItemExtnOtherService PsCardItemExtnOther { get; }

        IQueryable<PsCardItemExtnVM> GetAll();

        IQueryable<PsCardItemExtnVM> GetAll(int? accountGroup);

        IQueryable<PsCardItemExtn> GetCardItemExtnForSupplies();
        IQueryable<PsCardItemExtn> GetCardItemExtnForVehicles();
        IQueryable<PsCardItemExtn> GetCardItemExtnForEquipment();
        IQueryable<PsCardItemExtn> GetCardItemExtnForLand();
        IQueryable<PsCardItemExtn> GetCardItemExtnForStructures();

        PsCardItemExtnLandEntryVM GetCardItemExtnLandEntry(Guid? id);
        PsCardItemExtnStructuresEntryVM GetCardItemExtnStructuresEntry(Guid? id);
        PsCardItemExtnVehicleEntryVM GetCardItemExtnVehicleEntry(Guid? id);
        PsCardItemExtnPpeEntryVM GetCardItemExtnPpeEntry(Guid? id);        
    }


    public class PsCardItemExtnUpdateService : IPsCardItemExtnUpdateService
    {
        private readonly AppManEntities _db;
        private IPsCardItemExtnVehicleService _psCardItemExtnVehicleService;
        private IPsCardItemExtnOtherService _psCardItemExtnOtherService;

        public PsCardItemExtnUpdateService(AppManEntities db)
        {
            _db = db;
            _psCardItemExtnVehicleService = new PsCardItemExtnVehicleService(_db);
            _psCardItemExtnOtherService = new PsCardItemExtnOtherService(_db);
        }

        public IPsCardItemExtnVehicleService PsCardItemExtnVehicle { get { return _psCardItemExtnVehicleService = _psCardItemExtnVehicleService ?? new PsCardItemExtnVehicleService(_db); } }
        public IPsCardItemExtnOtherService PsCardItemExtnOther { get { return _psCardItemExtnOtherService = _psCardItemExtnOtherService ?? new PsCardItemExtnOtherService(_db); } }

        public IQueryable<PsCardItemExtnVM> GetAll()
        {
            return GetAll((int)AccountGroup.ALL);
        }

        public IQueryable<PsCardItemExtnVM> GetAll(int? accountGroup)
        {
            var data = _db.Database.SqlQuery<PsCardItemExtnVM>("Exec PsCardItemExtns_GetAll {0}", accountGroup).AsQueryable();
            return data;
        }

        public IQueryable<PsCardItemExtn> GetCardItemExtnForSupplies()
        {
            return GetCardItemExtns<PsCardItemExtnOther>();
        }

        public IQueryable<PsCardItemExtn> GetCardItemExtnForVehicles()
        {
            return GetCardItemExtns<PsCardItemExtnVehicle>();
        }

        public IQueryable<PsCardItemExtn> GetCardItemExtnForEquipment()
        {
            return GetCardItemExtns<PsCardItemExtnOther>();
        }

        public IQueryable<PsCardItemExtn> GetCardItemExtnForLand()
        {
            return GetCardItemExtns<PsCardItemExtnLand>();
        }

        public IQueryable<PsCardItemExtn> GetCardItemExtnForStructures()
        {
            return GetCardItemExtns<PsCardItemExtnBuilding>();
        }

        public IQueryable<T> GetCardItemExtns<T>() where T : PsCardItemExtn
        {
            var data = _db.PsCardItemExtns.OfType<T>().AsNoTracking()
                        .Include(i => i.PsCardItem.PsCard.ItemCode.ItemType)
                        .Include(i => i.IcsParItems)
                        .AsQueryable();
            return data;
        }
                                               
        public PsCardItemExtnStructuresEntryVM GetCardItemExtnStructuresEntry(Guid? id)
        {
            var data = _db.Database.SqlQuery<PsCardItemExtnStructuresEntryVM>("Exec PsCardItemExtn_Structures_GetById {0}", id).FirstOrDefault();
            return data;
        }

        public PsCardItemExtnLandEntryVM GetCardItemExtnLandEntry(Guid? id)
        {
            var data = _db.Database.SqlQuery<PsCardItemExtnLandEntryVM>("Exec PsCardItemExtn_Land_GetById {0}", id).FirstOrDefault();
            return data;
        }

        public PsCardItemExtnVehicleEntryVM GetCardItemExtnVehicleEntry(Guid? id)
        {
            var data = _db.Database.SqlQuery<PsCardItemExtnVehicleEntryVM>("Exec PsCardItemExtn_Vehicle_GetById {0}", id).FirstOrDefault();
            return data;
        }

        public PsCardItemExtnPpeEntryVM GetCardItemExtnPpeEntry(Guid? id)
        {
            var data = _db.Database.SqlQuery<PsCardItemExtnPpeEntryVM>("Exec PsCardItemExtn_PpeSupplies_GetById {0}", id).FirstOrDefault();
            return data;
        }

        public IQueryable<T> GetCardItemExtnForIcsPars<T>(Guid? psCardItemId) where T : PsCardItemExtn
        {
            var data = _db.PsCardItemExtns.OfType<T>().AsNoTracking()
                        .Include(i => i.PsCardItem.PsCard.ItemCode)
                        .Where(w => !w.IcsParItems.Any(a => a.PsCardItemExtnId == w.Id)
                            && (w.PsCardItemId == psCardItemId
                            //|| _db.PsCardItemUnitGroups.Any(a => a.PoNo == w.PsCardItem.PoNo)
                            )
                        )
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
            IPsCardService psCardService = new PsCardService(_db);
            var itemExtnName = psCardService.GetItemExtnName(psCardItemId);
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
            IPsCardService psCardService = new PsCardService(_db);
            var itemExtnName = psCardService.GetItemExtnName(psCardItemId);
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
            var data = _db.Database.SqlQuery<PsCardItemExtnSetVM>("Exec PsCardItemExtn_GetItemExtnSetForIcsPars {0}", unitGroupId).AsQueryable();
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
            IPsCardService psCardService = new PsCardService(_db);
            var itemExtnName = psCardService.GetItemExtnName(psCardItemId);
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

        public IQueryable<PsCardItemExtnVehicleVm> GetCardItemExtnForVehicleIssuanceSelection(Guid? psCardItemId)
        {
            var data = _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>().AsNoTracking()
                        .Include(i => i.IcsParItems)
                        .Where(w => w.PsCardItemId == psCardItemId
                        //&& !w.PsCardItemTransactions.Any(a => a.Remarks == "ISSUANCE")
                        )
                        .Select(s => new PsCardItemExtnVehicleVm
                        {
                            Id = s.Id,
                            YearModel = s.YearModel,
                            PlateNo = s.PlateNo,
                            BodyNo = s.BodyNo,
                            EngineNo = s.EngineNo,
                            ChasisNo = s.ChasisNo,
                            Color = s.Color,
                            CRN = s.CRN,
                            CRDate = s.CRDate,
                            MVFileNo = s.MVFileNo,
                            OrNo = s.OrNo,
                            OrDate = s.OrDate,
                            NetWeight = s.NetWeight,
                            InsPolicyNo = s.InsPolicyNo,
                            ParReissuance = s.ParReissuance,
                            Condition = s.Condition,
                            SubLocation = s.SubLocation,
                            ConductionNo = s.ConductionNo,
                            LocationId = s.IcsParItems.FirstOrDefault() == null ? null : s.IcsParItems.FirstOrDefault().IcsPar.LocationId,
                            Location = s.IcsParItems.FirstOrDefault() == null ? "" : s.IcsParItems.FirstOrDefault().IcsPar.Location,
                            LocationCode = s.IcsParItems.FirstOrDefault() == null ? "" : s.IcsParItems.FirstOrDefault().IcsPar.LocationCode,
                            ParIcsNo = s.IcsParItems.FirstOrDefault() == null ? "" : s.IcsParItems.FirstOrDefault().IcsPar.RefNo,
                            ParIcsDate = s.IcsParItems.FirstOrDefault() == null ? null : s.IcsParItems.FirstOrDefault().IcsPar.RefDate
                        })
                        .AsQueryable();
            return data;
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

        public string GetEndSeries(string startSeries, int qty)
        {
            // Regular expression to capture the numeric part at the end of the string
            string pattern = @"(.*?)(\d+)$";
            Match match = Regex.Match(startSeries, pattern);

            if (match.Success)
            {
                // Extract the non-numeric part (prefix) and the numeric part (number)
                string prefix = match.Groups[1].Value;  // 'AB-01-X-'
                string numericPart = match.Groups[2].Value;  // '01'

                int startNumber = int.Parse(numericPart);  // Convert '01' to 1

                // Add the quantity to the start number
                int endNumber = startNumber + qty - 1;

                // Reassemble the series and maintain the same padding (based on the length of the numeric part)
                int paddingLength = numericPart.Length;  // Get the length of the original numeric part
                return $"{prefix}{endNumber.ToString($"D{paddingLength}")}";  // Dynamically format the number with the same number of digits
            }

            // If no match is found, return the start series as it is
            return startSeries;
        }
    }
}