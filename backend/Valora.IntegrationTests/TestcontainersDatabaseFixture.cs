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
                // We construct the factory with the Testcontainers connection string to trigger
                // Testcontainers usage instead of falling back to InMemory.
                Factory = new IntegrationTestWebAppFactory("Testcontainers");

                using var scope = Factory.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
                await context.Database.EnsureCreatedAsync();
            }
            catch (Exception ex)
            {
                InitializationException = ex;
                throw new Exception("Failed to initialize Testcontainers database. Ensure Docker is running and accessible.", ex);
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
