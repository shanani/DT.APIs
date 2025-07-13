namespace DT.APIs.Models
{
    /// <summary>
    /// Represents a node in the permission tree.
    /// </summary>
    public class PermissionTreeNodeModel
    {
        /// <summary>
        /// The unique identifier for the permission node.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The parent identifier for the permission node.
        /// </summary>
        public string ParentID { get; set; }

        /// <summary>
        /// The description of the permission node.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Indicates whether the permission is granted.
        /// </summary>
        public int IsGranted { get; set; }

        /// <summary>
        /// The unique identifier for the permission.
        /// </summary>
        public int? PermissionID { get; set; }

        /// <summary>
        /// Indicates if the permission node is a menu item.
        /// </summary>
        public int IsMenu { get; set; }

        /// <summary>
        /// The tag associated with the permission node.
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// The sort order of the permission node.
        /// </summary>
        public int Sort { get; set; }

        /// <summary>
        /// The role ID associated with the permission.
        /// </summary>
        public int RoleID { get; set; }
    }
}