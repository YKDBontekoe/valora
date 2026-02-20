namespace Valora.Domain.Services.Scoring;

/// <summary>
/// Domain rules for calculating environment scores based on air quality metrics.
/// </summary>
public static class EnvironmentScoringRules
{
    /// <summary>
    /// Scores air quality based on PM2.5 concentration.
    /// </summary>
    /// <remarks>
    /// Reference: WHO guideline is &lt; 5 µg/m³.
    /// EU limit is 25 µg/m³.
    /// </remarks>
    public static double? ScorePm25(double? pm25)
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

    public static double? ScorePm10(double? pm10)
    {
        if (!pm10.HasValue) return null;

        return pm10.Value switch
        {
            <= 15 => 100,
            <= 25 => 85,
            <= 35 => 70,
            <= 45 => 50,
            <= 60 => 30,
            _ => 15
        };
    }

    public static double? ScoreNo2(double? no2)
    {
        if (!no2.HasValue) return null;

        return no2.Value switch
        {
            <= 20 => 100,
            <= 30 => 85,
            <= 40 => 70,
            <= 60 => 50,
            <= 80 => 30,
            _ => 15
        };
    }

    public static double? ScoreO3(double? o3)
    {
        if (!o3.HasValue) return null;

        return o3.Value switch
        {
            <= 60 => 100,
            <= 90 => 85,
            <= 120 => 70,
            <= 150 => 50,
            <= 180 => 30,
            _ => 15
        };
    }
}
