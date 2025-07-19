namespace DT.APIs.Models.DTOs
{
    public class TrafficByModuleModel
    {
        /// <summary>
        /// The date in the format 'MMM dd'.
        /// </summary>
        public string DateID { get; set; }

        /// <summary>
        /// The name of the module.
        /// </summary>
        public string Module { get; set; }

        /// <summary>
        /// The total number of transactions for the module.
        /// </summary>
        public string Total { get; set; }
    }
}