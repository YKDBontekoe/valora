using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public class AiModelConfig : BaseEntity
{
    public string Intent { get; set; } = string.Empty;
    public string PrimaryModel { get; set; } = string.Empty;
    public List<string> FallbackModels { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string? SafetySettings { get; set; }
}
