using BlogSystem.Data;
using BlogSystem.Services;
using BlogSystem.Routers;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Đọc biến môi trường từ .env
var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
var dbName = Environment.GetEnvironmentVariable("DB_NAME");
var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

// Validate nhanh
if (string.IsNullOrEmpty(dbHost) ||
    string.IsNullOrEmpty(dbName) ||
    string.IsNullOrEmpty(dbUser))
{
    throw new Exception("Database environment variables not loaded from .env");
}

// Connection string
var connectionString =
    $"Server={dbHost};Port={dbPort};Database={dbName};User={dbUser};Password={dbPassword};";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    );
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<BlogService>();
builder.Services.AddSession();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllers();

// Gọi Index gom tất cả router
RouteRegistry.MapAllRoutes(app);

app.Run();
