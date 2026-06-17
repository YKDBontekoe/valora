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
    PropertySaved,
    PropertyRemoved,
    CommentAdded,
    CommentReplied,
    RoleChanged,
    WorkspaceDeleted,
    UserDeleted
}

public class ActivityLog : BaseEntity
{
    public Guid? WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }

    public required string ActorId { get; set; }
    public ApplicationUser? Actor { get; set; }

    public ActivityLogType Type { get; set; }

    public required string Summary { get; set; }
    public string? Metadata { get; set; }

    public Guid? TargetListingId { get; set; }
}
