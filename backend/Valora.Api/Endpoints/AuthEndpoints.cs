using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (
            [FromBody] RegisterDto registerDto,
            UserManager<ApplicationUser> userManager) =>
        {
            if (registerDto.Password != registerDto.ConfirmPassword)
            {
                return Results.BadRequest(new { error = "Passwords do not match" });
            }

            var user = new ApplicationUser { UserName = registerDto.Email, Email = registerDto.Email };
            var result = await userManager.CreateAsync(user, registerDto.Password);

            if (result.Succeeded)
            {
                return Results.Ok(new { message = "User created successfully" });
            }

            return Results.BadRequest(result.Errors);
        });

        group.MapPost("/login", async (
            [FromBody] LoginDto loginDto,
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService) =>
        {
            var user = await userManager.FindByEmailAsync(loginDto.Email);
            if (user == null || !await userManager.CheckPasswordAsync(user, loginDto.Password))
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

            if (existingToken == null || !existingToken.IsActive)
            {
                return Results.Unauthorized();
            }

            var newAccessToken = tokenService.GenerateToken(existingToken.User!);

            return Results.Ok(new AuthResponseDto(
                newAccessToken,
                existingToken.Token,
                existingToken.User!.Email!,
                existingToken.User.Id
            ));
        });
    }
}
