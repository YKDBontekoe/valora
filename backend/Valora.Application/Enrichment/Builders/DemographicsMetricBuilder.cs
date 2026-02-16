using Valora.Application.DTOs;
using Valora.Application.Enrichment.Scoring;

namespace Valora.Application.Enrichment.Builders;

public static class DemographicsMetricBuilder
{
    private const string SourceCbs = "CBS StatLine 85618NED";
    private const string SourceValora = "Valora Composite";

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

        var familyScore = DemographicsScorer.ScoreFamilyFriendly(pFamily, p0_14, cbs.AverageHouseholdSize);
        var incomeScore = DemographicsScorer.ScoreIncome(cbs.AverageIncomePerInhabitant);
        var educationScore = DemographicsScorer.ScoreEducation(cbs.EducationLow, cbs.EducationMedium, cbs.EducationHigh);
        var urbanityScore = DemographicsScorer.ScoreUrbanity(cbs.Urbanity);

        return
        [
            new("age_0_14", "Age 0-14", p0_14, "%", null, SourceCbs),
            new("age_15_24", "Age 15-24", p15_24, "%", null, SourceCbs),
            new("age_25_44", "Age 25-44", p25_44, "%", null, SourceCbs),
            new("age_45_64", "Age 45-64", p45_64, "%", null, SourceCbs),
            new("age_65_plus", "Age 65+", p65Plus, "%", null, SourceCbs),
            new("avg_household_size", "Avg Household Size", cbs.AverageHouseholdSize, "people", null, SourceCbs),
            new("owner_occupied", "Owner-Occupied", cbs.PercentageOwnerOccupied, "%", null, SourceCbs),
            new("single_households", "Single Households", pSingle, "%", null, SourceCbs),
            new("income_per_inhabitant", "Avg Income per Inhabitant", cbs.AverageIncomePerInhabitant, "k€/year", incomeScore, SourceCbs),
            new("education_high_share", "Higher Education Share", DemographicsScorer.ToPercent(cbs.EducationHigh, cbs.EducationLow, cbs.EducationMedium), "%", educationScore, SourceCbs),
            new("urbanity_level", "Urbanity Level", DemographicsScorer.ParseUrbanityLevel(cbs.Urbanity), "level", urbanityScore, SourceCbs),
            new("family_friendly", "Family-Friendly Score", familyScore, "score", familyScore, SourceValora)
        ];
    }

    private static double? CalculatePercent(int? count, int? total)
    {
        if (!count.HasValue || !total.HasValue || total.Value == 0) return null;
        return Math.Round((double)count.Value / total.Value * 100, 1);
    }
}
