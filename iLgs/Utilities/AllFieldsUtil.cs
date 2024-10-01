using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Utilities
{
    public static class AllFieldsUtil
    {
        public static CategoryGroup GetCategoryGroup(string itemTypeCode, string itemCode)
        {
            CategoryGroup retval = CategoryGroup.NONE;
            if (Enum.TryParse(itemTypeCode, out Category c))
            {
                if (c == CatLands())
                {
                    retval = CategoryGroup.LAND;
                }
                else if (c == CatMachineries()
                    || c == CatTransportations()
                    || c == CatFurnitures()
                    || c == CatOtherProperties()
                    || c == CatMedicals()
                    || c == CatAgriculturals()
                    || c == CatAnimalSupplies()
                    || c == CatConstructionMaterials()
                    || c == CatOfficeSupplies()
                    || c == CatAccountableForms()
                    || c == CatNonAccountableForns()
                    || c == CatMilitaries()
                    || c == CatOtherSupplies())
                {
                    retval = CategoryGroup.OTHERS;
                }
                else if (c == CatDrugs())
                {
                    retval = CategoryGroup.DRUGS;
                }
                else if (c == CatRepairs())
                {
                    if (itemCode.Contains("-1.1") || itemCode.Contains("-2.1") || itemCode.Contains("-3.1") || itemCode.Contains("-4.1")
                        || itemCode.Contains("-7.1") || itemCode.Contains("-8.1") || itemCode.Contains("-9.1"))
                    {
                        retval = CategoryGroup.SERIAL_A;
                    }
                    else if (itemCode.Contains("-5.1") || itemCode.Contains("-6.1") || itemCode.Contains("-99.1"))
                    {
                        retval = CategoryGroup.SERIAL_B;
                    }
                    else if (itemCode.Contains("-1.2") || itemCode.Contains("-2.2") || itemCode.Contains("-3.2") || itemCode.Contains("-4.1")
                        || itemCode.Contains("-7.2") || itemCode.Contains("-8.2") || itemCode.Contains("-9.2"))
                    {
                        retval = CategoryGroup.SERIAL_C;
                    }
                    else
                    {
                        retval = CategoryGroup.SERIAL;
                    }
                }
            }
            return retval;
        }

        public static string GetPartialField(string itemTypeCode, string itemCode)
        {
            string partialName = "";
            var value = AllFieldsUtil.GetCategoryGroup(itemTypeCode, itemCode);
            if (value == CategoryGroup.LAND)
            {
                partialName = "_FieldLand";
            }
            else if (value == CategoryGroup.OTHERS)
            {
                partialName = "_FieldBrand";
            }
            else if (value == CategoryGroup.DRUGS)
            {
                if (itemCode.Contains("-5.1.")) // Alcoh1ol
                {
                    partialName = "_FieldAlcohol";
                }
                else
                {
                    partialName = "_FieldDrugs";
                }
            }
            else if (value == CategoryGroup.SERIAL_A)
            {
                partialName = "_FieldSerial_A";
            }
            else if (value == CategoryGroup.SERIAL_B)
            {
                partialName = "_FieldSerial_B";
            }
            else if (value == CategoryGroup.SERIAL_C)
            {
                partialName = "_FieldSerial_C";
            }
            else if (value == CategoryGroup.SERIAL)
            {
                partialName = "_FieldSerial";
            }
            return partialName;
        }

        public static string GetPartialItemField(string itemTypeCode, string itemCode)
        {
            string partialName = "";
            var value = AllFieldsUtil.GetCategoryGroup(itemTypeCode, itemCode);
            if (value == CategoryGroup.LAND)
            {
                partialName = "_ItemFieldLand";
            }
            else if (value == CategoryGroup.OTHERS)
            {
                partialName = "_ItemFieldBrand";
            }
            else if (value == CategoryGroup.DRUGS)
            {
                partialName = "_ItemFieldDrugs";
            }
            else if (value == CategoryGroup.SERIAL)
            {
                partialName = "_ItemFieldSerial";
            }
            return partialName;
        }
    }
}