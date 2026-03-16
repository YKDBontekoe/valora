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
    private readonly INeighborhoodRepository _neighborhoodRepository;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        IIdentityService identityService,
        INotificationRepository notificationRepository,
        INeighborhoodRepository neighborhoodRepository,
        ILogger<AdminService> logger)
    {
        _identityService = identityService;
        _notificationRepository = notificationRepository;
        _neighborhoodRepository = neighborhoodRepository;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of users for the administrative dashboard.
    /// </summary>
    /// <param name="pageNumber">The index of the page to retrieve (1-based).</param>
    /// <param name="pageSize">The number of records per page.</param>
    /// <param name="searchQuery">An optional search term to filter users by email or name.</param>
    /// <param name="sortBy">An optional string dictating the sorting property and direction (e.g., 'Email', 'CreatedAtDesc').</param>
    /// <param name="currentUserId">The ID of the admin requesting the list, for audit logging.</param>
    /// <remarks>
    /// <para>
    /// <strong>Why Pagination?</strong> Admin views can query tables with tens of thousands of users.
    /// Returning the full dataset at once causes excessive memory allocation (OOM errors)
    /// and degrades frontend performance. Paginated lists keep server memory usage flat and predictable.
    /// </para>
    /// <para>
    /// <strong>Audit Logging:</strong> It's crucial to log access to sensitive user data.
    /// That's why the <paramref name="currentUserId"/> and <paramref name="searchQuery"/> are recorded:
    /// to trace potential abuse (e.g., if an admin searches for specific user accounts excessively).
    /// </para>
    /// </remarks>
    /// <returns>A paginated subset of user data.</returns>
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

    public async Task<List<DatasetStatusDto>> GetDatasetStatusAsync(CancellationToken cancellationToken = default)
    {
        return await _neighborhoodRepository.GetDatasetStatusAsync(cancellationToken);
    }
}
