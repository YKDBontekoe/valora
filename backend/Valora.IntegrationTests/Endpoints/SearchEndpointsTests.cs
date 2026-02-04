using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Scraping;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;

namespace Valora.IntegrationTests.Endpoints;

public class SearchEndpointsTests : IDisposable
{
    private readonly IntegrationTestWebAppFactory _factory;
    private readonly HttpClient _client;
    private readonly Mock<IFundaSearchService> _mockSearchService = new();

    public SearchEndpointsTests()
    {
        _factory = new IntegrationTestWebAppFactory("InMemory");

        // Mock IFundaSearchService
        var clientFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => _mockSearchService.Object);
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
        var registerDto = new RegisterDto { Email = "user@example.com", Password = "Password123!", ConfirmPassword = "Password123!" };
        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        var loginDto = new LoginDto("user@example.com", "Password123!");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.Token);
    }

    [Fact]
    public async Task Search_WhenRegionMissing_ShouldReturnBadRequest()
    {
        await AuthenticateAsync();

        // Act
        // Parameter binding is case-insensitive, but explicit Empty value is key
        var response = await _client.GetAsync("/api/search?region=");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_WhenValid_ShouldReturnOk()
    {
        await AuthenticateAsync();

        // Arrange
        var expectedResult = new FundaSearchResult
        {
            Items = new List<Listing>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20,
            FromCache = true
        };

        _mockSearchService
            .Setup(x => x.SearchAsync(It.IsAny<FundaSearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        // Use lowercase parameter name to be safe
        var response = await _client.GetAsync("/api/search?region=Amsterdam");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<FundaSearchResult>();
        Assert.NotNull(content);
        Assert.Equal(0, content!.TotalCount);
    }

    [Fact]
    public async Task Lookup_WhenUrlMissing_ShouldReturnBadRequest()
    {
        await AuthenticateAsync();

        // Act
        var response = await _client.GetAsync("/api/lookup");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Lookup_WhenFound_ShouldReturnOk()
    {
        await AuthenticateAsync();

        // Arrange
        var listing = new Listing { Id = Guid.NewGuid(), FundaId = "123", Address = "Test" };
        _mockSearchService
            .Setup(x => x.GetByFundaUrlAsync("http://funda.nl/test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(listing);

        // Act
        var response = await _client.GetAsync("/api/lookup?url=http://funda.nl/test");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<Listing>();
        Assert.NotNull(content);
        Assert.Equal("123", content!.FundaId);
    }

    [Fact]
    public async Task Lookup_WhenNotFound_ShouldReturnNotFound()
    {
        await AuthenticateAsync();

        // Arrange
        _mockSearchService
            .Setup(x => x.GetByFundaUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Listing?)null);

        // Act
        var response = await _client.GetAsync("/api/lookup?url=http://funda.nl/test");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
