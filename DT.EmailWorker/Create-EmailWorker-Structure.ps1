# PowerShell script to create the CORRECT DT.EmailWorker structure from the design plan
Write-Host "Creating DT.EmailWorker with CORRECT structure from design plan..." -ForegroundColor Green

# Create the CORRECT folder structure as per the plan
$folders = @(
    "Workers",
    "Data",
    "Data\Configurations",
    "Data\Migrations", 
    "Data\Seeders",
    "Models",
    "Models\Entities",
    "Models\DTOs",
    "Models\Enums",
    "Services",
    "Services\Interfaces",
    "Services\Implementations",
    "Core",
    "Core\Configuration",
    "Core\Engines",
    "Core\Utilities",
    "Core\Extensions",
    "Repositories",
    "Repositories\Interfaces",
    "Repositories\Implementations",
    "Monitoring",
    "Monitoring\HealthChecks",
    "Monitoring\Metrics",
    "Monitoring\Alerts"
)

foreach ($folder in $folders) {
    if (!(Test-Path $folder)) {
        New-Item -ItemType Directory -Path $folder -Force | Out-Null
        Write-Host "Created folder: $folder" -ForegroundColor Yellow
    }
}

# Create files according to the CORRECT design plan
$filesToCreate = @{
    # Workers (Main background services)
    "Workers\EmailProcessingWorker.cs" = "// Main background service"
    "Workers\ScheduledEmailWorker.cs" = "// Handles scheduled emails"
    "Workers\HealthCheckWorker.cs" = "// Service health monitoring"
    "Workers\CleanupWorker.cs" = "// Auto archive/cleanup"
    "Workers\StatusReportWorker.cs" = "// Status reporting"
    
    # Data Configurations
    "Data\Configurations\EmailQueueConfiguration.cs" = ""
    "Data\Configurations\EmailTemplateConfiguration.cs" = ""
    "Data\Configurations\EmailHistoryConfiguration.cs" = ""
    "Data\Configurations\ProcessingLogConfiguration.cs" = ""
    "Data\Configurations\ServiceStatusConfiguration.cs" = ""
    "Data\Seeders\DefaultTemplateSeeder.cs" = ""
    
    # Models/Entities (Database entities)
    "Models\Entities\EmailQueue.cs" = "// Queue items"
    "Models\Entities\EmailTemplate.cs" = "// Serialized templates"
    "Models\Entities\EmailHistory.cs" = "// Send history"
    "Models\Entities\EmailAttachment.cs" = "// Attachment metadata"
    "Models\Entities\ProcessingLog.cs" = "// Service logs"
    "Models\Entities\ServiceStatus.cs" = "// Health status"
    "Models\Entities\ScheduledEmail.cs" = "// Scheduled items"
    
    # Models/DTOs (Data transfer objects)
    "Models\DTOs\EmailProcessingRequest.cs" = ""
    "Models\DTOs\TemplateData.cs" = ""
    "Models\DTOs\ServiceStatusDto.cs" = ""
    
    # Models/Enums
    "Models\Enums\EmailQueueStatus.cs" = ""
    "Models\Enums\EmailPriority.cs" = ""
    "Models\Enums\ProcessingStatus.cs" = ""
    "Models\Enums\ServiceHealthStatus.cs" = ""
    
    # Service Interfaces
    "Services\Interfaces\IEmailQueueService.cs" = ""
    "Services\Interfaces\IEmailProcessingService.cs" = ""
    "Services\Interfaces\ITemplateService.cs" = ""
    "Services\Interfaces\ISmtpService.cs" = ""
    "Services\Interfaces\ISchedulingService.cs" = ""
    "Services\Interfaces\IHealthService.cs" = ""
    "Services\Interfaces\ICleanupService.cs" = ""
    
    # Service Implementations
    "Services\Implementations\EmailQueueService.cs" = "// Queue management"
    "Services\Implementations\EmailProcessingService.cs" = "// Core processing logic"
    "Services\Implementations\TemplateService.cs" = "// Template processing"
    "Services\Implementations\SmtpService.cs" = "// SMTP handling"
    "Services\Implementations\SchedulingService.cs" = "// Scheduled email logic"
    "Services\Implementations\HealthService.cs" = "// Health monitoring"
    "Services\Implementations\CleanupService.cs" = "// Cleanup operations"
    
    # Core Configuration
    "Core\Configuration\EmailWorkerSettings.cs" = "// Configuration model"
    "Core\Configuration\SmtpSettings.cs" = ""
    "Core\Configuration\ProcessingSettings.cs" = ""
    "Core\Configuration\CleanupSettings.cs" = ""
    
    # Core Engines
    "Core\Engines\TemplateEngine.cs" = "// Template processing"
    "Core\Engines\CidImageProcessor.cs" = "// Image embedding"
    "Core\Engines\ParallelProcessingEngine.cs" = "// Parallel worker management"
    
    # Core Utilities
    "Core\Utilities\EmailValidator.cs" = ""
    "Core\Utilities\AttachmentProcessor.cs" = ""
    "Core\Utilities\LoggingHelper.cs" = ""
    
    # Core Extensions
    "Core\Extensions\ServiceCollectionExtensions.cs" = ""
    "Core\Extensions\StringExtensions.cs" = ""
    
    # Repository Interfaces
    "Repositories\Interfaces\IEmailQueueRepository.cs" = ""
    "Repositories\Interfaces\IEmailHistoryRepository.cs" = ""
    "Repositories\Interfaces\ITemplateRepository.cs" = ""
    "Repositories\Interfaces\IProcessingLogRepository.cs" = ""
    
    # Repository Implementations
    "Repositories\Implementations\EmailQueueRepository.cs" = ""
    "Repositories\Implementations\EmailHistoryRepository.cs" = ""
    "Repositories\Implementations\TemplateRepository.cs" = ""
    "Repositories\Implementations\ProcessingLogRepository.cs" = ""
    
    # Monitoring/HealthChecks
    "Monitoring\HealthChecks\DatabaseHealthCheck.cs" = ""
    "Monitoring\HealthChecks\SmtpHealthCheck.cs" = ""
    "Monitoring\HealthChecks\QueueHealthCheck.cs" = ""
    
    # Monitoring/Metrics
    "Monitoring\Metrics\ProcessingMetrics.cs" = ""
    "Monitoring\Metrics\PerformanceCounters.cs" = ""
    
    # Monitoring/Alerts
    "Monitoring\Alerts\AlertManager.cs" = ""
    "Monitoring\Alerts\NotificationService.cs" = ""
}

