namespace Valora.Application.DTOs;

public record SupportStatusDto(
    bool IsSupportActive,
    string SupportMessage,
    string? StatusPageUrl,
    string? ContactEmail
);
