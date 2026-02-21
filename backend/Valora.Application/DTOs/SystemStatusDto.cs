namespace Valora.Application.DTOs;

public record SystemStatusDto(
    double DbLatencyMs,
    int QueueDepth,
    string WorkerHealth,
    string DbConnectivity,
    DateTime? LastIngestionRun
);
