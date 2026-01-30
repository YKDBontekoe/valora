using Testcontainers.PostgreSql;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Valora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Valora.IntegrationTests;

public class TestDatabaseFixture : IAsyncLifetime
{
    public PostgreSqlContainer DbContainer { get; } = new PostgreSqlBuilder("postgres:latest")
        .WithDatabase("valora_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public IntegrationTestWebAppFactory? Factory { get; private set; }

    public async Task InitializeAsync()
    {
        await DbContainer.StartAsync();
        Factory = new IntegrationTestWebAppFactory(DbContainer.GetConnectionString());
        
        // Ensure database is created once
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        
        // If EnsureCreatedAsync returns false, it means the database/tables already exist.
        // But in our case, it might return false because Hangfire created its tables, 
        // even though EF Core tables are missing.
        var databaseCreator = context.Database.GetService<IRelationalDatabaseCreator>();
        try 
        {
            await databaseCreator.CreateTablesAsync();
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P07") // duplicate_table
        {
            // Tables already exist, ignore
        }
    }

    public async Task DisposeAsync()
    {
        if (Factory != null) await Factory.DisposeAsync();
        await DbContainer.StopAsync();
    }
}

[CollectionDefinition("TestDatabase")]
public class TestCollection : ICollectionFixture<TestDatabaseFixture>
{
}
