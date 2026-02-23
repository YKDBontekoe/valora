using Valora.Application.Common.Interfaces;

namespace Valora.Api.Background;

public class BatchJobWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BatchJobWorker> _logger;

    public BatchJobWorker(IServiceProvider serviceProvider, ILogger<BatchJobWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Batch Job Worker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var stopWorker = false;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var jobExecutor = scope.ServiceProvider.GetRequiredService<IBatchJobExecutor>();

                await jobExecutor.ProcessNextJobAsync(stoppingToken);
            }
            catch (Exception ex) when (IsDatabaseAuthenticationFailure(ex))
            {
                _logger.LogCritical(
                    ex,
                    "Stopping Batch Job Worker due to database authentication failure. Verify Azure SQL connection string/user/password configuration.");
                stopWorker = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing batch jobs.");
            }

            if (stopWorker)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }

        _logger.LogInformation("Batch Job Worker is stopping.");
    }

    internal static bool IsDatabaseAuthenticationFailure(Exception exception)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
        {
            if (current.Message.Contains("Login failed for user", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
