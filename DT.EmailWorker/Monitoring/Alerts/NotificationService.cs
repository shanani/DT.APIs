using DT.EmailWorker.Core.Configuration;
using DT.EmailWorker.Models.Entities;
using DT.EmailWorker.Models.Enums;
using DT.EmailWorker.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace DT.EmailWorker.Monitoring.Alerts
{
    /// <summary>
    /// Service for sending alert notifications via various channels
    /// </summary>
    public class NotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly EmailWorkerSettings _settings;
        private readonly IEmailQueueService _emailQueueService;
        private readonly Queue<Alert> _alertHistory = new();
        private readonly object _lockObject = new();
        private const int MaxAlertHistory = 1000;

        public NotificationService(
            ILogger<NotificationService> logger,
            IOptions<EmailWorkerSettings> settings,
            IEmailQueueService emailQueueService)
        {
            _logger = logger;
            _settings = settings.Value;
            _emailQueueService = emailQueueService;
        }

        /// <summary>
        /// Send alert notification
        /// </summary>
        public async Task SendAlertAsync(Alert alert)
        {
            try
            {
                // Store in history
                StoreAlertInHistory(alert);

                // Log the alert
                LogAlert(alert);

                // Send notifications based on configuration
                var tasks = new List<Task>();

                if (!string.IsNullOrEmpty(_settings.AlertEmail))
                {
                    tasks.Add(SendEmailNotificationAsync(alert));
                }

                if (!string.IsNullOrEmpty(_settings.WebhookUrl))
                {
                    tasks.Add(SendWebhookNotificationAsync(alert));
                }

                // Wait for all notifications to complete
                await Task.WhenAll(tasks);

                _logger.LogInformation("Alert notification sent successfully: {AlertId} - {Title}", alert.Id, alert.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send alert notification: {AlertId}", alert.Id);
            }
        }

        /// <summary>
        /// Send batch of alerts
        /// </summary>
        public async Task SendBatchAlertsAsync(IEnumerable<Alert> alerts)
        {
            var alertList = alerts.ToList();
            if (!alertList.Any()) return;

            try
            {
                // Store all alerts in history
                foreach (var alert in alertList)
                {
                    StoreAlertInHistory(alert);
                    LogAlert(alert);
                }

                // Send batch notifications
                var tasks = new List<Task>();

                if (!string.IsNullOrEmpty(_settings.AlertEmail))
                {
                    tasks.Add(SendBatchEmailNotificationAsync(alertList));
                }

                if (!string.IsNullOrEmpty(_settings.WebhookUrl))
                {
                    tasks.Add(SendBatchWebhookNotificationAsync(alertList));
                }

                await Task.WhenAll(tasks);

                _logger.LogInformation("Batch alert notification sent successfully: {AlertCount} alerts", alertList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send batch alert notifications");
            }
        }

        /// <summary>
        /// Get recent alert history
        /// </summary>
        public List<Alert> GetRecentAlerts(int count = 50)
        {
            lock (_lockObject)
            {
                return _alertHistory.TakeLast(count).ToList();
            }
        }

        /// <summary>
        /// Get alerts by level
        /// </summary>
        public List<Alert> GetAlertsByLevel(AlertLevel level, DateTime? since = null)
        {
            lock (_lockObject)
            {
                var query = _alertHistory.Where(a => a.Level == level);
                if (since.HasValue)
                {
                    query = query.Where(a => a.Timestamp >= since.Value);
                }
                return query.ToList();
            }
        }

        /// <summary>
        /// Get alert statistics
        /// </summary>
        public AlertStatistics GetAlertStatistics(TimeSpan period)
        {
            lock (_lockObject)
            {
                var cutoff = DateTime.UtcNow.AddHours(3) - period;
                var recentAlerts = _alertHistory.Where(a => a.Timestamp >= cutoff).ToList();

                return new AlertStatistics
                {
                    Period = period,
                    TotalAlerts = recentAlerts.Count,
                    CriticalAlerts = recentAlerts.Count(a => a.Level == AlertLevel.Critical),
                    WarningAlerts = recentAlerts.Count(a => a.Level == AlertLevel.Warning),
                    InfoAlerts = recentAlerts.Count(a => a.Level == AlertLevel.Info),
                    MostCommonSource = recentAlerts
                        .GroupBy(a => a.Source)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key ?? "Unknown",
                    AlertsByLevel = recentAlerts
                        .GroupBy(a => a.Level)
                        .ToDictionary(g => g.Key, g => g.Count())
                };
            }
        }

        #region Private Methods

        private void StoreAlertInHistory(Alert alert)
        {
            lock (_lockObject)
            {
                _alertHistory.Enqueue(alert);

                // Keep only recent alerts
                while (_alertHistory.Count > MaxAlertHistory)
                {
                    _alertHistory.Dequeue();
                }
            }
        }

        private void LogAlert(Alert alert)
        {
            var logLevel = alert.Level switch
            {
                AlertLevel.Critical => LogLevel.Critical,
                AlertLevel.Warning => LogLevel.Warning,
                AlertLevel.Info => LogLevel.Information,
                _ => LogLevel.Information
            };

            _logger.Log(logLevel, "ALERT [{Level}] {Title}: {Message} (Source: {Source})",
                alert.Level, alert.Title, alert.Message, alert.Source);
        }

        private async Task SendEmailNotificationAsync(Alert alert)
        {
            try
            {
                var emailRequest = new Models.DTOs.EmailProcessingRequest
                {
                    ToEmails = _settings.AlertEmail!,
                    Subject = FormatEmailSubject(alert),
                    Body = FormatEmailBody(alert),
                    IsHtml = true,
                    Priority = alert.Level switch
                    {
                        AlertLevel.Critical => EmailPriority.High,
                        AlertLevel.Warning => EmailPriority.Normal,
                        _ => EmailPriority.Low
                    },
                    CreatedBy = "NotificationService",
                    RequestSource = "AlertNotification"
                };

                await _emailQueueService.QueueEmailAsync(emailRequest);
                _logger.LogDebug("Alert email queued for {AlertLevel} alert: {Title}", alert.Level, alert.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue alert email");
            }
        }

        private async Task SendBatchEmailNotificationAsync(List<Alert> alerts)
        {
            try
            {
                var criticalAlerts = alerts.Where(a => a.Level == AlertLevel.Critical).ToList();
                var warningAlerts = alerts.Where(a => a.Level == AlertLevel.Warning).ToList();
                var infoAlerts = alerts.Where(a => a.Level == AlertLevel.Info).ToList();

                var emailRequest = new Models.DTOs.EmailProcessingRequest
                {
                    ToEmails = _settings.AlertEmail!,
                    Subject = FormatBatchEmailSubject(alerts),
                    Body = FormatBatchEmailBody(alerts, criticalAlerts, warningAlerts, infoAlerts),
                    IsHtml = true,
                    Priority = criticalAlerts.Any() ? EmailPriority.High : EmailPriority.Normal,
                    CreatedBy = "NotificationService",
                    RequestSource = "BatchAlertNotification"
                };

                await _emailQueueService.QueueEmailAsync(emailRequest);
                _logger.LogDebug("Batch alert email queued with {AlertCount} alerts", alerts.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue batch alert email");
            }
        }

        private async Task SendWebhookNotificationAsync(Alert alert)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var payload = new
                {
                    alert.Id,
                    alert.Title,
                    alert.Message,
                    Level = alert.Level.ToString(),
                    alert.Timestamp,
                    alert.Source,
                    alert.RuleId,
                    Service = "DT.EmailWorker"
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(_settings.WebhookUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Webhook notification sent successfully for alert: {AlertId}", alert.Id);
                }
                else
                {
                    _logger.LogWarning("Webhook notification failed with status {StatusCode} for alert: {AlertId}",
                        response.StatusCode, alert.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send webhook notification for alert: {AlertId}", alert.Id);
            }
        }

        private async Task SendBatchWebhookNotificationAsync(List<Alert> alerts)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var payload = new
                {
                    BatchId = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.UtcNow.AddHours(3),
                    AlertCount = alerts.Count,
                    Service = "DT.EmailWorker",
                    Alerts = alerts.Select(a => new
                    {
                        a.Id,
                        a.Title,
                        a.Message,
                        Level = a.Level.ToString(),
                        a.Timestamp,
                        a.Source,
                        a.RuleId
                    })
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(_settings.WebhookUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Batch webhook notification sent successfully for {AlertCount} alerts", alerts.Count);
                }
                else
                {
                    _logger.LogWarning("Batch webhook notification failed with status {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send batch webhook notification");
            }
        }

        private string FormatEmailSubject(Alert alert)
        {
            var prefix = alert.Level switch
            {
                AlertLevel.Critical => "[CRITICAL]",
                AlertLevel.Warning => "[WARNING]",
                AlertLevel.Info => "[INFO]",
                _ => ""
            };

            return $"{prefix} DT.EmailWorker Alert: {alert.Title}";
        }

        private string FormatEmailBody(Alert alert)
        {
            var levelColor = alert.Level switch
            {
                AlertLevel.Critical => "#dc3545",
                AlertLevel.Warning => "#ffc107",
                AlertLevel.Info => "#17a2b8",
                _ => "#6c757d"
            };

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>DT.EmailWorker Alert</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: {levelColor}; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0;'>
        <h1 style='margin: 0; font-size: 24px;'>{alert.Level.ToString().ToUpper()} ALERT</h1>
    </div>
    
    <div style='background: #f8f9fa; padding: 20px; border-radius: 0 0 8px 8px; border: 1px solid #dee2e6;'>
        <h2 style='margin-top: 0; color: {levelColor};'>{alert.Title}</h2>
        
        <div style='background: white; padding: 15px; border-radius: 6px; margin: 15px 0;'>
            <p style='margin: 0; font-size: 16px;'>{alert.Message}</p>
        </div>
        
        <table style='width: 100%; border-collapse: collapse; margin: 15px 0;'>
            <tr>
                <td style='padding: 8px 0; border-bottom: 1px solid #eee; font-weight: bold;'>Alert ID:</td>
                <td style='padding: 8px 0; border-bottom: 1px solid #eee;'>{alert.Id}</td>
            </tr>
            <tr>
                <td style='padding: 8px 0; border-bottom: 1px solid #eee; font-weight: bold;'>Timestamp:</td>
                <td style='padding: 8px 0; border-bottom: 1px solid #eee;'>{alert.Timestamp:yyyy-MM-dd HH:mm:ss} UTC</td>
            </tr>
            <tr>
                <td style='padding: 8px 0; border-bottom: 1px solid #eee; font-weight: bold;'>Source:</td>
                <td style='padding: 8px 0; border-bottom: 1px solid #eee;'>{alert.Source}</td>
            </tr>
            {(string.IsNullOrEmpty(alert.RuleId) ? "" : $@"
            <tr>
                <td style='padding: 8px 0; font-weight: bold;'>Rule ID:</td>
                <td style='padding: 8px 0;'>{alert.RuleId}</td>
            </tr>")}
        </table>
        
        <p style='font-size: 14px; color: #6c757d; margin: 20px 0 0 0;'>
            This alert was generated by the DT.EmailWorker monitoring system.
        </p>
    </div>
</body>
</html>";
        }

        private string FormatBatchEmailSubject(List<Alert> alerts)
        {
            var criticalCount = alerts.Count(a => a.Level == AlertLevel.Critical);
            var warningCount = alerts.Count(a => a.Level == AlertLevel.Warning);

            if (criticalCount > 0)
            {
                return $"[CRITICAL] DT.EmailWorker: {criticalCount} Critical, {warningCount} Warning Alerts";
            }
            else if (warningCount > 0)
            {
                return $"[WARNING] DT.EmailWorker: {warningCount} Warning Alerts";
            }
            else
            {
                return $"[INFO] DT.EmailWorker: {alerts.Count} Alerts Summary";
            }
        }

        private string FormatBatchEmailBody(List<Alert> allAlerts, List<Alert> critical, List<Alert> warning, List<Alert> info)
        {
            var summarySection = $@"
        <div style='background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #007bff;'>
            <h3 style='margin-top: 0; color: #007bff;'>Alert Summary</h3>
            <p><strong>Total Alerts:</strong> {allAlerts.Count}</p>
            <p><strong>Critical:</strong> {critical.Count} | <strong>Warning:</strong> {warning.Count} | <strong>Info:</strong> {info.Count}</p>
            <p><strong>Time Range:</strong> {allAlerts.Min(a => a.Timestamp):HH:mm:ss} - {allAlerts.Max(a => a.Timestamp):HH:mm:ss} UTC</p>
        </div>";

            var alertSections = "";

            if (critical.Any())
            {
                alertSections += FormatAlertSection("Critical Alerts", critical, "#dc3545");
            }

            if (warning.Any())
            {
                alertSections += FormatAlertSection("Warning Alerts", warning, "#ffc107");
            }

            if (info.Any())
            {
                alertSections += FormatAlertSection("Info Alerts", info, "#17a2b8");
            }

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>DT.EmailWorker Batch Alert</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: #343a40; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0;'>
        <h1 style='margin: 0; font-size: 24px;'>DT.EmailWorker Alert Summary</h1>
    </div>
    
    <div style='background: #f8f9fa; padding: 20px; border-radius: 0 0 8px 8px; border: 1px solid #dee2e6;'>
        {summarySection}
        {alertSections}
        
        <p style='font-size: 14px; color: #6c757d; margin: 20px 0 0 0;'>
            This batch alert summary was generated by the DT.EmailWorker monitoring system.
        </p>
    </div>
</body>
</html>";
        }

        private string FormatAlertSection(string title, List<Alert> alerts, string color)
        {
            var alertItems = string.Join("", alerts.Take(10).Select(a => $@"
            <tr>
                <td style='padding: 8px; border-bottom: 1px solid #eee; font-weight: bold;'>{a.Title}</td>
                <td style='padding: 8px; border-bottom: 1px solid #eee;'>{a.Message}</td>
                <td style='padding: 8px; border-bottom: 1px solid #eee; font-size: 12px;'>{a.Timestamp:HH:mm:ss}</td>
            </tr>"));

            var moreText = alerts.Count > 10 ? $"<p style='font-style: italic; color: #6c757d;'>... and {alerts.Count - 10} more alerts</p>" : "";

            return $@"
        <div style='background: white; padding: 15px; border-radius: 6px; margin: 15px 0; border-left: 4px solid {color};'>
            <h4 style='margin-top: 0; color: {color};'>{title} ({alerts.Count})</h4>
            <table style='width: 100%; border-collapse: collapse; font-size: 14px;'>
                <thead>
                    <tr style='background: #f8f9fa;'>
                        <th style='padding: 8px; text-align: left; border-bottom: 2px solid #dee2e6;'>Title</th>
                        <th style='padding: 8px; text-align: left; border-bottom: 2px solid #dee2e6;'>Message</th>
                        <th style='padding: 8px; text-align: left; border-bottom: 2px solid #dee2e6;'>Time</th>
                    </tr>
                </thead>
                <tbody>
                    {alertItems}
                </tbody>
            </table>
            {moreText}
        </div>";
        }

        #endregion
    }

    #region Data Classes

    public class AlertStatistics
    {
        public TimeSpan Period { get; set; }
        public int TotalAlerts { get; set; }
        public int CriticalAlerts { get; set; }
        public int WarningAlerts { get; set; }
        public int InfoAlerts { get; set; }
        public string MostCommonSource { get; set; } = string.Empty;
        public Dictionary<AlertLevel, int> AlertsByLevel { get; set; } = new();
    }

    #endregion
}