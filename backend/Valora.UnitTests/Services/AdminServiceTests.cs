using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Application.Services;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.UnitTests.Services;

public class AdminServiceTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<INotificationRepository> _notificationRepositoryMock = new();
    private readonly Mock<ILogger<AdminService>> _loggerMock = new();

    private AdminService CreateService()
    {
        return new AdminService(
            _identityServiceMock.Object,
            _notificationRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateUserAsync_ReturnsConflict_WhenUserAlreadyExists()
    {
        // Arrange
        var service = CreateService();
        var request = new AdminCreateUserDto("test@example.com", "Password123!", new List<string> { "Admin" });
        var currentUserId = "admin-123";

        _identityServiceMock
            .Setup(x => x.GetUserByEmailAsync(request.Email))
            .ReturnsAsync(new ApplicationUser { Email = request.Email });

        // Act
        var result = await service.CreateUserAsync(request, currentUserId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("Conflict", result.ErrorCode);
    }

    [Fact]
    public async Task CreateUserAsync_ReturnsFailure_WhenCreationFails()
    {
        // Arrange
        var service = CreateService();
        var request = new AdminCreateUserDto("test@example.com", "Password123!", new List<string> { "Admin" });
        var currentUserId = "admin-123";

        _identityServiceMock
            .Setup(x => x.GetUserByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _identityServiceMock
            .Setup(x => x.CreateUserAsync(request.Email, request.Password))
            .ReturnsAsync((Result.Failure(new[] { "Creation Failed" }), string.Empty));

        // Act
        var result = await service.CreateUserAsync(request, currentUserId);

        // Assert
        Assert.False(result.Succeeded);
        // We now return a generic error
        Assert.Contains("Failed to create user.", result.Errors);
    }

    [Fact]
    public async Task CreateUserAsync_ReturnsSuccess_WhenUserCreatedAndRoleAssigned()
    {
        // Arrange
        var service = CreateService();
        var request = new AdminCreateUserDto("test@example.com", "Password123!", new List<string> { "Admin" });
        var currentUserId = "admin-123";
        var newUserId = "new-user-123";

        _identityServiceMock
            .Setup(x => x.GetUserByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _identityServiceMock
            .Setup(x => x.CreateUserAsync(request.Email, request.Password))
            .ReturnsAsync((Result.Success(), newUserId));

        _identityServiceMock
            .Setup(x => x.EnsureRoleAsync("Admin"))
            .Returns(Task.CompletedTask);

        _identityServiceMock
            .Setup(x => x.AddToRoleAsync(newUserId, "Admin"))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await service.CreateUserAsync(request, currentUserId);

        // Assert
        Assert.True(result.Succeeded);
        _identityServiceMock.Verify(x => x.CreateUserAsync(request.Email, request.Password), Times.Once);
        _identityServiceMock.Verify(x => x.EnsureRoleAsync("Admin"), Times.Once);
        _identityServiceMock.Verify(x => x.AddToRoleAsync(newUserId, "Admin"), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_ReturnsFailure_WhenRoleIsInvalid()
    {
         // Arrange
        var service = CreateService();
        var request = new AdminCreateUserDto("test@example.com", "Password123!", new List<string> { "SuperAdmin" }); // Invalid Role
        var currentUserId = "admin-123";

        // Act
        var result = await service.CreateUserAsync(request, currentUserId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("BadRequest", result.ErrorCode);
        Assert.Contains("Invalid role assignment.", result.Errors);
        _identityServiceMock.Verify(x => x.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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
}
