using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public class AiChatRequest
{
    [Required]
    [MaxLength(5000)]
    public string Prompt { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Model { get; set; }
}
