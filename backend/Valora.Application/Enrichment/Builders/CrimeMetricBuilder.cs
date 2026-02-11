using Valora.Application.DTOs;

namespace Valora.Application.Enrichment.Builders;

public static class CrimeMetricBuilder
{
    public static List<ContextMetricDto> Build(CrimeStatsDto? crime, List<string> warnings)
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

    /// <summary>
    /// Scores total crime incidents per 1000 residents.
    /// </summary>
    /// <remarks>
    /// Based on CBS data where national average fluctuates around 45-50.
    /// Scores are bucketed to provide clear safety tiers (Very Safe, Safe, Average, etc.).
    /// </remarks>
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

    /// <summary>
    /// Scores burglary rate per 1000 residents.
    /// </summary>
    /// <remarks>
    /// Burglary is a high-impact crime for residents.
    /// </remarks>
    private static double? ScoreBurglary(int? burglaryPer1000)
    {
        if (!burglaryPer1000.HasValue) return null;

        return burglaryPer1000.Value switch
        {
            <= 2 => 100, // Rare
            <= 5 => 80,  // Low
            <= 10 => 60, // Moderate
            <= 15 => 40, // High
            _ => 20      // Very High
        };
    }

    /// <summary>
    /// Scores violent crime rate per 1000 residents.
    /// </summary>
    /// <remarks>
    /// Violent crime has a severe impact on perceived safety. The thresholds are much stricter than for total crime.
    /// </remarks>
    private static double? ScoreViolentCrime(int? violentPer1000)
    {
        if (!violentPer1000.HasValue) return null;

        return violentPer1000.Value switch
        {
            <= 2 => 100, // Very Rare
            <= 5 => 75,  // Low
            <= 10 => 50, // Moderate
            _ => 25      // High
        };
    }
}
