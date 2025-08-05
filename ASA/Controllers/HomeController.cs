using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using IndustrialSolutions.Models;
using ASA.Models;

namespace IndustrialSolutions.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
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

        [HttpPost]
        public IActionResult Contact(ContactModel model)
        {
            if (ModelState.IsValid)
            {
                // Process the contact form here
                // You can add email sending logic, database storage, etc.

                TempData["Message"] = "Thank you for your message! We will get back to you soon.";
                return RedirectToAction("Contact");
            }

            ViewData["Title"] = "Contact Us";
            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}