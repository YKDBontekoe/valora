using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Valora.Application;
using Valora.Infrastructure;
using Valora.Infrastructure.Persistence;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Mappings;
using Valora.Application.DTOs;
using Valora.Api.Endpoints;
using Valora.Api.Filters;
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
        // We enforce providing a strong secret via environment variables.
        var secret = configuration["JWT_SECRET"];

        if (string.IsNullOrEmpty(secret))
        {
            throw new InvalidOperationException("JWT Secret is missing in configuration. Please set JWT_SECRET.");
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
            if (dbContext.Database.IsRelational())
            {
                dbContext.Database.Migrate();
            }

            // Seed Admin User
            var adminEmail = app.Configuration["ADMIN_EMAIL"];
            var adminPassword = app.Configuration["ADMIN_PASSWORD"];

            if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPassword))
            {
                var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                await identityService.EnsureRoleAsync("Admin");
                
                var user = await identityService.GetUserByEmailAsync(adminEmail);
                if (user == null)
                {
                    // Only create and promote if user does NOT exist (prevents takeover of existing accounts)
                    var (createResult, userId) = await identityService.CreateUserAsync(adminEmail, adminPassword);
                    if (createResult.Succeeded)
                    {
                        var roleResult = await identityService.AddToRoleAsync(userId, "Admin");
                        if (roleResult.Succeeded)
                        {
                            logger.LogInformation("Successfully seeded initial Admin user.");
                        }
                        else
                        {
                            logger.LogWarning("Created Admin user but failed to assign role. Check identity logs.");
                        }
                    }
                    else
                    {
                        logger.LogWarning("Failed to create initial Admin user. Check identity logs.");
                    }
                }
                else
                {
                    logger.LogWarning("Admin seeding: User configured in ADMIN_EMAIL already exists. Skipping automatic promotion to prevent privilege escalation.");
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
app.MapNotificationEndpoints();

// API Endpoints
var api = app.MapGroup("/api")
    .AddEndpointFilter<ValidationFilter>();

/// <summary>
/// Health check endpoint. Used by Docker Compose and load balancers.
/// </summary>
api.MapGet("/health", async (ValoraDbContext db, ILogger<Program> logger, CancellationToken ct) =>
{
    try
    {
        if (await db.Database.CanConnectAsync(ct))
        {
            return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }
        else
        {
            return Results.Problem("Service unavailable", statusCode: 503);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Health check failed");
        return Results.Problem("Service unavailable", statusCode: 503);
    }
});

/// <summary>
/// Retrieves a paginated list of listings based on filter criteria.
/// Requires Authentication.
/// </summary>
api.MapGet("/listings", async ([AsParameters] ListingFilterDto filter, IListingRepository repo, CancellationToken ct) =>
{
    var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(filter);
    var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

    if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(filter, validationContext, validationResults, true))
    {
        return Results.BadRequest(validationResults.Select(r => new { Property = r.MemberNames.FirstOrDefault(), Error = r.ErrorMessage }));
    }

    var paginatedList = await repo.GetSummariesAsync(filter, ct);

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

/// <summary>
/// Looks up property details from PDOK by ID and enriches with neighborhood analytics.
/// Requires Authentication.
/// </summary>
api.MapGet("/listings/lookup", async (string id, IPdokListingService pdokService, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(id)) return Results.BadRequest("ID is required");
    
    var listing = await pdokService.GetListingDetailsAsync(id, ct);
    if (listing is null) return Results.NotFound();

    return Results.Ok(listing);
}).RequireAuthorization();

/// <summary>
/// Retrieves detailed information for a specific listing by ID.
/// Requires Authentication.
/// </summary>
api.MapGet("/listings/{id:guid}", async (Guid id, IListingRepository repo, CancellationToken ct) =>
{
    var listing = await repo.GetByIdAsync(id, ct);
    if (listing is null) return Results.NotFound();
    
    var dto = ListingMapper.ToDto(listing);
    return Results.Ok(dto);
}).RequireAuthorization();

api.MapPost("/context/report", async (
    ContextReportRequestDto request,
    IContextReportService contextReportService,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Input))
    {
        return Results.BadRequest(new { error = "Input is required" });
    }

    try
    {
        var report = await contextReportService.BuildAsync(request, ct);
        return Results.Ok(report);
    }
    catch (ValidationException ex)
    {
        return Results.BadRequest(new
        {
            error = "Validation failed",
            errors = ex.Errors
        });
    }
}).RequireAuthorization();

api.MapPost("/listings/{id:guid}/enrich", async (
    Guid id,
    IListingRepository repo,
    IContextReportService contextReportService,
    CancellationToken ct) =>
{
    var listing = await repo.GetByIdAsync(id, ct);
    if (listing is null) return Results.NotFound();

    // 1. Generate Report
    ContextReportRequestDto request = new(Input: listing.Address); // Default 1km radius
    
    try 
    {
        // We use the application DTO for the service call...
        var reportDto = await contextReportService.BuildAsync(request, ct);

        // ...and map it to the Domain model for storage
        var contextReportModel = ListingMapper.MapToDomain(reportDto);

        // 2. Update Entity
        listing.ContextReport = contextReportModel;
        listing.ContextCompositeScore = reportDto.CompositeScore;
        
        if (reportDto.CategoryScores.TryGetValue("Social", out var social)) listing.ContextSocialScore = social;
        if (reportDto.CategoryScores.TryGetValue("Safety", out var crime)) listing.ContextSafetyScore = crime; // Mapping "Safety" to "Safety" score
        if (reportDto.CategoryScores.TryGetValue("Demographics", out var demo)) { /* No specific column yet */ }
        if (reportDto.CategoryScores.TryGetValue("Amenities", out var amenities)) listing.ContextAmenitiesScore = amenities;
        if (reportDto.CategoryScores.TryGetValue("Environment", out var env)) listing.ContextEnvironmentScore = env;

        // 3. Save
        await repo.UpdateAsync(listing, ct);

        return Results.Ok(new { message = "Listing enriched successfully", compositeScore = reportDto.CompositeScore });
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to enrich listing {ListingId}", id);
        return Results.Problem("Failed to enrich listing. Please try again later.");
    }

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
