namespace DT.APIs.Models.DTOs
{
    public class SegmentModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public bool IsSegmentAdmin { get; set; }
        public bool IsGranted { get; set; }
        public int? ProjectID { get; set; }
    }
}
