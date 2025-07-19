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
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("get-token")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult GetToken([FromBody] LoginModel model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
                {
                    return BadRequest("Username and password are required.");
                }

                // USE ONLY APPSETTINGS - NO DATABASE
                var apiUser = _configuration["Jwt:User"];        // "fopp"
                var apiPass = _configuration["Jwt:Password"];    // "!x!a0j4a"

                if (string.IsNullOrEmpty(apiUser) || string.IsNullOrEmpty(apiPass))
                {
                    _logger.LogError("JWT credentials not configured in appsettings.json");
                    return StatusCode(500, "Authentication configuration error");
                }

                if (apiUser == model.Username && apiPass == model.Password)
                {
                    string token = GenerateJwtToken(model.Username);
                    _logger.LogInformation("JWT token generated successfully for user: {Username}", model.Username);
                    return Ok(new
                    {
                        token = token,
                        expires = DateTime.UtcNow.AddDays(1),
                        message = "Authentication successful"
                    });
                }
                else
                {
                    _logger.LogWarning("Invalid login attempt for username: {Username}", model.Username);
                    return BadRequest("Invalid credentials.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token");
                return StatusCode(500, "Internal server error");
            }
        }

        private string GenerateJwtToken(string username)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim("username", username)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
