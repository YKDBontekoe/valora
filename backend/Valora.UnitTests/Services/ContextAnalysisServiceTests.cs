using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Services;

namespace Valora.UnitTests.Services;

public class ContextAnalysisServiceTests
{
    private readonly Mock<IAiService> _aiServiceMock = new();
    private readonly Mock<IUserAiProfileService> _profileServiceMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

    private ContextAnalysisService CreateService()
    {
        return new ContextAnalysisService(_aiServiceMock.Object, _profileServiceMock.Object, _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task AnalyzeReportAsync_GeneratesCorrectPrompt_WithFullData()
    {
        // Arrange
        var service = CreateService();
        var report = CreateFullReportDto();

        string capturedPrompt = "";
        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), "detailed_analysis", It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((prompt, sys, model, ct) => capturedPrompt = prompt)
            .ReturnsAsync("Analysis Result");

        // Act
        await service.AnalyzeReportAsync(report, CancellationToken.None);

        // Assert
        Assert.Contains("<context_report>", capturedPrompt);
        Assert.Contains("<address>Damrak 1, Amsterdam</address>", capturedPrompt);
        Assert.Contains("<composite_score>78</composite_score>", capturedPrompt);

        // Check Categories
        Assert.Contains("<score category=\"Social\">80</score>", capturedPrompt);
        Assert.Contains("<score category=\"Safety\">75</score>", capturedPrompt);

        // Check Metrics
        Assert.Contains("<metric category=\"Social\" label=\"Restaurants\">15 count (Score: 85)</metric>", capturedPrompt);
        Assert.Contains("<metric category=\"Safety\" label=\"Crime Rate\">100 risk (Score: 90)</metric>", capturedPrompt);
    }

    [Fact]
    public async Task AnalyzeReportAsync_GeneratesCorrectPrompt_WithMinimalData()
    {
        // Arrange
        var service = CreateService();
        var report = new ContextReportDto(
            Location: new ResolvedLocationDto("q", "Address", 52.0, 4.0, null, null, "Muni", "MuniName", "Dist", "DistName", "Neigh", "NeighName", "1234AB"),
            SocialMetrics: new List<ContextMetricDto>(),
            CrimeMetrics: new List<ContextMetricDto>(),
            DemographicsMetrics: new List<ContextMetricDto>(),
            HousingMetrics: new List<ContextMetricDto>(),
            MobilityMetrics: new List<ContextMetricDto>(),
            AmenityMetrics: new List<ContextMetricDto>(),
            EnvironmentMetrics: new List<ContextMetricDto>(),
            CompositeScore: 50,
            CategoryScores: new Dictionary<string, double>(),
            Sources: new List<SourceAttributionDto>(),
            Warnings: new List<string>()
        );

        string capturedPrompt = "";
        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), "detailed_analysis", It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((prompt, sys, model, ct) => capturedPrompt = prompt)
            .ReturnsAsync("Analysis Result");

        // Act
        await service.AnalyzeReportAsync(report, CancellationToken.None);

        // Assert
        Assert.Contains("<context_report>", capturedPrompt);
        Assert.Contains("<address>Address</address>", capturedPrompt);
        // Should handle empty categories gracefully
        Assert.Contains("<category_scores>", capturedPrompt);
        Assert.Contains("</category_scores>", capturedPrompt);
        Assert.DoesNotContain("<score category=", capturedPrompt);
    }

    [Fact]
    public async Task AnalyzeReportAsync_SanitizesInputs()
    {
         // Arrange
        var service = CreateService();
        var report = CreateFullReportDto();

        // Inject malicious/special chars into a metric
        var maliciousMetric = new ContextMetricDto("test_key", "Bad <Script> Label & More", 5, "Unit", 10, "Source", null);
        report = report with { SocialMetrics = new List<ContextMetricDto> { maliciousMetric } };

        string capturedPrompt = "";
        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), "detailed_analysis", It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((prompt, sys, model, ct) => capturedPrompt = prompt)
            .ReturnsAsync("Analysis Result");

        // Act
        await service.AnalyzeReportAsync(report, CancellationToken.None);

        // Assert
        // < and > should be escaped to &lt; and &gt;
        Assert.Contains("label=\"Bad &lt;Script&gt; Label &amp; More\"", capturedPrompt);
        Assert.Contains(">5 Unit (Score: 10)<", capturedPrompt);
    }

    [Fact]
    public async Task ChatAsync_WithEnabledProfile_ShouldAugmentPrompt()
    {
        // Arrange
        var service = CreateService();
        var userId = "user1";
        var prompt = "Hello";
        var profile = new UserAiProfileDto
        {
            UserId = userId,
            IsEnabled = true,
            Preferences = "I like quiet areas."
        };

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _profileServiceMock.Setup(x => x.GetProfileAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        await service.ChatAsync(prompt, null, CancellationToken.None);

        // Assert
        _aiServiceMock.Verify(x => x.ChatAsync(
            prompt,
            It.Is<string>(s => s.Contains("User Personalization Profile") && s.Contains("I like quiet areas.")),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChatAsync_WithDisabledProfile_ShouldNotAugmentPrompt()
    {
        // Arrange
        var service = CreateService();
        var userId = "user1";
        var prompt = "Hello";
        var profile = new UserAiProfileDto
        {
            UserId = userId,
            IsEnabled = false,
            Preferences = "I like quiet areas."
        };

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _profileServiceMock.Setup(x => x.GetProfileAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        await service.ChatAsync(prompt, null, CancellationToken.None);

        // Assert
        _aiServiceMock.Verify(x => x.ChatAsync(
            prompt,
            It.Is<string>(s => !s.Contains("User Personalization Profile")),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChatAsync_WithSessionProfile_ShouldUseSessionProfile()
    {
        // Arrange
        var service = CreateService();
        var userId = "user1";
        var prompt = "Hello";
        var storedProfile = new UserAiProfileDto { UserId = userId, IsEnabled = false };
        var sessionProfile = new UserAiProfileDto { UserId = userId, IsEnabled = true, Preferences = "Session pref" };

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _profileServiceMock.Setup(x => x.GetProfileAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedProfile);

        // Act
        await service.ChatAsync(prompt, null, CancellationToken.None, sessionProfile);

        // Assert
        _aiServiceMock.Verify(x => x.ChatAsync(
            prompt,
            It.Is<string>(s => s.Contains("Session pref")),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ContextReportDto CreateFullReportDto()
    {
        return new ContextReportDto(
            Location: new ResolvedLocationDto("q", "Damrak 1, Amsterdam", 52.37, 4.89, null, null, "Muni", "Amsterdam", "Dist", "Centrum", "Neigh", "Oude Zijde", "1012LG"),
            SocialMetrics: new List<ContextMetricDto>
            {
                new("restaurants", "Restaurants", 15, "count", 85, "OSM", null)
            },
            CrimeMetrics: new List<ContextMetricDto>
            {
                new("crime_rate", "Crime Rate", 100, "risk", 90, "Police", null)
            },
            DemographicsMetrics: new List<ContextMetricDto>(),
            HousingMetrics: new List<ContextMetricDto>(),
            MobilityMetrics: new List<ContextMetricDto>(),
            AmenityMetrics: new List<ContextMetricDto>(),
            EnvironmentMetrics: new List<ContextMetricDto>(),
            CompositeScore: 78,
            CategoryScores: new Dictionary<string, double>
            {
                { "Social", 80 },
                { "Safety", 75 }
            },
            Sources: new List<SourceAttributionDto>(),
            Warnings: new List<string>()
        );
    }
}
