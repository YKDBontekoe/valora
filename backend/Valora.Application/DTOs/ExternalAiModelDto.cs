namespace Valora.Application.DTOs;

public class ExternalAiModelDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public int ContextLength { get; set; }
    public decimal PromptPrice { get; set; }
    public decimal CompletionPrice { get; set; }
    public bool IsFree => PromptPrice == 0 && CompletionPrice == 0;
}
