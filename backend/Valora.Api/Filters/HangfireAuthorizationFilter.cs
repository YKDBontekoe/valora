using Hangfire.Dashboard;
using System.Net.Http.Headers;
using System.Text;

namespace Valora.Api.Filters;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly string _username;
    private readonly string _password;

    public HangfireAuthorizationFilter(IConfiguration configuration)
    {
        _username = configuration["HANGFIRE_USERNAME"]
            ?? throw new InvalidOperationException("HANGFIRE_USERNAME is not configured.");
        _password = configuration["HANGFIRE_PASSWORD"]
            ?? throw new InvalidOperationException("HANGFIRE_PASSWORD is not configured.");
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = GetHttpContext(context);

        // Basic Auth implementation
        var authHeader = httpContext.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic "))
        {
            SetChallenge(httpContext);
            return false;
        }

        try
        {
            var headerValue = AuthenticationHeaderValue.Parse(authHeader);
            var credentialBytes = Convert.FromBase64String(headerValue.Parameter ?? string.Empty);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
            var username = credentials[0];
            var password = credentials[1];

            if (username == _username && password == _password)
            {
                return true;
            }
        }
        catch
        {
            // Ignore parsing failures
        }

        SetChallenge(httpContext);
        return false;
    }

    protected virtual HttpContext GetHttpContext(DashboardContext context)
    {
        return context.GetHttpContext();
    }

    private void SetChallenge(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = 401;
        httpContext.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Hangfire Dashboard\"");
    }
}
