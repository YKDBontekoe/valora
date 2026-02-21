using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Services;

public class AdminService : IAdminService
{
    private readonly IIdentityService _identityService;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        IIdentityService identityService,
        INotificationRepository notificationRepository,
        ILogger<AdminService> logger)
    {
        _identityService = identityService;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task<PaginatedList<AdminUserDto>> GetUsersAsync(int pageNumber, int pageSize, string? searchQuery = null, string? sortBy = null, string? currentUserId = null)
    {
        _logger.LogInformation("Admin user listing requested by {UserId}. Page: {Page}, PageSize: {PageSize}, Sort: {Sort}", currentUserId ?? "Unknown", pageNumber, pageSize, sortBy);

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            _logger.LogDebug("User listing search query: {Search}", searchQuery);
        }

        var paginatedUsers = await _identityService.GetUsersAsync(pageNumber, pageSize, searchQuery, sortBy);
        var rolesMap = await _identityService.GetRolesForUsersAsync(paginatedUsers.Items);

        var userDtos = paginatedUsers.Items
            .Select(user => MapToAdminUserDto(user, rolesMap))
            .ToList();

        return new PaginatedList<AdminUserDto>(userDtos, paginatedUsers.TotalCount, paginatedUsers.PageIndex, pageSize);
    }

    public async Task<Result> CreateUserAsync(AdminCreateUserDto request, string currentUserId)
    {
        _logger.LogInformation("Admin user creation requested by {AdminId} for email {Email}", currentUserId, request.Email);

        var existingUser = await _identityService.GetUserByEmailAsync(request.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("User creation failed. Email {Email} already exists.", request.Email);
            return Result.Failure(new[] { "User with this email already exists." }, "Conflict");
        }

        var (createResult, newUserId) = await _identityService.CreateUserAsync(request.Email, request.Password);
        if (!createResult.Succeeded)
        {
            _logger.LogError("Failed to create user {Email}: {Errors}", request.Email, string.Join(", ", createResult.Errors));
            return createResult;
        }

        foreach (var role in request.Roles)
        {
            if (string.IsNullOrWhiteSpace(role)) continue;

            try
            {
                await _identityService.EnsureRoleAsync(role);
                var roleResult = await _identityService.AddToRoleAsync(newUserId, role);
                if (!roleResult.Succeeded)
                {
                    _logger.LogWarning("Failed to add user {UserId} to role {Role}: {Errors}", newUserId, role, string.Join(", ", roleResult.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while ensuring/adding role {Role} for user {UserId}", role, newUserId);
            }
        }

        _logger.LogInformation("Successfully created user {UserId} with email {Email}", newUserId, request.Email);
        return Result.Success();
    }

    private static AdminUserDto MapToAdminUserDto(ApplicationUser user, IDictionary<string, IList<string>> rolesMap)
    {
        var roles = rolesMap.TryGetValue(user.Id, out var userRoles) ? userRoles : new List<string>();
        return new AdminUserDto(
            user.Id,
            user.Email ?? "No Email",
            roles
        );
    }

    public async Task<Result> DeleteUserAsync(string targetUserId, string currentUserId)
    {
        if (targetUserId == currentUserId)
        {
            _logger.LogWarning("Self-deletion attempted by user {UserId}", currentUserId);
            return Result.Failure(new[] { "Security Violation: You cannot delete your own account." }, "Forbidden");
        }

        _logger.LogInformation("Admin user deletion requested for user {TargetUserId} by admin {AdminId}", targetUserId, currentUserId);

        var result = await _identityService.DeleteUserAsync(targetUserId);
        if (result.Succeeded)
        {
            _logger.LogInformation("Successfully deleted user {TargetUserId}", targetUserId);
            return Result.Success();
        }

        _logger.LogError("Failed to delete user {TargetUserId}: {Errors}", targetUserId, string.Join(", ", result.Errors));

        // Check if error indicates "Not Found"
        if (result.Errors.Any(e => e.Contains("User not found", StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure(new[] { "User not found." }, "NotFound");
        }

        // Sanitize errors for external consumption
        return Result.Failure(new[] { "Operation failed. Please try again later." }, "Internal");
    }

    public async Task<AdminStatsDto> GetSystemStatsAsync()
    {
        _logger.LogInformation("Admin stats requested.");

        var usersCount = await _identityService.CountAsync();
        var notificationsCount = await _notificationRepository.CountAsync();

        return new AdminStatsDto(usersCount, notificationsCount);
    }
}
