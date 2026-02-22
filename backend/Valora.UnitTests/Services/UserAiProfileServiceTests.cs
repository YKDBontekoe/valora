using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Application.Services;
using Valora.Domain.Entities;

namespace Valora.UnitTests.Services;

public class UserAiProfileServiceTests
{
    private readonly Mock<IUserAiProfileRepository> _repositoryMock = new();

    private UserAiProfileService CreateService()
    {
        return new UserAiProfileService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsEmptyProfile_WhenNotFound()
    {
        // Arrange
        var service = CreateService();
        _repositoryMock.Setup(r => r.GetByUserIdAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserAiProfile?)null);

        // Act
        var result = await service.GetProfileAsync("user1", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user1", result.UserId);
        Assert.Empty(result.Preferences);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsProfile_WhenFound()
    {
        // Arrange
        var service = CreateService();
        var profile = new UserAiProfile { UserId = "user1", Preferences = "Quiet" };
        _repositoryMock.Setup(r => r.GetByUserIdAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await service.GetProfileAsync("user1", CancellationToken.None);

        // Assert
        Assert.Equal("Quiet", result.Preferences);
    }

    [Fact]
    public async Task UpdateProfileAsync_CreatesNew_WhenNotExists()
    {
        // Arrange
        var service = CreateService();
        var dto = new UserAiProfileDto { Preferences = "New Pref" };

        _repositoryMock.Setup(r => r.GetByUserIdAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserAiProfile?)null);

        UserAiProfile? capturedProfile = null;
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<UserAiProfile>(), It.IsAny<CancellationToken>()))
            .Callback<UserAiProfile, CancellationToken>((p, ct) => capturedProfile = p);

        // Act
        await service.UpdateProfileAsync("user1", dto, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedProfile);
        Assert.Equal("user1", capturedProfile!.UserId);
        Assert.Equal("New Pref", capturedProfile.Preferences);
        Assert.Equal(1, capturedProfile.Version);
    }

    [Fact]
    public async Task UpdateProfileAsync_UpdatesExisting_AndIncrementsVersion()
    {
        // Arrange
        var service = CreateService();
        var existingProfile = new UserAiProfile { UserId = "user1", Preferences = "Old", Version = 1 };
        var dto = new UserAiProfileDto { Preferences = "New" };

        _repositoryMock.Setup(r => r.GetByUserIdAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        // Act
        await service.UpdateProfileAsync("user1", dto, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.UpdateAsync(It.Is<UserAiProfile>(p =>
            p.Preferences == "New" &&
            p.Version == 2), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task DeleteProfileAsync_Deletes_WhenFound()
    {
        // Arrange
        var service = CreateService();
        var profile = new UserAiProfile { UserId = "user1" };
        _repositoryMock.Setup(r => r.GetByUserIdAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await service.DeleteProfileAsync("user1", CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        _repositoryMock.Verify(r => r.DeleteAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExportProfileAsync_ReturnsJson()
    {
        // Arrange
        var service = CreateService();
        var profile = new UserAiProfile { UserId = "user1", Preferences = "ExportMe" };
        _repositoryMock.Setup(r => r.GetByUserIdAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var json = await service.ExportProfileAsync("user1", CancellationToken.None);

        // Assert
        Assert.Contains("ExportMe", json);
        Assert.Contains("user1", json);
    }
}
