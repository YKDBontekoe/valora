using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;

namespace Valora.Infrastructure.Enrichment;

public sealed class ContextReportService : IContextReportService
{
    private readonly ILocationResolver _locationResolver;
    private readonly ICbsNeighborhoodStatsClient _cbsClient;
    private readonly IAmenityClient _amenityClient;
    private readonly IAirQualityClient _airQualityClient;
    private readonly IMemoryCache _cache;
    private readonly ContextEnrichmentOptions _options;

    public ContextReportService(
        ILocationResolver locationResolver,
        ICbsNeighborhoodStatsClient cbsClient,
        IAmenityClient amenityClient,
        IAirQualityClient airQualityClient,
        IMemoryCache cache,
        IOptions<ContextEnrichmentOptions> options)
    {
        _locationResolver = locationResolver;
        _cbsClient = cbsClient;
        _amenityClient = amenityClient;
        _airQualityClient = airQualityClient;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<ContextReportDto> BuildAsync(ContextReportRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Input))
        {
            throw new ValidationException(new[] { "Input is required." });
        }

        var normalizedRadius = Math.Clamp(request.RadiusMeters, 200, 5000);
        var cacheKey = $"context-report:{request.Input.Trim().ToLowerInvariant()}:{normalizedRadius}";

        if (_cache.TryGetValue(cacheKey, out ContextReportDto? cached) && cached is not null)
        {
            return cached;
        }

        var location = await _locationResolver.ResolveAsync(request.Input, cancellationToken);
        if (location is null)
        {
            throw new ValidationException(new[] { "Could not resolve input to a Dutch address." });
        }

        var cbsTask = _cbsClient.GetStatsAsync(location, cancellationToken);
        var amenitiesTask = _amenityClient.GetAmenitiesAsync(location, normalizedRadius, cancellationToken);
        var airQualityTask = _airQualityClient.GetSnapshotAsync(location, cancellationToken);

        await Task.WhenAll(cbsTask, amenitiesTask, airQualityTask);

        var cbs = cbsTask.Result;
        var amenities = amenitiesTask.Result;
        var air = airQualityTask.Result;

        var warnings = new List<string>();

        var socialMetrics = BuildSocialMetrics(cbs, warnings);
        var amenityMetrics = BuildAmenityMetrics(amenities, warnings);
        var environmentMetrics = BuildEnvironmentMetrics(air, warnings);
        var safetyMetrics = new List<ContextMetricDto>
        {
            new(
                Key: "safety_source_status",
                Label: "Safety Source Status",
                Value: null,
                Unit: null,
                Score: null,
                Source: "Politie Open Data",
                Note: "Not configured in this deployment yet.")
        };

        var compositeScore = ComputeCompositeScore(socialMetrics, amenityMetrics, environmentMetrics);

        var sources = BuildSourceAttributions(cbs, amenities, air);

        var report = new ContextReportDto(
            Location: location,
            SocialMetrics: socialMetrics,
            SafetyMetrics: safetyMetrics,
            AmenityMetrics: amenityMetrics,
            EnvironmentMetrics: environmentMetrics,
            CompositeScore: Math.Round(compositeScore, 1),
            Sources: sources,
            Warnings: warnings);

