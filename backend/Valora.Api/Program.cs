using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Valora.Api.Endpoints;
using Valora.Api.Middleware;
using Valora.Application;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Mappings;
using Valora.Application.DTOs;
using Valora.Infrastructure;
using Valora.Infrastructure.Persistence;
using Valora.Domain.Entities;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Valora API", Version = "v1" });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// Add Identity
builder.Services.AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ValoraDbContext>();

// Add Authentication
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var secret = builder.Configuration["JWT_SECRET"]
                     ?? throw new InvalidOperationException("JWT_SECRET is not configured.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JWT_ISSUER"],
            ValidAudience = builder.Configuration["JWT_AUDIENCE"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                // logger.LogError(context.Exception, "Authentication failed");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                // logger.LogDebug("Token validated for: {User}", context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                // logger.LogWarning("OnChallenge: {Error}, {ErrorDescription}", context.Error, context.ErrorDescription);
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
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();

        if (allowedOrigins is null || allowedOrigins.Length == 0)
        {
            if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
            {
                allowedOrigins = ["http://localhost:3000"];
            }
            else
            {
                throw new InvalidOperationException("AllowedOrigins must be configured in production.");
            }
        }

        policy.WithOrigins(allowedOrigins)
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
app.MapAiEndpoints();
app.MapMapEndpoints();

// API Endpoints
var api = app.MapGroup("/api");

/// <summary>
/// Health check endpoint. Used by Docker Compose and load balancers.
/// </summary>
api.MapGet("/health", async (ValoraDbContext db, CancellationToken ct) =>
{
    if (await db.Database.CanConnectAsync(ct))
    {
        return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    return Results.Problem("Service unavailable", statusCode: 503);
});

/// <summary>
/// Retrieves a paginated list of listings based on filter criteria.
/// Requires Authentication.
/// </summary>
api.MapGet("/listings", async ([AsParameters] ListingFilterDto filter, IListingService listingService, CancellationToken ct) =>
{
    var result = await listingService.GetSummariesAsync(filter, ct);

    if (!result.Succeeded)
    {
        return Results.BadRequest(result.Errors.Select(e => new { Error = e }));
    }

    var paginatedList = result.Value;

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
api.MapGet("/listings/{id:guid}", async (Guid id, IListingService listingService, CancellationToken ct) =>
{
    var result = await listingService.GetByIdAsync(id, ct);
    if (!result.Succeeded) return Results.NotFound();
    
    return Results.Ok(result.Value);
}).RequireAuthorization();

api.MapPost("/context/report", async (
    ContextReportRequestDto request,
    IContextReportService contextReportService,
    CancellationToken ct) =>
{
    var report = await contextReportService.BuildAsync(request, ct);
    return Results.Ok(report);
})
.RequireAuthorization()
.AddEndpointFilter<Valora.Api.Filters.ValidationFilter<ContextReportRequestDto>>();

api.MapPost("/listings/{id:guid}/enrich", async (
    Guid id,
    IListingService listingService,
    CancellationToken ct) =>
{
    var result = await listingService.EnrichListingAsync(id, ct);
    if (!result.Succeeded) return Results.NotFound();

    return Results.Ok(new { message = "Listing enriched successfully", compositeScore = result.Value });
}).RequireAuthorization();

app.Run();

public partial class Program { }
