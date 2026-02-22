using Valora.Application.Common.Models;
using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IUserAiProfileRepository
{
    Task<UserAiProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken);
    Task AddAsync(UserAiProfile profile, CancellationToken cancellationToken);
    Task UpdateAsync(UserAiProfile profile, CancellationToken cancellationToken);
    Task DeleteAsync(UserAiProfile profile, CancellationToken cancellationToken);
}
