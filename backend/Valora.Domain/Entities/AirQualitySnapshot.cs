using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public class AirQualitySnapshot : BaseEntity
{
    public required string StationId { get; set; }
    public string? StationName { get; set; }
    public double StationDistanceMeters { get; set; }
    public double? Pm25 { get; set; }
    public double? Pm10 { get; set; }
    public double? No2 { get; set; }
    public double? O3 { get; set; }
    public DateTimeOffset? MeasuredAtUtc { get; set; }
    public DateTimeOffset RetrievedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
