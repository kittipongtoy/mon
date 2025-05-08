using Microsoft.AspNetCore.Mvc;

namespace Monklongpae.Controllers
{
    public class FrontController : Controller
    {
        public IActionResult Index()
        {
            return PhysicalFile(
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "React", "index.html"),
                "text/html"
            );
        }
    }
}
