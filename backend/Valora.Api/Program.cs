using Valora.Api.Background;
using Sentry;
using Valora.Api.Endpoints;
using Valora.Api.Extensions;
using Valora.Application;
using Valora.Application.Common.Interfaces;
using Valora.Infrastructure;
using Valora.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, Valora.Api.Services.CurrentUserService>();

// Configure Sentry
// Design Decision: We use Sentry for error tracking but limit the volume of data sent.
// - Information logs are kept as 'breadcrumbs' to provide context leading up to an error.
// - Only Error logs trigger actual Sentry events to reduce noise and quota usage.
// - Request bodies are explicitly excluded to prevent PII leakage (GDPR compliance).
var sentryDsn = builder.Configuration["SENTRY_DSN"];
if (!string.IsNullOrEmpty(sentryDsn))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;

        // TracesSampleRate is read from configuration, defaulting to 1.0 if not specified.
        options.TracesSampleRate = builder.Configuration.GetValue<double>("SENTRY_TRACES_SAMPLE_RATE", 1.0);

        // Enable Sentry SDK debug mode in development
        options.Debug = builder.Environment.IsDevelopment();

        // Set Environment based on ASPNETCORE_ENVIRONMENT
        options.Environment = builder.Environment.EnvironmentName;

        // Set Release from assembly version or environment variable
        options.Release = builder.Configuration["SENTRY_RELEASE"]
            ?? System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

        // Add breadcrumbs for logger messages of level Information and higher
        options.MinimumBreadcrumbLevel = LogLevel.Information;

        // Capture events for logger messages of level Error and higher
        options.MinimumEventLevel = LogLevel.Error;

        // Disable request body capture to avoid leaking sensitive data
        options.MaxRequestBodySize = Sentry.Extensibility.RequestSize.None;

        // Do not send PII to Sentry
        options.SendDefaultPii = false;

        // Enable stacktrace attachment
        options.AttachStacktrace = true;

        // Configure Profiling (Disabled by default)
        options.ProfilesSampleRate = builder.Configuration.GetValue<double>("SENTRY_PROFILES_SAMPLE_RATE", 0.0);
        if (options.ProfilesSampleRate > 0)
        {
            options.AddProfilingIntegration();
        }
    });
}

builder.Services.AddSwaggerConfig();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration, builder.Environment);

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<BatchJobWorker>();
}

builder.Services.AddRateLimitingConfig(builder.Configuration, builder.Environment);

// Add CORS for Flutter
builder.Services.AddCustomCors(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseCors();

app.UseMiddleware<Valora.Api.Middleware.RequestMetricsMiddleware>();
app.UseMiddleware<Valora.Api.Middleware.ExceptionHandlingMiddleware>();

// Log warning if CORS is insecurely configured in production
app.LogCorsWarning();

// Apply database migrations
await DbInitializer.InitializeAsync(app.Services, app.Configuration, app.Environment);

// Only use HTTPS redirection if specifically enabled and NOT on Render
// Render handles SSL termination and redirection at the load balancer level.
var isOnRender = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RENDER"));
// Apply HSTS and HTTPS Redirection for all non-development environments
if (!isOnRender && (!app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("ENABLE_HTTPS_REDIRECTION")))
{
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }
    app.UseHttpsRedirection();
}

// Middleware Pipeline Order is Critical:
// -------------------------------------
// 1. Security Headers (Always first)
//    We must attach security headers (HSTS, CSP, X-Frame-Options) to *every* response,
//    even error responses or 404s.
app.UseMiddleware<Valora.Api.Middleware.SecurityHeadersMiddleware>();

// 2. Authentication (Identify the user)
//    Parses the JWT and sets the ClaimsPrincipal.
app.UseAuthentication();

// 3. Rate Limiting (After Auth)
//    We rate limit after authentication so we can exempt Admin users from limits.
//    Unauthenticated requests will still be limited as per policy (User is anonymous).
app.UseRateLimiter();

// Sentry User Enrichment Middleware
app.UseMiddleware<Valora.Api.Middleware.SentryUserMiddleware>();

app.UseAuthorization();

// Map Auth Endpoints (Injects IConfiguration into handler)
app.MapAuthEndpoints();
app.MapNotificationEndpoints();
app.MapAiEndpoints();
app.MapMapEndpoints();
app.MapAdminEndpoints();
app.MapUserProfileEndpoints();
app.MapWorkspaceEndpoints();
app.MapContextReportEndpoints();

// API Endpoints
var api = app.MapGroup("/api").RequireRateLimiting("fixed");


/// <summary>
/// Health check endpoint. Used by Docker Compose and load balancers.
/// </summary>
/// <summary>
/// Health check endpoint. Used by Docker Compose and load balancers.
/// </summary>
api.MapGet("/health", async (ISystemHealthService healthService, CancellationToken ct) =>
{
    var health = await healthService.GetHealthAsync(ct);

    if (health.IsHealthy)
    {
        return Results.Ok(health);
    }

    return Results.Json(health, statusCode: 503);
})
.DisableRateLimiting();

app.Run();

public partial class Program { }
