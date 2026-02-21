using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TeamServer.Middleware
{
    // This attribute can be added to any controller or action
    // as a second layer of defence — belt and suspenders.
    public class RequiresApiKeyAttribute : ActionFilterAttribute
    {
        private const string ApiKeyHeaderName = "X-Api-Key";

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var config = context.HttpContext
                .RequestServices
                .GetRequiredService<IConfiguration>();

            var validKey = config["Auth:ApiKey"];

            if (!context.HttpContext.Request.Headers
                    .TryGetValue(ApiKeyHeaderName, out var providedKey)
                || !validKey.Equals(providedKey))
            {
                context.Result = new UnauthorizedObjectResult("Invalid or missing API Key.");
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}