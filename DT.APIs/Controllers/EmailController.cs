using DT.APIs.Models;
using DT.APIs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DT.APIs.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/email-queue")]
    public class EmailController : ControllerBase
    {
        private readonly IEmailQueueService _emailQueueService;
        private readonly ILogger<EmailController> _logger;

        public EmailController(IEmailQueueService emailQueueService, ILogger<EmailController> logger)
        {
            _emailQueueService = emailQueueService;
            _logger = logger;
        }

        /// <summary>
        /// Queue a regular email for processing
        /// </summary>
        [HttpPost("queue")]
        [ProducesResponseType(typeof(QueueEmailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> QueueEmail([FromBody] QueueEmailRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _emailQueueService.QueueEmailAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing email");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Queue a template-based email for processing
        /// </summary>
        [HttpPost("queue-template")]
        [ProducesResponseType(typeof(QueueEmailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> QueueTemplateEmail([FromBody] QueueTemplateEmailRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _emailQueueService.QueueTemplateEmailAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing template email");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Queue multiple emails in bulk
        /// </summary>
        [HttpPost("queue-bulk")]
        [ProducesResponseType(typeof(BulkQueueEmailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> QueueBulkEmail([FromBody] QueueBulkEmailRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _emailQueueService.QueueBulkEmailAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing bulk email");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get status of a specific queued email
        /// </summary>
        [HttpGet("status/{queueId}")]
        [ProducesResponseType(typeof(EmailStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEmailStatus(Guid queueId)
        {
            try
            {
                var result = await _emailQueueService.GetEmailStatusAsync(queueId);
                if (result == null)
                {
                    return NotFound($"Email with ID {queueId} not found");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email status for {QueueId}", queueId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get status of multiple queued emails
        /// </summary>
        [HttpPost("status/batch")]
        [ProducesResponseType(typeof(List<EmailStatusResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetBatchEmailStatus([FromBody] List<Guid> queueIds)
        {
            try
            {
                if (queueIds == null || !queueIds.Any())
                {
                    return BadRequest("Queue IDs list cannot be empty");
                }

                var result = await _emailQueueService.GetBatchEmailStatusAsync(queueIds);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting batch email status");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Cancel a queued email
        /// </summary>
        [HttpPost("cancel/{queueId}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CancelEmail(Guid queueId)
        {
            try
            {
                var result = await _emailQueueService.CancelEmailAsync(queueId);
                if (!result)
                {
                    return NotFound($"Email with ID {queueId} not found or cannot be cancelled");
                }

                return Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling email {QueueId}", queueId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get email queue health status
        /// </summary>
        [HttpGet("health")]
        [ProducesResponseType(typeof(QueueHealthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetQueueHealth()
        {
            try
            {
                var result = await _emailQueueService.GetQueueHealthAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue health");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get email queue statistics
        /// </summary>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(QueueStatisticsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetQueueStatistics([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                var result = await _emailQueueService.GetQueueStatisticsAsync(fromDate, toDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue statistics");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get paginated list of queued emails
        /// </summary>
        [HttpGet("list")]
        [ProducesResponseType(typeof(PagedEmailQueueResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetQueuedEmails(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? status = null,
            [FromQuery] string? priority = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? search = null)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 50;

                var result = await _emailQueueService.GetQueuedEmailsAsync(page, pageSize, status, priority, fromDate, toDate, search);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queued emails");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}