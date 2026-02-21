using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IContextAnalysisService
{
    Task<string> ChatAsync(string prompt, string? intent, CancellationToken cancellationToken);
    Task<string> AnalyzeReportAsync(ContextReportDto report, CancellationToken cancellationToken);
}
