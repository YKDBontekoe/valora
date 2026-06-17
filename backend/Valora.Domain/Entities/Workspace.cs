using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public class Workspace : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }

    public required string OwnerId { get; set; }

    public ICollection<WorkspaceMember> Members { get; set; } = new List<WorkspaceMember>();
    public ICollection<SavedProperty> SavedProperties { get; set; } = new List<SavedProperty>();
    public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
}
