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

    /// <summary>
    /// Executes the ingestion process for a specific city.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Strategy:</strong>
    /// <list type="number">
    /// <item>Fetch <em>all</em> neighborhood geometries for the city from PDOK.</item>
    /// <item>Fetch <em>all</em> existing neighborhood entities from our DB for this city (optimization to avoid N+1 queries).</item>
    /// <item>Iterate through the PDOK list, fetching stats from CBS for each neighborhood.</item>
    /// <item>Upsert the entities in batches to keep memory usage low and provide incremental progress updates.</item>
    /// </list>
    /// </para>
    /// </remarks>
    public async Task ProcessAsync(BatchJob job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing city ingestion for {City}", job.Target);
        AppendLog(job, $"Processing city ingestion for {job.Target}");

        List<NeighborhoodGeometryDto> neighborhoods;
        try
        {
            neighborhoods = await _geoClient.GetNeighborhoodsByMunicipalityAsync(job.Target, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Failed to fetch neighborhoods for city '{job.Target}': {ex.Message}", ex);
        }

        if (neighborhoods == null || !neighborhoods.Any())
        {
            AppendLog(job, "No neighborhoods found for city.");
            job.ResultSummary = "No neighborhoods found for city.";
            return;
        }

        // Optimization: Pre-fetch all existing neighborhoods for this city to avoid N+1 reads.
        // If we checked the DB inside the loop ("SELECT * FROM Neighborhood WHERE Code = 'x'"),
        // processing 500 neighborhoods would result in 500 round-trips.
        // Fetching them all at once reduces this to 1 query.
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

            var (neighborhood, isNew) = await EnrichNeighborhoodAsync(geo, job.Target, existingDict, cancellationToken);

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

    private async Task<(Neighborhood Entity, bool IsNew)> EnrichNeighborhoodAsync(
        NeighborhoodGeometryDto geo,
        string city,
        Dictionary<string, Neighborhood> existingDict,
        CancellationToken cancellationToken)
    {
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
                City = city,
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

        try
        {
            await Task.WhenAll(statsTask, crimeTask);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch stats for neighborhood {NeighborhoodCode}", geo.Code);
            // We choose to abort the batch on error and rethrow for upstream handling
            throw new ApplicationException($"Failed to fetch stats for neighborhood '{geo.Code}' in city '{city}': {ex.Message}", ex);
        }

        var stats = statsTask.Result;
        var crime = crimeTask.Result;

        neighborhood.PopulationDensity = stats?.PopulationDensity;
        neighborhood.AverageWozValue = stats?.AverageWozValueKeur * 1000;
        neighborhood.CrimeRate = crime?.TotalCrimesPer1000;
        neighborhood.LastUpdated = DateTime.UtcNow;

        return (neighborhood, isNew);
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
