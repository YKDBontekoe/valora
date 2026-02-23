using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface ITokenService
{
    Task<string> CreateJwtTokenAsync(ApplicationUser user);
    Task SaveRefreshTokenAsync(Domain.Entities.RefreshToken token);
    Task<Domain.Entities.RefreshToken?> GetRefreshTokenAsync(string token);
    Task<Domain.Entities.RefreshToken?> GetActiveRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token);
    Task RevokeAllRefreshTokensAsync(string userId);
}
