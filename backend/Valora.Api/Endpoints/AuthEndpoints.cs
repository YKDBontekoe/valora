using Microsoft.AspNetCore.Mvc;
using Valora.Api.Filters;
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
            IAuthService authService,
            ILogger<IAuthService> logger) =>
        {
            logger.LogInformation("Registration attempt for email: {Email}", registerDto.Email);

            var result = await authService.RegisterAsync(registerDto);

            if (result.Succeeded)
            {
                logger.LogInformation("User registered successfully: {Email}", registerDto.Email);
                return Results.Ok(new { message = "User created successfully" });
            }

            logger.LogWarning("Registration failed for {Email}. Errors: {Errors}", registerDto.Email, string.Join(", ", result.Errors));
            return Results.BadRequest(result.Errors.Select(e => new { description = e }));
        })
        .AddEndpointFilter<ValidationFilter<RegisterDto>>();

        group.MapPost("/login", async (
            [FromBody] LoginDto loginDto,
            IAuthService authService,
            ILogger<IAuthService> logger) =>
        {
            logger.LogInformation("Login attempt for email: {Email}", loginDto.Email);

            var response = await authService.LoginAsync(loginDto);

            if (response == null)
            {
                logger.LogWarning("Login failed for email: {Email}", loginDto.Email);
                return Results.Unauthorized();
            }

            logger.LogInformation("User logged in successfully: {UserId}", response.UserId);
            return Results.Ok(response);
        })
        .AddEndpointFilter<ValidationFilter<LoginDto>>();

        group.MapPost("/refresh", async (
            [FromBody] RefreshTokenRequestDto request,
            IAuthService authService,
            ILogger<IAuthService> logger) =>
        {
            // Do not log the token itself
            logger.LogInformation("Token refresh attempt.");

            var response = await authService.RefreshTokenAsync(request.RefreshToken);

            if (response == null)
            {
                logger.LogWarning("Token refresh failed.");
                return Results.Unauthorized();
            }

            logger.LogInformation("Token refreshed successfully for user: {UserId}", response.UserId);
            return Results.Ok(response);
        })
        .AddEndpointFilter<ValidationFilter<RefreshTokenRequestDto>>();
    }
}
