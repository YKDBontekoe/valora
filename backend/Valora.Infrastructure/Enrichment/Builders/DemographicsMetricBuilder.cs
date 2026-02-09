using Valora.Application.DTOs;

namespace Valora.Infrastructure.Enrichment.Builders;

public class DemographicsMetricBuilder
{
    public List<ContextMetricDto> Build(DemographicsDto? demographics, List<string> warnings)
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
}
