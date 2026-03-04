using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public class UpdateAiModelConfigDto
{
    [Required]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Feature must only contain letters, numbers, and underscores.")]
    [StringLength(100)]
    public string Feature { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string ModelId { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    [StringLength(2000)]
    public string? SafetySettings { get; set; }
}
