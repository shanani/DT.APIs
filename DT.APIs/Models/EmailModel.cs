using System.Collections.Generic;

namespace DT.APIs.Models
{
    public class EmailModel
    {
        public string Subject { get; set; }
        public string RecipientEmail { get; set; }
        public string RecipientName { get; set; }
        public string Body { get; set; }

        public List<string> Images { get; set; }

        public EmailModel()
        {

            Images = new List<string>();

        }
    }

}
