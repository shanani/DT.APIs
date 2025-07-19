namespace DT.APIs.Models.DTOs
{
    public class ActionsModel
    {
        public int ProjectID { get; set; }
        public string ProjectEnglishName { get; set; }
        public string ProjectArabicName { get; set; }
        public int MenuID { get; set; }
        public int? MenuParentID { get; set; } // Nullable to handle cases where this might be null
        public string MenuEnglishDescription { get; set; }
        public string MenuArabicDescription { get; set; }
        public int? MenuSort { get; set; } // Nullable to handle cases where this might be null
        public string MenuTag { get; set; }
        public int ModuleID { get; set; }
        public string ModuleEnglishDescription { get; set; }
        public string ModuleArabicDescription { get; set; }
        public int ModuleSort { get; set; }
        public string ModuleTag { get; set; }
        public string ActionID { get; set; }
        public string ControllerID { get; set; }
        public string Url { get; set; }
        public bool IsMenu { get; set; }
    }
}
