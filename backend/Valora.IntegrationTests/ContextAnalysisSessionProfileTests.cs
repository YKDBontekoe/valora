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
public class ContextAnalysisSessionProfileTests : IAsyncLifetime
{
    private readonly TestcontainersDatabaseFixture _fixture;
    private readonly Mock<IAiService> _mockAiService = new();
    private readonly Mock<ICurrentUserService> _mockCurrentUserService = new();
    private IServiceScope? _scope;
    private IContextAnalysisService? _sut;
    private ValoraDbContext? _dbContext;

    public ContextAnalysisSessionProfileTests(TestcontainersDatabaseFixture fixture)
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
    public async Task ChatAsync_PrioritizesSessionProfile_OverStoredProfile()
    {
        // Arrange
        var userId = "user-override";
        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);

        // Create the user and a stored profile
        var user = new ApplicationUser { Id = userId, UserName = "overrideuser", Email = "override@example.com" };
        _dbContext!.Users.Add(user);

        var storedProfile = new UserAiProfile
        {
            UserId = userId,
            Preferences = "Stored preferences (should be ignored)",
            HouseholdProfile = "Stored household (should be ignored)",
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.UserAiProfiles.Add(storedProfile);
        await _dbContext.SaveChangesAsync();

        // Define a session profile override
        var sessionProfile = new UserAiProfileDto
        {
            Preferences = "Session preferences (should be used)",
            HouseholdProfile = "Session household (should be used)",
            IsEnabled = true,
            DisallowedSuggestions = new List<string>()
        };

        string caughtSystemPrompt = "";
        _mockAiService.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((userMsg, system, intent, token) => caughtSystemPrompt = system)
            .ReturnsAsync("AI Response");

        // Act
        await _sut!.ChatAsync("Hello", "chat", CancellationToken.None, sessionProfile);

        // Assert
        Assert.Contains("Session preferences (should be used)", caughtSystemPrompt);
        Assert.Contains("Session household (should be used)", caughtSystemPrompt);
        Assert.DoesNotContain("Stored preferences (should be ignored)", caughtSystemPrompt);
        Assert.DoesNotContain("Stored household (should be ignored)", caughtSystemPrompt);
    }

    [Fact]
    public async Task ChatAsync_IncludesDisallowedSuggestions_FromSessionProfile()
    {
        // Arrange
        var sessionProfile = new UserAiProfileDto
        {
            Preferences = "General preferences",
            IsEnabled = true,
            DisallowedSuggestions = new List<string> { "No noisy areas", "Avoid high crime" }
        };

        string caughtSystemPrompt = "";
        _mockAiService.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((userMsg, system, intent, token) => caughtSystemPrompt = system)
            .ReturnsAsync("AI Response");

        // Act
        await _sut!.ChatAsync("Suggest a place", "chat", CancellationToken.None, sessionProfile);

        // Assert
        Assert.Contains("Disallowed Suggestions (Do NOT suggest these):", caughtSystemPrompt);
        Assert.Contains("- No noisy areas", caughtSystemPrompt);
        Assert.Contains("- Avoid high crime", caughtSystemPrompt);
    }

    [Fact]
    public async Task AnalyzeReportAsync_UsesSessionProfile_WhenProvided()
    {
        // Arrange
        var sessionProfile = new UserAiProfileDto
        {
            Preferences = "Analyze carefully",
            IsEnabled = true
        };

        var report = new ContextReportDto(
            new ResolvedLocationDto("Query", "Address", 0, 0, 0, 0, "M", "M", "D", "D", "N", "N", "P"),
            new List<ContextMetricDto>(),
            new List<ContextMetricDto>(),
            new List<ContextMetricDto>(),
            new List<ContextMetricDto>(),
            new List<ContextMetricDto>(),
            new List<ContextMetricDto>(),
            new List<ContextMetricDto>(),
            8.5,
            new Dictionary<string, double>(),
            new List<SourceAttributionDto>(),
            new List<string>()
        );

        string caughtSystemPrompt = "";
        _mockAiService.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((userMsg, system, intent, token) => caughtSystemPrompt = system)
            .ReturnsAsync("Analysis Result");

        // Act
        await _sut!.AnalyzeReportAsync(report, CancellationToken.None, sessionProfile);

        // Assert
        Assert.Contains("User Personalization Profile", caughtSystemPrompt);
        Assert.Contains("Analyze carefully", caughtSystemPrompt);
    }
}
