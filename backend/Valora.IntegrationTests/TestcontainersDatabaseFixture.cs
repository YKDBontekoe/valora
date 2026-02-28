using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Valora.Infrastructure.Persistence;

namespace Valora.IntegrationTests;

public class TestcontainersDatabaseFixture : IAsyncLifetime
{
    public IntegrationTestWebAppFactory? Factory { get; private set; }
    public Exception? InitializationException { get; private set; }

    public Task InitializeAsync()
    {
        // Always use InMemory provider to avoid Docker dependency
        Factory = new IntegrationTestWebAppFactory("InMemory:Testcontainers");

        using var scope = Factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        // EnsureCreatedAsync creates the schema based on the current DbContext model.
        return context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (Factory != null) await Factory.DisposeAsync();
        await Task.CompletedTask;
    }
}

[CollectionDefinition("TestcontainersDatabase")]
public class TestcontainersDatabaseCollection : ICollectionFixture<TestcontainersDatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
