using Microsoft.AspNetCore.Mvc;
using Valora.Api.Filters;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;

namespace Valora.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").RequireRateLimiting("strict");

        group.MapPost("/register", async (
            [FromBody] RegisterDto registerDto,
            IAuthService authService) =>
        {
            var result = await authService.RegisterAsync(registerDto);

            if (result.Succeeded)
            {
                return Results.Ok(new { message = "User created successfully" });
            }

            // Return generic error to avoid leaking implementation details
            return Results.Problem(detail: "Registration failed. Please check your details and try again.", statusCode: 400);
        })
        .AddEndpointFilter<ValidationFilter<RegisterDto>>();

        group.MapPost("/login", async (
            [FromBody] LoginDto loginDto,
            IAuthService authService) =>
        {
            var response = await authService.LoginAsync(loginDto);

            if (response == null)
            {
                return Results.Problem(detail: "Invalid email or password.", statusCode: 401);
            }

            return Results.Ok(response);
        })
        .AddEndpointFilter<ValidationFilter<LoginDto>>();

        group.MapPost("/refresh", async (
            [FromBody] RefreshTokenRequestDto request,
            IAuthService authService) =>
        {
            var response = await authService.RefreshTokenAsync(request.RefreshToken);

            if (response == null)
            {
                return Results.Problem(detail: "Invalid refresh token.", statusCode: 401);
            }

            return Results.Ok(response);
        })
        .AddEndpointFilter<ValidationFilter<RefreshTokenRequestDto>>();

        group.MapPost("/external-login", async (
            [FromBody] ExternalLoginRequestDto request,
            IAuthService authService) =>
        {
            var response = await authService.ExternalLoginAsync(request);

            if (response == null)
            {
                return Results.Problem(detail: "External authentication failed.", statusCode: 401);
            }

            return Results.Ok(response);
        })
        .AddEndpointFilter<ValidationFilter<ExternalLoginRequestDto>>();
    }
}
