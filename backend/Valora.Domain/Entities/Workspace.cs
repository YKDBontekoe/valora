using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public class Workspace : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }

    // The user who owns/created the workspace
    public required string OwnerId { get; set; }

    // Navigation properties
    public ICollection<WorkspaceMember> Members { get; set; } = new List<WorkspaceMember>();
    public ICollection<SavedListing> SavedListings { get; set; } = new List<SavedListing>();
    public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
}
