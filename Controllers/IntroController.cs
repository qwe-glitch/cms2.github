using Microsoft.AspNetCore.Mvc;

namespace ComplaintManagementSystem.Controllers
{
    // The [Controller] name is "Intro"
    public class IntroController : Controller
    {
        // The [Action] name is "Index"
        public IActionResult Index()
        {
            // This looks for the view file: Views/Intro/Index.cshtml
            return View();
        }
    }
}