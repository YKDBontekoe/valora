using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;

namespace Valora.Api.Endpoints;

public static class SavedPropertiesEndpoints
{
    public static RouteGroupBuilder MapSavedPropertiesEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/saved-properties")
            .WithTags("SavedProperties")
            .RequireAuthorization();

        group.MapGet("/", async (ValoraDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var properties = await db.SavedProperties
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(ct);

            return Results.Ok(properties);
        })
        .WithName("GetSavedProperties");

        group.MapPost("/", async (
            [FromBody] SavedPropertyRequestDto request,
            ValoraDbContext db,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            // Check if already saved
            var exists = await db.SavedProperties.AnyAsync(x =>
                x.UserId == userId &&
                x.Address == request.Address, ct);

            if (exists)
            {
                return Results.Conflict("Property already saved.");
            }

            var entity = new SavedProperty
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Address = request.Address,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                CachedScore = request.CachedScore,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.SavedProperties.Add(entity);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/saved-properties/{entity.Id}", entity);
        })
        .WithName("SaveProperty");

        group.MapDelete("/{id}", async (Guid id, ValoraDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var property = await db.SavedProperties.FindAsync(new object[] { id }, ct);

            if (property == null)
            {
                return Results.NotFound();
            }

            if (property.UserId != userId)
            {
                return Results.Forbid();
            }

            db.SavedProperties.Remove(property);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("DeleteSavedProperty");

        return group;
    }
}

public record SavedPropertyRequestDto(string Address, double Latitude, double Longitude, string? CachedScore);
