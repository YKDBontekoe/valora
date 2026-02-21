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
    private readonly IBatchJobRepository _batchJobRepository;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        IIdentityService identityService,
        INotificationRepository notificationRepository,
        IBatchJobRepository batchJobRepository,
        ILogger<AdminService> logger)
    {
        _identityService = identityService;
        _notificationRepository = notificationRepository;
        _batchJobRepository = batchJobRepository;
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

    public async Task<SystemStatusDto> GetSystemStatusAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("System status requested.");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        bool dbConnected = false;
        try
        {
             dbConnected = await _identityService.CanConnectAsync();
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Database connectivity check failed.");
        }
        sw.Stop();
        double latency = sw.Elapsed.TotalMilliseconds;

        int queueDepth = 0;
        DateTime? lastIngestion = null;

        if (dbConnected)
        {
            try
            {
                queueDepth = await _batchJobRepository.GetQueueDepthAsync(cancellationToken);
                lastIngestion = await _batchJobRepository.GetLastIngestionRunAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve batch job metrics despite DB being connected.");
                // We proceed with defaults (0, null)
            }
        }

        string workerHealth;
        if (!dbConnected)
        {
             workerHealth = "Unreachable";
        }
        else if (queueDepth > 0)
        {
             workerHealth = "Active";
        }
        else
        {
             workerHealth = "Idle";
        }

        return new SystemStatusDto(
            latency,
            queueDepth,
            workerHealth,
            dbConnected ? "Connected" : "Disconnected",
            lastIngestion
        );
    }
}
