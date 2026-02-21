using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace TeamServer.Middleware
{
    // Middleware is like a security guard that checks EVERY request
    // before it reaches any controller.
    public class ApiKeyMiddleware
    {
        // The name of the header we expect in every request
        private const string ApiKeyHeaderName = "X-Api-Key";

        // _next is the "rest of the pipeline" — if we approve the request,
        // we call _next to let it continue. If not, we stop it here.
        private readonly RequestDelegate _next;
        private readonly string _validApiKey;

        public ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
        {
            _next = next;
            // Read the valid key from appsettings.json
            _validApiKey = config["Auth:ApiKey"];
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if the request has our API key header
            if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedKey))
            {
                // Header is completely missing
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API Key is missing.");
                return;
            }

            // Check if the provided key matches our valid key
            if (!_validApiKey.Equals(providedKey))
            {
                // Key is wrong
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid API Key.");
                return;
            }

            // Key is correct — let the request through
            await _next(context);
        }
    }
}