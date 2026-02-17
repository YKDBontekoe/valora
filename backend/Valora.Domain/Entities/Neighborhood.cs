using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public class Neighborhood : BaseEntity
{
    public required string Code { get; set; } // e.g. BU03630101
    public required string Name { get; set; }
    public required string City { get; set; }
    public required string Type { get; set; } // "Wijk" or "Buurt"
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Cached stats from CBS
    public double? PopulationDensity { get; set; }
    public double? CrimeRate { get; set; }
    public double? AverageWozValue { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
