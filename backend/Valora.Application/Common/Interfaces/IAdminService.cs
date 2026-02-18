using Valora.Application.Common.Models;
using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IAdminService
{
    Task<PaginatedList<AdminUserDto>> GetUsersAsync(int pageNumber, int pageSize, string? searchQuery = null, string? sortBy = null);
    Task<Result> DeleteUserAsync(string targetUserId, string currentUserId);
    Task<AdminStatsDto> GetSystemStatsAsync();
}
