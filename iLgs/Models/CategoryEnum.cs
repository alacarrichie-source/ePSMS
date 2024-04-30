using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public enum Category
    {
        L, // Land
        S, // Land Improvements
        I, // Infrastructure
        B, // Buildings and Other Structures
        E, // Machinery and Equipment
        T, // Transportation Equipment
        U, // Furniture, Fixtures and Books
        F, // Food Supplies
        W, // Welfare Goods for Distribution
        D, // "Drugs and Medicines for Distribution / Drugs and Medicines Inventory"
        M, // Medical, Dental, and Laboratory Supplies for Distribution / Medical, Dental and Laboratory Supplies Inventory
        G, // Agricultural and Marine Supplies for Distribution / Agricultural and Marine Supplies Inventory
        V, // Animal Supplies
        C, // Construction Materials Inventory
        O, // Office Supplies
        A, // Accountable Forms, Plates and Stickers
        N, // Non-Accountable Forms 
        P, // Military, Police and Traffic Supplies
        R, // Repairs and Replacements
        X // Other Supplies and Materials for Distribution / Other Supplies and Materials Inventory
    }
}