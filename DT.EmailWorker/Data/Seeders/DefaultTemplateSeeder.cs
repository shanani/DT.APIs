using DT.EmailWorker.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DT.EmailWorker.Data.Seeders
{
    /// <summary>
    /// Seeder for default email templates
    /// </summary>
    public static class DefaultTemplateSeeder
    {
        /// <summary>
        /// Seed default email templates
        /// </summary>
        public static async Task SeedAsync(EmailDbContext context)
        {
            // Check if templates already exist
            if (await context.EmailTemplates.AnyAsync())
            {
                return; // Templates already seeded
            }

            var defaultTemplates = new List<EmailTemplate>
            {
                // Welcome Email Template
                new EmailTemplate
                {
                    Name = "WelcomeEmail",
                    Category = "Authentication",
                    SubjectTemplate = "Welcome to {{CompanyName}}!",
                    BodyTemplate = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Welcome Email</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
        <h1 style='margin: 0; font-size: 28px;'>Welcome to {{CompanyName}}!</h1>
    </div>
    
    <div style='background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px;'>
        <p style='font-size: 18px; margin-bottom: 20px;'>Hello {{UserName}},</p>
        
        <p>We're excited to have you join our community! Your account has been successfully created.</p>
        
        <div style='background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #667eea;'>
            <h3 style='margin-top: 0; color: #667eea;'>Account Details:</h3>
            <p><strong>Email:</strong> {{UserEmail}}</p>
            <p><strong>Registration Date:</strong> {{RegistrationDate}}</p>
        </div>
        
        <p>To get started, please click the button below:</p>
        
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{{ActivationLink}}' style='background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; display: inline-block; font-weight: bold;'>Activate Account</a>
        </div>
        
        <p>If you have any questions, feel free to contact our support team.</p>
        
        <p>Best regards,<br>The {{CompanyName}} Team</p>
    </div>
</body>
</html>",
                    Description = "Welcome email template for new user registration",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // Password Reset Template
                new EmailTemplate
                {
                    Name = "PasswordReset",
                    Category = "Authentication",
                    SubjectTemplate = "Reset Your Password - {{CompanyName}}",
                    BodyTemplate = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Password Reset</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: #dc3545; color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
        <h1 style='margin: 0; font-size: 28px;'>Password Reset Request</h1>
    </div>
    
    <div style='background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px;'>
        <p style='font-size: 18px; margin-bottom: 20px;'>Hello {{UserName}},</p>
        
        <p>We received a request to reset your password for your {{CompanyName}} account.</p>
        
        <div style='background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 8px; margin: 20px 0;'>
            <p style='margin: 0; color: #856404;'><strong>Security Notice:</strong> If you didn't request this password reset, please ignore this email and your password will remain unchanged.</p>
        </div>
        
        <p>To reset your password, click the button below:</p>
        
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{{ResetLink}}' style='background: #dc3545; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; display: inline-block; font-weight: bold;'>Reset Password</a>
        </div>
        
        <p style='font-size: 14px; color: #6c757d;'>This link will expire in {{ExpirationHours}} hours for security reasons.</p>
        
        <p>If you continue to have problems, please contact our support team.</p>
        
        <p>Best regards,<br>The {{CompanyName}} Team</p>
    </div>
</body>
</html>",
                    Description = "Password reset email template",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // Order Confirmation Template
                new EmailTemplate
                {
                    Name = "OrderConfirmation",
                    Category = "Commerce",
                    SubjectTemplate = "Order Confirmation #{{OrderNumber}} - {{CompanyName}}",
                    BodyTemplate = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Order Confirmation</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: #28a745; color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
        <h1 style='margin: 0; font-size: 28px;'>Order Confirmed!</h1>
    </div>
    
    <div style='background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px;'>
        <p style='font-size: 18px; margin-bottom: 20px;'>Hello {{CustomerName}},</p>
        
        <p>Thank you for your order! We've received your order and will process it shortly.</p>
        
        <div style='background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border: 1px solid #dee2e6;'>
            <h3 style='margin-top: 0; color: #28a745;'>Order Details:</h3>
            <table style='width: 100%; border-collapse: collapse;'>
                <tr>
                    <td style='padding: 8px 0; border-bottom: 1px solid #eee;'><strong>Order Number:</strong></td>
                    <td style='padding: 8px 0; border-bottom: 1px solid #eee;'>{{OrderNumber}}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; border-bottom: 1px solid #eee;'><strong>Order Date:</strong></td>
                    <td style='padding: 8px 0; border-bottom: 1px solid #eee;'>{{OrderDate}}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; border-bottom: 1px solid #eee;'><strong>Total Amount:</strong></td>
                    <td style='padding: 8px 0; border-bottom: 1px solid #eee; font-weight: bold; color: #28a745;'>{{TotalAmount}}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0;'><strong>Estimated Delivery:</strong></td>
                    <td style='padding: 8px 0;'>{{EstimatedDelivery}}</td>
                </tr>
            </table>
        </div>
        
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{{TrackingLink}}' style='background: #28a745; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; display: inline-block; font-weight: bold;'>Track Your Order</a>
        </div>
        
        <p>We'll send you another email when your order ships.</p>
        
        <p>Thank you for choosing {{CompanyName}}!</p>
        
        <p>Best regards,<br>The {{CompanyName}} Team</p>
    </div>
</body>
</html>",
                    Description = "Order confirmation email template",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // System Notification Template
                new EmailTemplate
                {
                    Name = "SystemNotification",
                    Category = "System",
                    SubjectTemplate = "{{NotificationType}} - {{CompanyName}} System Alert",
                    BodyTemplate = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>System Notification</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: #ffc107; color: #212529; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
        <h1 style='margin: 0; font-size: 28px;'>System Notification</h1>
    </div>
    
    <div style='background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px;'>
        <div style='background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #ffc107;'>
            <h3 style='margin-top: 0; color: #ffc107;'>{{NotificationType}}</h3>
            <p style='font-size: 16px; margin-bottom: 10px;'><strong>Message:</strong> {{NotificationMessage}}</p>
            <p style='font-size: 14px; color: #6c757d; margin: 0;'><strong>Time:</strong> {{NotificationTime}}</p>
        </div>
        
        {{#if Details}}
        <div style='background: #e9ecef; padding: 15px; border-radius: 6px; margin: 20px 0;'>
            <h4 style='margin-top: 0;'>Additional Details:</h4>
            <p style='margin: 0; font-family: monospace; font-size: 14px;'>{{Details}}</p>
        </div>
        {{/if}}
        
        <p>This is an automated system notification from {{CompanyName}}.</p>
        
        <p style='font-size: 14px; color: #6c757d;'>If you believe this notification was sent in error, please contact the system administrator.</p>
    </div>
</body>
</html>",
                    Description = "Generic system notification email template",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.EmailTemplates.AddRange(defaultTemplates);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Create sample template data for testing
        /// </summary>
        public static Dictionary<string, string> GetSampleTemplateData(string templateName)
        {
            return templateName.ToLower() switch
            {
                "welcomeemail" => new Dictionary<string, string>
                {
                    ["CompanyName"] = "Digital Transformation Solutions",
                    ["UserName"] = "John Doe",
                    ["UserEmail"] = "john.doe@example.com",
                    ["RegistrationDate"] = DateTime.Now.ToString("MMMM dd, yyyy"),
                    ["ActivationLink"] = "https://example.com/activate?token=abc123"
                },
                "passwordreset" => new Dictionary<string, string>
                {
                    ["CompanyName"] = "Digital Transformation Solutions",
                    ["UserName"] = "John Doe",
                    ["ResetLink"] = "https://example.com/reset?token=xyz789",
                    ["ExpirationHours"] = "24"
                },
                "orderconfirmation" => new Dictionary<string, string>
                {
                    ["CompanyName"] = "Digital Transformation Solutions",
                    ["CustomerName"] = "John Doe",
                    ["OrderNumber"] = "DT-2024-001",
                    ["OrderDate"] = DateTime.Now.ToString("MMMM dd, yyyy"),
                    ["TotalAmount"] = "$299.99",
                    ["EstimatedDelivery"] = DateTime.Now.AddDays(3).ToString("MMMM dd, yyyy"),
                    ["TrackingLink"] = "https://example.com/track/DT-2024-001"
                },
                "systemnotification" => new Dictionary<string, string>
                {
                    ["CompanyName"] = "Digital Transformation Solutions",
                    ["NotificationType"] = "Service Update",
                    ["NotificationMessage"] = "System maintenance completed successfully",
                    ["NotificationTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    ["Details"] = "All services are now running normally."
                },
                _ => new Dictionary<string, string>
                {
                    ["CompanyName"] = "Digital Transformation Solutions",
                    ["UserName"] = "Test User"
                }
            };
        }
    }
}