namespace Valora.Application.DTOs;

public record AiModelDto(
    string Id,
    string Name,
    string Description,
    int ContextLength,
    decimal PromptPrice,
    decimal CompletionPrice
);
