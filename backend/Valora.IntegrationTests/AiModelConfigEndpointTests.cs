using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Valora.Application.DTOs;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.IntegrationTests;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Microsoft.AspNetCore.TestHost;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class AiModelConfigEndpointTests : BaseTestcontainersIntegrationTest
{
    private readonly Mock<IAiService> _mockAiService = new();

    public AiModelConfigEndpointTests(TestcontainersDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetModels_ReturnsForbidden_WhenNotAdmin()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/ai/config/models");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetModels_ReturnsOk_WhenAdmin()
    {
        // Arrange
        var email = $"admin_{Guid.NewGuid():N}@example.com";
        await AuthenticateAsAdminAsync(email);

        // We need to override the IAiService in DI for this test
        var customFactory = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => _mockAiService.Object);
            });
        });

        var client = customFactory.CreateClient();

        _mockAiService.Setup(x => x.GetAvailableModelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AiModelDto>
            {
                new AiModelDto("gpt-4", "GPT-4", "desc", 8000, 0.01m, 0.03m),
                new AiModelDto("gpt-3.5-turbo", "GPT-3.5 Turbo", "desc", 4000, 0.001m, 0.002m)
            });

        // Authenticate the custom client
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = "AdminPassword123!"
        });
        loginResponse.EnsureSuccessStatusCode();

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<Valora.Application.DTOs.AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.Token);

        // Act
        var response = await client.GetAsync("/api/ai/config/models");

        // Assert
        response.EnsureSuccessStatusCode();
        var models = await response.Content.ReadFromJsonAsync<List<AiModelDto>>();
        Assert.NotNull(models);
        Assert.NotEmpty(models);
    }

    [Fact]
    public async Task GetAllConfigs_ReturnsOk_WhenAdmin()
    {
        // Arrange
        var email = $"admin_{Guid.NewGuid():N}@example.com";
        await AuthenticateAsAdminAsync(email);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        db.AiModelConfigs.Add(new AiModelConfig
        {
            Feature = $"test_feature_{Guid.NewGuid():N}",
            ModelId = "gpt-4",
            Description = "Test"
        });
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/ai/config/");

        // Assert
        response.EnsureSuccessStatusCode();
        var configs = await response.Content.ReadFromJsonAsync<List<AiModelConfigDto>>();
        Assert.NotNull(configs);
    }

    [Fact]
    public async Task PutConfig_CreatesNewConfig_WhenAdmin()
    {
        // Arrange
        var email = $"admin_{Guid.NewGuid():N}@example.com";
        await AuthenticateAsAdminAsync(email);
        var request = new UpdateAiModelConfigDto
        {
            Feature = $"new_feature_{Guid.NewGuid():N}",
            ModelId = "gpt-4-turbo",
            Description = "A new feature",
            IsEnabled = true,
            SystemPrompt = "You are a helpful assistant.",
            Temperature = 0.7,
            MaxTokens = 1000
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/ai/config/{request.Feature}", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var config = await response.Content.ReadFromJsonAsync<AiModelConfigDto>();
        Assert.NotNull(config);
        Assert.Equal(request.Feature, config.Feature);
        Assert.Equal("gpt-4-turbo", config.ModelId);

        // Verify side effects
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        var dbConfig = await db.AiModelConfigs.FirstOrDefaultAsync(c => c.Feature == request.Feature);
        Assert.NotNull(dbConfig);
        Assert.Equal("gpt-4-turbo", dbConfig.ModelId);
    }

    [Fact]
    public async Task PutConfig_UpdatesExistingConfig_WhenAdmin()
    {
        // Arrange
        var email = $"admin_{Guid.NewGuid():N}@example.com";
        await AuthenticateAsAdminAsync(email);

        var feature = $"existing_feature_{Guid.NewGuid():N}";
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        var existingConfig = new AiModelConfig
        {
            Feature = feature,
            ModelId = "gpt-3.5-turbo",
            Description = "Old description"
        };
        db.AiModelConfigs.Add(existingConfig);
        await db.SaveChangesAsync();

        // Detach to avoid tracking issues
        db.Entry(existingConfig).State = EntityState.Detached;

        var request = new UpdateAiModelConfigDto
        {
            Feature = feature,
            ModelId = "gpt-4",
            Description = "Updated description"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/ai/config/{request.Feature}", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var config = await response.Content.ReadFromJsonAsync<AiModelConfigDto>();
        Assert.NotNull(config);
        Assert.Equal("gpt-4", config.ModelId);
        Assert.Equal("Updated description", config.Description);

        // Verify side effects
        var updatedDbConfig = await db.AiModelConfigs.AsNoTracking().FirstOrDefaultAsync(c => c.Feature == feature);
        Assert.NotNull(updatedDbConfig);
        Assert.Equal("gpt-4", updatedDbConfig.ModelId);
        Assert.Equal("Updated description", updatedDbConfig.Description);
    }

    [Fact]
    public async Task PutConfig_ReturnsBadRequest_WhenFeatureMismatch()
    {
        // Arrange
        var email = $"admin_{Guid.NewGuid():N}@example.com";
        await AuthenticateAsAdminAsync(email);
        var request = new UpdateAiModelConfigDto
        {
            Feature = "feature_a",
            ModelId = "gpt-4"
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/ai/config/feature_b", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteConfig_RemovesConfig_WhenAdmin()
    {
        // Arrange
        var email = $"admin_{Guid.NewGuid():N}@example.com";
        await AuthenticateAsAdminAsync(email);

        var feature = $"to_delete_{Guid.NewGuid():N}";
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        var existingConfig = new AiModelConfig
        {
            Feature = feature,
            ModelId = "gpt-4"
        };
        db.AiModelConfigs.Add(existingConfig);
        await db.SaveChangesAsync();

        // Detach
        db.Entry(existingConfig).State = EntityState.Detached;

        // Act
        var response = await Client.DeleteAsync($"/api/ai/config/{existingConfig.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify side effects
        var deletedConfig = await db.AiModelConfigs.FindAsync(existingConfig.Id);
        Assert.Null(deletedConfig);
    }

    [Fact]
    public async Task DeleteConfig_ReturnsNotFound_WhenConfigDoesNotExist()
    {
        // Arrange
        var email = $"admin_{Guid.NewGuid():N}@example.com";
        await AuthenticateAsAdminAsync(email);

        // Act
        var response = await Client.DeleteAsync($"/api/ai/config/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
