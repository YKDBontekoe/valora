using System.ComponentModel.DataAnnotations;
using Valora.Domain.Entities;

namespace Valora.Application.DTOs;

public record CreateWorkspaceDto(
    [property: Required] [property: StringLength(100)] string Name,
    [property: StringLength(500)] string? Description
);

public record WorkspaceDto(
    Guid Id,
    string Name,
    string? Description,
    string OwnerId,
    DateTime CreatedAt,
    int MemberCount,
    int SavedPropertyCount
);

public record WorkspaceMemberDto(
    Guid Id,
    string? UserId,
    string? Email,
    WorkspaceRole Role,
    bool IsPending,
    DateTime? JoinedAt
);

public record InviteMemberDto(
    [property: Required] [property: EmailAddress] [property: StringLength(254)] string Email,
    [property: Required] WorkspaceRole Role
);

public record SavePropertyDto(
    [property: Required] Guid PropertyId,
    [property: StringLength(2000)] string? Notes
);

public record SavePropertyFromReportDto(
    [property: Required] ContextReportDto Report,
    [property: StringLength(2000)] string? Notes
);

public record SavedPropertyDto(
    Guid Id,
    Guid PropertyId,
    PropertySummaryDto? Property,
    string AddedByUserId,
    string? Notes,
    DateTime AddedAt,
    int CommentCount
);

public record PropertySummaryDto(
    Guid Id,
    string Address,
    string? City,
    int? LivingAreaM2,
    double? SafetyScore,
    double? CompositeScore
);

public record CommentDto(
    Guid Id,
    string UserId,
    string Content,
    DateTime CreatedAt,
    Guid? ParentId,
    List<CommentDto> Replies,
    Dictionary<string, List<string>> Reactions
);

public record AddCommentDto(
    [property: Required] [property: StringLength(2000)] string Content,
    Guid? ParentId
);

public record ActivityLogDto(
    Guid Id,
    string ActorId,
    string Type,
    string Summary,
    DateTime CreatedAt,
    string? Metadata
);
