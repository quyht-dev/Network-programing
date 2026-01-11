using BlogSystem.Data;
using BlogSystem.Services;
using BlogSystem.Routers;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

// 1. Load biến môi trường
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// --- PHẦN 1: ĐỌC & VALIDATE BIẾN MÔI TRƯỜNG ---
var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
var dbName = Environment.GetEnvironmentVariable("DB_NAME");
var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET"); // Cần thêm cái này trong .env

if (string.IsNullOrEmpty(dbHost) || string.IsNullOrEmpty(dbName) || string.IsNullOrEmpty(dbUser) || string.IsNullOrEmpty(jwtSecret))
{
    throw new Exception("Missing essential environment variables (DB or JWT_SECRET) in .env file.");
}

var connectionString = $"Server={dbHost};Port={dbPort};Database={dbName};User={dbUser};Password={dbPassword};";

// --- PHẦN 2: CONFIG SERVICES (DEPENDENCY INJECTION) ---

// 2.1. Database (MySQL)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

// 2.2. Authentication (JWT) - QUAN TRỌNG CHO API PRIVATE
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

// 2.3. Swagger (Để test API & Nộp bài)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BlogSystem API", Version = "v1" });

    // Cấu hình nút "Authorize" (ổ khóa) trong Swagger để nhập Token
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập token theo định dạng: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

// 2.4. CORS (Cho phép Frontend gọi vào)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// 2.5. Đăng ký Services & Session
builder.Services.AddControllersWithViews(); // Dùng cái này vì bạn có cả Views MVC và API
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

var app = builder.Build();

// --- PHẦN 3: HTTP REQUEST PIPELINE (MIDDLEWARE) ---

// 3.1. Swagger UI (Chỉ hiện khi dev hoặc tùy cấu hình)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles(); // Load ảnh từ wwwroot

app.UseRouting();

app.UseCors("AllowAll"); // Mở chặn CORS

app.UseSession(); // Session (nếu dùng MVC login cũ)

// 3.2. Authentication & Authorization (Thứ tự cực kỳ quan trọng: Auth trước -> Author sau)
app.UseAuthentication(); // Giải mã Token: "Anh là ai?"
app.UseAuthorization();  // Kiểm tra quyền: "Anh được làm gì?"

app.MapControllers(); // Map các Controller API Attribute [Route]

// 3.3. Map Custom Router (MVC cũ của bạn)
RouteRegistry.MapAllRoutes(app);

app.Run();