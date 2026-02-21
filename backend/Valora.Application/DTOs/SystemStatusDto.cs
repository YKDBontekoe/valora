namespace Valora.Application.DTOs;

public record SystemStatusDto(
    double ApiLatencyMs,
    int QueueDepth,
    string WorkerHealth,
    string DbConnectivity,
    DateTime? LastIngestionRun
);
