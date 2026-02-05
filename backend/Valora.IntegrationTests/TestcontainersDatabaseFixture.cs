using Testcontainers.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

public class TestcontainersDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:15-alpine")
        .WithDatabase("valora_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public IntegrationTestWebAppFactory? Factory { get; private set; }
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        try
        {
            await _dbContainer.StartAsync();
            ConnectionString = _dbContainer.GetConnectionString();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to start Testcontainers: {ex.Message}");
            Console.Error.WriteLine(ex.ToString());

            // Fallback to In-Memory if Docker is unavailable (e.g., in CI or restricted environments)
            ConnectionString = "InMemory:TestcontainersDb";
        }

        Factory = new IntegrationTestWebAppFactory(ConnectionString);

        // Ensure database schema is created
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        if (ConnectionString.StartsWith("InMemory"))
        {
            await context.Database.EnsureCreatedAsync();
        }
        else
        {
            await context.Database.MigrateAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (Factory != null) await Factory.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }
}

[CollectionDefinition("TestcontainersDatabase")]
public class TestcontainersCollection : ICollectionFixture<TestcontainersDatabaseFixture>
{
}
