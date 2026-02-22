using Testcontainers.MsSql;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Valora.Infrastructure.Persistence;

namespace Valora.IntegrationTests;

public class TestcontainersDatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public IntegrationTestWebAppFactory? Factory { get; private set; }
    public Exception? InitializationException { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            await _dbContainer.StartAsync();
            Factory = new IntegrationTestWebAppFactory(_dbContainer.GetConnectionString());
        }
        catch (Exception ex)
        {
            InitializationException = ex;
            // Fallback for environments where Testcontainers is not supported (e.g. OverlayFS issues)
            Factory = new IntegrationTestWebAppFactory("InMemory:TestcontainersFallback");
        }

        using var scope = Factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        // EnsureCreatedAsync creates the schema based on the current DbContext model.
        // This is necessary because the project does not contain migration files.
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (Factory != null) await Factory.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }
}

[CollectionDefinition("TestcontainersDatabase")]
public class TestcontainersDatabaseCollection : ICollectionFixture<TestcontainersDatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
