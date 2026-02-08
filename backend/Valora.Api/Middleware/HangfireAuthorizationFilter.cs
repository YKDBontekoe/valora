using Hangfire;
using Hangfire.Dashboard;

namespace Valora.Api.Middleware;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly ILogger<HangfireAuthorizationFilter> _logger;

    public HangfireAuthorizationFilter(ILogger<HangfireAuthorizationFilter> logger)
    {
        _logger = logger;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = GetHttpContext(context);
        var user = httpContext.User;
        var isAuthenticated = user?.Identity?.IsAuthenticated == true;
        var isAdmin = user?.IsInRole("Admin") == true;
        var userName = user?.Identity?.Name ?? "Anonymous";
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        if (isAuthenticated && isAdmin)
        {
            _logger.LogInformation("Hangfire Dashboard access GRANTED for user {User} from {IP}", userName, ipAddress);
            return true;
        }

        _logger.LogWarning("Hangfire Dashboard access DENIED for user {User} from {IP}. Authenticated: {IsAuthenticated}, IsAdmin: {IsAdmin}",
            userName, ipAddress, isAuthenticated, isAdmin);

        return false;
    }

    protected virtual HttpContext GetHttpContext(DashboardContext context)
    {
        return context.GetHttpContext();
    }
}
