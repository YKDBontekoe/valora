using Valora.Application.Common.Models;
using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<(Result Result, string UserId)> CreateUserAsync(string email, string password);
    Task<bool> CheckPasswordAsync(string email, string password);
    Task<ApplicationUser?> GetUserByEmailAsync(string email);
    Task EnsureRoleAsync(string roleName);
    Task<Result> AddToRoleAsync(string userId, string roleName);
    Task<PaginatedList<ApplicationUser>> GetUsersAsync(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, string? sortOrder = null);
    Task<Result> DeleteUserAsync(string userId);
    Task<IList<string>> GetUserRolesAsync(ApplicationUser user);
    Task<int> CountAsync();
    Task<IDictionary<string, IList<string>>> GetRolesForUsersAsync(IEnumerable<ApplicationUser> users);
}
