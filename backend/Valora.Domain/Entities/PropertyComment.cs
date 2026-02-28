using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public class PropertyComment : BaseEntity
{
    public Guid SavedPropertyId { get; set; }
    public SavedProperty? SavedProperty { get; set; }

    // The user who wrote the comment
    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public required string Content { get; set; }

    // Threading
    public Guid? ParentCommentId { get; set; }
    public PropertyComment? ParentComment { get; set; }
    public ICollection<PropertyComment> Replies { get; set; } = new List<PropertyComment>();

    public Dictionary<string, List<string>> Reactions { get; set; } = new Dictionary<string, List<string>>();
}
