namespace DT.APIs.Models
{
    /// <summary>
    /// Represents a record in the system monitor.
    /// </summary>
    public class SystemMonitorModel
    {
        /// <summary>
        /// The index of the record.
        /// </summary>
        public decimal Index { get; set; }

        /// <summary>
        /// The unique identifier for the transaction.
        /// </summary>
        public decimal ID { get; set; }

        /// <summary>
        /// The time of the transaction.
        /// </summary>
        public DateTime TransactionTime { get; set; }

        /// <summary>
        /// The username associated with the transaction.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// The department associated with the transaction.
        /// </summary>
        public string Department { get; set; }

        /// <summary>
        /// The English description of the transaction.
        /// </summary>
        public string EnglishDescription { get; set; }

        /// <summary>
        /// The photo associated with the transaction.
        /// </summary>
        public byte[] Photo { get; set; }

        /// <summary>
        /// The module associated with the transaction.
        /// </summary>
        public string Module { get; set; }

        /// <summary>
        /// The permission associated with the transaction.
        /// </summary>
        public string Permission { get; set; }

        /// <summary>
        /// The parameters associated with the transaction.
        /// </summary>
        public string Parameters { get; set; }

        /// <summary>
        /// The IP address from which the transaction was made.
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// The name of the machine from which the transaction was made.
        /// </summary>
        public string MachineName { get; set; }
    }
}