using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly Microsoft.Extensions.Logging.ILogger<TokenService> _logger;

    public TokenService(
        IOptions<JwtOptions> options,
        ValoraDbContext context,
        TimeProvider timeProvider,
        UserManager<ApplicationUser> userManager,
        Microsoft.Extensions.Logging.ILogger<TokenService> logger)
    {
        _options = options.Value;
        _context = context;
        _timeProvider = timeProvider;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<string> CreateJwtTokenAsync(ApplicationUser user)
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
        var roles = await _userManager.GetRolesAsync(user);
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

    public async Task SaveRefreshTokenAsync(RefreshToken token)
    {
        if (string.IsNullOrWhiteSpace(token.TokenHash))
        {
            throw new InvalidOperationException("Refresh token hash is required.");
        }

        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var tokenHash = RefreshToken.ComputeHash(token);
        return await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash);
    }

    public async Task<RefreshToken?> GetActiveRefreshTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var tokenHash = RefreshToken.ComputeHash(token);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r =>
                r.TokenHash == tokenHash &&
                r.Revoked == null &&
                r.Expires > now);
    }

    public async Task RevokeRefreshTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return;

        var tokenHash = RefreshToken.ComputeHash(token);
        var existingToken = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == tokenHash);
        if (existingToken != null)
        {
            existingToken.Revoked = _timeProvider.GetUtcNow().UtcDateTime;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RevokeAllRefreshTokensAsync(string userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(r => r.UserId == userId && r.Revoked == null)
            .ToListAsync();

        if (tokens.Any())
        {
            _logger.LogWarning("Security: Revoking all {Count} active refresh tokens for user {UserId}", tokens.Count, userId);
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            foreach (var token in tokens)
            {
                token.Revoked = now;
            }
            await _context.SaveChangesAsync();
        }
    }
}
