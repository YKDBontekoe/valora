using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.Services;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.UnitTests.Services;

public class AdminServiceTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<INotificationRepository> _notificationRepositoryMock = new();
    private readonly Mock<IBatchJobRepository> _batchJobRepositoryMock = new();
    private readonly Mock<ILogger<AdminService>> _loggerMock = new();

    private AdminService CreateService()
    {
        return new AdminService(
            _identityServiceMock.Object,
            _notificationRepositoryMock.Object,
            _batchJobRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFailure_WhenTargetIsCurrentUser()
    {
        // Arrange
        var service = CreateService();
        var currentUserId = "user-123";
        var targetUserId = "user-123";

        // Act
        var result = await service.DeleteUserAsync(targetUserId, currentUserId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Security Violation: You cannot delete your own account.", result.Errors);
        Assert.Equal("Forbidden", result.ErrorCode);
        _identityServiceMock.Verify(x => x.DeleteUserAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsSuccess_WhenTargetIsDifferentUser()
    {
        // Arrange
        var service = CreateService();
        var currentUserId = "admin-123";
        var targetUserId = "user-456";

        _identityServiceMock
            .Setup(x => x.DeleteUserAsync(targetUserId))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await service.DeleteUserAsync(targetUserId, currentUserId);

        // Assert
        Assert.True(result.Succeeded);
        _identityServiceMock.Verify(x => x.DeleteUserAsync(targetUserId), Times.Once);
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsPaginatedList()
    {
        // Arrange
        var service = CreateService();
        var users = new List<ApplicationUser>
        {
            new ApplicationUser { Id = "1", Email = "test1@example.com" },
            new ApplicationUser { Id = "2", Email = "test2@example.com" }
        };
        var paginatedUsers = new PaginatedList<ApplicationUser>(users, 2, 1, 10);

        _identityServiceMock
            .Setup(x => x.GetUsersAsync(1, 10, null, null))
            .ReturnsAsync(paginatedUsers);

        _identityServiceMock
            .Setup(x => x.GetRolesForUsersAsync(It.IsAny<IEnumerable<ApplicationUser>>()))
            .ReturnsAsync(new Dictionary<string, IList<string>>());

        // Act
        var result = await service.GetUsersAsync(1, 10, null, null, "admin-user");

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("1", result.Items[0].Id);
        Assert.Equal("2", result.Items[1].Id);
    }

    [Fact]
    public async Task GetUsersAsync_PassesSearchAndSortParams()
    {
        // Arrange
        var service = CreateService();
        var users = new List<ApplicationUser>();
        var paginatedUsers = new PaginatedList<ApplicationUser>(users, 0, 1, 10);

        _identityServiceMock
            .Setup(x => x.GetUsersAsync(1, 10, "test", "email_asc"))
            .ReturnsAsync(paginatedUsers);

        _identityServiceMock
            .Setup(x => x.GetRolesForUsersAsync(It.IsAny<IEnumerable<ApplicationUser>>()))
            .ReturnsAsync(new Dictionary<string, IList<string>>());

        // Act
        await service.GetUsersAsync(1, 10, "test", "email_asc", "admin-id");

        // Assert
        _identityServiceMock.Verify(x => x.GetUsersAsync(1, 10, "test", "email_asc"), Times.Once);
    }

    [Fact]
    public async Task GetSystemStatsAsync_ReturnsCounts()
    {
        // Arrange
        var service = CreateService();

        _identityServiceMock.Setup(x => x.CountAsync()).ReturnsAsync(10);
        _notificationRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(5);

        // Act
        var result = await service.GetSystemStatsAsync();

        // Assert
        Assert.Equal(10, result.TotalUsers);
        Assert.Equal(5, result.TotalNotifications);
    }

    [Fact]
    public async Task GetSystemStatusAsync_ReturnsConnected_WhenCanConnect()
    {
        // Arrange
        var service = CreateService();
        _identityServiceMock.Setup(x => x.CanConnectAsync()).ReturnsAsync(true);
        _batchJobRepositoryMock.Setup(x => x.GetQueueDepthAsync(It.IsAny<CancellationToken>())).ReturnsAsync(5);

        // Act
        var result = await service.GetSystemStatusAsync();

        // Assert
        Assert.Equal("Connected", result.DbConnectivity);
        Assert.Equal(5, result.QueueDepth);
        Assert.Equal("Active", result.WorkerHealth);
    }

    [Fact]
    public async Task GetSystemStatusAsync_ReturnsUnreachable_WhenCanConnectReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        _identityServiceMock.Setup(x => x.CanConnectAsync()).ReturnsAsync(false);

        // Act
        var result = await service.GetSystemStatusAsync();

        // Assert
        Assert.Equal("Disconnected", result.DbConnectivity);
        Assert.Equal("Unreachable", result.WorkerHealth);
        Assert.Equal(0, result.QueueDepth);
    }

    [Fact]
    public async Task GetSystemStatusAsync_ReturnsUnreachable_WhenCanConnectThrows()
    {
        // Arrange
        var service = CreateService();
        _identityServiceMock.Setup(x => x.CanConnectAsync()).ThrowsAsync(new Exception("DB Error"));

        // Act
        var result = await service.GetSystemStatusAsync();

        // Assert
        Assert.Equal("Disconnected", result.DbConnectivity);
        Assert.Equal("Unreachable", result.WorkerHealth);
    }

    [Fact]
    public async Task GetSystemStatusAsync_HandlesRepositoryFailure_WhenDbConnected()
    {
        // Arrange
        var service = CreateService();
        _identityServiceMock.Setup(x => x.CanConnectAsync()).ReturnsAsync(true);
        _batchJobRepositoryMock.Setup(x => x.GetQueueDepthAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Repo Error"));

        // Act
        var result = await service.GetSystemStatusAsync();

        // Assert
        Assert.Equal("Connected", result.DbConnectivity);
        // Should default to 0/Idle on repo failure, but still show connected
        Assert.Equal(0, result.QueueDepth);
        Assert.Equal("Idle", result.WorkerHealth);
    }
}
