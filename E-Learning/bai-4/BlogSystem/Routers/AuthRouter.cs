using Microsoft.AspNetCore.Routing;

namespace BlogSystem.Routers
{
    public static class AuthRouter
    {
        public static void MapAuthRoutes(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapControllerRoute(
                name: "auth",
                pattern: "auth/{action=Login}/{id?}",
                defaults: new { controller = "Auth" }
            );
        }
    }
}
