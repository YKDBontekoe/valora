using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;

namespace Valora.IntegrationTests;

public class ContextReportEndpointTests
{
    [Fact]
    public async Task Post_ContextReport_ReturnsReport_WhenAuthenticated()
    {
        await using var factory = new ContextReportTestWebAppFactory("InMemory:ContextReport");
        var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var response = await client.PostAsJsonAsync("/api/context/report", new
        {
            input = "Damrak 1 Amsterdam",
            radiusMeters = 800
        });

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);

        Assert.True(json.RootElement.TryGetProperty("location", out var location));
        Assert.Equal("Damrak 1, 1012LG Amsterdam", location.GetProperty("displayAddress").GetString());
        Assert.True(json.RootElement.TryGetProperty("compositeScore", out _));
    }

    [Fact]
    public async Task Post_ContextReport_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        await using var factory = new ContextReportTestWebAppFactory("InMemory:ContextReportUnauthorized");
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/context/report", new { input = "Damrak 1 Amsterdam" });

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_ContextReport_WhenServiceThrowsValidationException_ReturnsBadRequest()
    {
        await using var factory = new ThrowingContextReportTestWebAppFactory("InMemory:ContextReportValidation");
        var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var response = await client.PostAsJsonAsync("/api/context/report", new
        {
            input = "unknown address",
            radiusMeters = 800
        });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var password = "Password123!";
        var email = "context@test.local";

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password,
            ConfirmPassword = password
        });

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = password
        });

        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
    }

    private sealed class ContextReportTestWebAppFactory : IntegrationTestWebAppFactory
    {
        public ContextReportTestWebAppFactory(string connectionString) : base(connectionString)
        {
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IContextReportService, FakeContextReportService>();
            });
        }
    }

    private sealed class ThrowingContextReportTestWebAppFactory : IntegrationTestWebAppFactory
    {
        public ThrowingContextReportTestWebAppFactory(string connectionString) : base(connectionString)
        {
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IContextReportService, ThrowingContextReportService>();
            });
        }
    }

    private sealed class FakeContextReportService : IContextReportService
    {
        public Task<ContextReportDto> BuildAsync(ContextReportRequestDto request, CancellationToken cancellationToken = default)
        {
            var location = new ResolvedLocationDto(
                Query: request.Input,
                DisplayAddress: "Damrak 1, 1012LG Amsterdam",
                Latitude: 52.37714,
                Longitude: 4.89803,
                RdX: 121691,
                RdY: 487809,
                MunicipalityCode: "GM0363",
                MunicipalityName: "Amsterdam",
                DistrictCode: "WK0363AD",
                DistrictName: "Burgwallen-Nieuwe Zijde",
                NeighborhoodCode: "BU0363AD03",
                NeighborhoodName: "Nieuwendijk-Noord",
                PostalCode: "1012LG");

            var categoryScores = new Dictionary<string, double>
            {
                ["Social"] = 80,
                ["Safety"] = 75,
                ["Demographics"] = 70,
                ["Amenities"] = 85,
                ["Environment"] = 72
            };

            var report = new ContextReportDto(
                Location: location,
                SocialMetrics: [],
                CrimeMetrics: [],
                DemographicsMetrics: [],
                AmenityMetrics: [],
                EnvironmentMetrics: [],
                CompositeScore: 75,
                CategoryScores: categoryScores,
                Sources: [],
                Warnings: []);

            return Task.FromResult(report);
        }
    }

    private sealed class ThrowingContextReportService : IContextReportService
    {
        public Task<ContextReportDto> BuildAsync(ContextReportRequestDto request, CancellationToken cancellationToken = default)
        {
            throw new ValidationException(new[] { "Could not resolve input to a Dutch address." });
        }
    }
}
