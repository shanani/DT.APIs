namespace DT.APIs.Models
{
    public class PermissionModel
    {
        public int PermissionID { get; set; }
        public string Permission { get; set; }
        public string Module { get; set; }
        public string Action { get; set; }
        public string ActionType { get; set; }
        public string ProjectName { get; set; }
    }
}
