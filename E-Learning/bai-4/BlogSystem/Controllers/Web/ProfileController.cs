using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogSystem.Controllers.Web;

[Authorize(AuthenticationSchemes = "MyCookieAuth")]
public class ProfileController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
