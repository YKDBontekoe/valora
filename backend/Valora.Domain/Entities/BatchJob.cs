using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public enum BatchJobStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public enum BatchJobType
{
    CityIngestion
}

public class BatchJob : BaseEntity
{
    public required BatchJobType Type { get; set; }
    public BatchJobStatus Status { get; set; } = BatchJobStatus.Pending;
    public required string Target { get; set; }
    public int Progress { get; set; }
    public string? Error { get; set; }
    public string? ResultSummary { get; set; }
    public string? ExecutionLog { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
