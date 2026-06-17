using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public class SavedProperty : BaseEntity
{
    public Guid WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }

    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }

    public required string AddedByUserId { get; set; }
    public ApplicationUser? AddedByUser { get; set; }

    public string? Notes { get; set; }

    public ICollection<PropertyComment> Comments { get; set; } = new List<PropertyComment>();
}
