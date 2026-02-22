using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IContextAnalysisService
{
    Task<string> ChatAsync(string prompt, string? intent, CancellationToken cancellationToken, UserAiProfileDto? sessionProfile = null);
    Task<string> AnalyzeReportAsync(ContextReportDto report, CancellationToken cancellationToken, UserAiProfileDto? sessionProfile = null);
}
