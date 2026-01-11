using BlogSystem.Services;
using Microsoft.AspNetCore.Mvc;
using BlogSystem.Models.Responses; // Dùng Wrapper chuẩn
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BlogSystem.Controllers.Api
{
    // Model giữ nguyên
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    [ApiController]
    [Route("api/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly IConfiguration _configuration; // Cần cái này để đọc JWT_SECRET từ .env

        public AuthApiController(AuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // 1. Validate đầu vào
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(ApiResponse<string>.Fail("Email hoặc Password không được để trống"));
            }

            // 2. Gọi Service kiểm tra User trong DB
            var user = _authService.Login(request.Email, request.Password);

            if (user == null)
            {
                return Unauthorized(ApiResponse<string>.Fail("Email hoặc mật khẩu sai"));
            }

            // 3. QUAN TRỌNG: Tạo JWT Token
            var tokenString = GenerateJwtToken(user);

            // 4. Trả về Token cho Client (Postman/Frontend)
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Đăng nhập thành công",
                Data = new
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Role = user.Role,
                    AccessToken = tokenString // <-- Đây là cái cần lấy
                }
            });
        }

        // POST: api/auth/logout
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // Với JWT, Server không cần làm gì nhiều, Client tự xóa token là được
            return Ok(ApiResponse<string>.Ok(null, "Đăng xuất thành công"));
        }

        // --- HÀM TẠO TOKEN (LOGIC CỐT LÕI) ---
        private string GenerateJwtToken(BlogSystem.Models.Entities.User user)
        {
            var jwtSecret = _configuration["JWT_SECRET"];
            if (string.IsNullOrEmpty(jwtSecret)) throw new Exception("JWT_SECRET chưa cấu hình trong .env");

            var key = Encoding.ASCII.GetBytes(jwtSecret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("Id", user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role ?? "User")
                }),
                Expires = DateTime.UtcNow.AddDays(1), // Token sống 1 ngày
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}