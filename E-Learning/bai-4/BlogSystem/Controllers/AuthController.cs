using BlogSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlogSystem.Controllers;

public class AuthController : Controller
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    // GET: /login
    public IActionResult Login()
    {
        return View();
    }

    // POST: /login
    [HttpPost]
    public IActionResult Login(string email, string password)
    {
        var user = _authService.Login(email, password);

        if (user == null)
        {
            ViewData["Error"] = "Email hoặc mật khẩu sai";
            return View();
        }

        // Lưu session
        HttpContext.Session.SetString("USER_ID", user.Id.ToString());

        ViewData["Success"] = "Đăng nhập thành công. Đang chuyển hướng vui lòng đợi...";

        return View();
    }

    // POST: /logout
    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
