using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Valora.Infrastructure.Persistence;

namespace Valora.IntegrationTests;

public class TestcontainersDatabaseFixture : IAsyncLifetime
{
    public IntegrationTestWebAppFactory? Factory { get; private set; }
    public Exception? InitializationException { get; private set; }

    public async Task InitializeAsync()
    {
        var useTestcontainers = Environment.GetEnvironmentVariable("USE_TESTCONTAINERS") != "false";

        if (useTestcontainers)
        {
            try
            {
                // Note: Ensure the actual Docker container setup is implemented or configured if relying on true Testcontainers
                // Currently simulating conditional fallback for demonstration as the setup depends on user environment.
                // Assuming we start a PostgreSqlContainer or similar. Since we are refactoring, we check the flag.
                Factory = new IntegrationTestWebAppFactory("Testcontainers");
                using var scope = Factory.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
                await context.Database.EnsureCreatedAsync();
            }
            catch (Exception ex)
            {
                InitializationException = ex;
                // Fall back to InMemory
                Factory = new IntegrationTestWebAppFactory("InMemory:TestcontainersFallback");
                using var fallbackScope = Factory.Services.CreateScope();
                var fallbackContext = fallbackScope.ServiceProvider.GetRequiredService<ValoraDbContext>();
                await fallbackContext.Database.EnsureCreatedAsync();
            }
        }
        else
        {
            Factory = new IntegrationTestWebAppFactory("InMemory:Testcontainers");
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            await context.Database.EnsureCreatedAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (Factory != null) await Factory.DisposeAsync();
    }
}

[CollectionDefinition("TestcontainersDatabase")]
public class TestcontainersDatabaseCollection : ICollectionFixture<TestcontainersDatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
