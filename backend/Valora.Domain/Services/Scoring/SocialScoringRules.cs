namespace Valora.Domain.Services.Scoring;

/// <summary>
/// Domain rules for calculating social scores based on neighborhood statistics.
/// </summary>
public static class SocialScoringRules
{
    private const int DensityRural = 500;
    private const int DensitySuburban = 1500;
    private const int DensityOptimal = 3500;
    private const int DensityDense = 7000;

    private const double LowIncomePenaltyMultiplier = 8.0;

    private const double WozBaseline = 150.0;
    private const double WozDivider = 3.0;

    /// <summary>
    /// Scores population density.
    /// Optimal density (~3500 people/km²) is preferred for access to amenities without overcrowding.
    /// </summary>
    public static double? ScoreDensity(int? density)
    {
        if (!density.HasValue) return null;

        return density.Value switch
        {
            <= DensityRural => 65,     // Rural / Isolated - good for tranquility but bad for access
            <= DensitySuburban => 85,  // Suburban spacious - balanced
            <= DensityOptimal => 100,  // Urban optimal - highly walkable
            <= DensityDense => 70,     // Urban dense - can be noisy
            _ => 50                    // Overcrowded
        };
    }

    /// <summary>
    /// Penalizes neighborhoods with a high percentage of low-income households.
    /// </summary>
    /// <remarks>
    /// This metric is a proxy for socio-economic stability.
    /// 0% low income = 100 score. 12.5% low income = 0 score.
    /// The steep penalty (8x multiplier) is designed to highlight areas with concentrated poverty.
    /// </remarks>
    public static double? ScoreLowIncome(double? lowIncomePercent)
    {
        if (!lowIncomePercent.HasValue) return null;
        // Inverse linear relationship: 0% low income -> 100 score, 12.5% -> 0 score
        // The multiplier 8 is aggressive to highlight socio-economic challenges.
        return Math.Clamp(100 - (lowIncomePercent.Value * LowIncomePenaltyMultiplier), 0, 100);
    }

    /// <summary>
    /// Scores WOZ value (property valuation).
    /// </summary>
    /// <remarks>
    /// Higher property values generally correlate with better neighborhood maintenance and services.
    /// Baseline: 150k (0 score). Target: 450k (100 score).
    /// </remarks>
    public static double? ScoreWoz(double? wozKeur)
    {
        if (!wozKeur.HasValue) return null;
        // Example: 450k -> (450-150)/3 = 100 score. 150k -> 0 score.
        return Math.Clamp((wozKeur.Value - WozBaseline) / WozDivider, 0, 100);
    }
}
