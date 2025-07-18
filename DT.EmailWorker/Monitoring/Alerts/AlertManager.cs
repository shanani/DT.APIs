using DT.EmailWorker.Models.Enums;
using DT.EmailWorker.Monitoring.Metrics;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace DT.EmailWorker.Monitoring.Alerts
{
    /// <summary>
    /// Alert manager for monitoring and triggering alerts based on system conditions
    /// </summary>
    public class AlertManager : IDisposable
    {
        private readonly ILogger<AlertManager> _logger;
        private readonly NotificationService _notificationService;
        private readonly ConcurrentDictionary<string, AlertRule> _alertRules = new();
        private readonly ConcurrentDictionary<string, AlertState> _alertStates = new();
        private readonly Timer _evaluationTimer;

        public AlertManager(ILogger<AlertManager> logger, NotificationService notificationService)
        {
            _logger = logger;
            _notificationService = notificationService;

            // Initialize default alert rules
            InitializeDefaultAlertRules();

            // Start alert evaluation timer (every 2 minutes)
            _evaluationTimer = new Timer(EvaluateAlerts, null, TimeSpan.Zero, TimeSpan.FromMinutes(2));
        }

        /// <summary>
        /// Add or update an alert rule
        /// </summary>
        public void AddOrUpdateAlertRule(AlertRule rule)
        {
            _alertRules.AddOrUpdate(rule.Id, rule, (key, existing) => rule);
            _logger.LogInformation("Alert rule added/updated: {RuleId} - {RuleName}", rule.Id, rule.Name);
        }

        /// <summary>
        /// Remove an alert rule
        /// </summary>
        public bool RemoveAlertRule(string ruleId)
        {
            var removed = _alertRules.TryRemove(ruleId, out _);
            if (removed)
            {
                _alertStates.TryRemove(ruleId, out _);
                _logger.LogInformation("Alert rule removed: {RuleId}", ruleId);
            }
            return removed;
        }

        /// <summary>
        /// Get all active alerts
        /// </summary>
        public List<ActiveAlert> GetActiveAlerts()
        {
            return _alertStates.Values
                .Where(state => state.IsActive)
                .Select(state => new ActiveAlert
                {
                    RuleId = state.RuleId,
                    RuleName = _alertRules.TryGetValue(state.RuleId, out var rule) ? rule.Name : "Unknown",
                    Level = state.Level,
                    Message = state.LastMessage,
                    TriggeredAt = state.TriggeredAt,
                    LastEvaluated = state.LastEvaluated,
                    TriggerCount = state.TriggerCount
                })
                .OrderByDescending(alert => alert.TriggeredAt)
                .ToList();
        }

        /// <summary>
        /// Evaluate specific metrics against alert rules
        /// </summary>
        public void EvaluateMetrics(MetricsSummary metrics, SystemHealthStatus systemHealth)
        {
            foreach (var rule in _alertRules.Values)
            {
                try
                {
                    var shouldTrigger = EvaluateRule(rule, metrics, systemHealth);
                    UpdateAlertState(rule.Id, shouldTrigger, rule);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error evaluating alert rule {RuleId}", rule.Id);
                }
            }
        }

        /// <summary>
        /// Manually trigger an alert
        /// </summary>
        public async Task TriggerManualAlertAsync(string title, string message, AlertLevel level)
        {
            var alert = new Alert
            {
                Id = Guid.NewGuid().ToString(),
                Title = title,
                Message = message,
                Level = level,
                Timestamp = DateTime.UtcNow,
                Source = "Manual"
            };

            await _notificationService.SendAlertAsync(alert);
            _logger.LogWarning("Manual alert triggered: {Title} - {Level}", title, level);
        }

        /// <summary>
        /// Get alert rules
        /// </summary>
        public List<AlertRule> GetAlertRules()
        {
            return _alertRules.Values.ToList();
        }

        /// <summary>
        /// Get alert rule by ID
        /// </summary>
        public AlertRule? GetAlertRule(string ruleId)
        {
            return _alertRules.TryGetValue(ruleId, out var rule) ? rule : null;
        }

        /// <summary>
        /// Enable or disable alert rule
        /// </summary>
        public bool SetAlertRuleEnabled(string ruleId, bool enabled)
        {
            if (_alertRules.TryGetValue(ruleId, out var rule))
            {
                rule.IsEnabled = enabled;
                rule.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation("Alert rule {RuleId} {Action}", ruleId, enabled ? "enabled" : "disabled");
                return true;
            }
            return false;
        }

        #region Private Methods

        private void InitializeDefaultAlertRules()
        {
            // High failure rate alert
            AddOrUpdateAlertRule(new AlertRule
            {
                Id = "high_failure_rate",
                Name = "High Email Failure Rate",
                Description = "Triggers when email failure rate exceeds threshold",
                Level = AlertLevel.Warning,
                Condition = "failure_rate > 10",
                Threshold = 10,
                EvaluationPeriodMinutes = 15,
                CooldownMinutes = 30,
                IsEnabled = true
            });

            // Queue backlog alert
            AddOrUpdateAlertRule(new AlertRule
            {
                Id = "queue_backlog",
                Name = "Email Queue Backlog",
                Description = "Triggers when email queue has too many pending items",
                Level = AlertLevel.Warning,
                Condition = "pending_emails > 1000",
                Threshold = 1000,
                EvaluationPeriodMinutes = 5,
                CooldownMinutes = 15,
                IsEnabled = true
            });

            // Critical queue backlog
            AddOrUpdateAlertRule(new AlertRule
            {
                Id = "critical_queue_backlog",
                Name = "Critical Email Queue Backlog",
                Description = "Triggers when email queue has critically high pending items",
                Level = AlertLevel.Critical,
                Condition = "pending_emails > 5000",
                Threshold = 5000,
                EvaluationPeriodMinutes = 5,
                CooldownMinutes = 10,
                IsEnabled = true
            });

            // High CPU usage alert
            AddOrUpdateAlertRule(new AlertRule
            {
                Id = "high_cpu_usage",
                Name = "High CPU Usage",
                Description = "Triggers when CPU usage is consistently high",
                Level = AlertLevel.Warning,
                Condition = "cpu_usage > 80",
                Threshold = 80,
                EvaluationPeriodMinutes = 10,
                CooldownMinutes = 20,
                IsEnabled = true
            });

            // High memory usage alert
            AddOrUpdateAlertRule(new AlertRule
            {
                Id = "high_memory_usage",
                Name = "High Memory Usage",
                Description = "Triggers when memory usage exceeds threshold",
                Level = AlertLevel.Warning,
                Condition = "memory_usage > 512",
                Threshold = 512, // MB
                EvaluationPeriodMinutes = 10,
                CooldownMinutes = 20,
                IsEnabled = true
            });

            // Service health alert
            AddOrUpdateAlertRule(new AlertRule
            {
                Id = "service_unhealthy",
                Name = "Service Health Critical",
                Description = "Triggers when service health is critical",
                Level = AlertLevel.Critical,
                Condition = "health_status = critical",
                EvaluationPeriodMinutes = 2,
                CooldownMinutes = 5,
                IsEnabled = true
            });

            // Low processing rate alert
            AddOrUpdateAlertRule(new AlertRule
            {
                Id = "low_processing_rate",
                Name = "Low Email Processing Rate",
                Description = "Triggers when email processing rate is too low",
                Level = AlertLevel.Warning,
                Condition = "hourly_rate < 10",
                Threshold = 10,
                EvaluationPeriodMinutes = 30,
                CooldownMinutes = 60,
                IsEnabled = true
            });

            // SMTP connection failure
            AddOrUpdateAlertRule(new AlertRule
            {
                Id = "smtp_connection_failure",
                Name = "SMTP Connection Failure",
                Description = "Triggers when SMTP connection fails",
                Level = AlertLevel.Critical,
                Condition = "smtp_health = failed",
                EvaluationPeriodMinutes = 5,
                CooldownMinutes = 10,
                IsEnabled = true
            });

            // Database connection failure
            AddOrUpdateAlertRule(new AlertRule
            {
                Id = "database_connection_failure",
                Name = "Database Connection Failure",
                Description = "Triggers when database connection fails",
                Level = AlertLevel.Critical,
                Condition = "database_health = failed",
                EvaluationPeriodMinutes = 5,
                CooldownMinutes = 10,
                IsEnabled = true
            });
        }

        private void EvaluateAlerts(object? state)
        {
            try
            {
                // This method would be called by external services with actual metrics
                // For now, we'll just log that evaluation is running
                _logger.LogDebug("Alert evaluation cycle started - {ActiveRules} rules active",
                    _alertRules.Values.Count(r => r.IsEnabled));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during alert evaluation cycle");
            }
        }

        private bool EvaluateRule(AlertRule rule, MetricsSummary metrics, SystemHealthStatus systemHealth)
        {
            if (!rule.IsEnabled) return false;

            return rule.Id switch
            {
                "high_failure_rate" => EvaluateFailureRate(metrics, rule.Threshold),
                "queue_backlog" => EvaluateQueueBacklog(metrics, rule.Threshold),
                "critical_queue_backlog" => EvaluateQueueBacklog(metrics, rule.Threshold),
                "high_cpu_usage" => systemHealth.CpuHealth >= HealthLevel.Warning,
                "high_memory_usage" => systemHealth.MemoryHealth >= HealthLevel.Warning,
                "service_unhealthy" => systemHealth.OverallHealth == HealthLevel.Critical,
                "low_processing_rate" => EvaluateLowProcessingRate(metrics, rule.Threshold),
                "smtp_connection_failure" => false, // Would be set by SMTP health check
                "database_connection_failure" => false, // Would be set by DB health check
                _ => false
            };
        }

        private bool EvaluateFailureRate(MetricsSummary metrics, double threshold)
        {
            if (metrics.Last24Hours.EmailsProcessed == 0) return false;

            var failureRate = (double)metrics.Last24Hours.FailedEmails / metrics.Last24Hours.EmailsProcessed * 100;
            return failureRate > threshold;
        }

        private bool EvaluateQueueBacklog(MetricsSummary metrics, double threshold)
        {
            // Calculate pending emails (assuming TotalEmailsProcessed includes pending)
            var pendingEmails = metrics.TotalEmailsProcessed - metrics.SuccessfulEmails - metrics.FailedEmails;
            return pendingEmails > threshold;
        }

        private bool EvaluateLowProcessingRate(MetricsSummary metrics, double threshold)
        {
            return metrics.Last24Hours.PeakHourlyRate < threshold;
        }

        private void UpdateAlertState(string ruleId, bool shouldTrigger, AlertRule rule)
        {
            var state = _alertStates.GetOrAdd(ruleId, _ => new AlertState { RuleId = ruleId });
            var now = DateTime.UtcNow;

            state.LastEvaluated = now;

            if (shouldTrigger)
            {
                if (!state.IsActive)
                {
                    // Check cooldown period
                    if (state.LastTriggered.HasValue &&
                        now - state.LastTriggered.Value < TimeSpan.FromMinutes(rule.CooldownMinutes))
                    {
                        return; // Still in cooldown
                    }

                    // Trigger new alert
                    state.IsActive = true;
                    state.TriggeredAt = now;
                    state.LastTriggered = now;
                    state.TriggerCount++;
                    state.Level = rule.Level;
                    state.LastMessage = GenerateAlertMessage(rule);

                    // Send notification
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var alert = new Alert
                            {
                                Id = Guid.NewGuid().ToString(),
                                Title = rule.Name,
                                Message = state.LastMessage,
                                Level = rule.Level,
                                Timestamp = now,
                                Source = "AlertManager",
                                RuleId = ruleId
                            };

                            await _notificationService.SendAlertAsync(alert);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send alert notification for rule {RuleId}", ruleId);
                        }
                    });

                    _logger.LogWarning("Alert triggered: {RuleName} ({RuleId})", rule.Name, ruleId);
                }
            }
            else if (state.IsActive)
            {
                // Alert resolved
                state.IsActive = false;
                state.ResolvedAt = now;

                _logger.LogInformation("Alert resolved: {RuleName} ({RuleId})", rule.Name, ruleId);

                // Send resolution notification
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var alert = new Alert
                        {
                            Id = Guid.NewGuid().ToString(),
                            Title = $"RESOLVED: {rule.Name}",
                            Message = $"Alert has been resolved. Condition is no longer met.",
                            Level = AlertLevel.Info,
                            Timestamp = now,
                            Source = "AlertManager",
                            RuleId = ruleId
                        };

                        await _notificationService.SendAlertAsync(alert);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send alert resolution notification for rule {RuleId}", ruleId);
                    }
                });
            }
        }

        private string GenerateAlertMessage(AlertRule rule)
        {
            return rule.Id switch
            {
                "high_failure_rate" => $"Email failure rate has exceeded {rule.Threshold}% threshold",
                "queue_backlog" => $"Email queue has more than {rule.Threshold} pending emails",
                "critical_queue_backlog" => $"CRITICAL: Email queue has more than {rule.Threshold} pending emails",
                "high_cpu_usage" => $"CPU usage has exceeded {rule.Threshold}% for extended period",
                "high_memory_usage" => $"Memory usage has exceeded {rule.Threshold}MB threshold",
                "service_unhealthy" => "Email service health status is critical",
                "low_processing_rate" => $"Email processing rate has fallen below {rule.Threshold} emails per hour",
                "smtp_connection_failure" => "SMTP server connection has failed",
                "database_connection_failure" => "Database connection has failed",
                _ => $"Alert condition met for rule: {rule.Name}"
            };
        }

        #endregion

        public void Dispose()
        {
            _evaluationTimer?.Dispose();
        }
    }

    #region Data Classes

    public class AlertRule
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public AlertLevel Level { get; set; }
        public string Condition { get; set; } = string.Empty;
        public double Threshold { get; set; }
        public int EvaluationPeriodMinutes { get; set; } = 5;
        public int CooldownMinutes { get; set; } = 15;
        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AlertState
    {
        public string RuleId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public AlertLevel Level { get; set; }
        public DateTime? TriggeredAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? LastTriggered { get; set; }
        public DateTime LastEvaluated { get; set; } = DateTime.UtcNow;
        public int TriggerCount { get; set; }
        public string LastMessage { get; set; } = string.Empty;
    }

    public class ActiveAlert
    {
        public string RuleId { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public AlertLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime? TriggeredAt { get; set; }
        public DateTime LastEvaluated { get; set; }
        public int TriggerCount { get; set; }
        public TimeSpan? Duration => TriggeredAt.HasValue ? DateTime.UtcNow - TriggeredAt.Value : null;
    }

    public class Alert
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public AlertLevel Level { get; set; }
        public DateTime Timestamp { get; set; }
        public string Source { get; set; } = string.Empty;
        public string? RuleId { get; set; }
    }

    public enum AlertLevel
    {
        Info = 0,
        Warning = 1,
        Critical = 2
    }

    #endregion
}