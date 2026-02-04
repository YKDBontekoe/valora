using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Valora.Application;
using Valora.Infrastructure;
using Valora.Infrastructure.Jobs;
using Valora.Infrastructure.Persistence;
using Valora.Application.Common.Interfaces;
using Valora.Api.Endpoints;
using Valora.Api.Hubs;
using Valora.Api.Services;
using Valora.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ValoraDbContext>()
    .AddDefaultTokenProviders();

// Add Authentication & JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration, IWebHostEnvironment, ILogger<Program>>((options, configuration, env, logger) =>
    {
        // JWT Secret configuration is critical.
        // In Production, we enforce providing a strong secret via environment variables.
        // In Development, we allow a fallback to a hardcoded secret to simplify onboarding,
        // but we log a warning to ensure developers are aware of this.
        var secret = configuration["JWT_SECRET"];

        if (string.IsNullOrEmpty(secret))
        {
            if (env.IsDevelopment())
            {
                secret = "DevSecretKey_ChangeMe_In_Production_Configuration_123!";
                logger.LogWarning("WARNING: JWT Secret is not configured. Using temporary development key.");
            }
            else
            {
                throw new InvalidOperationException("JWT Secret is missing in Production configuration.");
            }
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["JWT_ISSUER"],
            ValidAudience = configuration["JWT_AUDIENCE"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                logger.LogError(context.Exception, "Authentication failed");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                logger.LogDebug("Token validated for: {User}", context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                logger.LogWarning("OnChallenge: {Error}, {ErrorDescription}", context.Error, context.ErrorDescription);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Add SignalR
builder.Services.AddSignalR();
builder.Services.AddScoped<IScraperNotificationService, SignalRNotificationService>();

// Add Hangfire with PostgreSQL storage
// We manually parse the connection string because we might receive a raw URL (postgres://...)
// from cloud providers (e.g., Heroku, Fly.io) or a standard ADO.NET connection string.
// ConnectionStringParser handles this normalization.
var rawConnectionString = builder.Configuration["DATABASE_URL"] ?? builder.Configuration.GetConnectionString("DefaultConnection");
var connectionString = ConnectionStringParser.BuildConnectionString(rawConnectionString);
var hangfireEnabled = builder.Configuration.GetValue<bool>("HANGFIRE_ENABLED");

if (hangfireEnabled)
{
    // Configure Hangfire to use PostgreSQL for job storage.
    // This allows jobs to persist across restarts.
    builder.Services.AddHangfire(config =>
        config.UsePostgreSqlStorage(options =>
            options.UseNpgsqlConnection(connectionString)));
    builder.Services.AddHangfireServer();
}

// Add CORS for Flutter
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
    try
    {
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        // Log error or handle it (e.g., if database is not ready yet)
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.UseMiddleware<Valora.Api.Middleware.ExceptionHandlingMiddleware>();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsProduction() || app.Configuration.GetValue<bool>("ENABLE_HTTPS_REDIRECTION"))
{
    app.UseHttpsRedirection();
}

// Map Auth Endpoints (Injects IConfiguration into handler)
app.MapAuthEndpoints();

// Map Listing Endpoints
app.MapListingEndpoints();

// Map Scraper Endpoints
app.MapScraperEndpoints();

// Map Search Endpoints
app.MapSearchEndpoints();

// Map AI Endpoints
app.MapAiEndpoints();

// Map Hubs
app.MapHub<ScraperHub>("/hubs/scraper").RequireAuthorization();

// Re-check configuration from built app to ensure test overrides are respected
if (app.Configuration.GetValue<bool>("HANGFIRE_ENABLED"))
{
    // Hangfire Dashboard
    app.UseHangfireDashboard("/hangfire");

    // Configure recurring job for scraping
    RecurringJob.AddOrUpdate<FundaScraperJob>(
        "funda-scraper",
        job => job.ExecuteAsync(CancellationToken.None),
        builder.Configuration["SCRAPER_CRON"] ?? "0 */6 * * *"); // Default: every 6 hours
}

var api = app.MapGroup("/api");

api.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();

public partial class Program { }
