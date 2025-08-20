using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Text;
using IndustrialSolutions.Models;
using ASA.Models;
using Microsoft.EntityFrameworkCore;
using IndustrialSolutions.Models.Entities;
using EmailEntity = IndustrialSolutions.Models.Entities.Email;

namespace IndustrialSolutions.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IndustrialSolutionsEmailsContext _context;

        // ===== Hardcoded SMTP + Admin (replace with yours) =====
        private const string SmtpHost = "smtp.gmail.com";
        private const int SmtpPort = 587;              // STARTTLS
        private const bool UseSsl = true;
        private const string SmtpUsername = "eternalvision2025@gmail.com"; // your Gmail address
        private const string SmtpAppPassword = "gvvy enkz fjjo iccp"; // 16-char App Password
        private const string AdminEmail = "eternalvision2025@gmail.com";     // where you receive messages
        private const string FromDisplayName = "Contact from Website";      // display name shown to admin

        public HomeController(ILogger<HomeController> logger, IndustrialSolutionsEmailsContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index() { ViewData["Title"] = "Home"; return View(); }
        public IActionResult About() { ViewData["Title"] = "About Us"; return View(); }
        public IActionResult Products() { ViewData["Title"] = "Products"; return View(); }
        public IActionResult Offers() { ViewData["Title"] = "Special Offers"; return View(); }

        public IActionResult Contact()
        {
            ViewData["Title"] = "Contact Us";
            return View(new ContactModel());
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMail([Bind("FullName,Email,PhoneNumber,Company,GSTNumber,Subject,Message")] ContactModel model)
        {
            if (!ModelState.IsValid)
            {
                var firstError = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return Json(new { success = false, message = $"Validation failed: {firstError}" });
            }

            try
            {
                // 1. Save to database first
                var contactFormEntry = await SaveContactFormToDatabase(model);

                // 2. Send email notification
                await SendEmailNotification(model, contactFormEntry.UniqueEmailId);

                return Json(new
                {
                    success = true,
                    message = "Thanks! Your message has been sent. We'll get back to you shortly.",
                    referenceId = contactFormEntry.UniqueEmailId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendMail failed for {Email}", model.Email);
                return Json(new
                {
                    success = false,
                    message = "Sorry, we couldn't send your message right now. Please try again later."
                });
            }
        }

        private async Task<EmailEntity> SaveContactFormToDatabase(ContactModel model)
        {
            var now = DateTime.UtcNow;
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(now, TimeZoneInfo.Local);

            var contactFormEntry = new EmailEntity
            {
                UniqueEmailId = GenerateUniqueEmailId(),
                GmailUid = 0, // Contact form submissions have GmailUid = 0
                Folder = "ContactForm", // Distinguish from Gmail emails
                FromName = model.FullName,
                FromEmail = model.Email,
                Subject = model.Subject,
                Snippet = model.Message.Length > 100 ? model.Message.Substring(0, 100) + "..." : model.Message,
                ReceivedUtc = now,
                ReceivedLocal = localTime.ToString("yyyy-MM-dd HH:mm:ss"),
                Unread = true,
                HasAttachments = false,
                HtmlBody = null, // Contact forms don't have HTML body
                TextBody = model.Message,
                LabelsJson = "[\"Contact Form\"]", // JSON array of labels
                Company = model.Company,
                Phone = model.PhoneNumber,
                GstNumber = model.GSTNumber,
                Message = model.Message,
                IsContactForm = true, // Mark as contact form submission
                CreatedAt = now,
                UpdatedAt = now
            };

            try
            {
                _context.Emails.Add(contactFormEntry);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Contact form submission saved to database with ID: {UniqueId} from {Email}",
                    contactFormEntry.UniqueEmailId, model.Email);

                return contactFormEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save contact form to database for {Email}", model.Email);
                throw;
            }
        }

        private async Task SendEmailNotification(ContactModel model, string referenceId)
        {
            // Build HTML body safely
            string E(string s) => WebUtility.HtmlEncode(s ?? string.Empty);
            var bodyHtml = new StringBuilder()
                .AppendLine("<div style='font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#222'>")
                .AppendLine("<h2>New Contact Form Submission</h2>")
                .AppendLine($"<p><strong>Reference ID:</strong> {E(referenceId)}</p>")
                .AppendLine("<table style='border-collapse:collapse'>")
                .AppendLine($"<tr><td style='padding-right:10px'><b>Name</b></td><td>{E(model.FullName)}</td></tr>")
                .AppendLine($"<tr><td><b>Email</b></td><td>{E(model.Email)}</td></tr>")
                .AppendLine($"<tr><td><b>Phone</b></td><td>{E(model.PhoneNumber)}</td></tr>")
                .AppendLine($"<tr><td><b>Company</b></td><td>{E(model.Company ?? "-")}</td></tr>")
                .AppendLine($"<tr><td><b>GST</b></td><td>{E(model.GSTNumber ?? "-")}</td></tr>")
                .AppendLine($"<tr><td><b>Subject</b></td><td>{E(model.Subject)}</td></tr>")
                .AppendLine("</table>")
                .AppendLine("<hr/>")
                .AppendLine("<div><b>Message:</b></div>")
                .AppendLine($"<div style='white-space:pre-wrap'>{E(model.Message)}</div>")
                .AppendLine("<hr/>")
                .AppendLine($"<p><small>Submitted at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</small></p>")
                .AppendLine("</div>")
                .ToString();

            using var smtp = new SmtpClient
            {
                Host = SmtpHost,
                Port = SmtpPort,
                EnableSsl = UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(SmtpUsername, SmtpAppPassword)
            };

            using var msg = new MailMessage
            {
                From = new MailAddress(SmtpUsername, $"Website Contact: {model.FullName}"),
                Sender = new MailAddress(SmtpUsername, FromDisplayName),
                Subject = $"[REF: {referenceId}] Contact Form: {model.FullName} <{model.Email}>",
                Body = bodyHtml,
                IsBodyHtml = true
            };

            msg.To.Add(new MailAddress(AdminEmail));

            // Replies will go to the visitor
            if (!string.IsNullOrWhiteSpace(model.Email))
                msg.ReplyToList.Add(new MailAddress(model.Email, model.FullName));

            await smtp.SendMailAsync(msg);

            _logger.LogInformation("Email notification sent for contact form submission {ReferenceId}", referenceId);
        }

        private static string GenerateUniqueEmailId()
        {
            // Generate a unique ID for contact form submissions
            // Format: CF-YYYYMMDD-HHMMSS-XXXX (CF = Contact Form)
            var now = DateTime.Now;
            var random = new Random().Next(1000, 9999);
            return $"CF-{now:yyyyMMdd}-{now:HHmmss}-{random}";
        }

        // Optional: Add an endpoint to mark contact form entries as read
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(string uniqueEmailId)
        {
            try
            {
                var entry = await _context.Emails
                    .FirstOrDefaultAsync(e => e.UniqueEmailId == uniqueEmailId);

                if (entry != null)
                {
                    entry.Unread = false;
                    entry.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "Entry not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark entry as read: {UniqueEmailId}", uniqueEmailId);
                return Json(new { success = false, message = "Failed to update" });
            }
        }

        // Optional: Get contact form statistics for dashboard
        [HttpGet]
        public async Task<IActionResult> GetContactStats()
        {
            try
            {
                var today = DateTime.Today;
                var thisWeek = today.AddDays(-(int)today.DayOfWeek);
                var thisMonth = new DateTime(today.Year, today.Month, 1);

                var stats = new
                {
                    todayCount = await _context.Emails
                        .CountAsync(e => e.ReceivedUtc >= today && e.IsContactForm),

                    weekCount = await _context.Emails
                        .CountAsync(e => e.ReceivedUtc >= thisWeek && e.IsContactForm),

                    monthCount = await _context.Emails
                        .CountAsync(e => e.ReceivedUtc >= thisMonth && e.IsContactForm),

                    unreadCount = await _context.Emails
                        .CountAsync(e => e.Unread && e.IsContactForm)
                };

                return Json(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get contact form statistics");
                return Json(new { error = "Failed to load statistics" });
            }
        }
    }
}