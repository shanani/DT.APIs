using System.ComponentModel.DataAnnotations;

namespace DT.APIs.Models.DTOs
{
     
    public class LoginModel
    {
        [Required]
        public string? Username { get; set; }

        [Required]
        public string? Password { get; set; }
    }
}