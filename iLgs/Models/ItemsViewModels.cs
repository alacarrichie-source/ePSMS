using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    [MetadataType(typeof(PsCode.Metadata))]
    public partial class PsCode
    {
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }

            [Display(Name = "Item No.")]
            [Required]
            public string PsNo { get; set; }

            [Display(Name = "Type")]
            [Required]
            public string PsType { get; set; }

            [Display(Name = "Item Name")]
            [Required]
            public string ItemName { get; set; }

            [Display(Name = "Description")]
            public string ItemDescription { get; set; }

            [Display(Name = "Unit")]
            [Required]
            public string UnitMeas { get; set; }

            [Display(Name = "Reorder Point")]
            public Nullable<decimal> ReorderPoint { get; set; }
            public string InsertedBy { get; set; }
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }
        }
    }
}