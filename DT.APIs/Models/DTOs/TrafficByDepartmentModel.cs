using Swashbuckle.AspNetCore.Annotations;

namespace DT.APIs.Models.DTOs
{
    public class TrafficByDepartmentModel
    {
        [SwaggerSchema(Description = "Date ID")]
        public string DateID { get; set; }
        public string Department { get; set; }
        public string Total { get; set; }

    }
}