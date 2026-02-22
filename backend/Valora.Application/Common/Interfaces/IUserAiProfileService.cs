using Valora.Application.Common.Models;
using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IUserAiProfileService
{
    Task<UserAiProfileDto> GetProfileAsync(string userId, CancellationToken cancellationToken);
    Task<UserAiProfileDto> UpdateProfileAsync(string userId, UserAiProfileDto dto, CancellationToken cancellationToken);
    Task<Result> DeleteProfileAsync(string userId, CancellationToken cancellationToken);
    Task<string> ExportProfileAsync(string userId, CancellationToken cancellationToken);
}
