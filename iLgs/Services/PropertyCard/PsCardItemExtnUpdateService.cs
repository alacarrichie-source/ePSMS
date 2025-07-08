using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.Validators;
using iLgs.Utilities;
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

        ValueTask<PsCardItemExtnPpeEntryVM> UpdatePpeAsync(PsCardItemExtnPpeEntryVM model, string user, DateTime date);
        ValueTask<PsCardItemExtnVehicleEntryVM> UpdateVehicleAsync(PsCardItemExtnVehicleEntryVM model, string user, DateTime date);        
    }


    public class PsCardItemExtnUpdateService : BaseValidator, IPsCardItemExtnUpdateService
    {
        private readonly AppManEntities _db;
        private decimal? _SPHV;

        private readonly GetDisplayNameDelegate _getPpeDisplayName;
        private readonly GetDisplayNameDelegate _getVehicleDisplayName;
        private readonly IPsCardSharedService _psCardSharedService;
        private readonly IExceptionService<PsCardItemExtnPpeEntryVM> _ppeExceptionService;
        private readonly IExceptionService<PsCardItemExtnVehicleEntryVM> _vehicleExceptionService;
        private readonly IExceptionService<PsCardItemExtnLandEntryVM> _landExceptionService;
        private readonly ISemiExpendableService _semiExpendableService;

        public PsCardItemExtnUpdateService(AppManEntities db,
            IPsCardSharedService psCardSharedService,
            IExceptionService<PsCardItemExtnPpeEntryVM> ppeExceptionService,
            IExceptionService<PsCardItemExtnVehicleEntryVM> vehicleExceptionService,
            IExceptionService<PsCardItemExtnLandEntryVM> landExceptionService,
            ISemiExpendableService semiExpendableService)
        {
            _db = db;
            _psCardSharedService = psCardSharedService;
            _ppeExceptionService = ppeExceptionService;
            _vehicleExceptionService = vehicleExceptionService;
            _landExceptionService = landExceptionService;
            _semiExpendableService = semiExpendableService;
            _getPpeDisplayName = propertyName => Utility.GetDisplayName<PsCardItemExtnPpeEntryVM>(propertyName);
            _getVehicleDisplayName = propertyName => Utility.GetDisplayName<PsCardItemExtnVehicleEntryVM>(propertyName);
        }

        private decimal GetSPHV()
        {
            return _SPHV ?? (_SPHV = _semiExpendableService.GetSPHV()).Value;
        }

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
            var data = _db.Database.SqlQuery<PsCardItemExtnStructuresEntryVM>("Exec PsCardItemExtn_Building_GetById {0}", id).FirstOrDefault();
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

        
        public ValueTask<PsCardItemExtnPpeEntryVM> UpdatePpeAsync(PsCardItemExtnPpeEntryVM model, string user, DateTime date) => _ppeExceptionService.TryCatch(async () =>
        {
            if (model == null)
            {
                throw new NullException();
            }


            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItemExtns.Include(i => i.PsCardItem).OfType<PsCardItemExtnOther>().FirstOrDefaultAsync(f => f.Id == model.Id);

            if (entity == null)
            {
                throw new NotFoundException(model.Id);
            }

            entity.CustItemNo = model.CustItemNo;
            entity.Annex = model.Annex;
            entity.SeriesNo = model.SeriesNo;            
            entity.SubLocation = model.SubLocation;
            entity.Condition = model.Condition;
            entity.AddCost = model.AddCost;
            entity.AcqCost = entity.PsCardItem.UnitCost + model.AddCost;
            entity.Remarks = model.Remarks;
            entity.OldPropNo = model.OldPropNo;
            entity.OldAmount = model.OldAmount;
            entity.UpcomingOfficer = model.UpcomingOfficer;

            entity.SerialNo = model.SerialNo;

            _db.PsCardItemExtns.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<PsCardItemExtnVehicleEntryVM> UpdateVehicleAsync(PsCardItemExtnVehicleEntryVM model, string user, DateTime date) => _vehicleExceptionService.TryCatch(async () =>
        {
            if (model == null)
            {
                throw new NullException();
            }


            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItemExtns.Include(i => i.PsCardItem).OfType<PsCardItemExtnVehicle>().FirstOrDefaultAsync(f => f.Id == model.Id);

            if (entity == null)
            {
                throw new NotFoundException(model.Id);
            }

            entity.CustItemNo = model.CustItemNo;
            entity.Annex = model.Annex;
            entity.SeriesNo = model.SeriesNo;
            entity.SubLocation = model.SubLocation;
            entity.Condition = model.Condition;
            entity.AddCost = model.AddCost;
            entity.AcqCost = entity.PsCardItem.UnitCost + model.AddCost;
            entity.Remarks = model.Remarks;
            entity.OldPropNo = model.OldPropNo;
            entity.OldAmount = model.OldAmount;
            entity.UpcomingOfficer = model.UpcomingOfficer;

            entity.YearModel = model.YearModel;
            entity.PlateNo = model.PlateNo;
            entity.BodyNo = model.BodyNo;
            entity.EngineNo = model.EngineNo;
            entity.ChasisNo = model.ChasisNo;
            entity.Color = model.Color;
            entity.CRN = model.CRN;
            entity.CRDate = model.CRDate;
            entity.MVFileNo = model.MVFileNo;
            entity.OrNo = model.OrNo;
            entity.OrDate = model.OrDate;
            entity.NetWeight = model.NetWeight;
            entity.InsPolicyNo = model.InsPolicyNo;
            entity.ConductionNo = model.ConductionNo;

            _db.PsCardItemExtns.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });        

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