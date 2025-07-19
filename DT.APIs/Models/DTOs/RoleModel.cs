namespace DT.APIs.Models.DTOs
{
    public class RoleModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int? ProjectID { get; set; }
        public bool IsGranted { get; set; }
    }
}
