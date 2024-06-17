using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class PsCodeVM
    {
        public System.Guid Id { get; set; }
        [Display(Name = "Item Kind")]
        public Nullable<System.Guid> ItemCodeId { get; set; }

        [Display(Name = "Item Kind")]        
        public string ItemCodeDesc { get; set; }

        [Display(Name = "Item No.")]
        [Required]
        public string PsNo { get; set; }

        [Display(Name = "Type")]
        [Required]
        public string PsType { get; set; }

        [Display(Name = "Type")]
        public string PsTypeDesc { get; set; }

        [Display(Name = "Item Name")]
        [Required]
        public string ItemName { get; set; }

        [Display(Name = "Description")]
        public string ItemDescription { get; set; }

        [Display(Name = "Unit")]
        //[Required]
        public string UnitMeas { get; set; }

        [Display(Name = "Reorder Point")]
        public Nullable<decimal> ReorderPoint { get; set; }

        [Display(Name = "Days To Consume")]
        public Nullable<int> DaysToConsume { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        public string ImageUrl { get; set; }
        public string FileName { get; set; }
        public string ItemCode { get; set; }
    }

    public class ItemCodeVM
    {        
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> ItemTypeId { get; set; }
        [Display(Name = "Item No.")]
        public string ItemNo { get; set; }
        [Display(Name = "Item Index No.")]
        public string ItemNoIndex { get; set; }
        [Display(Name = "Item Code")]
        public string Code { get; set; }
        public string Description { get; set; }
        [Display(Name = "Article (Y/N)")]
        public string ItemSw { get; set; }

        [Display(Name = "Consumable (Y/N)")]
        public string IsConsumable { get; set; }

        [Display(Name = "Incorporated (Y/N)")]
        public string IsIncorporated { get; set; }

        [Display(Name = "For Distribution (Y/N/O)")]
        public string ForDistribution { get; set; }

        [Display(Name = "Account Code")]
        public string AccountCode { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // Transients
        public string MainDesc { get; set; }
        public string ItemType { get; set; }
        public string ItemTypeDesc { get; set; }
        public string Account { get; set; }
        public string SubArticle { get; set; }
        public string MainDescCode { get; set; }
        public int? FieldGroupNo { get; set; }

        [Display(Name = "Is Article?")]
        public bool? ItemSwUI { get; set; }
    }
   
    public class ItemFieldVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> ItemTypeId { get; set; }        
        public string FieldNo { get; set; }
        public string FieldName { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }        
    }

    public class ItemTypeVM
    {     
        public System.Guid Id { get; set; }
        [Display(Name = "Type Code")]
        public string Code { get; set; }
        public string Description { get; set; }
        [Display(Name = "Field Group No.")]
        public int? FormulaNo { get; set; }
        public string Category { get; set; }
        [Display(Name = "Account Group Code")]
        public string GroupCode { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
        public string CategoryDesc { get; set; }
    }
}