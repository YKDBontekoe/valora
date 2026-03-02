using Valora.Domain.Entities;

namespace Valora.Domain.Extensions;

public static class BatchJobExtensions
{
    public static void AppendLog(this BatchJob job, string message)
    {
        var entry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}";
        if (string.IsNullOrEmpty(job.ExecutionLog))
            job.ExecutionLog = entry;
        else
            job.ExecutionLog += Environment.NewLine + entry;
    }
}
