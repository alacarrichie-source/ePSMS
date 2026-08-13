using System;
using System.ComponentModel.DataAnnotations;

namespace iLgs.Models
{
    public class AccountCodeVM
    {        
        public System.Guid Id { get; set; }
        public string Type { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> Effectivity { get; set; }
        public string Description { get; set; }
        public string InsertedBy { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> UpdatedDt { get; set; }        
    }

    public class AccountCodeItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> AccountCodeId { get; set; }
        public Nullable<System.Guid> ItemTypeId { get; set; }
        public Nullable<System.Guid> ItemCodeId { get; set; }
        public string Code { get; set; }
        public string InsertedBy { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        public ItemCode ItemCode { get; set; }
        public ItemType ItemType { get; set; }        
    }
}