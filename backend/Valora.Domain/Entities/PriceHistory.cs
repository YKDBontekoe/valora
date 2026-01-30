using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public class PriceHistory : BaseEntity
{
    public Guid ListingId { get; set; }
    public required decimal Price { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    
    public Listing? Listing { get; set; }
}
