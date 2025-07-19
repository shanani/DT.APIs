namespace DT.APIs.Models.DTOs
{
    /// <summary>
    /// Represents a domain associated with a user.
    /// </summary>
    public class DomainModel
    {
        public int UserID { get; set; }
        public int DomainID { get; set; }
        public string DomainName { get; set; }
        public int ProjectID { get; set; }
    }
}