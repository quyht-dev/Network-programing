using BlogSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlogSystem.Controllers.Api;

// Model cho request
public class LoginRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthApiController(AuthService authService)
    {
        _authService = authService;
    }

    // POST: api/auth/login
    [HttpPost("login")]
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

    // POST: api/auth/logout
    [HttpPost("logout")]
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
