namespace Valora.Application.DTOs;

public class SystemHealthDto
{
    public bool IsHealthy => Status == "Healthy";
    public string Status { get; set; } = string.Empty;
    public bool Database { get; set; }
    public int ApiLatency { get; set; }
    public int ApiLatencyP50 { get; set; }
    public int ApiLatencyP95 { get; set; }
    public int ApiLatencyP99 { get; set; }
    public int ActiveJobs { get; set; }
    public int QueuedJobs { get; set; }
    public int FailedJobs { get; set; }
    public DateTime? LastPipelineSuccess { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Error { get; set; }
}
