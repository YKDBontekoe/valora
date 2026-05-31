using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Domain.Enums;
using Valora.Domain.Extensions;

namespace Valora.Application.Services.BatchJobs;

/// <summary>
/// Processor for executing "CityIngestion" batch jobs. This downloads and stores
/// context statistics for all neighborhoods within a specified city.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Ingestion Strategy &amp; Fan-Out/Fan-In:</strong><br/>
/// This processor follows an ETL (Extract, Transform, Load) pipeline:
/// <list type="number">
/// <item><strong>Extract:</strong> It first queries the PDOK Location Server to get all neighborhood geometries for the target city.</item>
/// <item><strong>Transform:</strong> For each neighborhood geometry, it performs a parallel "Fan-Out" to fetch stats from CBS (demographics, safety).</item>
/// <item><strong>Load:</strong> The enriched entities are persisted to the PostgreSQL database.</item>
/// </list>
/// </para>
/// <para>
/// <strong>Optimization (Avoiding N+1 Queries):</strong><br/>
/// Before processing the raw neighborhood lists, this job queries the database *once* to fetch all existing neighborhoods for the city.
/// Checking existing data in-memory is infinitely faster than dispatching hundreds of <c>SELECT</c> queries for individual neighborhoods.
/// </para>
/// <para>
/// <strong>Batching &amp; Memory Constraints:</strong><br/>
/// Operations are performed using EF Core <c>AddRange</c> and <c>UpdateRange</c> inside a batched loop (e.g., saving every 10 records).
/// This prevents memory spikes from tracking hundreds of entities and provides incremental progress updates to the database,
/// ensuring that UI clients can track the ingestion status continuously.
/// </para>
/// </remarks>
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
        job.AppendLog($"Processing city ingestion for {job.Target}");

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
            job.AppendLog("No neighborhoods found for city.");
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
                job.AppendLog($"Processed {count}/{total} neighborhoods.");
                await _jobRepository.UpdateAsync(job, cancellationToken);
            }
        }

        job.AppendLog($"Processed {total} neighborhoods.");
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
        var loc = new ResolvedLocationDto(
            Query: string.Empty,
            DisplayAddress: string.Empty,
            Latitude: 0,
            Longitude: 0,
            RdX: null,
            RdY: null,
            MunicipalityCode: null,
            MunicipalityName: null,
            DistrictCode: null,
            DistrictName: null,
            NeighborhoodCode: geo.Code,
            NeighborhoodName: null,
            PostalCode: null);

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
}
