using System;
using System.Collections.Generic;

namespace DT.APIs.Models
{
    public partial class User
    {
        public int Id { get; set; }

        public string Code { get; set; } = null!;

        public string? EnglishDescription { get; set; }

        public string? ArabicDescription { get; set; }

        public string? Department { get; set; }

        public bool? IsAdmin { get; set; }

        public bool? IsLocked { get; set; }

        public bool? IsSmsreceiver { get; set; }

        public bool? IsDistrictAdmin { get; set; }

        public bool? IsZoneAdmin { get; set; }

        public bool? IsSegmentAdmin { get; set; }

        public bool? IsVendorAdmin { get; set; }

        public bool? IsDomainAdmin { get; set; }

        public bool? IsSmsadmin { get; set; }

        public string? Email { get; set; }

        public string? Mobile { get; set; }

        public string? Tel { get; set; }

        public byte[]? Photo { get; set; }

        public DateTime? LastAdinfoUpdateTime { get; set; }

        public string? Tag { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public int? CreateUserId { get; set; }

        public DateTime? CreateTime { get; set; }

        public int? LastUpdateUserId { get; set; }

        public DateTime? LastUpdateTime { get; set; }

        public bool? IsWelcomeEmailSent { get; set; }

        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    }
}
