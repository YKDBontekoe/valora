namespace Valora.Application.DTOs;

public class HealthStatusDto
{
    public required string Status { get; set; }
    public required string DatabaseStatus { get; set; }
    public long ApiLatencyMs { get; set; }
    public int ActiveJobs { get; set; }
    public int QueuedJobs { get; set; }
    public int FailedJobs { get; set; }
    public DateTime? LastPipelineSuccess { get; set; }
    public DateTime Timestamp { get; set; }
}
