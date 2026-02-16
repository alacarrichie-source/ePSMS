using iLgs.Models;
using System;
using System.Linq;
using static iLgs.Models.Enums;

namespace iLgs.Services.AIRs_
{
    public interface IAirItemAbstractService
    {
        string GetItemExtnName(Guid? id);
        string GetItemExtnNameByCategory(string category);
    }

    public class AirItemAbstractService : IAirItemAbstractService
    {
        private readonly AppManEntities _db;

        public AirItemAbstractService(AppManEntities db)
        {
            _db = db;
        }

        public string GetItemExtnName(Guid? id)
        {
            var category = _db.AIRItems.Where(w => w.Id == id).Select(s => s.OrderItem.ItemCode.ItemType.Code).FirstOrDefault();
            return GetItemExtnNameByCategory(category);
        }

        public string GetItemExtnNameByCategory(string category)
        {
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
                    || c == CatSemiExpendableFurnitureSupply()
                    || c == CatSemiExpendableMachinerySupply()
                    )
                {
                    itemExtnName = "ItemExtnOther";
                }
            }
            return itemExtnName;
        }
    }
}