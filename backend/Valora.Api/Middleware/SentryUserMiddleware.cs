using System.Security.Claims;
using Sentry;

namespace Valora.Api.Middleware;

public class SentryUserMiddleware
{
    private readonly RequestDelegate _next;

    public SentryUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                var user = new SentryUser
                {
                    Id = userId
                };

                SentrySdk.ConfigureScope(scope => scope.User = user);
            }
        }

        await _next(context);
    }
}
