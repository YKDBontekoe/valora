using System.Text.Json;

namespace Valora.Application.DTOs.Map;

public record MapOverlayDto(
    string Id,
    string Name,
    string MetricName,
    double MetricValue,
    string DisplayValue,
    JsonElement GeoJson,
    double? SecondaryMetricValue = null,
    string? SecondaryDisplayValue = null,
    int? SampleSize = null,
    bool HasSufficientData = true);
