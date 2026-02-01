using System.Diagnostics.CodeAnalysis;
using Hangfire.Dashboard;

namespace Valora.Api.Middleware;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly string _username;
    private readonly string _password;

    public HangfireAuthorizationFilter(string username, string password)
    {
        _username = username;
        _password = password;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = GetHttpContext(context);
        var header = httpContext.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            SetChallenge(httpContext);
            return false;
        }

        try
        {
            var authHeader = System.Net.Http.Headers.AuthenticationHeaderValue.Parse(header);
            var credentialBytes = Convert.FromBase64String(authHeader.Parameter!);
            var credentials = System.Text.Encoding.UTF8.GetString(credentialBytes).Split(':', 2);

            if (credentials.Length != 2)
            {
                SetChallenge(httpContext);
                return false;
            }

            var usernameValid = FixedTimeEquals(credentials[0], _username);
            var passwordValid = FixedTimeEquals(credentials[1], _password);

            if (!usernameValid || !passwordValid)
            {
                SetChallenge(httpContext);
                return false;
            }

            return true;
        }
        catch
        {
            SetChallenge(httpContext);
            return false;
        }
    }

    private bool FixedTimeEquals(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int length = left.Length;
        int accum = 0;

        for (int i = 0; i < length; i++)
        {
            accum |= left[i] - right[i];
        }

        return accum == 0;
    }

    [ExcludeFromCodeCoverage]
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
