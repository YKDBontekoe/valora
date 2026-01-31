using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
            IConfiguration configuration) =>
        {
            var user = await userManager.FindByEmailAsync(loginDto.Email);
            if (user == null || !await userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                return Results.Unauthorized();
            }

            var authClaims = new List<Claim>
            {
                new(ClaimTypes.Name, user.UserName ?? ""),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var jwtSettings = configuration.GetSection("JwtSettings");
            var secret = jwtSettings["Secret"];
            if (string.IsNullOrEmpty(secret))
            {
                throw new InvalidOperationException("JwtSettings:Secret is not configured.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryMinutes"] ?? "60")),
                claims: authClaims,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return Results.Ok(new AuthResponseDto(
                new JwtSecurityTokenHandler().WriteToken(token),
                user.Email!,
                user.Id
            ));
        });
    }
}
