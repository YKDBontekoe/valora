using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IWorkspacePropertyService
{
    Task<SavedPropertyDto> SavePropertyAsync(string userId, Guid workspaceId, Guid propertyId, string? notes, CancellationToken ct = default);
    Task<List<SavedPropertyDto>> GetSavedPropertiesAsync(string userId, Guid workspaceId, CancellationToken ct = default);
    Task RemoveSavedPropertyAsync(string userId, Guid workspaceId, Guid savedPropertyId, CancellationToken ct = default);

    Task<CommentDto> AddCommentAsync(string userId, Guid workspaceId, Guid savedPropertyId, AddCommentDto dto, CancellationToken ct = default);
    Task<List<CommentDto>> GetCommentsAsync(string userId, Guid workspaceId, Guid savedPropertyId, CancellationToken ct = default);
    
    // New: Save a context report result directly
    Task<SavedPropertyDto> SaveContextReportAsync(string userId, Guid workspaceId, ContextReportDto report, string? notes, CancellationToken ct = default);
}
