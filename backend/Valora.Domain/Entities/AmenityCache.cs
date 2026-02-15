using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public class AmenityCache : BaseEntity
{
    public required string LocationKey { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int RadiusMeters { get; set; }
    public int SchoolCount { get; set; }
    public int SupermarketCount { get; set; }
    public int ParkCount { get; set; }
    public int HealthcareCount { get; set; }
    public int TransitStopCount { get; set; }
    public int ChargingStationCount { get; set; }
    public double? NearestAmenityDistanceMeters { get; set; }
    public double DiversityScore { get; set; }
    public DateTimeOffset RetrievedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
