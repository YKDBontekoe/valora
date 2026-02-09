using Valora.Application.DTOs;

namespace Valora.Infrastructure.Enrichment.Builders;

public class EnvironmentMetricBuilder
{
    public List<ContextMetricDto> Build(AirQualitySnapshotDto? air, List<string> warnings)
    {
        if (air is null)
        {
            warnings.Add("Air quality source was unavailable; environment score is partial.");
            return [];
        }

        var pm25Score = ScorePm25(air.Pm25);

        return
        [
            new("pm25", "PM2.5", air.Pm25, "µg/m³", pm25Score, "Luchtmeetnet Open API"),
            new("air_station", "Nearest Station", null, null, null, "Luchtmeetnet Open API", air.StationName),
            new("air_station_distance", "Distance to Station", air.StationDistanceMeters, "m", null, "Luchtmeetnet Open API")
        ];
    }

    private static double? ScorePm25(double? pm25)
    {
        if (!pm25.HasValue) return null;

        return pm25.Value switch
        {
            <= 5 => 100,
            <= 10 => 85,
            <= 15 => 70,
            <= 25 => 50,
            <= 35 => 25,
            _ => 10
        };
    }
}
