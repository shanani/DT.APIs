namespace DT.APIs.Models
{
    public class ProjectModel
    {
        public int ID { get; set; }
        public string Code { get; set; }
        public string EnglishName { get; set; }
        public string ArabicName { get; set; }
        public string Url { get; set; }
        public bool IsActive { get; set; }
        public int Sort { get; set; }
        public string Tag { get; set; }
    }
}
