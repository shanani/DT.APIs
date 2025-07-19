using DT.APIs.Helpers;
using DT.APIs.Models;
using DT.APIs.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Drawing;


namespace DT.APIs.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/ad")]
    public class ADController : ControllerBase
    {

        private readonly ILogger<ADController> _logger;

        private readonly string _connectionString;

        private readonly IConfiguration _configuration;

        private readonly EmailService _emailService;


        public ADController(ILogger<ADController> logger, IConfiguration configuration, EmailService emailService)
        {
            _logger = logger;           
            _configuration = configuration;
            _emailService = emailService;

        }
         


        #region AD APIs

        

        [HttpGet("users/get-ad-user")]
        [ProducesResponseType(typeof(ADUserDetails), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetADUser([FromQuery] string username)
        {
            var requestId = Guid.NewGuid().ToString("N")[..8];

            if (string.IsNullOrWhiteSpace(username))
            {
                _logger.LogWarning("[{RequestId}] Username is null or empty", requestId);
                return BadRequest("Username is required.");
            }

            _logger.LogInformation("[{RequestId}] Getting AD user details for username: '{Username}'", requestId, username);

            try
            {
                using (var adHelper = new ADHelper(_configuration))
                {
                    var userDetails = await Task.Run(() => adHelper.GetUserDetailsByUsername(username));

                    if (userDetails == null)
                    {
                        _logger.LogInformation("[{RequestId}] User not found: '{Username}'", requestId, username);
                        return NotFound($"User '{username}' not found in Active Directory.");
                    }

                    _logger.LogInformation("[{RequestId}] Successfully retrieved user details for: '{Username}'", requestId, username);
                    return Ok(userDetails);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{RequestId}] Error getting AD user details for username: '{Username}'", requestId, username);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"[{requestId}] Error retrieving user details: {ex.Message}");
            }
        }

         
      

        [HttpGet("users/search-ad")]
        [ProducesResponseType(typeof(IEnumerable<ADUserModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SearchADUsers([FromQuery] string searchKey, [FromQuery] int maxResults = 10)
        {
            var requestId = Guid.NewGuid().ToString("N")[..8];

            if (string.IsNullOrWhiteSpace(searchKey))
            {
                _logger.LogWarning("[{RequestId}] Search key is null or empty", requestId);
                return BadRequest("Search key is required and must be at least 3 characters.");
            }

            if (searchKey.Trim().Length < 3)
            {
                _logger.LogWarning("[{RequestId}] Search key too short: '{SearchKey}'", requestId, searchKey);
                return BadRequest("Search key must be at least 3 characters long.");
            }

            if (maxResults < 0)
            {
                _logger.LogWarning("[{RequestId}] Invalid maxResults value: {MaxResults}", requestId, maxResults);
                return BadRequest("maxResults cannot be negative.");
            }

            _logger.LogInformation("[{RequestId}] Starting AD user search with key: '{SearchKey}', maxResults: {MaxResults}",
                requestId, searchKey, maxResults);

            try
            {
                using (var adHelper = new ADHelper(_configuration))
                {
                    var allUsers = await Task.Run(() => adHelper.FindUsers(searchKey));

                    if (allUsers == null || !allUsers.Any())
                    {
                        _logger.LogInformation("[{RequestId}] No users found for search key: '{SearchKey}'", requestId, searchKey);
                        return NotFound($"No users found matching the search criteria '{searchKey}'");
                    }

                    // Apply maxResults filter
                    var users = maxResults == 0 ? allUsers : allUsers.Take(maxResults).ToList();

                    _logger.LogInformation("[{RequestId}] AD search completed. Found {TotalUsers} users, returning {ReturnedUsers} users for search key: '{SearchKey}'",
                        requestId, allUsers.Count, users.Count, searchKey);

                    _logger.LogDebug("[{RequestId}] Returning users: [{UserNames}]",
                        requestId, string.Join(", ", users.Select(u => u.UserName)));

                    return Ok(users);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{RequestId}] Error searching AD users with search key: '{SearchKey}'", requestId, searchKey);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"[{requestId}] Error occurred while searching Active Directory: {ex.Message}");
            }
        }


     

        [HttpPost("users/authenticate-ad")]
        [ProducesResponseType(typeof(ADUserDetails), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AuthenticateADUser([FromBody] ADAuthenticationRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Username and password are required.");
            }

            try
            {
                using (var adHelper = new ADHelper(_configuration))
                {
                    var adUser = adHelper.AuthenticateUser(request.Username, request.Password);

                    if (adUser != null)
                    {
                        return Ok(adUser);
                    }

                    return Unauthorized("Invalid AD credentials");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating AD user: {Username}", request.Username);
                return StatusCode(StatusCodes.Status500InternalServerError, "Authentication failed");
            }
        }

        #endregion

         

         









    }


}
