using Microsoft.AspNetCore.Routing;

///
/// Hàm này được sử dụng để tổng hợp đăng ký các Routers
/// 

namespace BlogSystem.Routers
{
    public static class RouteRegistry
    {
        public static void MapAllRoutes(IEndpointRouteBuilder endpoints)
        {
            // Gọi router
            HomeRouter.MapHomeRoutes(endpoints);

            AuthRouter.MapAuthRoutes(endpoints);

            ErrorRouter.MapErrorRoutes(endpoints);

            // Route mặc định
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        }
    }
}
