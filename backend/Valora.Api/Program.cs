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
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"];
if (string.IsNullOrEmpty(secretKey))
{
    if (builder.Environment.IsDevelopment())
    {
        secretKey = "DevSecretKey_ChangeMe_In_Production_Configuration_123!";
        Console.WriteLine("WARNING: JWT Secret is not configured. Using temporary development key.");
    }
    else
    {
        throw new InvalidOperationException("JWT Secret is missing in Production configuration.");
    }
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
    };
});

builder.Services.AddAuthorization();

// Add SignalR
builder.Services.AddSignalR();
builder.Services.AddScoped<IScraperNotificationService, SignalRNotificationService>();

// Add Hangfire with PostgreSQL storage
var rawConnectionString = builder.Configuration["DATABASE_URL"] ?? builder.Configuration.GetConnectionString("DefaultConnection");
var connectionString = ConnectionStringParser.BuildConnectionString(rawConnectionString);
var hangfireEnabled = builder.Configuration.GetValue<bool>("Hangfire:Enabled");

if (hangfireEnabled)
{
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

if (app.Environment.IsProduction() || builder.Configuration["EnableHttpsRedirection"] == "true")
{
    app.UseHttpsRedirection();
}

// Map Auth Endpoints
app.MapAuthEndpoints(builder.Configuration);

// Map Hubs
app.MapHub<ScraperHub>("/hubs/scraper").RequireAuthorization();

// Re-check configuration from built app to ensure test overrides are respected
if (app.Configuration.GetValue<bool>("Hangfire:Enabled"))
{
    // Hangfire Dashboard
    app.UseHangfireDashboard("/hangfire");

    // Configure recurring job for scraping
    RecurringJob.AddOrUpdate<FundaScraperJob>(
        "funda-scraper",
        job => job.ExecuteAsync(CancellationToken.None),
        builder.Configuration["Scraper:CronExpression"] ?? "0 */6 * * *"); // Default: every 6 hours
}

// API Endpoints
var api = app.MapGroup("/api");

api.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

api.MapGet("/listings", async ([AsParameters] ListingFilterDto filter, IListingRepository repo, CancellationToken ct) =>
{
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
        listing.PropertyType, listing.Status, listing.Url, listing.ImageUrl, listing.ListedDate, listing.CreatedAt
    );
    return Results.Ok(dto);
}).RequireAuthorization();

// Manual trigger endpoint for scraping
api.MapPost("/scraper/trigger", (FundaScraperJob job, CancellationToken ct) =>
{
    if (!hangfireEnabled) return Results.StatusCode(503);
    BackgroundJob.Enqueue<FundaScraperJob>(j => j.ExecuteAsync(ct));
    return Results.Ok(new { message = "Scraper job queued" });
}).RequireAuthorization();

// Seed endpoint
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
}).RequireAuthorization();

app.Run();

public partial class Program { }