        _cache.Set(cacheKey, report, TimeSpan.FromMinutes(_options.ReportCacheMinutes));
        return report;
    }

    private static List<ContextMetricDto> BuildSocialMetrics(NeighborhoodStatsDto? cbs, List<string> warnings)
    {
        if (cbs is null)
        {
            warnings.Add("CBS neighborhood indicators were unavailable; social score is partial.");
            return [];
        }

        var densityScore = ScoreDensity(cbs.PopulationDensity);
        var lowIncomeScore = ScoreLowIncome(cbs.LowIncomeHouseholdsPercent);
        var wozScore = ScoreWoz(cbs.AverageWozValueKeur);

        return
        [
            new("residents", "Residents", cbs.Residents, "people", null, "CBS StatLine 83765NED"),
            new("population_density", "Population Density", cbs.PopulationDensity, "people/km2", densityScore, "CBS StatLine 83765NED"),
            new("low_income_households", "Low Income Households", cbs.LowIncomeHouseholdsPercent, "%", lowIncomeScore, "CBS StatLine 83765NED"),
            new("average_woz", "Average WOZ Value", cbs.AverageWozValueKeur, "kEUR", wozScore, "CBS StatLine 83765NED")
        ];
    }

    private static List<ContextMetricDto> BuildAmenityMetrics(AmenityStatsDto? amenities, List<string> warnings)
    {
        if (amenities is null)
        {
            warnings.Add("OSM amenities were unavailable; amenity score is partial.");
            return [];
        }

        var proximityScore = ScoreAmenityProximity(amenities.NearestAmenityDistanceMeters);
        var countScore = ScoreAmenityCount(amenities);

        return
        [
            new("schools", "Schools in Radius", amenities.SchoolCount, "count", null, "OpenStreetMap / Overpass"),
            new("supermarkets", "Supermarkets in Radius", amenities.SupermarketCount, "count", null, "OpenStreetMap / Overpass"),
            new("parks", "Parks in Radius", amenities.ParkCount, "count", null, "OpenStreetMap / Overpass"),
            new("healthcare", "Healthcare in Radius", amenities.HealthcareCount, "count", null, "OpenStreetMap / Overpass"),
            new("transit_stops", "Transit Stops in Radius", amenities.TransitStopCount, "count", null, "OpenStreetMap / Overpass"),
            new("amenity_diversity", "Amenity Diversity", amenities.DiversityScore, "score", amenities.DiversityScore, "OpenStreetMap / Overpass"),
            new("amenity_proximity", "Nearest Amenity Distance", amenities.NearestAmenityDistanceMeters, "m", proximityScore, "OpenStreetMap / Overpass"),
            new("amenity_count_score", "Amenity Volume Score", countScore, "score", countScore, "OpenStreetMap / Overpass")
        ];
    }

    private static List<ContextMetricDto> BuildEnvironmentMetrics(AirQualitySnapshotDto? air, List<string> warnings)
    {
        if (air is null)
        {
            warnings.Add("Air quality source was unavailable; environment score is partial.");
            return [];
        }

        var pm25Score = ScorePm25(air.Pm25);

        return
        [
            new("pm25", "PM2.5", air.Pm25, "µg/m3", pm25Score, "Luchtmeetnet Open API"),
            new("air_station_distance", "Distance to Air Station", air.StationDistanceMeters, "m", null, "Luchtmeetnet Open API")
        ];
    }

    private static List<SourceAttributionDto> BuildSourceAttributions(
        NeighborhoodStatsDto? cbs,
        AmenityStatsDto? amenities,
        AirQualitySnapshotDto? air)
    {
        var sources = new List<SourceAttributionDto>
        {
            new("PDOK Locatieserver", "https://api.pdok.nl", "Publiek", DateTimeOffset.UtcNow)
        };

        if (cbs is not null)
        {
            sources.Add(new SourceAttributionDto("CBS StatLine", "https://opendata.cbs.nl", "Publiek", cbs.RetrievedAtUtc));
        }

        if (amenities is not null)
        {
            sources.Add(new SourceAttributionDto("OpenStreetMap Overpass", "https://overpass-api.de", "ODbL", amenities.RetrievedAtUtc));
        }

        if (air is not null)
        {
            sources.Add(new SourceAttributionDto("Luchtmeetnet", "https://api.luchtmeetnet.nl", "Publiek", air.RetrievedAtUtc));
        }

        return sources;
    }

    private static double ComputeCompositeScore(
        IReadOnlyList<ContextMetricDto> socialMetrics,
        IReadOnlyList<ContextMetricDto> amenityMetrics,
        IReadOnlyList<ContextMetricDto> environmentMetrics)
    {
        var social = AverageScore(socialMetrics);
        var amenity = AverageScore(amenityMetrics);
        var environment = AverageScore(environmentMetrics);

        var weighted = new List<(double Score, double Weight)>();

        if (social.HasValue) weighted.Add((social.Value, 0.45));
        if (amenity.HasValue) weighted.Add((amenity.Value, 0.35));
        if (environment.HasValue) weighted.Add((environment.Value, 0.20));

        if (weighted.Count == 0)
        {
            return 0;
        }

        var totalWeight = weighted.Sum(w => w.Weight);
        return weighted.Sum(w => w.Score * w.Weight) / totalWeight;
    }

    private static double? AverageScore(IReadOnlyList<ContextMetricDto> metrics)
    {
        var values = metrics.Where(m => m.Score.HasValue).Select(m => m.Score!.Value).ToList();
        if (values.Count == 0)
        {
            return null;
        }

        return values.Average();
    }

    private static double? ScoreDensity(int? density)
    {
        if (!density.HasValue)
        {
            return null;
        }

        return density.Value switch
        {
            <= 500 => 65,
            <= 1500 => 85,
            <= 3500 => 100,
            <= 7000 => 70,
            _ => 50
        };
    }

    private static double? ScoreLowIncome(double? lowIncomePercent)
    {
        if (!lowIncomePercent.HasValue)
        {
            return null;
        }

        return Clamp(100 - (lowIncomePercent.Value * 8), 0, 100);
    }

    private static double? ScoreWoz(double? wozKeur)
    {
        if (!wozKeur.HasValue)
        {
            return null;
        }

        return Clamp((wozKeur.Value - 150) / 3, 0, 100);
    }

    private static double ScoreAmenityCount(AmenityStatsDto amenities)
    {
        var total = amenities.SchoolCount + amenities.SupermarketCount + amenities.ParkCount + amenities.HealthcareCount + amenities.TransitStopCount;
        return Clamp(total * 5, 0, 100);
    }

    private static double? ScoreAmenityProximity(double? nearestDistanceMeters)
    {
        if (!nearestDistanceMeters.HasValue)
        {
            return null;
        }

        return nearestDistanceMeters.Value switch
        {
            <= 250 => 100,
            <= 500 => 85,
            <= 1000 => 70,
            <= 1500 => 55,
            <= 2000 => 40,
            _ => 25
        };
    }

    private static double? ScorePm25(double? pm25)
    {
        if (!pm25.HasValue)
        {
            return null;
        }

        return pm25.Value switch
        {
            <= 5 => 100,
            <= 10 => 85,
            <= 15 => 70,
            <= 25 => 50,
            <= 35 => 25,
            _ => 10
        };
    }

    private static double Clamp(double value, double min, double max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
