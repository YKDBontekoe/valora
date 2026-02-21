namespace Valora.Application.DTOs.Map;

public record MapQueryRequest(
    string Prompt,
    MapBoundsDto? CurrentBounds
);

public record MapQueryResultDto(
    string Explanation,
    MapLocationDto? TargetLocation = null,
    MapFilterDto? Filter = null
);

public record MapLocationDto(
    double Lat,
    double Lon,
    double Zoom
);

public record MapFilterDto(
    MapOverlayMetric? Metric,
    List<string>? AmenityTypes
);
