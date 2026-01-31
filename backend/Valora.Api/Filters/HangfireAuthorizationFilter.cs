using Hangfire.Dashboard;

namespace Valora.Api.Filters;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = GetHttpContext(context);
        return httpContext?.User.Identity?.IsAuthenticated == true;
    }

    protected virtual HttpContext? GetHttpContext(DashboardContext context)
    {
        return context.GetHttpContext();
    }
}
