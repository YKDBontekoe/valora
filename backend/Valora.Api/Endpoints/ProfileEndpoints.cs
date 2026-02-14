using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Valora.Api.Filters;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;

namespace Valora.Api.Endpoints;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profile")
            .RequireAuthorization()
            .RequireRateLimiting("strict");

        group.MapGet("/", async (
            IIdentityService identityService,
            ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var appUser = await identityService.GetUserByIdAsync(userId);
            if (appUser == null) return Results.NotFound();

            return Results.Ok(new UserProfileDto(
                appUser.Email!,
                appUser.FirstName,
                appUser.LastName,
                appUser.DefaultRadiusMeters,
                appUser.BiometricsEnabled
            ));
        });

        group.MapPut("/", async (
            [FromBody] UpdateProfileDto dto,
            IIdentityService identityService,
            ClaimsPrincipal user,
            ILoggerFactory loggerFactory) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var logger = loggerFactory.CreateLogger("ProfileEndpoints");

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var result = await identityService.UpdateProfileAsync(
                userId,
                dto.FirstName,
                dto.LastName,
                dto.DefaultRadiusMeters,
                dto.BiometricsEnabled);

            if (result.Succeeded)
            {
                return Results.Ok();
            }

            logger.LogWarning("Profile update failed for user {UserId}: {Errors}", userId, string.Join(", ", result.Errors));
            return Results.BadRequest(new { error = "Failed to update profile. Please check your input and try again." });
        })
        .AddEndpointFilter<ValidationFilter<UpdateProfileDto>>();

        group.MapPost("/change-password", async (
            [FromBody] ChangePasswordDto dto,
            IIdentityService identityService,
            ClaimsPrincipal user,
            ILoggerFactory loggerFactory) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var logger = loggerFactory.CreateLogger("ProfileEndpoints");

            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var result = await identityService.ChangePasswordAsync(
                userId,
                dto.CurrentPassword,
                dto.NewPassword);

            if (result.Succeeded)
            {
                return Results.Ok();
            }

            logger.LogWarning("Password change failed for user {UserId}: {Errors}", userId, string.Join(", ", result.Errors));
            return Results.BadRequest(new { error = "Failed to change password. Ensure your current password is correct." });
        })
        .AddEndpointFilter<ValidationFilter<ChangePasswordDto>>();
    }
}
