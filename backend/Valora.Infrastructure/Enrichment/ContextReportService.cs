using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
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
    private readonly ICbsCrimeStatsClient _crimeClient;
    private readonly IDemographicsClient _demographicsClient;
    private readonly IAmenityClient _amenityClient;
    private readonly IAirQualityClient _airQualityClient;
    private readonly IMemoryCache _cache;
    private readonly ContextEnrichmentOptions _options;
    private readonly ILogger<ContextReportService> _logger;

    public ContextReportService(
        ILocationResolver locationResolver,
        ICbsNeighborhoodStatsClient cbsClient,
        ICbsCrimeStatsClient crimeClient,
        IDemographicsClient demographicsClient,
        IAmenityClient amenityClient,
        IAirQualityClient airQualityClient,
        IMemoryCache cache,
        IOptions<ContextEnrichmentOptions> options,
        ILogger<ContextReportService> logger)
    {
        _locationResolver = locationResolver;
        _cbsClient = cbsClient;
        _crimeClient = crimeClient;
        _demographicsClient = demographicsClient;
        _amenityClient = amenityClient;
        _airQualityClient = airQualityClient;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ContextReportDto> BuildAsync(ContextReportRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Input))
        {
            throw new ValidationException(new[] { "Input is required." });
        }

        var normalizedRadius = Math.Clamp(request.RadiusMeters, 200, 5000);
        var cacheKey = $"context-report:v2:{request.Input.Trim().ToLowerInvariant()}:{normalizedRadius}";

        if (_cache.TryGetValue(cacheKey, out ContextReportDto? cached) && cached is not null)
        {
            return cached;
        }

        var location = await _locationResolver.ResolveAsync(request.Input, cancellationToken);
        if (location is null)
        {
            throw new ValidationException(new[] { "Could not resolve input to a Dutch address." });
        }

        // Fetch all data sources in parallel
        var cbsTask = TryGetSourceAsync("CBS", token => _cbsClient.GetStatsAsync(location, token), cancellationToken);
        var crimeTask = TryGetSourceAsync("CBS Crime", token => _crimeClient.GetStatsAsync(location, token), cancellationToken);
        var demographicsTask = TryGetSourceAsync("CBS Demographics", token => _demographicsClient.GetDemographicsAsync(location, token), cancellationToken);
        var amenitiesTask = TryGetSourceAsync("Overpass", token => _amenityClient.GetAmenitiesAsync(location, normalizedRadius, token), cancellationToken);
        var airQualityTask = TryGetSourceAsync("Luchtmeetnet", token => _airQualityClient.GetSnapshotAsync(location, token), cancellationToken);

        await Task.WhenAll(cbsTask, crimeTask, demographicsTask, amenitiesTask, airQualityTask);

        var cbs = await cbsTask;
        var crime = await crimeTask;
        var demographics = await demographicsTask;
        var amenities = await amenitiesTask;
        var air = await airQualityTask;

        var warnings = new List<string>();

        // Build metrics for each category
        var socialMetrics = BuildSocialMetrics(cbs, warnings);
        var crimeMetrics = BuildCrimeMetrics(crime, warnings);
        var demographicsMetrics = BuildDemographicsMetrics(demographics, warnings);
        var amenityMetrics = BuildAmenityMetrics(amenities, warnings);
        var environmentMetrics = BuildEnvironmentMetrics(air, warnings);

        // Compute category scores for radar chart
        var categoryScores = ComputeCategoryScores(socialMetrics, crimeMetrics, demographicsMetrics, amenityMetrics, environmentMetrics);
        var compositeScore = ComputeCompositeScore(categoryScores);

        var sources = BuildSourceAttributions(cbs, crime, demographics, amenities, air);

        var report = new ContextReportDto(
            Location: location,
            SocialMetrics: socialMetrics,
            CrimeMetrics: crimeMetrics,
            DemographicsMetrics: demographicsMetrics,
            AmenityMetrics: amenityMetrics,
            EnvironmentMetrics: environmentMetrics,
            CompositeScore: Math.Round(compositeScore, 1),
            CategoryScores: categoryScores,
            Sources: sources,
            Warnings: warnings);

        _cache.Set(cacheKey, report, TimeSpan.FromMinutes(_options.ReportCacheMinutes));
        return report;
    }

    private async Task<T?> TryGetSourceAsync<T>(
        string sourceName,
        Func<CancellationToken, Task<T?>> sourceCall,
        CancellationToken cancellationToken)
    {
        try
        {
            return await sourceCall(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Context source {SourceName} failed; report will continue with partial data", sourceName);
            return default;
        }
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
            new("population_density", "Population Density", cbs.PopulationDensity, "people/km²", densityScore, "CBS StatLine 83765NED"),
            new("low_income_households", "Low Income Households", cbs.LowIncomeHouseholdsPercent, "%", lowIncomeScore, "CBS StatLine 83765NED"),
            new("average_woz", "Average WOZ Value", cbs.AverageWozValueKeur, "k€", wozScore, "CBS StatLine 83765NED")
        ];
    }

    private static List<ContextMetricDto> BuildCrimeMetrics(CrimeStatsDto? crime, List<string> warnings)
    {
        if (crime is null)
        {
            warnings.Add("CBS crime statistics were unavailable; safety score is partial.");
            return [];
        }

        var totalScore = ScoreTotalCrime(crime.TotalCrimesPer1000);
        var burglaryScore = ScoreBurglary(crime.BurglaryPer1000);
        var violentScore = ScoreViolentCrime(crime.ViolentCrimePer1000);

        return
        [
            new("total_crimes", "Total Crimes", crime.TotalCrimesPer1000, "per 1000", totalScore, "CBS StatLine 47018NED"),
            new("burglary", "Burglary Rate", crime.BurglaryPer1000, "per 1000", burglaryScore, "CBS StatLine 47018NED"),
            new("violent_crime", "Violent Crime", crime.ViolentCrimePer1000, "per 1000", violentScore, "CBS StatLine 47018NED"),
            new("theft", "Theft Rate", crime.TheftPer1000, "per 1000", null, "CBS StatLine 47018NED"),
            new("vandalism", "Vandalism Rate", crime.VandalismPer1000, "per 1000", null, "CBS StatLine 47018NED")
        ];
    }

    private static List<ContextMetricDto> BuildDemographicsMetrics(DemographicsDto? demographics, List<string> warnings)
    {
        if (demographics is null)
        {
            warnings.Add("CBS demographics were unavailable; demographics score is partial.");
            return [];
        }

        var familyScore = ScoreFamilyFriendly(demographics);

        return
        [
            new("age_0_14", "Age 0-14", demographics.PercentAge0To14, "%", null, "CBS StatLine 83765NED"),
            new("age_15_24", "Age 15-24", demographics.PercentAge15To24, "%", null, "CBS StatLine 83765NED"),
            new("age_25_44", "Age 25-44", demographics.PercentAge25To44, "%", null, "CBS StatLine 83765NED"),
            new("age_45_64", "Age 45-64", demographics.PercentAge45To64, "%", null, "CBS StatLine 83765NED"),
            new("age_65_plus", "Age 65+", demographics.PercentAge65Plus, "%", null, "CBS StatLine 83765NED"),
            new("avg_household_size", "Avg Household Size", demographics.AverageHouseholdSize, "people", null, "CBS StatLine 83765NED"),
            new("owner_occupied", "Owner-Occupied", demographics.PercentOwnerOccupied, "%", null, "CBS StatLine 83765NED"),
            new("single_households", "Single Households", demographics.PercentSingleHouseholds, "%", null, "CBS StatLine 83765NED"),
            new("family_friendly", "Family-Friendly Score", familyScore, "score", familyScore, "Valora Composite")
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
            new("pm25", "PM2.5", air.Pm25, "µg/m³", pm25Score, "Luchtmeetnet Open API"),
            new("air_station", "Nearest Station", null, null, null, "Luchtmeetnet Open API", air.StationName),
            new("air_station_distance", "Distance to Station", air.StationDistanceMeters, "m", null, "Luchtmeetnet Open API")
        ];
    }

    private static List<SourceAttributionDto> BuildSourceAttributions(
        NeighborhoodStatsDto? cbs,
        CrimeStatsDto? crime,
        DemographicsDto? demographics,
        AmenityStatsDto? amenities,
        AirQualitySnapshotDto? air)
    {
        var sources = new List<SourceAttributionDto>
        {
            new("PDOK Locatieserver", "https://api.pdok.nl", "Publiek", DateTimeOffset.UtcNow)
        };

        if (cbs is not null)
        {
            sources.Add(new SourceAttributionDto("CBS StatLine 83765NED", "https://opendata.cbs.nl", "Publiek", cbs.RetrievedAtUtc));
        }

        if (crime is not null)
        {
            sources.Add(new SourceAttributionDto("CBS StatLine 47018NED", "https://opendata.cbs.nl", "Publiek", crime.RetrievedAtUtc));
        }

        if (demographics is not null)
        {
            sources.Add(new SourceAttributionDto("CBS Demographics", "https://opendata.cbs.nl", "Publiek", demographics.RetrievedAtUtc));
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

    private static Dictionary<string, double> ComputeCategoryScores(
        IReadOnlyList<ContextMetricDto> socialMetrics,
        IReadOnlyList<ContextMetricDto> crimeMetrics,
        IReadOnlyList<ContextMetricDto> demographicsMetrics,
        IReadOnlyList<ContextMetricDto> amenityMetrics,
        IReadOnlyList<ContextMetricDto> environmentMetrics)
    {
        var scores = new Dictionary<string, double>();

        var social = AverageScore(socialMetrics);
        if (social.HasValue) scores["Social"] = Math.Round(social.Value, 1);

        var crime = AverageScore(crimeMetrics);
        if (crime.HasValue) scores["Safety"] = Math.Round(crime.Value, 1);

        var demographics = AverageScore(demographicsMetrics);
        if (demographics.HasValue) scores["Demographics"] = Math.Round(demographics.Value, 1);

        var amenity = AverageScore(amenityMetrics);
        if (amenity.HasValue) scores["Amenities"] = Math.Round(amenity.Value, 1);

        var environment = AverageScore(environmentMetrics);
        if (environment.HasValue) scores["Environment"] = Math.Round(environment.Value, 1);

        return scores;
    }

    private static double ComputeCompositeScore(IReadOnlyDictionary<string, double> categoryScores)
    {
        if (categoryScores.Count == 0)
        {
            return 0;
        }

        // Weighted average with emphasis on safety and amenities
        var weights = new Dictionary<string, double>
        {
            ["Social"] = 0.20,
            ["Safety"] = 0.25,
            ["Demographics"] = 0.10,
            ["Amenities"] = 0.30,
            ["Environment"] = 0.15
        };

        double totalWeight = 0;
        double weightedSum = 0;

        foreach (var kvp in categoryScores)
        {
            if (weights.TryGetValue(kvp.Key, out var weight))
            {
                weightedSum += kvp.Value * weight;
                totalWeight += weight;
            }
        }

        return totalWeight > 0 ? weightedSum / totalWeight : 0;
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

    // Scoring functions
    private static double? ScoreDensity(int? density)
    {
        if (!density.HasValue) return null;

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
        if (!lowIncomePercent.HasValue) return null;
        return Math.Clamp(100 - (lowIncomePercent.Value * 8), 0, 100);
    }

    private static double? ScoreWoz(double? wozKeur)
    {
        if (!wozKeur.HasValue) return null;
        return Math.Clamp((wozKeur.Value - 150) / 3, 0, 100);
    }

    private static double? ScoreTotalCrime(int? crimesPer1000)
    {
        if (!crimesPer1000.HasValue) return null;

        // Lower crime is better - Dutch average is around 50 per 1000
        return crimesPer1000.Value switch
        {
            <= 20 => 100,
            <= 35 => 85,
            <= 50 => 70,
            <= 75 => 50,
            <= 100 => 30,
            _ => 15
        };
    }

    private static double? ScoreBurglary(int? burglaryPer1000)
    {
        if (!burglaryPer1000.HasValue) return null;

        return burglaryPer1000.Value switch
        {
            <= 2 => 100,
            <= 5 => 80,
            <= 10 => 60,
            <= 15 => 40,
            _ => 20
        };
    }

    private static double? ScoreViolentCrime(int? violentPer1000)
    {
        if (!violentPer1000.HasValue) return null;

        return violentPer1000.Value switch
        {
            <= 2 => 100,
            <= 5 => 75,
            <= 10 => 50,
            _ => 25
        };
    }

    private static double? ScoreFamilyFriendly(DemographicsDto demographics)
    {
        // Composite score based on presence of families and children
        double score = 50; // Base score

        if (demographics.PercentFamilyHouseholds.HasValue)
            score += (demographics.PercentFamilyHouseholds.Value - 20) * 1.5; // Boost for families

        if (demographics.PercentAge0To14.HasValue)
            score += (demographics.PercentAge0To14.Value - 15) * 2; // Boost for children

        if (demographics.AverageHouseholdSize.HasValue)
            score += (demographics.AverageHouseholdSize.Value - 2) * 15; // Larger households = more families

        return Math.Clamp(score, 0, 100);
    }

    private static double ScoreAmenityCount(AmenityStatsDto amenities)
    {
        var total = amenities.SchoolCount + amenities.SupermarketCount + amenities.ParkCount + amenities.HealthcareCount + amenities.TransitStopCount;
        return Math.Clamp(total * 5, 0, 100);
    }

    private static double? ScoreAmenityProximity(double? nearestDistanceMeters)
    {
        if (!nearestDistanceMeters.HasValue) return null;

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
        if (!pm25.HasValue) return null;

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
}
