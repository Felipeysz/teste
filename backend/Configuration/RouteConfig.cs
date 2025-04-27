using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Configuration
{
    public static class RouteConfig
    {
        public static IServiceCollection AddCustomRouting(this IServiceCollection services)
        {
            services.AddControllers();
            return services;
        }

        public static IEndpointRouteBuilder MapCustomRoutes(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/", context =>
            {
                context.Response.Redirect("/swagger/index.html");
                return Task.CompletedTask;
            });

            endpoints.MapControllers();
            return endpoints;
        }
    }
}
