namespace DT.APIs.Models.DTOs
{
    public class ZoneModel
    {
        public int UserID { get; set; }
        public int ZoneID { get; set; }
        public string ZoneName { get; set; }
        public int DistrictID { get; set; }
        public string DistrictName { get; set; }
        public int VendorID { get; set; }
        public string VendorName { get; set; }
        public int? ProjectID { get; set; }

    }
}
