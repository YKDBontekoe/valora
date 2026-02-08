using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public class AiChatRequest
{
    [Required]
    [MaxLength(2000)]
    public string Prompt { get; set; } = string.Empty;

    [AllowedValues("gpt-4o", "claude-3-5-sonnet")]
    public string? Model { get; set; }
}
