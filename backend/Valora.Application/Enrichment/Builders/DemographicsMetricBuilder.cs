using Valora.Application.DTOs;

namespace Valora.Application.Enrichment.Builders;

public static class DemographicsMetricBuilder
{
    public static List<ContextMetricDto> Build(DemographicsDto? demographics, List<string> warnings)
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

    /// <summary>
    /// Calculates a family-friendly score based on demographics.
    /// </summary>
    /// <remarks>
    /// Factors in:
    /// - Percentage of family households (>20% boosts score)
    /// - Percentage of children 0-14 (>15% boosts score)
    /// - Average household size (>2 people boosts score)
    /// </remarks>
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
}
