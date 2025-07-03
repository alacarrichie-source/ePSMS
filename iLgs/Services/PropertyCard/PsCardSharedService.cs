using iLgs.Models;
using System;
using System.Linq;
using static iLgs.Models.Enums;

namespace iLgs.Services.PropertyCard
{
    public interface IPsCardSharedService
    {
        string GetItemExtnNameByItmExtnId(Guid? id);
        string GetItemExtnName(Guid? id);
    }

    public class PsCardSharedService : IPsCardSharedService
    {
        private readonly AppManEntities _db;

        public PsCardSharedService(AppManEntities db)
        {
            _db = db;
        }

        public string GetItemExtnNameByItmExtnId(Guid? id)
        {
            var cardItem = _db.PsCardItems.FirstOrDefault(f => f.PsCardItemExtns.Any(a => a.Id == id));
            if (cardItem == null)
            {
                return string.Empty;
            }

            return GetItemExtnName(cardItem.Id);
        }

        public string GetItemExtnName(Guid? id)
        {
            var category = _db.PsCardItems.Where(w => w.Id == id).Select(s => s.PsCard.ItemCode.ItemType.Code).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(category))
            {
                return "";
            }

            string itemExtnName = "";
            if (Enum.TryParse(category, out Category c))
            {
                if (c == CatLandsProp())
                {
                    itemExtnName = "ItemExtnLand";
                }
                else if (c == CatBuildingsProp())
                {
                    itemExtnName = "ItemExtnBldg";
                }
                else if (c == CatTransportationProp())
                {
                    itemExtnName = "ItemExtnVehicle";
                }
                else if (c == CatMachineriesProp()
                    || c == CatFurnituresProp()
                    || c == CatOtherProperties()
                    || c == CatMedicalSupply()
                    || c == CatAgriculturalSupply()
                    || c == CatAnimalSupplies()
                    || c == CatConstructionMaterialsSupply()
                    || c == CatOfficeSupplies()
                    || c == CatAccountableFormsSupply()
                    || c == CatNonAccountableFornsSupply()
                    || c == CatMilitarySupply()
                    || c == CatOtherSupplies()
                    || c == CatDrugsSupply()
                    || c == CatRepairSupply()
                    )
                {
                    itemExtnName = "ItemExtnOther";
                }
            }
            return itemExtnName;
        }
    }
}