using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface ISavedPropertyRepository
{
    Task<SavedProperty?> GetSavedPropertyAsync(Guid workspaceId, Guid propertyId, CancellationToken ct = default);
    Task<SavedProperty?> GetSavedPropertyByIdAsync(Guid savedPropertyId, CancellationToken ct = default);
    Task<List<SavedProperty>> GetSavedPropertiesAsync(Guid workspaceId, CancellationToken ct = default);
    Task<List<SavedPropertyDto>> GetSavedPropertyDtosAsync(Guid workspaceId, CancellationToken ct = default);
    Task<SavedProperty> AddSavedPropertyAsync(SavedProperty savedProperty, CancellationToken ct = default);
    Task RemoveSavedPropertyAsync(SavedProperty savedProperty, CancellationToken ct = default);
    Task<PropertyComment> AddCommentAsync(PropertyComment comment, CancellationToken ct = default);
    Task<PropertyComment?> GetCommentAsync(Guid commentId, CancellationToken ct = default);
    Task<List<PropertyComment>> GetCommentsAsync(Guid savedPropertyId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
