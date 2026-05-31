using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Valora.IntegrationTests.Infrastructure;

public class UserAiProfileRepositoryTests : BaseTestcontainersIntegrationTest
{
    private readonly IUserAiProfileRepository _repository;

    public UserAiProfileRepositoryTests(TestcontainersDatabaseFixture fixture) : base(fixture)
    {
        _repository = new UserAiProfileRepository(DbContext);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistProfileToDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var profile = new UserAiProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Preferences = "Test Preferences",
            HouseholdProfile = "Test Profile",
            IsEnabled = true,
            IsSessionOnlyMode = false,
            Version = 1,
            DisallowedSuggestions = new List<string> { "Test" }
        };

        // Act
        await _repository.AddAsync(profile, CancellationToken.None);

        // Assert - Verify via DbContext directly to check actual side-effects
        var savedProfile = await DbContext.UserAiProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        Assert.NotNull(savedProfile);
        Assert.Equal("Test Preferences", savedProfile.Preferences);
        Assert.Equal("Test Profile", savedProfile.HouseholdProfile);
        Assert.Single(savedProfile.DisallowedSuggestions);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnProfile_WhenExists()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var profile = new UserAiProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Preferences = "Test Pref"
        };
        DbContext.UserAiProfiles.Add(profile);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAsync(userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(profile.Id, result.Id);
        Assert.Equal("Test Pref", result.Preferences);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByUserIdAsync(Guid.NewGuid().ToString(), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateProfileInDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var profile = new UserAiProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Preferences = "Old Pref"
        };
        DbContext.UserAiProfiles.Add(profile);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        // Act
        var toUpdate = new UserAiProfile
        {
            Id = profile.Id,
            UserId = profile.UserId,
            Preferences = "New Pref",
            HouseholdProfile = "New House"
        };
        await _repository.UpdateAsync(toUpdate, CancellationToken.None);

        // Assert
        var updatedProfile = await DbContext.UserAiProfiles.FirstOrDefaultAsync(p => p.Id == profile.Id);
        Assert.NotNull(updatedProfile);
        Assert.Equal("New Pref", updatedProfile.Preferences);
        Assert.Equal("New House", updatedProfile.HouseholdProfile);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveProfileFromDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var profile = new UserAiProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Preferences = "To Delete"
        };
        DbContext.UserAiProfiles.Add(profile);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        // Act
        var toDelete = new UserAiProfile { Id = profile.Id, UserId = profile.UserId };
        await _repository.DeleteAsync(toDelete, CancellationToken.None);

        // Assert
        var deletedProfile = await DbContext.UserAiProfiles.FirstOrDefaultAsync(p => p.Id == profile.Id);
        Assert.Null(deletedProfile);
    }
}
