using Microsoft.AspNetCore.Mvc;

namespace BlogSystem.Controllers.Web;

public class AuthController : Controller
{
    // GET: /login
    public IActionResult Login()
    {
        return View();
    }
}
