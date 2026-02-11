using Valora.Application.DTOs;

namespace Valora.Application.Enrichment.Builders;

public static class DemographicsMetricBuilder
{
    public static List<ContextMetricDto> Build(NeighborhoodStatsDto? cbs, List<string> warnings)
    {
        if (cbs is null)
        {
            warnings.Add("CBS demographics were unavailable; demographics score is partial.");
            return [];
        }

        // Calculate Age Percentages from absolute counts
        double? p0_14 = CalculatePercent(cbs.Age0To15, cbs.Residents);
        double? p15_24 = CalculatePercent(cbs.Age15To25, cbs.Residents);
        double? p25_44 = CalculatePercent(cbs.Age25To45, cbs.Residents);
        double? p45_64 = CalculatePercent(cbs.Age45To65, cbs.Residents);
        double? p65Plus = CalculatePercent(cbs.Age65Plus, cbs.Residents);

        // Calculate Household Percentages
        // Total households is the sum of single, without children, and with children households.
        // If any component is missing, we cannot reliably compute the total or percentages.
        int? totalHouseholds = null;
        if (cbs.SingleHouseholds.HasValue &&
            cbs.HouseholdsWithoutChildren.HasValue &&
            cbs.HouseholdsWithChildren.HasValue)
        {
            totalHouseholds = cbs.SingleHouseholds.Value +
                              cbs.HouseholdsWithoutChildren.Value +
                              cbs.HouseholdsWithChildren.Value;

            if (totalHouseholds == 0) totalHouseholds = null;
        }

        double? pSingle = CalculatePercent(cbs.SingleHouseholds, totalHouseholds);

        // "Family Households" in CBS context includes both households with and without children (couples).
        int? familyHouseholdsCount = null;
        if (cbs.HouseholdsWithoutChildren.HasValue && cbs.HouseholdsWithChildren.HasValue)
        {
            familyHouseholdsCount = cbs.HouseholdsWithoutChildren.Value + cbs.HouseholdsWithChildren.Value;
        }
        double? pFamily = CalculatePercent(familyHouseholdsCount, totalHouseholds);

        var familyScore = ScoreFamilyFriendly(pFamily, p0_14, cbs.AverageHouseholdSize);

        return
        [
            new("age_0_14", "Age 0-14", p0_14, "%", null, "CBS StatLine 85618NED"),
            new("age_15_24", "Age 15-24", p15_24, "%", null, "CBS StatLine 85618NED"),
            new("age_25_44", "Age 25-44", p25_44, "%", null, "CBS StatLine 85618NED"),
            new("age_45_64", "Age 45-64", p45_64, "%", null, "CBS StatLine 85618NED"),
            new("age_65_plus", "Age 65+", p65Plus, "%", null, "CBS StatLine 85618NED"),
            new("avg_household_size", "Avg Household Size", cbs.AverageHouseholdSize, "people", null, "CBS StatLine 85618NED"),
            new("owner_occupied", "Owner-Occupied", cbs.PercentageOwnerOccupied, "%", null, "CBS StatLine 85618NED"),
            new("single_households", "Single Households", pSingle, "%", null, "CBS StatLine 85618NED"),
            new("family_friendly", "Family-Friendly Score", familyScore, "score", familyScore, "Valora Composite")
        ];
    }

    private static double? CalculatePercent(int? count, int? total)
    {
        if (!count.HasValue || !total.HasValue || total.Value == 0) return null;
        return Math.Round((double)count.Value / total.Value * 100, 1);
    }

    /// <summary>
    /// Calculates a family-friendly score based on demographics.
    /// </summary>
    /// <remarks>
    /// Factors in:
    /// - Percentage of family households (with or without children) (>20% boosts score)
    /// - Percentage of children 0-14 (>15% boosts score)
    /// - Average household size (>2 people boosts score)
    /// </remarks>
    private static double? ScoreFamilyFriendly(double? percentFamilyHouseholds, double? percentAge0To14, double? averageHouseholdSize)
    {
        // If no relevant data is present, do not return a phantom score.
        if (!percentFamilyHouseholds.HasValue &&
            !percentAge0To14.HasValue &&
            !averageHouseholdSize.HasValue)
        {
            return null;
        }

        // Composite score based on presence of families and children
        double score = 50; // Start with neutral baseline

        if (percentFamilyHouseholds.HasValue)
            score += (percentFamilyHouseholds.Value - 20) * 1.5; // Boost if family households > 20%

        if (percentAge0To14.HasValue)
            score += (percentAge0To14.Value - 15) * 2; // Boost if children > 15%

        if (averageHouseholdSize.HasValue)
            score += (averageHouseholdSize.Value - 2) * 15; // Larger households indicate families

        return Math.Clamp(score, 0, 100);
    }
}
