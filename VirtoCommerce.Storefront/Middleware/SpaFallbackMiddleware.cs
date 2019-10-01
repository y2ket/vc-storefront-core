using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace VirtoCommerce.Storefront.Middleware
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class SpaFallbackMiddleware
    {
        private readonly RequestDelegate _next;

        public SpaFallbackMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {

            await _next.Invoke(context);

            if (!context.Response.HasStarted && context.Response.StatusCode == (int)HttpStatusCode.NotFound)
            {
                //context.Response.Redirect("home/index");
                context.Request.Path = "/";
                await _next.Invoke(context);
            }

        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class SpaFallbackMiddlewareExtensions
    {
        public static IApplicationBuilder UseSpaFallbackMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SpaFallbackMiddleware>();
        }
    }
}
