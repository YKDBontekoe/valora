using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Valora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Valora.IntegrationTests;

public class TestDatabaseFixture : IAsyncLifetime
{
    public IntegrationTestWebAppFactory? Factory { get; private set; }

    public async Task InitializeAsync()
    {
        // Use a marker string to signal InMemory usage
        Factory = new IntegrationTestWebAppFactory("InMemory");

        // Ensure database is created once
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (Factory != null) await Factory.DisposeAsync();
    }
}

[CollectionDefinition("TestDatabase")]
public class TestCollection : ICollectionFixture<TestDatabaseFixture>
{
}
