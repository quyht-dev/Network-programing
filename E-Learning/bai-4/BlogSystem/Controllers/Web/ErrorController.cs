using Microsoft.AspNetCore.Mvc;

namespace BlogSystem.Controllers.Web;

public class ErrorController : Controller
{
    // GET: /error
    public IActionResult Index()
    {
        return View();
    }
}
