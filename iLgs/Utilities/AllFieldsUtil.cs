using iLgs.Models;
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
                if (c == CatLandsProp())
                {
                    retval = CategoryGroup.LAND;
                }
                else if (
                    c == CatMedicalSupply()
                    || c == CatAgriculturalSupply()
                    || c == CatAnimalSupplies()
                    || c == CatConstructionMaterialsSupply()
                    || c == CatOfficeSupplies()
                    || c == CatAccountableFormsSupply()
                    || c == CatNonAccountableFornsSupply()
                    || c == CatMilitarySupply()
                    || c == CatOtherSupplies())
                {
                    retval = CategoryGroup.OTHERS; // with Multiples
                }
                else if (
                    c == CatMachineriesProp()
                    || c == CatOtherProperties()
                    || c == CatTransportationProp()
                    || c == CatFurnituresProp())
                {
                    // vehicles
                    if (c == CatTransportationProp()
                        || (itemTypeCode == "E" && (
                            itemCode.Contains("05-80")
                            || itemCode.Contains("05-90.1.1")
                            || itemCode.Contains("05-90.2.1")
                            || itemCode.Contains("05-90.3.1")
                            || itemCode.Contains("05-90.4.1")
                            || itemCode.Contains("05-90.5.1")
                            || itemCode.Contains("05-100.1")
                        ))
                      )
                    {
                        retval = CategoryGroup.OTHERS_B;
                    }
                    else
                    {
                        retval = CategoryGroup.OTHERS_A; // w/o Multiples
                    }
                }
                else if (c == CatFoodsSupply()
                    || c == CatWelfareGoodsSupply())
                {
                    retval = CategoryGroup.OTHERS_C; // multiples only
                }
                else if (c == CatDrugsSupply())
                {
                    retval = CategoryGroup.DRUGS;
                }
                //else if (c == CatRepairSupply())
                else if (c == CatFuelOilSupply())
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

        //public static string GetPartialView(ItemCode itemCode)
        //{
        //    string partialName = "";
        //    if (string.IsNullOrWhiteSpace(itemCode.PartialPage))
        //    {
        //        if (!string.IsNullOrWhiteSpace(itemCode.ItemType.PartialPage))
        //        {
        //            partialName = itemCode.ItemType.PartialPage;
        //        }
        //    }
        //    else
        //    {
        //        partialName = itemCode.PartialPage;
        //    }
        //    return partialName;
        //}

        //public static string GetPartialField(string itemTypeCode, string itemCode)
        //{
        //    string partialName = "";
        //    var value = AllFieldsUtil.GetCategoryGroup(itemTypeCode, itemCode);
        //    if (value == CategoryGroup.LAND)
        //    {
        //        partialName = "_FieldLand";
        //    }
        //    else if (value == CategoryGroup.OTHERS)
        //    { 
        //        partialName = "_FieldBrand";                
        //    }
        //    else if (value == CategoryGroup.OTHERS_A)
        //    {
        //        partialName = "_FieldBrand_A";
        //    }
        //    else if (value == CategoryGroup.OTHERS_B)
        //    {
        //        partialName = "_FieldBrand_B";
        //    }
        //    else if (value == CategoryGroup.DRUGS)
        //    {
        //        if (itemCode.Contains("-5.1.")) // Alcoh1ol
        //        {
        //            partialName = "_FieldAlcohol";
        //        }
        //        else
        //        {
        //            partialName = "_FieldDrugs";
        //        }
        //    }
        //    else if (value == CategoryGroup.SERIAL_A)
        //    {
        //        partialName = "_FieldSerial_A";
        //    }
        //    else if (value == CategoryGroup.SERIAL_B)
        //    {
        //        partialName = "_FieldSerial_B";
        //    }
        //    else if (value == CategoryGroup.SERIAL_C)
        //    {
        //        partialName = "_FieldSerial_C";
        //    }
        //    else if (value == CategoryGroup.SERIAL)
        //    {
        //        partialName = "_FieldSerial";
        //    }
        //    return partialName;
        //}

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

        public static AllField NewAllField(AllField model)
        {
            return new AllField()
            {
                Id = model.Id,
                AcqMode = model.AcqMode,
                InvDist = model.InvDist,
                GenericName = model.GenericName,
                DosageStrength = model.DosageStrength,
                DosageForm = model.DosageForm,
                DosageVolume = model.DosageVolume,
                Others = model.Others,
                Brand = model.Brand,
                Multipliers = model.Multipliers,
                Model_ = model.Model_,
                Area = model.Area,
                Barangay = model.Barangay,
                DateSale = model.DateSale,
                DateDonation = model.DateDonation,
                DateAcquisition = model.DateAcquisition,
                DateConstruction = model.DateConstruction,
                AreaSoldDonated = model.AreaSoldDonated,
                PricePerSqm = model.PricePerSqm,
                AcqCost = model.AcqCost,
                VendorDonor = model.VendorDonor,
                Type = model.Type,
                Dimension = model.Dimension,
                Size = model.Size,
                Weight = model.Weight,
                Materials = model.Materials,
                Capacity = model.Capacity,
                Color = model.Color,
                SerialNo = model.SerialNo,
                PropNo = model.PropNo,
                PlateNo = model.PlateNo,
                BodyNo = model.BodyNo,
                MVFileNo = model.MVFileNo,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt,
            };
        }
    }
}