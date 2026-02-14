using System.Text.Json;
using System.Text.Json.Serialization;

namespace Valora.Infrastructure.Enrichment;

internal sealed record OverpassResponse(
    [property: JsonPropertyName("elements")] List<OverpassElement> Elements
);

internal sealed record OverpassElement(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("lat")] double? Lat,
    [property: JsonPropertyName("lon")] double? Lon,
    [property: JsonPropertyName("center")] OverpassCenter? Center,
    [property: JsonPropertyName("tags")] JsonElement? Tags
);

internal sealed record OverpassCenter(
    [property: JsonPropertyName("lat")] double Lat,
    [property: JsonPropertyName("lon")] double Lon
);
