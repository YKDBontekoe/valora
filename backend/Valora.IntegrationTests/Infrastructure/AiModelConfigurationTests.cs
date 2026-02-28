using Microsoft.EntityFrameworkCore;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.IntegrationTests;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace Valora.IntegrationTests.Infrastructure;

[Collection("TestcontainersDatabase")]
public class AiModelConfigurationTests
{
    private readonly TestcontainersDatabaseFixture _fixture;

    public AiModelConfigurationTests(TestcontainersDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddConfig_ShouldFail_WhenIntentHasInvalidCharacters()
    {
        // Skip if running in memory fallback, as Check Constraints are not supported by EF Core InMemory
        // We can check if the provider is InMemory by looking at the DbContext options
        using var scope = _fixture.Factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            return;
        }

        var config = new AiModelConfig
        {
            Intent = "invalid-intent!",
            PrimaryModel = "gpt-4",
            Description = "Test"
        };

        // Act & Assert
        context.AiModelConfigs.Add(config);

        await Assert.ThrowsAnyAsync<DbUpdateException>(async () =>
            await context.SaveChangesAsync());
    }

    [Fact]
    public async Task AddConfig_ShouldPass_WhenIntentIsValid()
    {
        // Arrange
        using var scope = _fixture.Factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var config = new AiModelConfig
        {
            Intent = "valid_intent_123",
            PrimaryModel = "gpt-4",
            Description = "Test"
        };

        // Act
        context.AiModelConfigs.Add(config);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.AiModelConfigs.FirstOrDefaultAsync(c => c.Intent == "valid_intent_123");
        Assert.NotNull(saved);
    }
}
