using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;

namespace Valora.Api.Endpoints;

public static class UserProfileEndpoints
{
    public static void MapUserProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/user/ai-profile")
            .RequireAuthorization()
            .RequireRateLimiting("fixed");

        group.MapGet("/", async (
            IUserAiProfileService profileService,
            ICurrentUserService currentUserService,
            CancellationToken ct) =>
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var profile = await profileService.GetProfileAsync(userId, ct);
            return Results.Ok(profile);
        });

        group.MapPut("/", async (
            [FromBody] UserAiProfileDto dto,
            IUserAiProfileService profileService,
            ICurrentUserService currentUserService,
            CancellationToken ct) =>
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var updatedProfile = await profileService.UpdateProfileAsync(userId, dto, ct);
            return Results.Ok(updatedProfile);
        })
        .AddEndpointFilter<Valora.Api.Filters.ValidationFilter<UserAiProfileDto>>();

        group.MapDelete("/", async (
            IUserAiProfileService profileService,
            ICurrentUserService currentUserService,
            CancellationToken ct) =>
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            await profileService.DeleteProfileAsync(userId, ct);
            return Results.NoContent();
        });

        group.MapGet("/export", async (
            IUserAiProfileService profileService,
            ICurrentUserService currentUserService,
            CancellationToken ct) =>
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var json = await profileService.ExportProfileAsync(userId, ct);
            return Results.File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", "valora-ai-profile.json");
        });

        // Admin Endpoints
        var adminGroup = app.MapGroup("/api/admin/users/{userId}/ai-profile")
            .RequireAuthorization("Admin");

        adminGroup.MapGet("/", async (
            string userId,
            IUserAiProfileService profileService,
            CancellationToken ct) =>
        {
            var profile = await profileService.GetProfileAsync(userId, ct);
            return Results.Ok(profile);
        });

        adminGroup.MapDelete("/", async (
            string userId,
            IUserAiProfileService profileService,
            CancellationToken ct) =>
        {
            await profileService.DeleteProfileAsync(userId, ct);
            return Results.NoContent();
        });

        adminGroup.MapGet("/export", async (
            string userId,
            IUserAiProfileService profileService,
            CancellationToken ct) =>
        {
            var json = await profileService.ExportProfileAsync(userId, ct);
            return Results.File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", $"user-{userId}-ai-profile.json");
        });
    }
}
