using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public class SavedProperty : BaseEntity
{
    // The Workspace it belongs to
    public Guid WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }

    // The Property being saved
    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }

    // Who added it
    public required string AddedByUserId { get; set; }
    public ApplicationUser? AddedByUser { get; set; }

    // Optional notes/tags
    public string? Notes { get; set; }

    // Comments
    public ICollection<PropertyComment> Comments { get; set; } = new List<PropertyComment>();
}
