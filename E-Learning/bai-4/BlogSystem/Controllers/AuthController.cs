using BlogSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlogSystem.Controllers;

// Model cho request
// Mục đích lấy dữ liệu từ body của fetch từ frontend gửi lên
public class LoginRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}

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
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (request.Email == null || request.Password == null)
        {
            return BadRequest(new
            {
                success = false,
                message = "Email hoặc Password rỗng"
            });
        }

        var user = _authService.Login(request.Email, request.Password);

        if (user == null)
        {
            return BadRequest(new
            {
                success = false,
                message = "Email hoặc mật khẩu sai"
            });
        }

        HttpContext.Session.SetString("USER_ID", user.Id.ToString());

        return Ok(new
        {
            success = true,
            message = "Đăng nhập thành công",
            data = new
            {
                userId = user.Id,
                email = user.Email
            }
        });
    }

    // POST: /logout
    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();

        return Ok(new
        {
            success = true,
            message = "Đã logout thành công"
        });
    }
}
