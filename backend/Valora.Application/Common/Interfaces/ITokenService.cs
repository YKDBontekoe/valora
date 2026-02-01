using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateToken(ApplicationUser user);
    Domain.Entities.RefreshToken GenerateRefreshToken(string userId);
    Task SaveRefreshTokenAsync(Domain.Entities.RefreshToken token);
    Task<Domain.Entities.RefreshToken?> GetRefreshTokenAsync(string token);
}
