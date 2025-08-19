using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Text;
using IndustrialSolutions.Models;
using ASA.Models;

namespace IndustrialSolutions.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        // ===== Hardcoded SMTP + Admin (replace with yours) =====
        private const string SmtpHost = "smtp.gmail.com";
        private const int SmtpPort = 587;              // STARTTLS
        private const bool UseSsl = true;
        private const string SmtpUsername = "eternalvision2025@gmail.com"; // your Gmail address
        private const string SmtpAppPassword = "gvvy enkz fjjo iccp"; // 16-char App Password
        private const string AdminEmail = "eternalvision2025@gmail.com";     // where you receive messages
        private const string FromDisplayName = "Contact from Website";      // display name shown to admin

        public HomeController(ILogger<HomeController> logger) => _logger = logger;

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
        public IActionResult SendMail([Bind("FullName,Email,PhoneNumber,Company,GSTNumber,Subject,Message")] ContactModel model)
        {
            model.Subject = "Contact Form Submission";
            if (!ModelState.IsValid)
            {
                var firstError = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return Json(new { success = false, message = $"Validation failed: {firstError}" });
            }

            try
            {
                // Build HTML body safely
                string E(string s) => WebUtility.HtmlEncode(s ?? string.Empty);
                var bodyHtml = new StringBuilder()
                    .AppendLine("<div style='font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#222'>")
                    .AppendLine("<h2>New Contact Submission</h2>")
                    .AppendLine("<table style='border-collapse:collapse'>")
                    .AppendLine($"<tr><td style='padding-right:10px'><b>Name</b></td><td>{E(model.FullName)}</td></tr>")
                    .AppendLine($"<tr><td><b>Email</b></td><td>{E(model.Email)}</td></tr>")
                    .AppendLine($"<tr><td><b>Phone</b></td><td>{E(model.PhoneNumber ?? "-")}</td></tr>")
                    .AppendLine($"<tr><td><b>Company</b></td><td>{E(model.Company ?? "-")}</td></tr>")
                    .AppendLine($"<tr><td><b>GST</b></td><td>{E(model.GSTNumber ?? "-")}</td></tr>")
                    .AppendLine($"<tr><td><b>Subject</b></td><td>{E(model.Subject)}</td></tr>")
                    .AppendLine("</table>")
                    .AppendLine("<hr/>")
                    .AppendLine("<div><b>Message:</b></div>")
                    .AppendLine($"<div style='white-space:pre-wrap'>{E(model.Message)}</div>")
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

                // FromDisplayName = model.FullName;
                // IMPORTANT: Gmail requires From/Sender to match the authenticated account (or a verified alias)
                using var msg = new MailMessage
                {
                    From = new MailAddress(SmtpUsername, $"Website Contact: {model.FullName}"),
                    Sender = new MailAddress(SmtpUsername, FromDisplayName),
                    Subject = $"Contact Form: {model.FullName} <{model.Email}>",
                    Body = bodyHtml,
                    IsBodyHtml = true
                };

                msg.To.Add(new MailAddress(AdminEmail));

                // Replies will go to the visitor
                if (!string.IsNullOrWhiteSpace(model.Email))
                    msg.ReplyToList.Add(new MailAddress(model.Email, model.FullName));

                smtp.Send(msg);

                return Json(new
                {
                    success = true,
                    message = "Thanks! Your message has been sent. We’ll get back to you shortly."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendMail failed for {Email}", model.Email);
                return Json(new
                {
                    success = false,
                    message = "Sorry, we couldn’t send your message right now. Please try again later."
                });
            }
        }
    }
}
