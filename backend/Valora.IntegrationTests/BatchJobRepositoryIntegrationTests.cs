using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

public class BatchJobRepositoryIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ValoraDbContext> _options;
    private readonly ServiceProvider _serviceProvider;

    public BatchJobRepositoryIntegrationTests()
    {
        // Use a persistent SQLite in-memory database specifically for testing relational features
        // like ExecuteUpdateAsync which are not supported by the InMemory provider.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Ensure the schema is created in the SQLite database
        using var context = new ValoraDbContext(_options);
        context.Database.EnsureCreated();

        // Setup DI similar to what WebAppFactory does, but wired to SQLite
        var services = new ServiceCollection();
        services.AddDbContext<ValoraDbContext>(opts => opts.UseSqlite(_connection));
        services.AddScoped<IBatchJobRepository, Valora.Infrastructure.Persistence.Repositories.BatchJobRepository>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task GetNextPendingJobAsync_ShouldAtomicallyClaimJob_WithRelationalDb()
    {
        // Arrange
        // Create a single pending job using a dedicated scope
        var pendingJob = new BatchJob
        {
            Id = Guid.NewGuid(),
            Type = BatchJobType.CityIngestion,
            Status = BatchJobStatus.Pending,
            Target = "ConcurrentTestCity",
            CreatedAt = DateTime.UtcNow
        };

        using (var arrangeScope = _serviceProvider.CreateScope())
        {
            var dbContext = arrangeScope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            dbContext.BatchJobs.Add(pendingJob);
            await dbContext.SaveChangesAsync();
        }

        int concurrentWorkers = 5;
        var tasks = new List<Task<BatchJob?>>();

        // Use a Barrier to synchronize the start of all worker tasks
        var startGate = new Barrier(concurrentWorkers);

        // Act
        // Attempt to claim the job simultaneously from multiple scopes
        for (int i = 0; i < concurrentWorkers; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                // Wait for all workers to be ready to call the repository
                startGate.SignalAndWait();

                // Delay execution randomly slightly to increase chance of concurrent ExecuteUpdateAsync clashes
                // We use Thread.Sleep to not yield the thread before creating the scope
                Thread.Sleep(Random.Shared.Next(1, 10));

                // Create the scope inside the worker delegate
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IBatchJobRepository>();

                try
                {
                    return await repository.GetNextPendingJobAsync();
                }
                catch (DbUpdateException)
                {
                    // SQLite throws DbUpdateException if the database is locked during a concurrent ExecuteUpdateAsync.
                    // A real relational DB like Postgres would lock the row and execute sequentially, returning 0 rows affected.
                    // In this test, a DbUpdateException indicates another worker claimed it and locked the DB, which is a valid negative result.
                    return null;
                }
                catch (SqliteException)
                {
                    return null;
                }
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        var successfulClaims = results.Where(r => r != null).ToList();

        // Ensure only a single worker claimed the job.
        Assert.Single(successfulClaims);

        var claimedJob = successfulClaims.First();
        Assert.NotNull(claimedJob);
        Assert.Equal(pendingJob.Id, claimedJob.Id);
        Assert.Equal(BatchJobStatus.Processing, claimedJob.Status);
        Assert.NotNull(claimedJob.StartedAt);

        // Verify database state using a fresh context
        using (var verifyScope = _serviceProvider.CreateScope())
        {
            var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<ValoraDbContext>();

            var dbJob = await verifyDbContext.BatchJobs.FirstOrDefaultAsync(j => j.Id == pendingJob.Id);

            Assert.NotNull(dbJob);
            Assert.Equal(BatchJobStatus.Processing, dbJob.Status);
            Assert.NotNull(dbJob.StartedAt);

            // Ensure only one job exists in the table to be safe
            var totalJobsCount = await verifyDbContext.BatchJobs.CountAsync();
            Assert.Equal(1, totalJobsCount);
        }
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        _connection?.Dispose();
    }
}
