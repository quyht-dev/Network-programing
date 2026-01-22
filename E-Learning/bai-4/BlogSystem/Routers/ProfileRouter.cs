using Microsoft.AspNetCore.Routing;

namespace BlogSystem.Routers
{
    public static class ProfileRouter
    {
        public static void MapProfileRoutes(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapControllerRoute(
                name: "profile",
                pattern: "profile/{action=Index}/{id?}",
                defaults: new { controller = "Profile" }
            );
        }
    }
}