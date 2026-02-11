file_path = 'backend/Valora.Infrastructure/Enrichment/ContextReportService.cs'

with open(file_path, 'r') as f:
    content = f.read()

# Fix BuildHousingMetrics
# Old pattern (expression body)
old_housing = """    private static List<ContextMetricDto> BuildHousingMetrics(NeighborhoodStatsDto? cbs, FoundationRiskDto? soil, List<string> warnings)
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
    }"""

new_housing = """    private static List<ContextMetricDto> BuildHousingMetrics(NeighborhoodStatsDto? cbs, FoundationRiskDto? soil, List<string> warnings)
    {
        var metrics = new List<ContextMetricDto>();

        if (cbs != null)
        {
            metrics.Add(new("housing_owner", "Owner-Occupied", cbs.PercentageOwnerOccupied, "%", null, "CBS StatLine 85618NED"));
            metrics.Add(new("housing_rental", "Rental Properties", cbs.PercentageRental, "%", null, "CBS StatLine 85618NED"));
            metrics.Add(new("housing_social", "Social Housing", cbs.PercentageSocialHousing, "%", null, "CBS StatLine 85618NED"));
            metrics.Add(new("housing_pre2000", "Built Pre-2000", cbs.PercentagePre2000, "%", null, "CBS StatLine 85618NED"));
            metrics.Add(new("housing_post2000", "Built Post-2000", cbs.PercentagePost2000, "%", null, "CBS StatLine 85618NED"));
            metrics.Add(new("housing_multifamily", "Multi-Family Homes", cbs.PercentageMultiFamily, "%", null, "CBS StatLine 85618NED"));
        }

        if (soil != null)
        {
            metrics.Add(new("foundation_risk", "Foundation Risk", null, soil.RiskLevel, ScoreFoundation(soil.RiskLevel), "PDOK Bodemkaart", soil.Description));
            metrics.Add(new("soil_type", "Soil Type", null, soil.SoilType, null, "PDOK Bodemkaart"));
        }

        return metrics;
    }"""

content = content.replace(old_housing, new_housing)

# Fix BuildEnvironmentMetrics
# Wait, I already tried to replace it in previous script, but maybe it failed to match due to previous edits or whitespace.
# Let's inspect the current state of BuildEnvironmentMetrics.
# I'll rely on the fact that I replaced the *start* of the method signature.
# "private static List<ContextMetricDto> BuildEnvironmentMetrics(AirQualitySnapshotDto? air, SolarPotentialDto? solar, List<string> warnings)"
# But the previous script's  might have failed.

# Let's just find the entire block and replace it.
# The previous script might have left it in a broken state if only signature changed.
# The signature *did* change (grep output showed changes).

# I will define the new Environment method completely and replace the existing one by matching signature + body start.
# I need to know what the current body looks like.
# Assuming it wasn't changed by previous script because the match string was long and possibly mismatched whitespace.

# I'll just append the Scoring methods if they are missing (Step 12 of previous script used  which usually works).
# I'll check if  exists.

if "ScoreFoundation" not in content:
    # Append scoring methods before last closing brace
    last_brace_index = content.rfind("}")
    if last_brace_index != -1:
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
    }
"""
        content = content[:last_brace_index] + new_scores + content[last_brace_index:]

with open(file_path, 'w') as f:
    f.write(content)
