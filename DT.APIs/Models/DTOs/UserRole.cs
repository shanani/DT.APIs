using System;
using System.Collections.Generic;

namespace DT.APIs.Models.DTOs
{
    public partial class UserRole
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int RoleId { get; set; }

        public bool IsActive { get; set; }

        public int? CreateUserId { get; set; }

        public DateTime? CreateTime { get; set; }

        public int? LastUpdateUserId { get; set; }

        public DateTime? LastUpdateTime { get; set; }

        public virtual Role Role { get; set; } = null!;

        public virtual User User { get; set; } = null!;
    }
}
