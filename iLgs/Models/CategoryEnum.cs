using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
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
}