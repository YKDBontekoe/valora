using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Services;

public class BatchJobService : IBatchJobService
{
    private readonly IBatchJobRepository _jobRepository;
    private readonly INeighborhoodRepository _neighborhoodRepository;
    private readonly ICbsGeoClient _geoClient;
    private readonly ICbsNeighborhoodStatsClient _statsClient;
    private readonly ICbsCrimeStatsClient _crimeClient;
    private readonly ILogger<BatchJobService> _logger;

    public BatchJobService(
        IBatchJobRepository jobRepository,
        INeighborhoodRepository neighborhoodRepository,
        ICbsGeoClient geoClient,
        ICbsNeighborhoodStatsClient statsClient,
        ICbsCrimeStatsClient crimeClient,
        ILogger<BatchJobService> logger)
    {
        _jobRepository = jobRepository;
        _neighborhoodRepository = neighborhoodRepository;
        _geoClient = geoClient;
        _statsClient = statsClient;
        _crimeClient = crimeClient;
        _logger = logger;
    }

    public async Task<BatchJobDto> EnqueueJobAsync(BatchJobType type, string target, CancellationToken cancellationToken = default)
    {
        var job = new BatchJob
        {
            Type = type,
            Target = target,
            Status = BatchJobStatus.Pending,
            Progress = 0
        };

        await _jobRepository.AddAsync(job, cancellationToken);

        return MapToDto(job);
    }

    public async Task<List<BatchJobDto>> GetRecentJobsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var jobs = await _jobRepository.GetRecentJobsAsync(limit, cancellationToken);
        return jobs.Select(MapToDto).ToList();
    }

    public async Task ProcessNextJobAsync(CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetNextPendingJobAsync(cancellationToken);
        if (job == null) return;

        job.Status = BatchJobStatus.Processing;
        job.StartedAt = DateTime.UtcNow;
        await _jobRepository.UpdateAsync(job, cancellationToken);

        try
        {
            if (job.Type == BatchJobType.CityIngestion)
            {
                await ProcessCityIngestionAsync(job, cancellationToken);
            }

            job.Status = BatchJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.Progress = 100;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch job {JobId} failed", job.Id);
            job.Status = BatchJobStatus.Failed;
            job.Error = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
        }

        await _jobRepository.UpdateAsync(job, cancellationToken);
    }

    private async Task ProcessCityIngestionAsync(BatchJob job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing city ingestion for {City}", job.Target);

        var neighborhoods = await _geoClient.GetNeighborhoodsByMunicipalityAsync(job.Target, cancellationToken);
        if (!neighborhoods.Any())
        {
            job.ResultSummary = "No neighborhoods found for city.";
            return;
        }

        // Optimization: Pre-fetch all existing neighborhoods for this city to avoid N+1 reads
        var existingNeighborhoods = await _neighborhoodRepository.GetByCityAsync(job.Target, cancellationToken);
        var existingDict = existingNeighborhoods.ToDictionary(n => n.Code);

        int processedCount = 0;
        int total = neighborhoods.Count;

        // Batch size for writes & concurrency
        const int batchSize = 10;
        var chunks = neighborhoods.Chunk(batchSize);

        foreach (var chunk in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await ProcessNeighborhoodChunkAsync(chunk, existingDict, job.Target, cancellationToken);

            processedCount += chunk.Length;
            job.Progress = (int)((double)processedCount / total * 100);
            await _jobRepository.UpdateAsync(job, cancellationToken);
        }

        job.ResultSummary = $"Processed {total} neighborhoods.";
    }

    private async Task ProcessNeighborhoodChunkAsync(
        NeighborhoodGeometryDto[] chunk,
        IDictionary<string, Neighborhood> existingDict,
        string city,
        CancellationToken cancellationToken)
    {
        var chunkTasks = chunk.Select(async geo => await FetchNeighborhoodDataAsync(geo, cancellationToken)).ToList();
        var results = await Task.WhenAll(chunkTasks);

        var toAdd = new List<Neighborhood>();
        var toUpdate = new List<Neighborhood>();

        foreach (var result in results)
        {
            var neighborhood = UpsertNeighborhood(result.Geo, result.Stats, result.Crime, existingDict, city, out bool isNew);

            if (isNew) toAdd.Add(neighborhood);
            else toUpdate.Add(neighborhood);
        }

        if (toAdd.Count > 0)
        {
            _neighborhoodRepository.AddRange(toAdd);
        }

        if (toUpdate.Count > 0)
        {
            _neighborhoodRepository.UpdateRange(toUpdate);
        }

        await _neighborhoodRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<(NeighborhoodGeometryDto Geo, NeighborhoodStatsDto? Stats, CrimeStatsDto? Crime)> FetchNeighborhoodDataAsync(NeighborhoodGeometryDto geo, CancellationToken cancellationToken)
    {
        var neighborhoodLocation = new ResolvedLocationDto("", "", 0, 0, null, null, null, null, null, null, geo.Code, null, null);

        var statsTask = _statsClient.GetStatsAsync(neighborhoodLocation, cancellationToken);
        var crimeTask = _crimeClient.GetStatsAsync(neighborhoodLocation, cancellationToken);

        await Task.WhenAll(statsTask, crimeTask);

        return (geo, await statsTask, await crimeTask);
    }

    private static Neighborhood UpsertNeighborhood(
        NeighborhoodGeometryDto geo,
        NeighborhoodStatsDto? stats,
        CrimeStatsDto? crime,
        IDictionary<string, Neighborhood> existingDict,
        string city,
        out bool isNew)
    {
        Neighborhood neighborhood;
        isNew = false;

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
            existingDict[neighborhood.Code] = neighborhood;
        }

        neighborhood.PopulationDensity = stats?.PopulationDensity;
        neighborhood.AverageWozValue = stats?.AverageWozValueKeur * 1000;
        neighborhood.CrimeRate = crime?.TotalCrimesPer1000;
        neighborhood.LastUpdated = DateTime.UtcNow;

        return neighborhood;
    }

    private static BatchJobDto MapToDto(BatchJob job) => new(
        job.Id,
        job.Type.ToString(),
        job.Status.ToString(),
        job.Target,
        job.Progress,
        job.Error,
        job.ResultSummary,
        job.CreatedAt,
        job.StartedAt,
        job.CompletedAt
    );
}
