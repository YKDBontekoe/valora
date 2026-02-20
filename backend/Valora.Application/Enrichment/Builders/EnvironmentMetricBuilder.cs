using Valora.Application.DTOs;
using Valora.Domain.Services.Scoring;

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

        var pm25Score = EnvironmentScoringRules.ScorePm25(air.Pm25);
        var pm10Score = EnvironmentScoringRules.ScorePm10(air.Pm10);
        var no2Score = EnvironmentScoringRules.ScoreNo2(air.No2);
        var o3Score = EnvironmentScoringRules.ScoreO3(air.O3);

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
}
