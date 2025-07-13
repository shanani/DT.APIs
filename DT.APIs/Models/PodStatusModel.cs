namespace DT.APIs.Models
{
    /// <summary>
    /// Represents the status of a POD in the workflow.
    /// </summary>
    public class PodStatusModel
    {
        public decimal Index { get; set; }
        public decimal PodID { get; set; }
        public string PodCode { get; set; }
        public string PodName { get; set; }
        public int PODTypeID { get; set; }
        public string PODTypeName { get; set; }
        public string PODTypeUrl { get; set; }
        public DateTime PODCreateTime { get; set; }
        public string PeriodID { get; set; }
        public int ZoneID { get; set; }
        public string ZoneName { get; set; }
        public int VendorID { get; set; }
        public string VendorName { get; set; }
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