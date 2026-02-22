using Testcontainers.MsSql;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Valora.Infrastructure.Persistence;

namespace Valora.IntegrationTests;

public class TestcontainersDatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public IntegrationTestWebAppFactory? Factory { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            await _dbContainer.StartAsync();
            Factory = new IntegrationTestWebAppFactory(_dbContainer.GetConnectionString());
        }
        catch (Exception)
        {
            // Fallback for environments where Testcontainers is not supported (e.g. OverlayFS issues)
            Factory = new IntegrationTestWebAppFactory("InMemory:TestcontainersFallback");
        }

        using var scope = Factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (Factory != null) await Factory.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }
}
