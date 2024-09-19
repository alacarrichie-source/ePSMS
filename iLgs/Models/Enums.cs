using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public static class Enums
    {
        public enum CategoryGroup
        {
            LAND,
            DRUGS,
            SERIAL,
            OTHERS,
            NONE
        }

        public enum Category
        {
            A, // Accountable Forms, Plates and Stickers
            B, // Buildings and Other Structures
            C, // Construction Materials Inventory                        
            D, // "Drugs and Medicines for Distribution / Drugs and Medicines Inventory"
            E, // Machinery and Equipment
            F, // Food Supplies
            G, // Agricultural and Marine Supplies for Distribution / Agricultural and Marine Supplies Inventory
            I, // Infrastructure
            L, // Land
            M, // Medical, Dental, and Laboratory Supplies for Distribution / Medical, Dental and Laboratory Supplies Inventory
            N, // Non-Accountable Forms 
            O, // Office Supplies
            P, // Military, Police and Traffic Supplies
            R, // Repairs and Replacements
            S, // Land Improvements
            T, // Transportation Equipment
            U, // Furniture, Fixtures and Books
            V, // Animal Supplies
            W, // Welfare Goods for Distribution        
            X, // Other Supplies and Materials for Distribution / Other Supplies and Materials Inventory
            Y, // Construction in Progress
            Z // Other Property plant and equipment
        }

        public static Category CatAccountableForms() => Category.A;
        public static Category CatBuildings() => Category.B;        
        public static Category CatConstructionMaterials() => Category.C;        
        public static Category CatDrugs() => Category.D;        
        public static Category CatMachineries() => Category.E;
        public static Category CatFoodSupplies() => Category.F;
        public static Category CatAgriculturals() => Category.G;
        public static Category CatInfrastructures() => Category.I;
        public static Category CatLands() => Category.L;
        public static Category CatMedicals() => Category.M;
        public static Category CatNonAccountableForns() => Category.N;
        public static Category CatOfficeSupplies() => Category.O;
        public static Category CatMilitaries() => Category.P;
        public static Category CatRepairs() => Category.R;
        public static Category CatLandImprovements() => Category.S;
        public static Category CatTransportations() => Category.T;
        public static Category CatFurnitures() => Category.U;
        public static Category CatAnimalSupplies() => Category.V;
        public static Category CatWelfareGoods() => Category.W;
        public static Category CatOtherSupplies() => Category.X;
        public static Category CatConstructionInProgress() => Category.Y;
        public static Category CatOtherProperties() => Category.Z;

    }
}