namespace DT.APIs.Models.DTOs
{
    /// <summary>
    /// Represents a vendor associated with a user.
    /// </summary>
    public class VendorModel
    {
        public int UserID { get; set; }
        public int VendorID { get; set; }
        public string VendorName { get; set; }
        public int ProjectID { get; set; }
    }
}