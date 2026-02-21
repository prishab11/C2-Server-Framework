using Microsoft.AspNetCore.Builder;

namespace TeamServer.Middleware
{
    // This is just a helper so we can write app.UseApiKey()
    // in Startup.cs instead of the longer version.
    public static class ApiKeyMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiKey(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiKeyMiddleware>();
        }
    }
}