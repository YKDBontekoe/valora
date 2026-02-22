using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Valora.Application.DTOs;
using Valora.Infrastructure.Persistence;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Valora.IntegrationTests;

[Collection("TestDatabase")]
public class AiConfigEndpointTests : IClassFixture<TestDatabaseFixture>, IAsyncLifetime
{
    private readonly WireMockServer _wireMockServer;
    private readonly Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AiConfigEndpointTests(TestDatabaseFixture fixture)
    {
        _wireMockServer = WireMockServer.Start();

        var factory = fixture.Factory!.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "OPENROUTER_BASE_URL", _wireMockServer.Url },
                    { "OPENROUTER_API_KEY", "test-key" }
                });
            });
        });

        _factory = factory;
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await AuthenticateAsAdminAsync();
    }

    public Task DisposeAsync()
    {
        _wireMockServer.Stop();
        _wireMockServer.Dispose();
        return Task.CompletedTask;
    }

    private async Task AuthenticateAsAdminAsync()
    {
        var email = "admin@example.com";
        var password = "AdminPassword123!";

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Valora.Domain.Entities.ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole("Admin"));
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new Valora.Domain.Entities.ApplicationUser { UserName = email, Email = email };
            await userManager.CreateAsync(user, password);
        }

        if (!await userManager.IsInRoleAsync(user, "Admin"))
        {
            await userManager.AddToRoleAsync(user, "Admin");
        }

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = password
        });
        loginResponse.EnsureSuccessStatusCode();

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<Valora.Application.DTOs.AuthResponseDto>();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.Token);
    }

    [Fact]
    public async Task GetModels_ReturnsOk_WithModels()
    {
        var responseBody = JsonSerializer.Serialize(new
        {
            data = new[]
            {
                new { id = "model-a", name = "Model A", context_length = 1024, pricing = new { prompt = "0", completion = "0" } },
                new { id = "model-b", name = "Model B", context_length = 2048, pricing = new { prompt = "0.1", completion = "0.2" } }
            }
        });

        _wireMockServer
            .Given(Request.Create().WithPath("/models").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(responseBody));

        // Act
        var response = await _client.GetAsync("/api/ai/config/models");

        // Debug: if failing, print content
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Response status code does not indicate success: {response.StatusCode}. Content: {content}");
        }

        // Assert
        var models = await response.Content.ReadFromJsonAsync<List<ExternalAiModelDto>>();
        Assert.NotNull(models);
        Assert.Equal(2, models.Count);
        Assert.Contains(models, m => m.Id == "model-a");
        Assert.Contains(models, m => m.Id == "model-b");
    }
}
