using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public enum WorkspaceRole
{
    Owner,
    Editor,
    Viewer
}

public class WorkspaceMember : BaseEntity
{
    public Guid WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }

    // The user who is a member (FK) - Nullable if pending invite
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // Role (Owner, Editor, Viewer)
    public WorkspaceRole Role { get; set; }

    // Invitation details
    public string? InvitedEmail { get; set; }
    public DateTime? JoinedAt { get; set; }

    public bool IsPending => UserId == null && !string.IsNullOrEmpty(InvitedEmail);
}
