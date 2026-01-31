using System.Net.Http.Headers;
using System.Text;
using System.Security.Claims;

namespace Valora.Api.Middleware;

public class HangfireBasicAuthMiddleware
{
    private readonly RequestDelegate _next;

    public HangfireBasicAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration config)
    {
        string authHeader = context.Request.Headers["Authorization"].ToString();
        var apiKey = config["ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
             context.Response.StatusCode = 401;
             return;
        }

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var authHeaderVal = AuthenticationHeaderValue.Parse(authHeader);
                var credentialBytes = Convert.FromBase64String(authHeaderVal.Parameter ?? string.Empty);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);

                if (credentials.Length == 2)
                {
                    var username = credentials[0];
                    var password = credentials[1];

                    if (username == "admin" && password == apiKey)
                    {
                        var claims = new[] { new Claim(ClaimTypes.Name, "admin"), new Claim(ClaimTypes.AuthenticationMethod, "Basic") };
                        var identity = new ClaimsIdentity(claims, "Basic");
                        context.User = new ClaimsPrincipal(identity);

                        await _next(context);
                        return;
                    }
                }
            }
            catch
            {
                // Invalid header format
            }
        }

        context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Valora Hangfire\"";
        context.Response.StatusCode = 401;
    }
}
