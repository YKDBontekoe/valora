using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface ITokenService
{
    Task<string> GenerateTokenAsync(ApplicationUser user);
    Domain.Entities.RefreshToken GenerateRefreshToken(string userId);
    Task SaveRefreshTokenAsync(Domain.Entities.RefreshToken token);
    Task<Domain.Entities.RefreshToken?> GetRefreshTokenAsync(string token);
    Task<Domain.Entities.RefreshToken?> GetActiveRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token);
}
