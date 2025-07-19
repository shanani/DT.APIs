using System;
using System.Collections.Generic;

namespace DT.APIs.Models.DTOs
{
    public partial class Role
    {
        public int Id { get; set; }

        public string EnglishDescription { get; set; } = null!;

        public string? ArabicDescription { get; set; }

        public bool? IsZoneAdmin { get; set; }

        public bool? IsSegmentAdmin { get; set; }

        public bool? IsVendorAdmin { get; set; }

        public bool? IsDomainAdmin { get; set; }

        public bool? IsSmsadmin { get; set; }

        public bool? IsDistrictAdmin { get; set; }

        public string? Tag { get; set; }

        public int? CreateUserId { get; set; }

        public DateTime? CreateTime { get; set; }

        public int? LastUpdateUserId { get; set; }

        public DateTime? LastUpdateTime { get; set; }

        public int? ProjectId { get; set; }

        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
