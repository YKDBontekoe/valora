using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public class AiChatRequest
{
    [Required(ErrorMessage = "Prompt is required.")]
    [StringLength(2000)]
    [MinLength(1, ErrorMessage = "Prompt cannot be empty.")]
    [RegularExpression(@".*\S.*", ErrorMessage = "Prompt must contain at least one non-whitespace character.")]
    public string Prompt { get; set; } = string.Empty;

    [Required(ErrorMessage = "Intent is required.")]
    [StringLength(50)]
    // Allow alphanumeric and underscores for flexible intents managed via Admin UI
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Intent must only contain letters, numbers, and underscores.")]
    public string Intent { get; set; } = "chat";
}
