using Valora.Domain.Entities;

namespace Valora.Application.DTOs;

public record CreateWorkspaceDto(string Name, string? Description);

public record WorkspaceDto(
    Guid Id,
    string Name,
    string? Description,
    string OwnerId,
    DateTime CreatedAt,
    int MemberCount,
    int SavedListingCount
);

public record WorkspaceMemberDto(
    Guid Id,
    string? UserId,
    string? Email,
    WorkspaceRole Role,
    bool IsPending,
    DateTime? JoinedAt
);

public record InviteMemberDto(string Email, WorkspaceRole Role);

public record SavedListingDto(
    Guid Id,
    Guid ListingId,
    ListingSummaryDto? Listing,
    string AddedByUserId,
    string? Notes,
    DateTime AddedAt,
    int CommentCount
);

public record ListingSummaryDto(
    Guid Id,
    string Address,
    string? City,
    decimal? Price,
    string? ImageUrl,
    int? Bedrooms,
    int? LivingAreaM2
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

public record AddCommentDto(string Content, Guid? ParentId);

public record ActivityLogDto(
    Guid Id,
    string ActorId,
    string Type,
    string Summary,
    DateTime CreatedAt,
    string? Metadata
);
