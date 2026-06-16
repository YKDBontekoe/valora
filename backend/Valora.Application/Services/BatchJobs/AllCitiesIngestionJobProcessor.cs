using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Domain.Enums;
using Valora.Domain.Extensions;

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

    /// <summary>
    /// Executes a job to fetch all municipalities and spawns individual city ingestion jobs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Fan-Out Background Strategy:</strong> Fetching stats for every neighborhood in the
    /// Netherlands sequentially within a single API request would instantly timeout.
    /// Instead, this "orchestrator" job queries the CBS endpoints to retrieve all municipality codes.
    /// It then creates one <see cref="BatchJobType.CityIngestion"/> child job for each city.
    /// This effectively parallelizes the massive data ingestion via a FIFO queue, isolating failures
    /// so a single API timeout for "Amsterdam" does not crash the entire ingestion process.
    /// </para>
    /// </remarks>
    /// <param name="job">The current executing job context.</param>
    /// <param name="cancellationToken">Token to halt execution midway.</param>
    public async Task ProcessAsync(BatchJob job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing all cities ingestion.");
        job.AppendLog("Fetching all municipalities from CBS...");
        await _jobRepository.UpdateAsync(job, cancellationToken);

        var cities = await _geoClient.GetAllMunicipalitiesAsync(cancellationToken);
        if (cities == null || !cities.Any())
        {
            job.AppendLog("No municipalities found.");
            job.ResultSummary = "No municipalities found.";
            return;
        }

        job.AppendLog($"Found {cities.Count} municipalities. Queueing jobs...");

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
        job.AppendLog($"Successfully queued {cities.Count} jobs.");
    }
}
