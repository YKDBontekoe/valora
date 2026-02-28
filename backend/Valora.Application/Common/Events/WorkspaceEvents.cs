using System;
using Valora.Application.DTOs;

namespace Valora.Application.Common.Events;

public record WorkspaceInviteAcceptedEvent(Guid WorkspaceId, string WorkspaceName, string InviterId, string AcceptedByUserId, string AcceptedByEmail) : IDomainEvent;
public record CommentAddedEvent(Guid WorkspaceId, Guid SavedListingId, Guid CommentId, string UserId, string? Content, Guid? ParentCommentId) : IDomainEvent;
public record ReportSavedToWorkspaceEvent(Guid WorkspaceId, Guid ListingId, string UserId) : IDomainEvent;
