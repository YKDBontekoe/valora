using Valora.Application.Common.Models;
using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IAdminService
{
    Task<PaginatedList<AdminUserDto>> GetUsersAsync(int pageNumber, int pageSize);
    Task<Result> DeleteUserAsync(string targetUserId, string currentUserId);
    Task<AdminStatsDto> GetSystemStatsAsync();
}
