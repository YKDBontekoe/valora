using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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

    public TokenService(IOptions<JwtOptions> options, ValoraDbContext context)
    {
        _options = options.Value;
        _context = context;
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

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes),
            claims: authClaims,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public RefreshToken GenerateRefreshToken(string userId)
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

        return new RefreshToken
        {
            Token = Convert.ToBase64String(randomNumber),
            Expires = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };
    }

    public async Task SaveRefreshTokenAsync(RefreshToken token)
    {
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        return await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == token);
    }

    public async Task RevokeRefreshTokenAsync(string token)
    {
        var existingToken = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token);
        if (existingToken != null)
        {
            existingToken.Revoked = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
