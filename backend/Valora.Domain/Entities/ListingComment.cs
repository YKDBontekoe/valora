using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public class ListingComment : BaseEntity
{
    public Guid SavedListingId { get; set; }
    public SavedListing? SavedListing { get; set; }

    // The user who wrote the comment
    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public required string Content { get; set; }

    // Threading
    public Guid? ParentCommentId { get; set; }
    public ListingComment? ParentComment { get; set; }
    public ICollection<ListingComment> Replies { get; set; } = new List<ListingComment>();

    public Dictionary<string, List<string>> Reactions { get; set; } = new Dictionary<string, List<string>>();
}
