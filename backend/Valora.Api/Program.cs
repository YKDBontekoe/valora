using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Valora.Application;
using Valora.Infrastructure;
using Valora.Infrastructure.Jobs;
using Valora.Infrastructure.Persistence;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Api.Hubs;
using Valora.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add SignalR
builder.Services.AddSignalR();
builder.Services.AddScoped<IScraperNotificationService, SignalRNotificationService>();

// Add Hangfire with PostgreSQL storage
var connectionString = builder.Configuration["DATABASE_URL"] ?? builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(connectionString)));
builder.Services.AddHangfireServer();

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

if (app.Environment.IsProduction() || builder.Configuration["EnableHttpsRedirection"] == "true")
{
    app.UseHttpsRedirection();
}

// Hangfire Dashboard
app.UseHangfireDashboard("/hangfire");

// Map Hubs
app.MapHub<ScraperHub>("/hubs/scraper");

// Configure recurring job for scraping
RecurringJob.AddOrUpdate<FundaScraperJob>(
    "funda-scraper",
    job => job.ExecuteAsync(CancellationToken.None),
    builder.Configuration["Scraper:CronExpression"] ?? "0 */6 * * *"); // Default: every 6 hours

// API Endpoints
var api = app.MapGroup("/api");

api.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

api.MapGet("/listings", async ([AsParameters] ListingFilterDto filter, IListingRepository repo, CancellationToken ct) =>
{
    var paginatedList = await repo.GetAllAsync(filter, ct);
    var dtos = paginatedList.Items.Select(l => new ListingDto(
        l.Id, l.FundaId, l.Address, l.City, l.PostalCode, l.Price,
        l.Bedrooms, l.Bathrooms, l.LivingAreaM2, l.PlotAreaM2,
        l.PropertyType, l.Status, l.Url, l.ImageUrl, l.ListedDate, l.CreatedAt
    ));

    return Results.Ok(new
    {
        Items = dtos,
        paginatedList.PageIndex,
        paginatedList.TotalPages,
        paginatedList.TotalCount,
        paginatedList.HasNextPage,
        paginatedList.HasPreviousPage
    });
});

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
});

// Manual trigger endpoint for scraping
api.MapPost("/scraper/trigger", (FundaScraperJob job, CancellationToken ct) =>
{
    BackgroundJob.Enqueue<FundaScraperJob>(j => j.ExecuteAsync(ct));
    return Results.Ok(new { message = "Scraper job queued" });
});

// Seed endpoint
api.MapPost("/scraper/seed", async (string region, IListingRepository repo, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(region))
    {
        return Results.BadRequest("Region is required");
    }

    var count = await repo.CountAsync(ct);
    if (count > 0)
    {
        // "skip" if data exists as per requirements
        return Results.Ok(new { message = "Data already exists, skipping seed", skipped = true });
    }

    BackgroundJob.Enqueue<FundaSeedJob>(j => j.ExecuteAsync(region, CancellationToken.None));
    return Results.Ok(new { message = $"Seed job queued for {region}", skipped = false });
});

app.Run();

public partial class Program { }
