using iLgs.Models;
using System;
using System.Data.Entity;
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

        public string GetPartialPage(string code)
        {
            string partialPage = string.Empty;
            var itemCode = _db.ItemCodes.Include(i => i.ItemType).FirstOrDefault(f => f.Code == code);
            if (itemCode != null)
            {
                if (!string.IsNullOrWhiteSpace(itemCode.PartialPage))
                {
                    partialPage = itemCode.PartialPage;
                }
                else {
                    int lastDotIndex = code.LastIndexOf('.');
                    if (lastDotIndex >= 0)
                    {
                        partialPage = GetPartialPage(code.Substring(0, lastDotIndex));
                    }
                    else
                    {
                        partialPage = itemCode.ItemType.PartialPage;
                    }
                }
            }

            return partialPage;
        }

        public string GetPartialPageDescription(string code)
        {
            string partialPageDescription = string.Empty;
            var partialpage = GetPartialPage(code);
            if (!string.IsNullOrWhiteSpace(partialpage))
            {
                var codextn = _db.Codextns.FirstOrDefault(f => f.CodeMast.Code == "REQUIRED-FIELDS" && f.Desc2 == partialpage);
                if (codextn != null)
                {
                    partialPageDescription = codextn.Description;
                }
            }

            return partialPageDescription;
        }

        public string GetItemExtnName(Guid? id)
        {
            var psCardItem = _db.PsCardItems.Include(i => i.PsCard.ItemCode.ItemType).Where(w => w.Id == id).FirstOrDefault();
            if (psCardItem == null)
            {
                return "";
            }

            string partialPage = GetPartialPageDescription(psCardItem.PsCard.ItemCode.Code);
            string itemExtnName = "";
            if (partialPage.Contains("Vehicle"))
            {
                itemExtnName = "ItemExtnVehicle";
            }
            else
            {
                var category = psCardItem.PsCard.ItemCode.ItemType.Code;
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
                        || c == CatFuelOilSupply()
                        || c == CatSemiExpendableFurnitureSupply()
                        || c == CatSemiExpendableMachinerySupply()
                        )
                    {
                        itemExtnName = "ItemExtnOther";
                    }
                }
            }

            return itemExtnName;
        }
    }
}