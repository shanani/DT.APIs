namespace DT.APIs.Models
{
    /// <summary>
    /// Represents the status of a workflow.
    /// </summary>
    public class WorkflowStatusModel
    {
        public decimal Index { get; set; }
        public decimal WorkflowID { get; set; }
        public string WorkflowCode { get; set; }
        public int PatternID { get; set; }
        public string PatternName { get; set; }
        public int StepID { get; set; }
        public string StepName { get; set; }
        public int RoleID { get; set; }
        public string RoleName { get; set; }
        public bool IsFirstStep { get; set; }
        public bool IsPreview { get; set; }
        public bool IncludeAttachements { get; set; }
        public bool IncludeComment { get; set; }
        public bool AllowEdit { get; set; }
        public int DataSensitivityLevel { get; set; }
        public DateTime LastTransactionDate { get; set; }
        public string LastComment { get; set; }
        public int TotalActions { get; set; }
        public bool IsUserPending { get; set; }
    }
}