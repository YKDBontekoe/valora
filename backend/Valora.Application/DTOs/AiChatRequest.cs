using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public class AiChatRequest
{
    [Required]
    [StringLength(2000)]
    public string Prompt { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Model { get; set; }
}
