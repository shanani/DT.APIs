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

                // CID Image Test Template
                new EmailTemplate
{
    Name = "CIDImageTest",
    Category = "Testing",
    Description = "Template to test CID image conversion from Base64",
    SubjectTemplate = "Welcome {{UserName}} - Image Test Email",
    BodyTemplate = @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <title>CID Image Test</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <div style=""background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;"">
        <h1 style=""margin: 0; font-size: 28px;"">Hello {{UserName}}!</h1>
        <p style=""margin: 10px 0 0 0; font-size: 16px;"">Testing CID Image Conversion</p>
    </div>
    
    <div style=""background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px;"">
        <p style=""font-size: 18px; margin-bottom: 20px;"">Dear {{UserName}},</p>
        
        <p>This email tests the automatic conversion of Base64 images to CID attachments.</p>
        
        <!-- Test Image 1: Empty Base64 placeholder for rocket emoji -->
        <div style=""text-align: center; margin: 20px 0;"">
            <img src=""data:image/png;base64,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"" 
                 alt=""🚀"" 
                 style=""width: 50px; height: 50px; border: 2px solid #667eea; border-radius: 8px;"" />

            <p style=""font-size: 12px; color: #666; margin: 5px 0 0 0;"">Rocket Emoji 🚀 (Base64 → CID)</p>
        </div>
        
        <!-- Test Image 2: Empty Base64 placeholder for success checkmark -->
        <div style=""background: white; padding: 20px; border-radius: 8px; margin: 20px 0; text-align: center;"">
            <img src=""data:image/png;base64,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"" 
                 alt=""✅"" 
                 style=""width: 40px; height: 40px; background: #e7f3ff; padding: 10px; border-radius: 50%;"" />
            <p style=""font-size: 12px; color: #666; margin: 10px 0 0 0;"">Success Icon ✅ (Base64 → CID)</p>
        </div>
        
        <p><strong>Company:</strong> {{CompanyName}}</p>
        <p><strong>Email:</strong> {{UserEmail}}</p>
        <p><strong>Test Date:</strong> {{TestDate}}</p>
        
        <div style=""background: #e7f3ff; padding: 15px; border-radius: 6px; margin: 20px 0; border-left: 4px solid #667eea;"">
            <p style=""margin: 0; font-weight: bold; color: #667eea;"">✅ CID Conversion Test</p>
            <p style=""margin: 5px 0 0 0; font-size: 14px;"">The images above should be automatically converted from Base64 to CID attachments for better email compatibility.</p>
        </div>
        
        <p>If you can see the images properly, the CID conversion is working correctly!</p>
        
        <p>Best regards,<br>
        The {{CompanyName}} Development Team</p>
    </div>
</body>
</html>",
    IsActive = true,
    CreatedAt = DateTime.UtcNow.AddHours(3),
    UpdatedAt = DateTime.UtcNow.AddHours(3),
    CreatedBy = "SYSTEM_SEEDER",
    UpdatedBy = "SYSTEM_SEEDER"
},

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
                    CreatedAt = DateTime.UtcNow.AddHours(3),
                    UpdatedAt = DateTime.UtcNow.AddHours(3)
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
                    CreatedAt = DateTime.UtcNow.AddHours(3),
                    UpdatedAt = DateTime.UtcNow.AddHours(3)
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
                    CreatedAt = DateTime.UtcNow.AddHours(3),
                    UpdatedAt = DateTime.UtcNow.AddHours(3)
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
                    CreatedAt = DateTime.UtcNow.AddHours(3),
                    UpdatedAt = DateTime.UtcNow.AddHours(3)
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