using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Domain.Enums;

namespace Valora.Application.Services.BatchJobs;

public class AllCitiesIngestionJobProcessor : IBatchJobProcessor
{
    private readonly IBatchJobRepository _jobRepository;
    private readonly ICbsGeoClient _geoClient;
    private readonly ILogger<AllCitiesIngestionJobProcessor> _logger;

    public AllCitiesIngestionJobProcessor(
        IBatchJobRepository jobRepository,
        ICbsGeoClient geoClient,
        ILogger<AllCitiesIngestionJobProcessor> logger)
    {
        _jobRepository = jobRepository;
        _geoClient = geoClient;
        _logger = logger;
    }

    public BatchJobType JobType => BatchJobType.AllCitiesIngestion;

    public async Task ProcessAsync(BatchJob job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing all cities ingestion.");
        AppendLog(job, "Fetching all municipalities from CBS...");
        await _jobRepository.UpdateAsync(job, cancellationToken);

        var cities = await _geoClient.GetAllMunicipalitiesAsync(cancellationToken);
        if (cities == null || !cities.Any())
        {
            AppendLog(job, "No municipalities found.");
            job.ResultSummary = "No municipalities found.";
            return;
        }

        AppendLog(job, $"Found {cities.Count} municipalities. Queueing jobs...");

        int count = 0;
        foreach (var city in cities)
        {
             cancellationToken.ThrowIfCancellationRequested();

             // Check for cancellation every 10 iterations
             if (count % 10 == 0)
             {
                 var status = await _jobRepository.GetStatusAsync(job.Id, cancellationToken);
                 if (status == BatchJobStatus.Failed)
                 {
                     _logger.LogInformation("Job {JobId} was cancelled during execution", job.Id);
                     throw new OperationCanceledException("Job cancelled by user.");
                 }

                 job.Progress = (int)((double)count / cities.Count * 100);
                 await _jobRepository.UpdateAsync(job, cancellationToken);
             }

             var newJob = new BatchJob
             {
                 Type = BatchJobType.CityIngestion,
                 Target = city,
                 Status = BatchJobStatus.Pending,
                 Progress = 0
             };

            await _jobRepository.AddAsync(newJob, cancellationToken);
            count++;
        }

        job.ResultSummary = $"Queued ingestion for {cities.Count} municipalities.";
        AppendLog(job, $"Successfully queued {cities.Count} jobs.");
    }

    private void AppendLog(BatchJob job, string message)
    {
        var entry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}";
        if (string.IsNullOrEmpty(job.ExecutionLog))
            job.ExecutionLog = entry;
        else
            job.ExecutionLog += Environment.NewLine + entry;
    }
}
