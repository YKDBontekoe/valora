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
    private readonly SqliteConnection _anchorConnection;
    private readonly ServiceProvider _serviceProvider;

    public BatchJobRepositoryIntegrationTests()
    {
        // Use a shared-cache in-memory connection string to allow multiple thread-safe connections
        // to the same in-memory database.
        var connectionString = "Data Source=BatchJobTestDb;Mode=Memory;Cache=Shared";

        // Open a single anchor SqliteConnection and keep it open to preserve the in-memory DB
        // throughout the lifetime of the test.
        _anchorConnection = new SqliteConnection(connectionString);
        _anchorConnection.Open();

        // Ensure the schema is created in the SQLite database
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseSqlite(connectionString)
            .Options;
        using var context = new ValoraDbContext(options);
        context.Database.EnsureCreated();

        // Setup DI. Use the connection string so each scope creates its own thread-safe connection.
        var services = new ServiceCollection();
        services.AddDbContext<ValoraDbContext>(opts => opts.UseSqlite(connectionString));
        services.AddScoped<IBatchJobRepository, Valora.Infrastructure.Persistence.Repositories.BatchJobRepository>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task GetNextPendingJobAsync_ShouldAtomicallyClaimJob_WithRelationalDb()
    {
        // Arrange
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

        var startGate = new Barrier(concurrentWorkers);

        // Act
        for (int i = 0; i < concurrentWorkers; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                startGate.SignalAndWait();

                Thread.Sleep(Random.Shared.Next(1, 10));

                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IBatchJobRepository>();

                try
                {
                    return await repository.GetNextPendingJobAsync();
                }
                catch (DbUpdateException ex) when (ex.InnerException is SqliteException sqliteEx)
                {
                    // DbUpdateException wraps SqliteException from SaveChanges/SaveChangesAsync.
                    // If SQLite is busy or locked (5 or 6), it means another worker successfully claimed
                    // and locked the database. This is a valid negative result (no claim).
                    if (sqliteEx.SqliteErrorCode is 5 or 6) // SQLITE_BUSY or SQLITE_LOCKED
                    {
                        return null;
                    }
                    throw;
                }
                catch (SqliteException sqliteEx)
                {
                    // Catch direct SqliteExceptions in case ExecuteUpdateAsync throws it directly
                    if (sqliteEx.SqliteErrorCode is 5 or 6) // SQLITE_BUSY or SQLITE_LOCKED
                    {
                        return null;
                    }
                    throw;
                }
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        var successfulClaims = results.Where(r => r != null).ToList();

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

            var totalJobsCount = await verifyDbContext.BatchJobs.CountAsync();
            Assert.Equal(1, totalJobsCount);
        }
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        _anchorConnection?.Dispose();
    }
}
