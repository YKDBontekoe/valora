using Microsoft.EntityFrameworkCore;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.IntegrationTests;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace Valora.IntegrationTests.Infrastructure;

[Collection("TestcontainersDatabase")]
public class AiModelConfigurationTests : BaseTestcontainersIntegrationTest
{
    public AiModelConfigurationTests(TestcontainersDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task AddConfig_ShouldFail_WhenIntentHasInvalidCharacters()
    {
        // Skip if running in memory fallback, as Check Constraints are not supported by EF Core InMemory
        // We can check if the provider is InMemory by looking at the DbContext options
        if (DbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
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
        DbContext.AiModelConfigs.Add(config);

        await Assert.ThrowsAnyAsync<DbUpdateException>(async () =>
            await DbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task AddConfig_ShouldPass_WhenIntentIsValid()
    {
        // Arrange
        var intent = $"valid_intent_{Guid.NewGuid():N}";
        var config = new AiModelConfig
        {
            Intent = intent,
            PrimaryModel = "gpt-4",
            Description = "Test"
        };

        // Act
        DbContext.AiModelConfigs.Add(config);
        await DbContext.SaveChangesAsync();

        // Assert
        var saved = await DbContext.AiModelConfigs.FirstOrDefaultAsync(c => c.Intent == intent);
        Assert.NotNull(saved);
    }
}
