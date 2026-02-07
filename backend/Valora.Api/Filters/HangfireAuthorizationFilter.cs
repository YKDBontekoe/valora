using Hangfire.Dashboard;
using System.Diagnostics.CodeAnalysis;

namespace Valora.Api.Filters;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize([NotNull] DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Allow all authenticated users with Admin role to see the Dashboard.
        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole("Admin");
    }
}
