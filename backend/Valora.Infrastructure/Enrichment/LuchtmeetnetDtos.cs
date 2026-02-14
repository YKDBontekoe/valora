using System.Text.Json.Serialization;

namespace Valora.Infrastructure.Enrichment;

internal sealed record LuchtmeetnetStationListResponse(
    [property: JsonPropertyName("pagination")] LuchtmeetnetPagination Pagination,
    [property: JsonPropertyName("data")] List<LuchtmeetnetStationSummary> Data
);

internal sealed record LuchtmeetnetPagination(
    [property: JsonPropertyName("last_page")] int LastPage
);

internal sealed record LuchtmeetnetStationSummary(
    [property: JsonPropertyName("number")] string Id,
    [property: JsonPropertyName("location")] string Name
);

internal sealed record LuchtmeetnetStationDetailResponse(
    [property: JsonPropertyName("data")] LuchtmeetnetStationDetail Data
);

internal sealed record LuchtmeetnetStationDetail(
    [property: JsonPropertyName("number")] string Id,
    [property: JsonPropertyName("location")] string Name,
    [property: JsonPropertyName("geometry")] LuchtmeetnetGeometry Geometry
);

internal sealed record LuchtmeetnetGeometry(
    [property: JsonPropertyName("coordinates")] double[] Coordinates
);

internal sealed record LuchtmeetnetMeasurementResponse(
    [property: JsonPropertyName("data")] List<LuchtmeetnetMeasurement> Data
);

internal sealed record LuchtmeetnetMeasurement(
    [property: JsonPropertyName("value")] double? Value,
    [property: JsonPropertyName("timestamp_measured")] string TimestampMeasured
);
