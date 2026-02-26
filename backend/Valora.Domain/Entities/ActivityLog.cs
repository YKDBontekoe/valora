using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public enum ActivityLogType
{
    WorkspaceCreated,
    MemberInvited,
    MemberJoined,
    MemberRemoved,
    ListingSaved,
    ListingRemoved,
    CommentAdded,
    CommentReplied,
    RoleChanged,
    WorkspaceUpdated,
    WorkspaceDeleted
}

public class ActivityLog : BaseEntity
{
    public Guid? WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }

    // Who performed the action
    public required string ActorId { get; set; }
    public ApplicationUser? Actor { get; set; }

    public ActivityLogType Type { get; set; }

    // Details (e.g., "Invited user X", "Saved property at Address Y")
    // Store structured data if needed, but string summary + optional JSON metadata is good.
    public required string Summary { get; set; }
    public string? Metadata { get; set; } // JSON

    // Link to related entities if needed (e.g., target ListingId)
    public Guid? TargetListingId { get; set; }
}
