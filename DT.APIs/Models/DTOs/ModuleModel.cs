namespace DT.APIs.Models.DTOs
{
    public class ModuleModel
    {
        public int ID { get; set; }
        public string Code { get; set; }
        public string EnglishDescription { get; set; }
        public string ArabicDescription { get; set; }
        public int ProjectID { get; set; }
        public string ProjectEnglishName { get; set; }
        public string ProjectArabicName { get; set; }
        public bool IsActive { get; set; }
        public int Sort { get; set; }
        public string Tag { get; set; }
    }
}
