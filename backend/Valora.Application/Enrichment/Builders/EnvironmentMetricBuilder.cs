using Valora.Application.DTOs;

namespace Valora.Application.Enrichment.Builders;

public static class EnvironmentMetricBuilder
{
    public static List<ContextMetricDto> Build(AirQualitySnapshotDto? air, List<string> warnings)
    {
        if (air is null)
        {
            warnings.Add("Air quality source was unavailable; environment score is partial.");
            return [];
        }

        var pm25Score = ScorePm25(air.Pm25);
        var pm10Score = ScorePm10(air.Pm10);
        var no2Score = ScoreNo2(air.No2);
        var o3Score = ScoreO3(air.O3);

        return
        [
            new("pm25", "PM2.5", air.Pm25, "µg/m³", pm25Score, "Luchtmeetnet Open API"),
            new("pm10", "PM10", air.Pm10, "µg/m³", pm10Score, "Luchtmeetnet Open API"),
            new("no2", "NO2", air.No2, "µg/m³", no2Score, "Luchtmeetnet Open API"),
            new("o3", "O3", air.O3, "µg/m³", o3Score, "Luchtmeetnet Open API"),
            new("air_station", "Nearest Station", null, null, null, "Luchtmeetnet Open API", air.StationName),
            new("air_station_distance", "Distance to Station", air.StationDistanceMeters, "m", null, "Luchtmeetnet Open API")
        ];
    }

    /// <summary>
    /// Scores air quality based on PM2.5 concentration.
    /// </summary>
    /// <remarks>
    /// Reference: WHO guideline is &lt; 5 µg/m³.
    /// EU limit is 25 µg/m³.
    /// </remarks>
    private static double? ScorePm25(double? pm25)
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

    private static double? ScorePm10(double? pm10)
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

    private static double? ScoreNo2(double? no2)
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

    private static double? ScoreO3(double? o3)
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
