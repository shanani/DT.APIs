using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;


namespace DT.APIs.Models.DTOs
{
    public class UserUpdateFromADModel
    {
        [SwaggerSchema(Description = "Unique identifier for the user.")]
        public int UserId { get; set; }

        [SwaggerSchema(Description = "English description of the user.")]
        public string? EnglishDescription { get; set; }

        [SwaggerSchema(Description = "Telephone number of the user.")]
        public string? Tel { get; set; }

        [SwaggerSchema(Description = "Email address of the user.")]
        public string? Email { get; set; }

        [SwaggerSchema(Description = "Mobile number of the user.")]
        public string? Mobile { get; set; }

        [SwaggerSchema(Description = "Department of the user.")]
        public string? Department { get; set; }

        [SwaggerSchema(Description = "First name of the user.")]
        public string? FirstName { get; set; }

        [SwaggerSchema(Description = "Title of the user.")]
        public string? Title { get; set; }

        [SwaggerSchema(Description = "Last AD Info Update Time.")]
        public DateTime? LastADInfoUpdateTime { get; set; }

        [SwaggerSchema(Description = "Base64 encoded photo of the user.")]
        public string? Photo { get; set; }


    }
}

