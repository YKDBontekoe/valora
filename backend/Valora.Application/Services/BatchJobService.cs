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

        int count = 0;
        int total = neighborhoods.Count;

        foreach (var geo in neighborhoods)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var neighborhood = await _neighborhoodRepository.GetByCodeAsync(geo.Code, cancellationToken);
            if (neighborhood == null)
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
                await _neighborhoodRepository.AddAsync(neighborhood, cancellationToken);
            }

            // Fetch stats
            var loc = CreateResolvedLocation(geo.Code);

            var stats = await _statsClient.GetStatsAsync(loc, cancellationToken);
            var crime = await _crimeClient.GetStatsAsync(loc, cancellationToken);

            neighborhood.PopulationDensity = stats?.PopulationDensity;
            neighborhood.AverageWozValue = stats?.AverageWozValueKeur * 1000;
            neighborhood.CrimeRate = crime?.TotalCrimesPer1000;
            neighborhood.LastUpdated = DateTime.UtcNow;

            await _neighborhoodRepository.UpdateAsync(neighborhood, cancellationToken);

            count++;
            job.Progress = (int)((double)count / total * 100);
            if (count % 5 == 0)
            {
                await _jobRepository.UpdateAsync(job, cancellationToken);
            }
        }

        job.ResultSummary = $"Processed {total} neighborhoods.";
    }

    private static ResolvedLocationDto CreateResolvedLocation(string code)
    {
        // We only need the neighborhood code for stats fetching
        return new ResolvedLocationDto(
            string.Empty,
            string.Empty,
            0,
            0,
            null,
            null,
            null,
            null,
            null,
            null,
            code,
            null,
            null);
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
