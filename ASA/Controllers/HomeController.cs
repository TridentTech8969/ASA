using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Mail;
using System.Net;
using ASA.Models;
using Microsoft.Extensions.Options;

namespace IndustrialSolutions.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SmtpOptions _smtpOptions;

        public HomeController(ILogger<HomeController> logger, IOptions<SmtpOptions> smtpOptions)
        {
            _logger = logger;
            _smtpOptions = smtpOptions.Value;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Home";
            return View();
        }

        public IActionResult About()
        {
            ViewData["Title"] = "About Us";
            return View();
        }

        public IActionResult Products()
        {
            ViewData["Title"] = "Products";
            return View();
        }

        public IActionResult Offers()
        {
            ViewData["Title"] = "Special Offers";
            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Title"] = "Contact Us";
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendMail(string email, string message)
        {
            try
            {
                var fromAddress = new MailAddress(_smtpOptions.FromEmail, _smtpOptions.FromName);
                var toAddress = new MailAddress(email);

                using (var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_smtpOptions.FromEmail, _smtpOptions.AppPassword)
                })
                using (var msg = new MailMessage(fromAddress, toAddress)
                {
                    Subject = "Your OTP Code",
                    Body = message
                })
                {
                    smtp.Send(msg);
                }

                return Json(new { success = true, message = "Email sent successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", email);
                return Json(new { success = false, message = "An error occurred while sending your message." });
            }
        }
    }

    // SMTP config class for binding
    public class SmtpOptions
    {
        public string FromEmail { get; set; }
        public string FromName { get; set; }
        public string AppPassword { get; set; }
    }
}
