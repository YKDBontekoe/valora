using Valora.Application.Common.Models;
using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<(Result Result, string UserId)> CreateUserAsync(string email, string password);
    Task<bool> CheckPasswordAsync(string email, string password);
    Task<ApplicationUser?> GetUserByEmailAsync(string email);
    Task<ApplicationUser?> GetUserByIdAsync(string userId);
    Task EnsureRoleAsync(string roleName);
    Task<Result> AddToRoleAsync(string userId, string roleName);
    Task<Result> UpdateProfileAsync(string userId, string? firstName, string? lastName, int defaultRadiusMeters, bool biometricsEnabled);
    Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
}
