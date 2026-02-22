using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public class SavedListing : BaseEntity
{
    // The Workspace it belongs to
    public Guid WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }

    // The Listing being saved
    public Guid ListingId { get; set; }
    public Listing? Listing { get; set; }

    // Who added it
    public required string AddedByUserId { get; set; }
    public ApplicationUser? AddedByUser { get; set; }

    // Optional notes/tags
    public string? Notes { get; set; }

    // Comments
    public ICollection<ListingComment> Comments { get; set; } = new List<ListingComment>();
}
