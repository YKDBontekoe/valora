using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateToken(ApplicationUser user);
}
