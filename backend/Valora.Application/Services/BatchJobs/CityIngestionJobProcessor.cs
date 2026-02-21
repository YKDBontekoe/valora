using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Services.BatchJobs;

public class CityIngestionJobProcessor : IBatchJobProcessor
{
    private readonly IBatchJobRepository _jobRepository;
    private readonly INeighborhoodRepository _neighborhoodRepository;
    private readonly ICbsGeoClient _geoClient;
    private readonly ICbsNeighborhoodStatsClient _statsClient;
    private readonly ICbsCrimeStatsClient _crimeClient;
    private readonly ILogger<CityIngestionJobProcessor> _logger;

    public CityIngestionJobProcessor(
        IBatchJobRepository jobRepository,
        INeighborhoodRepository neighborhoodRepository,
        ICbsGeoClient geoClient,
        ICbsNeighborhoodStatsClient statsClient,
        ICbsCrimeStatsClient crimeClient,
        ILogger<CityIngestionJobProcessor> logger)
    {
        _jobRepository = jobRepository;
        _neighborhoodRepository = neighborhoodRepository;
        _geoClient = geoClient;
        _statsClient = statsClient;
        _crimeClient = crimeClient;
        _logger = logger;
    }

    public BatchJobType JobType => BatchJobType.CityIngestion;

    public async Task ProcessAsync(BatchJob job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing city ingestion for {City}", job.Target);
        AppendLog(job, $"Processing city ingestion for {job.Target}");

        var neighborhoods = await _geoClient.GetNeighborhoodsByMunicipalityAsync(job.Target, cancellationToken);
        if (!neighborhoods.Any())
        {
            AppendLog(job, "No neighborhoods found for city.");
            job.ResultSummary = "No neighborhoods found for city.";
            return;
        }

        // Optimization: Pre-fetch all existing neighborhoods for this city to avoid N+1 reads
        var existingNeighborhoods = await _neighborhoodRepository.GetByCityAsync(job.Target, cancellationToken);
        var existingDict = existingNeighborhoods.ToDictionary(n => n.Code);

        int count = 0;
        int total = neighborhoods.Count;

        var toAdd = new List<Neighborhood>();
        var toUpdate = new List<Neighborhood>();
        // Batch size for writes
        const int batchSize = 10;

        foreach (var geo in neighborhoods)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Check for cancellation every iteration or periodically
            if (count % 10 == 0)
            {
                var status = await _jobRepository.GetStatusAsync(job.Id, cancellationToken);
                if (status == BatchJobStatus.Failed)
                {
                    _logger.LogInformation("Job {JobId} was cancelled during execution", job.Id);
                    throw new OperationCanceledException("Job cancelled by user.");
                }
            }

            Neighborhood neighborhood;
            bool isNew = false;

            if (existingDict.TryGetValue(geo.Code, out var existing))
            {
                neighborhood = existing;
            }
            else
            {
                neighborhood = new Neighborhood
                {
                    Code = geo.Code,
                    Name = geo.Name,
                    City = job.Target,
                    Type = geo.Type,
                    Latitude = geo.Latitude,
                    Longitude = geo.Longitude
                };
                isNew = true;
            }

            // Fetch stats (external calls still per item, but could be parallelized if API allows)
            var loc = new ResolvedLocationDto("", "", 0, 0, null, null, null, null, null, null, geo.Code, null, null);

            // Parallelize stats fetching
            var statsTask = _statsClient.GetStatsAsync(loc, cancellationToken);
            var crimeTask = _crimeClient.GetStatsAsync(loc, cancellationToken);

            await Task.WhenAll(statsTask, crimeTask);

            var stats = await statsTask;
            var crime = await crimeTask;

            neighborhood.PopulationDensity = stats?.PopulationDensity;
            neighborhood.AverageWozValue = stats?.AverageWozValueKeur * 1000;
            neighborhood.CrimeRate = crime?.TotalCrimesPer1000;
            neighborhood.LastUpdated = DateTime.UtcNow;

            if (isNew) toAdd.Add(neighborhood);
            else toUpdate.Add(neighborhood);

            count++;

            // Batch Flush
            if (count % batchSize == 0 || count == total)
            {
                if (toAdd.Count > 0)
                {
                    _neighborhoodRepository.AddRange(toAdd);
                    // Add to dictionary so we don't try to add again if duplicates in list (unlikely)
                    foreach (var n in toAdd) existingDict[n.Code] = n;
                    // Create new list to avoid reference mutation issues with Moq in tests
                    toAdd = new List<Neighborhood>();
                }

                if (toUpdate.Count > 0)
                {
                    _neighborhoodRepository.UpdateRange(toUpdate);
                    // Create new list to avoid reference mutation issues with Moq in tests
                    toUpdate = new List<Neighborhood>();
                }

                // Persist all changes
                await _neighborhoodRepository.SaveChangesAsync(cancellationToken);

                job.Progress = (int)((double)count / total * 100);
                AppendLog(job, $"Processed {count}/{total} neighborhoods.");
                await _jobRepository.UpdateAsync(job, cancellationToken);
            }
        }

        AppendLog(job, $"Processed {total} neighborhoods.");
        job.ResultSummary = $"Processed {total} neighborhoods.";
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
