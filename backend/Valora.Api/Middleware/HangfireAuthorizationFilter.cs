using System.Security.Claims;
using Hangfire.Dashboard;

namespace Valora.Api.Middleware;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return AuthorizeUser(httpContext.User);
    }

    public bool AuthorizeUser(ClaimsPrincipal user)
    {
        return user.Identity?.IsAuthenticated == true && user.IsInRole("Admin");
    }
}
