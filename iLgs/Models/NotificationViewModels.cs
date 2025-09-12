using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class NotificationVM : Notification
    {

    }

    [MetadataType(typeof(Notification.Metadata))]
    public partial class Notification
    {
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }

            [Required]
            public string Name { get; set; }

            [Required]
            public string Description { get; set; }

            [Display(Name = "Inserted By")]
            public string InsertedBy { get; set; }

            [Display(Name = "Inserted Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> InsertedDt { get; set; }

            [Display(Name = "Updated By")]
            public string UpdatedBy { get; set; }

            [Display(Name = "Updated Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> UpdatedDt { get; set; }
        }
    }

    public class NotificationMessageVM : NotificationMessage
    {

    }

    [MetadataType(typeof(NotificationMessage.Metadata))]
    public partial class NotificationMessage
    {
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public Nullable<System.Guid> NotificationId { get; set; }

            [Required]
            public string Message { get; set; }

            [Display(Name = "Created By")]
            public string CreatedBy { get; set; }

            [Display(Name = "Updated Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> CreatedDt { get; set; }
        }
    }

    public class NotificationMessageStatuVM: NotificationMessageStatu
    {

    }

    [MetadataType(typeof(NotificationMessageStatu.Metadata))]
    public partial class NotificationMessageStatu
    {
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public Nullable<System.Guid> NotificationMessageId { get; set; }
            public Nullable<System.Guid> NotificationUserId { get; set; }

            [Display(Name = "Is Read?")]
            public Nullable<bool> IsRead { get; set; }

            [Display(Name = "Read Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> IsReadAt { get; set; }

            [Display(Name = "Updated By")]
            public string UpdatedBy { get; set; }

            [Display(Name = "Updated Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> UpdatedDt { get; set; }
        }        
    }

    public class NotificationUserVM : NotificationUser
    {
        [Required]
        [Display(Name = "User Name")]
        public string UserName { get; set; }

        [Display(Name = "Department")]
        public string UserDepartment { get; set; }

        [Display(Name = "Full Name")]
        public string UserFullName { get; set; }
    }

    [MetadataType(typeof(NotificationUser.Metadata))]
    public partial class NotificationUser
    {
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public Nullable<System.Guid> NotificationId { get; set; }

            [Display(Name = "Updated Id")]
            public string UserId { get; set; }

            [Display(Name = "InsertedBy")]
            public string InsertedBy { get; set; }

            [Display(Name = "Inserted Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> InsertedDt { get; set; }

            [Display(Name = "Updated By")]
            public string UpdatedBy { get; set; }

            [Display(Name = "Updated Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> UpdatedDt { get; set; }
        }
    }
}