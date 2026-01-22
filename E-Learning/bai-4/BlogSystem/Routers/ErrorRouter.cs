using Microsoft.AspNetCore.Routing;

namespace BlogSystem.Routers
{
    public static class ErrorRouter
    {
        public static void MapErrorRoutes(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapControllerRoute(
                name: "error",
                pattern: "error/{action=Index}/{id?}",
                defaults: new { controller = "Error" }
            );
        }
    }
}