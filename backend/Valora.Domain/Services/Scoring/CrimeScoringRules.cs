namespace Valora.Domain.Services.Scoring;

/// <summary>
/// Domain rules for calculating safety scores based on crime statistics.
/// </summary>
public static class CrimeScoringRules
{
    /// <summary>
    /// Scores total crime incidents per 1000 residents.
    /// </summary>
    /// <remarks>
    /// Based on CBS data where national average fluctuates around 45-50.
    /// Scores are bucketed to provide clear safety tiers (Very Safe, Safe, Average, etc.).
    /// </remarks>
    public static double? ScoreTotalCrime(int? crimesPer1000)
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
    public static double? ScoreBurglary(int? burglaryPer1000)
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
    public static double? ScoreViolentCrime(int? violentPer1000)
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
