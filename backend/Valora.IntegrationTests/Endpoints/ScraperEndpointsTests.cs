using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Infrastructure.Persistence;

namespace Valora.IntegrationTests.Endpoints;

public class ScraperEndpointsTests : IDisposable
{
    private readonly IntegrationTestWebAppFactory _factory;
    private readonly HttpClient _client;
    private readonly Mock<IScraperJobScheduler> _mockScheduler = new();
    private readonly Mock<IListingRepository> _mockRepo = new();

    public ScraperEndpointsTests()
    {
        _factory = new IntegrationTestWebAppFactory("InMemory");

        // Use WithWebHostBuilder to inject mocks and override config
        var clientFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((ctx, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "HANGFIRE_ENABLED", "true" }
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => _mockScheduler.Object);
                // Note: IListingRepository is used in Seed. We can mock it or use real one.
                // If we mock it, we control CountAsync.
                services.AddScoped(_ => _mockRepo.Object);
            });
        });

        _client = clientFactory.CreateClient();
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private async Task AuthenticateAsync()
    {
        // Register and login to get token
        var registerDto = new RegisterDto { Email = "admin@example.com", Password = "Password123!", ConfirmPassword = "Password123!" };
        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        var loginDto = new LoginDto("admin@example.com", "Password123!");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.Token);
    }

    [Fact]
    public async Task TriggerScraper_WhenHangfireEnabled_ShouldQueueJob()
    {
        await AuthenticateAsync();

        // Act
        var response = await _client.PostAsync("/api/scraper/trigger", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _mockScheduler.Verify(x => x.EnqueueScraper(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TriggerLimitedScraper_WhenHangfireEnabled_ShouldQueueJob()
    {
        await AuthenticateAsync();

        // Act
        var response = await _client.PostAsync("/api/scraper/trigger-limited?region=amsterdam&limit=10", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _mockScheduler.Verify(x => x.EnqueueLimitedScraper("amsterdam", 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Seed_WhenDataExists_ShouldSkip()
    {
        await AuthenticateAsync();

        _mockRepo.Setup(x => x.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var response = await _client.PostAsync("/api/scraper/seed?region=amsterdam", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<SeedResponse>();
        Assert.True(content!.Skipped);
        _mockScheduler.Verify(x => x.EnqueueSeed(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Seed_WhenDataEmpty_ShouldQueueJob()
    {
        await AuthenticateAsync();

        _mockRepo.Setup(x => x.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        // Act
        var response = await _client.PostAsync("/api/scraper/seed?region=amsterdam", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<SeedResponse>();
        Assert.False(content!.Skipped);
        _mockScheduler.Verify(x => x.EnqueueSeed("amsterdam", It.IsAny<CancellationToken>()), Times.Once);
    }

    record SeedResponse(string Message, bool Skipped);
}
