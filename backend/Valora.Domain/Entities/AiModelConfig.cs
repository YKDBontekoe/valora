using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public class AiModelConfig : BaseEntity
{
    public string Feature { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string? SafetySettings { get; set; }
}
