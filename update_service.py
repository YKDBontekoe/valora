import re

file_path = 'backend/Valora.Infrastructure/Enrichment/ContextReportService.cs'

with open(file_path, 'r') as f:
    content = f.read()

# 1. Add fields
fields_injection = """    private readonly IAmenityClient _amenityClient;
    private readonly IAirQualityClient _airQualityClient;
    private readonly IPdokSoilClient _soilClient;
    private readonly IPdokBuildingClient _buildingClient;
    private readonly IMemoryCache _cache;"""

content = content.replace(
    "private readonly IAmenityClient _amenityClient;\n    private readonly IAirQualityClient _airQualityClient;\n    private readonly IMemoryCache _cache;",
    fields_injection
)

# 2. Update Constructor Signature
ctor_sig_injection = """        IAmenityClient amenityClient,
        IAirQualityClient airQualityClient,
        IPdokSoilClient soilClient,
        IPdokBuildingClient buildingClient,
        IMemoryCache cache,"""

content = content.replace(
    "IAmenityClient amenityClient,\n        IAirQualityClient airQualityClient,\n        IMemoryCache cache,",
    ctor_sig_injection
)

# 3. Update Constructor Body
ctor_body_injection = """        _amenityClient = amenityClient;
        _airQualityClient = airQualityClient;
        _soilClient = soilClient;
        _buildingClient = buildingClient;
        _cache = cache;"""

content = content.replace(
    "_amenityClient = amenityClient;\n        _airQualityClient = airQualityClient;\n        _cache = cache;",
    ctor_body_injection
)

# 4. Add Tasks in BuildAsync
tasks_match = 'var airQualityTask = TryGetSourceAsync("Luchtmeetnet", token => _airQualityClient.GetSnapshotAsync(location, token), cancellationToken);'
tasks_injection = """var airQualityTask = TryGetSourceAsync("Luchtmeetnet", token => _airQualityClient.GetSnapshotAsync(location, token), cancellationToken);
        var soilTask = TryGetSourceAsync("PDOK Soil", token => _soilClient.GetFoundationRiskAsync(location, token), cancellationToken);
        var solarTask = TryGetSourceAsync("PDOK Building", token => _buildingClient.GetSolarPotentialAsync(location, token), cancellationToken);"""

content = content.replace(tasks_match, tasks_injection)

# 5. Await Tasks
await_match = 'await Task.WhenAll(cbsTask, crimeTask, demographicsTask, amenitiesTask, airQualityTask);'
await_injection = 'await Task.WhenAll(cbsTask, crimeTask, demographicsTask, amenitiesTask, airQualityTask, soilTask, solarTask);'

content = content.replace(await_match, await_injection)

# 6. Get Results
results_match = 'var air = await airQualityTask;'
results_injection = """var air = await airQualityTask;
        var soil = await soilTask;
        var solar = await solarTask;"""

content = content.replace(results_match, results_injection)

# 7. Update Build calls
content = content.replace(
    'var housingMetrics = BuildHousingMetrics(cbs, warnings);',
    'var housingMetrics = BuildHousingMetrics(cbs, soil, warnings);'
)

content = content.replace(
    'var environmentMetrics = BuildEnvironmentMetrics(air, warnings);',
    'var environmentMetrics = BuildEnvironmentMetrics(air, solar, warnings);'
)

# 8. Update Sources Build call
content = content.replace(
    'var sources = BuildSourceAttributions(cbs, crime, demographics, amenities, air);',
    'var sources = BuildSourceAttributions(cbs, crime, demographics, amenities, air, soil, solar);'
)

# 9. Update BuildHousingMetrics method
housing_method_match = 'private static List<ContextMetricDto> BuildHousingMetrics(NeighborhoodStatsDto? cbs, List<string> warnings)'
housing_method_replace = 'private static List<ContextMetricDto> BuildHousingMetrics(NeighborhoodStatsDto? cbs, FoundationRiskDto? soil, List<string> warnings)'
content = content.replace(housing_method_match, housing_method_replace)

# Inject Soil logic into BuildHousingMetrics (before return metrics)
housing_logic_match = '    private static List<ContextMetricDto> BuildHousingMetrics(NeighborhoodStatsDto? cbs, FoundationRiskDto? soil, List<string> warnings)\n    {'
housing_logic_injection = """    private static List<ContextMetricDto> BuildHousingMetrics(NeighborhoodStatsDto? cbs, FoundationRiskDto? soil, List<string> warnings)
    {
        var metrics = new List<ContextMetricDto>();"""

# Remove the old "var metrics = new List..." line inside the method if simpler to just prepend
# Actually, the method likely starts with
# I'll just append to the list before return.

# Regex to find the end of BuildHousingMetrics and insert soil logic
# Look for: return metrics; }
# Warning: multiple methods return metrics.
# BuildHousingMetrics ends around line 300.
# I will use a more specific replace.

