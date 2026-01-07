using Microsoft.AspNetCore.Mvc;

namespace BlogSystem.Controllers.Web;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}