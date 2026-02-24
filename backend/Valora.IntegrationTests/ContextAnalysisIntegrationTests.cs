using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Services;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class ContextAnalysisIntegrationTests : IAsyncLifetime
{
    private readonly TestcontainersDatabaseFixture _fixture;
    private readonly Mock<IAiService> _mockAiService = new();
    private readonly Mock<ICurrentUserService> _mockCurrentUserService = new();
    private IServiceScope? _scope;
    private IContextAnalysisService? _sut;
    private ValoraDbContext? _dbContext;

    public ContextAnalysisIntegrationTests(TestcontainersDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Ensure clean slate for DB
        var factory = _fixture.Factory!.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAiService>();
                services.AddSingleton(_mockAiService.Object);
                services.RemoveAll<ICurrentUserService>();
                services.AddSingleton(_mockCurrentUserService.Object);
            });
        });

        _scope = factory.Services.CreateScope();
        _sut = _scope.ServiceProvider.GetRequiredService<IContextAnalysisService>();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        // Clean up users and profiles - Order matters for FK constraints
        _dbContext.ListingComments.RemoveRange(_dbContext.ListingComments);
        _dbContext.SavedListings.RemoveRange(_dbContext.SavedListings);
        _dbContext.ActivityLogs.RemoveRange(_dbContext.ActivityLogs);
        _dbContext.WorkspaceMembers.RemoveRange(_dbContext.WorkspaceMembers);
        _dbContext.Workspaces.RemoveRange(_dbContext.Workspaces);

        _dbContext.UserAiProfiles.RemoveRange(_dbContext.UserAiProfiles);
        _dbContext.Users.RemoveRange(_dbContext.Users);
        await _dbContext.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _scope?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ChatAsync_UsesBasePrompt_WhenNoProfileExists()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.UserId).Returns((string?)null);

        string caughtSystemPrompt = "";
        _mockAiService.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((user, system, intent, token) => caughtSystemPrompt = system)
            .ReturnsAsync("AI Response");

        // Act
        await _sut!.ChatAsync("Hello", "chat", CancellationToken.None);

        // Assert
        Assert.Contains("You are Valora", caughtSystemPrompt);
        Assert.DoesNotContain("User Personalization Profile", caughtSystemPrompt);
    }

    [Fact]
    public async Task ChatAsync_AugmentsPrompt_WhenProfileExists()
    {
        // Arrange
        var userId = "user-123";
        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);

        // Create the user first to satisfy FK constraint
        var user = new ApplicationUser { Id = userId, UserName = "testuser", Email = "test@example.com" };
        _dbContext!.Users.Add(user);

        var profile = new UserAiProfile
        {
            UserId = userId,
            Preferences = "I prefer quiet neighborhoods.",
            HouseholdProfile = "Couple with a dog",
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.UserAiProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        string caughtSystemPrompt = "";
        _mockAiService.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((user, system, intent, token) => caughtSystemPrompt = system)
            .ReturnsAsync("AI Response");

        // Act
        await _sut!.ChatAsync("Find me a home", "chat", CancellationToken.None);

        // Assert
        Assert.Contains("User Personalization Profile", caughtSystemPrompt);
        Assert.Contains("I prefer quiet neighborhoods", caughtSystemPrompt);
        Assert.Contains("Couple with a dog", caughtSystemPrompt);
    }

    [Fact]
    public async Task AnalyzeReportAsync_AugmentsPrompt_WhenProfileExists()
    {
        // Arrange
        var userId = "user-456";
        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);

        // Create the user first to satisfy FK constraint
        var user = new ApplicationUser { Id = userId, UserName = "testuser2", Email = "test2@example.com" };
        _dbContext!.Users.Add(user);

        var profile = new UserAiProfile
        {
            UserId = userId,
            Preferences = "Avoid busy streets.",
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.UserAiProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        var report = new ContextReportDto(
            new ResolvedLocationDto("Query", "Address", 0, 0, 0, 0, "M", "M", "D", "D", "N", "N", "P"),
            new List<ContextMetricDto>(), // Social
            new List<ContextMetricDto>(), // Crime
            new List<ContextMetricDto>(), // Demographics
            new List<ContextMetricDto>(), // Housing
            new List<ContextMetricDto>(), // Mobility
            new List<ContextMetricDto>(), // Amenity
            new List<ContextMetricDto>(), // Environment
            8.5, // CompositeScore
            new Dictionary<string, double>(), // CategoryScores
            new List<SourceAttributionDto>(), // Sources
            new List<string>() // Warnings
        );

        string caughtSystemPrompt = "";
        _mockAiService.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((user, system, intent, token) => caughtSystemPrompt = system)
            .ReturnsAsync("Analysis Result");

        // Act
        await _sut!.AnalyzeReportAsync(report, CancellationToken.None);

        // Assert
        Assert.Contains("User Personalization Profile", caughtSystemPrompt);
        Assert.Contains("Avoid busy streets", caughtSystemPrompt);
    }

    [Fact]
    public async Task ChatAsync_DoesNotAugmentPrompt_WhenProfileIsDisabled()
    {
        // Arrange
        var userId = "user-disabled";
        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);

        // Create the user first to satisfy FK constraint
        var user = new ApplicationUser { Id = userId, UserName = "testuser3", Email = "test3@example.com" };
        _dbContext!.Users.Add(user);

        var profile = new UserAiProfile
        {
            UserId = userId,
            Preferences = "I like parks.",
            IsEnabled = false, // DISABLED
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.UserAiProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        string caughtSystemPrompt = "";
        _mockAiService.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((user, system, intent, token) => caughtSystemPrompt = system)
            .ReturnsAsync("AI Response");

        // Act
        await _sut!.ChatAsync("Hello", "chat", CancellationToken.None);

        // Assert
        Assert.DoesNotContain("User Personalization Profile", caughtSystemPrompt);
        Assert.DoesNotContain("I like parks", caughtSystemPrompt);
    }
}
