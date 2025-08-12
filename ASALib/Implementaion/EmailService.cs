using System.Net;
using System.Net.Mail;
using System.Text;
using ASALib.Model;
using ASALib.Interface;

namespace ASALib.Implementaion
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        //private readonly ILogger<EmailService> _logger;

        //public EmailService(IOptions<EmailSettings> emailSettings)//, ILogger<EmailService> logger)
        //{
        //    _emailSettings = emailSettings.Value;
        //    //_logger = logger;
        //}

        public async Task<bool> SendContactEmailAsync(ContactModel contactModel)
        {
            try
            {
                using var client = CreateSmtpClient();
                using var mailMessage = CreateContactEmailMessage(contactModel);

                await client.SendMailAsync(mailMessage);
                //_logger.LogInformation("Contact email sent successfully from {Email}", contactModel.Email);
                return true;
            }
            catch (Exception ex)
            {
               // _logger.LogError(ex, "Failed to send contact email from {Email}", contactModel.Email);
                return false;
            }
        }

        public async Task<bool> SendConfirmationEmailAsync(ContactModel contactModel)
        {
            try
            {
                using var client = CreateSmtpClient();
                using var mailMessage = CreateConfirmationEmailMessage(contactModel);

                await client.SendMailAsync(mailMessage);
               // _logger.LogInformation("Confirmation email sent successfully to {Email}", contactModel.Email);
                return true;
            }
            catch (Exception ex)
            {
               // _logger.LogError(ex, "Failed to send confirmation email to {Email}", contactModel.Email);
                return false;
            }
        }

        private SmtpClient CreateSmtpClient()
        {
            return new SmtpClient(_emailSettings.SmtpServer)
            {
                Port = _emailSettings.Port,
                Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password),
                EnableSsl = _emailSettings.EnableSsl,
                Timeout = 30000 // 30 seconds
            };
        }

        private MailMessage CreateContactEmailMessage(ContactModel contactModel)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                Subject = $"New Contact Form Submission - {contactModel.Subject}",
                Body = GenerateContactEmailBody(contactModel),
                IsBodyHtml = true
            };

            // Send to admin
            mailMessage.To.Add(_emailSettings.AdminEmail);

            // Set reply-to as the customer's email
            mailMessage.ReplyToList.Add(new MailAddress(contactModel.Email, contactModel.FullName));

            return mailMessage;
        }

        private MailMessage CreateConfirmationEmailMessage(ContactModel contactModel)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                Subject = "Thank you for contacting Aniket Sales Agencies",
                Body = GenerateConfirmationEmailBody(contactModel),
                IsBodyHtml = true
            };

            mailMessage.To.Add(contactModel.Email);

            return mailMessage;
        }

        private string GenerateContactEmailBody(ContactModel contactModel)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body style='font-family: Arial, sans-serif;'>");
            sb.AppendLine("<div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>");

            // Header
            sb.AppendLine("<div style='background: linear-gradient(135deg, #1a365d, #2d5a87); color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0;'>");
            sb.AppendLine("<h2 style='margin: 0;'><i class='fas fa-industry'></i> New Contact Form Submission</h2>");
            sb.AppendLine("</div>");

            // Content
            sb.AppendLine("<div style='padding: 20px; background: #f8f9fa;'>");
            sb.AppendLine("<h3 style='color: #1a365d; border-bottom: 2px solid #ff6b35; padding-bottom: 10px;'>Contact Details</h3>");

            sb.AppendLine("<table style='width: 100%; border-collapse: collapse;'>");
            sb.AppendLine($"<tr><td style='padding: 8px; font-weight: bold; color: #1a365d;'>Name:</td><td style='padding: 8px;'>{contactModel.FullName}</td></tr>");
            sb.AppendLine($"<tr style='background: #e9ecef;'><td style='padding: 8px; font-weight: bold; color: #1a365d;'>Email:</td><td style='padding: 8px;'><a href='mailto:{contactModel.Email}'>{contactModel.Email}</a></td></tr>");
            sb.AppendLine($"<tr><td style='padding: 8px; font-weight: bold; color: #1a365d;'>Phone:</td><td style='padding: 8px;'><a href='tel:{contactModel.PhoneNumber}'>{contactModel.PhoneNumber}</a></td></tr>");

            if (!string.IsNullOrEmpty(contactModel.Company))
            {
                sb.AppendLine($"<tr style='background: #e9ecef;'><td style='padding: 8px; font-weight: bold; color: #1a365d;'>Company:</td><td style='padding: 8px;'>{contactModel.Company}</td></tr>");
            }

            if (!string.IsNullOrEmpty(contactModel.GSTNumber))
            {
                sb.AppendLine($"<tr><td style='padding: 8px; font-weight: bold; color: #1a365d;'>GST Number:</td><td style='padding: 8px;'>{contactModel.GSTNumber}</td></tr>");
            }

            sb.AppendLine($"<tr style='background: #e9ecef;'><td style='padding: 8px; font-weight: bold; color: #1a365d;'>Subject:</td><td style='padding: 8px;'>{GetSubjectDisplay(contactModel.Subject)}</td></tr>");
            sb.AppendLine("</table>");

            sb.AppendLine("<h3 style='color: #1a365d; border-bottom: 2px solid #ff6b35; padding-bottom: 10px; margin-top: 20px;'>Message</h3>");
            sb.AppendLine($"<div style='background: white; padding: 15px; border-left: 4px solid #ff6b35; border-radius: 5px;'>");
            sb.AppendLine($"<p style='margin: 0; line-height: 1.6;'>{contactModel.Message.Replace("\n", "<br>")}</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("</div>");

            // Footer
            sb.AppendLine("<div style='background: #1a365d; color: white; padding: 15px; text-align: center; border-radius: 0 0 10px 10px;'>");
            sb.AppendLine($"<p style='margin: 0; font-size: 12px;'>Received on: {DateTime.Now:dddd, MMMM dd, yyyy 'at' hh:mm tt}</p>");
            sb.AppendLine("<p style='margin: 5px 0 0 0; font-size: 12px;'>Aniket Sales Agencies - Industrial Solutions</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("</div>");
            sb.AppendLine("</body></html>");

            return sb.ToString();
        }

        private string GenerateConfirmationEmailBody(ContactModel contactModel)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body style='font-family: Arial, sans-serif;'>");
            sb.AppendLine("<div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>");

            // Header
            sb.AppendLine("<div style='background: linear-gradient(135deg, #1a365d, #2d5a87); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>");
            sb.AppendLine("<h2 style='margin: 0;'><i class='fas fa-industry'></i> Aniket Sales Agencies</h2>");
            sb.AppendLine("<p style='margin: 10px 0 0 0; opacity: 0.9;'>Industrial Tools & Equipment</p>");
            sb.AppendLine("</div>");

            // Content
            sb.AppendLine("<div style='padding: 30px; background: #f8f9fa;'>");
            sb.AppendLine($"<h3 style='color: #1a365d;'>Dear {contactModel.FullName},</h3>");
            sb.AppendLine("<p style='line-height: 1.6; color: #333;'>Thank you for contacting us! We have received your inquiry and our team will get back to you within 24 hours.</p>");

            sb.AppendLine("<div style='background: white; padding: 20px; border-left: 4px solid #ff6b35; border-radius: 5px; margin: 20px 0;'>");
            sb.AppendLine("<h4 style='margin: 0 0 10px 0; color: #1a365d;'>Your Inquiry Summary:</h4>");
            sb.AppendLine($"<p style='margin: 5px 0;'><strong>Subject:</strong> {GetSubjectDisplay(contactModel.Subject)}</p>");
            sb.AppendLine($"<p style='margin: 5px 0;'><strong>Submitted on:</strong> {DateTime.Now:dddd, MMMM dd, yyyy 'at' hh:mm tt}</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("<p style='line-height: 1.6; color: #333;'>In the meantime, feel free to explore our product catalog or contact us directly:</p>");

            sb.AppendLine("<div style='background: white; padding: 20px; border-radius: 5px; margin: 20px 0;'>");
            sb.AppendLine("<h4 style='margin: 0 0 15px 0; color: #1a365d;'>Contact Information:</h4>");
            sb.AppendLine("<p style='margin: 5px 0;'><strong>📞 Phone:</strong> +91 9164812967</p>");
            sb.AppendLine("<p style='margin: 5px 0;'><strong>📧 Email:</strong> office@aniketsalesagencies.com</p>");
            sb.AppendLine("<p style='margin: 5px 0;'><strong>📍 Address:</strong> Udyambag, Industrial Estate, Belgaum, Karnataka 590008</p>");
            sb.AppendLine("<p style='margin: 5px 0;'><strong>🕒 Business Hours:</strong> Mon - Sat: 9:00 AM - 6:00 PM</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("</div>");

            // Footer
            sb.AppendLine("<div style='background: #1a365d; color: white; padding: 20px; text-align: center; border-radius: 0 0 10px 10px;'>");
            sb.AppendLine("<p style='margin: 0 0 10px 0;'>Thank you for choosing Aniket Sales Agencies!</p>");
            sb.AppendLine("<p style='margin: 0; font-size: 12px; opacity: 0.8;'>Leading provider of industrial tools and manufacturing equipment</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("</div>");
            sb.AppendLine("</body></html>");

            return sb.ToString();
        }

        private string GetSubjectDisplay(string subject)
        {
            return subject switch
            {
                "product-inquiry" => "Product Inquiry",
                "bulk-order" => "Bulk Order",
                "technical-support" => "Technical Support",
                "partnership" => "Partnership Opportunity",
                "warranty" => "Warranty Claim",
                "other" => "Other",
                _ => subject
            };
        }
    }

    // Models/EmailSettings.cs
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
    }
}