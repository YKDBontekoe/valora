namespace Valora.Application.DTOs;

public class AiModelConfigDto
{
    public Guid Id { get; set; }
    public string Feature { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? SafetySettings { get; set; }
}
