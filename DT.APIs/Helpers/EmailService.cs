
using System.Net.Mail;
using System.Net.Mime;
using DT.APIs.Models;




namespace DT.APIs.Helpers
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        private readonly IWebHostEnvironment _env;

        public EmailService(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        private string ConvertImageToBase64(string filePath)
        {
            // Read the file and convert it to Base64
            var imageBytes = File.ReadAllBytes(filePath);
            return Convert.ToBase64String(imageBytes);
        }

        public async Task SendEmailAsync(EmailModel emailModel)
        {
            if (emailModel == null)
            {
                throw new ArgumentNullException(nameof(emailModel), "Email model cannot be null.");
            }

            if (string.IsNullOrEmpty(emailModel.RecipientEmail))
            {
                throw new ArgumentException("Recipient email cannot be null or empty.", nameof(emailModel.RecipientEmail));
            }

            if (string.IsNullOrEmpty(emailModel.Subject))
            {
                throw new ArgumentException("Subject cannot be null or empty.", nameof(emailModel.Subject));
            }

            var mailSettings = _configuration.GetSection("MailSettings");

            var smtpClient = new SmtpClient(mailSettings["Server"], int.Parse(mailSettings["Port"]));



            var mailMessage = new MailMessage
            {
                From = new MailAddress(mailSettings["SenderEmail"], mailSettings["SenderName"]),
                Subject = emailModel.Subject,
                Body = emailModel.Body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(emailModel.RecipientEmail);

            // Attach images as linked resources
            var logoPath = Path.Combine(_env.WebRootPath, "site/images/mail_logo.png");
            var headerPath = Path.Combine(_env.WebRootPath, "site/images/mail_header.png");

            AlternateView htmlView = AlternateView.CreateAlternateViewFromString(emailModel.Body, null, "text/html");

            // Add the logo as a linked resource
            LinkedResource logoImage = new LinkedResource(logoPath)
            {
                ContentId = "mail_img1",
                ContentType = new ContentType("image/png")
            };
            htmlView.LinkedResources.Add(logoImage);

            // Add the header as a linked resource
            LinkedResource headerImage = new LinkedResource(headerPath)
            {
                ContentId = "mail_img2",
                ContentType = new ContentType("image/png")
            };
            htmlView.LinkedResources.Add(headerImage);

            mailMessage.AlternateViews.Add(htmlView);

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error sending email: {ex.Message}");
                //Console.WriteLine(ex.StackTrace);
            }
        }


    }
}
