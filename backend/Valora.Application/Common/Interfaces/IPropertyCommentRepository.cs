using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IPropertyCommentRepository
{
    Task<PropertyComment> AddCommentAsync(PropertyComment comment, CancellationToken ct = default);
    Task<PropertyComment?> GetCommentAsync(Guid commentId, CancellationToken ct = default);
    Task<List<PropertyComment>> GetCommentsAsync(Guid savedPropertyId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
