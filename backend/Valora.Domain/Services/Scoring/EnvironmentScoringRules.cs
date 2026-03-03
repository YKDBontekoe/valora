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

    /// <summary>
    /// Scores air quality based on PM10 concentration.
    /// </summary>
    /// <remarks>
    /// Reference: WHO guideline is &lt; 15 µg/m³.
    /// EU limit is 40 µg/m³ (annual mean).
    /// </remarks>
    public static double? ScorePm10(double? pm10)
    {
        if (!pm10.HasValue) return null;

        return pm10.Value switch
        {
            <= 15 => 100, // Excellent (WHO Goal)
            <= 25 => 85,  // Good
            <= 35 => 70,  // Moderate
            <= 45 => 50,  // Poor (EU Limit territory)
            <= 60 => 30,  // Unhealthy
            _ => 15       // Hazardous
        };
    }

    /// <summary>
    /// Scores air quality based on NO2 concentration.
    /// </summary>
    /// <remarks>
    /// Reference: WHO guideline is &lt; 10 µg/m³ (annual mean).
    /// EU limit is 40 µg/m³. Nitrogen dioxide is primarily emitted by traffic.
    /// </remarks>
    public static double? ScoreNo2(double? no2)
    {
        if (!no2.HasValue) return null;

        return no2.Value switch
        {
            <= 20 => 100, // Excellent/Good (Approaching WHO Goal)
            <= 30 => 85,  // Moderate
            <= 40 => 70,  // Acceptable (Approaching EU Limit)
            <= 60 => 50,  // Poor
            <= 80 => 30,  // Unhealthy
            _ => 15       // Hazardous
        };
    }

    /// <summary>
    /// Scores air quality based on O3 (Ozone) concentration.
    /// </summary>
    /// <remarks>
    /// Reference: WHO guideline is &lt; 60 µg/m³ (peak season).
    /// EU limit (target value) is 120 µg/m³.
    /// </remarks>
    public static double? ScoreO3(double? o3)
    {
        if (!o3.HasValue) return null;

        return o3.Value switch
        {
            <= 60 => 100, // Excellent (WHO Goal)
            <= 90 => 85,  // Good
            <= 120 => 70, // Moderate (Approaching EU Limit)
            <= 150 => 50, // Poor
            <= 180 => 30, // Unhealthy
            _ => 15       // Hazardous
        };
    }
}
