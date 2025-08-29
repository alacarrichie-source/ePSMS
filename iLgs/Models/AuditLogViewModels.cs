using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class AuditLogVM
    {
        public System.Guid Id { get; set; }
        public string TableName { get; set; }
        public string Action { get; set; }
        public string RecordId { get; set; }
        public string OldValues { get; set; }
        public string NewValues { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
        public string IpAddress { get; set; }
        public int DetailsCount { get; set; }

        // Formatted properties for display
        public string ChangedAtFormatted => UpdatedDt.Value.ToString("yyyy-MM-dd HH:mm:ss");
        public string ActionIcon
        {
            get
            {
                switch (Action)
                {
                    case "INSERT": return "fa-plus-circle text-success";
                    case "UPDATE": return "fa-edit text-warning";
                    case "DELETE": return "fa-trash text-danger";
                    default: return "fa-info-circle";
                }
            }
        }
    }


    public class AuditLogDetailVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> AuditLogId { get; set; }
        public string FieldName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }        

        // For display
        public string ChangeDescription
        {
            get
            {
                if (string.IsNullOrEmpty(OldValue)) return $"Set to: {NewValue}";
                if (string.IsNullOrEmpty(NewValue)) return $"Removed: {OldValue}";
                return $"{OldValue} → {NewValue}";
            }
        }
    }
}