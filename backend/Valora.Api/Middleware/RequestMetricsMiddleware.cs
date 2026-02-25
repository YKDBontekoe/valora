using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Valora.Application.Common.Interfaces;

namespace Valora.Api.Middleware;

public class RequestMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRequestMetricsService _metricsService;

    public RequestMetricsMiddleware(RequestDelegate next, IRequestMetricsService metricsService)
    {
        _next = next;
        _metricsService = metricsService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            // Exclude the health check endpoint itself from metrics to avoid skewing data with very fast checks
            if (!context.Request.Path.StartsWithSegments("/health") && !context.Request.Path.StartsWithSegments("/api/health"))
            {
                 _metricsService.RecordRequestDuration(sw.ElapsedMilliseconds);
            }
        }
    }
}
