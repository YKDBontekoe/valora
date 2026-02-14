using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Valora.Domain.Entities;
using Valora.Infrastructure.Services;
using Xunit;

namespace Valora.UnitTests.Services;

public class IdentityServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManager;
    private readonly Mock<RoleManager<IdentityRole>> _roleManager;
    private readonly Mock<ILogger<IdentityService>> _logger;
    private readonly IdentityService _service;

    public IdentityServiceTests()
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _userManager = new Mock<UserManager<ApplicationUser>>(userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var roleStore = new Mock<IRoleStore<IdentityRole>>();
        _roleManager = new Mock<RoleManager<IdentityRole>>(roleStore.Object, null!, null!, null!, null!);

        _logger = new Mock<ILogger<IdentityService>>();

        _service = new IdentityService(_userManager.Object, _roleManager.Object, _logger.Object);
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldReturnSuccess_WhenUserExists()
    {
        // Arrange
        var userId = "user123";
        var user = new ApplicationUser { Id = userId };
        _userManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.UpdateProfileAsync(userId, "John", "Doe", 500, true);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal(500, user.DefaultRadiusMeters);
        Assert.True(user.BiometricsEnabled);
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldReturnFailure_WhenUserDoesNotExist()
    {
        // Arrange
        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null!);

        // Act
        var result = await _service.UpdateProfileAsync("none", "John", "Doe", 500, true);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User not found.", result.Errors);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldReturnSuccess_WhenCurrentPasswordIsCorrect()
    {
        // Arrange
        var userId = "user123";
        var user = new ApplicationUser { Id = userId };
        _userManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManager.Setup(x => x.ChangePasswordAsync(user, "old", "new")).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.ChangePasswordAsync(userId, "old", "new");

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldReturnFailure_WhenUserDoesNotExist()
    {
        // Arrange
        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null!);

        // Act
        var result = await _service.ChangePasswordAsync("none", "old", "new");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User not found.", result.Errors);
    }
}
