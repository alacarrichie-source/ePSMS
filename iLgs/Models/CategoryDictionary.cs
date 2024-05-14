using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class CategoryDictionary
    {
        private Dictionary<string, string> dataDictionary;

        //// Constructor to initialize the data dictionary with some initial values
        //L, // Land
        //S, // Land Improvements
        //I, // Infrastructure
        //B, // Buildings and Other Structures
        //E, // Machinery and Equipment
        //T, // Transportation Equipment
        //U, // Furniture, Fixtures and Books
        //F, // Food Supplies
        //W, // Welfare Goods for Distribution
        //D, // "Drugs and Medicines for Distribution / Drugs and Medicines Inventory"
        //M, // Medical, Dental, and Laboratory Supplies for Distribution / Medical, Dental and Laboratory Supplies Inventory
        //G, // Agricultural and Marine Supplies for Distribution / Agricultural and Marine Supplies Inventory
        //V, // Animal Supplies
        //C, // Construction Materials Inventory
        //O, // Office Supplies
        //A, // Accountable Forms, Plates and Stickers
        //N, // Non-Accountable Forms 
        //P, // Military, Police and Traffic Supplies
        //R, // Repairs and Replacements
        //X // Other Supplies and Materials for Distribution / Other Supplies and Materials Inventory
        public CategoryDictionary()
        {
                dataDictionary = new Dictionary<string, string>()
            {
                { "L", "LAND" },
                { "L", "LAND IMPROVEMENT" },
                { "i", "INFRASTRUCTURE" },
                { "B", "BULDINGS" },
                { "E", "MACHINERY" },
                { "T", "TRANSPORTATION" },
                { "U", "FURNITURE AND FIXTURES" },
                { "F", "FOOD SUPPLIES" },
                { "W", "WELFARE GOODS" },
                { "D", "DRUGS" },
                { "M", "MEDICAL" },
                { "i", "AGRICULLTURAL" }
            };
        }

        public void AddData(string key, string value)
        {
            dataDictionary.Add(key, value);
        }

        public string GetData(string key)
        {
            if (dataDictionary.ContainsKey(key))
                return dataDictionary[key];
            else
                throw new KeyNotFoundException("Key not found in dictionary.");
        }

        public bool ContainsKey(string key)
        {
            return dataDictionary.ContainsKey(key);
        }

        public void RemoveData(string key)
        {
            dataDictionary.Remove(key);
        }

        public void ClearData()
        {
            dataDictionary.Clear();
        }

        public void PrintData()
        {
            foreach (KeyValuePair<string, string> entry in dataDictionary)
            {
                Console.WriteLine("Name: " + entry.Key + ", Age: " + entry.Value);
            }
        }
    }
}