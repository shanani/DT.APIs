namespace DT.APIs.Models.DTOs
{
    /// <summary>
    /// Represents a user in the system.
    /// </summary>
    /// <summary>
    /// Represents a user in the system.
    /// </summary>
    /// <summary>
    /// Represents a user in the system.
    /// </summary>
    public class UserModel
    {
        /// <summary>
        /// The unique identifier for the user.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The code associated with the user.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// The English description of the user.
        /// </summary>
        public string EnglishDescription { get; set; }

        /// <summary>
        /// The Arabic description of the user.
        /// </summary>
        public string ArabicDescription { get; set; }

        /// <summary>
        /// The department associated with the user.
        /// </summary>
        public string Department { get; set; }


        public string? FirstName { get; set; }

        public string? Title { get; set; }

        /// <summary>
        /// Indicates if the user is an admin.
        /// </summary>
        public bool IsAdmin { get; set; }

        /// <summary>
        /// Indicates if the user is locked.
        /// </summary>
        public bool IsLocked { get; set; }

        /// <summary>
        /// Indicates if the user receives SMS notifications.
        /// </summary>
        public bool IsSMSReceiver { get; set; }

        /// <summary>
        /// Indicates if the user is a zone admin.
        /// </summary>
        public bool IsZoneAdmin { get; set; }

        /// <summary>
        /// Indicates if the user is a segment admin.
        /// </summary>
        public bool IsSegmentAdmin { get; set; }

        /// <summary>
        /// Indicates if the user is a vendor admin.
        /// </summary>
        public bool IsVendorAdmin { get; set; }

        /// <summary>
        /// Indicates if the user is a domain admin.
        /// </summary>
        public bool IsDomainAdmin { get; set; }

        /// <summary>
        /// Indicates if the user is an SMS admin.
        /// </summary>
        public bool IsSMSAdmin { get; set; }

        /// <summary>
        /// The user's email address.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// The user's mobile number.
        /// </summary>
        public string Mobile { get; set; }

        /// <summary>
        /// The user's telephone number.
        /// </summary>
        public string Tel { get; set; }

        /// <summary>
        /// The user's photo.
        /// </summary>
        public byte[] Photo { get; set; }

        /// <summary>
        /// The last update time of the user's AD info.
        /// </summary>
        public DateTime? LastADInfoUpdateTime { get; set; }

        /// <summary>
        /// The tag associated with the user.
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// The expiration date of the user's account.
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// The ID of the user who created this user.
        /// </summary>
        public int? CreateUserID { get; set; }

        /// <summary>
        /// The time when this user was created.
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// The ID of the user who last updated this user.
        /// </summary>
        public int? LastUpdateUserID { get; set; }

        /// <summary>
        /// The time when this user was last updated.
        /// </summary>
        public DateTime? LastUpdateTime { get; set; }

        /// <summary>
        /// Indicates if a welcome email has been sent to the user.
        /// </summary>
        public bool IsWelcomeEmailSent { get; set; }

        public int Index { get; set; }

        public int RoleID { get; set; }

        public bool IsActive { get; set; }

    }
}