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

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public WorkspaceRole Role { get; set; }

    public string? InvitedEmail { get; set; }
    public DateTime? JoinedAt { get; set; }

    public bool IsPending => UserId == null && !string.IsNullOrEmpty(InvitedEmail);
}
