using Microsoft.AspNetCore.Routing;

namespace BlogSystem.Routers
{
    public static class HomeRouter
    {
        public static void MapHomeRoutes(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapControllerRoute(
                name: "home",
                pattern: "home/{action=Index}/{id?}",
                defaults: new { controller = "Home" }
            );
        }
    }
}