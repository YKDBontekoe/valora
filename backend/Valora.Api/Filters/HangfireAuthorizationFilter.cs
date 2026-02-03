using Hangfire.Dashboard;
using Microsoft.Extensions.Primitives;
using System.Net.Http.Headers;
using System.Text;

namespace Valora.Api.Filters;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly string _username;
    private readonly string _password;

    public HangfireAuthorizationFilter(IConfiguration configuration)
    {
        _username = configuration["HANGFIRE_USERNAME"] ?? throw new InvalidOperationException("Hangfire username is not configured.");
        _password = configuration["HANGFIRE_PASSWORD"] ?? throw new InvalidOperationException("Hangfire password is not configured.");
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Allow if user is already authenticated via main app auth (optional, but Basic Auth is safer for admin tools)
        // For this task, we enforce Basic Auth as per memory.

        string? authHeader = httpContext.Request.Headers["Authorization"];
        if (string.IsNullOrEmpty(authHeader))
        {
            SetChallenge(httpContext);
            return false;
        }

        AuthenticationHeaderValue authValues;
        try
        {
            authValues = AuthenticationHeaderValue.Parse(authHeader);
        }
        catch (FormatException)
        {
            SetChallenge(httpContext);
            return false;
        }

        if (!"Basic".Equals(authValues.Scheme, StringComparison.InvariantCultureIgnoreCase) ||
            string.IsNullOrWhiteSpace(authValues.Parameter))
        {
            SetChallenge(httpContext);
            return false;
        }

        try
        {
            var parameter = Encoding.UTF8.GetString(Convert.FromBase64String(authValues.Parameter));
            var parts = parameter.Split(':');

            if (parts.Length < 2)
            {
                SetChallenge(httpContext);
                return false;
            }

            var username = parts[0];
            var password = parameter.Substring(username.Length + 1);

            if (username == _username && password == _password)
            {
                return true;
            }
        }
        catch
        {
            // Ignore
        }

        SetChallenge(httpContext);
        return false;
    }

    private void SetChallenge(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = 401;
        httpContext.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Hangfire Dashboard\"");
    }
}
