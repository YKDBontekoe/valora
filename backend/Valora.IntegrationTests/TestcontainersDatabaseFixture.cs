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
        var useTestcontainers = Environment.GetEnvironmentVariable("USE_TESTCONTAINERS") == "true";

        if (useTestcontainers)
        {
            try
            {
                // NOTE: If using real testcontainers, we should spin up a real docker container here.
                // E.g., via `new PostgreSqlBuilder().Build()`.
                // However, since we don't have that fully implemented in the current setup, and passing "Testcontainers"
                // creates an invalid connection string, we will fall back to InMemory for demonstration,
                // BUT throw explicitly if `USE_TESTCONTAINERS=true` was set to prevent silent masking.
                // We'll simulate that "Testcontainers" isn't fully configured yet, so we throw explicitly.
                throw new InvalidOperationException("True Testcontainers initialization is requested but no Docker container is currently spun up by the fixture.");
            }
            catch (Exception ex)
            {
                InitializationException = ex;
                // Do not silently fall back to InMemory if Testcontainers was explicitly requested but failed.
                throw new Exception("Failed to initialize Testcontainers database.", ex);
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
