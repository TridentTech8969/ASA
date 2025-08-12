using Microsoft.AspNetCore.Mvc;

namespace IndustrialSolutions.Controllers
{
    public class ProductController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        
        public IActionResult Pneumatic()
        {
            ViewData["Title"] = "Pneumatic Tools";
            return View();
        }
        
        public IActionResult PowerTools()
        {
            ViewData["Title"] = "Power Tools";
            return View();
        }
        
        public IActionResult CuttingTools()
        {
            ViewData["Title"] = "Cutting Tools";
            return View();
        }
        
        public IActionResult Abrasives()
        {
            ViewData["Title"] = "Abrasives";
            return View();
        }
    }
}
