namespace Valora.Application.DTOs;

public class AiChatRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string? Model { get; set; }
}
