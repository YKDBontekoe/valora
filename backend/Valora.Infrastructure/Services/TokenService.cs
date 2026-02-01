using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly JwtOptions _options;

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
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
}
