using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DT.APIs.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;



namespace DT.APIs.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }


        [HttpPost("get-token")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetToken([FromBody] LoginModel model)
        {
            var apiUser = GetConfig("ACTIVE_DIRECTORY_USER");
            var apiPass = GetConfig("ACTIVE_DIRECTORY_PASSWORD");
            if (apiUser == model.Username && apiPass == model.Password)
            {
                string token = GenerateJwtToken(model.Username);

                return Ok(token);
            }
            else
                return BadRequest("Invalid login attempt.");

        }

        private string GenerateJwtToken(string username)
        {
            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, username),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Generate a GUID for the kid
            var kid = Guid.NewGuid().ToString();

            // Create the JWT header with the kid
            var header = new JwtHeader(creds)
    {
        { "kid", kid } // Set the key ID here
    };

            var payload = new JwtPayload(
                issuer: null, // Set your issuer if applicable
                audience: null, // Set your audience if applicable
                claims: claims,
                notBefore: null,
                expires: DateTime.UtcNow.AddDays(1)
            );

            var token = new JwtSecurityToken(header, payload);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        private string GetConfig(string key)
        {
            if (_context == null)
            {
                throw new InvalidOperationException("Util has not been initialized with a valid DbContext.");
            }

            // Retrieve the setting with the specified key
            var setting = _context.Setting
                .AsNoTracking()
                .FirstOrDefault(s => s.ID == key);

            return setting?.Value; // Return the value or null if not found
        }


    }
}
