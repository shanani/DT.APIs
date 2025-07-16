using DT.APIs.Helpers;
using DT.APIs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Drawing;


namespace DT.APIs.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api")]
    public class HUBController : ControllerBase
    {

        private readonly ILogger<HUBController> _logger;

        private readonly string _connectionString;

        private readonly IConfiguration _configuration;

        private readonly EmailService _emailService;


        public HUBController(ILogger<HUBController> logger, IConfiguration configuration, EmailService emailService)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("HubDbConn")!;
            _configuration = configuration;
            _emailService = emailService;

        }



        #region Email APIs

        [HttpPost("send-email")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendEmail([FromBody] EmailModel emailModel)
        {
            try
            {
                // Basic null check
                if (emailModel == null)
                {
                    return BadRequest("Email model cannot be null");
                }

                await _emailService.SendEmailAsync(emailModel);
                return Ok(true);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid email data provided");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {RecipientEmail}", emailModel?.RecipientEmail);
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to send email");
            }
        }

        [HttpPost("send-bulk-email")]
        [ProducesResponseType(typeof(BulkEmailResultModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendBulkEmail([FromBody] BulkEmailModel bulkEmailModel)
        {
            try
            {
                // Basic null check
                if (bulkEmailModel == null)
                {
                    return BadRequest("Bulk email model cannot be null");
                }

                var result = await _emailService.SendBulkEmailAsync(bulkEmailModel);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid bulk email data provided");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending bulk email");
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to send bulk email");
            }
        }

        [HttpPost("send-template-email")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendTemplateEmail([FromBody] EmailTemplateModel templateModel, [FromQuery] string recipientEmail)
        {
            try
            {
                if (templateModel == null)
                {
                    return BadRequest("Template model cannot be null");
                }

                if (string.IsNullOrWhiteSpace(recipientEmail))
                {
                    return BadRequest("Recipient email is required");
                }

                // Handle null placeholders gracefully
                templateModel.Placeholders ??= new Dictionary<string, string>();
                templateModel.Subject ??= "No Subject";
                templateModel.Body ??= string.Empty;

                var processedSubject = _emailService.ProcessTemplate(templateModel.Subject, templateModel.Placeholders);
                var processedBody = _emailService.ProcessTemplate(templateModel.Body, templateModel.Placeholders);

                var emailModel = new EmailModel
                {
                    Subject = processedSubject,
                    RecipientEmail = recipientEmail.Trim(),
                    Body = processedBody,
                    IsBodyHtml = true
                    // All other properties will be handled by NormalizeEmailModel
                };

                await _emailService.SendEmailAsync(emailModel);
                return Ok(true);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid template email data provided");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending template email to {RecipientEmail}", recipientEmail);
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to send template email");
            }
        }

        [HttpPost("send-email-with-attachments")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendEmailWithAttachments([FromBody] EmailModel emailModel)
        {
            try
            {
                if (emailModel == null)
                {
                    return BadRequest("Email model cannot be null");
                }

                // Check attachment size limits (e.g., 25MB total) - only if attachments exist
                const int maxTotalSizeMB = 25;
                const int maxTotalSizeBytes = maxTotalSizeMB * 1024 * 1024;

                if (emailModel.Attachments?.Any() == true)
                {
                    var totalSize = 0L;
                    foreach (var attachment in emailModel.Attachments)
                    {
                        if (!string.IsNullOrWhiteSpace(attachment?.Content))
                        {
                            try
                            {
                                totalSize += Convert.FromBase64String(attachment.Content).Length;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Invalid attachment content for {FileName}", attachment.FileName);
                                // Continue with other attachments
                            }
                        }
                    }

                    if (totalSize > maxTotalSizeBytes)
                    {
                        return StatusCode(StatusCodes.Status413RequestEntityTooLarge,
                            $"Total attachment size exceeds {maxTotalSizeMB}MB limit");
                    }
                }

                await _emailService.SendEmailAsync(emailModel);
                return Ok(true);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid email data provided");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email with attachments to {RecipientEmail}", emailModel?.RecipientEmail);
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to send email with attachments");
            }
        }

        [HttpGet("test-email-connection")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> TestEmailConnection()
        {
            try
            {
                var mailSettings = _configuration.GetSection("MailSettings");

                var testEmail = new EmailModel
                {
                    Subject = "Email Service Test",
                    RecipientEmail = mailSettings["SenderEmail"] ?? "test@example.com",
                    Body = "<h1>Email Service Test</h1><p>This is a test email to verify the email service is working correctly.</p>",
                    IsBodyHtml = true
                    // Intentionally leaving other properties null to test normalization
                };

                await _emailService.SendEmailAsync(testEmail);

                return Ok(new
                {
                    success = true,
                    message = "Email service is working correctly",
                    server = mailSettings["Server"],
                    port = mailSettings["Port"],
                    senderEmail = mailSettings["SenderEmail"],
                    testSentAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email connection test failed");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "Email service connection failed",
                    error = ex.Message
                });
            }
        }

        [HttpPost("send-email-flexible-test")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendEmailFlexibleTest([FromBody] EmailModel emailModel)
        {
            try
            {
                if (emailModel == null)
                {
                    return BadRequest("Email model cannot be null");
                }

                _logger.LogInformation("Testing flexible email handling with input: {@EmailModel}", new
                {
                    emailModel.Subject,
                    emailModel.RecipientEmail,
                    emailModel.ReplyTo,
                    CCCount = emailModel.CC?.Count ?? 0,
                    BCCCount = emailModel.BCC?.Count ?? 0,
                    AttachmentsCount = emailModel.Attachments?.Count ?? 0,
                    CustomHeadersCount = emailModel.CustomHeaders?.Count ?? 0
                });

                await _emailService.SendEmailAsync(emailModel);

                return Ok(new
                {
                    success = true,
                    message = "Email sent successfully with flexible handling",
                    processedAt = DateTime.UtcNow,
                    originalReplyTo = emailModel.ReplyTo,
                    finalCCCount = emailModel.CC?.Count ?? 0,
                    finalBCCCount = emailModel.BCC?.Count ?? 0,
                    finalAttachmentsCount = emailModel.Attachments?.Count ?? 0
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid email data provided in flexible test");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in flexible email test");
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to send test email");
            }
        }

        #endregion


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


        #region Post Actions



        [HttpPost("logging-users-auto-insert")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UsersAutoInsert([FromBody] UsersAutoInsertModel request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request.");
            }

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_LOGGING_USERS_AUTO_INSERT", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;


                    cmd.Parameters.AddWithValue("@Code", request.Code);
                    cmd.Parameters.AddWithValue("@EnglishDescription", request.EnglishDescription);
                    cmd.Parameters.AddWithValue("@ArabicDescription", request.ArabicDescription);
                    cmd.Parameters.AddWithValue("@Department", request.Department);
                    cmd.Parameters.AddWithValue("@Email", request.Email);
                    cmd.Parameters.AddWithValue("@Mobile", request.Mobile);
                    cmd.Parameters.AddWithValue("@Tel", request.Tel);
                    byte[] imageBytes = null!;
                    if (!string.IsNullOrEmpty(request.Photo) && request.Photo.StartsWith("data:image/png;base64,"))
                    {
                        try
                        {
                            imageBytes = Convert.FromBase64String(request.Photo.Replace("data:image/png;base64,", ""));
                        }
                        catch { }

                    }
                    // Set the @Photo parameter explicitly
                    SqlParameter photoParam = new SqlParameter("@Photo", SqlDbType.Image);
                    photoParam.Value = (object)imageBytes ?? DBNull.Value;
                    cmd.Parameters.Add(photoParam);


                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            return Ok("User inserted successfully.");
        }




        [HttpPost("security-stc-user-add")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> STCUserAdd([FromBody] STCUserAddModel request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request.");
            }

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_STC_USER_ADD", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Convert Photo from Base64 string to byte array if provided
                    byte[] imageBytes = null!;
                    if (!string.IsNullOrEmpty(request.Photo) && request.Photo.StartsWith("data:image/png;base64,"))
                    {
                        try
                        {
                            imageBytes = Convert.FromBase64String(request.Photo.Replace("data:image/png;base64,", ""));
                        }
                        catch { }

                    }

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", request.UserID);
                    cmd.Parameters.AddWithValue("@UserCode", request.UserCode);
                    cmd.Parameters.AddWithValue("@EnglishDescription", request.EnglishDescription);
                    cmd.Parameters.AddWithValue("@Email", request.Email);
                    cmd.Parameters.AddWithValue("@Mobile", request.Mobile);
                    cmd.Parameters.AddWithValue("@Department", request.Department);
                    cmd.Parameters.AddWithValue("@STCId", request.STCId);

                    // Set the @Photo parameter explicitly
                    SqlParameter photoParam = new SqlParameter("@Photo", SqlDbType.Image);
                    photoParam.Value = (object)imageBytes ?? DBNull.Value;
                    cmd.Parameters.Add(photoParam);


                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            return Ok("User added or updated successfully.");
        }



        [HttpPost("security-transaction-log-add")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> TransactioLogAdd([FromBody] TransactioLogAddModel request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request.");
            }

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_TRANSACTION_LOG_ADD", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@PermissionID", (object)request.PermissionID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ControllerName", request.ControllerName);
                    cmd.Parameters.AddWithValue("@ActionName", request.ActionName);
                    cmd.Parameters.AddWithValue("@RequestType", request.RequestType);
                    cmd.Parameters.AddWithValue("@Parameters", (object)request.Parameters ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserName", request.UserName);
                    cmd.Parameters.AddWithValue("@UserIPAddress", request.UserIPAddress);
                    cmd.Parameters.AddWithValue("@UserMachineName", request.UserMachineName);

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            return Ok("Transaction logged successfully.");
        }





        [HttpPost("post-action")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PostAction(int userId, int patternStepActionId, decimal workflowId, string comment, string attached)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_WORKFLOW_POST_ACTION", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@PatternStepActionID", patternStepActionId);
                    cmd.Parameters.AddWithValue("@WorkflowID", workflowId);
                    cmd.Parameters.AddWithValue("@Comment", comment);
                    cmd.Parameters.AddWithValue("@Attached", attached);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // If rowsAffected is greater than 0, the insert was successful
                    bool isSuccess = rowsAffected > 0;

                    return Ok(isSuccess); // Return 200 with the success status
                }
            }
        }



        [HttpPost("reject-certificate")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RejectCertificate(decimal podId, int userId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_WORKFLOW_REJECT_CERFIFICATE_POD", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PODID", podId);
                    cmd.Parameters.AddWithValue("@UserID", userId);

                    await conn.OpenAsync();
                    int result = (int)await cmd.ExecuteScalarAsync();

                    // Check if the result indicates success
                    bool isSuccess = result == 1;

                    return Ok(isSuccess); // Return 200 with the success status
                }
            }
        }


        [HttpPost("undo-last-action")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UndoLastAction(int userId, decimal workflowId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_WORKFLOW_UNDO_LAST_ACTION", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@WorkflowID", workflowId);

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();

                    // Check if the last action was successfully updated
                    // Assuming success if no exception occurs; can be enhanced with logging or checks.
                    return Ok(true); // Return 200 indicating success
                }
            }
        }



        #endregion


        #region Update APIs

        [HttpPut("update-user-from-ad")]
        public async Task<IActionResult> UpdateUserFromAD([FromBody] UserUpdateFromADModel request)
        {
            if (request == null || request.UserId <= 0)
            {
                return BadRequest("Invalid request data.");
            }

            byte[]? imageBytes = null;
            if (!string.IsNullOrEmpty(request.Photo))
            {
                try
                {
                    imageBytes = Convert.FromBase64String(request.Photo.Replace("data:image/png;base64,", ""));
                }
                catch (FormatException)
                {
                    return BadRequest("Invalid photo format.");
                }
            }

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_USER_UPDATE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@UserId", request.UserId);
                    cmd.Parameters.AddWithValue("@EnglishDescription", (object)request.EnglishDescription ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Tel", (object)request.Tel ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object)request.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Mobile", (object)request.Mobile ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Department", (object)request.Department ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FirstName", (object)request.FirstName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Title", (object)request.Title ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LastADInfoUpdateTime", (object)request.LastADInfoUpdateTime ?? DBNull.Value);

                    // Set the @Photo parameter explicitly
                    SqlParameter photoParam = new SqlParameter("@Photo", SqlDbType.Image);
                    photoParam.Value = (object)imageBytes ?? DBNull.Value;
                    cmd.Parameters.Add(photoParam);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    if (rowsAffected == 0)
                    {
                        return NotFound("User not found or no changes were made.");
                    }
                }
            }

            return Ok("User updated successfully.");
        }


        [HttpPut("set-role-district-admin")]
        public async Task<IActionResult> SetRoleDistrictAdmin([FromQuery] int userID, [FromQuery] int roleID, [FromQuery] bool isAdmin)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_SET_ROLE_DISTRICT_ADMIN", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@RoleID", roleID);
                    cmd.Parameters.AddWithValue("@IsAdmin", isAdmin);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Validate the number of rows affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No rows were updated. Please check the RoleID.");
                    }
                }
            }

            return Ok("Role updated successfully.");
        }


        [HttpPut("set-role-domain-admin")]
        public async Task<IActionResult> SetRoleDomainAdmin([FromQuery] int userID, [FromQuery] int roleID, [FromQuery] bool isAdmin)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_SET_ROLE_DOMAIN_ADMIN", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@RoleID", roleID);
                    cmd.Parameters.AddWithValue("@IsAdmin", isAdmin);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Validate the number of rows affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No rows were updated. Please check the RoleID.");
                    }
                }
            }

            return Ok("Role updated successfully.");
        }


        [HttpPut("set-role-segment-admin")]
        public async Task<IActionResult> SetRoleSegmentAdmin([FromQuery] int userID, [FromQuery] int roleID, [FromQuery] bool isAdmin)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_SET_ROLE_SEGMENT_ADMIN", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@RoleID", roleID);
                    cmd.Parameters.AddWithValue("@IsAdmin", isAdmin);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Validate the number of rows affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No rows were updated. Please check the RoleID.");
                    }
                }
            }

            return Ok("Role updated successfully.");
        }

        [HttpPut("set-role-sms-receiver-admin")]
        public async Task<IActionResult> SetRoleSmsReceiverAdmin([FromQuery] int userID, [FromQuery] int roleID, [FromQuery] bool isAdmin)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_SET_ROLE_SMS_RECEIVER_ADMIN", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@RoleID", roleID);
                    cmd.Parameters.AddWithValue("@IsAdmin", isAdmin);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Validate the number of rows affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No rows were updated. Please check the RoleID.");
                    }
                }
            }

            return Ok("Role updated successfully.");
        }


        [HttpPut("set-role-vendor-admin")]
        public async Task<IActionResult> SetRoleVendorAdmin([FromQuery] int userID, [FromQuery] int roleID, [FromQuery] bool isAdmin)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_SET_ROLE_VENDOR_ADMIN", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@RoleID", roleID);
                    cmd.Parameters.AddWithValue("@IsAdmin", isAdmin);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Validate the number of rows affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No rows were updated. Please check the RoleID.");
                    }
                }
            }

            return Ok("Role updated successfully.");
        }


        [HttpPut("set-role-zone-admin")]
        public async Task<IActionResult> SetRoleZoneAdmin([FromQuery] int userID, [FromQuery] int roleID, [FromQuery] bool isAdmin)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_SET_ROLE_ZONE_ADMIN", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@RoleID", roleID);
                    cmd.Parameters.AddWithValue("@IsAdmin", isAdmin);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Validate the number of rows affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No rows were updated. Please check the RoleID.");
                    }
                }
            }

            return Ok("Role updated successfully.");
        }


        [HttpPut("set-user-admin")]
        public async Task<IActionResult> SetUserAdmin([FromQuery] int systemUserID, [FromQuery] int userID, [FromQuery] bool isAdmin)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_SET_USER_ADMIN", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@SystemUserID", systemUserID);
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@IsAdmin", isAdmin);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Validate the number of rows affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No rows were updated. Please check the UserID.");
                    }
                }
            }

            return Ok("User admin status updated successfully.");
        }


        [HttpPut("set-user-district-admin")]
        public async Task<IActionResult> SetUserDistrictAdmin([FromQuery] int userID, [FromQuery] int targetUserID, [FromQuery] bool isAdmin)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_SET_USER_DISTRICT_ADMIN", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@TargetUserID", targetUserID);
                    cmd.Parameters.AddWithValue("@IsAdmin", isAdmin);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Validate the number of rows affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No rows were updated. Please check the TargetUserID.");
                    }
                }
            }

            return Ok("User district admin status updated successfully.");
        }


        [HttpPut("set-user-domain-admin")]
        public async Task<IActionResult> SetUserDomainAdmin([FromQuery] int userID, [FromQuery] int targetUserID, [FromQuery] bool isAdmin)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_SET_USER_DOMAIN_ADMIN", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@TargetUserID", targetUserID);
                    cmd.Parameters.AddWithValue("@IsAdmin", isAdmin);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Validate the number of rows affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No rows were updated. Please check the TargetUserID.");
                    }
                }
            }

            return Ok("User domain admin status updated successfully.");
        }


        [HttpPut("set-user-expiration-date")]
        public async Task<IActionResult> SetUserExpirationDate([FromQuery] int systemUserID, [FromQuery] int userID, [FromQuery] string expirationDate)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_SET_USER_EXPIRATION_DATE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@SystemUserID", systemUserID);
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@ExpirationDate", expirationDate);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Validate the number of rows affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No rows were updated. Please check the UserID.");
                    }
                }
            }

            return Ok("User expiration date updated successfully.");
        }


        [HttpPut("set-user-locked")]
        public async Task<IActionResult> SetUserLocked([FromQuery] int systemUserID, [FromQuery] int userID, [FromQuery] bool isLocked)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_SET_USER_LOCKED", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@SystemUserID", systemUserID);
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@IsLocked", isLocked);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Validate the number of rows affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No rows were updated. Please check the UserID.");
                    }
                }
            }

            return Ok("User locked status updated successfully.");
        }



        [HttpPut("set-user-segment-admin")]
        public async Task<IActionResult> SetUserSegmentAdmin([FromQuery] int userID, [FromQuery] int targetUserID, [FromQuery] bool isAdmin)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_SET_USER_SEGMENT_ADMIN", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@TargetUserID", targetUserID);
                    cmd.Parameters.AddWithValue("@IsAdmin", isAdmin);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Validate the number of rows affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No rows were updated. Please check the TargetUserID.");
                    }
                }
            }

            return Ok("User segment admin status updated successfully.");
        }


        [HttpPut("set-user-sms-receiver")]
        public async Task<IActionResult> SetUserSmsReceiver([FromQuery] int systemUserID, [FromQuery] int userID, [FromQuery] bool isSMSReceiver)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_SET_USER_SMS_RECEIVER", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@SystemUserID", systemUserID);
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@IsSMSReceiver", isSMSReceiver);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Validate the number of rows affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No rows were updated. Please check the UserID.");
                    }
                }
            }

            return Ok("User SMS receiver status updated successfully.");
        }


        [HttpPut("set-user-vendor-admin")]
        public async Task<IActionResult> SetUserVendorAdmin([FromQuery] int userID, [FromQuery] int targetUserID, [FromQuery] bool isAdmin)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_SET_USER_VENDOR_ADMIN", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@TargetUserID", targetUserID);
                    cmd.Parameters.AddWithValue("@IsAdmin", isAdmin);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Validate the number of rows affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No rows were updated. Please check the TargetUserID.");
                    }
                }
            }

            return Ok("User vendor admin status updated successfully.");
        }


        [HttpPut("set-user-zone-admin")]
        public async Task<IActionResult> SetUserZoneAdmin([FromQuery] int userID, [FromQuery] int targetUserID, [FromQuery] bool isAdmin)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_SET_USER_ZONE_ADMIN", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@TargetUserID", targetUserID);
                    cmd.Parameters.AddWithValue("@IsAdmin", isAdmin);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Validate the number of rows affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No rows were updated. Please check the TargetUserID.");
                    }
                }
            }

            return Ok("User zone admin status updated successfully.");
        }



        [HttpPut("role-district-update")]
        public async Task<IActionResult> RoleDistrictUpdate([FromQuery] int userID, [FromQuery] int roleID, [FromQuery] int districtID, [FromQuery] bool isSelected)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_ROLE_DISTRICT_UPDATE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@RoleID", roleID);
                    cmd.Parameters.AddWithValue("@DistrictID", districtID);
                    cmd.Parameters.AddWithValue("@IsSelected", isSelected ? 1 : 0);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Check if any rows were affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No changes were made. Please check the RoleID and DistrictID.");
                    }
                }
            }

            return Ok("Role district updated successfully.");
        }



        [HttpPut("role-domain-update")]
        public async Task<IActionResult> RoleDomainUpdate([FromQuery] int userID, [FromQuery] int roleID, [FromQuery] int domainID, [FromQuery] bool isSelected)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_ROLE_DOMAIN_UPDATE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@RoleID", roleID);
                    cmd.Parameters.AddWithValue("@DomainID", domainID);
                    cmd.Parameters.AddWithValue("@IsSelected", isSelected ? 1 : 0);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Check if any rows were affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No changes were made. Please check the provided parameters.");
                    }
                }
            }

            return Ok("Role domain updated successfully.");
        }



        [HttpPut("role-permission-update")]
        public async Task<IActionResult> RolePermissionUpdate([FromQuery] int userID, [FromQuery] int roleID, [FromQuery] string selectedActions)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_ROLE_PERMISSION_UPDATE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@RoleID", roleID);
                    cmd.Parameters.AddWithValue("@SelectedActions", selectedActions);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Check if any rows were affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No changes were made. Please check the provided parameters.");
                    }
                }
            }

            return Ok("Role permissions updated successfully.");
        }



        [HttpPut("role-segment-update")]
        public async Task<IActionResult> RoleSegmentUpdate([FromQuery] int userID, [FromQuery] int roleID, [FromQuery] int segmentID, [FromQuery] bool isSelected)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_ROLE_SEGMENT_UPDATE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@RoleID", roleID);
                    cmd.Parameters.AddWithValue("@SegmentID", segmentID);
                    cmd.Parameters.AddWithValue("@IsSelected", isSelected ? 1 : 0);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Check if any rows were affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No changes were made. Please check the provided parameters.");
                    }
                }
            }

            return Ok("Role segment updated successfully.");
        }



        [HttpPut("role-sms-receiver-update")]
        public async Task<IActionResult> RoleSmsReceiverUpdate([FromQuery] int userID, [FromQuery] int roleID, [FromQuery] int smsReceiverGroupID, [FromQuery] bool isSelected)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_ROLE_SMS_RECEIVER_UPDATE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@RoleID", roleID);
                    cmd.Parameters.AddWithValue("@SMSReceiverGroupID", smsReceiverGroupID);
                    cmd.Parameters.AddWithValue("@IsSelected", isSelected ? 1 : 0);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Check if any rows were affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No changes were made. Please check the provided parameters.");
                    }
                }
            }

            return Ok("Role SMS receiver updated successfully.");
        }




        [HttpPut("role-sms-receiver-group-update")]
        public async Task<IActionResult> RoleSmsReceiverGroupUpdate([FromQuery] int userID, [FromQuery] int roleID, [FromQuery] int smsReceiverGroupID, [FromQuery] bool isSelected)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_ROLE_SMS_REVEIVER_GROUP_UPDATE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@RoleID", roleID);
                    cmd.Parameters.AddWithValue("@SMSReceiverGroupID", smsReceiverGroupID);
                    cmd.Parameters.AddWithValue("@IsSelected", isSelected ? 1 : 0);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Check if any rows were affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No changes were made. Please check the provided parameters.");
                    }
                }
            }

            return Ok("Role SMS receiver group updated successfully.");
        }



        [HttpPut("role-sms-sender-group-update")]
        public async Task<IActionResult> RoleSmsSenderGroupUpdate([FromQuery] int userID, [FromQuery] int roleID, [FromQuery] int smsSenderGroupID, [FromQuery] bool isSelected)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_ROLE_SMS_SENDER_GROUP_UPDATE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@RoleID", roleID);
                    cmd.Parameters.AddWithValue("@SMSSenderGroupID", smsSenderGroupID);
                    cmd.Parameters.AddWithValue("@IsSelected", isSelected ? 1 : 0);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Check if any rows were affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No changes were made. Please check the provided parameters.");
                    }
                }
            }

            return Ok("Role SMS sender group updated successfully.");
        }



        [HttpPut("role-user-update")]
        public async Task<IActionResult> RoleUserUpdate([FromQuery] int userID, [FromQuery] int roleID, [FromQuery] int targetUserID, [FromQuery] bool isSelected)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_ROLE_USER_UPDATE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@RoleID", roleID);
                    cmd.Parameters.AddWithValue("@TargetUserID", targetUserID);
                    cmd.Parameters.AddWithValue("@IsSelected", isSelected ? 1 : 0);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Check if any rows were affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No changes were made. Please check the provided parameters.");
                    }
                }
            }

            return Ok("Role user updated successfully.");
        }



        [HttpPut("role-vendor-update")]
        public async Task<IActionResult> RoleVendorUpdate([FromQuery] int userID, [FromQuery] int roleID, [FromQuery] int vendorID, [FromQuery] bool isSelected)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_ROLE_VENDOR_UPDATE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@RoleID", roleID);
                    cmd.Parameters.AddWithValue("@VendorID", vendorID);
                    cmd.Parameters.AddWithValue("@IsSelected", isSelected ? 1 : 0);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Check if any rows were affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No changes were made. Please check the provided parameters.");
                    }
                }
            }

            return Ok("Role vendor updated successfully.");
        }



        [HttpPut("role-zone-update")]
        public async Task<IActionResult> RoleZoneUpdate([FromQuery] int userID, [FromQuery] int roleID, [FromQuery] int zoneID, [FromQuery] bool isSelected)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_ROLE_ZONE_UPDATE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@RoleID", roleID);
                    cmd.Parameters.AddWithValue("@ZoneID", zoneID);
                    cmd.Parameters.AddWithValue("@IsSelected", isSelected ? 1 : 0);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Check if any rows were affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No changes were made. Please check the provided parameters.");
                    }
                }
            }

            return Ok("Role zone updated successfully.");
        }



        [HttpPut("user-district-update")]
        public async Task<IActionResult> UserDistrictUpdate([FromQuery] int userID, [FromQuery] int targetUserID, [FromQuery] int districtID, [FromQuery] bool isSelected)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_USER_DISTRICT_UPDATE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@TargetUserID", targetUserID);
                    cmd.Parameters.AddWithValue("@DistrictID", districtID);
                    cmd.Parameters.AddWithValue("@IsSelected", isSelected ? 1 : 0);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Check if any rows were affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No changes were made. Please check the provided parameters.");
                    }
                }
            }

            return Ok("User district updated successfully.");
        }



        [HttpPut("user-domain-update")]
        public async Task<IActionResult> UserDomainUpdate([FromQuery] int userID, [FromQuery] int targetUserID, [FromQuery] int domainID, [FromQuery] bool isSelected)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_USER_DOMAIN_UPDATE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@TargetUserID", targetUserID);
                    cmd.Parameters.AddWithValue("@DomainID", domainID);
                    cmd.Parameters.AddWithValue("@IsSelected", isSelected ? 1 : 0);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Check if any rows were affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No changes were made. Please check the provided parameters.");
                    }
                }
            }

            return Ok("User domain updated successfully.");
        }



        [HttpPut("user-permission-update")]
        public async Task<IActionResult> UserPermissionUpdate([FromQuery] int userID, [FromQuery] int targetUserID, [FromQuery] string selectedActions)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_USER_PERMISSION_UPDATE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@TargetUserID", targetUserID);
                    cmd.Parameters.AddWithValue("@SelectedActions", selectedActions);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Check if any rows were affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No changes were made. Please check the provided parameters.");
                    }
                }
            }

            return Ok("User permissions updated successfully.");
        }




        [HttpPut("user-role-update")]
        public async Task<IActionResult> UserRoleUpdate([FromQuery] int userID, [FromQuery] int targetUserID, [FromQuery] string selectedRoles)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_USER_ROLE_UPDATE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@TargetUserID", targetUserID);
                    cmd.Parameters.AddWithValue("@SelectedRoles", selectedRoles);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Check if any rows were affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No changes were made. Please check the provided parameters.");
                    }
                }
            }

            return Ok("User roles updated successfully.");
        }



        [HttpPut("user-segment-update")]
        public async Task<IActionResult> UserSegmentUpdate([FromQuery] int userID, [FromQuery] int targetUserID, [FromQuery] int segmentID, [FromQuery] bool isSelected)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_USER_SEGMENT_UPDATE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@TargetUserID", targetUserID);
                    cmd.Parameters.AddWithValue("@SegmentID", segmentID);
                    cmd.Parameters.AddWithValue("@IsSelected", isSelected ? 1 : 0);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Check if any rows were affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No changes were made. Please check the provided parameters.");
                    }
                }
            }

            return Ok("User segment updated successfully.");
        }



        [HttpPut("user-sms-receiver-group-update")]
        public async Task<IActionResult> UserSmsReceiverGroupUpdate([FromQuery] int userID, [FromQuery] int targetUserID, [FromQuery] int smsReceiverGroupID, [FromQuery] bool isSelected)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_USER_SMS_RECEIVER_GROUP_UPDATE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@TargetUserID", targetUserID);
                    cmd.Parameters.AddWithValue("@SMSReceiverGroupID", smsReceiverGroupID);
                    cmd.Parameters.AddWithValue("@IsSelected", isSelected ? 1 : 0);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Check if any rows were affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No changes were made. Please check the provided parameters.");
                    }
                }
            }

            return Ok("User SMS receiver group updated successfully.");
        }



        [HttpPut("user-sms-sender-group-update")]
        public async Task<IActionResult> UserSmsSenderGroupUpdate([FromQuery] int userID, [FromQuery] int targetUserID, [FromQuery] int smsSenderGroupID, [FromQuery] bool isSelected)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_USER_SMS_SENDER_GROUP_UPDATE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@TargetUserID", targetUserID);
                    cmd.Parameters.AddWithValue("@SMSSenderGroupID", smsSenderGroupID);
                    cmd.Parameters.AddWithValue("@IsSelected", isSelected ? 1 : 0);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Check if any rows were affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No changes were made. Please check the provided parameters.");
                    }
                }
            }

            return Ok("User SMS sender group updated successfully.");
        }




        [HttpPut("user-vendor-update")]
        public async Task<IActionResult> UserVendorUpdate([FromQuery] int userID, [FromQuery] int targetUserID, [FromQuery] int vendorID, [FromQuery] bool isSelected)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_USER_VENDOR_UPDATE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@TargetUserID", targetUserID);
                    cmd.Parameters.AddWithValue("@VendorID", vendorID);
                    cmd.Parameters.AddWithValue("@IsSelected", isSelected ? 1 : 0);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Check if any rows were affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No changes were made. Please check the provided parameters.");
                    }
                }
            }

            return Ok("User vendor updated successfully.");
        }



        [HttpPut("user-zone-update")]
        public async Task<IActionResult> UserZoneUpdate([FromQuery] int userID, [FromQuery] int targetUserID, [FromQuery] int zoneID, [FromQuery] bool isSelected)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_USER_ZONE_UPDATE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    cmd.Parameters.AddWithValue("@TargetUserID", targetUserID);
                    cmd.Parameters.AddWithValue("@ZoneID", zoneID);
                    cmd.Parameters.AddWithValue("@IsSelected", isSelected ? 1 : 0);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    // Check if any rows were affected
                    if (rowsAffected == 0)
                    {
                        return NotFound("No changes were made. Please check the provided parameters.");
                    }
                }
            }

            return Ok("User zone updated successfully.");
        }



        [HttpPut("generate-otp")]
        [ProducesResponseType(typeof(DateTime?), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GenerateOtp(int userId, int actionId, decimal workflowId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_WORKFLOW_GENERATE_OTP", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@ActionID", actionId);
                    cmd.Parameters.AddWithValue("@WorkflowID", workflowId);

                    // Output parameter for ExpiryDate
                    var expiryDateParam = new SqlParameter("@ExpiryDate", SqlDbType.DateTime)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(expiryDateParam);

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();

                    // Return the expiry date
                    return Ok((DateTime?)expiryDateParam.Value); // Return 200 with the expiry date
                }
            }
        }


        [HttpGet("confirm-otp")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ConfirmOtp(int userId, int actionId, decimal workflowId, string otp)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_WORKFLOW_OTP_CONFIRM", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@ActionID", actionId);
                    cmd.Parameters.AddWithValue("@WorkflowID", workflowId);
                    cmd.Parameters.AddWithValue("@OTP", otp);

                    // Output parameter for Success
                    var successParam = new SqlParameter("@Success", SqlDbType.Bit)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(successParam);

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();

                    // Get the success value
                    bool isSuccess = (bool)successParam.Value;

                    return Ok(isSuccess); // Return 200 with the success status
                }
            }
        }

        #endregion



        #region Get APIs

        [HttpGet("users/emails")]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAllSystemUserEmails()
        {
            var emails = new List<string>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT email FROM V_ALL_SYSTEMS_USERS", conn))
                {
                    cmd.CommandType = CommandType.Text;

                    await conn.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            emails.Add(reader["email"].ToString());
                        }
                    }
                }
            }

            if (emails.Count == 0)
            {
                return NotFound(); // Return 404 if no emails found
            }

            return Ok(emails); // Return 200 with email list
        }


        [HttpGet("users/{userId}/zones")]
        [ProducesResponseType(typeof(IEnumerable<ZoneModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetZonesByUserId(int userId, [FromQuery] int? projectId = null)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("S_WORKFLOW_ZONE_SELECT_BY_USER_ID", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@ProjectID", projectId.HasValue ? (object)projectId.Value : DBNull.Value);

                    await conn.OpenAsync();
                    var zones = new List<ZoneModel>();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            zones.Add(new ZoneModel
                            {
                                UserID = (int)reader["UserID"],
                                ZoneID = (int)reader["ZoneID"],
                                ZoneName = reader["ZoneName"].ToString(),
                                DistrictID = (int)reader["DistrictID"],
                                DistrictName = reader["DistrictName"].ToString(),
                                VendorID = (int)reader["VendorID"],
                                VendorName = reader["VendorName"].ToString(),
                                ProjectID = reader["ProjectID"] as int?
                            });
                        }
                    }

                    // Check if zones are found
                    if (zones.Count == 0)
                    {
                        return NotFound(); // Return 404 if no zones found
                    }

                    return Ok(zones); // Return 200 with zone data
                }
            }
        }



        [HttpGet("users/{userId}/segments")]
        [ProducesResponseType(typeof(IEnumerable<SegmentModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSegmentsByUserId(int userId, [FromQuery] int? projectId = null)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("S_SECURITY_SEGMENT_SELECT_BY_USER_ID", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@ProjectID", projectId.HasValue ? (object)projectId.Value : DBNull.Value);

                    await conn.OpenAsync();
                    var segments = new List<SegmentModel>();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            segments.Add(new SegmentModel
                            {
                                ID = (int)reader["ID"],
                                Name = reader["Name"].ToString(),
                                IsSegmentAdmin = reader["IsSegmentAdmin"] as bool? ?? false,
                                IsGranted = (int)reader["IsGranted"] == 1,
                                ProjectID = reader["ProjectID"] as int?
                            });
                        }
                    }

                    // Check if segments are found
                    if (segments.Count == 0)
                    {
                        return NotFound(); // Return 404 if no segments found
                    }

                    return Ok(segments); // Return 200 with segment data
                }
            }
        }

        [HttpGet("users/{userId}/roles")]
        [ProducesResponseType(typeof(IEnumerable<RoleModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRolesByUserId(int userId, [FromQuery] int? projectId = null)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("S_SECURITY_ROLE_SELECT_BY_USER_ID", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@ProjectID", projectId.HasValue ? (object)projectId.Value : DBNull.Value);

                    await conn.OpenAsync();
                    var roles = new List<RoleModel>();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            roles.Add(new RoleModel
                            {
                                ID = (int)reader["ID"],
                                Name = reader["Name"].ToString(),
                                ProjectID = reader["ProjectID"] as int?,
                                IsGranted = (int)reader["IsGranted"] == 1
                            });
                        }
                    }

                    // Check if roles are found
                    if (roles.Count == 0)
                    {
                        return NotFound(); // Return 404 if no roles found
                    }

                    return Ok(roles); // Return 200 with role data
                }
            }
        }


        [HttpGet("users/{userId}/modules")]
        [ProducesResponseType(typeof(IEnumerable<ModuleModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetModulesByUserId(int userId, [FromQuery] int? projectId = null)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("S_SECURITY_MODULE_SELECT_BY_USER_ID", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@ProjectID", projectId.HasValue ? (object)projectId.Value : DBNull.Value);

                    await conn.OpenAsync();
                    var modules = new List<ModuleModel>();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            modules.Add(new ModuleModel
                            {
                                ID = (int)reader["ID"],
                                Code = reader["Code"].ToString(),
                                EnglishDescription = reader["EnglishDescription"].ToString(),
                                ArabicDescription = reader["ArabicDescription"].ToString(),
                                ProjectID = (int)reader["ProjectID"],
                                ProjectEnglishName = reader["ProjectEnglishName"].ToString(),
                                ProjectArabicName = reader["ProjectArabicName"].ToString(),
                                IsActive = (bool)reader["IsActive"],
                                Sort = (int)reader["Sort"],
                                Tag = reader["Tag"].ToString()
                            });
                        }
                    }

                    // Check if modules are found
                    if (modules.Count == 0)
                    {
                        return NotFound(); // Return 404 if no modules found
                    }

                    return Ok(modules); // Return 200 with module data
                }
            }
        }


        [HttpGet("users/{userId}/permissions")]
        [ProducesResponseType(typeof(IEnumerable<PermissionModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPermissionsByUserId(int userId, [FromQuery] int? projectId = null)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("S_SECURITY_PERMISSION_SELECT_BY_USER_ID", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@ProjectID", projectId.HasValue ? (object)projectId.Value : DBNull.Value);

                    await conn.OpenAsync();
                    var permissions = new List<PermissionModel>();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            permissions.Add(new PermissionModel
                            {
                                PermissionID = (int)reader["PermissionID"],
                                Permission = reader["Permission"].ToString(),
                                Module = reader["Module"].ToString(),
                                Action = reader["Action"].ToString(),
                                ActionType = reader["ActionType"].ToString(),
                                ProjectName = reader["ProjectName"].ToString()
                            });
                        }
                    }

                    // Check if permissions are found
                    if (permissions.Count == 0)
                    {
                        return NotFound(); // Return 404 if no permissions found
                    }

                    return Ok(permissions); // Return 200 with permission data
                }
            }
        }

        [HttpGet("users/{userId}/actions")]
        [ProducesResponseType(typeof(IEnumerable<ActionsModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetActionsByUserId(int userId, [FromQuery] int? projectId = null)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("S_SECURITY_ACTION_SELECT_BY_USER_ID", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@ProjectID", projectId.HasValue ? (object)projectId.Value : DBNull.Value);

                    await conn.OpenAsync();
                    var actions = new List<ActionsModel>();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            actions.Add(new ActionsModel
                            {
                                ProjectID = Convert.ToInt32(reader["ProjectID"]), // Use Convert.ToInt32
                                ProjectEnglishName = reader["ProjectEnglishName"].ToString(),
                                ProjectArabicName = reader["ProjectArabicName"].ToString(),
                                MenuID = Convert.ToInt32(reader["MenuID"]), // Use Convert.ToInt32
                                MenuParentID = reader["MenuParentID"] is DBNull ? (int?)null : Convert.ToInt32(reader["MenuParentID"]), // Check for DBNull
                                MenuEnglishDescription = reader["MenuEnglishDescription"].ToString(),
                                MenuArabicDescription = reader["MenuArabicDescription"].ToString(),
                                MenuSort = reader["MenuSort"] is DBNull ? (int?)null : Convert.ToInt32(reader["MenuSort"]), // Check for DBNull
                                MenuTag = reader["MenuTag"].ToString(),
                                ModuleID = Convert.ToInt32(reader["ModuleID"]), // Use Convert.ToInt32
                                ModuleEnglishDescription = reader["ModuleEnglishDescription"].ToString(),
                                ModuleArabicDescription = reader["ModuleArabicDescription"].ToString(),
                                ModuleSort = Convert.ToInt32(reader["ModuleSort"]), // Use Convert.ToInt32
                                ModuleTag = reader["ModuleTag"].ToString(),
                                ActionID = reader["ActionID"].ToString(),
                                ControllerID = reader["ControllerID"].ToString(),
                                Url = reader["Url"].ToString(),
                                IsMenu = reader["IsMenu"] is DBNull ? false : (bool)reader["IsMenu"] // Check for DBNull
                            });
                        }
                    }

                    // Check if actions are found
                    if (actions.Count == 0)
                    {
                        return NotFound(); // Return 404 if no actions found
                    }

                    return Ok(actions); // Return 200 with action data
                }
            }
        }

        [HttpGet("users/{userId}/projects")]
        [ProducesResponseType(typeof(IEnumerable<ProjectModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProjectsByUserId(int userId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("S_SECURITY_PROJECT_SELECT_BY_USER_ID", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserID", userId);

                    await conn.OpenAsync();
                    var projects = new List<ProjectModel>();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            projects.Add(new ProjectModel
                            {
                                ID = (int)reader["ID"],
                                Code = reader["Code"].ToString(),
                                EnglishName = reader["EnglishName"].ToString(),
                                ArabicName = reader["ArabicName"].ToString(),
                                Url = reader["Url"].ToString(),
                                IsActive = (bool)reader["IsActive"],
                                Sort = (int)reader["Sort"],
                                Tag = reader["Tag"].ToString()
                            });
                        }
                    }

                    // Check if projects are found
                    if (projects.Count == 0)
                    {
                        return NotFound(); // Return 404 if no projects found
                    }

                    return Ok(projects); // Return 200 with project data
                }
            }
        }


        [HttpGet("settings")]
        [ProducesResponseType(typeof(IDictionary<string, string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSettings()
        {
            var settings = new Dictionary<string, string>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT ID, Value FROM Setting", conn))
                {
                    await conn.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var id = reader["ID"].ToString();
                            var value = reader["Value"].ToString();
                            settings[id] = value; // Add to dictionary
                        }
                    }
                }
            }

            // Return 404 if no settings found
            if (settings.Count == 0)
            {
                return NotFound(); // Return 404 if no data found
            }

            return Ok(settings); // Return 200 with settings
        }


        [HttpGet("traffic-by-department")]
        [ProducesResponseType(typeof(IEnumerable<TrafficByDepartmentModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTrafficByDepartment()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_TRAFFIC_BY_DEPARTMENT", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    await conn.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        var results = new List<TrafficByDepartmentModel>();

                        while (await reader.ReadAsync())
                        {
                            results.Add(new TrafficByDepartmentModel
                            {
                                DateID = reader["DateID"].ToString(),
                                Department = reader["Department"].ToString(),
                                Total = reader["Total"].ToString()
                            });
                        }

                        // Return 404 if no results found
                        if (results.Count == 0)
                        {
                            return NotFound(); // Return 404 if no data found
                        }

                        return Ok(results); // Return 200 with results
                    }
                }
            }
        }



        [HttpGet("traffic-by-module")]
        [ProducesResponseType(typeof(IEnumerable<TrafficByModuleModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTrafficByModule()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_TRAFFIC_BY_MODULE", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    await conn.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        var results = new List<TrafficByModuleModel>();

                        while (await reader.ReadAsync())
                        {
                            results.Add(new TrafficByModuleModel
                            {
                                DateID = reader["DateID"].ToString(),
                                Module = reader["Module"].ToString(),
                                Total = reader["Total"].ToString()
                            });
                        }

                        // Return 404 if no results found
                        if (results.Count == 0)
                        {
                            return NotFound(); // Return 404 if no data found
                        }

                        return Ok(results); // Return 200 with results
                    }
                }
            }
        }



        [HttpGet("active-project/{permissionId}")]
        [ProducesResponseType(typeof(ActiveProjectModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetActiveProject(int permissionId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_GET_ACTIVE_PROJECT", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PermissionID", permissionId);

                    await conn.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var project = new ActiveProjectModel
                            {
                                ID = (int)reader["ID"],
                                Code = reader["Code"].ToString(),
                                EnglishName = reader["EnglishName"].ToString(),
                                ArabicName = reader["ArabicName"].ToString(),
                                Url = reader["Url"].ToString(),
                                Sort = (int)reader["Sort"],
                                IsActive = (bool)reader["IsActive"],
                                Tag = reader["Tag"].ToString()
                            };

                            return Ok(project); // Return 200 with project data
                        }
                        else
                        {
                            return NotFound(); // Return 404 if no project found
                        }
                    }
                }
            }
        }



        [HttpGet("first-action/{userId}/{projectId}")]
        [ProducesResponseType(typeof(ActionDetailsModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFirstAction(int userId, int projectId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_ACTION_GET_FIRST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@ProjectID", projectId);

                    await conn.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var actionDetails = new ActionDetailsModel
                            {
                                ProjectID = (int)reader["ProjectID"],
                                ProjectEnglishName = reader["ProjectEnglishName"].ToString(),
                                ProjectArabicName = reader["ProjectArabicName"].ToString(),
                                MenuID = (int)reader["MenuID"],
                                MenuParentID = reader["MenuParentID"] as int?,
                                MenuEnglishDescription = reader["MenuEnglishDescription"].ToString(),
                                MenuArabicDescription = reader["MenuArabicDescription"].ToString(),
                                MenuSort = (int)reader["MenuSort"],
                                MenuTag = reader["MenuTag"].ToString(),
                                ModuleID = (int)reader["ModuleID"],
                                ModuleEnglishDescription = reader["ModuleEnglishDescription"].ToString(),
                                ModuleArabicDescription = reader["ModuleArabicDescription"].ToString(),
                                ModuleSort = (int)reader["ModuleSort"],
                                ModuleTag = reader["ModuleTag"].ToString(),
                                ActionID = reader["ActionID"].ToString(),
                                ControllerID = reader["ControllerID"].ToString(),
                                Url = reader["Url"].ToString(),
                                IsMenu = (bool)reader["IsMenu"]
                            };

                            return Ok(actionDetails); // Return 200 with action details
                        }
                        else
                        {
                            return NotFound(); // Return 404 if no action found
                        }
                    }
                }
            }
        }


        [HttpGet("permissions-tree/{roleId}/{projectId}")]
        [ProducesResponseType(typeof(IEnumerable<PermissionTreeNodeModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPermissionTreeByRoleId(int roleId, int? projectId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_PERMISSION_TREE_SELECT_BY_ROLE_ID", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@RoleID", roleId);
                    cmd.Parameters.AddWithValue("@ProjectID", projectId.HasValue ? (object)projectId.Value : DBNull.Value);

                    await conn.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        var results = new List<PermissionTreeNodeModel>();

                        while (await reader.ReadAsync())
                        {
                            results.Add(new PermissionTreeNodeModel
                            {
                                ID = (int)reader["ID"],
                                ParentID = reader["ParentID"].ToString(),
                                Description = reader["Description"].ToString(),
                                IsGranted = (int)reader["IsGranted"],
                                PermissionID = reader["PermissionID"] as int?,
                                IsMenu = (int)reader["IsMenu"],
                                Tag = reader["Tag"].ToString(),
                                Sort = (int)reader["Sort"],
                                RoleID = (int)reader["RoleID"]
                            });
                        }

                        // Return 404 if no results found
                        if (results.Count == 0)
                        {
                            return NotFound(); // Return 404 if no data found
                        }

                        return Ok(results); // Return 200 with results
                    }
                }
            }
        }


        [HttpGet("system-monitor")]
        [ProducesResponseType(typeof(IEnumerable<SystemMonitorModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSystemMonitor(int page = 0, int size = 100, string sortColumn = "ID", string sortDirection = "ASC", string filter = "")
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_SYSTEM_MONITOR_SELECT", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Page", page);
                    cmd.Parameters.AddWithValue("@Size", size);
                    cmd.Parameters.AddWithValue("@SortColumn", sortColumn);
                    cmd.Parameters.AddWithValue("@SortDirection", sortDirection);
                    cmd.Parameters.AddWithValue("@Filter", filter);
                    SqlParameter totalRowParam = new SqlParameter("@TotalRow", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(totalRowParam);

                    await conn.OpenAsync();
                    var results = new List<SystemMonitorModel>();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new SystemMonitorModel
                            {
                                Index = (decimal)reader["Index"],
                                ID = (decimal)reader["ID"],
                                TransactionTime = (DateTime)reader["TransactionTime"],
                                UserName = reader["UserName"].ToString(),
                                Department = reader["Department"].ToString(),
                                EnglishDescription = reader["EnglishDescription"].ToString(),
                                Photo = reader["Photo"] as byte[],
                                Module = reader["Module"].ToString(),
                                Permission = reader["Permission"].ToString(),
                                Parameters = reader["Parameters"].ToString(),
                                IPAddress = reader["IPAddress"].ToString(),
                                MachineName = reader["MachineName"].ToString()
                            });
                        }
                    }

                    // Check if results are found
                    if (results.Count == 0)
                    {
                        return NotFound(); // Return 404 if no data found
                    }

                    // Return total row count as part of the response header
                    Response.Headers.Add("X-Total-Count", totalRowParam.Value.ToString());
                    return Ok(results); // Return 200 with results
                }
            }
        }



        [HttpGet("users/{roleId}")]
        [ProducesResponseType(typeof(IEnumerable<UserModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUsersByRoleId(int roleId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_USER_SELECT_BY_ROLE_ID", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@RoleID", roleId);

                    await conn.OpenAsync();
                    var results = new List<UserModel>();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new UserModel
                            {
                                ID = (int)reader["ID"],
                                Code = reader["Code"].ToString(),
                                EnglishDescription = reader["EnglishDescription"].ToString(),
                                ArabicDescription = reader["ArabicDescription"].ToString(),
                                Department = reader["Department"].ToString(),
                                IsAdmin = (bool)reader["IsAdmin"],
                                IsLocked = (bool)reader["IsLocked"],
                                IsSMSReceiver = (bool)reader["IsSMSReceiver"],
                                IsZoneAdmin = (bool)reader["IsZoneAdmin"],
                                IsSegmentAdmin = (bool)reader["IsSegmentAdmin"],
                                IsVendorAdmin = (bool)reader["IsVendorAdmin"],
                                Email = reader["Email"].ToString(),
                                Mobile = reader["Mobile"].ToString(),
                                Tel = reader["Tel"].ToString(),
                                Photo = reader["Photo"] as byte[],
                                LastADInfoUpdateTime = reader["LastADInfoUpdateTime"] as DateTime?,
                                Tag = reader["Tag"].ToString(),
                                ExpirationDate = reader["ExpirationDate"] as DateTime?,
                                CreateUserID = reader["CreateUserID"] as int?,
                                CreateTime = (DateTime)reader["CreateTime"],
                                LastUpdateUserID = reader["LastUpdateUserID"] as int?,
                                LastUpdateTime = reader["LastUpdateTime"] as DateTime?,
                                RoleID = (int)reader["RoleID"],
                                IsActive = (bool)reader["IsActive"]
                            });
                        }
                    }

                    // Check if results are found
                    if (results.Count == 0)
                    {
                        return NotFound(); // Return 404 if no data found
                    }

                    return Ok(results); // Return 200 with results
                }
            }
        }



        [HttpGet("users")]
        [ProducesResponseType(typeof(IEnumerable<UserModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUsers(int page = 0, int size = 100, string sortColumn = "ID", string sortDirection = "ASC", string filter = "")
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_USER_SELECT", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Page", page);
                    cmd.Parameters.AddWithValue("@Size", size);
                    cmd.Parameters.AddWithValue("@SortColumn", sortColumn);
                    cmd.Parameters.AddWithValue("@SortDirection", sortDirection);
                    cmd.Parameters.AddWithValue("@Filter", filter);
                    SqlParameter totalRowParam = new SqlParameter("@TotalRow", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(totalRowParam);

                    await conn.OpenAsync();
                    var results = new List<UserModel>();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new UserModel
                            {
                                Index = (int)reader["Index"],
                                ID = (int)reader["ID"],
                                Code = reader["Code"].ToString(),
                                EnglishDescription = reader["EnglishDescription"].ToString(),
                                ArabicDescription = reader["ArabicDescription"].ToString(),
                                Department = reader["Department"].ToString(),
                                IsAdmin = (bool)reader["IsAdmin"],
                                IsLocked = (bool)reader["IsLocked"],
                                IsSMSReceiver = (bool)reader["IsSMSReceiver"],
                                IsZoneAdmin = (bool)reader["IsZoneAdmin"],
                                IsSegmentAdmin = (bool)reader["IsSegmentAdmin"],
                                IsVendorAdmin = (bool)reader["IsVendorAdmin"],
                                Email = reader["Email"].ToString(),
                                Mobile = reader["Mobile"].ToString(),
                                Tel = reader["Tel"].ToString(),
                                Photo = reader["Photo"] as byte[],
                                LastADInfoUpdateTime = reader["LastADInfoUpdateTime"] as DateTime?,
                                Tag = reader["Tag"].ToString(),
                                ExpirationDate = reader["ExpirationDate"] as DateTime?,
                                CreateUserID = reader["CreateUserID"] as int?,
                                CreateTime = (DateTime)reader["CreateTime"],
                                LastUpdateUserID = reader["LastUpdateUserID"] as int?,
                                LastUpdateTime = reader["LastUpdateTime"] as DateTime?
                            });
                        }
                    }

                    // Check if results are found
                    if (results.Count == 0)
                    {
                        return NotFound(); // Return 404 if no data found
                    }

                    // Return total row count as part of the response header
                    Response.Headers.Add("X-Total-Count", totalRowParam.Value.ToString());
                    return Ok(results); // Return 200 with results
                }
            }
        }




        [HttpGet("users/search")]
        [ProducesResponseType(typeof(IEnumerable<UserModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SearchUsers(string filter)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_SECURITY_USER_SEARCH", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Filter", filter);

                    await conn.OpenAsync();
                    var results = new List<UserModel>();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new UserModel
                            {
                                ID = (int)reader["ID"],
                                Code = reader["Code"].ToString(),
                                EnglishDescription = reader["EnglishDescription"].ToString(),
                                ArabicDescription = reader["ArabicDescription"].ToString(),
                                Department = reader["Department"].ToString(),
                                IsAdmin = (bool)reader["IsAdmin"],
                                IsLocked = (bool)reader["IsLocked"],
                                IsSMSReceiver = (bool)reader["IsSMSReceiver"],
                                IsZoneAdmin = (bool)reader["IsZoneAdmin"],
                                IsSegmentAdmin = (bool)reader["IsSegmentAdmin"],
                                IsVendorAdmin = (bool)reader["IsVendorAdmin"],
                                IsDomainAdmin = (bool)reader["IsDomainAdmin"],
                                IsSMSAdmin = (bool)reader["IsSMSAdmin"],
                                Email = reader["Email"].ToString(),
                                Mobile = reader["Mobile"].ToString(),
                                Tel = reader["Tel"].ToString(),
                                Photo = reader["Photo"] as byte[],
                                LastADInfoUpdateTime = reader["LastADInfoUpdateTime"] as DateTime?,
                                Tag = reader["Tag"].ToString(),
                                ExpirationDate = reader["ExpirationDate"] as DateTime?,
                                CreateUserID = reader["CreateUserID"] as int?,
                                CreateTime = (DateTime)reader["CreateTime"],
                                LastUpdateUserID = reader["LastUpdateUserID"] as int?,
                                LastUpdateTime = reader["LastUpdateTime"] as DateTime?,
                                IsWelcomeEmailSent = (bool)reader["IsWelcomeEmailSent"]
                            });
                        }
                    }

                    if (results.Count == 0)
                    {
                        return NotFound(); // Return 404 if no data found
                    }

                    return Ok(results); // Return 200 with results
                }
            }
        }



        [HttpGet("pod-status/{podId}")]
        [ProducesResponseType(typeof(PodStatusModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPodStatusById(decimal podId, [FromQuery] string userCode)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_WORKFLOW_POD_STATUS_SELECT_BY_ID", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PODID", podId);
                    cmd.Parameters.AddWithValue("@UserCode", userCode);

                    await conn.OpenAsync();
                    var results = new List<PodStatusModel>();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new PodStatusModel
                            {
                                Index = (decimal)reader["Index"],
                                PodID = (decimal)reader["PodID"],
                                PodCode = reader["PodCode"].ToString(),
                                PodName = reader["PodName"].ToString(),
                                PODTypeID = (int)reader["PODTypeID"],
                                PODTypeName = reader["PODTypeName"].ToString(),
                                PODTypeUrl = reader["PODTypeUrl"].ToString(),
                                PODCreateTime = (DateTime)reader["PODCreateTime"],
                                PeriodID = reader["PeriodID"].ToString(),
                                ZoneID = (int)reader["ZoneID"],
                                ZoneName = reader["ZoneName"].ToString(),
                                VendorID = (int)reader["VendorID"],
                                VendorName = reader["VendorName"].ToString(),
                                StepID = (int)reader["StepID"],
                                StepName = reader["StepName"].ToString(),
                                RoleID = (int)reader["RoleID"],
                                RoleName = reader["RoleName"].ToString(),
                                IsFirstStep = (bool)reader["IsFirstStep"],
                                IsPreview = (bool)reader["IsPreview"],
                                IncludeAttachements = (bool)reader["IncludeAttachements"],
                                IncludeComment = (bool)reader["IncludeComment"],
                                AllowEdit = (bool)reader["AllowEdit"],
                                DataSensitivityLevel = (int)reader["DataSensitivityLevel"],
                                LastTransactionDate = (DateTime)reader["LastTransactionDate"],
                                LastComment = reader["LastComment"].ToString(),
                                TotalActions = (int)reader["TotalActions"],
                                IsUserPending = (bool)reader["IsUserPending"]
                            });
                        }
                    }

                    if (results.Count == 0)
                    {
                        return NotFound(); // Return 404 if no data found
                    }

                    return Ok(results); // Return 200 with results
                }
            }
        }




        [HttpGet("receivers/{roleId}")]
        [ProducesResponseType(typeof(List<ReceiverModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetReceiversByRoleId(int roleId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_WORKFLOW_RECEIVERS_SELECT_BY_ROLE_ID", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@RoleID", roleId);

                    await conn.OpenAsync();
                    var receivers = new List<ReceiverModel>();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            receivers.Add(new ReceiverModel
                            {
                                EnglishDescription = reader["EnglishDescription"].ToString(),
                                Department = reader["Department"].ToString(),
                                Email = reader["Email"].ToString(),
                                Mobile = reader["Mobile"].ToString()
                            });
                        }
                    }

                    if (receivers.Count == 0)
                    {
                        return NotFound(); // Return 404 if no data found
                    }

                    return Ok(receivers); // Return 200 with results
                }
            }
        }




        [HttpGet("workflow-status/{workflowId}")]
        [ProducesResponseType(typeof(WorkflowStatusModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWorkflowStatusById(decimal workflowId, [FromQuery] string userCode)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_WORKFLOW_STATUS_SELECT_BY_ID", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@WorkflowID", workflowId);
                    cmd.Parameters.AddWithValue("@UserCode", userCode);

                    await conn.OpenAsync();
                    var results = new List<WorkflowStatusModel>();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new WorkflowStatusModel
                            {
                                Index = (decimal)reader["Index"],
                                WorkflowID = (decimal)reader["WorkflowID"],
                                WorkflowCode = reader["WorkflowCode"].ToString(),
                                PatternID = (int)reader["PatternID"],
                                PatternName = reader["PatternName"].ToString(),
                                StepID = (int)reader["StepID"],
                                StepName = reader["StepName"].ToString(),
                                RoleID = (int)reader["RoleID"],
                                RoleName = reader["RoleName"].ToString(),
                                IsFirstStep = (bool)reader["IsFirstStep"],
                                IsPreview = (bool)reader["IsPreview"],
                                IncludeAttachements = (bool)reader["IncludeAttachements"],
                                IncludeComment = (bool)reader["IncludeComment"],
                                AllowEdit = (bool)reader["AllowEdit"],
                                DataSensitivityLevel = (int)reader["DataSensitivityLevel"],
                                LastTransactionDate = (DateTime)reader["LastTransactionDate"],
                                LastComment = reader["LastComment"].ToString(),
                                TotalActions = (int)reader["TotalActions"],
                                IsUserPending = (bool)reader["IsUserPending"]
                            });
                        }
                    }

                    if (results.Count == 0)
                    {
                        return NotFound(); // Return 404 if no data found
                    }

                    return Ok(results); // Return 200 with results
                }
            }
        }



        [HttpGet("domains/{userId}")]
        [ProducesResponseType(typeof(List<DomainModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDomainsByUserId(int userId, [FromQuery] int? projectId = null)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_WORKFLOW_DOMAIN_SELECT_BY_USER_ID", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@ProjectID", projectId.HasValue ? (object)projectId.Value : DBNull.Value);

                    await conn.OpenAsync();
                    var domains = new List<DomainModel>();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            domains.Add(new DomainModel
                            {
                                UserID = (int)reader["UserID"],
                                DomainID = (int)reader["DomainID"],
                                DomainName = reader["DomainName"].ToString(),
                                ProjectID = (int)reader["ProjectID"]
                            });
                        }
                    }

                    if (domains.Count == 0)
                    {
                        return NotFound(); // Return 404 if no data found
                    }

                    return Ok(domains); // Return 200 with results
                }
            }
        }




        [HttpGet("vendors/{userId}")]
        [ProducesResponseType(typeof(List<VendorModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetVendorsByUserId(int userId, [FromQuery] int? projectId = null)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.S_WORKFLOW_VENDOR_SELECT_BY_USER_ID", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@ProjectID", projectId.HasValue ? (object)projectId.Value : DBNull.Value);

                    await conn.OpenAsync();
                    var vendors = new List<VendorModel>();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            vendors.Add(new VendorModel
                            {
                                UserID = (int)reader["UserID"],
                                VendorID = (int)reader["VendorID"],
                                VendorName = reader["VendorName"].ToString(),
                                ProjectID = (int)reader["ProjectID"]
                            });
                        }
                    }

                    if (vendors.Count == 0)
                    {
                        return NotFound(); // Return 404 if no data found
                    }

                    return Ok(vendors); // Return 200 with results
                }
            }
        }



        [HttpGet("users/code/{code}")]
        [ProducesResponseType(typeof(UserModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserByCode(string code)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                // Query to select user by code directly from the User table
                string query = "SELECT * FROM [User] WHERE Code = @Code";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Code", code.ToLower());  // Ensure code is passed in lowercase

                    await conn.OpenAsync();
                    UserModel user = null;

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            user = new UserModel
                            {
                                ID = (int)reader["ID"],
                                Code = reader["Code"].ToString(),
                                EnglishDescription = reader["EnglishDescription"].ToString(),
                                ArabicDescription = reader["ArabicDescription"].ToString(),
                                Department = reader["Department"].ToString(),
                                FirstName = reader["FirstName"].ToString(),
                                Title = reader["Title"].ToString(),
                                IsAdmin = reader["IsAdmin"] != DBNull.Value && (bool)reader["IsAdmin"],
                                IsLocked = reader["IsLocked"] != DBNull.Value && (bool)reader["IsLocked"],
                                IsSMSReceiver = reader["IsSMSReceiver"] != DBNull.Value && (bool)reader["IsSMSReceiver"],
                                IsZoneAdmin = reader["IsZoneAdmin"] != DBNull.Value && (bool)reader["IsZoneAdmin"],
                                IsSegmentAdmin = reader["IsSegmentAdmin"] != DBNull.Value && (bool)reader["IsSegmentAdmin"],
                                IsVendorAdmin = reader["IsVendorAdmin"] != DBNull.Value && (bool)reader["IsVendorAdmin"],
                                Email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : null,
                                Mobile = reader["Mobile"] != DBNull.Value ? reader["Mobile"].ToString() : null,
                                Tel = reader["Tel"] != DBNull.Value ? reader["Tel"].ToString() : null,
                                Photo = reader["Photo"] as byte[],
                                LastADInfoUpdateTime = reader["LastADInfoUpdateTime"] as DateTime?,
                                Tag = reader["Tag"] != DBNull.Value ? reader["Tag"].ToString() : null,
                                ExpirationDate = reader["ExpirationDate"] as DateTime?,
                                CreateUserID = reader["CreateUserID"] != DBNull.Value ? (int?)reader["CreateUserID"] : null,
                                CreateTime = reader["CreateTime"] != DBNull.Value ? (DateTime)reader["CreateTime"] : default(DateTime), // Handle default
                                LastUpdateUserID = reader["LastUpdateUserID"] != DBNull.Value ? (int?)reader["LastUpdateUserID"] : null,
                                LastUpdateTime = reader["LastUpdateTime"] as DateTime?,
                            };
                        }

                        // Check if user is found
                        if (user == null)
                        {
                            return NotFound(); // Return 404 if no user found
                        }

                        return Ok(user); // Return 200 with user data
                    }
                }
            }
        }

        #endregion









    }


}
