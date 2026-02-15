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
    public async Task Get_Resolve_ReturnsLocation_WhenAuthenticated()
    {
        await using var factory = new ContextReportTestWebAppFactory("InMemory:ContextResolve");
        var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var response = await client.GetAsync("/api/context/resolve?input=Damrak+1+Amsterdam");

        response.EnsureSuccessStatusCode();

        var location = await response.Content.ReadFromJsonAsync<ResolvedLocationDto>();
        Assert.NotNull(location);
        Assert.Equal("Damrak 1, 1012LG Amsterdam", location.DisplayAddress);
    }

    [Fact]
    public async Task Post_MetricsSocial_ReturnsMetrics()
    {
        await using var factory = new ContextReportTestWebAppFactory("InMemory:ContextSocial");
        var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var location = CreateLocation();
        var response = await client.PostAsJsonAsync("/api/context/metrics/social", location);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        Assert.True(json.RootElement.TryGetProperty("metrics", out _));
    }

    [Fact]
    public async Task Post_MetricsSafety_ReturnsMetrics()
    {
        await using var factory = new ContextReportTestWebAppFactory("InMemory:ContextSafety");
        var client = factory.CreateClient();
        await AuthenticateAsync(client);
        var response = await client.PostAsJsonAsync("/api/context/metrics/safety", CreateLocation());
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Post_MetricsAmenities_ReturnsMetrics()
    {
        await using var factory = new ContextReportTestWebAppFactory("InMemory:ContextAmenities");
        var client = factory.CreateClient();
        await AuthenticateAsync(client);
        var response = await client.PostAsJsonAsync("/api/context/metrics/amenities", new { Location = CreateLocation(), RadiusMeters = 1000 });
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Post_MetricsEnvironment_ReturnsMetrics()
    {
        await using var factory = new ContextReportTestWebAppFactory("InMemory:ContextEnvironment");
        var client = factory.CreateClient();
        await AuthenticateAsync(client);
        var response = await client.PostAsJsonAsync("/api/context/metrics/environment", CreateLocation());
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Post_MetricsDemographics_ReturnsMetrics()
    {
        await using var factory = new ContextReportTestWebAppFactory("InMemory:ContextDemographics");
        var client = factory.CreateClient();
        await AuthenticateAsync(client);
        var response = await client.PostAsJsonAsync("/api/context/metrics/demographics", CreateLocation());
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Post_MetricsHousing_ReturnsMetrics()
    {
        await using var factory = new ContextReportTestWebAppFactory("InMemory:ContextHousing");
        var client = factory.CreateClient();
        await AuthenticateAsync(client);
        var response = await client.PostAsJsonAsync("/api/context/metrics/housing", CreateLocation());
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Post_MetricsMobility_ReturnsMetrics()
    {
        await using var factory = new ContextReportTestWebAppFactory("InMemory:ContextMobility");
        var client = factory.CreateClient();
        await AuthenticateAsync(client);
        var response = await client.PostAsJsonAsync("/api/context/metrics/mobility", CreateLocation());
        response.EnsureSuccessStatusCode();
    }

    private static ResolvedLocationDto CreateLocation()
    {
        return new ResolvedLocationDto(
            Query: "Damrak 1 Amsterdam",
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
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var password = "Password123!";
        var email = Guid.NewGuid().ToString() + "@test.local";

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

    private sealed class ContextReportTestWebAppFactory(string connectionString) : IntegrationTestWebAppFactory(connectionString)
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IContextReportService, FakeContextReportService>();
            });
        }
    }

    private sealed class FakeContextReportService : IContextReportService
    {
        public Task<ResolvedLocationDto?> ResolveLocationAsync(string input, CancellationToken ct = default)
        {
            return Task.FromResult<ResolvedLocationDto?>(CreateLocation());
        }

        public Task<List<ContextMetricDto>> GetSocialMetricsAsync(ResolvedLocationDto location, List<string> warnings, CancellationToken ct = default)
            => Task.FromResult(new List<ContextMetricDto>());

        public Task<List<ContextMetricDto>> GetSafetyMetricsAsync(ResolvedLocationDto location, List<string> warnings, CancellationToken ct = default)
            => Task.FromResult(new List<ContextMetricDto>());

        public Task<List<ContextMetricDto>> GetAmenityMetricsAsync(ResolvedLocationDto location, int radiusMeters, List<string> warnings, CancellationToken ct = default)
            => Task.FromResult(new List<ContextMetricDto>());

        public Task<List<ContextMetricDto>> GetEnvironmentMetricsAsync(ResolvedLocationDto location, List<string> warnings, CancellationToken ct = default)
            => Task.FromResult(new List<ContextMetricDto>());

        public Task<List<ContextMetricDto>> GetDemographicsMetricsAsync(ResolvedLocationDto location, List<string> warnings, CancellationToken ct = default)
            => Task.FromResult(new List<ContextMetricDto>());

        public Task<List<ContextMetricDto>> GetHousingMetricsAsync(ResolvedLocationDto location, List<string> warnings, CancellationToken ct = default)
            => Task.FromResult(new List<ContextMetricDto>());

        public Task<List<ContextMetricDto>> GetMobilityMetricsAsync(ResolvedLocationDto location, List<string> warnings, CancellationToken ct = default)
            => Task.FromResult(new List<ContextMetricDto>());

        public Task<ContextReportDto> BuildAsync(ContextReportRequestDto request, CancellationToken cancellationToken = default)
        {
            var location = CreateLocation();
            return Task.FromResult(new ContextReportDto(
                Location: location,
                SocialMetrics: [],
                CrimeMetrics: [],
                DemographicsMetrics: [],
                HousingMetrics: [],
                MobilityMetrics: [],
                AmenityMetrics: [],
                EnvironmentMetrics: [],
                CompositeScore: 0,
                CategoryScores: new Dictionary<string, double>(),
                Sources: [],
                Warnings: []));
        }
    }
}
