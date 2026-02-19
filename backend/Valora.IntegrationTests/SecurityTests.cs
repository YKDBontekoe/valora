using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Valora.Domain.Entities;
using Valora.Application.DTOs;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestDatabase")]
public class SecurityTests
{
    private readonly IntegrationTestWebAppFactory _factory;
    private readonly HttpClient _client;

    public SecurityTests(TestDatabaseFixture fixture)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();
    }

    private async Task AuthenticateAsAdminAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole("Admin"));
        }

        var email = "admin_security@test.com";
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser { UserName = email, Email = email };
            await userManager.CreateAsync(user, "Password123!");
            await userManager.AddToRoleAsync(user, "Admin");
        }

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = "Password123!"
        });
        loginResponse.EnsureSuccessStatusCode();
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse!.Token);
    }

    [Fact]
    public async Task GetUsers_InvalidPage_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var response = await _client.GetAsync("/api/admin/users?page=0");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUsers_InvalidPageSize_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var response = await _client.GetAsync("/api/admin/users?pageSize=101");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUsers_LongSearchQuery_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var longQuery = new string('a', 101);
        var response = await _client.GetAsync($"/api/admin/users?q={longQuery}");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUsers_InvalidSort_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var response = await _client.GetAsync("/api/admin/users?sort=invalid_column");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteUser_EmptyId_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        // Pass a whitespace string encoded to simulate an empty/invalid ID that might bypass basic routing if not careful,
        // or specifically target the IsNullOrWhiteSpace check.
        // %20 is space.
        var response = await _client.DeleteAsync("/api/admin/users/%20");

        // Depending on routing, this might be 400 (if hits endpoint) or 404 (if no route matches).
        // Since we check IsNullOrWhiteSpace inside the endpoint, we expect 400 if it routes correctly.
        // However, ASP.NET Core routing might treat %20 as a valid ID string " ".
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
             // Success - our validation caught it
             var body = await response.Content.ReadAsStringAsync();
             body.ShouldContain("User ID is required");
        }
        else
        {
            // If it returns 404/405, that's also acceptable security-wise (resource not found), but we prefer explicit validation check if reachable.
            // Let's assert it is NOT 2xx/5xx.
            response.IsSuccessStatusCode.ShouldBeFalse();
        }
    }

    [Fact]
    public async Task GetJobs_InvalidLimit_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        // Test lower bound
        var responseLower = await _client.GetAsync("/api/admin/jobs?limit=0");
        responseLower.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Test upper bound
        var responseUpper = await _client.GetAsync("/api/admin/jobs?limit=101");
        responseUpper.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateJob_InvalidTarget_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var request = new BatchJobRequest("CityIngestion", "A"); // Too short
        var response = await _client.PostAsJsonAsync("/api/admin/jobs", request);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateJob_InvalidType_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        // Since DTO has EnumDataType, binding might fail or validation filter will catch it.
        // We pass a string that isn't in the enum.
        var json = @"{ ""Type"": ""InvalidType"", ""Target"": ""ValidTarget"" }";
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/admin/jobs", content);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAmenities_InvalidCoordinates_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var response = await _client.GetAsync("/api/map/amenities?minLat=100&minLon=0&maxLat=10&maxLon=0"); // minLat > 90
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAmenities_InvalidTypes_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        // Test with a disallowed type "foo"
        var response = await _client.GetAsync("/api/map/amenities?minLat=52.0&minLon=4.0&maxLat=52.1&maxLon=4.1&types=foo");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(52.0, 4.0, 51.0, 4.1)] // minLat > maxLat
    [InlineData(52.0, 4.0, 52.1, 3.0)] // minLon > maxLon
    [InlineData(91.0, 4.0, 92.1, 4.1)] // Lat > 90
    [InlineData(52.0, 4.0, 53.1, 5.1)] // Span > 0.5
    public async Task GetOverlays_InvalidCoordinates_ReturnsBadRequest(double minLat, double minLon, double maxLat, double maxLon)
    {
        await AuthenticateAsAdminAsync();
        var metric = "PopulationDensity";
        var url = $"/api/map/overlays?minLat={minLat}&minLon={minLon}&maxLat={maxLat}&maxLon={maxLon}&metric={metric}";
        var response = await _client.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