soil_logic = """
        if (soil != null)
        {
            metrics.Add(new ContextMetricDto("foundation_risk", "Foundation Risk", null, soil.RiskLevel, ScoreFoundation(soil.RiskLevel), "PDOK Bodemkaart", soil.Description));
            metrics.Add(new ContextMetricDto("soil_type", "Soil Type", null, soil.SoilType, null, "PDOK Bodemkaart"));
        }
"""
# Insert before the return of BuildHousingMetrics
# I need to find the specific return of BuildHousingMetrics.
# It ends with:
#             metrics.Add(new("schools_3km", "Schools within 3km", cbs.SchoolsWithin3km, "count", null, "CBS StatLine 85618NED"));
#         }
#
#         return metrics;
#     }

housing_end_anchor = 'metrics.Add(new("schools_3km", "Schools within 3km", cbs.SchoolsWithin3km, "count", null, "CBS StatLine 85618NED"));'
content = content.replace(
    housing_end_anchor,
    housing_end_anchor + soil_logic
)

# 10. Update BuildEnvironmentMetrics method
env_method_match = 'private static List<ContextMetricDto> BuildEnvironmentMetrics(AirQualitySnapshotDto? air, List<string> warnings)'
env_method_replace = 'private static List<ContextMetricDto> BuildEnvironmentMetrics(AirQualitySnapshotDto? air, SolarPotentialDto? solar, List<string> warnings)'
content = content.replace(env_method_match, env_method_replace)

# Replace the body start to handle null air but continue to solar
env_body_start_match = """    {
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
    }"""

env_body_new = """    {
        var metrics = new List<ContextMetricDto>();

        if (air != null)
        {
            var pm25Score = ScorePm25(air.Pm25);
            metrics.Add(new("pm25", "PM2.5", air.Pm25, "µg/m³", pm25Score, "Luchtmeetnet Open API"));
            metrics.Add(new("air_station", "Nearest Station", null, null, null, "Luchtmeetnet Open API", air.StationName));
            metrics.Add(new("air_station_distance", "Distance to Station", air.StationDistanceMeters, "m", null, "Luchtmeetnet Open API"));
        }
        else
        {
             warnings.Add("Air quality source was unavailable.");
        }

        if (solar != null)
        {
            metrics.Add(new("solar_potential", "Solar Potential", solar.EstimatedGenerationKwh, "kWh/yr", ScoreSolar(solar.Potential), "PDOK 3D BAG", $"Based on {solar.RoofAreaM2}m² roof area"));
            metrics.Add(new("solar_panels", "Est. Panels", solar.InstallablePanels, "count", null, "PDOK 3D BAG"));
        }

        return metrics;
    }"""

content = content.replace(env_body_start_match, env_body_new)


# 11. Update BuildSourceAttributions
sources_sig_match = """    private static List<SourceAttributionDto> BuildSourceAttributions(
        NeighborhoodStatsDto? cbs,
        CrimeStatsDto? crime,
        DemographicsDto? demographics,
        AmenityStatsDto? amenities,
        AirQualitySnapshotDto? air)"""

sources_sig_replace = """    private static List<SourceAttributionDto> BuildSourceAttributions(
        NeighborhoodStatsDto? cbs,
        CrimeStatsDto? crime,
        DemographicsDto? demographics,
        AmenityStatsDto? amenities,
        AirQualitySnapshotDto? air,
        FoundationRiskDto? soil,
        SolarPotentialDto? solar)"""

content = content.replace(sources_sig_match, sources_sig_replace)

sources_logic_anchor = 'sources.Add(new SourceAttributionDto("Luchtmeetnet", "https://api.luchtmeetnet.nl", "Publiek", air.RetrievedAtUtc));\n        }'
sources_logic_append = """

        if (soil is not null)
        {
            sources.Add(new SourceAttributionDto("PDOK BRO Bodemkaart", "https://service.pdok.nl", "CC-BY", soil.RetrievedAtUtc));
        }

        if (solar is not null)
        {
            sources.Add(new SourceAttributionDto("PDOK BAG", "https://service.pdok.nl", "CC-BY", solar.RetrievedAtUtc));
        }"""

content = content.replace(sources_logic_anchor, sources_logic_anchor + sources_logic_append)

# 12. Add Scoring Methods
# Append to the end of class (before the last closing brace)
# I'll look for the last method "ScoreProximity" and append after it.

score_proximity_anchor = """    private static double? ScoreProximity(double? distanceKm, double optimalKm, double acceptableKm)
    {
        if (!distanceKm.HasValue) return null;

        if (distanceKm <= optimalKm) return 100;
        if (distanceKm <= acceptableKm) return 70;
        return 40;
    }"""

new_scores = """
    private static double? ScoreFoundation(string riskLevel)
    {
        return riskLevel.ToLowerInvariant() switch
        {
            "low" => 100,
            "medium" => 60,
            "high" => 30,
            _ => null
        };
    }

    private static double? ScoreSolar(string potential)
    {
        return potential.ToLowerInvariant() switch
        {
            "high" => 100,
            "medium" => 75,
            "low" => 50,
            _ => null
        };
    }"""

content = content.replace(score_proximity_anchor, score_proximity_anchor + new_scores)

with open(file_path, 'w') as f:
    f.write(content)

print("ContextReportService.cs updated successfully.")
