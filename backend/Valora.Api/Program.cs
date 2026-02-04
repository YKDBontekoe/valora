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
using Valora.Application.DTOs;
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
if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        try
        {
            dbContext.Database.Migrate();

            // Seed Admin User
            var adminEmail = app.Configuration["ADMIN_EMAIL"];
            if (!string.IsNullOrEmpty(adminEmail))
            {
                var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                await identityService.EnsureRoleAsync("Admin");
                var user = await identityService.GetUserByEmailAsync(adminEmail);
                if (user != null)
                {
                    var result = await identityService.AddToRoleAsync(user.Id, "Admin");
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Successfully ensured Admin role for user: {Email}", adminEmail);
                    }
                    else
                    {
                        logger.LogWarning("Failed to add user {Email} to Admin role: {Errors}", adminEmail, string.Join(", ", result.Errors));
                    }
                }
                else
                {
                    logger.LogWarning("Admin seeding: User with email {Email} not found. Role ensured but not assigned.", adminEmail);
                }
            }
        }
        catch (Exception ex)
        {
            // Log error or handle it (e.g., if database is not ready yet)
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating the database.");
        }
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

// API Endpoints
var api = app.MapGroup("/api");

api.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

api.MapGet("/listings", async ([AsParameters] ListingFilterDto filter, IListingRepository repo, CancellationToken ct) =>
{
    var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(filter);
    var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

    if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(filter, validationContext, validationResults, true))
    {
        return Results.BadRequest(validationResults.Select(r => new { Property = r.MemberNames.FirstOrDefault(), Error = r.ErrorMessage }));
    }

    var paginatedList = await repo.GetAllAsync(filter, ct);

    return Results.Ok(new
    {
        paginatedList.Items,
        paginatedList.PageIndex,
        paginatedList.TotalPages,
        paginatedList.TotalCount,
        paginatedList.HasNextPage,
        paginatedList.HasPreviousPage
    });
}).RequireAuthorization();

api.MapGet("/listings/{id:guid}", async (Guid id, IListingRepository repo, CancellationToken ct) =>
{
    var listing = await repo.GetByIdAsync(id, ct);
    if (listing is null) return Results.NotFound();
    
    var dto = new ListingDto(
        listing.Id, listing.FundaId, listing.Address, listing.City, listing.PostalCode, listing.Price,
        listing.Bedrooms, listing.Bathrooms, listing.LivingAreaM2, listing.PlotAreaM2,
        listing.PropertyType, listing.Status, listing.Url, listing.ImageUrl, listing.ListedDate, listing.CreatedAt,
        // Rich Data
        listing.Description, listing.EnergyLabel, listing.YearBuilt, listing.ImageUrls,
        // Phase 2
        listing.OwnershipType, listing.CadastralDesignation, listing.VVEContribution, listing.HeatingType,
        listing.InsulationType, listing.GardenOrientation, listing.HasGarage, listing.ParkingType,
        // Phase 3
        listing.AgentName, listing.VolumeM3, listing.BalconyM2, listing.GardenM2, listing.ExternalStorageM2,
        listing.Features,
        // Geo & Media
        listing.Latitude, listing.Longitude, listing.VideoUrl, listing.VirtualTourUrl, listing.FloorPlanUrls, listing.BrochureUrl,
        // Construction
        listing.RoofType, listing.NumberOfFloors, listing.ConstructionPeriod, listing.CVBoilerBrand, listing.CVBoilerYear,
        // Broker
        listing.BrokerPhone, listing.BrokerLogoUrl,
        // Infra
        listing.FiberAvailable,
        // Status
        listing.PublicationDate, listing.IsSoldOrRented, listing.Labels
    );
    return Results.Ok(dto);
}).RequireAuthorization();

// Manual trigger endpoint for scraping
api.MapPost("/scraper/trigger", (FundaScraperJob job, CancellationToken ct) =>
{
    if (!hangfireEnabled) return Results.StatusCode(503);
    BackgroundJob.Enqueue<FundaScraperJob>(j => j.ExecuteAsync(ct));
    return Results.Ok(new { message = "Scraper job queued" });
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

// Limited trigger endpoint
api.MapPost("/scraper/trigger-limited", (string region, int limit, FundaScraperJob job, CancellationToken ct) =>
{
    if (!hangfireEnabled) return Results.StatusCode(503);
    BackgroundJob.Enqueue<FundaScraperJob>(j => j.ExecuteLimitedAsync(region, limit, ct));
    return Results.Ok(new { message = $"Limited scraper job queued for {region} (limit {limit})" });
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

// Seed endpoint
// Clear database endpoint
api.MapPost("/scraper/clear", async (IListingRepository repo, CancellationToken ct) =>
{
    await repo.ClearAllAsync(ct);
    return Results.Ok(new { message = "Database cleared" });
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

api.MapPost("/scraper/seed", async (string region, IListingRepository repo, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(region))
    {
        return Results.BadRequest("Region is required");
    }

    if (!hangfireEnabled) return Results.StatusCode(503);

    var count = await repo.CountAsync(ct);
    if (count > 0)
    {
        // "skip" if data exists as per requirements
        return Results.Ok(new { message = "Data already exists, skipping seed", skipped = true });
    }

    BackgroundJob.Enqueue<FundaSeedJob>(j => j.ExecuteAsync(region, CancellationToken.None));
    return Results.Ok(new { message = $"Seed job queued for {region}", skipped = false });
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

// Dynamic Funda search - cache-through pattern
// Searches Funda on-demand, caching results in the database
api.MapGet("/search", async (
    [AsParameters] Valora.Application.Scraping.FundaSearchQuery query,
    Valora.Application.Scraping.IFundaSearchService searchService,
    CancellationToken ct) =>
{
    // Explicit fallback validation to ensure constraints are respected even if attribute validation behaves unexpectedly
    if (!Valora.Application.Validators.SearchQueryValidator.IsValid(query, out var validationError))
    {
        return Results.BadRequest(new { error = validationError });
    }
    
    var result = await searchService.SearchAsync(query, ct);
    return Results.Ok(new
    {
        result.Items,
        result.TotalCount,
        result.Page,
        result.PageSize,
        result.FromCache
    });
}).RequireAuthorization();

// Lookup a specific Funda listing by URL
// Fetches from Funda if not cached or stale
api.MapGet("/lookup", async (
    string url,
    Valora.Application.Scraping.IFundaSearchService searchService,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(url))
    {
        return Results.BadRequest(new { error = "URL is required" });
    }
    
    var listing = await searchService.GetByFundaUrlAsync(url, ct);
    return listing is null 
        ? Results.NotFound(new { error = "Listing not found" }) 
        : Results.Ok(listing);
}).RequireAuthorization();

// AI Chat Endpoint
api.MapPost("/ai/chat", async (
    AiChatRequest request,
    IAiService aiService,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Prompt))
    {
        return Results.BadRequest(new { error = "Prompt is required" });
    }

    try
    {
        var response = await aiService.ChatAsync(request.Prompt, request.Model, ct);
        return Results.Ok(new { response });
    }
    catch (Exception ex)
    {
        // Log error
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
}).RequireAuthorization();

app.Run();

public partial class Program { }
