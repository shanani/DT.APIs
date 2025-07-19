namespace DT.APIs.Models.DTOs
{
    /// <summary>
    /// Represents the details of the first action for a user in a project.
    /// </summary>
    public class ActionDetailsModel
    {
        /// <summary>
        /// The unique identifier for the project.
        /// </summary>
        public int ProjectID { get; set; }

        /// <summary>
        /// The English name of the project.
        /// </summary>
        public string ProjectEnglishName { get; set; }

        /// <summary>
        /// The Arabic name of the project.
        /// </summary>
        public string ProjectArabicName { get; set; }

        /// <summary>
        /// The menu ID associated with the action.
        /// </summary>
        public int MenuID { get; set; }

        /// <summary>
        /// The parent menu ID.
        /// </summary>
        public int? MenuParentID { get; set; }

        /// <summary>
        /// The English description of the menu.
        /// </summary>
        public string MenuEnglishDescription { get; set; }

        /// <summary>
        /// The Arabic description of the menu.
        /// </summary>
        public string MenuArabicDescription { get; set; }

        /// <summary>
        /// The sort order of the menu.
        /// </summary>
        public int MenuSort { get; set; }

        /// <summary>
        /// The tag associated with the menu.
        /// </summary>
        public string MenuTag { get; set; }

        /// <summary>
        /// The module ID associated with the action.
        /// </summary>
        public int ModuleID { get; set; }

        /// <summary>
        /// The English description of the module.
        /// </summary>
        public string ModuleEnglishDescription { get; set; }

        /// <summary>
        /// The Arabic description of the module.
        /// </summary>
        public string ModuleArabicDescription { get; set; }

        /// <summary>
        /// The sort order of the module.
        /// </summary>
        public int ModuleSort { get; set; }

        /// <summary>
        /// The tag associated with the module.
        /// </summary>
        public string ModuleTag { get; set; }

        /// <summary>
        /// The unique identifier for the action.
        /// </summary>
        public string ActionID { get; set; }

        /// <summary>
        /// The controller ID associated with the module.
        /// </summary>
        public string ControllerID { get; set; }

        /// <summary>
        /// The URL associated with the action.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Indicates if the action is a menu item.
        /// </summary>
        public bool IsMenu { get; set; }
    }
}