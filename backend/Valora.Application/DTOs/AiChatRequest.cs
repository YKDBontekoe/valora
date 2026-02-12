using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public class AiChatRequest
{
    [Required(ErrorMessage = "Prompt is required.")]
    [StringLength(2000)]
    [MinLength(1, ErrorMessage = "Prompt cannot be empty.")]
    public string Prompt { get; set; } = string.Empty;

    [StringLength(50)]
    [RegularExpression(@"^(openai\/gpt-4o-mini|openai\/gpt-4o)$", ErrorMessage = "Invalid model selected.")]
    public string? Model { get; set; }
}
