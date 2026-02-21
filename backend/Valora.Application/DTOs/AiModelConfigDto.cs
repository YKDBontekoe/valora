namespace Valora.Application.DTOs;

public class AiModelConfigDto
{
    public Guid Id { get; set; }
    public string Intent { get; set; } = string.Empty;
    public string PrimaryModel { get; set; } = string.Empty;
    public List<string> FallbackModels { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? SafetySettings { get; set; }
}
