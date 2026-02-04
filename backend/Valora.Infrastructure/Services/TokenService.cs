using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;

namespace Valora.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly ValoraDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly UserManager<ApplicationUser> _userManager;

    public TokenService(
        IOptions<JwtOptions> options,
        ValoraDbContext context,
        TimeProvider timeProvider,
        UserManager<ApplicationUser> userManager)
    {
        _options = options.Value;
        _context = context;
        _timeProvider = timeProvider;
        _userManager = userManager;
    }

    public string GenerateToken(ApplicationUser user)
    {
        if (string.IsNullOrEmpty(_options.Secret))
        {
            throw new InvalidOperationException("JWT_SECRET is not configured.");
        }

        var authClaims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName ?? ""),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        // Add User Roles
        var roles = _userManager.GetRolesAsync(user).GetAwaiter().GetResult();
        foreach (var role in roles)
        {
            authClaims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            expires: now.AddMinutes(_options.ExpiryMinutes),
            claims: authClaims,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public RefreshToken GenerateRefreshToken(string userId)
    {
        var randomNumber = new byte[64];
        RandomNumberGenerator.Fill(randomNumber);

        var rawToken = Convert.ToBase64String(randomNumber);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return new RefreshToken
        {
            RawToken = rawToken,
            TokenHash = HashRefreshToken(rawToken),
            Expires = now.AddDays(30),
            CreatedAt = now,
            UserId = userId
        };
    }

    public async Task SaveRefreshTokenAsync(RefreshToken token)
    {
        if (string.IsNullOrWhiteSpace(token.TokenHash))
        {
            if (string.IsNullOrWhiteSpace(token.RawToken))
            {
                throw new InvalidOperationException("Refresh token value is required.");
            }

            token.TokenHash = HashRefreshToken(token.RawToken);
        }

        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        var tokenHash = HashRefreshToken(token);
        return await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash);
    }

    public async Task RevokeRefreshTokenAsync(string token)
    {
        var tokenHash = HashRefreshToken(token);
        var existingToken = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == tokenHash);
        if (existingToken != null)
        {
            existingToken.Revoked = _timeProvider.GetUtcNow().UtcDateTime;
            await _context.SaveChangesAsync();
        }
    }

    private static string HashRefreshToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
