using Microsoft.AspNetCore.Mvc;
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

            var token = tokenService.GenerateToken(user);
            var refreshToken = tokenService.GenerateRefreshToken(user.Id);

            await tokenService.SaveRefreshTokenAsync(refreshToken);

            return Results.Ok(new AuthResponseDto(
                token,
                refreshToken.Token,
                user.Email!,
                user.Id
            ));
        });

        group.MapPost("/refresh", async (
            [FromBody] RefreshTokenRequestDto request,
            ITokenService tokenService) =>
        {
            var existingToken = await tokenService.GetRefreshTokenAsync(request.RefreshToken);

            if (existingToken == null || !existingToken.IsActive || existingToken.User == null)
            {
                return Results.Unauthorized();
            }

            // Rotate Refresh Token
            await tokenService.RevokeRefreshTokenAsync(existingToken.Token);
            var newRefreshToken = tokenService.GenerateRefreshToken(existingToken.UserId);
            await tokenService.SaveRefreshTokenAsync(newRefreshToken);

            var newAccessToken = tokenService.GenerateToken(existingToken.User);

            return Results.Ok(new AuthResponseDto(
                newAccessToken,
                newRefreshToken.Token,
                existingToken.User.Email!,
                existingToken.User.Id
            ));
        });
    }
}
