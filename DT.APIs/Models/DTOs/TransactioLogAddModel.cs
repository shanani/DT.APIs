using System.ComponentModel;

namespace DT.APIs.Models.DTOs
{
    public class TransactioLogAddModel
    {

        //[DefaultValue(52)]
        public int? PermissionID { get; set; } // Nullable int for optional parameter
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public string RequestType { get; set; }
        public string? Parameters { get; set; } // Nullable for optional parameter
        public string UserName { get; set; }
        public string UserIPAddress { get; set; }
        public string UserMachineName { get; set; }
    }
}
