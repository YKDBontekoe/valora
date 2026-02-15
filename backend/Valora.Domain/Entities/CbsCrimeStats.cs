using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public class CbsCrimeStats : BaseEntity
{
    public required string RegionCode { get; set; }
    public string? DatasetId { get; set; }
    public int? TotalCrimesPer1000 { get; set; }
    public int? BurglaryPer1000 { get; set; }
    public int? ViolentCrimePer1000 { get; set; }
    public int? TheftPer1000 { get; set; }
    public int? VandalismPer1000 { get; set; }
    public double? YearOverYearChangePercent { get; set; }
    public DateTimeOffset RetrievedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
