using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class UserAiProfileRepositoryTests : BaseTestcontainersIntegrationTest
{
    public UserAiProfileRepositoryTests(TestcontainersDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task AddAsync_ShouldAddProfile()
    {
        // Arrange
        var repository = GetRequiredService<IUserAiProfileRepository>();
        var userId = $"user_{Guid.NewGuid()}";

        var profile = new UserAiProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Preferences = "Quiet neighborhood",
            HouseholdProfile = "Family of 4",
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        await repository.AddAsync(profile, CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var dbProfile = await DbContext.UserAiProfiles.FirstOrDefaultAsync(p => p.Id == profile.Id);

        Assert.NotNull(dbProfile);
        Assert.Equal(userId, dbProfile.UserId);
        Assert.Equal("Quiet neighborhood", dbProfile.Preferences);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnProfile_WhenExists()
    {
        // Arrange
        var repository = GetRequiredService<IUserAiProfileRepository>();
        var userId = $"user_{Guid.NewGuid()}";

        var profile = new UserAiProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Preferences = "Strict Analyst",
            HouseholdProfile = "Single",
            UpdatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(profile, CancellationToken.None);

        // Act
        var result = await repository.GetByUserIdAsync(userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(profile.Id, result.Id);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("Strict Analyst", result.Preferences);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateProfile()
    {
        // Arrange
        var repository = GetRequiredService<IUserAiProfileRepository>();
        var userId = $"user_{Guid.NewGuid()}";

        var profile = new UserAiProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Preferences = "Original Preferences",
            HouseholdProfile = "Original Profile",
            UpdatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(profile, CancellationToken.None);

        DbContext.ChangeTracker.Clear();
        var toUpdate = await repository.GetByUserIdAsync(userId, CancellationToken.None);
        Assert.NotNull(toUpdate);

        toUpdate.Preferences = "Updated Preferences";
        toUpdate.UpdatedAt = DateTime.UtcNow.AddMinutes(5);

        // Act
        await repository.UpdateAsync(toUpdate, CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var result = await DbContext.UserAiProfiles.FirstOrDefaultAsync(p => p.Id == profile.Id);

        Assert.NotNull(result);
        Assert.Equal("Updated Preferences", result.Preferences);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteProfile()
    {
        // Arrange
        var repository = GetRequiredService<IUserAiProfileRepository>();
        var userId = $"user_{Guid.NewGuid()}";

        var profile = new UserAiProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Preferences = "To Be Deleted",
            HouseholdProfile = "Delete me",
            UpdatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(profile, CancellationToken.None);

        DbContext.ChangeTracker.Clear();
        var toDelete = await repository.GetByUserIdAsync(userId, CancellationToken.None);
        Assert.NotNull(toDelete);

        // Act
        await repository.DeleteAsync(toDelete, CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var result = await DbContext.UserAiProfiles.FirstOrDefaultAsync(p => p.Id == profile.Id);

        Assert.Null(result);
    }
}
