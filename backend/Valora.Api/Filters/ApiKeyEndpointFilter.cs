using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Valora.Api.Filters;

public class ApiKeyEndpointFilter : IEndpointFilter
{
    private readonly IConfiguration _configuration;
    private const string ApiKeyHeaderName = "X-Api-Key";

    public ApiKeyEndpointFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            return Results.Unauthorized();
        }

        var apiKey = _configuration["ApiKey"];

        // If no API key is configured, block everything to be safe
        if (string.IsNullOrEmpty(apiKey))
        {
            return Results.Unauthorized();
        }

        if (!string.Equals(apiKey, extractedApiKey))
        {
            return Results.Unauthorized();
        }

        return await next(context);
    }
}
