using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public class UpdateAiModelConfigDto
{
    [Required]
    public string Intent { get; set; } = string.Empty;

    [Required]
    public string PrimaryModel { get; set; } = string.Empty;

    public List<string> FallbackModels { get; set; } = new();

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    [StringLength(2000)]
    public string? SafetySettings { get; set; }
}
