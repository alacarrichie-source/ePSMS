using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services
{
    public interface IPsCardItemExtnService
    {
        IPsCardItemExtnVehicleService PsCardItemExtnVehicle { get; }
        IPsCardItemExtnOtherService PsCardItemExtnOther { get; }        
        IQueryable<T> GetCardItemExtnForIcsPars<T>(Guid? psCardItemId) where T : PsCardItemExtn;
        IQueryable<T> GetCardItemExtnForIssuance<T>(Guid? psCardItemId) where T : PsCardItemExtn;
        IQueryable<T> GetCardItemExtnForIssuanceSelection<T>(Guid? psCardItemId, string mode) where T : PsCardItemExtn;

        IQueryable<PsCardItemExtn> GetCardItemExtnForIcsParsByType(Guid? psCardItemId);
        IQueryable<PsCardItemExtn> GetCardItemExtnForIssuanceByType(Guid? psCardItemId);
        IQueryable<PsCardItemExtn> GetCardItemExtnForVehicleIssuanceSelection(Guid? psCardItemId, string mode);
        IQueryable<PsCardItemExtn> GetCardItemExtnForOtherIssuanceSelection(Guid? psCardItemId, string mode);
        IQueryable<PsCardItemExtn> GetCardItemExtnForLandIssuanceSelection(Guid? psCardItemId, string mode);
        IQueryable<PsCardItemExtn> GetCardItemExtnForBldgIssuanceSelection(Guid? psCardItemId, string mode);

        string GetEndSeries(string startSeries, Guid? itemId);
        string GetEndSeries(string startSeries, int qty);
    }


    public class PsCardItemExtnService : IPsCardItemExtnService
    {
        private readonly AppManEntities _db;        
        private IPsCardItemExtnVehicleService _psCardItemExtnVehicleService;
        private IPsCardItemExtnOtherService _psCardItemExtnOtherService;        

        public PsCardItemExtnService(AppManEntities db)
        {
            _db = db;
            _psCardItemExtnVehicleService = new PsCardItemExtnVehicleService(_db);
            _psCardItemExtnOtherService = new PsCardItemExtnOtherService(_db);
        }

        public IPsCardItemExtnVehicleService PsCardItemExtnVehicle { get { return _psCardItemExtnVehicleService = _psCardItemExtnVehicleService ?? new PsCardItemExtnVehicleService(_db); } }
        public IPsCardItemExtnOtherService PsCardItemExtnOther { get { return _psCardItemExtnOtherService = _psCardItemExtnOtherService ?? new PsCardItemExtnOtherService(_db); } }

        public IQueryable<T> GetCardItemExtnForIcsPars<T>(Guid? psCardItemId) where T : PsCardItemExtn
        {
            var data = _db.PsCardItemExtns.OfType<T>().AsNoTracking()
                        .Where(w => !w.IcsParItems.Any(a => a.PsCardItemExtnId == w.Id)
                            && (w.PsCardItemId == psCardItemId ||
                                // Get items from same PO of different CardItem (Due to Transfer of Item)
                                _db.PsCardItems.Any(a => a.PoNo == w.PsCardItem.PoNo && a.PoDate == w.PsCardItem.PoDate
                                    && a.DeptId == w.PsCardItem.DeptId && a.PsCardId == w.PsCardItem.PsCardId && a.Id != psCardItemId)
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

        public IQueryable<T> GetCardItemExtnForIssuanceSelection<T>(Guid? psCardItemId, string mode) where T : PsCardItemExtn
        {
            var data = _db.PsCardItemExtns.OfType<T>().AsNoTracking()
                        .Include(i => i.PsCardItemTransactions)
                        .Where(w => w.PsCardItemId == psCardItemId)
                        .AsQueryable();
            if (mode == "EDIT")
            {
                data = data.Where(w => !w.PsCardItemTransactions.Any());
            }
            return data;
        }
        
        public IQueryable<PsCardItemExtn> GetCardItemExtnForVehicleIssuanceSelection(Guid? psCardItemId, string mode)
        {
            return GetCardItemExtnForIssuanceSelection<PsCardItemExtnVehicle>(psCardItemId, mode);            
        }

        public IQueryable<PsCardItemExtn> GetCardItemExtnForOtherIssuanceSelection(Guid? psCardItemId, string mode)
        {
            return GetCardItemExtnForIssuanceSelection<PsCardItemExtnOther>(psCardItemId, mode);
        }

        public IQueryable<PsCardItemExtn> GetCardItemExtnForLandIssuanceSelection(Guid? psCardItemId, string mode)
        {
            return GetCardItemExtnForIssuanceSelection<PsCardItemExtnLand>(psCardItemId, mode);
        }

        public IQueryable<PsCardItemExtn> GetCardItemExtnForBldgIssuanceSelection(Guid? psCardItemId, string mode)
        {
            return GetCardItemExtnForIssuanceSelection<PsCardItemExtnBuilding>(psCardItemId, mode);
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