Write-Host ""
Write-Host "Creating files according to design plan..." -ForegroundColor Cyan

foreach ($file in $filesToCreate.Keys) {
    if (!(Test-Path $file)) {
        New-Item -ItemType File -Path $file -Force | Out-Null
        Add-Content -Path $file -Value "// TODO: Implement $($file.Split('\')[-1]) $($filesToCreate[$file])"
        Write-Host "  Created: $file" -ForegroundColor Cyan
    }
}

Write-Host ""
Write-Host "CRITICAL: EmailProcessingService.cs is where you copy EmailService from DT.APIs!" -ForegroundColor Red
Write-Host ""
Write-Host "CORRECT structure created as per design plan:" -ForegroundColor Green
Write-Host "  Workers/ - Background services" -ForegroundColor White
Write-Host "  Models/Entities/ - Database entities" -ForegroundColor White
Write-Host "  Models/DTOs/ - Data transfer objects" -ForegroundColor White
Write-Host "  Models/Enums/ - Enumerations" -ForegroundColor White
Write-Host "  Services/ - Business logic services" -ForegroundColor White
Write-Host "  Core/Engines/ - Template and processing engines" -ForegroundColor White
Write-Host "  Repositories/ - Data access layer" -ForegroundColor White
Write-Host "  Monitoring/ - Health checks and metrics" -ForegroundColor White
Write-Host ""
Write-Host "Structure matches the original design plan!" -ForegroundColor Green