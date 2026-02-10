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

    /// <summary>
    /// Coordinates the retrieval of data from multiple public sources and builds a unified context report.
    /// </summary>
    /// <remarks>
    /// This method employs a "fan-out" pattern to query all external APIs in parallel.
    /// It is designed for resilience: if a non-critical source fails (e.g., air quality),
    /// the method catches the exception via <see cref="TryGetSourceAsync{T}"/> and returns a partial report
    /// with a warning, rather than failing the entire request.
    /// </remarks>
    public async Task<ContextReportDto> BuildAsync(ContextReportRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Input))
        {
            throw new ValidationException(new[] { "Input is required." });
        }

        // Radius is clamped to prevent excessive load on Overpass/external APIs
        var normalizedRadius = Math.Clamp(request.RadiusMeters, 200, 5000);

        // 1. Resolve Location First
        // The location resolver has its own cache. We need the resolved coordinates/IDs
        // to build a stable cache key for the expensive report generation.
        var location = await _locationResolver.ResolveAsync(request.Input, cancellationToken);
        if (location is null)
        {
            throw new ValidationException(new[] { "Could not resolve input to a Dutch address." });
        }

        // 2. Check Report Cache using stable location key
        // Key format: context-report:v3:{lat_f5}_{lon_f5}:{radius}
        // This ensures "Damrak 1" and "Damrak 1 Amsterdam" share the same report if they resolve to the same point.
        var latKey = location.Latitude.ToString("F5");
        var lonKey = location.Longitude.ToString("F5");
        var cacheKey = $"context-report:v3:{latKey}_{lonKey}:{normalizedRadius}";

        if (_cache.TryGetValue(cacheKey, out ContextReportDto? cached) && cached is not null)
        {
            return cached;
        }

        // Fetch all data sources in parallel (Fan-out)
        // Each task is wrapped in a safe executor that returns null on failure instead of throwing
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

        if (normalizedRadius != request.RadiusMeters)
        {
            warnings.Add($"Radius clamped from {request.RadiusMeters}m to {normalizedRadius}m to respect system limits.");
        }

        // Build normalized metrics for each category (Fan-in)
        var socialMetrics = BuildSocialMetrics(cbs, warnings);
        var crimeMetrics = BuildCrimeMetrics(crime, warnings);
        var demographicsMetrics = BuildDemographicsMetrics(demographics, warnings);
        var housingMetrics = BuildHousingMetrics(cbs, warnings); // Phase 2
        var mobilityMetrics = BuildMobilityMetrics(cbs, warnings); // Phase 2
        var amenityMetrics = BuildAmenityMetrics(amenities, cbs, warnings); // Phase 2: CBS Proximity
        var environmentMetrics = BuildEnvironmentMetrics(air, warnings);

        // Compute category scores for radar chart
        var categoryScores = ComputeCategoryScores(socialMetrics, crimeMetrics, demographicsMetrics, housingMetrics, mobilityMetrics, amenityMetrics, environmentMetrics);
        var compositeScore = ComputeCompositeScore(categoryScores);

        var sources = BuildSourceAttributions(cbs, crime, demographics, amenities, air);

        var report = new ContextReportDto(
            Location: location,
            SocialMetrics: socialMetrics,
            CrimeMetrics: crimeMetrics,
            DemographicsMetrics: demographicsMetrics,
            HousingMetrics: housingMetrics, // Phase 2
            MobilityMetrics: mobilityMetrics, // Phase 2
            AmenityMetrics: amenityMetrics,
            EnvironmentMetrics: environmentMetrics,
            CompositeScore: Math.Round(compositeScore, 1),
            CategoryScores: categoryScores,
            Sources: sources,
            Warnings: warnings);

        _cache.Set(cacheKey, report, TimeSpan.FromMinutes(_options.ReportCacheMinutes));
        return report;
    }

    /// <summary>
    /// Wraps an external API call in a try-catch block to ensure partial success.
    /// </summary>
    /// <remarks>
    /// If an exception occurs (other than cancellation), it is logged as an error,
    /// and the method returns <c>default</c> (null), allowing the report builder to continue
    /// without that specific data source.
    /// </remarks>
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
            new("residents", "Residents", cbs.Residents, "people", null, "CBS StatLine 85618NED"),
            new("population_density", "Population Density", cbs.PopulationDensity, "people/km²", densityScore, "CBS StatLine 85618NED"),
            new("low_income_households", "Low Income Households", cbs.LowIncomeHouseholdsPercent, "%", lowIncomeScore, "CBS StatLine 85618NED"),
            new("average_woz", "Average WOZ Value", cbs.AverageWozValueKeur, "k€", wozScore, "CBS StatLine 85618NED")
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
            new("age_0_14", "Age 0-14", demographics.PercentAge0To14, "%", null, "CBS StatLine 85618NED"),
            new("age_15_24", "Age 15-24", demographics.PercentAge15To24, "%", null, "CBS StatLine 85618NED"),
            new("age_25_44", "Age 25-44", demographics.PercentAge25To44, "%", null, "CBS StatLine 85618NED"),
            new("age_45_64", "Age 45-64", demographics.PercentAge45To64, "%", null, "CBS StatLine 85618NED"),
            new("age_65_plus", "Age 65+", demographics.PercentAge65Plus, "%", null, "CBS StatLine 85618NED"),
            new("avg_household_size", "Avg Household Size", demographics.AverageHouseholdSize, "people", null, "CBS StatLine 85618NED"),
            new("owner_occupied", "Owner-Occupied", demographics.PercentOwnerOccupied, "%", null, "CBS StatLine 85618NED"),
            new("single_households", "Single Households", demographics.PercentSingleHouseholds, "%", null, "CBS StatLine 85618NED"),
            new("family_friendly", "Family-Friendly Score", familyScore, "score", familyScore, "Valora Composite")
        ];
    }

    private static List<ContextMetricDto> BuildHousingMetrics(NeighborhoodStatsDto? cbs, List<string> warnings)
    {
        if (cbs is null) return [];

        return
        [
            new("housing_owner", "Owner-Occupied", cbs.PercentageOwnerOccupied, "%", null, "CBS StatLine 85618NED"),
            new("housing_rental", "Rental Properties", cbs.PercentageRental, "%", null, "CBS StatLine 85618NED"),
            new("housing_social", "Social Housing", cbs.PercentageSocialHousing, "%", null, "CBS StatLine 85618NED"),
            new("housing_pre2000", "Built Pre-2000", cbs.PercentagePre2000, "%", null, "CBS StatLine 85618NED"),
            new("housing_post2000", "Built Post-2000", cbs.PercentagePost2000, "%", null, "CBS StatLine 85618NED"),
            new("housing_multifamily", "Multi-Family Homes", cbs.PercentageMultiFamily, "%", null, "CBS StatLine 85618NED")
        ];
    }

    private static List<ContextMetricDto> BuildMobilityMetrics(NeighborhoodStatsDto? cbs, List<string> warnings)
    {
        if (cbs is null) return [];

        return
        [
            new("mobility_cars_household", "Cars per Household", cbs.CarsPerHousehold, "cars/hh", null, "CBS StatLine 85618NED"),
            new("mobility_car_density", "Car Density", cbs.CarDensity, "cars/km²", null, "CBS StatLine 85618NED"),
            new("mobility_total_cars", "Total Cars", cbs.TotalCars, "cars", null, "CBS StatLine 85618NED")
        ];
    }

    private static List<ContextMetricDto> BuildAmenityMetrics(AmenityStatsDto? amenities, NeighborhoodStatsDto? cbs, List<string> warnings)
    {
        var metrics = new List<ContextMetricDto>();

        if (amenities != null)
        {
            var proximityScore = ScoreAmenityProximity(amenities.NearestAmenityDistanceMeters);
            var countScore = ScoreAmenityCount(amenities);

            metrics.AddRange(
            [
                new("schools", "Schools in Radius", amenities.SchoolCount, "count", null, "OpenStreetMap / Overpass"),
                new("supermarkets", "Supermarkets in Radius", amenities.SupermarketCount, "count", null, "OpenStreetMap / Overpass"),
                new("parks", "Parks in Radius", amenities.ParkCount, "count", null, "OpenStreetMap / Overpass"),
                new("healthcare", "Healthcare in Radius", amenities.HealthcareCount, "count", null, "OpenStreetMap / Overpass"),
                new("transit_stops", "Transit Stops in Radius", amenities.TransitStopCount, "count", null, "OpenStreetMap / Overpass"),
                new("amenity_diversity", "Amenity Diversity", amenities.DiversityScore, "score", amenities.DiversityScore, "OpenStreetMap / Overpass"),
                new("amenity_proximity", "Nearest Amenity Distance", amenities.NearestAmenityDistanceMeters, "m", proximityScore, "OpenStreetMap / Overpass"),
                new("amenity_count_score", "Amenity Volume Score", countScore, "score", countScore, "OpenStreetMap / Overpass")
            ]);
        }
        else
        {
            warnings.Add("OSM amenities were unavailable; amenity score is partial.");
        }

        if (cbs != null)
        {
            // Phase 2: CBS Proximity - Walkability
            metrics.Add(new("dist_supermarket", "Dist. to Supermarket", cbs.DistanceToSupermarket, "km", ScoreProximity(cbs.DistanceToSupermarket, 1.0, 2.5), "CBS StatLine 85618NED"));
            metrics.Add(new("dist_gp", "Dist. to GP", cbs.DistanceToGp, "km", ScoreProximity(cbs.DistanceToGp, 1.5, 3.0), "CBS StatLine 85618NED"));
            metrics.Add(new("dist_school", "Dist. to School", cbs.DistanceToSchool, "km", ScoreProximity(cbs.DistanceToSchool, 1.0, 3.0), "CBS StatLine 85618NED"));
            metrics.Add(new("dist_daycare", "Dist. to Daycare", cbs.DistanceToDaycare, "km", null, "CBS StatLine 85618NED"));
            metrics.Add(new("schools_3km", "Schools within 3km", cbs.SchoolsWithin3km, "count", null, "CBS StatLine 85618NED"));
        }

        return metrics;
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
            sources.Add(new SourceAttributionDto("CBS StatLine 85618NED", "https://opendata.cbs.nl", "Publiek", cbs.RetrievedAtUtc));
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
        IReadOnlyList<ContextMetricDto> housingMetrics,
        IReadOnlyList<ContextMetricDto> mobilityMetrics,
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

        var housing = AverageScore(housingMetrics);
        if (housing.HasValue) scores["Housing"] = Math.Round(housing.Value, 1);

        var mobility = AverageScore(mobilityMetrics);
        if (mobility.HasValue) scores["Mobility"] = Math.Round(mobility.Value, 1);

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
            ["Safety"] = 0.20,
            ["Demographics"] = 0.10,
            ["Housing"] = 0.10,
            ["Mobility"] = 0.05,
            ["Amenities"] = 0.25,
            ["Environment"] = 0.10
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

    // Scoring functions - Heuristics based on Dutch urban planning standards

    /// <summary>
    /// Scores population density.
    /// Optimal density (~3500 people/km²) is preferred for access to amenities without overcrowding.
    /// </summary>
    private static double? ScoreDensity(int? density)
    {
        if (!density.HasValue) return null;

        return density.Value switch
        {
            <= 500 => 65,    // Rural / Isolated
            <= 1500 => 85,   // Suburban spacious
            <= 3500 => 100,  // Urban optimal
            <= 7000 => 70,   // Urban dense
            _ => 50          // Overcrowded
        };
    }

    /// <summary>
    /// Penalizes neighborhoods with a high percentage of low-income households.
    /// </summary>
    private static double? ScoreLowIncome(double? lowIncomePercent)
    {
        if (!lowIncomePercent.HasValue) return null;
        // Inverse linear relationship: 0% low income -> 100 score, 12.5% -> 0 score
        // The multiplier 8 is aggressive to highlight socio-economic challenges.
        return Math.Clamp(100 - (lowIncomePercent.Value * 8), 0, 100);
    }

    /// <summary>
    /// Scores WOZ value (property valuation).
    /// </summary>
    private static double? ScoreWoz(double? wozKeur)
    {
        if (!wozKeur.HasValue) return null;
        // Example: 450k -> (450-150)/3 = 100 score. 150k -> 0 score.
        return Math.Clamp((wozKeur.Value - 150) / 3, 0, 100);
    }

    /// <summary>
    /// Scores total crime incidents per 1000 residents.
    /// Based on CBS data where national average fluctuates around 45-50.
    /// </summary>
    private static double? ScoreTotalCrime(int? crimesPer1000)
    {
        if (!crimesPer1000.HasValue) return null;

        // Lower crime is better - Dutch average is around 50 per 1000
        return crimesPer1000.Value switch
        {
            <= 20 => 100, // Very Safe
            <= 35 => 85,  // Safe
            <= 50 => 70,  // Average
            <= 75 => 50,  // Below Average
            <= 100 => 30, // Unsafe
            _ => 15       // Very Unsafe
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

    /// <summary>
    /// Calculates a family-friendly score based on demographics.
    /// Factors in household composition, presence of children, and household size.
    /// </summary>
    private static double? ScoreFamilyFriendly(DemographicsDto demographics)
    {
        // Composite score based on presence of families and children
        double score = 50; // Start with neutral baseline

        if (demographics.PercentFamilyHouseholds.HasValue)
            score += (demographics.PercentFamilyHouseholds.Value - 20) * 1.5; // Boost if family households > 20%

        if (demographics.PercentAge0To14.HasValue)
            score += (demographics.PercentAge0To14.Value - 15) * 2; // Boost if children > 15%

        if (demographics.AverageHouseholdSize.HasValue)
            score += (demographics.AverageHouseholdSize.Value - 2) * 15; // Larger households indicate families

        return Math.Clamp(score, 0, 100);
    }

    /// <summary>
    /// Scores the volume of amenities in the search radius.
    /// Simple quantity heuristic: 20 amenities = 100 score.
    /// </summary>
    private static double ScoreAmenityCount(AmenityStatsDto amenities)
    {
        var total = amenities.SchoolCount + amenities.SupermarketCount + amenities.ParkCount + amenities.HealthcareCount + amenities.TransitStopCount;
        return Math.Clamp(total * 5, 0, 100);
    }

    /// <summary>
    /// Scores the "15-minute city" potential based on proximity to the nearest key amenity.
    /// </summary>
    private static double? ScoreAmenityProximity(double? nearestDistanceMeters)
    {
        if (!nearestDistanceMeters.HasValue) return null;

        return nearestDistanceMeters.Value switch
        {
            <= 250 => 100, // Very Walkable
            <= 500 => 85,  // Walkable
            <= 1000 => 70, // Bikeable
            <= 1500 => 55, // Short Drive
            <= 2000 => 40, // Drive
            _ => 25        // Isolated
        };
    }

    /// <summary>
    /// Scores air quality based on PM2.5 concentration.
    /// WHO guideline is < 5 µg/m³.
    /// </summary>
    private static double? ScorePm25(double? pm25)
    {
        if (!pm25.HasValue) return null;

        return pm25.Value switch
        {
            <= 5 => 100, // Excellent (WHO Goal)
            <= 10 => 85, // Good
            <= 15 => 70, // Moderate
            <= 25 => 50, // Poor (EU Limit)
            <= 35 => 25, // Unhealthy
            _ => 10      // Hazardous
        };
    }
    /// <summary>
    /// Scores proximity to key amenities (Supermarket, GP, School).
    /// </summary>
    private static double? ScoreProximity(double? distanceKm, double optimalKm, double acceptableKm)
    {
        if (!distanceKm.HasValue) return null;

        if (distanceKm <= optimalKm) return 100;
        if (distanceKm <= acceptableKm) return 70;
        return 40;
    }
}
