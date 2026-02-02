using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;

namespace Valora.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (
            [FromBody] RegisterDto registerDto,
            IAuthService authService) =>
        {
            var result = await authService.RegisterAsync(registerDto);

            if (result.Succeeded)
            {
                return Results.Ok(new { message = "User created successfully" });
            }

            return Results.BadRequest(result.Errors.Select(e => new { description = e }));
        });

        group.MapPost("/login", async (
            [FromBody] LoginDto loginDto,
            IAuthService authService) =>
        {
            var response = await authService.LoginAsync(loginDto);

            if (response == null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(response);
        });

        group.MapPost("/refresh", async (
            [FromBody] RefreshTokenRequestDto request,
            IAuthService authService) =>
        {
            var response = await authService.RefreshTokenAsync(request.RefreshToken);

            if (response == null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(response);
        });

        group.MapPut("/profile", async (
            [FromBody] UpdateProfileDto updateProfileDto,
            IAuthService authService,
            ClaimsPrincipal user) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var result = await authService.UpdateProfileAsync(userId, updateProfileDto);

            if (result.Succeeded)
            {
                return Results.Ok(new { message = "Profile updated successfully" });
            }

            return Results.BadRequest(result.Errors.Select(e => new { description = e }));
        }).RequireAuthorization();
    }
}
