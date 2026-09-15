using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public static class Enums
    {
        public enum AirGroup
        {
            INSPECTION = 1,
            ACCEPTANCE = 2,
            SERIAL = 3,
            ADMIN = 4,
            NONE = 5
        }

        public enum IcsValue
        {
            SPLV = 1,
            SPHV = 2
        }

        public enum CustodianAccountGroup
        {
            STOCK = 1,
            PPE = 2,
            VEHICLE = 3,
            LAND = 4,
            BUILDING = 5
        }

        public enum AccountGroup
        {
            ALL = 0,
            SUPPLIES = 1,
            PPE = 2,
            VEHICLE = 3,
            LAND = 4,
            BUILDING = 5,
            REGISTRY = 6
        }

        public enum CategoryGroup
        {            
            LAND,
            DRUGS,
            SERIAL,
            SERIAL_A,
            SERIAL_B,
            SERIAL_C,
            SERIAL_D,
            OTHERS, // Brand w/ multiples
            OTHERS_A, // Brand w/o multiples 
            OTHERS_B, // Brand vehicles
            OTHERS_C, // Multiples only
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
            H, // Land Improvement
            I, // Infrastructure
            J, // Semi-Expendable Furniture, Fixtures and Books -- new
            K, // Chemical and Filtering Supplies
            L, // Land
            M, // Medical, Dental, and Laboratory Supplies for Distribution / Medical, Dental and Laboratory Supplies Inventory
            N, // Non-Accountable Forms 
            O, // Office Supplies
            P, // Military, Police and Traffic Supplies
            Q, // Fuel Oils // new
            //R, // Repairs and Replacements
            R, // Textbooks and Instructional Materials
            //S, // Land Improvements
            S, // Semi-Expendable Machinery and Equipment
            T, // Transportation Equipment
            U, // Furniture, Fixtures and Books
            V, // Animal Supplies
            W, // Welfare Goods for Distribution        
            X, // Other Supplies and Materials for Distribution / Other Supplies and Materials Inventory
            //Y, // Construction in Progress
            //Z // Other Property plant and equipment
            Y, // Other Property, Plant and Equipment
            Z, // Construction in Progress
        }


            //A Accountable Forms, Plates and Stickers S
            //B Buildings and Other Structures P
            //C Construction Materials Inventory    S
            //D   Drugs and Medicines Inventory / Drugs and Medicines for Distribution S
            //E Machinery and Equipment P
            //F   Food Supplies   S
            //G   Agricultural and Marine Supplies Inventory / Agricultural and Marine Supplies for Distribution S
            //I Infrastructure Assets P
            //L Land    P
            //M   Medical, Dental and Laboratory Supplies Inventory / Medical, Dental, and Laboratory Supplies for Distribution S
            //N Non-Accountable Forms   S
            //O   Office Supplies S
            //P   Military, Police and Traffic Supplies   S
            //R   Repairs and Replacements S
            //S Land Improvements P
            //T Transportation Equipment P
            //U Furniture, Fixtures and Books   P
            //V   Animal Supplies S
            //W   Welfare Goods for Distribution S
            //X Other Supplies and Materials Inventory / Other Supplies and Materials for Distribution S
            //Y Construction in Progress P
            //Z Other Property, Plant and Equipment P


        public static Category CatAccountableFormsSupply() => Category.A;
        public static Category CatBuildingsProp() => Category.B;        
        public static Category CatConstructionMaterialsSupply() => Category.C;        
        public static Category CatDrugsSupply() => Category.D;        
        public static Category CatMachineriesProp() => Category.E;
        public static Category CatFoodsSupply() => Category.F;
        public static Category CatAgriculturalSupply() => Category.G;
        public static Category CatLandImprovementsProp() => Category.H; // new
        public static Category CatInfrastructuresProp() => Category.I;
        public static Category CatSemiExpendableFurnitureSupply() => Category.J; // new
        public static Category CatChemicalSupply() => Category.K;
        public static Category CatLandsProp() => Category.L;
        public static Category CatMedicalSupply() => Category.M;
        public static Category CatNonAccountableFornsSupply() => Category.N;
        public static Category CatOfficeSupplies() => Category.O;        
        public static Category CatMilitarySupply() => Category.P;
        public static Category CatFuelOilSupply() => Category.Q; // new
        //public static Category CatRepairSupply() => Category.R;
        public static Category CatTextBookSupply() => Category.R; // mod
        //public static Category CatLandImprovementsProp() => Category.S;
        public static Category CatSemiExpendableMachinerySupply() => Category.S; // mod
        public static Category CatTransportationProp() => Category.T;
        public static Category CatFurnituresProp() => Category.U;
        public static Category CatAnimalSupplies() => Category.V;
        public static Category CatWelfareGoodsSupply() => Category.W;
        public static Category CatOtherSupplies() => Category.X;
        public static Category CatOtherProperties() => Category.Y;
        public static Category CatConstructionInProgressProp() => Category.Z;
        
        //public static Category CatConstructionInProgressProp() => Category.Y;
        //public static Category CatOtherProperties() => Category.Z;

        public enum Module
        {
            CARD,
            RIS,
            REQUEST,
            ORDER,
            AIR
        }

        public enum Mode
        {
            ADD,
            EDIT,
            DELETE,
            POST
        }

        public enum CardCategory
        {
            PROPERTY = 1,
            STOCK = 2, 
            SE = 3, // SEMI-EXPENDABLE
            CONSUMABLE = 4
        }
    }
}