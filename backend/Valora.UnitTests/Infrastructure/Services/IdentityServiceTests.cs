using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Services;
using Xunit;

namespace Valora.UnitTests.Infrastructure.Services;

public class IdentityServiceTests : IDisposable
{
    private readonly ValoraDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly IdentityService _identityService;
    private readonly ApplicationUser _testUser;

    public IdentityServiceTests()
    {
        // Setup SQLite In-Memory Database for relational transaction testing
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new ValoraDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        // Seed Test User
        _testUser = new ApplicationUser { Id = "user-1", UserName = "test", Email = "test@example.com" };
        _context.Users.Add(_testUser);
        _context.SaveChanges();

        // Setup Mock UserManager
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // Make FindByIdAsync return the user
        _mockUserManager.Setup(x => x.FindByIdAsync(_testUser.Id))
            .ReturnsAsync(_testUser);

        // Setup SUT with mocked managers but real context
        var roleStore = new Mock<IRoleStore<IdentityRole>>();
        var roleManager = new Mock<RoleManager<IdentityRole>>(
            roleStore.Object, null!, null!, null!, null!);

        _identityService = new IdentityService(
            _mockUserManager.Object,
            roleManager.Object,
            _context);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldRollbackTransaction_WhenDeleteFails()
    {
        // Arrange
        Assert.True(_context.Database.IsRelational(), "Test requires Relational database provider");

        // Simulate UserManager failure
        _mockUserManager.Setup(x => x.DeleteAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Delete failed" }));

        // Add dependent data that would be deleted by IdentityService BEFORE UserManager is called
        var notification = new Notification
        {
            UserId = _testUser.Id,
            Title = "Should Remain",
            Body = "Body",
            Type = NotificationType.Info
        };
        _context.Notifications.Add(notification);
        _context.SaveChanges();

        // Act
        var result = await _identityService.DeleteUserAsync(_testUser.Id);

        // Assert
        Assert.False(result.Succeeded);

        // Verify Rollback: Notification should still exist because the transaction rolled back
        var notificationInDb = await _context.Notifications.FindAsync(notification.Id);
        Assert.NotNull(notificationInDb);
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldRollbackTransaction_WhenExceptionThrown()
    {
        // Arrange
        // Simulate UserManager throwing exception
        _mockUserManager.Setup(x => x.DeleteAsync(It.IsAny<ApplicationUser>()))
            .ThrowsAsync(new Exception("Unexpected database error"));

        // Add dependent data
        var notification = new Notification
        {
            UserId = _testUser.Id,
            Title = "Should Remain",
            Body = "Body",
            Type = NotificationType.Info
        };
        _context.Notifications.Add(notification);
        _context.SaveChanges();

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _identityService.DeleteUserAsync(_testUser.Id));

        // Verify Rollback
        var notificationInDb = await _context.Notifications.FindAsync(notification.Id);
        Assert.NotNull(notificationInDb);
    }
}
