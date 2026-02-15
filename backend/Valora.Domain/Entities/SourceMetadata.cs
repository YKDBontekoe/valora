using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public class SourceMetadata : BaseEntity
{
    public required string Source { get; set; }
    public required string DatasetId { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset LastCheckedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
}
