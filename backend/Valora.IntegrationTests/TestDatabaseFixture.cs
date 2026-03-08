using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Valora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Valora.IntegrationTests;

using Testcontainers.MsSql;

public class TestDatabaseFixture : IAsyncLifetime
{
    public MsSqlContainer DbContainer { get; } = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("StrongP@ssw0rd!")
        .Build();

    public IntegrationTestWebAppFactory? Factory { get; private set; }

    public async Task InitializeAsync()
    {
        await DbContainer.StartAsync();
        var connectionString = DbContainer.GetConnectionString();
        Factory = new IntegrationTestWebAppFactory(connectionString);
        
        // Ensure database is created once
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        
        await context.Database.EnsureCreatedAsync();
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
