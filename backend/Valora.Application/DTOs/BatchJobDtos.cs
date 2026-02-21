namespace Valora.Application.DTOs;

public record BatchJobDto(
    Guid Id,
    string Type,
    string Status,
    string Target,
    int Progress,
    string? Error,
    string? ResultSummary,
    string? ExecutionLog,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt
);

public record BatchJobSummaryDto(
    Guid Id,
    string Type,
    string Status,
    string Target,
    int Progress,
    string? Error,
    string? ResultSummary,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt
);
