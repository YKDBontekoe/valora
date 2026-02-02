using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public class RegionScrapeCursor : BaseEntity
{
    public required string Region { get; set; }
    public int NextBackfillPage { get; set; } = 1;
    public DateTime? LastRecentScrapeUtc { get; set; }
    public DateTime? LastBackfillScrapeUtc { get; set; }
}
